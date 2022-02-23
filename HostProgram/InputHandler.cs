using LowLevelLibrary;

using SharedLibrary;

using System;
using System.Windows.Forms;

namespace HostProgram
{
    class InputHandler
    {
        private IntPtr windowHandle;

        public InputHandler(int wHandle)
        {
            this.windowHandle = new IntPtr(wHandle);
        }

        public static void HandleKeyboardEvents(Payload payload)
        {
            SendKeys.SendWait(payload.Key);
        }

        public void HandleMouseEvents(Payload payload)
        {
            var currentPosition = User32.GetCursorPosition();
            var mEvent = payload.MouseEvent;

            User32.RECT appRect;
            bool res = User32.GetWindowRect(windowHandle, out appRect);
            var mouseX = (int)payload.MouseX + appRect.Left;
            var mouseY = (int)payload.MouseY + appRect.Top;

            User32.SetCursorPos((int)mouseX, (int)mouseY);

            if (mEvent == EventConst.MouseL_Up)
            {
                User32.mouse_event(User32.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                User32.mouse_event(User32.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            }
            else if (mEvent == EventConst.MouseR_Up)
            {
                User32.mouse_event(User32.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                User32.mouse_event(User32.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
            }

            // Restore cursor
            User32.SetCursorPos(currentPosition.X, currentPosition.Y);
        }
    }
}
