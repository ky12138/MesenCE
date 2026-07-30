#pragma once
#include "pch.h"
#include "Debugger/DebugTypes.h"
#include <string>

class Debugger;
struct AddressInfo;

// ---- Feature entry points: free functions so hot paths don't include this header. ----

// Called from NesDebugger::ProcessInstruction() for indirect-mode instructions.
// No-op when disabled or non-NES.
void RecordIndirectAccessOnInstruction(
	Debugger* debugger, CpuType cpuType,
	uint16_t prevProgramCounter, uint8_t prevOpCode,
	AddressInfo& funcAddr);

// Enable/disable is NOT exposed — tracking is always-on for NES (like MappingTracker).
// Use ResetIndirectRecords() to clear data, SaveIndirectRecordsToFile() to persist.
// Filter mask: bit0=Read, bit1=Write, bit2=Jump.  Default 0x07 (all on).
void SetIndirectTrackerFilter(uint8_t mask);
bool IsIndirectOpEnabled(uint8_t opCode);

void ResetIndirectRecords();

// Write all collected data as JSON to the given file path.
// Returns true on success.
bool SaveIndirectRecordsToFile(const std::string& path);

// Return count for UI awareness.
uint32_t GetIndirectRecordCount();
