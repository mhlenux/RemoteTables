using System;
using System.Runtime.InteropServices;

namespace LowLevelLibrary
{
    /// <summary>
    /// Helper class containing Gdi32 API functions
    /// </summary>
    public static class GDI32
    {
        public const Int32 WM_SYSCOMMAND = 0x112;
        public const Int32 MF_SEPARATOR = 0x800;
        public const Int32 MF_STRING = 0x0;

        public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter

        [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
        public extern static IntPtr DeleteDC(IntPtr hDc);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        public extern static IntPtr DeleteObject(IntPtr hDc);

        [DllImport("gdi32.dll", EntryPoint = "BitBlt")]
        public extern static bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, int RasterOp);

        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
        public extern static IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC")]
        public extern static IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        public extern static IntPtr SelectObject(IntPtr hdc, IntPtr bmp);
    }
}