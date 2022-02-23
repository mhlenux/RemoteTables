using SharedLibrary.Interfaces;

using System;
using System.Drawing;

namespace SharedLibrary
{
    public interface IClient
    {
        public int WindowHandle { get; set; }

        public string WindowTitle { get; set; }

        public IWindow attachedWindow { get; set; }
        Bitmap WindowShot { get; }
        bool WindowIsFocused { get; set; }

        public IOHReader GetOHReader();

        public bool SendEvent(Payload payload);

        public void Close();
    }
}
