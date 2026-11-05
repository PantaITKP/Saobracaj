using System.Windows.Forms;
using Saobracaj.Planiranje.Podaci;

namespace Saobracaj.Planiranje.Forme
{
    public enum VrstaPlana { Nedeljni, Operativni }

    public enum TabTrase { Uvoz, SveTrase, AdHoc }

    internal static class PraviloPristupa
    {
        // Ista poruka kao u postojećim formama; kodove formi upisuje administrator kroz frmForme.
        internal static bool Proveri(string code)
        {
            if (PravaRepo.Ucitaj(code).Pristup) return true;
            MessageBox.Show("Nemate prava za pristup ovoj formi", code);
            return false;
        }
    }
}
