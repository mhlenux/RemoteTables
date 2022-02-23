
using LogWindowUI;

using LowLevelLibrary;

using SharedLibrary;
using SharedLibrary.Models;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HostProgram
{
    public static class Server
    {
        // Network
        private static TcpListener listener = null;
        private static TcpClient client = null;
        private static NetworkStream stream = null;

        // Opening/opened  service\s
        private static readonly List<WindowModel> openServices = new List<WindowModel>();
        private static WindowModel openingService = new WindowModel();

        // Switches
        private static bool running = true;
        private static bool closing = false; // Only set from outside onProcessExit
        private static bool clientDisconnecting = false;

        private static CancellationTokenSource serverCancellationToken = new CancellationTokenSource();

        /// <summary>
        /// Start listening for new connections
        /// </summary>
        public static void StartListening()
        {
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, "StartListening ::: START", LoggerStateConst.DEBUG);
            ResetServerState(out CancellationToken cancellationToken);

            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, "Listening ::: START..", LoggerStateConst.INFORMATIVE);
            var listenTask = Task.Run(() => Listening(cancellationToken), cancellationToken);
            listenTask.Wait();
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, "Listening ::: FINISHED..", LoggerStateConst.INFORMATIVE);

            if (!closing)
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, "StartListening ::: RESTARTING..", LoggerStateConst.INFORMATIVE);
                StopListening(false);
                Thread.Sleep(500); // give some time for client to disconnect
                StartListening();
            }
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, "StartListening ::: FINISHED", LoggerStateConst.DEBUG);
        }

        private static void ResetServerState(out CancellationToken cancellationToken)
        {
            openingService = new WindowModel();
            openServices.Clear();
            running = true;
            closing = false;
            clientDisconnecting = false;
            serverCancellationToken = new CancellationTokenSource();

            // out
            cancellationToken = serverCancellationToken.Token;
        }

        private static void Listening(CancellationToken cancellationToken)
        {
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"Listening :: START", LoggerStateConst.DEBUG);

            InitListener();

            try
            {
                PayloadConverter payloadConverter = new PayloadConverter(LogWindowApp.Instance);
                InitWaitForClient();
              
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"Client connected from ::: {client.Client.RemoteEndPoint}", LoggerStateConst.INFORMATIVE);

                while (running && client != null && client.Connected)
                {
                    Thread.Sleep(250);
                    cancellationToken.ThrowIfCancellationRequested();
                    stream = client.GetStream();

                    if (IsLocked())
                    {
                        // failed to open in time
                        if (IsLockTimeOver())
                        {
                            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"Failed to open {openingService.Port} on time. RESTART", LoggerStateConst.ERROR);
                            running = false;
                            break;
                        }
                        continue;
                    }

                    /**
                     *  SEND TO CLIENT
                     */

                    // Get next window to open
                    var uiApp = NextWindowToOpen();
                    if (uiApp.WindowHandles != null)
                    {
                        // already open or opening
                        var foundOpened = openServices.Where(o => o.Handle == (int)uiApp.HWnd).FirstOrDefault();
                        if (foundOpened != null || openingService.Handle == (int)uiApp.HWnd) continue;

                        var freePort = NextAvailablePort();

                        // Lock server for this opening
                        LockServer(uiApp, freePort);

                        LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"Creating service for {freePort} :: {uiApp.Caption}", LoggerStateConst.DEBUG);
                        // Service start listening for it.. 
                        Task.Run(() => new Service(uiApp, freePort, cancellationToken), cancellationToken);

                        // reset loop
                        continue;
                    }

                    /**
                     *  RECEIVE FROM CLIENT
                     */

                    var payloadIn = payloadConverter.Deserialize(stream);
                    if (payloadIn != null)
                    {
                        //errorCounter = 0;

                        if (payloadIn.Command == CommandConst.Disconnect)
                        {
                            // client disconnected
                            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"Client disconnected. RESTART", LoggerStateConst.INFORMATIVE);
                            clientDisconnecting = true;
                            running = false;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"Listening Failed ::: {ex.Message}", LoggerStateConst.ERROR);
            }
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"Listening :: FINISHED", LoggerStateConst.DEBUG);
        }

        private static bool IsLocked()
        {
            return openingService != null && openingService.Handle != 0;
        }

        private static void InitListener()
        {
            listener = new TcpListener(IPAddress.Any, int.Parse(Settings.Instance.Port));
            listener.Server.ReceiveTimeout = 2000;
            listener.Server.SendTimeout = 2000;
            listener.Start();
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"Listening on ::: {Settings.Instance.Port}", LoggerStateConst.INFORMATIVE);
        }

        private static void InitWaitForClient()
        {
            client = listener.AcceptTcpClient();
            client.ReceiveTimeout = 2000;
            client.SendTimeout = 2000;
        }

        private static void LockServer(UIApp uiApp, int freePort)
        {
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"LOCKED :: {freePort} :: {uiApp.Caption}", LoggerStateConst.DEBUG);

            openingService = new WindowModel
            {
                Handle = (int)uiApp.HWnd,
                Port = freePort,
                Title = uiApp.Caption
            };
            lockTimeCounter = 0;
        }

        private static void ReleaseLock()
        {
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"LOCK RELEASED :: {openingService.Port}", LoggerStateConst.DEBUG);
            openingService = new WindowModel();
            lockTimeCounter = 0;
        }

        private static UIApp NextWindowToOpen()
        {
            foreach (var title in Settings.Instance.WindowTitles)
            {
                // get all by title
                List<int> allOpenHwnds = new List<int>();
                openServices.ForEach(s => allOpenHwnds.Add(s.Handle));

                var appsByCaption = UIAppsInProcesses(title).Where(app => allOpenHwnds.Contains((int)app.HWnd) == false).FirstOrDefault();
                if (appsByCaption.HWnd != IntPtr.Zero)
                {
                    return appsByCaption;
                }
            }
            return new UIApp();
        }

        private static IEnumerable<UIApp> UIAppsInProcesses(string title)
        {
            var allApps = new UIApps(Process.GetProcesses());
            return allApps.Where(app => app.Caption.Contains(title));
        }

        private static int NextAvailablePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        // Lock server when opening new windowses
        private static int lockTimeCounter = 0;
        private static readonly int maxLockTime = 20; // max 5 sec lock
        private static bool IsLockTimeOver()
        {
            if (lockTimeCounter == maxLockTime)
            {
                lockTimeCounter = 0;
                return true;
            }

            lockTimeCounter++;
            return false;
        }

        // Only called from Server.StartListening() or Program.OnProcessExit()
        private static bool _stopping = false;
        public static void StopListening(bool closingApp)
        {
            closing = closingApp;
            if (_stopping) return;
            _stopping = true;
            running = false;

            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, "StopListening ::: START", LoggerStateConst.DEBUG);

            if (!clientDisconnecting && client != null && client.Connected)
            {
                PayloadConverter payloadConverter = new PayloadConverter(LogWindowApp.Instance);
                var payload = new Payload() { Command = CommandConst.Disconnect };
                payloadConverter.Serialize(payload, stream);
            };
            if (client != null) client.Close();

            serverCancellationToken.Cancel();

            // wait for all services to exit
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, "StopListening ::: Closing services START", LoggerStateConst.DEBUG);
            while (openServices.Count != 0)
            {
                Thread.Sleep(100);
            }
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, "StopListening ::: Closing services FINISHED", LoggerStateConst.DEBUG);

            if (listener != null)
            {
                listener.Server.Close();
                listener.Stop();
            }

            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, "StopListening ::: FINISHED", LoggerStateConst.DEBUG);
            _stopping = false;
        }

        /**
         *  Events
         */

        internal static void OnClientConnected(int serviceHandle)
        {
            // Add to opened services
            openServices.Add(openingService);
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"OnClientConnected ::: {openingService.Port} :: [{openServices.Count} now opened]", LoggerStateConst.INFORMATIVE);

            // Release lock
            ReleaseLock();
        }

        internal static void OnServiceOpened(int serviceHandle)
        {
            if (serviceHandle != openingService.Handle) return;

            // Inform client about it
            var payload = new Payload()
            {
                Command = CommandConst.ClientConnecting,
                Port = openingService.Port,
                WindowHandle = openingService.Handle,
                WindowTitle = openingService.Title
            };
            PayloadConverter payloadConverter = new PayloadConverter(LogWindowApp.Instance);
            payloadConverter.Serialize(payload, stream);

            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"OnServiceOpened ::: Informed client about {openingService.Port}", LoggerStateConst.DEBUG);

            // zero one more time, to let client to connect
            lockTimeCounter = 0;
        }

        internal static void OnServiceClosing(int windowHandle)
        {
            var serviceToClose = openServices.Where(s => s.Handle == windowHandle).FirstOrDefault();
            if (serviceToClose != null)
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"OnServiceClosing event :: {serviceToClose.Port}:: [{openServices.Count - 1} still opened]", LoggerStateConst.INFORMATIVE);
                openServices.Remove(serviceToClose);
            }
            else
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Server, $"OnServiceClosing event :: {openingService.Port} was never open.", LoggerStateConst.ERROR);
                ReleaseLock();
            }
        }
    }
}
