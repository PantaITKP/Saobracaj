using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Dokumenta
{
    public partial class frmMapa : Form
    {
        public string connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        GMapControl map = new GMapControl();
        GMapOverlay overlay = new GMapOverlay("marker");
        GMapOverlay polyOvelray = new GMapOverlay("polygons");
        GMapPolygon polygon;
        //GMapOverlay routes = new GMapOverlay("routes");
        GMarkerGoogle marker2;
        double lat;
        double lng;
        double lngS;
        double latS;
        public static int stanicaMarker;
        public static int partner;
        public static int najava;

        public static string code = "frmTest";
        public bool Pravo;
        int idGrupe;
        int idForme;
        bool insert;
        bool update;
        bool delete;
        string Kor = Sifarnici.frmLogovanje.user.ToString();
        string niz = "";
        public frmMapa()
        {
            InitializeComponent();
            /*IdGrupe();
            IdForme();
            PravoPristupa();*/


            LoadMap();
        }
        /*public string IdGrupe()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            //Sifarnici.frmLogovanje frm = new Sifarnici.frmLogovanje();         
            string query = "Select IdGrupe from KorisnikGrupa Where Korisnik = " + "'" + Kor.TrimEnd() + "'";
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            int count = 0;

            while (dr.Read())
            {
                if (count == 0)
                {
                    niz = dr["IdGrupe"].ToString();
                    count++;
                }
                else
                {
                    niz = niz + "," + dr["IdGrupe"].ToString();
                    count++;
                }

            }
            conn.Close();
            return niz;
        }
        private int IdForme()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            string query = "Select IdForme from Forme where Rtrim(Code)=" + "'" + code + "'";
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);

            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                idForme = Convert.ToInt32(dr["IdForme"].ToString());
            }
            conn.Close();
            return idForme;
        }

        private void PravoPristupa()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            string query = "Select * From GrupeForme Where IdGrupe in (" + niz + ") and IdForme=" + idForme;
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows == false)
            {
                MessageBox.Show("Nemate prava za pristup ovoj formi", code);
                Pravo = false;
            }
            else
            {
                Pravo = true;
                while (reader.Read())
                {
                    insert = Convert.ToBoolean(reader["Upis"]);
                    if (insert == false)
                    {
                        //tsNew.Enabled = false;
                    }
                    update = Convert.ToBoolean(reader["Izmena"]);
                    if (update == false)
                    {
                        //tsSave.Enabled = false;
                    }
                    delete = Convert.ToBoolean(reader["Brisanje"]);
                    if (delete == false)
                    {
                        //tsDelete.Enabled = false;
                    }
                }
            }

            conn.Close();
        }*/
        private void frmMapa_Load(object sender, EventArgs e)
        {
            LoadNajave();
        }
        private void LoadMap()
        {
            FillCombo();

            map.MapProvider = GMapProviders.BingMap;
            map.Position = new PointLatLng(44.65486, 20.20017);
            map.MinZoom = 3;
            map.MaxZoom = 20;
            map.Zoom = 8;
            map.DragButton = MouseButtons.Left;
            map.Dock = DockStyle.Fill;
            map.ShowCenter = false;
            this.Controls.Add(map);
        }
        //Sve otpravne stanice bi trebalo da bude, trenutno su sve granicne zato sto se ne upisuju ispravni podaci
        //Sve najave koje nisu u statusu 7 i 9 i cije stanice imaju upisane koordinate
        private void LoadNajave()
        {
            int brKola;
            double tezina;
            double neto;
            string opis;
            int id;
            SqlConnection conn = new SqlConnection(connect);
            conn.Open();
            var query = "select distinct Najava.Granicna as Granicna,Stanice.Opis as Opis,Stanice.Longitude,Stanice.Latitude,Sum(BrojKola) as BrojKola , Sum(Tezina) as Tezina," +
                "Sum(NetoTezinaM) as NetoTezinaM " +
                "from Najava " +
                "inner join stanice on Stanice.id=Najava.Granicna " +
                "where Status <>7 and Status <>9 and Longitude <>0 and latitude <>0 " +
                "group by Najava.Granicna,Stanice.Opis,Stanice.Longitude,Stanice.Latitude";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            
            while (dr.Read())
            {
                id = Convert.ToInt32(dr["Granicna"].ToString());
                opis = dr["Opis"].ToString().TrimEnd();
                lat = Convert.ToDouble(dr["Latitude"].ToString());
                lng = Convert.ToDouble(dr["Longitude"].ToString());
                brKola = Convert.ToInt32(dr["BrojKola"].ToString());
                tezina = Convert.ToDouble(dr["Tezina"].ToString());
                neto = Convert.ToDouble(dr["NetoTezinaM"].ToString());

                string text = opis + " \nBroj Kola: " + brKola.ToString() + "\nTezina: " + tezina.ToString() + "\nNeto: " + neto.ToString();
                GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(lng, lat), GMarkerGoogleType.red_dot);
                marker.ToolTipMode = MarkerTooltipMode.Always;
                marker.ToolTipText = text;
                marker.ToolTip.Foreground = Brushes.Black;
                marker.Tag = id;
                Brush brush = new SolidBrush(Color.WhiteSmoke);
                Font f = new Font("Arial", 9, FontStyle.Bold);
                marker.ToolTip.Font = f;
                marker.ToolTip.Fill = brush;
                overlay.Markers.Add(marker);
                map.Overlays.Add(overlay);
            }
            map.OnMarkerClick += new MarkerClick(map_OnMarkerClick);

            conn.Close();
        }
        private void map_OnMarkerClick(GMapMarker marker, MouseEventArgs e)
        {
            /*overlay.Clear();

            marker.ToolTipMode = MarkerTooltipMode.Always;
            overlay.Markers.Add(marker);
            var position = marker.Position;
            double ln = position.Lng;
            double lt = position.Lat;
            var pocetna = marker.Tag;

            SqlConnection conn = new SqlConnection(connect);
            conn.Open();
            var query = "Select Najava.Id as ID,Najava.uputna as Uputna,stanice.Opis as Opis, stanice.Longitude as Longitude, stanice.Latitude as Latitude, Najava.Status as Status," +
                "BrojKola, Tezina,NetoTezinaM " +
                "From Najava " +
                "Inner join stanice on stanice.ID=Najava.Uputna Where Najava.Granicna=" + Convert.ToInt32(pocetna.ToString()) + "and Najava.Status<>7 and Najava.Status<>9 " +
                "and stanice.Longitude<>0 and stanice.Latitude<>0";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            string stanica;
            int status;
            int brKola;
            double tezina;
            double neto;
            stanicaMarker = Convert.ToInt32(pocetna.ToString());
            while (dr.Read())
            {
                stanica = dr["Opis"].ToString().TrimEnd();
                lngS = Convert.ToDouble(dr["Longitude"].ToString());
                latS = Convert.ToDouble(dr["Latitude"].ToString());
                status = Convert.ToInt32(dr["Status"].ToString());
                brKola = Convert.ToInt32(dr["BrojKola"].ToString());
                tezina = Convert.ToDouble(dr["Tezina"].ToString());
                neto = Convert.ToDouble(dr["NetoTezinaM"].ToString());

                string text = stanica + " \nBroj Kola: " + brKola.ToString() + "\nTezina: " + tezina.ToString() + "\nNeto: " + neto.ToString();

                marker2 = new GMarkerGoogle(new PointLatLng(lngS, latS), GMarkerGoogleType.blue_pushpin);
                marker2.ToolTipMode = MarkerTooltipMode.Always;
                marker2.ToolTipText = text;
                Brush brush = new SolidBrush(Color.WhiteSmoke);
                Font f = new Font("Arial", 9, FontStyle.Bold);
                marker2.ToolTip.Font = f;
                marker2.ToolTip.Fill = brush;
                overlay.Markers.Add(marker2);

                for (int i = 0; i < dr.FieldCount; i++)
                {
                    List<PointLatLng> points = new List<PointLatLng>();
                    points.Add(new PointLatLng(lt, ln));
                    points.Add(new PointLatLng(lngS, latS));
                    polygon = new GMapPolygon(points, "mypoligon");
                    polygon.Stroke.Width = 2;
                    polyOvelray.Polygons.Add(polygon);
                }
            }
            map.Overlays.Add(polyOvelray);
            map.Overlays.Add(overlay);

            conn.Close();*/
        } 
        private void btn_SveStanice_Click(object sender, EventArgs e)
        {
            this.Close();
            frmMapa m = new frmMapa();
            m.Show();
        }
        private void FillCombo()
        {
            SqlConnection conn = new SqlConnection(connect);
            var queryNajava = "select ID,RTrim(PrevozniPut) as PrevozniPut from Najava Where Status<>7 and Status<>9 order by ID desc";
            var adapterNajava = new SqlDataAdapter(queryNajava, conn);
            var setNajava = new DataSet();
            adapterNajava.Fill(setNajava);
            combo_Najave.DataSource = setNajava.Tables[0];
            combo_Najave.DisplayMember = "PrevozniPut";
            combo_Najave.ValueMember = "ID";

            var queryPartner = "Select PaSifra, RTrim(PaNaziv) as Partner From Partnerji where Primalac = 1 order By PaNaziv";
            var adapterPartner = new SqlDataAdapter(queryPartner, conn);
            var setPartner = new DataSet();
            adapterPartner.Fill(setPartner);
            combo_Partner.DataSource = setPartner.Tables[0];
            combo_Partner.DisplayMember = "Partner";
            combo_Partner.ValueMember = "PaSifra";

            var queryStatus = "Select distinct Status From Najava order by status asc";
            var adapterStatus = new SqlDataAdapter(queryStatus, conn);
            var setStatus = new DataSet();
            adapterStatus.Fill(setStatus);
            combo_Status.DataSource = setStatus.Tables[0];
            combo_Status.DisplayMember = "Status";
            combo_Status.ValueMember = "Status";

            var queryStanica = "select distinct Najava.Granicna as Granicna,Stanice.Opis as Opis " +
                "from Najava " +
                "inner join stanice on Stanice.id=Najava.Granicna " +
                "where Status <>7 and Status <>9 and Longitude <>0 and latitude <>0  group by Najava.Granicna,Stanice.Opis,Stanice.Longitude,Stanice.Latitude";
            var adapterStanica = new SqlDataAdapter(queryStanica, conn);
            var setStanica = new DataSet();
            adapterStanica.Fill(setStanica);
            combo_Stanica.DataSource = setStanica.Tables[0];
            combo_Stanica.DisplayMember = "Opis";
            combo_Stanica.ValueMember = "Granicna";
        }
        private void btn_ZoomIn_Click(object sender, EventArgs e)
        {
            map.Zoom++;
        }
        private void btn_ZoomOut_Click(object sender, EventArgs e)
        {
            map.Zoom--;
        }
        private void btn_Trasa_Click(object sender, EventArgs e)
        {
            map.Overlays.Clear();
            overlay.Clear();

            SqlConnection conn = new SqlConnection(connect);
            conn.Open();
            najava = Convert.ToInt32(combo_Najave.SelectedValue);
            int Od = 0;
            int Do = 0;
            int status = 0;
            var query = "Select * From Najava Where ID= " + combo_Najave.SelectedValue;
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                Od = Convert.ToInt32(dr["Granicna"].ToString());
                Do = Convert.ToInt32(dr["Uputna"].ToString());
                status = Convert.ToInt32(dr["Status"].ToString());
            }
            conn.Close();

            var query2 = "Select Opis,Longitude,Latitude From Stanice Where ID= " + Od.ToString();
            string od = "";
            double lngOD = 0;
            double latOD = 0;
            conn.Open();
            SqlCommand cmd2 = new SqlCommand(query2, conn);
            SqlDataReader dr2 = cmd2.ExecuteReader();
            while (dr2.Read())
            {
                lngOD = Convert.ToDouble(dr2["Longitude"].ToString());
                latOD = Convert.ToDouble(dr2["Latitude"].ToString());
                od = dr2["Opis"].ToString();
            }
            conn.Close();

            var query3 = "Select Opis,Longitude,Latitude From Stanice Where ID= " + Do.ToString();
            string doo = "";
            double lngDO = 0;
            double latDO = 0;
            conn.Open();
            SqlCommand cmd3 = new SqlCommand(query3, conn);
            SqlDataReader dr3 = cmd3.ExecuteReader();
            while (dr3.Read())
            {
                lngDO = Convert.ToDouble(dr3["Longitude"].ToString());
                latDO = Convert.ToDouble(dr3["Latitude"].ToString());
                doo = dr3["Opis"].ToString();
            }
            conn.Close();

            GMapProviders.BingMap.ClientKey = @"vYzQBrYv7RcjJQFw7tPm~xouelNoiO9OfAnm3N3Crww~Ar2-Rl3OhJCb2JzOGmnKSuQPeX9cZ8X2CMF9rvGYol0A2fufTETcMEtA5bPY7VNk";
            GMarkerGoogle marker2 = new GMarkerGoogle(new PointLatLng(lngOD, latOD), GMarkerGoogleType.green_dot);
            overlay.Markers.Add(marker2);
            marker2.ToolTipMode = MarkerTooltipMode.Always;
            marker2.ToolTipText = "Otpravna: " + od;

            GMarkerGoogle marker3 = new GMarkerGoogle(new PointLatLng(lngDO, latDO), GMarkerGoogleType.blue_dot);
            overlay.Markers.Add(marker3);
            marker3.ToolTipMode = MarkerTooltipMode.Always;
            marker3.ToolTipText = "Uputna: " + doo;

            GMapOverlay polyOverlay = new GMapOverlay("polygons");
            List<PointLatLng> points = new List<PointLatLng>();
            points.Add(new PointLatLng(lngOD, latOD));
            points.Add(new PointLatLng(lngDO, latDO));
            GMapPolygon polygon = new GMapPolygon(points, "mypolygon");

            polygon.Stroke.Width = 3;
            if (status == 1) { polygon.Stroke.Color = Color.Red; }
            else if (status == 2) { polygon.Stroke.Color = Color.Blue; }
            else if (status == 3) { polygon.Stroke.Color = Color.Green; }
            else if (status == 4) { polygon.Stroke.Color = Color.Yellow; }
            else if (status == 5) { polygon.Stroke.Color = Color.Black; }
            else if (status == 6) { polygon.Stroke.Color = Color.White; }
            else if (status == 8) { polygon.Stroke.Color = Color.Gray; }
            else { polygon.Stroke.Color = Color.RosyBrown; }

            polyOverlay.Polygons.Add(polygon);
            map.Overlays.Add(polyOverlay);
            map.Overlays.Add(overlay);

            map.Zoom = 7;
            map.Zoom = 8;
            frmMapaOpis opis = new frmMapaOpis();
            opis.Show();
            opis.Focus();
        }

        private void btn_Partneri_Click(object sender, EventArgs e)
        {
            overlay.Clear();
            overlay.Markers.Clear();
            polyOvelray.Clear();
            polyOvelray.Polygons.Clear();

            SqlConnection conn = new SqlConnection(connect);
            conn.Open();
            var query = "select Najava.ID as ID, Najava.Uputna as Uputna,stanice.Opis as UputnaOpis, stanice.Longitude as UputnaLongitude, stanice.latitude as UputnaLatitude," +
                "najava.Otpravna, stanice_1.Opis as OtpravnaOpis, stanice_1.longitude as OtpravnaLongitude,stanice_1.Latitude as OtpravnaLatitude, Najava.Status as status," +
                "BrojKola, Tezina,NetoTezinaM " +
                "From Najava " +
                "inner Join Partnerji on partnerji.PaSifra=Najava.Platilac " +
                "inner join stanice on stanice.id=najava.Uputna " +
                "inner join stanice as stanice_1 on stanice_1.id=najava.Otpravna " +
                "Where Najava.Platilac=" + combo_Partner.SelectedValue + " and Najava.Status<>7 and Status<>9 and stanice.Longitude<>0 and stanice.Latitude<>0 and stanice_1.Longitude<>0 and stanice_1.Latitude<>0";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.HasRows == false)
            {
                MessageBox.Show("Za izabranog partnera nema aktivnih najava");
            }
            else
            {
                partner = Convert.ToInt32(combo_Partner.SelectedValue);
                double upLng;
                double upLat;
                string upOpis;
                string otOpis;
                double otLng;
                double otLat;
                while (dr.Read())
                {
                    upLng = Convert.ToDouble(dr["UputnaLongitude"].ToString());
                    upLat = Convert.ToDouble(dr["UputnaLatitude"].ToString());
                    upOpis = dr["UputnaOpis"].ToString();
                    otOpis = dr["OtpravnaOpis"].ToString();
                    otLng = Convert.ToDouble(dr["OtpravnaLongitude"].ToString());
                    otLat = Convert.ToDouble(dr["OtpravnaLatitude"].ToString());

                    GMarkerGoogle markerUp = new GMarkerGoogle(new PointLatLng(upLng, upLat), GMarkerGoogleType.red_dot);
                    markerUp.ToolTipMode = MarkerTooltipMode.Always;
                    markerUp.ToolTipText = upOpis;
                    overlay.Markers.Add(markerUp);


                    GMarkerGoogle markerOt = new GMarkerGoogle(new PointLatLng(otLng, otLat), GMarkerGoogleType.blue_dot);
                    markerOt.ToolTipMode = MarkerTooltipMode.Always;
                    markerOt.ToolTipText = otOpis;
                    overlay.Markers.Add(markerOt);

                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        List<PointLatLng> points = new List<PointLatLng>();
                        points.Add(new PointLatLng(upLng, upLat));
                        points.Add(new PointLatLng(otLng, otLat));
                        polygon = new GMapPolygon(points, "mypoligon");
                        polygon.Stroke.Color = Color.DarkOrange;
                        polygon.Stroke.Width = 3;
                        polyOvelray.Polygons.Add(polygon);
                    }
                }
                frmMapaOpis mapa = new frmMapaOpis();
                mapa.Show();
            }
            
            map.Overlays.Add(polyOvelray);
            map.Overlays.Add(overlay);
            map.Zoom = 7;
            map.Zoom = 8;

        }

        private void btn_Status_Click(object sender, EventArgs e)
        {
            overlay.Clear();
            overlay.Markers.Clear();
            polyOvelray.Clear();
            polyOvelray.Polygons.Clear();

            SqlConnection conn = new SqlConnection(connect);
            conn.Open();
            var query = "select Najava.ID as ID, Najava.Uputna as Uputna,stanice.Opis as UputnaOpis, stanice.Longitude as UputnaLongitude, stanice.latitude as UputnaLatitude," +
                "najava.Otpravna, stanice_1.Opis as OtpravnaOpis, stanice_1.longitude as OtpravnaLongitude,stanice_1.Latitude as OtpravnaLatitude, Najava.Status as status," +
                "BrojKola, Tezina,NetoTezinaM " +
                "From Najava " +
                "inner Join Partnerji on partnerji.PaSifra=Najava.Platilac " +
                "inner join stanice on stanice.id=najava.Uputna " +
                "inner join stanice as stanice_1 on stanice_1.id=najava.Otpravna " +
                "Where Najava.Status=" + combo_Status.SelectedValue + " and stanice.Longitude<>0 and stanice.Latitude<>0 and stanice_1.Longitude<>0 and stanice_1.Latitude<>0";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();

            double upLng;
            double upLat;
            string upOpis;
            string otOpis;
            double otLng;
            double otLat;
            int status;
            int brojKola;
            double tezina;
            double neto;

            while (dr.Read())
            {
                upLng = Convert.ToDouble(dr["UputnaLongitude"].ToString());
                upLat = Convert.ToDouble(dr["UputnaLatitude"].ToString());
                upOpis = dr["UputnaOpis"].ToString();
                otOpis = dr["OtpravnaOpis"].ToString();
                otLng = Convert.ToDouble(dr["OtpravnaLongitude"].ToString());
                otLat = Convert.ToDouble(dr["OtpravnaLatitude"].ToString());
                
                brojKola = Convert.ToInt32(dr["BrojKola"].ToString());
                tezina = Convert.ToDouble(dr["Tezina"].ToString());
                neto = Convert.ToDouble(dr["NetoTezinaM"].ToString());

                GMarkerGoogle markerUp = new GMarkerGoogle(new PointLatLng(upLng, upLat), GMarkerGoogleType.red_dot);
                markerUp.ToolTipMode = MarkerTooltipMode.Always;
                markerUp.ToolTipText = "Uputna: "+ upOpis;
                overlay.Markers.Add(markerUp);

                GMarkerGoogle markerOt = new GMarkerGoogle(new PointLatLng(otLng, otLat), GMarkerGoogleType.blue_dot);
                markerOt.ToolTipMode = MarkerTooltipMode.Always;
                markerOt.ToolTipText = "Otpravna: "+otOpis;
                overlay.Markers.Add(markerOt);
                int selectStatus = Convert.ToInt32(combo_Status.SelectedValue);
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    List<PointLatLng> points = new List<PointLatLng>();
                    points.Add(new PointLatLng(upLng, upLat));
                    points.Add(new PointLatLng(otLng, otLat));
                    polygon = new GMapPolygon(points, "mypoligon");
                    
                    if (selectStatus == 1) { polygon.Stroke.Color = Color.Red; }
                    else if (selectStatus==2) { polygon.Stroke.Color = Color.Blue; }
                    else if (selectStatus==3) { polygon.Stroke.Color = Color.Green; }
                    else if (selectStatus==4) { polygon.Stroke.Color = Color.Yellow; }
                    else if (selectStatus==5) { polygon.Stroke.Color = Color.Black; }
                    else if (selectStatus==6) { polygon.Stroke.Color = Color.White; }
                    else if (selectStatus==8) { polygon.Stroke.Color = Color.Gray; }
                    else { polygon.Stroke.Color = Color.RosyBrown; }
                    polygon.Stroke.Width = 2;
                    polyOvelray.Polygons.Add(polygon);
                }
            }
            map.Overlays.Add(polyOvelray);
            map.Overlays.Add(overlay);
            map.Zoom = 7;
            map.Zoom = 8;
        }


        //Pravi rute za zadati status i otpravnu stanicu
        private void btn_StanicaStatus_Click(object sender, EventArgs e)
        {
            overlay.Clear();
            overlay.Markers.Clear();
            polyOvelray.Clear();
            polyOvelray.Polygons.Clear();

            SqlConnection conn = new SqlConnection(connect);
            conn.Open();
            var query = "select Najava.ID as ID,Najava.Uputna as Uputna,stanice.Opis as UputnaOpis,stanice.Longitude as UputnaLongitude,stanice.latitude as UputnaLatitude," +
                "najava.Otpravna, stanice_1.Opis as OtpravnaOpis, stanice_1.longitude as OtpravnaLongitude,stanice_1.Latitude as OtpravnaLatitude, Najava.Status as status," +
                "BrojKola, Tezina,NetoTezinaM " +
                "From Najava " +
                "inner join stanice on stanice.id=najava.Uputna " +
                "inner join stanice as stanice_1 on stanice_1.id=najava.Otpravna " +
                "Where Najava.Otpravna=" + combo_Stanica.SelectedValue + " and Najava.Status="+combo_Status.SelectedValue+" and stanice.Longitude<>0 and stanice.Latitude<>0 and stanice_1.Longitude<>0 and stanice_1.Latitude<>0";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();

            double upLng;
            double upLat;
            string upOpis;
            string otOpis;
            double otLng;
            double otLat;

            while (dr.Read())
            {
                upLng = Convert.ToDouble(dr["UputnaLongitude"].ToString());
                upLat = Convert.ToDouble(dr["UputnaLatitude"].ToString());
                upOpis = dr["UputnaOpis"].ToString();
                otOpis = dr["OtpravnaOpis"].ToString();
                otLng = Convert.ToDouble(dr["OtpravnaLongitude"].ToString());
                otLat = Convert.ToDouble(dr["OtpravnaLatitude"].ToString());

                GMarkerGoogle markerUp = new GMarkerGoogle(new PointLatLng(upLng, upLat), GMarkerGoogleType.red_dot);
                markerUp.ToolTipMode = MarkerTooltipMode.Always;
                markerUp.ToolTipText = upOpis;
                overlay.Markers.Add(markerUp);

                GMarkerGoogle markerOt = new GMarkerGoogle(new PointLatLng(otLng, otLat), GMarkerGoogleType.blue_dot);
                markerOt.ToolTipText = otOpis;
                overlay.Markers.Add(markerOt);
                int selectStatus = Convert.ToInt32(combo_Status.SelectedValue);

                for (int i = 0; i < dr.FieldCount; i++)
                {
                    List<PointLatLng> points = new List<PointLatLng>();
                    points.Add(new PointLatLng(upLng, upLat));
                    points.Add(new PointLatLng(otLng, otLat));
                    polygon = new GMapPolygon(points, "mypoligon");
                    if (selectStatus == 1) { polygon.Stroke.Color = Color.Red; }
                    else if (selectStatus == 2) { polygon.Stroke.Color = Color.Blue; }
                    else if (selectStatus == 3) { polygon.Stroke.Color = Color.Green; }
                    else if (selectStatus == 4) { polygon.Stroke.Color = Color.Yellow; }
                    else if (selectStatus == 5) { polygon.Stroke.Color = Color.Black; }
                    else if (selectStatus == 6) { polygon.Stroke.Color = Color.White; }
                    else if (selectStatus == 8) { polygon.Stroke.Color = Color.Gray; }
                    else { polygon.Stroke.Color = Color.RosyBrown; }
                    polygon.Stroke.Width = 3;
                    polyOvelray.Polygons.Add(polygon);
                }
            }
            map.Overlays.Add(polyOvelray);
            map.Overlays.Add(overlay);
            map.Zoom = 7;
            map.Zoom = 8;
        }
        private void btn_Detaljno_Click(object sender, EventArgs e)
        {
            frmMapaOpis opis = new frmMapaOpis();
            opis.Show();
            opis.Focus();
        }
        private class StanicaRuta
        {
            public string Naziv { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }

            public StanicaRuta(string naziv, double latitude, double longitude)
            {
                Naziv = naziv;
                Latitude = latitude;
                Longitude = longitude;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                map.Overlays.Clear();

                var markerOverlay = new GMapOverlay("stanice");
                var routeOverlay = new GMapOverlay("ruta");

                var stanice = new List<StanicaRuta>
        {
            new StanicaRuta("Jakovo", 44.751418600658795, 20.254923569636006),
            new StanicaRuta("ŠID", 45.124416370313526, 19.222164225500624),
            new StanicaRuta("Zagreb", 45.80517412003623, 15.977577696973462),
            new StanicaRuta("Savski Marof", 45.87295439092795, 15.737627745007833),
            new StanicaRuta("Sezana", 45.70341317673655, 13.8629912329809),
            new StanicaRuta("Vila Opicina", 45.694700105408536, 13.791487170305157),
            new StanicaRuta("Venezia Porto Marghera", 45.472590254351054, 12.256132184016668)
        };

                List<PointLatLng> tackeRute = new List<PointLatLng>();

                foreach (var s in stanice)
                {
                    PointLatLng tacka = new PointLatLng(s.Latitude, s.Longitude);
                    tackeRute.Add(tacka);

                    GMarkerGoogle marker = new GMarkerGoogle(tacka, GMarkerGoogleType.red_dot);
                    marker.ToolTipMode = MarkerTooltipMode.Always;
                    marker.ToolTipText = s.Naziv;
                    marker.ToolTip.Fill = new SolidBrush(Color.WhiteSmoke);
                    marker.ToolTip.Foreground = Brushes.Black;
                    marker.ToolTip.Stroke = Pens.DarkGray;
                    marker.ToolTip.Font = new Font("Arial", 9, FontStyle.Bold);

                    markerOverlay.Markers.Add(marker);
                }

                GMapRoute ruta = new GMapRoute(tackeRute, "PutStanica");
                ruta.Stroke = new Pen(Color.DarkBlue, 3);

                routeOverlay.Routes.Add(ruta);

                map.Overlays.Add(routeOverlay);
                map.Overlays.Add(markerOverlay);

                map.Position = tackeRute[0];

                if (tackeRute.Count > 0)
                {
                    double minLat = tackeRute.Min(p => p.Lat);
                    double maxLat = tackeRute.Max(p => p.Lat);
                    double minLng = tackeRute.Min(p => p.Lng);
                    double maxLng = tackeRute.Max(p => p.Lng);

                    RectLatLng rect = RectLatLng.FromLTRB(maxLng, maxLat, minLng, minLat);
                    map.SetZoomToFitRect(rect);
                }

                map.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri iscrtavanju rute: " + ex.Message);
            }
        }
    }
}
