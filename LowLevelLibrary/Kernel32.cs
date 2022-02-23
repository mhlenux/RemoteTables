using System;
using System.Runtime.InteropServices;

namespace LowLevelLibrary
{
    public static class Kernel32
    {
        public static int PROCESS_VM_READ = 0x10;

        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, IntPtr bInheritHandle, IntPtr dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer,
                                                                        int dwSize, out IntPtr lpNumberOfBytesRead);
    }
}
