namespace SharedLibrary
{
    /// <summary>
    /// Type of commands we are sending
    /// </summary>
    public static class CommandConst
    {
        public const string ClientConnecting = "CLIENT_CONNECTING";
        public const string Ping = "WINDOW_SYNC";
        public const string KeyEvent = "KEY";
        public const string MouseEvent = "MOUSE";
        public const string WindowShot = "WINDOWSHOT";
        public const string Disconnect = "DISCONNECT";
    }

    /// <summary>
    /// Events of those commands
    /// </summary>
    public static class EventConst
    {
        public const string Mouse_Move = "Mouse_Move";
        public const string MouseL_Down = "MouseL_Down";
        public const string MouseR_Down = "MouseR_Down";
        public const string MouseL_Up = "MouseL_Up";
        public const string MouseR_Up = "MouseR_Up";
        public const string Mouse_DoubleClick = "Mouse_DoubleClick";
    }

    public static class LoggerStateConst
    {
        // Debug Levels
        public const string DEBUG = "DEBUG";
        public const string INFORMATIVE = "INFORMATIVE";
        public const string ERROR = "ERROR";

        // System levels
        public const string System = "System";
        public const string Server = "Server";
        public const string App = "App";
        public const string Hooks = "Hooks";
        public const string Client = "Client";
        public const string Service = "Service";
        public const string Window = "Window";
        public const string Screen = "Screen";
        public const string Command = "Command";
    }
}
