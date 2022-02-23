using SharedLibrary;
using SharedLibrary.Models;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace ClientWindowUI
{
    public sealed class ClientWindowApp
    {
        private static Application m_application = null;
        public ClientWindowApp(Application application = null)
        {
            if (application != null && m_application == null)
            {
                m_application = application;
            }

            CreateApp();
        }
        private static void CreateApp()
        {
            if (m_application != null) return;

            // application needs their own thread, so we create it
            AutoResetEvent appCreatedEvent = new AutoResetEvent(false);
            Thread t = new Thread(() =>
            {
                if (m_application == null)
                    m_application = new App();

                appCreatedEvent.Set();

                m_application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                m_application.Run();
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            // wait until the application is created
            appCreatedEvent.WaitOne();
        }
        public static Application GetApplication() => m_application;

        public static void CreateWindow(IClient client) // IClient
        {
            CreateApp();

            // show window via application thread
            m_application.Dispatcher.BeginInvoke((Action)delegate
            {
                ClientWindow window = new ClientWindow(client);
                window.Show();
            });
        }

        public static void CloseWindow(int windowHandle)
        {
            if (m_application == null) return;

            m_application.Dispatcher.BeginInvoke((Action)delegate
            {
                var windowCollection = m_application.Windows;
                ClientWindow selectedWindow = null;

                foreach (Window window in windowCollection)
                {
                    var mainWindow = window as ClientWindow;
                    if (mainWindow != null && mainWindow.windowHandle == windowHandle)
                    {
                        selectedWindow = mainWindow;
                    }
                }

                if (selectedWindow != null)
                {
                    selectedWindow.Close();
                }
            });
        }

        public static void Update(IClient client)
        {
            if (m_application == null) return;
            if (client.attachedWindow == null) return;

            m_application.Dispatcher.BeginInvoke((Action)delegate
            {
                var windowCollection = m_application.Windows;
                ClientWindow selectedWindow = null;

                // Find our window by wHandle
                foreach (Window window in windowCollection)
                {
                    var mainWindow = window as ClientWindow;
                    if (mainWindow != null && mainWindow.windowHandle == client.WindowHandle)
                    {
                        selectedWindow = mainWindow;
                    }
                }

                if (selectedWindow == null)
                {
                    return;
                }

                selectedWindow.Update();
            });
        }

        public static void RemoveWindowIfClosed(List<WindowModel> clientOpenWindowses, Action<int> onClientClosing)
        {
            if (m_application == null) return;

            m_application.Dispatcher.BeginInvoke((Action)delegate
            {
                var windowCollection = m_application.Windows;

                // Look for any closed windowes
                foreach (var openedWindow in clientOpenWindowses)
                {
                    bool found = false;

                    foreach (Window window in windowCollection)
                    {
                        var mainWindow = window as ClientWindow;
                        if (mainWindow != null && mainWindow.windowHandle == openedWindow.Handle)
                        {
                            found = true;
                        }
                    }

                    // Window was not found.. inform to close.
                    if (!found)
                    {
                        onClientClosing(openedWindow.Handle);
                    };
                }
            });
        }
    }
}
