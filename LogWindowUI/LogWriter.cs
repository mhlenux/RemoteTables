using System;
using System.IO;
using System.Reflection;

namespace LogWindowUI
{
    public static class LogWriter
    {
        private static string logPath = string.Empty;

        public static void LogWrite(string logMessage)
        {

            if (logPath == string.Empty)
            {
                logPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "log.log";
                File.Delete(logPath);
            }

            try
            {
                using (StreamWriter writer = File.AppendText(logPath))
                {
                    Log(logMessage, writer);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
