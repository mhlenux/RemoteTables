using ClientWindowUI;

using LogWindowUI;

using SharedLibrary;
using SharedLibrary.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ClientProgram
{
    public static class ClientApp
    {
        private static ClientWindowApp clientWindowApp;

        private static CancellationTokenSource clientAppCancellationToken = new CancellationTokenSource();

        // network
        private static TcpClient tcpClient;
        private static NetworkStream stream;

        // opening/opened states
        private static WindowModel openingWindow = new WindowModel();
        private static readonly List<WindowModel> openedClients = new List<WindowModel>();

        // Only set from outside onProcessExit
        private static bool closing = false;
        private static bool running = true;
        private static bool serverDisconnecting = false;

        public static void RunApp()
        {
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"RunApp :: START", LoggerStateConst.DEBUG);
            // Initialize
            if (clientWindowApp == null) clientWindowApp = new ClientWindowApp(LogWindowApp.App);

            openingWindow = new WindowModel();
            openedClients.Clear();
            clientAppCancellationToken = new CancellationTokenSource();
            running = true;
            closing = false;
            serverDisconnecting = false;

            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"Starting app.. ::: MainLoop START", LoggerStateConst.DEBUG);
            var cancellationToken = clientAppCancellationToken.Token;
            var mainTask = Task.Run(() => MainLoop(cancellationToken), cancellationToken);
            mainTask.Wait();
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"Starting app.. ::: MainLoop FINISHED", LoggerStateConst.DEBUG);

            if (!closing)
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"RunApp :: RESTARTING..", LoggerStateConst.INFORMATIVE);
                StopApp(false);
                Thread.Sleep(500); // give some time for server to disconnect
                RunApp();
            }
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"RunApp :: FINISHED", LoggerStateConst.DEBUG);
        }

        private static void MainLoop(CancellationToken cancellationToken)
        {
            try
            {
                running = true;
                var firstConnect = true;
                TryToConnectLoop();

                PayloadConverter payloadConverter = new PayloadConverter(LogWindowApp.Instance);

                // Our main loop for messaging with server
                while (running && tcpClient.Connected)
                {
                    stream = tcpClient.GetStream();
                    if (firstConnect) LogWindowApp.Instance.Add(0.0f,
                        LoggerStateConst.App, $"MainLoop :: FirstConnect :: Looping with server..", LoggerStateConst.DEBUG); firstConnect = false;
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(250);

                    // we are opening a window return
                    if (openingWindow.Handle > 0)
                    {
                        // failed to open in time
                        if (IsLockTimeOver())
                        {
                            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"Failed to open {openingWindow.Port} on time. RESTART", LoggerStateConst.ERROR);
                            running = false;
                            break;
                        }
                        continue;
                    }

                    /**
                     *  RECEIVE FROM SERVER
                     */

                    var payloadIn = payloadConverter.Deserialize(stream);
                    if (payloadIn != null)
                    {
                        if (payloadIn.Command == CommandConst.ClientConnecting)
                        {
                            LockApp(payloadIn);

                            // validations
                            var alreadyOpen = openedClients.Select(c => c.Handle == openingWindow.Handle).FirstOrDefault();
                            if (alreadyOpen) continue;
                            if (payloadIn.WindowHandle == 0) continue;
                            if (string.IsNullOrEmpty(payloadIn.WindowTitle)) continue;

                            // set port
                            var port = payloadIn.Port;
                            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App,
                                $"Opening client on port {port} :: {payloadIn.WindowTitle}", LoggerStateConst.DEBUG);

                            // Try to create client
                            Task.Run(() => new Client(payloadIn, OnClientClosing, OnClientConnected, cancellationToken), cancellationToken);

                            // opening a window return
                            lockTimeCounter = 0; // is used to timeout failed open

                            // reset loop
                            continue;
                        }
                        else if (payloadIn.Command == CommandConst.Disconnect)
                        {
                            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"Server disconnected. RESTART", LoggerStateConst.INFORMATIVE);
                            serverDisconnecting = true;
                            running = false;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"MainLoop failed ::: {ex.Message}", LoggerStateConst.ERROR);
            }
        }

        private static void LockApp(Payload payloadIn)
        {
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"LOCK ::: {payloadIn.Port}", LoggerStateConst.DEBUG);
            openingWindow = new WindowModel
            {
                Handle = payloadIn.WindowHandle,
                Port = payloadIn.Port
            };
        }

        private static void ReleaseLock()
        {
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"LOCK RELEASED ::: {openingWindow.Port}", LoggerStateConst.DEBUG);
            openingWindow = new WindowModel();
            lockTimeCounter = 0;
        }

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

        private static void OnClientClosing(int windowHandle)
        {
            // Check if client is opened
            var clientIsOpen = openedClients.Where(c => c.Handle == windowHandle).FirstOrDefault();
            if (clientIsOpen != null)
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"OnClientClosing event ::: Client is closed. :: {clientIsOpen.Port} ::[{openedClients.Count - 1} still opened]", LoggerStateConst.INFORMATIVE);
                openedClients.Remove(clientIsOpen);
            }
            else
            {
                // Client never connected
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"OnClientClosing event ::: Client is closed. :: {openingWindow.Port} was never connected", LoggerStateConst.ERROR);
                ReleaseLock();
            }
        }

        /// <summary>
        /// Client successfully received window from server
        /// </summary>
        private static void OnClientConnected(int windowHandle)
        {
            if (windowHandle != openingWindow.Handle) return;

            // add to open windowses
            openedClients.Add(openingWindow);
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"OnClientOpened event ::: {openingWindow.Port} opened. [{openedClients.Count} now opened]", LoggerStateConst.INFORMATIVE);

            ReleaseLock();
        }

        private static void TryToConnectLoop()
        {
            var firstConnect = true;
            var disconnected = true;

            while (disconnected)
            {
                if (firstConnect)
                {
                    // Avoid duplicate message
                    firstConnect = false;
                }
                else
                {
                    LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"Connecting to server..", LoggerStateConst.INFORMATIVE);
                }

                try
                {
                    if (tcpClient != null && tcpClient.Connected)
                    {
                        tcpClient.ReceiveTimeout = 2000;
                        tcpClient.SendTimeout = 2000;

                        LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App,
                            $"Connected to ::: {Settings.IP}:{Settings.Port}", LoggerStateConst.INFORMATIVE);
                        stream = tcpClient.GetStream();
                        disconnected = false;
                    }
                    else
                    {
                        if (stream != null) stream.Close();
                        if (tcpClient != null) tcpClient.Close();
                        tcpClient = new TcpClient(Settings.IP, Settings.Port);
                    }
                }
                catch
                {
                    LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App,
                            $"Retrying connection.. ::: {Settings.IP}:{Settings.Port} ...", LoggerStateConst.INFORMATIVE);
                }
                Thread.Sleep(500);
            }
        }

        // only called from startApp or onProcesExit
        private static bool _stopping = false;
        public static void StopApp(bool closingApp)
        {
            closing = closingApp;
            if (_stopping) return;
            _stopping = true;

            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"StopApp :: START", LoggerStateConst.DEBUG);
            // Disconnect server from us
            if (!serverDisconnecting)
            {
                var payload = new Payload() { Command = CommandConst.Disconnect };
                PayloadConverter payloadConverter = new PayloadConverter(LogWindowApp.Instance);
                payloadConverter.Serialize(payload, stream);
            }

            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"StopApp :: Closing clients :: START", LoggerStateConst.DEBUG);
            clientAppCancellationToken.Cancel();
            while (openedClients.Count != 0)
            {
                Thread.Sleep(100);
            }
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"StopApp :: Closing clients :: FINISHED", LoggerStateConst.DEBUG);

            if (stream != null) stream.Close();
            if (tcpClient != null) tcpClient.Close();
            tcpClient = null;
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.App, $"StopApp :: FINISH", LoggerStateConst.DEBUG);
            _stopping = false;
        }
    }
}
