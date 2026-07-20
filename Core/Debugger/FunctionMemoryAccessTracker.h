#pragma once
#include "pch.h"
#include "Debugger/DebugTypes.h"
#include "Shared/MemoryOperationType.h"

// Drives per-memory-access recording for the Function Memory Access feature.
// Defined in FunctionMemoryAccessTracker.cpp so the only thing the debugger's
// hot path (Debugger::ProcessBreakConditions) needs is this single call — all
// feature logic lives outside Debugger.cpp/h and never perturbs incremental builds.
class Debugger;
void RecordFunctionMemoryAccess(Debugger* debugger, CpuType cpuType, AddressInfo& memAddr, MemoryOperationType opType);

// Per-function cap on distinct tracked addresses. Stops _accessMap from growing
// without bound on pathological access patterns (e.g. a function touching a huge
// address space). Counts for already-tracked addresses keep updating past the cap.
constexpr uint32_t MaxTrackedAddressesPerFunction = 16384;

// Stride-merge limits for GetFunctionMemoryAccess. Addresses are coalesced into a
// single range only when consecutive accesses are evenly spaced and similarly
// frequent. MaxStride bounds the gap; MaxRunLength bounds run length; the factor
// bounds how different two accesses' counts may be before they split into runs.
constexpr uint32_t MaxStride = 0xF; // stride ∈ [1, 16]
constexpr uint32_t MaxRunLength = 256;
constexpr uint32_t AccessSimilarityFactor = 2;

// Default mask: only regular data R/W + DMA R/W are tracked (matches old behavior).
// Instruction fetch / dummy / PPU render reads are opt-in via the UI checkboxes.
constexpr uint32_t DefaultFunctionMemAccessMask =
	(1u << (uint8_t)MemoryOperationType::Read) |
	(1u << (uint8_t)MemoryOperationType::Write) |
	(1u << (uint8_t)MemoryOperationType::DmaRead) |
	(1u << (uint8_t)MemoryOperationType::DmaWrite);

#pragma pack(push, 1)
struct FunctionAccessRange
{
	uint32_t Start;
	uint32_t Length;
	MemoryType Type;
	uint8_t Flags;     // bit0 = Read, bit1 = Write
	uint32_t ReadCount;   // total read accesses to this range (live tracking only)
	uint32_t WriteCount;  // total write accesses to this range (live tracking only)
	uint32_t AccessCount; // ReadCount + WriteCount (live tracking only)
	uint32_t Interval;    // stride between consecutive addresses of a merged run
	                       // (1 = contiguous; >1 = evenly-spaced accesses, e.g. $1/$3/$5)
};

struct FunctionMemoryAccessRecord
{
	FunctionAccessRange Ranges[1024];
	uint32_t Count;
};
#pragma pack(pop)

// Tracks which memory addresses (ROM/RAM) each function reads/writes.
// Mirrors CallerCalleeTracker, but records memory accesses instead of call relations.
// Only functions explicitly marked for tracking (via SetTracked) are recorded,
// to keep the per-access overhead negligible during emulation.
class FunctionMemoryAccessTracker
{
private:
	// Per-address access info (flags + counts) for each tracked function.
	struct AccessInfo
	{
		uint8_t Flags = 0;     // bit0 = Read, bit1 = Write
		uint32_t ReadCount = 0;
		uint32_t WriteCount = 0;
	};

	// funcKey -> (memKey -> AccessInfo)
	unordered_map<int32_t, unordered_map<int64_t, AccessInfo>> _accessMap;

	static int32_t MakeFuncKey(AddressInfo& addr)
	{
		return addr.Address | ((int32_t)(uint8_t)addr.Type << 24);
	}

	static int64_t MakeMemKey(AddressInfo& addr)
	{
		return ((int64_t)addr.Type << 40) | (int64_t)addr.Address;
	}

public:
	FunctionMemoryAccessTracker();
	~FunctionMemoryAccessTracker();

	// Mark/unmark a function for tracking. Tracking only records accesses for marked functions.
	void SetTracked(AddressInfo& funcAddr, bool tracked);
	bool IsTracked(AddressInfo& funcAddr);

	// True when at least one function is currently marked for tracking. Used by the
	// debugger's per-memory-access hot path to skip all tracking work when nothing
	// is tracked (cheaper than touching the profiler on every access).
	bool HasTrackedFunctions() const { return !_accessMap.empty(); }

	// Set the bitmask of MemoryOperationType values to record (opt-in special accesses).
	void SetRecordMask(uint32_t mask);
	uint32_t GetRecordMask() const;

	// Record a memory access performed by funcAddr. No-op if funcAddr is not tracked
	// or if opType is not enabled in the current record mask.
	void RecordAccess(AddressInfo& funcAddr, AddressInfo& memAddr, MemoryOperationType opType);

	// Get the merged, run-length-compressed access ranges for a function.
	void GetFunctionMemoryAccess(AddressInfo& funcAddr, FunctionMemoryAccessRecord& output);

	// Get the per-address access details (one entry per address, Length == 1)
	// within [start, end] for the given memory type. Used by the UI to "drill down"
	// into a merged range and inspect its individual addresses. When interval > 1
	// (a stride range), only addresses of the form start + k*interval are returned,
	// keeping the drill-down aligned with the merged run the user expanded.
	void GetFunctionMemoryAccessDetails(AddressInfo& funcAddr, MemoryType memType, uint32_t start, uint32_t end, uint32_t interval, FunctionMemoryAccessRecord& output);

	void Reset();

private:
	uint32_t _recordMask = DefaultFunctionMemAccessMask;
};
