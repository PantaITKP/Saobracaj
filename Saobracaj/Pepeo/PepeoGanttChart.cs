using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Saobracaj.Pepeo
{
    public class PepeoGanttChart : UserControl
    {
        private readonly List<PepeoKorakVremena> _items = new List<PepeoKorakVremena>();
        private readonly ToolTip _toolTip = new ToolTip();

        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private DateTime _pocetakOse;
        private DateTime _krajOse;

        private const int LeftMargin = 300;
        private const int HeaderHeight = 64;
        private const int RowHeight = 30;
        private const int RightMargin = 25;
        private const int BottomMargin = 35;
        private const int PixelsPerDay = 168;

        public event EventHandler SelectionChanged;

        public int BrojDanaPrikaza { get; set; }

        public PepeoKorakVremena SelectedItem
        {
            get
            {
                if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
                    return null;

                return _items[_selectedIndex];
            }
        }

        public PepeoGanttChart()
        {
            BrojDanaPrikaza = 14;
            DoubleBuffered = true;
            AutoScroll = true;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 8.25F);

            _toolTip.InitialDelay = 250;
            _toolTip.ReshowDelay = 100;
            _toolTip.AutoPopDelay = 10000;
            _toolTip.ShowAlways = true;

            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void Prikazi(List<PepeoKorakVremena> koraci)
        {
            _items.Clear();

            if (koraci != null)
            {
                _items.AddRange(
                    koraci
                        .OrderBy(x => x.RB)
                        .ToList());
            }

            _selectedIndex = -1;
            _hoverIndex = -1;
            AutoScrollPosition = Point.Empty;

            if (_items.Count > 0)
            {
                _pocetakOse = _items.Min(x => x.PlaniraniPocetak).Date;
                _krajOse = _pocetakOse.AddDays(Math.Max(1, BrojDanaPrikaza));
            }
            else
            {
                _pocetakOse = DateTime.Today;
                _krajOse = _pocetakOse.AddDays(Math.Max(1, BrojDanaPrikaza));
            }

            AutoScrollMinSize = new Size(
                LeftMargin + TimelineWidth() + RightMargin,
                HeaderHeight + (_items.Count * RowHeight) + BottomMargin);

            Invalidate();
        }

        public void Ocisti()
        {
            _items.Clear();
            _selectedIndex = -1;
            _hoverIndex = -1;
            AutoScrollMinSize = Size.Empty;
            AutoScrollPosition = Point.Empty;
            _toolTip.SetToolTip(this, string.Empty);
            Invalidate();
        }

        public void SelektujPoId(int id)
        {
            int index = _items.FindIndex(x => x.ID == id);
            if (index < 0)
                return;

            _selectedIndex = index;
            OsigurajVidljivRed(index);
            Invalidate();

            if (SelectionChanged != null)
                SelectionChanged(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            DrawBackground(g);
            DrawHeader(g);

            if (_items.Count == 0)
            {
                DrawEmpty(g);
                return;
            }

            DrawTimelineBackground(g);
            DrawRows(g);
            DrawLegend(g);
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            int index = VratiIndeksReda(e.Y);
            if (index < 0 || index >= _items.Count)
                return;

            _selectedIndex = index;
            Invalidate();

            if (SelectionChanged != null)
                SelectionChanged(this, EventArgs.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int index = VratiIndeksReda(e.Y);

            if (index >= 0 && index < _items.Count)
            {
                Cursor = Cursors.Hand;

                if (_hoverIndex != index)
                {
                    _hoverIndex = index;
                    _toolTip.SetToolTip(this, NapraviTooltip(_items[index]));
                }
            }
            else
            {
                Cursor = Cursors.Default;

                if (_hoverIndex != -1)
                {
                    _hoverIndex = -1;
                    _toolTip.SetToolTip(this, string.Empty);
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
            _hoverIndex = -1;
            _toolTip.SetToolTip(this, string.Empty);
        }

        private void DrawBackground(Graphics g)
        {
            g.Clear(Color.White);
        }

        private void DrawHeader(Graphics g)
        {
            using (Brush headerBrush = new SolidBrush(Color.FromArgb(245, 245, 245)))
            using (Pen borderPen = new Pen(Color.FromArgb(205, 205, 205)))
            using (Font boldFont = new Font(Font, FontStyle.Bold))
            {
                g.FillRectangle(headerBrush, new Rectangle(0, 0, ClientSize.Width, HeaderHeight));
                g.DrawLine(borderPen, 0, HeaderHeight - 1, ClientSize.Width, HeaderHeight - 1);
                g.DrawLine(borderPen, LeftMargin - 1, 0, LeftMargin - 1, ClientSize.Height);

                TextRenderer.DrawText(
                    g,
                    "Stanica / operacija",
                    boldFont,
                    new Rectangle(8, 0, LeftMargin - 16, HeaderHeight),
                    Color.Black,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            DrawTimeHeader(g);
        }

        private void DrawTimeHeader(Graphics g)
        {
            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(LeftMargin, 0, Math.Max(0, ClientSize.Width - LeftMargin), ClientSize.Height));

            int scrollX = AutoScrollPosition.X;

            using (Pen dayPen = new Pen(Color.FromArgb(175, 175, 175)))
            using (Pen hourPen = new Pen(Color.FromArgb(215, 215, 215)))
            using (Brush saturdayBrush = new SolidBrush(Color.FromArgb(255, 250, 225)))
            using (Brush sundayBrush = new SolidBrush(Color.FromArgb(255, 235, 235)))
            using (Brush normalBrush = new SolidBrush(Color.FromArgb(248, 248, 248)))
            using (Font dayFont = new Font(Font, FontStyle.Bold))
            {
                for (int day = 0; day < BrojDanaPrikaza; day++)
                {
                    DateTime datum = _pocetakOse.AddDays(day);
                    int dayX = LeftMargin + scrollX + (day * PixelsPerDay);
                    Rectangle dayRect = new Rectangle(dayX, 0, PixelsPerDay, 31);

                    Brush dayBrush = normalBrush;
                    if (datum.DayOfWeek == DayOfWeek.Saturday)
                        dayBrush = saturdayBrush;
                    else if (datum.DayOfWeek == DayOfWeek.Sunday)
                        dayBrush = sundayBrush;

                    g.FillRectangle(dayBrush, dayRect);
                    g.DrawRectangle(dayPen, dayRect);

                    TextRenderer.DrawText(
                        g,
                        VratiDanSkraceno(datum.DayOfWeek) + " " + datum.ToString("dd.MM."),
                        dayFont,
                        dayRect,
                        Color.Black,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                    for (int hour = 0; hour <= 24; hour += 3)
                    {
                        int hourX = dayX + Convert.ToInt32((hour / 24.0) * PixelsPerDay);
                        bool major = hour % 6 == 0;

                        using (Pen pen = new Pen(
                            major ? Color.FromArgb(190, 190, 190) : Color.FromArgb(230, 230, 230),
                            major ? 1.0F : 1.0F))
                        {
                            g.DrawLine(pen, hourX, 31, hourX, HeaderHeight);
                        }

                        if (major && hour < 24)
                        {
                            TextRenderer.DrawText(
                                g,
                                hour.ToString("00") + ":00",
                                Font,
                                new Rectangle(hourX - 20, 34, 40, 22),
                                Color.DimGray,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }

                    g.DrawLine(hourPen, dayX, 31, dayX + PixelsPerDay, 31);
                }
            }

            g.Restore(state);
        }

        private void DrawTimelineBackground(Graphics g)
        {
            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(LeftMargin, HeaderHeight, Math.Max(0, ClientSize.Width - LeftMargin), Math.Max(0, ClientSize.Height - HeaderHeight)));

            int scrollX = AutoScrollPosition.X;

            using (Brush saturdayBrush = new SolidBrush(Color.FromArgb(255, 252, 238)))
            using (Brush sundayBrush = new SolidBrush(Color.FromArgb(255, 242, 242)))
            using (Pen dayPen = new Pen(Color.FromArgb(185, 185, 185)))
            using (Pen sixHourPen = new Pen(Color.FromArgb(218, 218, 218)))
            using (Pen threeHourPen = new Pen(Color.FromArgb(238, 238, 238)))
            {
                int bodyHeight = Math.Max(
                    ClientSize.Height - HeaderHeight,
                    (_items.Count * RowHeight) + BottomMargin);

                for (int day = 0; day < BrojDanaPrikaza; day++)
                {
                    DateTime datum = _pocetakOse.AddDays(day);
                    int dayX = LeftMargin + scrollX + (day * PixelsPerDay);
                    Rectangle dayRect = new Rectangle(dayX, HeaderHeight, PixelsPerDay, bodyHeight);

                    if (datum.DayOfWeek == DayOfWeek.Saturday)
                        g.FillRectangle(saturdayBrush, dayRect);
                    else if (datum.DayOfWeek == DayOfWeek.Sunday)
                        g.FillRectangle(sundayBrush, dayRect);

                    g.DrawLine(dayPen, dayX, HeaderHeight, dayX, HeaderHeight + bodyHeight);

                    for (int hour = 3; hour < 24; hour += 3)
                    {
                        int x = dayX + Convert.ToInt32((hour / 24.0) * PixelsPerDay);
                        Pen pen = hour % 6 == 0 ? sixHourPen : threeHourPen;
                        g.DrawLine(pen, x, HeaderHeight, x, HeaderHeight + bodyHeight);
                    }
                }

                int endX = LeftMargin + scrollX + TimelineWidth();
                g.DrawLine(dayPen, endX, HeaderHeight, endX, HeaderHeight + bodyHeight);
            }

            g.Restore(state);
        }

        private void DrawRows(Graphics g)
        {
            int scrollY = AutoScrollPosition.Y;
            int scrollX = AutoScrollPosition.X;

            using (Pen rowPen = new Pen(Color.FromArgb(225, 225, 225)))
            using (Pen selectedPen = new Pen(Color.FromArgb(40, 90, 150), 2))
            using (Pen actualPen = new Pen(Color.DarkRed, 2))
            using (Brush evenBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
            using (Brush oddBrush = new SolidBrush(Color.FromArgb(250, 250, 250)))
            using (Brush selectedBrush = new SolidBrush(Color.FromArgb(225, 238, 255)))
            using (Brush normalBarBrush = new SolidBrush(Color.FromArgb(70, 130, 180)))
            using (Brush changedBarBrush = new SolidBrush(Color.FromArgb(240, 150, 50)))
            using (Brush finishedBarBrush = new SolidBrush(Color.FromArgb(110, 170, 110)))
            using (Brush zeroBarBrush = new SolidBrush(Color.FromArgb(65, 65, 65)))
            using (Brush overflowBrush = new SolidBrush(Color.DarkRed))
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    PepeoKorakVremena item = _items[i];
                    int y = HeaderHeight + scrollY + (i * RowHeight);

                    if (y + RowHeight < HeaderHeight || y > ClientSize.Height)
                        continue;

                    Brush rowBrush = i % 2 == 0 ? evenBrush : oddBrush;
                    if (i == _selectedIndex)
                        rowBrush = selectedBrush;

                    // Leva kolona ostaje naizmenično obojena, dok se na vremenskom delu
                    // zadržavaju linije i boje subote/nedelje.
                    g.FillRectangle(rowBrush, new Rectangle(0, y, LeftMargin, RowHeight));

                    if (i == _selectedIndex)
                    {
                        using (Brush selectedTimelineBrush = new SolidBrush(Color.FromArgb(45, 120, 170, 230)))
                        {
                            g.FillRectangle(
                                selectedTimelineBrush,
                                new Rectangle(LeftMargin, y, Math.Max(0, ClientSize.Width - LeftMargin), RowHeight));
                        }
                    }

                    g.DrawLine(rowPen, 0, y + RowHeight, ClientSize.Width, y + RowHeight);

                    string leftText = item.RB.ToString() + ". " +
                        NullSafe(item.StanicaOpis) + " | " +
                        NullSafe(item.Operacija);

                    TextRenderer.DrawText(
                        g,
                        leftText,
                        Font,
                        new Rectangle(8, y + 3, LeftMargin - 16, RowHeight - 6),
                        Color.Black,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                    GraphicsState state = g.Save();
                    g.SetClip(new Rectangle(LeftMargin, HeaderHeight, Math.Max(0, ClientSize.Width - LeftMargin), Math.Max(0, ClientSize.Height - HeaderHeight)));

                    DateTime start = item.PlaniraniPocetak;
                    DateTime end = item.PlaniranoVreme;
                    if (end < start)
                        end = start;

                    bool izlaziDesno = end > _krajOse ||
                        (item.StvarnoVreme.HasValue && item.StvarnoVreme.Value > _krajOse);

                    DateTime prikazaniStart = start < _pocetakOse ? _pocetakOse : start;
                    DateTime prikazaniEnd = end > _krajOse ? _krajOse : end;

                    int x1 = VremeUX(prikazaniStart) + scrollX;
                    int x2 = VremeUX(prikazaniEnd) + scrollX;
                    int barHeight = 16;
                    int barY = y + ((RowHeight - barHeight) / 2);
                    int barWidth = Math.Max(4, x2 - x1);

                    Brush barBrush = normalBarBrush;
                    if (string.Equals(item.StatusCiklusa, "ZAVRSEN", StringComparison.OrdinalIgnoreCase))
                        barBrush = finishedBarBrush;
                    if (item.RucnoIzmenjeno)
                        barBrush = changedBarBrush;

                    if ((end - start).TotalMinutes <= 0)
                    {
                        g.FillRectangle(zeroBarBrush, new Rectangle(x1 - 2, barY, 4, barHeight));
                    }
                    else
                    {
                        Rectangle barRect = new Rectangle(x1, barY, barWidth, barHeight);
                        g.FillRectangle(barBrush, barRect);
                        g.DrawRectangle(Pens.SteelBlue, barRect);

                        string durationText = FormatTrajanje(start, end);
                        if (barWidth >= 58)
                        {
                            TextRenderer.DrawText(
                                g,
                                durationText,
                                Font,
                                barRect,
                                Color.White,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        }
                    }

                    if (item.StvarnoVreme.HasValue)
                    {
                        DateTime stvarno = item.StvarnoVreme.Value;
                        DateTime prikazanoStvarno = stvarno > _krajOse ? _krajOse : stvarno;
                        if (prikazanoStvarno < _pocetakOse)
                            prikazanoStvarno = _pocetakOse;

                        int actualX = VremeUX(prikazanoStvarno) + scrollX;
                        g.DrawLine(actualPen, actualX, y + 3, actualX, y + RowHeight - 3);
                    }

                    if (izlaziDesno)
                    {
                        int rightX = LeftMargin + scrollX + TimelineWidth() - 1;
                        Point[] triangle =
                        {
                            new Point(rightX - 10, barY),
                            new Point(rightX, barY + (barHeight / 2)),
                            new Point(rightX - 10, barY + barHeight)
                        };
                        g.FillPolygon(overflowBrush, triangle);
                    }

                    g.Restore(state);

                    if (i == _selectedIndex)
                    {
                        g.DrawRectangle(
                            selectedPen,
                            new Rectangle(1, y + 1, ClientSize.Width - 3, RowHeight - 2));
                    }
                }
            }
        }

        private void DrawLegend(Graphics g)
        {
            int scrollY = AutoScrollPosition.Y;
            int y = HeaderHeight + scrollY + (_items.Count * RowHeight) + 7;

            if (y < HeaderHeight || y > ClientSize.Height)
                return;

            using (Brush planBrush = new SolidBrush(Color.FromArgb(70, 130, 180)))
            using (Brush changeBrush = new SolidBrush(Color.FromArgb(240, 150, 50)))
            using (Brush finishedBrush = new SolidBrush(Color.FromArgb(110, 170, 110)))
            using (Pen actualPen = new Pen(Color.DarkRed, 2))
            {
                g.FillRectangle(planBrush, 10, y + 5, 18, 10);
                TextRenderer.DrawText(g, "Planirano", Font, new Rectangle(34, y, 90, 20), Color.Black);

                g.FillRectangle(finishedBrush, 120, y + 5, 18, 10);
                TextRenderer.DrawText(g, "Završen ciklus", Font, new Rectangle(144, y, 115, 20), Color.Black);

                g.FillRectangle(changeBrush, 270, y + 5, 18, 10);
                TextRenderer.DrawText(g, "Ručno izmenjeno", Font, new Rectangle(294, y, 130, 20), Color.Black);

                g.DrawLine(actualPen, 445, y + 4, 445, y + 16);
                TextRenderer.DrawText(g, "Stvarno vreme", Font, new Rectangle(455, y, 120, 20), Color.Black);
            }
        }

        private void DrawEmpty(Graphics g)
        {
            TextRenderer.DrawText(
                g,
                "Nema podataka za prikaz.",
                Font,
                new Rectangle(20, HeaderHeight + 20, Math.Max(100, ClientSize.Width - 40), 40),
                Color.DimGray,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private int VratiIndeksReda(int mouseY)
        {
            int realY = mouseY - HeaderHeight - AutoScrollPosition.Y;
            if (realY < 0)
                return -1;

            return realY / RowHeight;
        }

        private void OsigurajVidljivRed(int index)
        {
            int rowTop = index * RowHeight;
            int rowBottom = rowTop + RowHeight;
            int currentScrollY = -AutoScrollPosition.Y;
            int visibleHeight = Math.Max(1, ClientSize.Height - HeaderHeight);
            int noviScrollY = currentScrollY;

            if (rowTop < currentScrollY)
                noviScrollY = rowTop;
            else if (rowBottom > currentScrollY + visibleHeight)
                noviScrollY = rowBottom - visibleHeight;

            if (noviScrollY != currentScrollY)
            {
                int currentScrollX = -AutoScrollPosition.X;
                AutoScrollPosition = new Point(currentScrollX, Math.Max(0, noviScrollY));
            }
        }

        private int TimelineWidth()
        {
            return Math.Max(1, BrojDanaPrikaza) * PixelsPerDay;
        }

        private int VremeUX(DateTime vreme)
        {
            double totalMinutes = Math.Max(1, (_krajOse - _pocetakOse).TotalMinutes);
            double currentMinutes = (vreme - _pocetakOse).TotalMinutes;
            double ratio = currentMinutes / totalMinutes;

            if (ratio < 0)
                ratio = 0;
            if (ratio > 1)
                ratio = 1;

            return LeftMargin + Convert.ToInt32(ratio * TimelineWidth());
        }

        private string NapraviTooltip(PepeoKorakVremena item)
        {
            DateTime krajZaPrikaz = item.StvarnoVreme.HasValue
                ? item.StvarnoVreme.Value
                : item.PlaniranoVreme;

            string najava = item.Najava.HasValue && item.Najava.Value > 0
                ? item.Najava.Value.ToString()
                : "bez najave";

            string tekst =
                "Ciklus: " + item.Ciklus.ToString() + Environment.NewLine +
                "Najava: " + najava + Environment.NewLine +
                "Stanica: " + NullSafe(item.StanicaOpis) + Environment.NewLine +
                "Operacija: " + NullSafe(item.Operacija) + Environment.NewLine +
                "Planirani početak: " + item.PlaniraniPocetak.ToString("dd.MM.yyyy HH:mm") + Environment.NewLine +
                "Planirani kraj: " + item.PlaniranoVreme.ToString("dd.MM.yyyy HH:mm") + Environment.NewLine +
                "Trajanje: " + FormatTrajanje(item.PlaniraniPocetak, item.PlaniranoVreme) + Environment.NewLine +
                "Status: " + NullSafe(item.StatusCiklusa);

            if (item.StvarnoVreme.HasValue)
                tekst += Environment.NewLine + "Stvarno vreme: " + krajZaPrikaz.ToString("dd.MM.yyyy HH:mm");

            if (!string.IsNullOrWhiteSpace(item.RazlogKasnjenja))
                tekst += Environment.NewLine + "Razlog kašnjenja: " + item.RazlogKasnjenja.Trim();

            if (!string.IsNullOrWhiteSpace(item.Napomena))
                tekst += Environment.NewLine + "Napomena: " + item.Napomena.Trim();

            if (item.PlaniranoVreme > _krajOse ||
                (item.StvarnoVreme.HasValue && item.StvarnoVreme.Value > _krajOse))
            {
                tekst += Environment.NewLine + "Upozorenje: vreme prelazi okvir prikaza od " +
                    BrojDanaPrikaza.ToString() + " dana.";
            }

            return tekst;
        }

        private string FormatTrajanje(DateTime start, DateTime end)
        {
            TimeSpan ts = end - start;

            if (ts.TotalMinutes < 1)
                return "0 min";

            if (ts.TotalHours < 1)
                return Convert.ToInt32(ts.TotalMinutes).ToString() + " min";

            int dani = Convert.ToInt32(Math.Floor(ts.TotalDays));
            int sati = ts.Hours;
            int minuti = ts.Minutes;

            if (dani > 0)
            {
                string rezultat = dani.ToString() + " d";
                if (sati > 0)
                    rezultat += " " + sati.ToString() + " h";
                if (minuti > 0)
                    rezultat += " " + minuti.ToString() + " min";
                return rezultat;
            }

            string samoSati = Convert.ToInt32(Math.Floor(ts.TotalHours)).ToString() + " h";
            if (minuti > 0)
                samoSati += " " + minuti.ToString() + " min";

            return samoSati;
        }

        private string VratiDanSkraceno(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday:
                    return "Pon";
                case DayOfWeek.Tuesday:
                    return "Uto";
                case DayOfWeek.Wednesday:
                    return "Sre";
                case DayOfWeek.Thursday:
                    return "Čet";
                case DayOfWeek.Friday:
                    return "Pet";
                case DayOfWeek.Saturday:
                    return "Sub";
                case DayOfWeek.Sunday:
                    return "Ned";
                default:
                    return string.Empty;
            }
        }

        private string NullSafe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}