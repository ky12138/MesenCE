#include "pch.h"
#include "Debugger/CpuRegisterAccessTracker.h"
#include "Debugger/Debugger.h"
#include "Debugger/IDebugger.h"
#include "Debugger/DebugUtilities.h"
#include "NES/NesTypes.h"
#include "SNES/SnesCpuTypes.h"
#include "Gameboy/GbTypes.h"
#include "GBA/GbaTypes.h"
#include "PCE/PceTypes.h"
#include "SMS/SmsTypes.h"
#include "WS/WsTypes.h"
#include "Shared/BaseState.h"
#include "Utilities/SimpleLock.h"
#include <deque>
#include <algorithm>

//Tracks the last N writes made to each cpu register, using a state-diff approach:
//on each ProcessInstruction call (before the instruction runs), the current cpu state
//is compared with the snapshot taken on the previous call. Any register that changed
//was written by the previous instruction (GetProgramCounter(true) still returns its PC
//at that point). Entries are deduplicated per instruction address.

namespace
{
	constexpr uint32_t MaxRegsPerCpu = 16;

	struct RegValue
	{
		uint32_t NameCode;
		uint8_t Size;
		uint32_t Value;
	};

	uint32_t GetRegNameCode(const char* name)
	{
		uint32_t code = 0;
		for(int i = 0; i < 4 && name[i]; i++) {
			code |= (uint32_t)(uint8_t)name[i] << (i * 8);
		}
		return code;
	}

	uint32_t ExtractRegisters(CpuType cpuType, BaseState& state, RegValue out[MaxRegsPerCpu])
	{
		//PC and status/flag registers are intentionally excluded (they change on almost
		//every instruction and would drown out meaningful entries)
		switch(cpuType) {
			case CpuType::Nes: {
				NesCpuState& s = (NesCpuState&)state;
				out[0] = { GetRegNameCode("A"), 1, s.A };
				out[1] = { GetRegNameCode("X"), 1, s.X };
				out[2] = { GetRegNameCode("Y"), 1, s.Y };
				out[3] = { GetRegNameCode("SP"), 1, s.SP };
				return 4;
			}

			case CpuType::Snes:
			case CpuType::Sa1: {
				SnesCpuState& s = (SnesCpuState&)state;
				out[0] = { GetRegNameCode("A"), 2, s.A };
				out[1] = { GetRegNameCode("X"), 2, s.X };
				out[2] = { GetRegNameCode("Y"), 2, s.Y };
				out[3] = { GetRegNameCode("SP"), 2, s.SP };
				out[4] = { GetRegNameCode("D"), 2, s.D };
				out[5] = { GetRegNameCode("K"), 1, s.K };
				out[6] = { GetRegNameCode("DBR"), 1, s.DBR };
				return 7;
			}

			case CpuType::Gameboy: {
				GbCpuState& s = (GbCpuState&)state;
				out[0] = { GetRegNameCode("A"), 1, s.A };
				out[1] = { GetRegNameCode("B"), 1, s.B };
				out[2] = { GetRegNameCode("C"), 1, s.C };
				out[3] = { GetRegNameCode("D"), 1, s.D };
				out[4] = { GetRegNameCode("E"), 1, s.E };
				out[5] = { GetRegNameCode("H"), 1, s.H };
				out[6] = { GetRegNameCode("L"), 1, s.L };
				out[7] = { GetRegNameCode("SP"), 2, s.SP };
				return 8;
			}

			case CpuType::Pce: {
				PceCpuState& s = (PceCpuState&)state;
				out[0] = { GetRegNameCode("A"), 1, s.A };
				out[1] = { GetRegNameCode("X"), 1, s.X };
				out[2] = { GetRegNameCode("Y"), 1, s.Y };
				out[3] = { GetRegNameCode("SP"), 1, s.SP };
				return 4;
			}

			case CpuType::Sms: {
				SmsCpuState& s = (SmsCpuState&)state;
				out[0] = { GetRegNameCode("A"), 1, s.A };
				out[1] = { GetRegNameCode("B"), 1, s.B };
				out[2] = { GetRegNameCode("C"), 1, s.C };
				out[3] = { GetRegNameCode("D"), 1, s.D };
				out[4] = { GetRegNameCode("E"), 1, s.E };
				out[5] = { GetRegNameCode("H"), 1, s.H };
				out[6] = { GetRegNameCode("L"), 1, s.L };
				out[7] = { GetRegNameCode("IX"), 2, (uint32_t)((s.IXH << 8) | s.IXL) };
				out[8] = { GetRegNameCode("IY"), 2, (uint32_t)((s.IYH << 8) | s.IYL) };
				out[9] = { GetRegNameCode("SP"), 2, s.SP };
				out[10] = { GetRegNameCode("I"), 1, s.I };
				return 11;
			}

			case CpuType::Gba: {
				GbaCpuState& s = (GbaCpuState&)state;
				static constexpr const char* names[15] = { "R0", "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9", "R10", "R11", "R12", "SP", "LR" };
				for(int i = 0; i < 15; i++) {
					out[i] = { GetRegNameCode(names[i]), 4, s.R[i] };
				}
				return 15;
			}

			case CpuType::Ws: {
				WsCpuState& s = (WsCpuState&)state;
				out[0] = { GetRegNameCode("AX"), 2, s.AX };
				out[1] = { GetRegNameCode("BX"), 2, s.BX };
				out[2] = { GetRegNameCode("CX"), 2, s.CX };
				out[3] = { GetRegNameCode("DX"), 2, s.DX };
				out[4] = { GetRegNameCode("SP"), 2, s.SP };
				out[5] = { GetRegNameCode("BP"), 2, s.BP };
				out[6] = { GetRegNameCode("SI"), 2, s.SI };
				out[7] = { GetRegNameCode("DI"), 2, s.DI };
				out[8] = { GetRegNameCode("CS"), 2, s.CS };
				out[9] = { GetRegNameCode("DS"), 2, s.DS };
				out[10] = { GetRegNameCode("ES"), 2, s.ES };
				out[11] = { GetRegNameCode("SS"), 2, s.SS };
				return 12;
			}

			default:
				//Coprocessors (Spc, Gsu, NecDsp, Cx4, St018) are not tracked
				return 0;
		}
	}

	struct CpuTrackerState
	{
		bool HasSnapshot = false;
		uint32_t PrevCount = 0;
		RegValue Prev[MaxRegsPerCpu] = {};
		std::deque<RegisterWriteEntry> History[MaxRegsPerCpu];

		void Clear()
		{
			HasSnapshot = false;
			PrevCount = 0;
			for(uint32_t i = 0; i < MaxRegsPerCpu; i++) {
				History[i].clear();
			}
		}
	};

	SimpleLock _lock;
	Debugger* _owner = nullptr;
	uint64_t _sequence = 0;
	uint32_t _maxEntriesPerRegister = 3;
	CpuTrackerState _cpuTrackers[(int)DebugUtilities::GetLastCpuType() + 1];
}

void RecordCpuRegisterAccess(Debugger* debugger, CpuType cpuType, IDebugger* cpuDebugger)
{
	RegValue current[MaxRegsPerCpu];
	uint32_t count = ExtractRegisters(cpuType, cpuDebugger->GetState(), current);
	if(count == 0) {
		return;
	}

	auto lock = _lock.AcquireSafe();

	if(_owner != debugger) {
		//New debugger instance (rom reload/reset) - clear all history
		for(CpuTrackerState& tracker : _cpuTrackers) {
			tracker.Clear();
		}
		_owner = debugger;
	}

	CpuTrackerState& tracker = _cpuTrackers[(int)cpuType];

	if(tracker.HasSnapshot && tracker.PrevCount == count) {
		//GetProgramCounter(true) still holds the PC of the previous (just completed) instruction,
		//because this runs before the console-specific ProcessInstruction updates it
		uint32_t prevPc = cpuDebugger->GetProgramCounter(true);
		bool addrResolved = false;
		AddressInfo absAddr = { -1, MemoryType::None };

		for(uint32_t i = 0; i < count; i++) {
			if(current[i].Value == tracker.Prev[i].Value) {
				continue;
			}

			std::deque<RegisterWriteEntry>& history = tracker.History[i];
			auto match = std::find_if(history.begin(), history.end(), [&](RegisterWriteEntry& e) { return e.RelativeAddress == prevPc; });
			if(match != history.end()) {
				//Same instruction address already recorded for this register - update it and move to front
				RegisterWriteEntry entry = *match;
				entry.OldValue = tracker.Prev[i].Value;
				entry.NewValue = current[i].Value;
				entry.HitCount++;
				entry.Sequence = ++_sequence;
				history.erase(match);
				history.push_front(entry);
			} else {
				if(!addrResolved) {
					AddressInfo relAddr = { (int32_t)prevPc, DebugUtilities::GetCpuMemoryType(cpuType) };
					absAddr = debugger->GetAbsoluteAddress(relAddr);
					addrResolved = true;
				}

				RegisterWriteEntry entry = {};
				entry.RelativeAddress = prevPc;
				entry.AbsAddress = absAddr.Address;
				entry.AbsMemType = absAddr.Type;
				entry.OldValue = tracker.Prev[i].Value;
				entry.NewValue = current[i].Value;
				entry.Sequence = ++_sequence;
				entry.HitCount = 1;
				entry.RegNameCode = current[i].NameCode;
				entry.RegisterId = (uint8_t)i;
				entry.ValueSize = current[i].Size;

				history.push_front(entry);
				while(history.size() > _maxEntriesPerRegister) {
					history.pop_back();
				}
			}
		}
	}

	memcpy(tracker.Prev, current, sizeof(RegValue) * count);
	tracker.PrevCount = count;
	tracker.HasSnapshot = true;
}

void GetCpuRegisterWriteHistory(CpuType cpuType, RegisterWriteEntry* entries, uint32_t& count)
{
	auto lock = _lock.AcquireSafe();

	count = 0;
	CpuTrackerState& tracker = _cpuTrackers[(int)cpuType];
	for(uint32_t i = 0; i < MaxRegsPerCpu; i++) {
		for(RegisterWriteEntry& entry : tracker.History[i]) {
			if(count >= MaxRegisterWriteHistoryEntries) {
				return;
			}
			entries[count++] = entry;
		}
	}
}

void SetCpuRegisterWriteHistorySize(uint32_t size)
{
	auto lock = _lock.AcquireSafe();

	_maxEntriesPerRegister = std::clamp<uint32_t>(size, 1, 5);
	for(CpuTrackerState& tracker : _cpuTrackers) {
		for(uint32_t i = 0; i < MaxRegsPerCpu; i++) {
			while(tracker.History[i].size() > _maxEntriesPerRegister) {
				tracker.History[i].pop_back();
			}
		}
	}
}

void ResetCpuRegisterWriteHistory()
{
	auto lock = _lock.AcquireSafe();
	for(CpuTrackerState& tracker : _cpuTrackers) {
		tracker.Clear();
	}
}
