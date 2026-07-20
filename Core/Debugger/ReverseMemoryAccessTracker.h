#pragma once
#include "pch.h"
#include "Debugger/DebugTypes.h"
#include "Shared/MemoryOperationType.h"

class Debugger;
class Breakpoint;
struct MemoryOperationInfo;
struct AddressInfo;

// Reverse of FunctionMemoryAccessTracker: instead of tracking which memory
// addresses each function touches, this records which functions have accessed
// a given memory/ROM address (with R/W/E type and a running count). It is driven
// by breakpoints flagged for recording (Breakpoint::IsRecord): when such a
// breakpoint matches and its condition passes, the currently executing function
// is recorded as having accessed the matched address. Recording never pauses
// emulation and is independent of whether the breakpoint is enabled (pauses).
void RecordReverseMemoryAccess(Debugger* debugger, CpuType cpuType, Breakpoint& bp, MemoryOperationInfo& operation, AddressInfo& address);

// Per-address cap on distinct functions recorded. Stops _accessMap from growing
// without bound when a Record breakpoint spans a large region. Counts for already
// recorded functions keep updating past the cap.
constexpr uint32_t MaxFunctionsPerAddress = 16384;

// Packed output structure returned to the UI. The C# side parses it as
// int32 FuncAddress, int32 FuncType (4 bytes), uint8 Flags, uint32 AccessCount
// (13 bytes per entry), laid out as [Functions...][Count] so the C# parser reads
// entries at i*13 and Count at 1024*13 (mirrors FunctionMemoryAccessRecord).
#pragma pack(push, 1)
struct MemoryAccessFunctionRecord
{
	struct FunctionEntry {
		int32_t FuncAddress;  // function absolute address
		MemoryType FuncType;   // 4 bytes
		uint8_t Flags;         // bit0 = Read, bit1 = Write, bit2 = Execute
		uint32_t AccessCount;  // total accesses by this function (live only)
	} Functions[1024];
	uint32_t Count;
};
#pragma pack(pop)

// Packed interop record for JSON persistence. To avoid one entry per address
// (which explodes when a Record breakpoint spans a range), consecutive addresses
// that share the same (func, flags) are coalesced into a single range record.
// MemoryType is 4 bytes, so each record is 4+4+4+4+4+1 = 21 bytes.
#pragma pack(push, 1)
struct ReverseAccessDumpRecord
{
	int32_t StartAddr;  // inclusive start of the coalesced address range
	int32_t EndAddr;    // inclusive end of the coalesced address range
	MemoryType MemType;
	int32_t FuncAddress;
	MemoryType FuncType;
	uint8_t Flags;
};
#pragma pack(pop)

// Tracks which functions (ROM/RAM) have accessed each memory address.
// Mirrors FunctionMemoryAccessTracker but with the map inverted:
//   funcKey -> memKey   (forward)   becomes   memKey -> funcKey   (reverse)
class ReverseMemoryAccessTracker
{
private:
	struct AccessInfo {
		uint8_t Flags = 0;     // bit0 = Read, bit1 = Write, bit2 = Execute
		uint32_t AccessCount = 0;
	};

	// memKey -> (funcKey -> AccessInfo)
	unordered_map<int64_t, unordered_map<int32_t, AccessInfo>> _accessMap;

	static int32_t MakeFuncKey(AddressInfo& addr)
	{
		return addr.Address | ((int32_t)(uint8_t)addr.Type << 24);
	}

	static int64_t MakeMemKey(AddressInfo& addr)
	{
		return ((int64_t)addr.Type << 40) | (int64_t)addr.Address;
	}

public:
	ReverseMemoryAccessTracker();
	~ReverseMemoryAccessTracker();

	// True when at least one address has been recorded. Used by the UI to know
	// whether any reverse data exists.
	bool HasRecorded() const { return !_accessMap.empty(); }

	// Record that funcAddr accessed memAddr with the given operation type.
	// No-op if either address is invalid.
	void RecordAccess(AddressInfo& memAddr, AddressInfo& funcAddr, MemoryOperationType opType);

	// Aggregate, per function, all accesses recorded for addresses within
	// [start, end] of the given memory type. Output sorted by function address.
	void GetMemoryAccessFunctions(MemoryType memType, uint32_t start, uint32_t end, MemoryAccessFunctionRecord& output);

	// Dump every recorded (mem range, func, flags) entry for JSON persistence.
	// Consecutive addresses sharing the same (func, flags) are coalesced into a
	// single range so a Record breakpoint's region stays compact. Capped at
	// maxRecords to avoid pathological blowup on large recorded regions.
	void GetAllRecords(std::vector<ReverseAccessDumpRecord>& out, size_t maxRecords = 65536) const;

	// Re-insert persisted range entries (OR flags) into the tracker. Ranges are
	// expanded back into per-address entries so GetMemoryAccessFunctions keeps
	// working. Safe to call on a non-empty tracker (merges with existing data).
	void LoadRecords(const std::vector<ReverseAccessDumpRecord>& in);

	void Reset();
};
