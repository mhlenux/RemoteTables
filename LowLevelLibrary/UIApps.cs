using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LowLevelLibrary
{
    /// <summary>
    /// process structure
    /// </summary>
    public struct UIApp
    {
        static bool EnumThreadCallback(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gch = GCHandle.FromIntPtr(lParam);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(hWnd);
            return true;
        }

        readonly Process _proc;
        readonly IntPtr _RealHWnd;
        public string Description
        {
            get
            {
                return string.Format("{0}:{1}", _proc.ProcessName, Caption);
            }
        }

        public string WindowClass
        {
            get
            {
                System.Text.StringBuilder classNameBuilder = new System.Text.StringBuilder(256);
                User32.GetClassName(this.HWnd, classNameBuilder, classNameBuilder.Capacity);
                return classNameBuilder.ToString();
            }

        }
        public IntPtr HWnd
        {
            get { return _RealHWnd; }
        }


        public string Caption
        {
            get
            {
                // Allocate correct string length first
                int length = User32.GetWindowTextLength(this.HWnd);
                StringBuilder sb = new StringBuilder(length + 1);
                User32.GetWindowText(this.HWnd, sb, sb.Capacity);
                return sb.ToString();
            }

        }
        readonly List<IntPtr> _windowHandles;

        public List<IntPtr> WindowHandles
        {
            get { return _windowHandles; }
        }

        static User32.RECT _DesktopRect;

        static UIApp()
        {

            IntPtr DesktopHandle = User32.GetDesktopWindow();
            User32.GetWindowRect(DesktopHandle, out _DesktopRect);

        }
        internal static bool IsValidUIWnd(IntPtr hWnd)
        {
            bool res = false;
            if (hWnd == IntPtr.Zero || !User32.IsWindow(hWnd) || !User32.IsWindowVisible(hWnd))
                return false;
            User32.RECT CrtWndRect;
            if (!User32.GetWindowRect(hWnd, out CrtWndRect))
                return false;
            if (CrtWndRect.Height > 0 && CrtWndRect.Width > 0)
            {// a valid rectangle means the right window is the mainframe and it intersects the desktop
                User32.RECT visibleRect;//if the rectangle is outside the desktop, it's a dummy window
                if (User32.IntersectRect(out visibleRect, ref _DesktopRect, ref CrtWndRect)
                    && !User32.IsRectEmpty(ref visibleRect))
                    res = true;
            }
            return res;
        }

        internal UIApp(Process proc)
        {

            _proc = proc;
            _RealHWnd = IntPtr.Zero;
            _windowHandles = new List<IntPtr>();
            GCHandle listHandle = default(GCHandle);
            try
            {
                if (proc.MainWindowHandle == IntPtr.Zero)
                    throw new ApplicationException("Can't add a process with no MainFrame");

                User32.RECT MaxRect = default(User32.RECT);//init with 0
                if (IsValidUIWnd(proc.MainWindowHandle))
                {
                    _RealHWnd = proc.MainWindowHandle;
                    return;
                }
                // the mainFrame is size == 0, so we look for the 'real' window
                listHandle = GCHandle.Alloc(_windowHandles);
                foreach (ProcessThread pt in proc.Threads)
                {
                    User32.EnumThreadWindows((uint)pt.Id, new User32.EnumThreadDelegate(EnumThreadCallback), GCHandle.ToIntPtr(listHandle));
                }


                //get the biggest visible window in the current proc
                IntPtr MaxHWnd = IntPtr.Zero;
                foreach (IntPtr hWnd in _windowHandles)
                {
                    User32.RECT CrtWndRect;
                    //do we have a valid rect for this window
                    if (User32.IsWindowVisible(hWnd) && User32.GetWindowRect(hWnd, out CrtWndRect) &&
                        CrtWndRect.Height > MaxRect.Height && CrtWndRect.Width > MaxRect.Width)
                    {   //if the rect is outside the desktop, it's a dummy window
                        User32.RECT visibleRect;
                        if (User32.IntersectRect(out visibleRect, ref _DesktopRect, ref CrtWndRect)
                            && !User32.IsRectEmpty(ref visibleRect))
                        {
                            MaxHWnd = hWnd;
                            MaxRect = CrtWndRect;
                        }
                    }
                }
                if (MaxHWnd != IntPtr.Zero && MaxRect.Width > 0 && MaxRect.Height > 0)
                {
                    _RealHWnd = MaxHWnd;
                }
                else
                    _RealHWnd = proc.MainWindowHandle;//just add something even if it's a bad window

            }//try ends
            finally
            {
                if (listHandle != default(GCHandle) && listHandle.IsAllocated)
                    listHandle.Free();
            }

        }


    }

    /// <summary>
    /// list with UI procs
    /// </summary>
    public class UIApps : List<UIApp>
    {
        public UIApps(Process[] procs)
        {
            //filter the processes thht don't have UI
            foreach (Process proc in procs)
            {
                if (proc.MainWindowHandle != IntPtr.Zero)
                {
                    UIApp entry = new UIApp(proc);
                    //addd an extra check for the UI window
                    if (UIApp.IsValidUIWnd(entry.HWnd))
                        this.Add(entry);
                }
            }
        }
    }
}
