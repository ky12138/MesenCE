#include "pch.h"
#include "Debugger/CallerCalleeTracker.h"
#include "Debugger/DebugBreakHelper.h"

CallerCalleeTracker::CallerCalleeTracker()
{
}

CallerCalleeTracker::~CallerCalleeTracker()
{
}

void CallerCalleeTracker::RecordCall(AddressInfo& caller, AddressInfo& callee)
{
	if(!_enabled) return;

	int32_t callerKey = MakeKey(caller);
	int32_t calleeKey = MakeKey(callee);

	_calleeMap[callerKey][calleeKey]++;
	_callerMap[calleeKey][callerKey]++;
}

void CallerCalleeTracker::GetCallerCalleeData(AddressInfo& funcAddr, CallerCalleeRecord& output)
{
	int32_t key = MakeKey(funcAddr);

	output.CallerCount = 0;
	auto callerIt = _callerMap.find(key);
	if(callerIt != _callerMap.end()) {
		for(auto& [callerKey, count] : callerIt->second) {
			if(output.CallerCount >= 64) break;
			CallerCalleeEntry& entry = output.Callers[output.CallerCount];
			entry.Address.Address = callerKey & 0x00FFFFFF;
			entry.Address.Type = (MemoryType)((callerKey >> 24) & 0xFF);
			entry.CallCount = count;
			output.CallerCount++;
		}
	}

	output.CalleeCount = 0;
	auto calleeIt = _calleeMap.find(key);
	if(calleeIt != _calleeMap.end()) {
		for(auto& [calleeKey, count] : calleeIt->second) {
			if(output.CalleeCount >= 64) break;
			CallerCalleeEntry& entry = output.Callees[output.CalleeCount];
			entry.Address.Address = calleeKey & 0x00FFFFFF;
			entry.Address.Type = (MemoryType)((calleeKey >> 24) & 0xFF);
			entry.CallCount = count;
			output.CalleeCount++;
		}
	}
}

void CallerCalleeTracker::GetAllEdges(std::vector<CallerCalleeEdge>& out, size_t maxEdges) const
{
	out.clear();
	out.reserve(std::min(_calleeMap.size() * 8, maxEdges));
	for(auto& [callerKey, callees] : _calleeMap) {
		for(auto& [calleeKey, count] : callees) {
			if(out.size() >= maxEdges) {
				return;
			}
			CallerCalleeEdge e;
			e.CallerAddress = callerKey & 0x00FFFFFF;
			e.CallerType = (MemoryType)((callerKey >> 24) & 0xFF);
			e.CalleeAddress = calleeKey & 0x00FFFFFF;
			e.CalleeType = (MemoryType)((calleeKey >> 24) & 0xFF);
			e.CallCount = count;
			out.push_back(e);
		}
	}
}

void CallerCalleeTracker::Reset()
{
	_callerMap.clear();
	_calleeMap.clear();
}
