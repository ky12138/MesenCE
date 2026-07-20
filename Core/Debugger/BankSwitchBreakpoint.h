#pragma once
#include <cstdint>

namespace BankSwitchBreakpoint
{
	//Stores NES PRG/CHR bank-switch breakpoint state outside of DebugConfig so that
	//iterating on this feature does not force a recompile of SettingTypes.h (~60 TUs).
	void SetConfig(
		bool prgEnabled,
		const int32_t* prgPages,
		const uint8_t* prgNegated,
		uint8_t prgCount,
		bool chrEnabled,
		const int32_t* chrPages,
		const uint8_t* chrNegated,
		uint8_t chrCount);

	bool IsPrgEnabled();
	bool IsChrEnabled();
	bool ShouldBreakPrg(int32_t page);
	bool ShouldBreakChr(int32_t page);
}
