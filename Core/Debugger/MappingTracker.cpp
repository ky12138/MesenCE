#include "pch.h"
#include "MappingTracker.h"
#include "Debugger/Debugger.h"
#include "Debugger/IDebugger.h"
#include "Core/Shared/Interfaces/IConsole.h"
#include "Core/NES/NesConsole.h"
#include "Core/NES/BaseMapper.h"
#include "Core/NES/NesTypes.h"

namespace
{
	// ---- PRG: funcKey -> list of snapshots ----
	// Each snapshot = vector of PrgSlotEntry for every bank in $8000-$FFFF
	unordered_map<int32_t, vector<vector<PrgSlotEntry>>> _recordedPrgMappings;
	unordered_map<int32_t, int32_t> _funcPrgPageSizes;
	unordered_map<int32_t, unordered_set<uint64_t>> _seenPrgFingerprints;

	// ---- CHR: funcKey -> list of snapshots ----
	// Each snapshot = vector of ChrSlotEntry for every bank in $0000-$1FFF
	// Only populated when cartridge has CHR-ROM.
	unordered_map<int32_t, vector<vector<ChrSlotEntry>>> _recordedChrMappings;
	unordered_map<int32_t, int32_t> _funcChrPageSizes;
	unordered_map<int32_t, unordered_set<uint64_t>> _seenChrFingerprints;
	bool _hasChrRom = false;

	constexpr int MAX_MAPPINGS_PER_FUNC = 32;
	constexpr int PRG_CPU_START = 0x80;  // $8000
	constexpr int PRG_CPU_COUNT = 0x80;  // $8000-$FFFF = 128 pages of 0x100 bytes
	constexpr int CHR_PPU_START = 0x00;  // $0000
	constexpr int CHR_PPU_COUNT = 0x20;  // $0000-$1FFF = 32 pages of 0x100 bytes

	static int32_t MakeFuncKey(const AddressInfo& addr)
	{
		return addr.Address | ((int32_t)(uint8_t)addr.Type << 24);
	}

	// ---- PRG fingerprint & capture ----

	static uint64_t ComputePrgFingerprint(BaseMapper* mapper)
	{
		const CartridgeState& state = mapper->GetState();
		uint64_t h = 0;
		for(int i = PRG_CPU_START; i < PRG_CPU_START + PRG_CPU_COUNT; i++) {
			h = h * 16777619ull + (uint64_t)(uint32_t)state.PrgMemoryOffset[i];
			h = h * 16777619ull + (uint64_t)(uint8_t)state.PrgType[i];
		}
		return h;
	}

	static void CapturePrgSnapshot(BaseMapper* mapper, vector<PrgSlotEntry>& out, int32_t& outPageSize)
	{
		const CartridgeState& state = mapper->GetState();
		outPageSize = (int32_t)state.PrgPageSize;
		int bankCount = 0x8000 / (int)state.PrgPageSize;
		int pagesPerBank = (int)state.PrgPageSize / 0x100;
		out.resize(bankCount);
		for(int i = 0; i < bankCount; i++) {
			int idx = PRG_CPU_START + i * pagesPerBank;
			if(state.PrgType[idx] == PrgMemoryType::PrgRom) {
				out[i].PageNumber = state.PrgMemoryOffset[idx] / (int32_t)state.PrgPageSize;
			} else {
				out[i].PageNumber = -1;
			}
			out[i].PrgType = (uint32_t)state.PrgType[idx];
		}
	}

	// ---- CHR fingerprint & capture ----

	static uint64_t ComputeChrFingerprint(BaseMapper* mapper)
	{
		const CartridgeState& state = mapper->GetState();
		uint64_t h = 0;
		for(int i = CHR_PPU_START; i < CHR_PPU_START + CHR_PPU_COUNT; i++) {
			h = h * 16777619ull + (uint64_t)(uint32_t)state.ChrMemoryOffset[i];
			h = h * 16777619ull + (uint64_t)(uint8_t)state.ChrType[i];
		}
		return h;
	}

	static void CaptureChrSnapshot(BaseMapper* mapper, vector<ChrSlotEntry>& out, int32_t& outPageSize)
	{
		const CartridgeState& state = mapper->GetState();
		outPageSize = (int32_t)state.ChrPageSize;
		int bankCount = 0x2000 / (int)state.ChrPageSize;
		int pagesPerBank = (int)state.ChrPageSize / 0x100;
		out.resize(bankCount);
		for(int i = 0; i < bankCount; i++) {
			int idx = CHR_PPU_START + i * pagesPerBank;
			if(state.ChrType[idx] == ChrMemoryType::ChrRom) {
				out[i].PageNumber = state.ChrMemoryOffset[idx] / (int32_t)state.ChrPageSize;
			} else {
				out[i].PageNumber = -1;
			}
			out[i].ChrType = (uint32_t)state.ChrType[idx];
		}
	}
} // anonymous namespace

void RecordMappingOnFunctionCall(Debugger* debugger, CpuType cpuType, AddressInfo& funcAddr)
{
	if(cpuType != CpuType::Nes) {
		return;
	}

	int32_t fkey = MakeFuncKey(funcAddr);

	IConsole* console = debugger->GetConsole();
	if(!console) {
		return;
	}

	NesConsole* nes = dynamic_cast<NesConsole*>(console);
	if(!nes) {
		return;
	}

	BaseMapper* mapper = nes->GetMapper();
	if(!mapper) {
		return;
	}

	const CartridgeState& state = mapper->GetState();

	// ---- PRG: always record ----
	uint64_t prgFingerprint = ComputePrgFingerprint(mapper);
	if(_seenPrgFingerprints[fkey].insert(prgFingerprint).second) {
		vector<PrgSlotEntry> snapshot;
		int32_t pageSize = 0;
		CapturePrgSnapshot(mapper, snapshot, pageSize);

		auto& records = _recordedPrgMappings[fkey];
		if(records.size() < (size_t)MAX_MAPPINGS_PER_FUNC) {
			records.push_back(std::move(snapshot));
			if(_funcPrgPageSizes.find(fkey) == _funcPrgPageSizes.end() && pageSize > 0) {
				_funcPrgPageSizes[fkey] = pageSize;
			}
		}
	}

	// ---- CHR: only record when cartridge has CHR-ROM ----
	if(state.ChrRomSize > 0) {
		_hasChrRom = true;

		uint64_t chrFingerprint = ComputeChrFingerprint(mapper);
		if(_seenChrFingerprints[fkey].insert(chrFingerprint).second) {
			vector<ChrSlotEntry> snapshot;
			int32_t pageSize = 0;
			CaptureChrSnapshot(mapper, snapshot, pageSize);

			auto& records = _recordedChrMappings[fkey];
			if(records.size() < (size_t)MAX_MAPPINGS_PER_FUNC) {
				records.push_back(std::move(snapshot));
				if(_funcChrPageSizes.find(fkey) == _funcChrPageSizes.end() && pageSize > 0) {
					_funcChrPageSizes[fkey] = pageSize;
				}
			}
		}
	}
}

void ResetMappingRecords()
{
	_seenPrgFingerprints.clear();
	_recordedPrgMappings.clear();
	_funcPrgPageSizes.clear();
	_seenChrFingerprints.clear();
	_recordedChrMappings.clear();
	_funcChrPageSizes.clear();
	_hasChrRom = false;
}

void GetPrgMappingRecords(
	AddressInfo& funcAddr,
	int32_t* prgPageSize,
	PrgSlotEntry* output, uint32_t maxEntries, uint32_t* outCount)
{
	*prgPageSize = -1;
	*outCount = 0;

	int32_t fkey = MakeFuncKey(funcAddr);
	auto pit = _funcPrgPageSizes.find(fkey);
	if(pit != _funcPrgPageSizes.end()) {
		*prgPageSize = pit->second;
	}

	auto rit = _recordedPrgMappings.find(fkey);
	if(rit == _recordedPrgMappings.end()) {
		return;
	}

	uint32_t idx = 0;
	for(const auto& snapshot : rit->second) {
		for(const auto& entry : snapshot) {
			if(idx >= maxEntries) {
				break;
			}
			output[idx] = entry;
			idx++;
		}
		// Sentinel to separate snapshots
		if(idx < maxEntries) {
			output[idx].PageNumber = INT32_MIN;
			output[idx].PrgType = 0;
			idx++;
		}
	}
	*outCount = idx;
}

void GetChrMappingRecords(
	AddressInfo& funcAddr,
	int32_t* chrPageSize,
	ChrSlotEntry* output, uint32_t maxEntries, uint32_t* outCount)
{
	*chrPageSize = -1;
	*outCount = 0;

	int32_t fkey = MakeFuncKey(funcAddr);
	auto pit = _funcChrPageSizes.find(fkey);
	if(pit != _funcChrPageSizes.end()) {
		*chrPageSize = pit->second;
	}

	auto rit = _recordedChrMappings.find(fkey);
	if(rit == _recordedChrMappings.end()) {
		return;
	}

	uint32_t idx = 0;
	for(const auto& snapshot : rit->second) {
		for(const auto& entry : snapshot) {
			if(idx >= maxEntries) {
				break;
			}
			output[idx] = entry;
			idx++;
		}
		// Sentinel to separate snapshots
		if(idx < maxEntries) {
			output[idx].PageNumber = INT32_MIN;
			output[idx].ChrType = 0;
			idx++;
		}
	}
	*outCount = idx;
}
