#include "pch.h"
#include "Debugger/BankSwitchBreakpoint.h"

namespace
{
	constexpr uint8_t MaxPages = 64;

	bool _prgEnabled = false;
	bool _chrEnabled = false;
	int32_t _prgPages[MaxPages] = {};
	bool _prgNegated[MaxPages] = {};
	uint8_t _prgCount = 0;
	int32_t _chrPages[MaxPages] = {};
	bool _chrNegated[MaxPages] = {};
	uint8_t _chrCount = 0;

	bool PageMatches(const int32_t* pages, const bool* negated, uint8_t count, int32_t page)
	{
		if(count == 0) {
			//No filter specified: break on any page switch
			return true;
		}
		bool hasPositive = false;
		bool positiveMatch = false;
		bool negativeMatch = false;
		for(uint8_t i = 0; i < count; i++) {
			if(negated[i]) {
				if(pages[i] == page) {
					negativeMatch = true;
				}
			} else {
				hasPositive = true;
				if(pages[i] == page) {
					positiveMatch = true;
				}
			}
		}
		//If any positive (included) entries exist, only those pages match.
		//Otherwise, all pages except the negated (excluded) ones match.
		return hasPositive ? positiveMatch : !negativeMatch;
	}
}

namespace BankSwitchBreakpoint
{
	void SetConfig(
		bool prgEnabled,
		const int32_t* prgPages,
		const uint8_t* prgNegated,
		uint8_t prgCount,
		bool chrEnabled,
		const int32_t* chrPages,
		const uint8_t* chrNegated,
		uint8_t chrCount)
	{
		_prgEnabled = prgEnabled;
		_chrEnabled = chrEnabled;

		_prgCount = prgCount > MaxPages ? MaxPages : prgCount;
		for(uint8_t i = 0; i < _prgCount; i++) {
			_prgPages[i] = prgPages[i];
			_prgNegated[i] = prgNegated[i] != 0;
		}

		_chrCount = chrCount > MaxPages ? MaxPages : chrCount;
		for(uint8_t i = 0; i < _chrCount; i++) {
			_chrPages[i] = chrPages[i];
			_chrNegated[i] = chrNegated[i] != 0;
		}
	}

	bool IsPrgEnabled()
	{
		return _prgEnabled;
	}

	bool IsChrEnabled()
	{
		return _chrEnabled;
	}

	bool ShouldBreakPrg(int32_t page)
	{
		if(!_prgEnabled) {
			return false;
		}
		return PageMatches(_prgPages, _prgNegated, _prgCount, page);
	}

	bool ShouldBreakChr(int32_t page)
	{
		if(!_chrEnabled) {
			return false;
		}
		return PageMatches(_chrPages, _chrNegated, _chrCount, page);
	}
}
