using LogWindowUI;

using Microsoft.Extensions.Configuration;

using SharedLibrary;

using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Windows;

namespace ClientProgram
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
                ClientApp.RunApp();
            }
            catch (Exception ex)
            {
                // TODO LOGGING
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.System, $"Program ::: CRASHED :: {ex}", LoggerStateConst.ERROR);
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.System, ex.Message, LoggerStateConst.ERROR);
                ClientApp.StopApp(false);
            }

            OnProcessExit(null, null);
        }

        private static void InitializeMain()
        {
            InitLoggerUI();
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.System, $"Program ::: Initializing main..", LoggerStateConst.INFORMATIVE);

            // Set settings
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            Settings.IP = config["IP"];
            Settings.Port = int.Parse(config["Port"]);
        }

        private static void InitLoggerUI()
        {
            // intialization step
            LogWindowApp.Initialize(800, 800, 600, 400, OnProcessExit);

            // Configure Levels
            LogWindowApp.Instance.ConfigureLevels(new List<Tuple<string, string>>
            {
                Tuple.Create(LoggerStateConst.DEBUG,   "#000000"),
                Tuple.Create(LoggerStateConst.INFORMATIVE, "#B8860B"),
                Tuple.Create(LoggerStateConst.ERROR,   "#FF0000")
            });
            // Configure Systems
            var logSystems = new List<string>
            {
                LoggerStateConst.System,
                LoggerStateConst.App,
                LoggerStateConst.Client,
                LoggerStateConst.Window,
                LoggerStateConst.Command
            };
            LogWindowApp.Instance.ConfigureSytems(logSystems);
            Thread.Sleep(250);
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool _closing = false;
        /// <summary>
        /// Exit event.. Called by MainWindow on LoggerUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnProcessExit(object sender, EventArgs e)
        {
            if (_closing) return; _closing = true;

            ClientApp.StopApp(true);
            Environment.Exit(0);
        }
    }
}
