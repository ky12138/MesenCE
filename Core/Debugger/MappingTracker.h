#pragma once
#include "pch.h"
#include "Debugger/DebugTypes.h"

class Debugger;
class IDebugger;
struct AddressInfo;

// Packed records for interop with C#: one slot's (pageNumber, type).
// Mirrors the C# on the managed side; kept tightly packed for bulk transfer.
#pragma pack(push, 1)
struct PrgSlotEntry
{
	int32_t PageNumber;  // PRG-ROM page number, or -1 if slot is not PRG-ROM
	uint32_t PrgType;    // PrgMemoryType enum value
};

struct ChrSlotEntry
{
	int32_t PageNumber;  // CHR-ROM page number, or -1 if slot is not CHR-ROM
	uint32_t ChrType;    // ChrMemoryType enum value
};
#pragma pack(pop)

// ---- Feature entry points: free functions so profiling hot paths don't
//      include this header and edits don't force rebuilds. ----

// Call from Profiler::StackFunction (per function call) to incrementally
// record the current NES PRG + CHR page mapping for the called function.
// CHR mapping is only recorded when the cartridge has CHR-ROM.
// No-op for non-NES cpuTypes.
void RecordMappingOnFunctionCall(Debugger* debugger, CpuType cpuType, AddressInfo& funcAddr);

// Reset all recorded mapping data (PRG + CHR).
void ResetMappingRecords();

// Enable/disable mapping tracking at runtime. When disabled,
// RecordMappingOnFunctionCall is a no-op, avoiding mapper state
// queries and fingerprint computation overhead. Default: enabled.
void SetMappingTrackingEnabled(bool enabled);

// Copy all recorded PrgSlotEntry lists for a function into the output buffer.
// prgPageSize returns the page size (0x2000/0x4000 etc), -1 if unknown.
// outCount returns the number of entries written (capped at maxEntries).
// Each snapshot is terminated by a sentinel entry with PageNumber = INT32_MIN.
void GetPrgMappingRecords(
	AddressInfo& funcAddr,
	int32_t* prgPageSize,
	PrgSlotEntry* output, uint32_t maxEntries, uint32_t* outCount);

// Copy all recorded ChrSlotEntry lists for a function into the output buffer.
// chrPageSize returns the page size (0x2000/0x4000 etc), -1 if unknown or no CHR-ROM.
// outCount returns the number of entries written (capped at maxEntries).
// Each snapshot is terminated by a sentinel entry with PageNumber = INT32_MIN.
void GetChrMappingRecords(
	AddressInfo& funcAddr,
	int32_t* chrPageSize,
	ChrSlotEntry* output, uint32_t maxEntries, uint32_t* outCount);
