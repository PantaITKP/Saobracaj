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
            stan.Show();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Sifarnici.frmTrase trase = new Sifarnici.frmTrase();
            trase.Show();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            Sifarnici.frmPruge prug = new Sifarnici.frmPruge();
            prug.Show();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            Sifarnici.frmLokomotive lokomotive = new Sifarnici.frmLokomotive();
            lokomotive.Show();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            Sifarnici.frmSerijeLokomotiva serije = new Sifarnici.frmSerijeLokomotiva();
            serije.Show();
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            Sifarnici.frmStatusVoza statvoz = new Sifarnici.frmStatusVoza();
            statvoz.Show();
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            Sifarnici.frmRazlozi raz = new Sifarnici.frmRazlozi();
            raz.Show();
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            Sifarnici.frmTipPrevoza tipP = new Sifarnici.frmTipPrevoza();
            tipP.Show();
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            Sifarnici.frmNHM nhm = new Sifarnici.frmNHM();
            nhm.Show();
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            Sifarnici.frmOsoblje osob = new Sifarnici.frmOsoblje();
            osob.Show();
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            Sifarnici.frmPartnerji part = new Sifarnici.frmPartnerji();
            part.Show();
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            Sifarnici.frmTipTelegrama ttel = new Sifarnici.frmTipTelegrama();
            ttel.Show();
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            Sifarnici.frmTelegrami tel = new Sifarnici.frmTelegrami();
            tel.Show();
        }
        //Priprema
        private void toolStripButton15_Click(object sender, EventArgs e)
        {
            Dokumenta.frmNajava frmNaj = new Dokumenta.frmNajava(Korisnik, 0);
            frmNaj.Show();
        }

        private void toolStripButton16_Click(object sender, EventArgs e)
        {
            Dokumenta.frmNajava frmNaj = new Dokumenta.frmNajava(Korisnik, 1);
            frmNaj.Show();
        }

        private void toolStripButton17_Click(object sender, EventArgs e)
        {
            Dokumenta.frmNajava20 naj = new Dokumenta.frmNajava20();
            naj.Show();
        }

        private void toolStripButton18_Click(object sender, EventArgs e)
        {
            SyncForm.frmNajavaArhivaAnaliza sNA = new SyncForm.frmNajavaArhivaAnaliza();
            sNA.Show();
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            SyncForm.frmAnalizaPorudzbina apor = new SyncForm.frmAnalizaPorudzbina();
            apor.Show();
        }

        private void toolStripButton19_Click(object sender, EventArgs e)
        {
            Dokumenta.frmTeretnicePregled frmTer = new Dokumenta.frmTeretnicePregled(Korisnik);
            frmTer.Show();
        }

        private void toolStripButton20_Click(object sender, EventArgs e)
        {
            Dokumenta.frmIskljuceniVagoni iv = new Dokumenta.frmIskljuceniVagoni();
            iv.Show();
        }

        private void toolStripButton21_Click(object sender, EventArgs e)
        {
            Dokumenta.frmNajaveBezTeretnice nbt = new Dokumenta.frmNajaveBezTeretnice();
            nbt.Show();
        }

        private void toolStripButton22_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPregledRID prid = new Dokumenta.frmPregledRID();
            prid.Show();
        }

        private void toolStripButton23_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRIDPoNajavama rpn = new Dokumenta.frmRIDPoNajavama();
            rpn.Show();
        }

        private void toolStripButton24_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRIDTeretnice fridter = new Dokumenta.frmRIDTeretnice();
            fridter.Show();
        }

        private void toolStripButton25_Click(object sender, EventArgs e)
        {
            Mobile.frmZavrsnaDokumentacija zdok = new Mobile.frmZavrsnaDokumentacija();
            zdok.Show();
        }

        private void toolStripButton27_Click(object sender, EventArgs e)
        {
            Dokumenta.frmTest test = new Dokumenta.frmTest();
            test.Show();
        }

        private void toolStripButton28_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPregledRN prn = new Dokumenta.frmPregledRN();
            prn.Show();
        }

        private void toolStripButton29_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRadniNalogPregled frmp2 = new Dokumenta.frmRadniNalogPregled();
            frmp2.Show();
            //Ovde imam duplo
            //  Dokumenta.frmRadniNalogPregled frmRN = new Dokumenta.frmRadniNalogPregled();
            //  frmRN.Show();
        }

        private void toolStripButton30_Click(object sender, EventArgs e)
        {

            Dokumenta.frmRAdniNalogPregledPoLokomotivama rnpl = new Dokumenta.frmRAdniNalogPregledPoLokomotivama();
            rnpl.Show();
        }

        private void toolStripButton31_Click(object sender, EventArgs e)
        {
            Dokumenta.frmStampaRadnogNaloga srn = new Dokumenta.frmStampaRadnogNaloga();
            srn.Show();
        }

        private void toolStripButton33_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRaspustiVagone frv = new Dokumenta.frmRaspustiVagone();
            frv.Show();
        }

        private void toolStripButton34_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPronadjiVagon pv = new Dokumenta.frmPronadjiVagon();
            pv.Show();

        }

        private void toolStripButton38_Click(object sender, EventArgs e)
        {
            Dokumenta.frmMUPDozvola Dozvola = new Dokumenta.frmMUPDozvola();
            Dozvola.Show();
        }

        private void toolStripButton37_Click(object sender, EventArgs e)
        {
            Dokumenta.frmMupMesto mesto = new Dokumenta.frmMupMesto();
            mesto.Show();
        }

        private void toolStripButton35_Click(object sender, EventArgs e)
        {
            Administracija.frmKorisnici kor = new Administracija.frmKorisnici();
            kor.Show();
        }

        private void toolStripButton36_Click(object sender, EventArgs e)
        {
            Dokumenta.frmAutomobili autom = new Dokumenta.frmAutomobili();
            autom.Show();
        }

        private void toolStripButton39_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRegistator reg = new Dokumenta.frmRegistator();
            reg.Show();
        }

        private void toolStripButton40_Click(object sender, EventArgs e)
        {
            Dokumenta.frmRegistratorPregled regpre = new Dokumenta.frmRegistratorPregled();
            regpre.Show();
        }

        private void toolStripButton41_Click(object sender, EventArgs e)
        {
            Servis.frmVrstePopisa vrPopisa = new Servis.frmVrstePopisa();
            vrPopisa.Show();
        }

        private void toolStripButton42_Click(object sender, EventArgs e)
        {
            Servis.frmLokomotivaVrstaPopisa lokvp = new Servis.frmLokomotivaVrstaPopisa();
            lokvp.Show();
        }

        private void toolStripButton43_Click(object sender, EventArgs e)
        {
            Servis.frmGrupaKvarova gkv = new Servis.frmGrupaKvarova();
            gkv.Show();
        }

        private void toolStripButton44_Click(object sender, EventArgs e)
        {
            Servis.frmKvarovi kv = new Servis.frmKvarovi();
            kv.Show();
        }

        private void toolStripButton45_Click(object sender, EventArgs e)
        {
            Servis.frmEvidencijaKvarova pkvar = new Servis.frmEvidencijaKvarova();
            pkvar.Show();
        }

        private void toolStripButton46_Click(object sender, EventArgs e)
        {
            Servis.frmEvidencijaKvarovaAnaliza evkva = new Servis.frmEvidencijaKvarovaAnaliza();
            evkva.Show();
        }

        private void toolStripButton50_Click(object sender, EventArgs e)
        {
            Administracija.frmSistematizacija sistem = new Administracija.frmSistematizacija();
            sistem.Show();
        }

        private void toolStripButton51_Click(object sender, EventArgs e)
        {
            Administracija.frmSistematizacijaPovezivanje sist = new Administracija.frmSistematizacijaPovezivanje();
            sist.Show();
        }

        private void toolStripButton52_Click(object sender, EventArgs e)
        {
            Sifarnici.frmVrsteAktivnosti vrakt = new Sifarnici.frmVrsteAktivnosti();
            if (Korisnik == "admin")
            {
                vrakt.Show();
            }
        }

        private void toolStripButton53_Click(object sender, EventArgs e)
        {
            Sifarnici.frmCenaPoRadniku cpr = new Sifarnici.frmCenaPoRadniku();
            if (Korisnik == "admin")
            {
                cpr.Show();
            }
        }

        private void toolStripButton54_Click(object sender, EventArgs e)
        {

            Dokumenta.frmPravoAktivnosti pravo = new Dokumenta.frmPravoAktivnosti();
            pravo.Show();
        }

        private void toolStripButton55_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPravoAktivnostiViseRadnika pravovise = new Dokumenta.frmPravoAktivnostiViseRadnika();
            pravovise.Show();
        }

        private void toolStripButton72_Click(object sender, EventArgs e)
        {
            Dokumenta.frmIzracunZarada fiz = new Dokumenta.frmIzracunZarada();
            fiz.Show();
        }

        private void toolStripButton73_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPlacenoNeplaceno pln = new Dokumenta.frmPlacenoNeplaceno();
            pln.Show();
        }

        private void toolStripButton74_Click(object sender, EventArgs e)
        {
            Dokumenta.frmOsnovnaZarada osnzar = new Dokumenta.frmOsnovnaZarada();
            osnzar.Show();



        }

        private void toolStripButton69_Click(object sender, EventArgs e)
        {
            Dokumenta.frmBrojSmena brsm = new Dokumenta.frmBrojSmena();
            brsm.Show();
        }

        private void toolStripButton57_Click(object sender, EventArgs e)
        {
            Dokumenta.frmEvidencijaGodišnjihOdmora evgo = new Dokumenta.frmEvidencijaGodišnjihOdmora();
            evgo.Show();
        }

        private void toolStripButton60_Click(object sender, EventArgs e)
        {
            Mobile.frmPrijavaSmene prodj = new Mobile.frmPrijavaSmene();
            prodj.Show();
        }

        private void toolStripButton61_Click(object sender, EventArgs e)
        {
            SyncForm.frmPregledMobilni drugi = new SyncForm.frmPregledMobilni();
            drugi.Show();
        }

        private void toolStripButton62_Click(object sender, EventArgs e)
        {
            Servis.frmPrijavaMasinovodje mas = new Servis.frmPrijavaMasinovodje();
            mas.Show();
        }

        private void toolStripButton63_Click(object sender, EventArgs e)
        {
            SyncForm.frmPregledMasinovodje novi = new SyncForm.frmPregledMasinovodje();
            novi.Show();
        }

        private void toolStripButton48_Click(object sender, EventArgs e)
        {
            Servis.frmNamirenja namir = new Servis.frmNamirenja();
            namir.Show();
        }

        private void toolStripButton49_Click(object sender, EventArgs e)
        {
            Servis.frmNamirenjaSumarno namSum = new Servis.frmNamirenjaSumarno();
            namSum.Show();
        }

        private void toolStripButton58_Click(object sender, EventArgs e)
        {
            Mobile.frmAnalizaGO ago = new Mobile.frmAnalizaGO();
            ago.Show();
        }

        private void toolStripButton59_Click(object sender, EventArgs e)
        {
            Mobile.frmAnalizaGOSum GoSum = new Mobile.frmAnalizaGOSum();
            GoSum.Show();
        }

        private void toolStripButton56_Click(object sender, EventArgs e)
        {
            Mobile.frmSlobodniDani slob = new Mobile.frmSlobodniDani(Korisnik);
            slob.Show();
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
            fpn.Show();
        }

        private void toolStripButton79_Click(object sender, EventArgs e)
        {
            Dokumenta.frmEvidencijaRAdaNeplaceno nepl = new Dokumenta.frmEvidencijaRAdaNeplaceno();
            nepl.Show();
        }

        private void toolStripButton80_Click(object sender, EventArgs e)
        {
            Dokumenta.frmFinansije novak = new Dokumenta.frmFinansije();
            novak.Show();
        }

        private void toolStripButton65_Click(object sender, EventArgs e)
        {
            SyncForm.frmPlanRada planrada = new SyncForm.frmPlanRada();
            planrada.Show();
        }

        private void toolStripButton67_Click(object sender, EventArgs e)
        {
            Dokumenta.frmEvidencijaRada erM = new Dokumenta.frmEvidencijaRada(Korisnik);
            erM.Show();
        }

        private void toolStripButton68_Click(object sender, EventArgs e)
        {

            Dokumenta.frmZarade zar = new Dokumenta.frmZarade();
            if (Korisnik == "admin")
            {
                zar.Show();
            }
        }
        private void toolStripButton70_Click(object sender, EventArgs e)
        {
            Dokumenta.frmEvidencijaRadaZaglavlje erz = new Dokumenta.frmEvidencijaRadaZaglavlje(Korisnik);
            erz.Show();
        }

        private void toolStripButton71_Click(object sender, EventArgs e)
        {
            SyncForm.frmAnalizaAktivnosti aAktiv = new SyncForm.frmAnalizaAktivnosti();
            aAktiv.Show();
        }
        private void toolStripButton75_Click(object sender, EventArgs e)
        {
            Dokumenta.frmParametriObracuna parobr = new Dokumenta.frmParametriObracuna();
            parobr.Show();
        }
        private void toolStripButton76_Click(object sender, EventArgs e)
        {
            Dokumenta.frmZarade zar = new Dokumenta.frmZarade();
            if (Korisnik == "admin")
            {
                zar.Show();
            }
        }
        private void toolStripButton81_Click(object sender, EventArgs e)
        {
            Tehnologija.frmTehnologija teh = new Tehnologija.frmTehnologija(Korisnik);
            teh.Show();
        }
        private void toolStripButton82_Click(object sender, EventArgs e)
        {
            Sifarnici.frmTraseAnaliticki ta = new Sifarnici.frmTraseAnaliticki();
            ta.Show();
        }
        private void toolStripButton83_Click(object sender, EventArgs e)
        {
            Tehnologija.frmTehnologijaPregled tehp = new Tehnologija.frmTehnologijaPregled(Korisnik);
            tehp.Show();
        }
        private void toolStripButton84_Click(object sender, EventArgs e)
        {
            Servis.frmPrijavaKvara pkv = new Servis.frmPrijavaKvara();
            pkv.Show();
        }
        private void RibbonPanel_Click(object sender, EventArgs e)
        {
            //  Sifarnici.frmSifarnikKontrolnihGresaka skg = new Sifarnici.frmSifarnikKontrolnihGresaka();
            // skg.Show();
        }
        private void toolStripButton85_Click(object sender, EventArgs e)
        {
            Sifarnici.frmSifarnikKontrolnihGresaka skg = new Sifarnici.frmSifarnikKontrolnihGresaka();
            skg.Show();
        }
        private void toolStripButton86_Click(object sender, EventArgs e)
        {
            Dokumenta.frmKontrola kontrola = new Dokumenta.frmKontrola();
            kontrola.Show();
        }
        private void toolStripButton87_Click(object sender, EventArgs e)
        {
            Sifarnici.frmSifarnikKontrolnihTipovaDokumenta kgtp = new Sifarnici.frmSifarnikKontrolnihTipovaDokumenta();
            kgtp.Show();
        }
        private void toolStripButton88_Click(object sender, EventArgs e)
        {
            Dokumenta.frmCentralnaTablaRN cen = new Dokumenta.frmCentralnaTablaRN();
            cen.Show();
        }
        private void toolStripButton91_Click(object sender, EventArgs e)
        {
            Servis.frmPregledPopisa1 pp = new Servis.frmPregledPopisa1();
            pp.Show();
        }
        private void toolStripButton92_Click(object sender, EventArgs e)
        {
            SyncForm.frmPregledPopisa2 ppp = new SyncForm.frmPregledPopisa2();
            ppp.Show();
        }

        private void toolStripButton93_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPredjenaKilometrazaLokomotiva predjKM = new Dokumenta.frmPredjenaKilometrazaLokomotiva();
            predjKM.Show();
        }

        private void toolStripButton94_Click(object sender, EventArgs e)
        {
            Dokumenta.frmCentralnaTablaCpajal cpajak = new Dokumenta.frmCentralnaTablaCpajal();
            cpajak.Show();
        }

        private void toolStripButton95_Click(object sender, EventArgs e)
        {
            Servis.frmGrupaKvarovaAuto gka = new Servis.frmGrupaKvarovaAuto();
            gka.Show();
        }

        private void toolStripButton96_Click(object sender, EventArgs e)
        {
            Servis.frmKvaroviAuto kvaut = new Servis.frmKvaroviAuto();
            kvaut.Show();
        }

        private void toolStripButton99_Click(object sender, EventArgs e)
        {
            Dokumenta.frmEvidencijaRadaPromene prom = new Dokumenta.frmEvidencijaRadaPromene();
            prom.Show();
        }

        private void toolStripButton100_Click(object sender, EventArgs e)
        {
            Dokumenta.frmBolovanja bol = new Dokumenta.frmBolovanja();
            bol.Show();
        }

        private void toolStripButton101_Click(object sender, EventArgs e)
        {
            Dokumenta.frmPrekovremeniRad prekrad = new Dokumenta.frmPrekovremeniRad();
            prekrad.Show();
        }

        private void toolStripButton103_Click(object sender, EventArgs e)
        {
            Dokumenta.frmObracunFiksni fiksniobr = new Dokumenta.frmObracunFiksni();
            fiksniobr.Show();
        }

        private void toolStripButton104_Click(object sender, EventArgs e)
        {
            Testiranje.Pitanja pitanja = new Testiranje.Pitanja();
            pitanja.Show();
        }

        private void ToolStripButton105_Click(object sender, EventArgs e)
        {
            Testiranje.frmGenerisanjeTestaKorisnik generisanje = new Testiranje.frmGenerisanjeTestaKorisnik();
            generisanje.Show();
        }

        private void toolStripButton106_Click(object sender, EventArgs e)
        {
            Testiranje.TestiranjeStampa tesS = new Testiranje.TestiranjeStampa();
            tesS.Show();
        }

        private void toolStripButton107_Click(object sender, EventArgs e)
        {
            Testiranje.Obrasci tes6 = new Testiranje.Obrasci();
            tes6.Show();
        }

        private void toolStripButton108_Click(object sender, EventArgs e)
        {
            Dokumenta.frmAutomobili auto = new Dokumenta.frmAutomobili();
            auto.Show();
        }

        private void toolStripButton109_Click(object sender, EventArgs e)
        {
            Dokumenta.frmAutomobiliPregledPrijava prijaveautomobili = new Dokumenta.frmAutomobiliPregledPrijava();
            prijaveautomobili.Show();
        }

        private void toolStripButton97_Click(object sender, EventArgs e)
        {
            Dokumenta.frmAutomobiliKvarovi kvarovi = new Dokumenta.frmAutomobiliKvarovi();
            kvarovi.Show();
        }

        private void toolStripButton110_Click(object sender, EventArgs e)
        {
            Servis.frmPrijavaKvaraAuto prijavaKvarAuto = new Servis.frmPrijavaKvaraAuto();
            prijavaKvarAuto.Show();
        }

        private void toolStripButton111_Click(object sender, EventArgs e)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "sp_AzuriranjeOdmora";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;
            myCommand.ExecuteNonQuery();
        }

        private void toolStripButton116_Click(object sender, EventArgs e)
        {

            Administracija.frmForme forme = new Administracija.frmForme();
            forme.Show();

        }
        private void toolStripButton114_Click(object sender, EventArgs e)
        {
            Administracija.frmKreirajGrupu kreirajGrupu = new Administracija.frmKreirajGrupu();
            kreirajGrupu.Show();
        }

        private void toolStripButton115_Click(object sender, EventArgs e)
        {
            Administracija.frmDodeliGrupu dodeliGrupu = new Administracija.frmDodeliGrupu();
            dodeliGrupu.Show();
        }



        private void toolStripButton117_Click(object sender, EventArgs e)
        {
            Administracija.frmFormePrava formePrava = new Administracija.frmFormePrava();
            formePrava.Show();
        }

        private void MainP_Load(object sender, EventArgs e)
        {

        }
    }
}
