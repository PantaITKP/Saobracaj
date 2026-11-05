using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Telegrami
{
    /// <summary>
    /// Jedan po procesu aplikacije. Dok je otvorena bar jedna forma Planer ili dispMain, na 30 s proverava
    /// da li je agent upisao nove telegrame (Telegrami.ID &gt; poslednji viđeni, UnosAutomatski = 1) ili otkazivanja
    /// (TelegramiMailLog.Status = 'Otkazano') i prikazuje MessageBox. Telegrami stigli pre otvaranja forme se ne prikazuju.
    /// </summary>
    public sealed class TelegramNotifier
    {
        public static TelegramNotifier Instance { get; } = new TelegramNotifier();

        /// <summary>Za buduće automatsko dodavanje trasa; za sada ga niko ne obrađuje.</summary>
        public event EventHandler<TelegramInfo> TelegramPrimljen;

        private const int MaksPojedinacnih = 3;
        private const int MaksRedovaPoruke = 15;

        private readonly Timer timer = new Timer { Interval = 30000 };
        private readonly List<Prijava> prijave = new List<Prijava>();
        private readonly Queue<List<TelegramInfo>> red = new Queue<List<TelegramInfo>>();
        private int? poslednjiTelegramID, poslednjiLogID;
        private bool proverava, prikazuje;

        private sealed class Prijava
        {
            public Form Forma;
            public TelegramiTab Tab;     // samo Planer
            public DateTime Prijavljena;
        }

        private TelegramNotifier()
        {
            timer.Tick += async (s, e) => await Proveri();
        }

        /// <param name="tab">Tab Telegrami (za Planer); za dispMain null — posle poruke se ništa ne radi.</param>
        public void Register(Form forma, object tab = null)
        {
            if (prijave.Any(p => p.Forma == forma)) return;
            prijave.Add(new Prijava { Forma = forma, Tab = tab as TelegramiTab, Prijavljena = DateTime.Now });
            forma.FormClosed += (s, e) => Unregister(forma);
            if (prijave.Count == 1) Pokreni();
        }

        public void Unregister(Form forma)
        {
            prijave.RemoveAll(p => p.Forma == forma);
            if (prijave.Count > 0) return;
            timer.Stop();
            poslednjiTelegramID = poslednjiLogID = null; // ništa se ne čuva između otvaranja
            red.Clear();
        }

        private async void Pokreni()
        {
            try
            {
                var p = await Task.Run(() => TelegramiRepository.PoslednjiID());
                if (prijave.Count == 0) return;
                poslednjiTelegramID = p.Item1;
                poslednjiLogID = p.Item2;
            }
            catch (Exception ex)
            {
                Status("Telegrami: provera nije pokrenuta (" + ex.Message + ")");
            }
            timer.Start();
        }

        private async Task Proveri()
        {
            if (proverava || prijave.Count == 0) return;
            proverava = true;
            try
            {
                if (poslednjiTelegramID == null)
                {
                    var p = await Task.Run(() => TelegramiRepository.PoslednjiID());
                    poslednjiTelegramID = p.Item1;
                    poslednjiLogID = p.Item2;
                    return;
                }
                int t = poslednjiTelegramID.Value, l = poslednjiLogID ?? 0, nt = t, nl = l;
                var novi = await Task.Run(() => TelegramiRepository.Novi(t, l, out nt, out nl));
                if (prijave.Count == 0) return;
                // Pomeranje odmah: isti telegram nikada ne izlazi dvaput
                poslednjiTelegramID = nt;
                poslednjiLogID = nl;
                if (novi.Count == 0) return;

                foreach (var info in novi)
                {
                    try { TelegramPrimljen?.Invoke(this, info); }
                    catch (Exception ex) { OperationalDiagnostics.Write("TelegramPrimljen: " + ex); }
                }
                red.Enqueue(novi);
                PrikaziRed();
            }
            catch (Exception ex)
            {
                // Greška čitanja baze se ne prikazuje kao MessageBox
                Status("Telegrami: provera nije uspela " + DateTime.Now.ToString("HH:mm") + " (" + ex.Message + ")");
                OperationalDiagnostics.Write("TelegramNotifier: " + ex);
            }
            finally
            {
                proverava = false;
            }
        }

        private void PrikaziRed()
        {
            if (prikazuje) return; // dok je jedna poruka otvorena, nova čekaju
            prikazuje = true;
            try
            {
                while (red.Count > 0 && prijave.Count > 0)
                {
                    var paket = red.Dequeue();
                    if (paket.Count > MaksPojedinacnih)
                        Prikazi("Stiglo je " + paket.Count + " novih telegrama",
                            "Stiglo je " + paket.Count + " novih telegrama: " + string.Join(", ", paket.Select(x => x.Naziv)) + "." + Environment.NewLine + Environment.NewLine + "Vidi tab Telegrami na Planeru.",
                            paket.Any(x => x.Otkazivanje) ? MessageBoxIcon.Warning : MessageBoxIcon.Information, paket);
                    else
                        foreach (var info in paket)
                            Prikazi(info.Otkazivanje ? "Otkazivanje: telegram br. " + info.Naziv : "Novi telegram br. " + info.Naziv,
                                Tekst(info), info.Otkazivanje ? MessageBoxIcon.Warning : MessageBoxIcon.Information, new List<TelegramInfo> { info });
                }
            }
            finally
            {
                prikazuje = false;
            }
        }

        private void Prikazi(string naslov, string tekst, MessageBoxIcon ikona, List<TelegramInfo> telegrami)
        {
            var vlasnik = Vlasnik();
            if (vlasnik == null) return;
            if (vlasnik.WindowState == FormWindowState.Minimized)
            {
                // Minimizovana forma: treptanje u taskbar-u + poruka preko svih prozora
                Treperi(vlasnik);
                using (var gore = new Form { TopMost = true, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-32000, -32000), Size = new System.Drawing.Size(1, 1) })
                {
                    gore.Show();
                    MessageBox.Show(gore, tekst, naslov, MessageBoxButtons.OK, ikona);
                }
            }
            else MessageBox.Show(vlasnik, tekst, naslov, MessageBoxButtons.OK, ikona);

            // Planer: osveži grid i selektuj redove telegrama. dispMain: ništa.
            var ids = telegrami.SelectMany(x => x.Redovi).Select(r => r.ID).ToList();
            foreach (var p in prijave.Where(p => p.Tab != null).ToList())
                _ = p.Tab.Ucitaj(ids.Count > 0 ? ids : null);
        }

        /// <summary>Aktivna prijavljena forma, inače poslednja otvorena.</summary>
        private Form Vlasnik()
        {
            var aktivna = Form.ActiveForm;
            var p = prijave.FirstOrDefault(x => x.Forma == aktivna && !x.Forma.IsDisposed)
                    ?? prijave.Where(x => !x.Forma.IsDisposed).OrderByDescending(x => x.Prijavljena).FirstOrDefault();
            return p?.Forma;
        }

        internal static string Tekst(TelegramInfo info)
        {
            var sb = new StringBuilder();
            var linije = new List<string>();
            if (info.Otkazivanje)
            {
                var ciljevi = info.Ugaseni.GroupBy(r => new { r.Broj, r.DatumTelegrama }).ToList();
                if (ciljevi.Count == 0) linije.Add("Telegram br. " + info.Naziv + " otkazuje / stavlja van snage telegram koji nije u tabeli Telegrami.");
                foreach (var g in ciljevi)
                {
                    var prvi = g.OrderBy(r => r.VaziOd).First();
                    var poslednji = g.OrderByDescending(r => r.VaziDo).First();
                    var rel = string.Join(", ", g.Select(r => r.Od == r.Do ? r.Od : r.Od + " – " + r.Do).Distinct());
                    linije.Add("Telegram br. " + info.Naziv + " OTKAZUJE telegram br. " + g.Key.Broj +
                               (g.Key.DatumTelegrama.HasValue ? " od " + g.Key.DatumTelegrama.Value.ToString("dd.MM.yyyy") : "") +
                               " (" + rel + ", " + prvi.VaziOd.ToString("dd.MM.") + "–" + poslednji.VaziDo.ToString("dd.MM.yyyy") + ")");
                    linije.Add("Ugašeno redova: " + g.Count());
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(info.Pruga)) linije.Add("Pruga: " + info.Pruga);
                int i = 0;
                foreach (var r in info.Redovi)
                {
                    i++;
                    linije.Add((info.Redovi.Count > 1 ? i + ") " : "") + r.Opis());
                    if (!string.IsNullOrWhiteSpace(r.Napomena))
                        linije.Add("    " + Skrati(r.Napomena, 160));
                }
            }
            var ukupno = info.Otkazivanje ? info.Ugaseni.Count : info.Redovi.Count;
            if (linije.Count > MaksRedovaPoruke)
            {
                linije = linije.Take(MaksRedovaPoruke).ToList();
                linije.Add("… (ukupno " + ukupno + " stavki, vidi tab Telegrami)");
            }
            foreach (var l in linije) sb.AppendLine(l);
            return sb.ToString().TrimEnd();
        }

        private static string Skrati(string s, int max)
        {
            s = s.Replace("\r", " ").Replace("\n", " ");
            if (s.Length <= max) return s;
            var cut = s.LastIndexOf(' ', max);
            return s.Substring(0, cut > max / 2 ? cut : max) + " …";
        }

        private void Status(string tekst)
        {
            foreach (var p in prijave)
            {
                if (p.Tab != null) p.Tab.Status(tekst);
            }
            OperationalDiagnostics.Write(tekst);
        }

        // ------------------------------------------------------------------ treptanje u taskbar-u

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO { public uint cbSize; public IntPtr hwnd; public uint dwFlags; public uint uCount; public uint dwTimeout; }

        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        private static void Treperi(Form f)
        {
            try
            {
                var fi = new FLASHWINFO { hwnd = f.Handle, dwFlags = 3 | 12, uCount = uint.MaxValue, dwTimeout = 0 }; // FLASHW_ALL | FLASHW_TIMERNOFG
                fi.cbSize = (uint)Marshal.SizeOf(fi);
                FlashWindowEx(ref fi);
            }
            catch { /* treptanje nije bitno */ }
        }
    }
}
