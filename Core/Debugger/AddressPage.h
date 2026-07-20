#pragma once
#include "Debugger/DebugTypes.h"
#include "Debugger/AddressInfo.h"

class Debugger;

// Thin C#-facing helpers kept out of Debugger.h so the broadly-included header
// stays stable. They resolve the active IConsole at runtime via Debugger::GetConsole().

// Page/bank size (bytes) for a memory type, read directly from mapper/PPu state
// so the UI no longer needs to serialize the whole console/Ppu state.
int32_t GetPageSize(Debugger* dbg, MemoryType memType);

// Relative bank/page index of an absolute address (PCE MPR window, etc.).
// Routes to IConsole::GetAbsoluteAddressPage so the page/bank is derived on
// the C++ side without serializing the whole console/Ppu state.
int32_t GetAbsoluteAddressPage(Debugger* dbg, AddressInfo absAddr, CpuType cpuType);
