using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saobracaj.MLProd
{
    public static class TrainLogger
    {
        private static readonly object _lock = new object();
        private static readonly string _dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        private static string FilePath =>
            Path.Combine(_dir, $"ml_train_{DateTime.Now:yyyy-MM-dd}.log");

        public static string StartSession()
        {
            var id = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            Info($"===== TRAIN START {id} =====");
            return id;
        }

        public static void EndSession(string id) =>
            Info($"===== TRAIN END   {id} =====");

        public static void Info(string msg) => Write("INFO", msg);
        public static void Warn(string msg) => Write("WARN", msg);
        public static void Error(string msg, Exception ex = null) =>
            Write("ERROR", ex == null ? msg : $"{msg}\r\n{ex}");

        public static void Progress(TrainProgress p)
        {
            if (p == null) return;
            var tail = string.IsNullOrWhiteSpace(p.Message) ? "" : " — " + p.Message;
            Write("PROG", $"{p.Percent,3}% {p.Stage}{tail}");
        }

        public static void OpenLogFolder()
        {
            try
            {
                if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);
                Process.Start("explorer.exe", _dir);
            }
            catch { /* ignore */ }
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);
                    using (var fs = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (var sw = new StreamWriter(fs, new UTF8Encoding(false)))
                    {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}");
                    }
                }
            }
            catch { /* ne ruši app zbog log IO problema */ }
        }
    }
}
