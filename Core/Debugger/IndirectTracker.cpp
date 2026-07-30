#include "pch.h"
#include "IndirectTracker.h"
#include "Debugger/Debugger.h"
#include "Debugger/IDebugger.h"
#include "Debugger/CallstackManager.h"
#include "Debugger/Profiler.h"
#include "Core/Shared/Interfaces/IConsole.h"
#include "Core/NES/NesConsole.h"
#include "Core/NES/NesCpu.h"
#include "Core/NES/BaseMapper.h"
#include "Core/NES/NesTypes.h"
#include "Core/NES/NesMemoryManager.h"
#include "Core/NES/Debugger/NesDisUtils.h"
#include <fstream>
#include <map>

using namespace std;

namespace
{
	// ---- Internal storage struct (no interop) ----
	struct Record
	{
		int32_t FuncAddress;
		uint32_t FuncType;
		int32_t InstAbsoluteAddress;
		uint8_t OpCode;
		uint8_t AddrMode;
		uint32_t AbsTarget;
		uint8_t RegA, RegX, RegY, RegPS;
		uint16_t PtrAddress;
		uint32_t PtrMemType;
		uint16_t PtrValue;
		uint32_t TargetMemType;
		uint8_t TargetByte;
	};

	vector<Record> _records;
	unordered_set<uint64_t> _seen;
	constexpr int MAX_RECORDS = 65536;

	// Filter mask: bit0=Read, bit1=Write, bit2=Jump.
	static uint8_t _filterMask = 0x04;

	// Return bit for the opcode category: 1=Read, 2=Write, 4=Jump
	static uint8_t GetIndirectOpCategory(uint8_t opCode) {
		if (opCode == 0x6C) return 4; // JMP (Ind)
		// Indirect write opcodes: STA(0x81,0x91), SAX(0x83), SHA(0x93)
		if (opCode == 0x81 || opCode == 0x91 || opCode == 0x83 || opCode == 0x93)
			return 2;
		return 1; // All others are reads (including RMW)
	}

	// ---- Helpers ----
	static int32_t MakeFuncKey(const AddressInfo& addrInfo) { return addrInfo.Address | ((int32_t)(uint8_t)addrInfo.Type << 24); }
	static uint32_t MakeMemTypeValue(const AddressInfo& addrInfo) { return (uint32_t)addrInfo.Type; }

	static uint32_t MakeRegHash(uint8_t a, uint8_t x, uint8_t y, uint8_t ps) {
		return (uint32_t)a | ((uint32_t)x << 8) | ((uint32_t)y << 16) | ((uint32_t)ps << 24);
	}
	static uint64_t MakeMemHash(uint16_t ptrAddr, uint32_t ptrMemType, uint16_t ptrValue, uint32_t tgtMemType, uint8_t tgtByte) {
		uint64_t hash = 0;
		hash = hash * 16777619ull + ptrAddr; hash = hash * 16777619ull + ptrMemType;
		hash = hash * 16777619ull + ptrValue; hash = hash * 16777619ull + tgtMemType;
		hash = hash * 16777619ull + tgtByte;
		return hash;
	}

	struct IndirectInfo { uint16_t PtrAddr, PtrVal; uint32_t AbsTarget; };

	static IndirectInfo ResolveIndirect(NesCpu* cpu, NesMemoryManager* memManager, uint8_t opCode, uint16_t programCounter)
	{
		IndirectInfo info = {};
		NesAddrMode addrMode = NesDisUtils::GetOpMode(opCode);
		NesCpuState& cpuState = cpu->GetState();
		if(addrMode == NesAddrMode::Ind) {
			uint16_t operandAddr = memManager->DebugRead(programCounter + 1) | ((uint16_t)memManager->DebugRead(programCounter + 2) << 8);
			info.PtrAddr = operandAddr;
			uint8_t loByte = memManager->DebugRead(operandAddr);
			uint16_t hiAddr = (operandAddr & 0xFF) == 0xFF ? (operandAddr - 0xFF) : (operandAddr + 1);
			info.PtrVal = loByte | ((uint16_t)memManager->DebugRead(hiAddr) << 8);
			info.AbsTarget = info.PtrVal;
		} else if(addrMode == NesAddrMode::IndX) {
			uint8_t zpAddr = memManager->DebugRead((programCounter + 1) & 0xFFFF);
			uint8_t zpEffective = (zpAddr + cpuState.X) & 0xFF;
			info.PtrAddr = zpEffective;
			info.PtrVal = memManager->DebugRead(zpEffective) | ((uint16_t)memManager->DebugRead((zpEffective + 1) & 0xFF) << 8);
			info.AbsTarget = info.PtrVal;
		} else if(addrMode == NesAddrMode::IndY || addrMode == NesAddrMode::IndYW) {
			uint8_t zpAddr = memManager->DebugRead((programCounter + 1) & 0xFFFF);
			info.PtrAddr = zpAddr;
			info.PtrVal = memManager->DebugRead(zpAddr) | ((uint16_t)memManager->DebugRead((zpAddr + 1) & 0xFF) << 8);
			info.AbsTarget = info.PtrVal + cpuState.Y;
		}
		return info;
	}

	// ---- Opcode name table (all official + unofficial indirect-mode opcodes) ----
	static const char* LookupOpName(uint8_t opCode) {
		switch(opCode) {
			case 0x01:case 0x11: return "ORA";  case 0x21:case 0x31: return "AND";
			case 0x41:case 0x51: return "EOR";  case 0x61:case 0x71: return "ADC";
			case 0x81:case 0x91: return "STA";  case 0xA1:case 0xB1: return "LDA";
			case 0xC1:case 0xD1: return "CMP";  case 0xE1:case 0xF1: return "SBC";
			case 0x6C: return "JMP";
			case 0x03:case 0x13:case 0x17:case 0x1F:case 0x07:case 0x0F: return "SLO";
			case 0x23:case 0x33:case 0x37:case 0x3F:case 0x27:case 0x2F: return "RLA";
			case 0x43:case 0x53:case 0x57:case 0x5F:case 0x47:case 0x4F: return "SRE";
			case 0x63:case 0x73:case 0x77:case 0x7F:case 0x67:case 0x6F: return "RRA";
			case 0x83:case 0x87:case 0x8F:case 0x97: return "SAX";
			case 0xA3:case 0xB3:case 0xAF:case 0xB7: return "LAX";
			case 0xC3:case 0xD3:case 0xD7:case 0xDF:case 0xC7:case 0xCF:case 0xDB: return "DCP";
			case 0xE3:case 0xF3:case 0xF7:case 0xFF:case 0xE7:case 0xEF:case 0xFB: return "ISB";
			case 0x93:case 0x9F: return "SHA";
			case 0x9C: return "SHY"; case 0x9E: return "SHX";
			case 0xBB: return "LAS"; case 0x9B: return "SHS";
		}
		return nullptr;
	}

	static const char* LookupAddrModeName(uint8_t addrModeValue) {
		switch((NesAddrMode)addrModeValue) {
			case NesAddrMode::Ind: return "Ind"; case NesAddrMode::IndX: return "IndX";
			case NesAddrMode::IndY: return "IndY"; case NesAddrMode::IndYW: return "IndYW";
		}
		return nullptr;
	}

	// Simple static mapping of common MemoryType values to string names.
	// The MemoryType enum is shared between C++/C#; we list the NES-relevant ones.
	static const char* LookupMemoryTypeName(uint32_t memTypeValue) {
		switch((MemoryType)(uint8_t)memTypeValue) {
			case MemoryType::NesPrgRom:       return "NesPrgRom";
			case MemoryType::NesInternalRam:  return "NesInternalRam";
			case MemoryType::NesWorkRam:      return "NesWorkRam";
			case MemoryType::NesSaveRam:      return "NesSaveRam";
			case MemoryType::NesNametableRam: return "NesNametableRam";
			case MemoryType::NesMapperRam:    return "NesMapperRam";
			case MemoryType::NesSpriteRam:    return "NesSpriteRam";
			case MemoryType::NesPaletteRam:   return "NesPaletteRam";
			case MemoryType::NesChrRam:       return "NesChrRam";
			case MemoryType::NesChrRom:       return "NesChrRom";
			case MemoryType::NesMemory:       return "NesMemory";
			case MemoryType::NesPpuMemory:    return "NesPpuMemory";
			case MemoryType::NesSecondarySpriteRam: return "NesSecondarySpriteRam";
		}
		return nullptr;
	}

	// ---- JSON writing helpers ----
	static void WriteJsonString(ostream& output, const char* str) { output << '"' << str << '"'; }
	static void WriteJsonNamedValue(ostream& output, const char* str) {
		if(str) WriteJsonString(output, str);
		else { WriteJsonString(output, "?"); }
	}
	static void WriteJsonInt(ostream& output, int32_t value) { output << value; }
	static void WriteJsonUint(ostream& output, uint32_t value) { output << value; }

	static void WriteJsonFile(const string& path)
	{
		utf8::ofstream file(path, ios::out);
		if(!file) return;

		map<int32_t, map<int32_t, vector<Record*>>> byFunc;
		for(auto& record : _records) {
			byFunc[record.FuncAddress][record.InstAbsoluteAddress].push_back(&record);
		}

		bool firstFunc = true;
		file << "{\n  \"IndirectByCpu\": {\n    \"Nes\": [\n";
		for(auto& funcPair : byFunc) {
			if(!firstFunc) file << ",\n";
			firstFunc = false;
			Record* firstRecord = funcPair.second.begin()->second[0];
			file << "      {\n";
			file << "        \"Address\": "; WriteJsonInt(file, funcPair.first); file << ",\n";
			file << "        \"Type\": "; WriteJsonNamedValue(file, LookupMemoryTypeName(firstRecord->FuncType)); file << ",\n";
			file << "        \"IndirectAccesses\": [\n";

			bool firstInst = true;
			for(auto& instPair : funcPair.second) {
				if(!firstInst) file << ",\n";
				firstInst = false;

				auto& records = instPair.second;
			Record* firstInstRecord = records[0];

			// --- Detect instruction-level invariants ---
			bool tgtMemUniform = true;
			uint32_t commonTgtMem = firstInstRecord->TargetMemType;
				for(auto* record : records) {
					if(record->TargetMemType != commonTgtMem) { tgtMemUniform = false; }
				}

				// --- Group entries by PtrValue ---
				map<uint16_t, vector<Record*>> groups;
				for(auto* record : records) {
					groups[record->PtrValue].push_back(record);
				}

				// --- Output instruction header ---
				file << "          {\n";
				file << "            \"InstAddress\": "; WriteJsonInt(file, (int32_t)instPair.first); file << ",\n";
				file << "            \"OpCode\": "; WriteJsonInt(file, firstRecord->OpCode); file << ",\n";
				file << "            \"OpName\": "; WriteJsonNamedValue(file, LookupOpName(firstRecord->OpCode)); file << ",\n";
				file << "            \"AddrMode\": "; WriteJsonNamedValue(file, LookupAddrModeName(firstRecord->AddrMode)); file << ",\n";
				file << "            \"AbsTgt\": "; WriteJsonUint(file, firstRecord->AbsTarget); file << ",\n";

				// --- "Common" block: instr-level invariants ---
				file << "            \"Common\": {\n";
				file << "              \"PtrAddr\": "; WriteJsonInt(file, (int32_t)firstRecord->PtrAddress); file << ",\n";
				file << "              \"PtrMem\": "; WriteJsonNamedValue(file, LookupMemoryTypeName(firstRecord->PtrMemType));
				if(tgtMemUniform) {
					file << ",\n              \"TgtMem\": "; WriteJsonNamedValue(file, LookupMemoryTypeName(commonTgtMem));
				}
				file << "\n            }";

				// --- Groups ---
				file << ",\n            \"Groups\": [\n";
				bool firstGroup = true;
				for(auto& groupPair : groups) {
					if(!firstGroup) file << ",\n";
					firstGroup = false;

					auto& groupRecords = groupPair.second;
					Record* firstGroupRecord = groupRecords[0];

					file << "              {\n";
					file << "                \"PtrVal\": "; WriteJsonInt(file, (int32_t)groupPair.first);

					// Group-level TgtMem when not uniform across instruction
					if(!tgtMemUniform) {
						bool groupTgtUniform = true;
						uint32_t groupTgtMem = firstGroupRecord->TargetMemType;
						for(auto* record : groupRecords) {
							if(record->TargetMemType != groupTgtMem) { groupTgtUniform = false; break; }
						}
						if(groupTgtUniform) {
							file << ",\n                \"TgtMem\": "; WriteJsonNamedValue(file, LookupMemoryTypeName(groupTgtMem));
						}
					}

					// JMP: no Items, PtrVal alone is enough
					if(firstRecord->OpCode != 0x6C) {
						file << ",\n                \"Items\": [\n";
						bool firstItem = true;
						for(auto* record : groupRecords) {
							if(!firstItem) file << ",\n";
							firstItem = false;
							file << "                  {";
							file << "\"A\":"; WriteJsonInt(file, record->RegA);
							file << ",\"X\":"; WriteJsonInt(file, record->RegX);
							file << ",\"Y\":"; WriteJsonInt(file, record->RegY);
							file << ",\"PS\":"; WriteJsonInt(file, record->RegPS);
							file << ",\"TgtByte\":"; WriteJsonInt(file, record->TargetByte);
							file << "}";
						}
						file << "\n                ]";
					}
					file << "\n              }";
				}
				file << "\n            ]\n          }";
			}
			file << "\n        ]\n      }";
		}
		file << "\n    ]\n  }\n}\n";
		file.close();
	}

} // anonymous namespace

// ---- Public API ----

void RecordIndirectAccessOnInstruction(
	Debugger* debugger, CpuType cpuType,
	uint16_t prevProgramCounter, uint8_t prevOpCode,
	AddressInfo& funcAddr)
{
	if(cpuType != CpuType::Nes || funcAddr.Address < 0) return;

	IConsole* console = debugger->GetConsole();
	if(!console) return;
	NesConsole* nes = dynamic_cast<NesConsole*>(console);
	if(!nes) return;
	NesCpu* cpu = nes->GetCpu();
	NesMemoryManager* memManager = nes->GetMemoryManager();
	BaseMapper* mapper = nes->GetMapper();
	if(!cpu || !memManager || !mapper) return;

	IndirectInfo indirect = ResolveIndirect(cpu, memManager, prevOpCode, prevProgramCounter);
	NesCpuState& cpuState = cpu->GetState();

	Record record = {};
	record.FuncAddress = funcAddr.Address;
	record.FuncType = MakeMemTypeValue(funcAddr);
	record.InstAbsoluteAddress = mapper->GetAbsoluteAddress(prevProgramCounter).Address;
	record.OpCode = prevOpCode;
	record.AddrMode = (uint8_t)NesDisUtils::GetOpMode(prevOpCode);
	record.AbsTarget = indirect.AbsTarget & 0xFFFF;
	record.RegA = cpuState.A; record.RegX = cpuState.X; record.RegY = cpuState.Y; record.RegPS = cpuState.PS;

	record.PtrAddress = indirect.PtrAddr;
	{ AddressInfo addrInfo = mapper->GetAbsoluteAddress(indirect.PtrAddr); record.PtrMemType = MakeMemTypeValue(addrInfo); }
	record.PtrValue = indirect.PtrVal;

	{ AddressInfo addrInfo = mapper->GetAbsoluteAddress(indirect.AbsTarget & 0xFFFF); record.TargetMemType = MakeMemTypeValue(addrInfo); }
	record.TargetByte = prevOpCode == 0x6C ? 0xFF : memManager->DebugRead(indirect.AbsTarget & 0xFFFF);

	int32_t funcHash = MakeFuncKey(funcAddr);
	uint32_t regHash = MakeRegHash(record.RegA, record.RegX, record.RegY, record.RegPS);
	uint64_t memHash = MakeMemHash(record.PtrAddress, record.PtrMemType, record.PtrValue, record.TargetMemType, record.TargetByte);

	uint64_t dedupKey = 0;
	dedupKey = dedupKey * 16777619ull + (uint64_t)(uint32_t)funcHash;
	dedupKey = dedupKey * 16777619ull + (uint64_t)(uint32_t)record.InstAbsoluteAddress;
	dedupKey = dedupKey * 16777619ull + regHash;
	dedupKey = dedupKey * 16777619ull + memHash;

	if(_seen.insert(dedupKey).second && _records.size() < (size_t)MAX_RECORDS) {
		_records.push_back(record);
	}
}

void SetIndirectTrackerFilter(uint8_t mask)
{
	_filterMask = mask;
}

bool IsIndirectOpEnabled(uint8_t opCode)
{
	return (_filterMask & GetIndirectOpCategory(opCode)) != 0;
}

void ResetIndirectRecords()
{
	_records.clear();
	_seen.clear();
}

bool SaveIndirectRecordsToFile(const string& path)
{
	if(_records.empty()) return false;
	WriteJsonFile(path);
	return true;
}

uint32_t GetIndirectRecordCount()
{
	return (uint32_t)_records.size();
}
