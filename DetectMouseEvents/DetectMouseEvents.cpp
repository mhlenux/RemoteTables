// DetectMouseEvents.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>

#include <Windows.h>

HHOOK hook;

// https://msdn.microsoft.com/en-us/library/windows/desktop/ms644970(v=vs.85).aspx
MSLLHOOKSTRUCT mouseStruct;

int idx = 0;
const char* mouseMessage[] = { "default", "left mousebutton down", "left mousebutton up", "mouse was moved", "mousewheel moved",
"horizontal mousewheel moved", "right mousebutton down", "right mousebutton up" };

// https://msdn.microsoft.com/en-us/library/windows/desktop/ms644986(v=vs.85).aspx
LRESULT CALLBACK LLMP(int nCode, WPARAM wParam, LPARAM lParam)
{
	if (nCode >= 0)
	{
		mouseStruct = *((MSLLHOOKSTRUCT*)lParam);

		// LLMHF_INJECTED FLAG
		if (mouseStruct.flags & 0x01)
		{
			switch (wParam)
			{
			case WM_LBUTTONDOWN: idx = 1; break;
			case WM_LBUTTONUP: idx = 2; break;
			case WM_MOUSEMOVE: idx = 3; break;
			case WM_MOUSEWHEEL: idx = 4; break;
			case WM_MOUSEHWHEEL: idx = 5; break;
			case WM_RBUTTONDOWN: idx = 6; break;
			case WM_RBUTTONUP: idx = 7;
			}

			MessageBox(NULL, mouseMessage[idx], "Detected!", MB_ICONINFORMATION);
		}
	}

	return CallNextHookEx(hook, nCode, wParam, lParam);
};

void InstallHook()
{
	// https://msdn.microsoft.com/en-us/library/windows/desktop/ms644990(v=vs.85).aspx
	if (!(hook = SetWindowsHookEx(WH_MOUSE_LL, LLMP, NULL, 0)))
	{
		MessageBox(NULL, "failed to install hook", "fail", MB_ICONERROR);
	}
}

int main()
{
	InstallHook();

	// Message loop to keep console alive/get messages.
	MSG gmsg;
	while (GetMessage(&gmsg, NULL, 0, 0))
	{

	}
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
