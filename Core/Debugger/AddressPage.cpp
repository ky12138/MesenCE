#include "pch.h"
#include "Debugger/AddressPage.h"
#include "Debugger/Debugger.h"
#include "Core/Shared/Interfaces/IConsole.h"

int32_t GetPageSize(Debugger* dbg, MemoryType memType)
{
	return dbg->GetConsole()->GetPageSize(memType);
}

int32_t GetAbsoluteAddressPage(Debugger* dbg, AddressInfo absAddr, CpuType cpuType)
{
	return dbg->GetConsole()->GetAbsoluteAddressPage(absAddr, cpuType);
}
