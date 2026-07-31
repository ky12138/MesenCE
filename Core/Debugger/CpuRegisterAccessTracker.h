#pragma once
#include "pch.h"
#include "Debugger/DebugTypes.h"

class Debugger;
class IDebugger;

#pragma pack(push, 1)
struct RegisterWriteEntry
{
	uint32_t RelativeAddress;
	int32_t AbsAddress;
	MemoryType AbsMemType;
	uint32_t OldValue;
	uint32_t NewValue;
	uint64_t Sequence;
	uint32_t HitCount;
	uint32_t RegNameCode; //up to 4 ascii chars, little-endian
	uint8_t RegisterId;
	uint8_t ValueSize; //value width in bytes (1/2/4)
};
#pragma pack(pop)

constexpr uint32_t MaxRegisterWriteHistoryEntries = 256;

//Free functions - keeps Debugger.h stable (see AGENTS.md); all state lives in CpuRegisterAccessTracker.cpp
void RecordCpuRegisterAccess(Debugger* debugger, CpuType cpuType, IDebugger* cpuDebugger);
void GetCpuRegisterWriteHistory(CpuType cpuType, RegisterWriteEntry* entries, uint32_t& count);
void SetCpuRegisterWriteHistorySize(uint32_t size);
void ResetCpuRegisterWriteHistory();
