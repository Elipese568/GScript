#include"Clipboard.h"
#include<iostream>
#include "pch.h"

extern "C" __declspec(dllexport) char* GetCurrentClipboardContent()
{
	HGLOBAL clipboardData;
	if (!OpenClipboard(0)) return nullptr;;
	if ((clipboardData = GetClipboardData(1)) == 0) if ((clipboardData = GetClipboardData(13)) == 0) return nullptr;

	void* memoryPointer;
	if ((memoryPointer = GlobalLock(clipboardData)) == 0) return nullptr;
	
	LPSTR result = (LPSTR)memoryPointer;

	GlobalUnlock(clipboardData);

	CloseClipboard();

	return result;
}