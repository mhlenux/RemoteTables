using LogWindowUI;

using SharedLibrary;

using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Windows;

namespace HostProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            // validations
            if (IsAdministrator() == false)
            {
                MessageBox.Show("Needs to be runned as admin!", "No admin rights", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Environment.Exit(0);
            }

            InitializeMain();

            try
            {
                Server.StartListening();
            }
            catch (Exception ex)
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, "Server crashed!", LoggerStateConst.ERROR);
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.System, ex.Message, LoggerStateConst.ERROR);
                Server.StopListening(false);
            }

            OnProcessExit(null, null);
        }

        private static void InitializeMain()
        {
            InitLogger();
            Settings.InitializeSettings();

            if (string.IsNullOrEmpty(Settings.Instance.WindowTitles[0]))
            {
                var message = "InitializeMain :: WindowTitle to scan is missing from appSetting";
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, message, LoggerStateConst.ERROR);
                throw new Exception(message);
            }

            if (string.IsNullOrEmpty(Settings.Instance.Port)
                || string.IsNullOrEmpty(Settings.Instance.MinDelayReceive)
                || string.IsNullOrEmpty(Settings.Instance.DelaySend)
                || (Settings.Instance.WindowTitles.Length <= 0)
                || string.IsNullOrEmpty(Settings.Instance.ImageAmountSeed))
            {
                var message = "InitializeMain :: Parsing settings is fucked up.";
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, message, LoggerStateConst.ERROR);
                throw new Exception(message);
            }

            // Hooks to get rid of LLMHF_INJECTED or LLMHF_LOWER_IL_INJECTED flags.
            LowLevelHooks.KeyboardMouseHooks.Initialize(LogWindowApp.Instance);
        }

        private static void InitLogger()
        {
            // intialization step
            LogWindowApp.Initialize(400, 400, 600, 400, OnProcessExit);

            // Configure Levels
            LogWindowApp.Instance.ConfigureLevels(new List<Tuple<string, string>>
            {
                Tuple.Create(LoggerStateConst.DEBUG,   "#000000"),
                Tuple.Create(LoggerStateConst.INFORMATIVE, "#B8860B"),
                Tuple.Create(LoggerStateConst.ERROR,   "#FF0000")
            });
            // Configure Systems
            var logSystems = new List<string>();
            logSystems.Add(LoggerStateConst.System);
            logSystems.Add(LoggerStateConst.Hooks);
            logSystems.Add(LoggerStateConst.Server);
            logSystems.Add(LoggerStateConst.Service);
            logSystems.Add(LoggerStateConst.Command);
            logSystems.Add(LoggerStateConst.Screen);
            LogWindowApp.Instance.ConfigureSytems(logSystems);
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool _closing = false;
        /// <summary>
        /// Exit Event.. Called by MainWindow on LoggerUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnProcessExit(object sender, EventArgs e)
        {
            if (_closing) return; _closing = true;
            Server.StopListening(true);
            LowLevelHooks.KeyboardMouseHooks.Initialize(LogWindowApp.Instance);
            Environment.Exit(0);
        }
    }
}
