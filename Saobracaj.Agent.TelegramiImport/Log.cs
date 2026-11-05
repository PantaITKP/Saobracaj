using System;
using System.IO;
using System.Text;

namespace Saobracaj.Agent.TelegramiImport
{
    /// <summary>Log fajl po danu u LogFolder (TelegramiImport-yyyyMMdd.log) + konzola. Bez lozinki i bez celog teksta mail-a.</summary>
    internal sealed class Log
    {
        private readonly string folder;
        private readonly object sync = new object();

        public Log(string folder)
        {
            this.folder = folder;
        }

        public void Info(string poruka) { Pisi("INFO ", poruka); }
        public void Upozorenje(string poruka) { Pisi("WARN ", poruka); }
        public void Greska(string poruka, Exception ex = null) { Pisi("ERROR", poruka + (ex != null ? Environment.NewLine + ex : "")); }

        private void Pisi(string nivo, string poruka)
        {
            var red = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + nivo + " " + poruka;
            lock (sync)
            {
                Console.WriteLine(red);
                try
                {
                    Directory.CreateDirectory(folder);
                    File.AppendAllText(Path.Combine(folder, AgentConfig.AgentNaziv + "-" + DateTime.Now.ToString("yyyyMMdd") + ".log"),
                        red + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Log fajl nije dostupan: " + ex.Message);
                }
            }
        }
    }
}
