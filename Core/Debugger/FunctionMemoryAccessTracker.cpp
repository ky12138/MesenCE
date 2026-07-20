#include "pch.h"
#include "Debugger/FunctionMemoryAccessTracker.h"
#include "Debugger/Debugger.h"
#include "Debugger/CallstackManager.h"
#include "Debugger/Profiler.h"
#include <algorithm>

FunctionMemoryAccessTracker::FunctionMemoryAccessTracker() {}
FunctionMemoryAccessTracker::~FunctionMemoryAccessTracker() {}

void FunctionMemoryAccessTracker::SetTracked(AddressInfo& funcAddr, bool tracked)
{
	int32_t key = MakeFuncKey(funcAddr);
	if(tracked) {
		_accessMap[key]; // create an empty entry so RecordAccess knows to track this function
	} else {
		_accessMap.erase(key);
	}
}

bool FunctionMemoryAccessTracker::IsTracked(AddressInfo& funcAddr)
{
	return _accessMap.find(MakeFuncKey(funcAddr)) != _accessMap.end();
}

void FunctionMemoryAccessTracker::SetRecordMask(uint32_t mask)
{
	_recordMask = mask;
}

uint32_t FunctionMemoryAccessTracker::GetRecordMask() const
{
	return _recordMask;
}

void FunctionMemoryAccessTracker::RecordAccess(AddressInfo& funcAddr, AddressInfo& memAddr, MemoryOperationType opType)
{
	if(_accessMap.empty()) {
		return;
	}

	int32_t fkey = MakeFuncKey(funcAddr);
	auto it = _accessMap.find(fkey);
	if(it == _accessMap.end()) {
		return; // function not being tracked
	}

	if(memAddr.Address < 0) {
		return;
	}

	if(!(_recordMask & (1u << (uint8_t)opType))) {
		return; // this operation type is not being recorded (opt-in special accesses)
	}

	// Map the operation type to the R/W flag used by the access ranges.
	bool isWrite = (opType == MemoryOperationType::Write ||
		opType == MemoryOperationType::DmaWrite ||
		opType == MemoryOperationType::DummyWrite);
	uint8_t flag = isWrite ? 2 : 1;

	int64_t mkey = MakeMemKey(memAddr);
	auto it2 = it->second.find(mkey);
	if(it2 != it->second.end()) {
		// Already tracked for this function: just update its counters.
		AccessInfo& info = it2->second;
		info.Flags |= flag;
		if(isWrite) {
			info.WriteCount++;
		} else {
			info.ReadCount++;
		}
	} else if(it->second.size() < MaxTrackedAddressesPerFunction) {
		// New address within the per-function cap: record it.
		AccessInfo& info = it->second[mkey];
		info.Flags |= flag;
		if(isWrite) {
			info.WriteCount++;
		} else {
			info.ReadCount++;
		}
	}
	// else: per-function cap reached — drop the access to keep memory bounded.
}

void FunctionMemoryAccessTracker::Reset()
{
	for(auto& [key, inner] : _accessMap) {
		inner.clear();
	}
}

void FunctionMemoryAccessTracker::GetFunctionMemoryAccess(AddressInfo& funcAddr, FunctionMemoryAccessRecord& output)
{
	output.Count = 0;

	auto it = _accessMap.find(MakeFuncKey(funcAddr));
	if(it == _accessMap.end()) {
		return;
	}

	struct Tmp
	{
		uint32_t Address;
		MemoryType Type;
		uint8_t Flags;
		uint32_t ReadCount;
		uint32_t WriteCount;
		uint32_t AccessCount;
	};

	vector<Tmp> list;
	list.reserve(it->second.size());
	for(auto& [mkey, info] : it->second) {
		MemoryType mt = (MemoryType)((mkey >> 40) & 0xFF);
		uint32_t addr = (uint32_t)(mkey & 0xFFFFFFFF);
		list.push_back({ addr, mt, info.Flags, info.ReadCount, info.WriteCount, info.ReadCount + info.WriteCount });
	}

	sort(list.begin(), list.end(), [](const Tmp& a, const Tmp& b) {
		if(a.Type != b.Type) {
			return (uint8_t)a.Type < (uint8_t)b.Type;
		}
		if(a.Flags != b.Flags) {
			return a.Flags < b.Flags;
		}
		return a.Address < b.Address;
	});

	// Coalesce sorted accesses into runs. Within each (Type, Flags) group we form
	// a run of consecutive accesses that (a) are evenly spaced by a constant
	// stride in [1, MaxStride], (b) have "similar" access counts (Factor-based),
	// and (c) stay under MaxRunLength. This groups e.g. $1/$3/$5 (same R/W flag,
	// comparable counts, stride 2) into one range (Start=1, Length=3, Interval=2)
	// instead of three unrelated rows. Contiguous accesses (stride 1) merge exactly
	// as before. Per-address counts are intentionally NOT used to decide *whether*
	// addresses belong together (they differ even within a hotspot); only the
	// constant-stride + similarity test does, keeping genuine regions intact while
	// still letting the drill-down show individual counts.
	const auto Similar = [](uint32_t a, uint32_t b) {
		if(a == 0 && b == 0) {
			return true;
		}
		uint32_t lo = a < b ? a : b;
		uint32_t hi = a > b ? a : b;
		return hi <= AccessSimilarityFactor * lo;
	};

	const uint32_t MaxRanges = 1024;
	size_t i = 0;
	while(i < list.size() && output.Count < MaxRanges) {
		size_t runStart = i;
		uint32_t runStride = 0; // 0 => run length 1, stride not yet determined
		uint32_t readCount = list[i].ReadCount;
		uint32_t writeCount = list[i].WriteCount;

		size_t j = i;
		while(j + 1 < list.size() && output.Count < MaxRanges) {
			const Tmp& cur = list[j];
			const Tmp& nxt = list[j + 1];
			// Runs never cross a (Type, Flags) boundary — the sort guarantees the
			// next element is the same type/flags, but guard anyway.
			if(nxt.Type != cur.Type || nxt.Flags != cur.Flags) {
				break;
			}
			uint32_t stride = nxt.Address - cur.Address;
			uint32_t curRunLen = (uint32_t)(j - runStart + 1);
			bool strideOk = (stride >= 1 && stride <= MaxStride);
			bool similar = Similar(cur.AccessCount, nxt.AccessCount);
			bool strideMatches = (runStride == 0) ? true : (stride == runStride);
			if(strideOk && similar && strideMatches && curRunLen < MaxRunLength) {
				if(runStride == 0) {
					runStride = stride;
				}
				readCount += nxt.ReadCount;
				writeCount += nxt.WriteCount;
				j++;
			} else {
				break;
			}
		}

		const Tmp& first = list[runStart];
		uint32_t length = (uint32_t)(j - runStart + 1);
		uint32_t interval = (runStride == 0) ? 1 : runStride;
		if(runStride > 1 && length < 3) {
			// A spaced run (stride > 1) of only 1 or 2 addresses is not a
			// meaningful pattern — emit each access as its own single-address
			// row instead of a (start, length, interval) range. Contiguous runs
			// (stride == 1, runStride == 1) are unaffected and still merge.
			for(size_t k = runStart; k <= j && output.Count < MaxRanges; k++) {
				const Tmp& e = list[k];
				output.Ranges[output.Count++] = {
					e.Address,
					1,
					e.Type,
					e.Flags,
					e.ReadCount,
					e.WriteCount,
					e.ReadCount + e.WriteCount,
					1
				};
			}
		} else {
			output.Ranges[output.Count++] = {
				first.Address,
				length,
				first.Type,
				first.Flags,
				readCount,
				writeCount,
				readCount + writeCount,
				interval
			};
		}
		i = j + 1;
	}
}

void FunctionMemoryAccessTracker::GetFunctionMemoryAccessDetails(AddressInfo& funcAddr, MemoryType memType, uint32_t start, uint32_t end, uint32_t interval, FunctionMemoryAccessRecord& output)
{
	output.Count = 0;

	auto it = _accessMap.find(MakeFuncKey(funcAddr));
	if(it == _accessMap.end()) {
		return;
	}

	struct Tmp
	{
		uint32_t Address;
		uint8_t Flags;
		uint32_t ReadCount;
		uint32_t WriteCount;
	};

	// Collect every individually-tracked address that falls inside the merged
	// range the user is drilling into (same memory type + address window). When
	// the range is a stride run (interval > 1), keep only addresses that are
	// members of that run (start + k*interval) so the drill-down matches the
	// collapsed row exactly.
	vector<Tmp> list;
	for(auto& [mkey, info] : it->second) {
		MemoryType mt = (MemoryType)((mkey >> 40) & 0xFF);
		if(mt != memType) {
			continue;
		}
		uint32_t addr = (uint32_t)(mkey & 0xFFFFFFFF);
		if(addr < start || addr > end) {
			continue;
		}
		if(interval > 1) {
			uint64_t d = (uint64_t)addr - start;
			if(d % interval != 0) {
				continue;
			}
		}
		list.push_back({ addr, info.Flags, info.ReadCount, info.WriteCount });
	}

	sort(list.begin(), list.end(), [](const Tmp& a, const Tmp& b) {
		return a.Address < b.Address;
	});

	const uint32_t MaxRanges = 1024;
	for(size_t i = 0; i < list.size() && output.Count < MaxRanges; i++) {
		const Tmp& t = list[i];
		output.Ranges[output.Count++] = {
			t.Address,
			1,
			memType,
			t.Flags,
			t.ReadCount,
			t.WriteCount,
			t.ReadCount + t.WriteCount,
			1 // Interval: detail rows are single addresses (contiguous)
		};
	}
}

void RecordFunctionMemoryAccess(Debugger* debugger, CpuType cpuType, AddressInfo& memAddr, MemoryOperationType opType)
{
	if(opType == MemoryOperationType::Idle) {
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

	FunctionMemoryAccessTracker* tracker = profiler->GetFunctionMemoryAccessTracker();
	if(tracker && tracker->HasTrackedFunctions()) {
		AddressInfo funcAddr = profiler->GetCurrentFunctionAddress();
		if(funcAddr.Address >= 0) {
			tracker->RecordAccess(funcAddr, memAddr, opType);
		}
	}
}
