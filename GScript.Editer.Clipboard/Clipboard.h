#pragma once

#include"framework.h"

#define callCheck(api, callback) \
if(!api) \
	callback

#define callCheckAssign(var, api, callback) \
if((var = api) == NULL) \
	callback


#define _export extern "C" __declspec(dllexport)

_export char* GetCurrentClipboardContent();