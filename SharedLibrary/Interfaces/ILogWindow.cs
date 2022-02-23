namespace SharedLibrary.Interfaces
{
    public interface ILogWindow
    {
        public void Add(float timestamp, string system, string message, string level);
    }
}
