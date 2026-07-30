#pragma once
#include "pch.h"
#include "Debugger/DebugTypes.h"

#pragma pack(push, 1)
struct CallerCalleeEntry
{
	AddressInfo Address;
	uint64_t CallCount;
};

struct CallerCalleeRecord
{
	CallerCalleeEntry Callers[64];
	uint32_t CallerCount;
	CallerCalleeEntry Callees[64];
	uint32_t CalleeCount;
};

// Packed interop edge for JSON persistence / bulk cache population of the
// caller/callee graph. One entry per directed call edge (caller -> callee)
// with its call count. MemoryType is 4 bytes, so each edge is 4+4+4+4+8 = 24 bytes.
struct CallerCalleeEdge
{
	int32_t CallerAddress;
	MemoryType CallerType;
	int32_t CalleeAddress;
	MemoryType CalleeType;
	uint64_t CallCount;
};
#pragma pack(pop)

class CallerCalleeTracker
{
private:
	unordered_map<int32_t, unordered_map<int32_t, uint64_t>> _callerMap;
	unordered_map<int32_t, unordered_map<int32_t, uint64_t>> _calleeMap;

	static int32_t MakeKey(AddressInfo& addr)
	{
		return addr.Address | ((int32_t)addr.Type << 24);
	}

public:
	CallerCalleeTracker();
	~CallerCalleeTracker();

	void RecordCall(AddressInfo& caller, AddressInfo& callee);
	void GetCallerCalleeData(AddressInfo& funcAddr, CallerCalleeRecord& output);
	// Dump every recorded (caller -> callee, count) edge for JSON persistence /
	// bulk caller/callee cache population. Capped at maxEdges to avoid pathological blowup.
	void GetAllEdges(std::vector<CallerCalleeEdge>& out, size_t maxEdges = 65536) const;
	void SetEnabled(bool enabled) { _enabled = enabled; }
	void Reset();

private:
	bool _enabled = true;
};
