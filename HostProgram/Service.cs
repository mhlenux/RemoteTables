using LogWindowUI;

using LowLevelLibrary;

using SharedLibrary;

using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HostProgram
{
    public class Service
    {
        // Network
        private TcpListener listener;
        private TcpClient client;
        private NetworkStream stream;
        private readonly int port = 0;

        // Identification of this service
        public readonly int windowHandle;

        // Events
        private readonly Action<int> OnServiceClosing;
        private readonly Action<int> OnServiceOpened;
        private readonly Action<int> OnClientConnected;

        // Switches
        private bool running = true;
        private bool clientDisconnecting = false;

        // instances
        public UIApp uiApp;
        private readonly InputHandler inputHandler;
        readonly PayloadConverter payloadConverter = new PayloadConverter(LogWindowApp.Instance);

        public Service(UIApp uiApp, int freePort, CancellationToken cancellationToken)
        {
            this.port = freePort;
            this.uiApp = uiApp;

            // Events
            this.OnServiceOpened = Server.OnServiceOpened;
            this.OnServiceClosing = Server.OnServiceClosing;
            this.OnClientConnected = Server.OnClientConnected;

            this.windowHandle = (int)uiApp.HWnd;

            inputHandler = new InputHandler(windowHandle);

            try
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"Listening... {port} :: {uiApp.Caption}", LoggerStateConst.DEBUG);
                InitListener();

                // Inform server we opened this
                OnServiceOpened(this.windowHandle);

                // Wait for pending connection connection 
                client = listener.AcceptTcpClient();

                // Client connected

                // Hooks to get rid of LLMHF_INJECTED or LLMHF_LOWER_IL_INJECTED flags.
                // Reinitialize  when client opens
                LowLevelHooks.KeyboardMouseHooks.Initialize(LogWindowApp.Instance);

                Start(cancellationToken);
            }
            catch (Exception)
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service,
                    $"Failed to connect OR crashed. Closing {port} :: {uiApp.Caption}", LoggerStateConst.DEBUG);
            }
            finally
            {
                Stop();
            }
        }

        private void InitListener()
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Server.ReceiveTimeout = 2000;
            listener.Server.SendTimeout = 2000;
            listener.Start();
        }

        private void Start(CancellationToken cancellationToken)
        {
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"Starting for {port} :: {uiApp.Caption}", LoggerStateConst.INFORMATIVE);
            running = true;
            stream = client.GetStream();

            // Start threads
            var receiveTask = Task.Run(() => ReceiveEvents(cancellationToken), cancellationToken);
            Thread.Sleep(50); // some time to start
            var sendTask = Task.Run(() => SendShot(cancellationToken), cancellationToken);

            // Inform server client successfully connected
            OnClientConnected(this.windowHandle);

            receiveTask.Wait(cancellationToken);
            sendTask.Wait(cancellationToken);
        }

        private void SendShot(CancellationToken cancellationToken)
        {
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"{nameof(SendShot)} START :: {port} :: {uiApp.Caption}", LoggerStateConst.DEBUG);
            string stage = "Start"; // for debugging
            var delay = int.Parse(Settings.Instance.DelaySend);

            var errorCounter = 0;
            var errorTolerance = 3;
            WindowProcesserHost windowProcesser = InitWindowProcessor();

            while (running)
            {
                SleepForCooldown(delay);
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    stage = "Take a shot";
                    Bitmap[,] changedImages = windowProcesser.CaptureWindowAsImages();

                    stage = "Serialize Screen";
                    var payload = new Payload
                    {
                        Command = "STREAM",
                        WindowImages = changedImages,
                        WindowWidth = windowProcesser.WindowWidth,
                        WindowHeight = windowProcesser.WindowHeight,
                        WindowTitle = uiApp.Caption,
                        WindowIsFocused = IsOurWindowFocused(),
                        ImageAmountSeed = windowProcesser.ImageAmountSeed
                    };
                    payloadConverter.Serialize(payload, stream);

                    // Release changed images..
                    for (int col = 0; col < changedImages.GetLength(0); col++)
                    {
                        for (int row = 0; row < changedImages.GetLength(1); row++)
                        {
                            if (changedImages[row, col] != null)
                            {
                                changedImages[row, col].Dispose();
                            }
                        }
                    }

                    errorCounter = 0;
                }
                catch
                {
                    if (errorCounter > errorTolerance)
                    {
                        if (stage == "Take a shot")
                        {
                            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"{nameof(SendShot)} (FAILED_TO_TAKE_SHOT) :: Propably window closed :: {port} :: {uiApp.Caption}", LoggerStateConst.DEBUG);
                        }
                        else
                        {
                            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"{nameof(SendShot)} (ERROR) :: Failed at {stage} for {port} :: {uiApp.Caption}", LoggerStateConst.ERROR);
                        }


                        Stop();
                    }
                    errorCounter++;
                    LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"{nameof(SendShot)} (COMMUNICATION_ERROR) :: errorCounter now {errorCounter}/{errorTolerance} :: {port} :: {uiApp.Caption}", LoggerStateConst.DEBUG);
                }
            }
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"{nameof(SendShot)} FINISHED :: {port} :: {uiApp.Caption}", LoggerStateConst.DEBUG);
        }

        private WindowProcesserHost InitWindowProcessor()
        {
            WindowProcesserHost windowProcesser = null;
            try
            {
                _ = int.TryParse(Settings.Instance.ImageAmountSeed, out int imageAmountSeed);
                windowProcesser = new WindowProcesserHost(windowHandle, imageAmountSeed);
            }
            catch (Exception)
            {
                LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"ERROR Parsing imageAmountSeed :: {port} :: {uiApp.Caption}", LoggerStateConst.DEBUG);
            }

            return windowProcesser;
        }

        private bool IsOurWindowFocused()
        {
            var activatedHandle = User32.GetForegroundWindow();
            var isWindowIsFocused = activatedHandle == uiApp.HWnd;
            return isWindowIsFocused;
        }

        private void ReceiveEvents(CancellationToken cancellationToken)
        {
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"{nameof(ReceiveEvents)} START :: {port} :: {uiApp.Caption}", LoggerStateConst.DEBUG);

            // randomized delays, too look more like human
            Random rnd = new Random();
            var minDelay = int.Parse(Settings.Instance.MinDelayReceive);
            var maxDelay = int.Parse(Settings.Instance.MaxDelayReceive);

            var errorCounter = 0;
            var errorTolerance = 2;

            while (running)
            {
                SleepForCooldown(rnd.Next(minDelay, maxDelay));
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                try
                {
                    var payload = payloadConverter.Deserialize(stream);
                    if (payload == null) continue;

                    ReadCommandValues(payload);
                    errorCounter = 0;
                }
                catch
                {
                    if (errorCounter > errorTolerance)
                    {
                        LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"{nameof(ReceiveEvents)} ::: Failed for {port} :: {windowHandle}", LoggerStateConst.DEBUG);
                        Stop();
                    }
                    errorCounter++;
                }
            }
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"{nameof(ReceiveEvents)} FINISHED :: {port} :: {uiApp.Caption}", LoggerStateConst.DEBUG);
        }

        private void ReadCommandValues(Payload payload)
        {
            switch (payload.Command)
            {
                case CommandConst.KeyEvent:
                    InputHandler.HandleKeyboardEvents(payload);
                    break;
                case CommandConst.MouseEvent:
                    inputHandler.HandleMouseEvents(payload);
                    break;
                case CommandConst.Disconnect:
                    clientDisconnecting = true;
                    Stop();
                    break;
            }
        }

        private static void SleepForCooldown(int delay)
        {
            Thread.Sleep(delay);
        }

        private bool _stopping = false;
        public void Stop()
        {
            if (_stopping) return;
            _stopping = true;
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"Stop :: START :: {port} :: {uiApp.Caption}", LoggerStateConst.DEBUG);

            // Disconnect client
            if (!clientDisconnecting && stream != null)
            {
                var payload = new Payload
                {
                    Command = CommandConst.Disconnect,
                };
                payloadConverter.Serialize(payload, stream);
            }

            this.running = false;
            Thread.Sleep(750); // Cool down stopping threads

            if (stream != null) stream.Close();
            if (client != null) client.Close();
            if (listener != null)
            {
                listener.Stop();
                listener.Server.Close();
            }

            // callback
            OnServiceClosing(this.windowHandle);
            LogWindowApp.Instance.Add(0.0f, LoggerStateConst.Service, $"Stop :: FINISHED :: {port} :: {uiApp.Caption}", LoggerStateConst.DEBUG);
        }
    }
}
