#include "pch.h"
#include "Debugger/MemorySearchExpression.h"
#include "Debugger/Debugger.h"
#include "Debugger/ExpressionEvaluator.h"

// Per-Debugger, per-CpuType evaluator cache (keeps the RPN parse cache
// separate from the breakpoint/trace evaluator used by Debugger itself).
static unordered_map<Debugger*, unordered_map<int, unique_ptr<ExpressionEvaluator>>>& GetEvaluatorMap()
{
	static unordered_map<Debugger*, unordered_map<int, unique_ptr<ExpressionEvaluator>>> map;
	return map;
}

static ExpressionEvaluator* GetOrCreateEvaluator(Debugger* debugger, CpuType cpuType)
{
	auto& perCpu = GetEvaluatorMap()[debugger];
	unique_ptr<ExpressionEvaluator>& entry = perCpu[(int)cpuType];
	if(!entry) {
		entry.reset(new ExpressionEvaluator(debugger, debugger->GetCpuDebugger(cpuType), cpuType));
	}
	return entry.get();
}

void CleanupMemorySearchEvaluators(Debugger* debugger)
{
	GetEvaluatorMap().erase(debugger);
}

int64_t EvaluateMemorySearchExpressionForAddress(Debugger* debugger, const string& expression, CpuType cpuType, uint32_t address, AddressCounters* counters, uint32_t counterCount, EvalResultType& resultType)
{
	if(!debugger || !debugger->GetCpuDebugger(cpuType)) {
		resultType = EvalResultType::Invalid;
		return 0;
	}

	auto* evaluator = GetOrCreateEvaluator(debugger, cpuType);
	evaluator->SetAddressCounters(counters, counterCount);

	bool success = true;
	ExpressionData data = evaluator->GetRpnList(expression, success);
	if(!success) {
		resultType = EvalResultType::Invalid;
		return 0;
	}

	return evaluator->EvaluateForAddress(data, resultType, address);
}

void EvaluateMemorySearchExpressionForRange(Debugger* debugger, const string& expression, CpuType cpuType, uint32_t startAddr, uint32_t endAddr, AddressCounters* counters, uint32_t counterCount, uint8_t* results)
{
	if(!debugger || !debugger->GetCpuDebugger(cpuType)) {
		return;
	}

	auto* evaluator = GetOrCreateEvaluator(debugger, cpuType);
	evaluator->SetAddressCounters(counters, counterCount);

	bool success = true;
	ExpressionData data = evaluator->GetRpnList(expression, success);
	if(!success) {
		return;
	}

	EvalResultType resultType;
	for(uint32_t addr = startAddr; addr <= endAddr; addr++) {
		int64_t result = evaluator->EvaluateForAddress(data, resultType, addr);
		results[addr - startAddr] = (resultType == EvalResultType::Invalid || resultType == EvalResultType::DivideBy0) ? 1 : (result != 0 ? 1 : 0);
	}
}
