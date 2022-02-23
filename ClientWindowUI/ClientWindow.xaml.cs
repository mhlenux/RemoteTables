using SharedLibrary;

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClientWindowUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window, IWindow
    {
        public int windowHandle;
        private readonly IClient client;

        public ClientWindow(IClient client)
        {
            InitializeComponent();
            DataContext = this;
            this.client = client;
            this.windowHandle = client.WindowHandle;
            this.Title = client.WindowTitle;

            // Reverse attachment
            client.attachedWindow = this;

            // Bind events
            this.KeyUp += OnKeyUpEvent;
            //theImage.MouseMove += OnMouseMoveEvent;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseRightButtonDown += OnMouseRightButtonDown;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseRightButtonUp += OnMouseRightButtonUp;
            this.MouseDoubleClick += OnMouseDoubleClick;

            //
            this.Activate();
        }

        public void Update()
        {
            if (client == null || client.WindowShot == null) return;

            // Update shot
            var windowShot = client.WindowShot;
            theImage.Source = ImageSourceForBitmap(windowShot);
            theImage.Width = windowShot.Width;
            theImage.Height = windowShot.Height;
            this.SizeToContent = SizeToContent.WidthAndHeight;

            // Update texts
            this.OpenHoldemInfo.Text = client.GetOHReader().GetOHInfo(client.WindowTitle);
            this.Title = client.WindowTitle;

            // Focus
            if (client.WindowIsFocused)
            {
                this.Focus();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            client.Close();
        }

        private double GetMouseX(MouseEventArgs e) => e.GetPosition(theImage).X;
        private double GetMouseY(MouseEventArgs e) => e.GetPosition(theImage).Y;
        private void OnMouseMoveEvent(object sender, MouseEventArgs e) => SendMouseEvent(sender, e, EventConst.Mouse_Move);
        private void OnMouseLeftButtonDown(object sender, MouseEventArgs e) => SendMouseEvent(sender, e, EventConst.MouseL_Down);
        private void OnMouseRightButtonDown(object sender, MouseEventArgs e) => SendMouseEvent(sender, e, EventConst.MouseR_Down);
        private void OnMouseLeftButtonUp(object sender, MouseEventArgs e) => SendMouseEvent(sender, e, EventConst.MouseL_Up);
        private void OnMouseRightButtonUp(object sender, MouseEventArgs e) => SendMouseEvent(sender, e, EventConst.MouseR_Up);
        private void OnMouseDoubleClick(object sender, MouseEventArgs e) => SendMouseEvent(sender, e, EventConst.Mouse_DoubleClick);
        private void SendMouseEvent(object sender, MouseEventArgs e, string mouseEvent)
        {
            try
            {
                var payload = new Payload
                {
                    Command = CommandConst.MouseEvent,
                    MouseX = GetMouseX(e),
                    MouseY = GetMouseY(e),
                    MouseEvent = mouseEvent
                };
                client.SendEvent(payload);
            }
            catch (Exception) { }
        }

        private void OnKeyUpEvent(object sender, KeyEventArgs e)
        {
            try
            {
                string key = e.Key.ToString();
                string keysToSend = "";

                if (key.Equals("Back"))
                    keysToSend += "{BS}";
                else if (key.Equals("Pause"))
                    keysToSend += "{BREAK}";
                else if (key.Equals("Capital"))
                    keysToSend += "{CAPSLOCK}";
                else if (key.Equals("Space"))
                    keysToSend += " ";
                else if (key.Equals("Home"))
                    keysToSend += "{HOME}";
                else if (key.Equals("Return"))
                    keysToSend += "{ENTER}";
                else if (key.Equals("End"))
                    keysToSend += "{END}";
                else if (key.Equals("Tab"))
                    keysToSend += "{TAB}";
                else if (key.Equals("Escape"))
                    keysToSend += "{ESC}";
                else if (key.Equals("Insert"))
                    keysToSend += "{INS}";
                else if (key.Equals("Up"))
                    keysToSend += "{UP}";
                else if (key.Equals("Down"))
                    keysToSend += "{DOWN}";
                else if (key.Equals("Left"))
                    keysToSend += "{LEFT}";
                else if (key.Equals("Right"))
                    keysToSend += "{RIGHT}";
                else if (key.Equals("PageUp"))
                    keysToSend += "{PGUP}";
                else if (key.Equals("Next"))
                    keysToSend += "{PGDN}";
                else if (key.Equals("Tab"))
                    keysToSend += "{TAB}";
                // Numbers and F buttons
                else if (key.Equals("D1"))
                    keysToSend += "1";
                else if (key.Equals("D2"))
                    keysToSend += "2";
                else if (key.Equals("D3"))
                    keysToSend += "3";
                else if (key.Equals("D4"))
                    keysToSend += "4";
                else if (key.Equals("D5"))
                    keysToSend += "5";
                else if (key.Equals("D6"))
                    keysToSend += "6";
                else if (key.Equals("D7"))
                    keysToSend += "7";
                else if (key.Equals("D8"))
                    keysToSend += "8";
                else if (key.Equals("D9"))
                    keysToSend += "9";
                else if (key.Equals("D0"))
                    keysToSend += "0";
                else if (key.Equals("F1"))
                    keysToSend += "{F1}";
                else if (key.Equals("F2"))
                    keysToSend += "{F2}";
                else if (key.Equals("F3"))
                    keysToSend += "{F3}";
                else if (key.Equals("F4"))
                    keysToSend += "{F4}";
                else if (key.Equals("F5"))
                    keysToSend += "{F5}";
                else if (key.Equals("F6"))
                    keysToSend += "{F6}";
                else if (key.Equals("F7"))
                    keysToSend += "{F7}";
                else if (key.Equals("F8"))
                    keysToSend += "{F8}";
                else if (key.Equals("F9"))
                    keysToSend += "{F9}";
                else if (key.Equals("F10"))
                    keysToSend += "{F10}";
                else if (key.Equals("F11"))
                    keysToSend += "{F11}";
                else if (key.Equals("F12"))
                    keysToSend += "{F12}";
                // Commas and others
                else if (key.Equals("OemPeriod"))
                    keysToSend += ".";
                else if (key.Equals("OemComma"))
                    keysToSend += ",";
                // propably standard letter
                else if (key.Length == 1)
                    keysToSend += key.ToLower();
                // Unknown
                else
                    return;

                var payload = new Payload { Command = CommandConst.KeyEvent, Key = keysToSend };
                client.SendEvent(payload);
            }
            catch (Exception)
            {
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteObject([In] IntPtr hObject);
        static ImageSource ImageSourceForBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                ImageSource newSource = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(handle);
                return newSource;
            }
            catch
            {
                DeleteObject(handle);
                return null;
            }
        }
    }
}
