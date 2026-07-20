#include "pch.h"
#include "Debugger/ReverseMemoryAccessTracker.h"
#include "Debugger/Debugger.h"
#include "Debugger/Breakpoint.h"
#include "Debugger/CallstackManager.h"
#include "Debugger/Profiler.h"
#include "Debugger/DebugUtilities.h"
#include "Shared/MemoryOperationType.h"
#include <algorithm>
#include <map>

ReverseMemoryAccessTracker::ReverseMemoryAccessTracker() {}
ReverseMemoryAccessTracker::~ReverseMemoryAccessTracker() {}

void ReverseMemoryAccessTracker::RecordAccess(AddressInfo& memAddr, AddressInfo& funcAddr, MemoryOperationType opType)
{
	if(memAddr.Address < 0 || funcAddr.Address < 0) {
		return;
	}

	// Map the operation type to the R/W/E flag used by the access records.
	uint8_t flag = 1; // bit0 = Read (default for read-type ops)
	switch(opType) {
		case MemoryOperationType::ExecOpCode:
		case MemoryOperationType::ExecOperand:
			flag = 4; // bit2 = Execute
			break;
		case MemoryOperationType::Write:
		case MemoryOperationType::DmaWrite:
		case MemoryOperationType::DummyWrite:
			flag = 2; // bit1 = Write
			break;
		default:
			flag = 1; // bit0 = Read
			break;
	}

	int64_t mkey = MakeMemKey(memAddr);
	int32_t fkey = MakeFuncKey(funcAddr);
	auto it = _accessMap.find(mkey);
	if(it == _accessMap.end()) {
		it = _accessMap.emplace(mkey, unordered_map<int32_t, AccessInfo>()).first;
	}

	auto it2 = it->second.find(fkey);
	if(it2 != it->second.end()) {
		// Already recorded for this address+function: just update its counters.
		it2->second.Flags |= flag;
		it2->second.AccessCount++;
	} else if(it->second.size() < MaxFunctionsPerAddress) {
		// New function accessing this address within the per-address cap.
		AccessInfo& info = it->second[fkey];
		info.Flags |= flag;
		info.AccessCount++;
	}
	// else: per-address cap reached — drop the access to keep memory bounded.
}

void ReverseMemoryAccessTracker::GetMemoryAccessFunctions(MemoryType memType, uint32_t start, uint32_t end, MemoryAccessFunctionRecord& output)
{
	output.Count = 0;

	// Aggregate per-function records across every recorded address that falls
	// inside the requested region. Flags are OR-ed (a function may read AND
	// write the same address) and counts are summed.
	unordered_map<int32_t, AccessInfo> agg;
	for(auto& [mkey, funcMap] : _accessMap) {
		MemoryType mt = (MemoryType)((mkey >> 40) & 0xFF);
		if(mt != memType) {
			continue;
		}
		uint32_t addr = (uint32_t)(mkey & 0xFFFFFFFF);
		if(addr < start || addr > end) {
			continue;
		}
		for(auto& [fkey, info] : funcMap) {
			AccessInfo& a = agg[fkey];
			a.Flags |= info.Flags;
			a.AccessCount += info.AccessCount;
		}
	}

	struct Tmp {
		int32_t FuncAddress;
		MemoryType FuncType;
		uint8_t Flags;
		uint32_t AccessCount;
	};

	vector<Tmp> list;
	list.reserve(agg.size());
	for(auto& [fkey, info] : agg) {
		MemoryType ft = (MemoryType)((fkey >> 24) & 0xFF);
		int32_t addr = fkey & 0x00FFFFFF;
		// Sign-extend the 24-bit address field.
		if(addr & 0x00800000) {
			addr |= (int32_t)0xFF000000;
		}
		list.push_back({ addr, ft, info.Flags, info.AccessCount });
	}

	sort(list.begin(), list.end(), [](const Tmp& a, const Tmp& b) {
		if(a.FuncType != b.FuncType) {
			return (uint8_t)a.FuncType < (uint8_t)b.FuncType;
		}
		return a.FuncAddress < b.FuncAddress;
	});

	const uint32_t MaxEntries = 1024;
	for(size_t i = 0; i < list.size() && output.Count < MaxEntries; i++) {
		const Tmp& t = list[i];
		output.Functions[output.Count++] = { t.FuncAddress, t.FuncType, t.Flags, t.AccessCount };
	}
}

void ReverseMemoryAccessTracker::Reset()
{
	_accessMap.clear();
}

void ReverseMemoryAccessTracker::GetAllRecords(std::vector<ReverseAccessDumpRecord>& out, size_t maxRecords) const
{
	out.clear();
	// Coalesce by memory-address signature: the *set* of (func, flags) that
	// touches an address. Consecutive addresses with an identical signature are
	// merged into one range, and each range emits one flat record per function
	// carrying the FULL range span. The C# side then groups these by range and
	// nests the function list, so a recorded region stays one compact JSON entry
	// regardless of how many functions touched it.
	// _accessMap is an unordered_map, so build a sorted view (by memType then
	// address) first; only then are consecutive addresses adjacent and can be
	// coalesced into contiguous ranges.
	std::map<int64_t, std::vector<std::pair<int32_t, uint8_t>>> sorted;
	for(auto& [mkey, funcMap] : _accessMap) {
		auto& sig = sorted[mkey];
		for(auto& [fkey, info] : funcMap) {
			sig.push_back({ fkey, info.Flags });
		}
		sort(sig.begin(), sig.end());
	}

	std::vector<std::pair<int32_t, uint8_t>> prevSig;
	MemoryType curMt = (MemoryType)0;
	int32_t runStart = 0;
	int32_t runEnd = 0;
	bool inRun = false;

	auto flush = [&]() {
		for(auto& [fkey, flags] : prevSig) {
			if(out.size() >= maxRecords) {
				return;
			}
			MemoryType ft = (MemoryType)((fkey >> 24) & 0xFF);
			int32_t faddr = fkey & 0x00FFFFFF;
			// Sign-extend the 24-bit address field.
			if(faddr & 0x00800000) {
				faddr |= (int32_t)0xFF000000;
			}
			out.push_back({ runStart, runEnd, curMt, faddr, ft, flags });
		}
	};

	for(auto& [mkey, sig] : sorted) {
		MemoryType mt = (MemoryType)((mkey >> 40) & 0xFF);
		uint32_t addr = (uint32_t)(mkey & 0xFFFFFFFF);
		if(!inRun || mt != curMt || sig != prevSig) {
			if(inRun) {
				flush();
			}
			curMt = mt;
			prevSig = sig;
			runStart = runEnd = (int32_t)addr;
			inRun = true;
		} else {
			runEnd = (int32_t)addr;
		}
	}
	if(inRun) {
		flush();
	}
}

void ReverseMemoryAccessTracker::LoadRecords(const std::vector<ReverseAccessDumpRecord>& in)
{
	for(auto& e : in) {
		if(e.FuncAddress < 0) {
			continue;
		}
		AddressInfo funcAddr { e.FuncAddress, e.FuncType };
		// Expand the coalesced range back into per-address entries so the
		// existing GetMemoryAccessFunctions query keeps working. AccessCount is
		// not persisted (rebuilt live), so it stays 0 here.
		for(int32_t addr = e.StartAddr; addr <= e.EndAddr; addr++) {
			AddressInfo memAddr { addr, e.MemType };
			if(memAddr.Address < 0 || funcAddr.Address < 0) {
				continue;
			}
			int64_t mkey = MakeMemKey(memAddr);
			int32_t fkey = MakeFuncKey(funcAddr);
			auto& funcMap = _accessMap[mkey];
			AccessInfo& info = funcMap[fkey];
			info.Flags |= e.Flags;
		}
	}
}

void RecordReverseMemoryAccess(Debugger* debugger, CpuType cpuType, Breakpoint& bp, MemoryOperationInfo& operation, AddressInfo& address)
{
	if(operation.Type == MemoryOperationType::Idle) {
		return;
	}

	CallstackManager* csm = debugger->GetCallstackManager(cpuType);
	if(!csm) {
		return;
	}

	Profiler* profiler = csm->GetProfiler();
	if(!profiler) {
		return;
	}

	ReverseMemoryAccessTracker* tracker = profiler->GetReverseMemoryAccessTracker();
	if(!tracker) {
		return;
	}

	// Only record when a function is actually on the stack.
	AddressInfo funcAddr = profiler->GetCurrentFunctionAddress();
	if(funcAddr.Address < 0) {
		return;
	}

	// Record in the breakpoint's own address space so the UI query (which uses
	// the breakpoint's MemoryType + Start/End) lines up exactly.
	AddressInfo memAddr;
	if(DebugUtilities::IsRelativeMemory(bp.GetMemoryType())) {
		memAddr = { (int32_t)operation.Address, bp.GetMemoryType() };
	} else {
		memAddr = { address.Address, address.Type };
	}

	tracker->RecordAccess(memAddr, funcAddr, operation.Type);
}
