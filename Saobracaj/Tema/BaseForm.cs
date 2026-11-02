using System;
using System.Windows.Forms;

namespace Saobracaj.Tema
{
    // Osnovna forma novog izgleda: animacija otvaranja (500 ms, fade + pomak 12 px).
    public class BaseForm : Form
    {
        private bool cekaOtvaranje;

        protected override void OnLoad(EventArgs e)
        {
            if (!DesignMode && TopLevel && !IsMdiChild && Theme.AnimacijeUkljucene)
            {
                Opacity = 0;
                cekaOtvaranje = true;
            }
            base.OnLoad(e);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!cekaOtvaranje) return;
            cekaOtvaranje = false;

            int vrh = Top;
            bool pomak = WindowState == FormWindowState.Normal;
            int rastojanje = Theme.Dp(12);
            Animator.Pokreni(this, Theme.TrajanjeForme, Animator.Uvod, t =>
            {
                Opacity = t;
                if (pomak) Top = vrh + (int)(rastojanje * (1 - t));
            }, () =>
            {
                Opacity = 1;
                if (pomak) Top = vrh;
            });
        }
    }
}
