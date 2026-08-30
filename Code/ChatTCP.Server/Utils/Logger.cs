using System;
using System.IO;
using System.Text;

namespace ChatTCP.Server.Utils
{
  
    public class Logger
    {
        private static readonly Lazy<Logger> _defaultInstance =
            new Lazy<Logger>(() => new Logger());
        public static Logger Instance => _defaultInstance.Value;

        private readonly string _logDirectory;
        private readonly object _writeLock = new object();

        /// <param name="logDirectory">
       
       
        public Logger(string? logDirectory = null)
        {
            _logDirectory = logDirectory
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch
            {
                
            }
        }

        private string CurrentLogFilePath =>
            Path.Combine(_logDirectory, $"server_{DateTime.Now:yyyyMMdd}.log");

      
        public void Log(string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

            lock (_writeLock)
            {
                try
                {
                    File.AppendAllText(CurrentLogFilePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    
                    
                    
                }
            }
        }

        
        public void LogError(Exception ex, string? context = null)
        {
            string prefix = string.IsNullOrEmpty(context) ? "[ERROR]" : $"[ERROR] ({context})";
            Log($"{prefix} {ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
    }
}