#include "pch.h"
#include "Debugger/Debugger.h"
#include "Debugger/DebugBreakHelper.h"
#include "Debugger/ITraceLogger.h"
#include "Debugger/DebugUtilities.h"

void ClearTraceAddressCache(Debugger* debugger)
{
	DebugBreakHelper helper(debugger);
	for(int i = 0; i <= (int)DebugUtilities::GetLastCpuType(); i++) {
		CpuType cpuType = (CpuType)i;
		ITraceLogger* logger = debugger->GetTraceLogger(cpuType);
		if(logger) {
			logger->ClearAddressCache();
		}
	}
}
