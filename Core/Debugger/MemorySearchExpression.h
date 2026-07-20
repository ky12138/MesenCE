#pragma once
#include "Debugger/ExpressionEvaluator.h"

class Debugger;

// Memory-search expression evaluation, exposed as free functions so that
// Debugger.h does not need to carry feature-specific state/methods.
int64_t EvaluateMemorySearchExpressionForAddress(Debugger* debugger, const string& expression, CpuType cpuType, uint32_t address, AddressCounters* counters, uint32_t counterCount, EvalResultType& resultType);
void EvaluateMemorySearchExpressionForRange(Debugger* debugger, const string& expression, CpuType cpuType, uint32_t startAddr, uint32_t endAddr, AddressCounters* counters, uint32_t counterCount, uint8_t* results);
void CleanupMemorySearchEvaluators(Debugger* debugger);
