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
	void Reset();
};
