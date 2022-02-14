using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Syncfusion.Windows.Forms.Grid.Grouping;
using Syncfusion.Windows.Forms;
using System.Configuration;
using System.Data.SqlClient;

namespace Saobracaj
{
    public partial class MainP : Syncfusion.Windows.Forms.Tools.RibbonForm
    {
        string Korisnik = "";
        public bool PravoP;
        public MainP()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzcwMDg5QDMxMzgyZTM0MmUzMFhQSmlDM0M2bGpxcXVtT1VScTg1a0dtVTFLcUZiK0tLRnpvRTYyRFpMc3M9");
            InitializeComponent();
        }

        public MainP(string Logovan, int Lozinka)
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzcwMDg5QDMxMzgyZTM0MmUzMFhQSmlDM0M2bGpxcXVtT1VScTg1a0dtVTFLcUZiK0tLRnpvRTYyRFpMc3M9");
            InitializeComponent();
            Korisnik = Logovan;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Sifarnici.frmStanice stan = new Sifarnici.frmStanice();
            PravoP = stan.Pravo;
            if (PravoP == true) { stan.Show(); } else { return; }
        }
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Sifarnici.frmTrase trase = new Sifarnici.frmTrase();
            PravoP = trase.Pravo;
            if (PravoP == true) { trase.Show(); } else { return; }
        }
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            Sifarnici.frmPruge prug = new Sifarnici.frmPruge();
            PravoP = prug.Pravo;
            if (PravoP == true) { prug.Show(); } else { return; }
        }
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            Sifarnici.frmLokomotive lokomotive = new Sifarnici.frmLokomotive();
            PravoP = lokomotive.Pravo;
            if (PravoP == true) { lokomotive.Show(); } else { return; }
        }
        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            Sifarnici.frmSerijeLokomotiva serije = new Sifarnici.frmSerijeLokomotiva();
            PravoP = serije.Pravo;
            if (PravoP == true) { serije.Show(); } else { return; }
        }
        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            Sifarnici.frmStatusVoza statvoz = new Sifarnici.frmStatusVoza();
            PravoP = statvoz.Pravo;
            if (PravoP == true) { statvoz.Show(); } else { return; }
        }
        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            Sifarnici.frmRazlozi raz = new Sifarnici.frmRazlozi();
            PravoP = raz.Pravo;
            if (PravoP == true) { raz.Show(); } else { return; }
        }
        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            Sifarnici.frmTipPrevoza tipP = new Sifarnici.frmTipPrevoza();
            PravoP = tipP.Pravo;
            if (PravoP == true) { tipP.Show(); } else { return; }
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            Sifarnici.frmNHM nhm = new Sifarnici.frmNHM();
            PravoP = nhm.Pravo;
            if (PravoP == true) { nhm.Show(); } else { return; }
        }
        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            Sifarnici.frmOsoblje osob = new Sifarnici.frmOsoblje();
            PravoP = osob.Pravo;
            if (PravoP == true) { osob.Show(); } else { return; }
        }
        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            Sifarnici.frmPartnerji part = new Sifarnici.frmPartnerji();
            PravoP = part.Pravo;
            if (PravoP == true) { part.Show(); } else { return; }
        }
        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            Sifarnici.frmTipTelegrama ttel = new Sifarnici.frmTipTelegrama();
            PravoP = ttel.Pravo;
            if (PravoP == true) { ttel.Show(); } else { return; }
        }
        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            Sifarnici.frmTelegrami tel = new Sifarnici.frmTelegrami();
            PravoP = tel.Pravo;
            if (PravoP == true) { tel.Show(); } else { return; }
        }
        //Priprema
        private void toolStripButton15_Click(object sender, EventArgs e)
        {
            Dokumenta.frmNajava frmNaj = new Dokumenta.frmNajava(Korisnik, 0);
            PravoP = frmNaj.Pravo;
            if (PravoP == true) { frmNaj.Show(); } else { return; }
        }
        private void toolStripButton16_Click(object sender, EventArgs e)
        {
            Dokumenta.frmNajava frmNaj = new Dokumenta.frmNajava(Korisnik, 1);
            PravoP = frmNaj.Pravo;
            if (PravoP == true) { frmNaj.Show(); } else { return; }
        }
        private void toolStripButton17_Click(object sender, EventArgs e)
        {
            Dokumenta.frmNajava20 naj = new Dokumenta.frmNajava20();
            PravoP = naj.Pravo;
            if (PravoP == true) { naj.Show(); } else { return; }
        }
        private void toolStripButton18_Click(object sender, EventArgs e)
        {
            SyncForm.frmNajavaArhivaAnaliza sNA = new SyncForm.frmNajavaArhivaAnaliza();
            PravoP = sNA.Pravo;
            if (PravoP == true) { sNA.Show(); } else { return; }
        }
        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            SyncForm.frmAnalizaPorudzbina apor = new SyncForm.frmAnalizaPorudzbina();
            PravoP = apor.Pravo;
            if (PravoP == true) { apor.Show(); } else { return; }
        }
        private void toolStripButton19_Click(object sender, EventArgs e)
        {
            Dokumenta.frmTeretnicePregled frmTer = new Dokumenta.frmTeretnicePregled(Korisnik);
            PravoP = frmTer.Pravo;
            if (PravoP == true) { frmTer.Show(); } else { return; }
        }
        private void toolStripButton20_Click(object sender, EventArgs e)
        {
            Dokumenta.frmIskljuceniVagoni iv = new Dokumenta.frmIskljuceniVagoni();
            PravoP = iv.Pravo;
            if (PravoP == true) { iv.Show(); } else { return; }
        }
        private void toolStripButton21_Click(object sender, EventArgs e)
        {
            Dokumenta.frmNajaveBezTeretnice nbt = new Dokumenta.frmNajaveBezTeretnice();
            PravoP = nbt.Pravo;
            if (PravoP == true) { nbt.Show(); } else { return; }
        }
        private void toolStripButton22_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPregledRID prid = new Dokumenta.frmPregledRID();
            PravoP = prid.Pravo;
            if (PravoP == true) { prid.Show(); } else { return; }
        }
        private void toolStripButton23_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRIDPoNajavama rpn = new Dokumenta.frmRIDPoNajavama();
            PravoP = rpn.Pravo;
            if (PravoP == true) { rpn.Show(); } else { return; }
        }
        private void toolStripButton24_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRIDTeretnice fridter = new Dokumenta.frmRIDTeretnice();
            PravoP = fridter.Pravo;
            if (PravoP == true) { fridter.Show(); } else { return; }
        }
        private void toolStripButton25_Click(object sender, EventArgs e)
        {
            Mobile.frmZavrsnaDokumentacija zdok = new Mobile.frmZavrsnaDokumentacija();
            PravoP = zdok.Pravo;
            if (PravoP == true) { zdok.Show(); } else { return; }
        }
        private void toolStripButton27_Click(object sender, EventArgs e)
        {
            Dokumenta.frmMapa mapa = new Dokumenta.frmMapa();
            mapa.Show();
            /*
            Dokumenta.frmTest test = new Dokumenta.frmTest();
            PravoP = test.Pravo;
            if (PravoP == true) { test.Show(); } else { return; }*/
        }
        private void toolStripButton28_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPregledRN prn = new Dokumenta.frmPregledRN();
            PravoP = prn.Pravo;
            if (PravoP == true) { prn.Show(); } else { return; }
        }
        private void toolStripButton29_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRadniNalogPregled frmp2 = new Dokumenta.frmRadniNalogPregled();
            PravoP = frmp2.Pravo;
            if (PravoP == true) { frmp2.Show(); } else { return; }
            //Ovde imam duplo
            //  Dokumenta.frmRadniNalogPregled frmRN = new Dokumenta.frmRadniNalogPregled();
            //  frmRN.Show();
        }
        private void toolStripButton30_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRAdniNalogPregledPoLokomotivama rnpl = new Dokumenta.frmRAdniNalogPregledPoLokomotivama();
            PravoP = rnpl.Pravo;
            if (PravoP == true) { rnpl.Show(); } else { return; }
        }
        private void toolStripButton31_Click(object sender, EventArgs e)
        {
            Dokumenta.frmStampaRadnogNaloga srn = new Dokumenta.frmStampaRadnogNaloga();
            PravoP = srn.Pravo;
            if (PravoP == true) { srn.Show(); } else { return; }
        }
        private void toolStripButton33_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRaspustiVagone frv = new Dokumenta.frmRaspustiVagone();
            PravoP = frv.Pravo;
            if (PravoP == true) { frv.Show(); } else { return; }
        }
        private void toolStripButton34_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPronadjiVagon pv = new Dokumenta.frmPronadjiVagon();
            PravoP = pv.Pravo;
            if (PravoP == true) { pv.Show(); } else { return; }
        }
        private void toolStripButton38_Click(object sender, EventArgs e)
        {
            Dokumenta.frmMUPDozvola Dozvola = new Dokumenta.frmMUPDozvola();
            PravoP = Dozvola.Pravo;
            if (PravoP == true) { Dozvola.Show(); } else { return; }
        }

        private void toolStripButton37_Click(object sender, EventArgs e)
        {
            Dokumenta.frmMupMesto mesto = new Dokumenta.frmMupMesto();
            PravoP = mesto.Pravo;
            if (PravoP == true) { mesto.Show(); } else { return; }
        }

        private void toolStripButton35_Click(object sender, EventArgs e)
        {
            Administracija.frmKorisnici kor = new Administracija.frmKorisnici();
            PravoP = kor.Pravo;
            if (PravoP == true) { kor.Show(); } else { return; }
        }

        private void toolStripButton36_Click(object sender, EventArgs e)
        {
            Dokumenta.frmAutomobili autom = new Dokumenta.frmAutomobili();
            PravoP = autom.Pravo;
            if (PravoP == true) { autom.Show(); } else { return; }
        }

        private void toolStripButton39_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRegistator reg = new Dokumenta.frmRegistator();
            PravoP = reg.Pravo;
            if (PravoP == true) { reg.Show(); } else { return; }
        }

        private void toolStripButton40_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRegistratorPregled regpre = new Dokumenta.frmRegistratorPregled();
            PravoP = regpre.Pravo;
            if (PravoP == true) { regpre.Show(); } else { return; }
        }

        private void toolStripButton41_Click(object sender, EventArgs e)
        {
            Servis.frmVrstePopisa vrPopisa = new Servis.frmVrstePopisa();
            PravoP = vrPopisa.Pravo;
            if (PravoP == true) { vrPopisa.Show(); } else { return; }
        }

        private void toolStripButton42_Click(object sender, EventArgs e)
        {
            Servis.frmLokomotivaVrstaPopisa lokvp = new Servis.frmLokomotivaVrstaPopisa();
            PravoP = lokvp.Pravo;
            if (PravoP == true) { lokvp.Show(); } else { return; }
        }

        private void toolStripButton43_Click(object sender, EventArgs e)
        {
            Servis.frmGrupaKvarova gkv = new Servis.frmGrupaKvarova();
            PravoP = gkv.Pravo;
            if (PravoP == true) { gkv.Show(); } else { return; }
        }

        private void toolStripButton44_Click(object sender, EventArgs e)
        {
            Servis.frmKvarovi kv = new Servis.frmKvarovi();
            PravoP = kv.Pravo;
            if (PravoP == true) { kv.Show(); } else { return; }
        }

        private void toolStripButton45_Click(object sender, EventArgs e)
        {
            Servis.frmEvidencijaKvarova pkvar = new Servis.frmEvidencijaKvarova();
            PravoP = pkvar.Pravo;
            if (PravoP == true) { pkvar.Show(); } else { return; }
        }

        private void toolStripButton46_Click(object sender, EventArgs e)
        {
            Servis.frmEvidencijaKvarovaAnaliza evkva = new Servis.frmEvidencijaKvarovaAnaliza();
            PravoP = evkva.Pravo;
            if (PravoP == true) { evkva.Show(); } else { return; }
        }

        private void toolStripButton50_Click(object sender, EventArgs e)
        {
            Administracija.frmSistematizacija sistem = new Administracija.frmSistematizacija();
            PravoP = sistem.Pravo;
            if (PravoP == true) { sistem.Show(); } else { return; }
        }

        private void toolStripButton51_Click(object sender, EventArgs e)
        {
            Administracija.frmSistematizacijaPovezivanje sist = new Administracija.frmSistematizacijaPovezivanje();
            PravoP = sist.Pravo;
            if (PravoP == true) { sist.Show(); } else { return; }
        }

        private void toolStripButton52_Click(object sender, EventArgs e)
        {
            Sifarnici.frmVrsteAktivnosti vrakt = new Sifarnici.frmVrsteAktivnosti();
            if (Korisnik == "admin")
            {
                vrakt.Show();
            }
            else { MessageBox.Show("Nemate prava za pristup ovoj formi"); return; }
        }

        private void toolStripButton53_Click(object sender, EventArgs e)
        {
            Sifarnici.frmCenaPoRadniku cpr = new Sifarnici.frmCenaPoRadniku();
            if (Korisnik == "admin")
            {
                cpr.Show();
            }
            else { MessageBox.Show("Nemate prava za pristup ovoj formi"); return; }
        }

        private void toolStripButton54_Click(object sender, EventArgs e)
        {

            Dokumenta.frmPravoAktivnosti pravo = new Dokumenta.frmPravoAktivnosti();
            PravoP = pravo.Pravo;
            if (PravoP == true) { pravo.Show(); } else { return; }
        }

        private void toolStripButton55_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPravoAktivnostiViseRadnika pravovise = new Dokumenta.frmPravoAktivnostiViseRadnika();
            PravoP = pravovise.Pravo;
            if (PravoP == true) { pravovise.Show(); } else { return; }
        }

        private void toolStripButton72_Click(object sender, EventArgs e)
        {
            Dokumenta.frmIzracunZarada fiz = new Dokumenta.frmIzracunZarada();
            PravoP = fiz.Pravo;
            if (PravoP == true) { fiz.Show(); } else { return; }
        }

        private void toolStripButton73_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPlacenoNeplaceno pln = new Dokumenta.frmPlacenoNeplaceno();
            PravoP = pln.Pravo;
            if (PravoP == true) { pln.Show(); } else { return; }
        }

        private void toolStripButton74_Click(object sender, EventArgs e)
        {
            Dokumenta.frmOsnovnaZarada osnzar = new Dokumenta.frmOsnovnaZarada();
            PravoP = osnzar.Pravo;
            if (PravoP == true) { osnzar.Show(); } else { return; }
        }

        private void toolStripButton69_Click(object sender, EventArgs e)
        {
            Dokumenta.frmBrojSmena brsm = new Dokumenta.frmBrojSmena();
            PravoP = brsm.Pravo;
            if (PravoP == true) { brsm.Show(); } else { return; }
        }

        private void toolStripButton57_Click(object sender, EventArgs e)
        {
            Dokumenta.frmEvidencijaGodišnjihOdmora evgo = new Dokumenta.frmEvidencijaGodišnjihOdmora();
            PravoP = evgo.Pravo;
            if (PravoP == true) { evgo.Show(); } else { return; }
        }

        private void toolStripButton60_Click(object sender, EventArgs e)
        {
            Mobile.frmPrijavaSmene prodj = new Mobile.frmPrijavaSmene();
            PravoP = prodj.Pravo;
            if (PravoP == true) { prodj.Show(); } else { return; }
        }

        private void toolStripButton61_Click(object sender, EventArgs e)
        {
            SyncForm.frmPregledMobilni drugi = new SyncForm.frmPregledMobilni();
            PravoP = drugi.Pravo;
            if (PravoP == true) { drugi.Show(); } else { return; }
        }

        private void toolStripButton62_Click(object sender, EventArgs e)
        {
           Servis.frmPrijavaMasinovodje mas = new Servis.frmPrijavaMasinovodje();
            PravoP = mas.Pravo;
            if (PravoP == true) { mas.Show(); } else { return; }
        }

        private void toolStripButton63_Click(object sender, EventArgs e)
        {
            SyncForm.frmPregledMasinovodje novi = new SyncForm.frmPregledMasinovodje();
            PravoP = novi.Pravo;
            if (PravoP == true) { novi.Show(); } else { return; }
        }

        private void toolStripButton48_Click(object sender, EventArgs e)
        {
            Servis.frmNamirenja namir = new Servis.frmNamirenja();
            PravoP = namir.Pravo;
            if (PravoP == true) { namir.Show(); } else { return; }
        }

        private void toolStripButton49_Click(object sender, EventArgs e)
        {
            Servis.frmNamirenjaSumarno namSum = new Servis.frmNamirenjaSumarno();
            PravoP = namSum.Pravo;
            if (PravoP == true) { namSum.Show(); } else { return; }
        }

        private void toolStripButton58_Click(object sender, EventArgs e)
        {
            Mobile.frmAnalizaGO ago = new Mobile.frmAnalizaGO();
            PravoP = ago.Pravo;
            if (PravoP == true) { ago.Show(); } else { return; }
        }

        private void toolStripButton59_Click(object sender, EventArgs e)
        {
            Mobile.frmAnalizaGOSum GoSum = new Mobile.frmAnalizaGOSum();
            PravoP = GoSum.Pravo;
            if (PravoP == true) { GoSum.Show(); } else { return; }
        }

        private void toolStripButton56_Click(object sender, EventArgs e)
        {
            Mobile.frmSlobodniDani slob = new Mobile.frmSlobodniDani(Korisnik);
            PravoP = slob.Pravo;
            if (PravoP == true) { slob.Show(); } else { return; }
        }

        private void toolStripButton77_Click(object sender, EventArgs e)
        {
            Administracija.InsertAdministracije upd = new Administracija.InsertAdministracije();
            upd.UpdateNull();
            MessageBox.Show("Gotovo zatvori");
        }

        private void toolStripButton78_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPutniNaloziPregled fpn = new Dokumenta.frmPutniNaloziPregled();
            PravoP = fpn.Pravo;
            if (PravoP == true) { fpn.Show(); } else { return; }
        }

        private void toolStripButton79_Click(object sender, EventArgs e)
        {
            Dokumenta.frmEvidencijaRAdaNeplaceno nepl = new Dokumenta.frmEvidencijaRAdaNeplaceno();
            PravoP = nepl.Pravo;
            if (PravoP == true) { nepl.Show(); } else { return; }
        }

        private void toolStripButton80_Click(object sender, EventArgs e)
        {
            Dokumenta.frmFinansije novak = new Dokumenta.frmFinansije();
            PravoP = novak.Pravo;
            if (PravoP == true) { novak.Show(); } else { return; }
        }

        private void toolStripButton65_Click(object sender, EventArgs e)
        {
            SyncForm.frmPlanRada planrada = new SyncForm.frmPlanRada();
            PravoP = planrada.Pravo;
            if (PravoP == true) { planrada.Show(); } else { return; }
        }

        private void toolStripButton67_Click(object sender, EventArgs e)
        {
            Dokumenta.frmEvidencijaRada erM = new Dokumenta.frmEvidencijaRada(Korisnik);
            PravoP = erM.Pravo;
            if (PravoP == true) { erM.Show(); } else { return; }
        }

        private void toolStripButton68_Click(object sender, EventArgs e)
        {

            Dokumenta.frmZarade zar = new Dokumenta.frmZarade();
            if (Korisnik == "admin")
            {
                zar.Show();
            }
            else { MessageBox.Show("Nemate prava za pristup ovoj formi");return; }
        }
        private void toolStripButton70_Click(object sender, EventArgs e)
        {
            Dokumenta.frmEvidencijaRadaZaglavlje erz = new Dokumenta.frmEvidencijaRadaZaglavlje(Korisnik);
            PravoP = erz.Pravo;
            if (PravoP == true) { erz.Show(); } else { return; }
        }

        private void toolStripButton71_Click(object sender, EventArgs e)
        {
            SyncForm.frmAnalizaAktivnosti aAktiv = new SyncForm.frmAnalizaAktivnosti();
            PravoP = aAktiv.Pravo;
            if (PravoP == true) { aAktiv.Show(); } else { return; }
        }
        private void toolStripButton75_Click(object sender, EventArgs e)
        {
            Dokumenta.frmParametriObracuna parobr = new Dokumenta.frmParametriObracuna();
            PravoP = parobr.Pravo;
            if (PravoP == true) { parobr.Show(); } else { return; }
        }
        private void toolStripButton76_Click(object sender, EventArgs e)
        {
            Dokumenta.frmZarade zar = new Dokumenta.frmZarade();
            if (Korisnik == "admin")
            {
                zar.Show();
            }
            else { MessageBox.Show("Nemate prava za pristup ovoj formi");return; }
        }
        private void toolStripButton81_Click(object sender, EventArgs e)
        {
            Tehnologija.frmTehnologija teh = new Tehnologija.frmTehnologija(Korisnik);
            PravoP = teh.Pravo;
            if (PravoP == true) { teh.Show(); } else { return; }
        }
        private void toolStripButton82_Click(object sender, EventArgs e)
        {
            Sifarnici.frmTraseAnaliticki ta = new Sifarnici.frmTraseAnaliticki();
            PravoP = ta.Pravo;
            if (PravoP == true) { ta.Show(); } else { return; }
        }
        private void toolStripButton83_Click(object sender, EventArgs e)
        {
            Tehnologija.frmTehnologijaPregled tehp = new Tehnologija.frmTehnologijaPregled(Korisnik);
            PravoP = tehp.Pravo;
            if (PravoP == true) { tehp.Show(); } else { return; }
        }
        private void toolStripButton84_Click(object sender, EventArgs e)
        {
            Servis.frmPrijavaKvara pkv = new Servis.frmPrijavaKvara();
            PravoP = pkv.Pravo;
            if (PravoP == true) { pkv.Show(); } else { return; }
        }
        private void RibbonPanel_Click(object sender, EventArgs e)
        {
          //  Sifarnici.frmSifarnikKontrolnihGresaka skg = new Sifarnici.frmSifarnikKontrolnihGresaka();
           // skg.Show();
        }
        private void toolStripButton85_Click(object sender, EventArgs e)
        {
            Sifarnici.frmSifarnikKontrolnihGresaka skg = new Sifarnici.frmSifarnikKontrolnihGresaka();
            PravoP = skg.Pravo;
            if (PravoP == true) { skg.Show(); } else { return; }
        }
        private void toolStripButton86_Click(object sender, EventArgs e)
        {
            Dokumenta.frmKontrola kontrola = new Dokumenta.frmKontrola();
            PravoP = kontrola.Pravo;
            if (PravoP == true) { kontrola.Show(); } else { return; }
        }
        private void toolStripButton87_Click(object sender, EventArgs e)
        {
            Sifarnici.frmSifarnikKontrolnihTipovaDokumenta kgtp = new Sifarnici.frmSifarnikKontrolnihTipovaDokumenta();
            PravoP = kgtp.Pravo;
            if (PravoP == true) { kgtp.Show(); } else { return; }
        }
        private void toolStripButton88_Click(object sender, EventArgs e)
        {
            Dokumenta.frmCentralnaTablaRN cen = new Dokumenta.frmCentralnaTablaRN();
            PravoP = cen.Pravo;
            if (PravoP == true) { cen.Show(); } else { return; }
        }
        private void toolStripButton91_Click(object sender, EventArgs e)
        {
            Servis.frmPregledPopisa1 pp = new Servis.frmPregledPopisa1();
            PravoP = pp.Pravo;
            if (PravoP == true) { pp.Show(); } else { return; }
        }
        private void toolStripButton92_Click(object sender, EventArgs e)
        {
            SyncForm.frmPregledPopisa2 ppp = new SyncForm.frmPregledPopisa2();
            PravoP = ppp.Pravo;
            if (PravoP == true) { ppp.Show(); } else { return; }
        }

        private void toolStripButton93_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPredjenaKilometrazaLokomotiva predjKM = new Dokumenta.frmPredjenaKilometrazaLokomotiva();
            PravoP = predjKM.Pravo;
            if (PravoP == true) { predjKM.Show(); } else { return; }
        }

        private void toolStripButton94_Click(object sender, EventArgs e)
        {
            Dokumenta.frmCentralnaTablaCpajal cpajak = new Dokumenta.frmCentralnaTablaCpajal();
            PravoP = cpajak.Pravo;
            if (PravoP == true) { cpajak.Show(); } else { return; }
        }

        private void toolStripButton95_Click(object sender, EventArgs e)
        {
            Servis.frmGrupaKvarovaAuto gka = new Servis.frmGrupaKvarovaAuto();
            PravoP = gka.Pravo;
            if (PravoP == true) { gka.Show(); } else { return; }
        }

        private void toolStripButton96_Click(object sender, EventArgs e)
        {
            Servis.frmKvaroviAuto kvaut = new Servis.frmKvaroviAuto();
            PravoP = kvaut.Pravo;
            if (PravoP == true) { kvaut.Show(); } else { return; }
        }

        private void toolStripButton99_Click(object sender, EventArgs e)
        {
            Dokumenta.frmEvidencijaRadaPromene prom = new Dokumenta.frmEvidencijaRadaPromene();
            PravoP = prom.Pravo;
            if (PravoP == true) { prom.Show(); } else { return; }
        }

        private void toolStripButton100_Click(object sender, EventArgs e)
        {
            Dokumenta.frmBolovanja bol = new Dokumenta.frmBolovanja();
            PravoP = bol.Pravo;
            if (PravoP == true) { bol.Show(); } else { return; }
        }

        private void toolStripButton101_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPrekovremeniRad prekrad = new Dokumenta.frmPrekovremeniRad();
            PravoP = prekrad.Pravo;
            if (PravoP == true) { prekrad.Show(); } else { return; }
        }

        private void toolStripButton103_Click(object sender, EventArgs e)
        {
            Dokumenta.frmObracunFiksni fiksniobr = new Dokumenta.frmObracunFiksni();
            PravoP = fiksniobr.Pravo;
            if (PravoP == true) { fiksniobr.Show(); } else { return; }
        }

        private void toolStripButton104_Click(object sender, EventArgs e)
        {
            Testiranje.Pitanja pitanja = new Testiranje.Pitanja();
            PravoP = pitanja.Pravo;
            if (PravoP == true) { pitanja.Show(); } else { return; }
        }

        private void ToolStripButton105_Click(object sender, EventArgs e)
        {
            Testiranje.frmGenerisanjeTestaKorisnik generisanje = new Testiranje.frmGenerisanjeTestaKorisnik();
            PravoP = generisanje.Pravo;
            if (PravoP == true) { generisanje.Show(); } else { return; }
        }

        private void toolStripButton106_Click(object sender, EventArgs e)
        {
            Testiranje.TestiranjeStampa tesS = new Testiranje.TestiranjeStampa();
            PravoP = tesS.Pravo;
            if (PravoP == true) { tesS.Show(); } else { return; }
        }

        private void toolStripButton107_Click(object sender, EventArgs e)
        {
            Testiranje.Obrasci tes6 = new Testiranje.Obrasci();
            PravoP = tes6.Pravo;
            if (PravoP == true) { tes6.Show(); } else { return; }
        }

        private void toolStripButton108_Click(object sender, EventArgs e)
        {
            Dokumenta.frmAutomobili auto = new Dokumenta.frmAutomobili();
            PravoP = auto.Pravo;
            if (PravoP == true) { auto.Show(); } else { return; }
        }

        private void toolStripButton109_Click(object sender, EventArgs e)
        {
            Dokumenta.frmAutomobiliPregledPrijava prijaveautomobili = new Dokumenta.frmAutomobiliPregledPrijava();
            PravoP = prijaveautomobili.Pravo;
            if (PravoP == true) { prijaveautomobili.Show(); } else { return; }
        }

        private void toolStripButton97_Click(object sender, EventArgs e)
        {
            Dokumenta.frmAutomobiliKvarovi kvarovi = new Dokumenta.frmAutomobiliKvarovi();
            PravoP = kvarovi.Pravo;
            if (PravoP == true) { kvarovi.Show(); } else { return; }
        }

        private void toolStripButton110_Click(object sender, EventArgs e)
        {
            Servis.frmPrijavaKvaraAuto prijavaKvarAuto = new Servis.frmPrijavaKvaraAuto();
            PravoP = prijavaKvarAuto.Pravo;
            if (PravoP == true) { prijavaKvarAuto.Show(); } else { return; }
        }

        private void toolStripButton111_Click(object sender, EventArgs e)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            myConnection.Open();
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "sp_AzuriranjeOdmora";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;
            SqlTransaction myTransaction = myConnection.BeginTransaction();
            myCommand.Transaction = myTransaction;
            myCommand.ExecuteNonQuery();
            myConnection.Close();
            MessageBox.Show("Obrada zavrsena");
/*
            myConnection.Open();
            SqlTransaction myTransaction = myConnection.BeginTransaction();
            myCommand.Transaction = myTransaction;
            bool error = true;
            try
            {
                myCommand.ExecuteNonQuery();
                myTransaction.Commit();
                myTransaction = myConnection.BeginTransaction();
                myCommand.Transaction = myTransaction;
            }
*/

        }

        private void toolStripButton112_Click(object sender, EventArgs e)
        {
            Administracija.frmKreirajGrupu kreirajGrupu = new Administracija.frmKreirajGrupu();
            PravoP = kreirajGrupu.Pravo;
            if (PravoP == true) { kreirajGrupu.Show(); } else { return; }
        }

        private void toolStripButton113_Click(object sender, EventArgs e)
        {
            Administracija.frmDodeliGrupu dodeliGrupu = new Administracija.frmDodeliGrupu();
            PravoP = dodeliGrupu.Pravo;
            if (PravoP == true) { dodeliGrupu.Show(); } else { return; }
        }

        private void toolStripButton114_Click(object sender, EventArgs e)
        {
            Administracija.frmForme forme = new Administracija.frmForme();
            PravoP = forme.Pravo;
            if (PravoP == true) { forme.Show(); } else { return; }
        }

        private void toolStripButton115_Click(object sender, EventArgs e)
        {
            Administracija.frmFormePrava formePrava = new Administracija.frmFormePrava();
            formePrava.Show();
        }

        private void toolStripButton26_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton32_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton64_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton66_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton47_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton98_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton116_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPropratnice propratnice = new Dokumenta.frmPropratnice();
            propratnice.Show();
        }
    }
}
