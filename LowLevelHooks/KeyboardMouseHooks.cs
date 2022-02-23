using SharedLibrary;
using SharedLibrary.Interfaces;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using static LowLevelHooks.Constants;
using static LowLevelHooks.Enums;
using static LowLevelHooks.Imports;
using static LowLevelHooks.Structs;

namespace LowLevelHooks
{
    public unsafe class KeyboardMouseHooks
    {
        // RUN AS ADMIN, AFTER P0K3R CLIENT
        private static Thread _hookThread = null;

        private static nint _activeHookMouse = 0;
        private static readonly object _activeHookMouseLockObject = new();

        private static nint _activeHookKeyboard = 0;
        private static readonly object _activeHookKeybdLockObject = new();

        public static nint ActiveHookMouseHandle
        {
            get
            {
                lock (_activeHookMouseLockObject)
                {
                    return _activeHookMouse;
                }
            }
            set
            {
                lock (_activeHookMouseLockObject)
                {
                    _activeHookMouse = value;
                }
            }
        }
        public static nint ActiveHookKeybdHandle
        {
            get
            {
                lock (_activeHookKeybdLockObject)
                {
                    return _activeHookKeyboard;
                }
            }
            set
            {
                lock (_activeHookKeybdLockObject)
                {
                    _activeHookKeyboard = value;
                }
            }
        }

        public static unsafe void Initialize(ILogWindow logWindow)
        {
            if (!Init()) throw new NullReferenceException($"Failed initializing function pointers");
            else InitializeHooks(logWindow);

            //         Console.WriteLine("Pressing enter will close the program ...");
            //Console.ReadLine();
        }

        private static void InitializeHooks(ILogWindow logWindow)
        {
            var process = Process.GetCurrentProcess();
            var module = process.MainModule;
            fixed (char* lpModName = module.ModuleName)
            {
                nint hModule = GetModuleHandle(lpModName);
                Debug.Assert(hModule > 0);

                _hookThread = new Thread(() =>
                {
                    lock (_activeHookMouseLockObject)
                    {
                        if (_activeHookMouse > 0)
                        {
                            UnhookWindowsHookEx(_activeHookMouse);
                            _activeHookMouse = 0;
                        }

                        _activeHookMouse = SetWindowsHookExMouse(HookType.WH_MOUSE_LL, &LowLevelMouseCallback, hModule, 0);
                        Debug.Assert(_activeHookMouse > 0, "SetWindowsHookExMouse returned 0");
                        logWindow.Add(0.0f, LoggerStateConst.Hooks, "Mouse hook initialized succcessfully!", LoggerStateConst.DEBUG);
                        //Console.WriteLine("Mouse hook initialized succcessfully!");
                    }
                    lock (_activeHookKeybdLockObject)
                    {
                        if (_activeHookKeyboard > 0)
                        {
                            UnhookWindowsHookEx(_activeHookKeyboard);
                            _activeHookKeyboard = 0;
                        }

                        _activeHookKeyboard = SetWindowsHookExKeyboard(HookType.WH_KEYBOARD_LL, &LowLevelKeyboardCallback, hModule, 0);
                        Debug.Assert(_activeHookKeyboard > 0, "SetWindowsHookExKeyboard returned 0");
                        logWindow.Add(0.0f, LoggerStateConst.Hooks, "Keyboard hook initialized succcessfully!", LoggerStateConst.DEBUG);
                        //Console.WriteLine("Keyboard hook initialized succcessfully!");
                    }

                    //Console.WriteLine("Starting MessagePump");
                    logWindow.Add(0.0f, LoggerStateConst.Hooks, "Starting MessagePump", LoggerStateConst.DEBUG);
                    MSG msg = default;

                    while (GetMessage(&msg, IntPtr.Zero, 0, 0) != -1)
                    {
                        TranslateMessage(&msg);
                        DispatchMessage(&msg);

                        if (ActiveHookMouseHandle == 0 && ActiveHookKeybdHandle == 0)
                            break;
                    }

                    //Console.WriteLine("GetMessage returned -1 or cancellation was requested, unhooking our windows hook");
                    logWindow.Add(0.0f, LoggerStateConst.Hooks,
                        "GetMessage returned -1 or cancellation was requested, unhooking our windows hook", LoggerStateConst.ERROR);

                    lock (_activeHookMouseLockObject)
                    {
                        if (_activeHookMouse > 0)
                            if (!UnhookWindowsHookEx(_activeHookMouse))
                            {
                                //Console.WriteLine($"UnhookWindowsHookEx with parameter '{_activeHookMouse}' returned false");
                                logWindow.Add(0.0f, LoggerStateConst.Hooks,
                                    $"UnhookWindowsHookEx with parameter '{_activeHookMouse}' returned false", LoggerStateConst.ERROR);
                            }

                        _activeHookMouse = 0;
                    }
                    lock (_activeHookKeybdLockObject)
                    {
                        if (_activeHookKeyboard > 0)
                            if (!UnhookWindowsHookEx(_activeHookKeyboard))
                            {
                                //Console.WriteLine($"UnhookWindowsHookEx with parameter '{_activeHookKeyboard}' returned false");
                                logWindow.Add(0.0f, LoggerStateConst.Hooks,
                                    $"UnhookWindowsHookEx with parameter '{_activeHookKeyboard}' returned false", LoggerStateConst.ERROR);
                            }

                        _activeHookKeyboard = 0;
                    }
                });
                _hookThread.Start();
            }

            Debug.Assert(_hookThread != null
                         && _hookThread.ThreadState == System.Threading.ThreadState.Running);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private unsafe static nint LowLevelMouseCallback(int code, IntPtr wParam, MSLLHOOKSTRUCT* lParam)
        {
            if (code < 0) return CallNextHookEx(IntPtr.Zero, code, wParam, *(IntPtr*)lParam);

            if ((lParam->flags & LLMHF_INJECTED) != 0)
                lParam->flags &= ~LLMHF_INJECTED;

            if ((lParam->flags & LOWER_IL_INJECTED) != 0)
                lParam->flags &= ~LOWER_IL_INJECTED;

            return CallNextHookEx(IntPtr.Zero, code, wParam, *(IntPtr*)lParam);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private unsafe static nint LowLevelKeyboardCallback(int code, IntPtr wParam, KBDLLHOOKSTRUCT* lParam)
        {
            if (code < 0) return CallNextHookEx(IntPtr.Zero, code, wParam, *(IntPtr*)lParam);

            int keybd_event = (int)wParam;

            if (keybd_event == WM_KEYDOWN
                || keybd_event == WM_KEYUP)
            {
                if ((lParam->flags & LLKHF_INJECTED) != 0)
                    lParam->flags &= ~LLKHF_INJECTED;

                if ((lParam->flags & LOWER_IL_INJECTED) != 0)
                    lParam->flags &= ~LOWER_IL_INJECTED;
            }

            return CallNextHookEx(IntPtr.Zero, code, wParam, *(IntPtr*)lParam);
        }
    }
}
