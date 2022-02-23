using ClientWindowUI;

using LogWindowUI;

using SharedLibrary;
using SharedLibrary.Interfaces;

using System;
using System.Drawing;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace ClientProgram
{
    public class Client : IClient
    {
        // Window properties
        public int WindowHandle { get; set; }
        public string WindowTitle { get; set; } = "";
        public bool WindowIsFocused { get; set; }
        public IWindow attachedWindow { get; set; }
        public Bitmap WindowShot { get; private set; }

        // Network
        private NetworkStream stream;
        private readonly TcpClient tcpClient;
        private readonly int port = 0;

        // private switches
        private bool running = true;
        private bool serverDisconnecting;

        // Events
        private readonly Action<int> OnClientClosing;
        private readonly Action<int> OnClientConnected;

        // Others instanses
        private readonly IOHReader openHoldemReader;
        readonly PayloadConverter payloadConverter = new PayloadConverter(LogWindowApp.Instance);

        public Client(Payload payload,
                        Action<int> onClientClosing,
                        Action<int> OnClientConnected,
                        CancellationToken cancellationToken)
        {
            this.OnClientClosing = onClientClosing;
            this.OnClientConnected = OnClientConnected;

            this.WindowHandle = payload.WindowHandle;
            this.WindowTitle = payload.WindowTitle;

            port = payload.Port;

            // Init connection
            if (tcpClient == null) tcpClient = new TcpClient(Settings.IP, port);
            tcpClient.ReceiveTimeout = 2000;
            tcpClient.SendTimeout = 2000;
            var connected = ConnectOrTryToConnect();
            int connectionAttempts = 0;
            while (connected == false)
            {
                if (connectionAttempts > 2) break;
                connected = ConnectOrTryToConnect();
                connectionAttempts++;
            }

            if (connected)
            {
                // Create Table window
                ClientWindowApp.CreateWindow(this);
                Thread.Sleep(250); // Wait for window to be created

                // Init oh reader
                openHoldemReader = new OHReader(WindowTitle);

                ReceiveShot(cancellationToken);
            }
            else
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"Connection failed, closing :: {Settings.IP}:{port} :: {WindowTitle}", LoggerStateConst.ERROR);
            }

            Close();
        }

        private bool ConnectOrTryToConnect()
        {
            bool connected = true;

            if (tcpClient != null && tcpClient.Connected == false)
            {
                tcpClient.Connect(Settings.IP, Settings.Port);
            }

            // Get stream
            if (tcpClient.Connected)
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"Connected to :: {Settings.IP}:{port} :: {WindowTitle}", LoggerStateConst.INFORMATIVE);
                stream = tcpClient.GetStream();
            }
            else
            {
                connected = false;
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"Retrying to connect to :: {Settings.IP}:{port}...", LoggerStateConst.INFORMATIVE);
                Thread.Sleep(1000);
            }

            return connected;
        }

        public IOHReader GetOHReader()
        {
            return openHoldemReader;
        }

        public bool SendEvent(Payload payload)
        {
            try
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"{nameof(SendEvent)} :: START :: CMD:[{payload.Command}] KEY:[{payload.Key}] M:[{payload.MouseEvent}] :: {port} :: {WindowTitle}", LoggerStateConst.DEBUG);
                payloadConverter.Serialize(payload, stream);
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"{nameof(SendEvent)} :: FINISHED :: {port} :: {WindowTitle}", LoggerStateConst.DEBUG);
                return true;
            }
            catch (Exception ex)
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"{nameof(SendEvent)} ::: ERROR (CLIENT_COMMUNICATION_ERROR) failed to send {payload.Command} :: {port} :: {WindowTitle} :: {ex}", LoggerStateConst.DEBUG);
                return false;
            }
        }

        private void ReceiveShot(CancellationToken cancellationToken)
        {
            var windowProcesser = new WindowProcesserClient();
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"{nameof(ReceiveShot)} ::: START :: {port} :: {WindowTitle}", LoggerStateConst.DEBUG);

            running = true;
            var firstRun = true;

            while (running)
            {
                try
                {
                    if (attachedWindow == null) Thread.Sleep(400); // Extra time to let the window spawn
                    Thread.Sleep(100);

                    cancellationToken.ThrowIfCancellationRequested();

                    // Early exit if our Window is Closed
                    bool isDisposed = IsOurWindowDisposed();
                    if (isDisposed)
                    {
                        LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"{nameof(ReceiveShot)} ::: Windows is disposed.. closing {port} :: {WindowTitle}", LoggerStateConst.DEBUG);
                        running = false;
                        continue;
                    }

                    // Get payload
                    var payload = payloadConverter.Deserialize(stream);
                    if (payload == null)
                    {
                        continue;
                    }

                    if (payload.Command == CommandConst.Disconnect)
                    {
                        LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"{nameof(ReceiveShot)} ::: Server disconnecting. Disconnect {port} :: {WindowTitle}", LoggerStateConst.DEBUG);
                        serverDisconnecting = true;
                        running = false;
                        continue;
                    }

                    // Update window
                    if (payload.WindowImages != null && payload.WindowWidth > 0 && payload.WindowHeight > 0)
                    {
                        WindowShot = windowProcesser.RenderImage(payload);
                        WindowTitle = payload.WindowTitle;
                        WindowIsFocused = payload.WindowIsFocused;

                        // Update
                        if (WindowShot != null) ClientWindowApp.Update(this);

                        if (WindowShot != null && firstRun)
                        {
                            // Inform we have connected successfully
                            OnClientConnected(this.WindowHandle);
                            firstRun = false;
                        }
                    }

                    // Cleanup
                    if (payload.WindowImages != null)
                    {
                        foreach (var image in payload.WindowImages)
                        {
                            if (image != null) image.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"{nameof(ReceiveShot)} ::: crashed {port} :: {WindowTitle} :: {ex.Message}", LoggerStateConst.DEBUG);
                    running = false;
                    continue;
                }
            }
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"{nameof(ReceiveShot)} ::: FINISHED :: {port} :: {WindowTitle}", LoggerStateConst.DEBUG);
        }

        private bool IsOurWindowDisposed()
        {
            var propertyInfo = typeof(Window).GetProperty("IsDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
            var isDisposed = (bool)propertyInfo.GetValue(attachedWindow as Window);
            return isDisposed;
        }

        private bool _closing = false;

        public void Close()
        {
            if (_closing) return;
            _closing = true;
            running = false;
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"Close :: START :: {port} :: {WindowTitle}", LoggerStateConst.DEBUG);

            Thread.Sleep(200); // Cool down stopping threads

            if (!serverDisconnecting)
            {
                var payload = new Payload() { Command = CommandConst.Disconnect };
                SendEvent(payload);
            }

            ClientWindowApp.CloseWindow(WindowHandle);

            if (stream != null) stream.Close();
            if (tcpClient != null) tcpClient.Close();

            // Callback
            OnClientClosing(this.WindowHandle);
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Client, $"Close :: FINISHED :: {port} :: {WindowTitle}", LoggerStateConst.DEBUG);
        }
    }
}
