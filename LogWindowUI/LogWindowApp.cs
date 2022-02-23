using SharedLibrary.Interfaces;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;

// Credits to https://coding-scars.com/log-window-0/

namespace LogWindowUI
{
    public sealed class LogWindowApp : ILogWindow
    {
        private static LogWindowApp m_instance = null;
        public static LogWindowApp Instance
        {
            get
            {
                Debug.Assert(m_instance != null, "LoggerUI not initialized");
                return m_instance;
            }
        }

        private static App m_application = null;
        public static App App => m_application;

        private LogWindowApp(Rect dimensions, Action<object, EventArgs> onProcessExit)
        {
            // application and window need their own thread, so we create it
            AutoResetEvent windowCreatedEvent = new AutoResetEvent(false);
            Thread t = new Thread(() =>
            {
                m_application = new App();

                LogWindow window = new LogWindow(onProcessExit)
                {
                    // set window dimensions
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = dimensions.Left,
                    Top = dimensions.Top,
                    Width = dimensions.Width,
                    Height = dimensions.Height
                };

                m_application.MainWindow = window;
                m_application.MainWindow.Show();

                // notify they are created before we block this thread
                windowCreatedEvent.Set();

                m_application.Run();
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            // wait until the application and window are created
            windowCreatedEvent.WaitOne();
        }

        public static void Initialize(int x, int y, int w, int h, Action<object, EventArgs> OnProcessExit)
        {
            Debug.Assert(m_instance == null, "LoggerUI already initialized");
            m_instance = new LogWindowApp(new Rect(x, y, w, h), OnProcessExit);
        }

        public static void Destroy()
        {
            if (m_instance == null) return;
            m_instance = null;
        }

        public void Add(float timestamp, string system, string message, string level)
        {
            Debug.Assert(m_application != null);

            // add it to the window via UI thread
            m_application.Dispatcher.BeginInvoke((Action)delegate
            {
                // window can be closed already
                if (m_application.MainWindow == null)
                {
                    return;
                }

                LogWriter.LogWrite($"{level.PadRight(11, ' ')} {system.PadRight(8, ' ')} {message}");
                (m_application.MainWindow as LogWindow).AddLogEntry(timestamp, system, message, level);
            });
        }

        public void ConfigureSytems(List<string> systems)
        {
            Debug.Assert(m_application != null);

            m_application.Dispatcher.BeginInvoke((Action)delegate
            {
                Debug.Assert(m_application.MainWindow != null);

                (m_application.MainWindow as LogWindow).ConfigureSystems(systems);
            });
        }

        public void ConfigureLevels(List<Tuple<string, string>> levels)
        {
            Debug.Assert(m_application != null);

            m_application.Dispatcher.BeginInvoke((Action)delegate
            {
                Debug.Assert(m_application.MainWindow != null);

                (m_application.MainWindow as LogWindow).ConfigureLevels(levels);
            });
        }
    }
}
