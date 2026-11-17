using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>
    /// Log po danu (CimProvera-yyyyMMdd.log) + konzola. Vreme sa oznakom zone. Bez lozinki i bez sadržaja dokumenata:
    /// loguju se ID poruke, heš, nalazi i trajanje.
    /// </summary>
    internal sealed class Log
    {
        private readonly string folder;
        private readonly object sync = new object();
        public bool Tiho { get; set; }

        public Log(string folder, int cuvajDana = 90)
        {
            this.folder = folder;
            try
            {
                if (Directory.Exists(folder))
                    foreach (var f in Directory.GetFiles(folder, AgentConfig.AgentNaziv + "-*.log")
                                 .Where(f => File.GetLastWriteTime(f) < DateTime.Now.AddDays(-cuvajDana)))
                        File.Delete(f);
            }
            catch { /* brisanje starih logova ne sme da obori agent */ }
        }

        public void Info(string poruka) { Pisi("INFO ", poruka); }
        public void Upozorenje(string poruka) { Pisi("WARN ", poruka); }
        public void Greska(string poruka, Exception ex = null) { Pisi("ERROR", poruka + (ex != null ? Environment.NewLine + ex : "")); }

        private void Pisi(string nivo, string poruka)
        {
            var red = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " " + nivo + " " + poruka;
            lock (sync)
            {
                if (!Tiho) Console.Error.WriteLine(red);
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
