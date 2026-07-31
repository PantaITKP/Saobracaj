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
        private int _lastTooltipIndex = -1;

        private const int LeftWidth = 285;
        private const int HeaderHeight = 60;
        private const int RowHeight = 48;
        private const int DayWidth = 150;
        private const int Days = 16;
        private const int BottomHeight = 38;
        private const int BarHeight = 16;

        private static readonly Color PlannedColor = Color.FromArgb(65, 126, 180);
        private static readonly Color LateColor = Color.FromArgb(194, 42, 48);
        private static readonly Color EarlyColor = Color.FromArgb(42, 145, 89);
        private static readonly Color WeekendColor = Color.FromArgb(249, 239, 239);
        private static readonly Color SelectedBackColor = Color.FromArgb(232, 241, 251);

        public event EventHandler SelectionChanged;

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
            DoubleBuffered = true;
            AutoScroll = true;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 8.25F);

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            UpdateStyles();
        }

        public void Prikazi(List<PepeoKorakVremena> koraci)
        {
            _items.Clear();

            if (koraci != null)
                _items.AddRange(koraci.OrderBy(x => x.Redosled));

            _selectedIndex = -1;
            _lastTooltipIndex = -1;
            _toolTip.Hide(this);

            int width = LeftWidth + (Days * DayWidth) + 20;
            int height = HeaderHeight + (_items.Count * RowHeight) + BottomHeight;

            AutoScrollMinSize = new Size(width, height);
            AutoScrollPosition = Point.Empty;
            Invalidate();
        }

        public void SelektujPoID(int korakID)
        {
            int index = _items.FindIndex(x => x.ID == korakID);
            if (index == _selectedIndex)
                return;

            _selectedIndex = index;
            Invalidate();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);

            // Kod custom crtanja WinForms ponekad ne osvezi ceo viewport pri pomeranju
            // horizontalnog scrollbara. Potpuno ponovno crtanje uklanja "razmazivanje" teksta.
            Invalidate();
            Update();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;

            if (_items.Count == 0)
            {
                TextRenderer.DrawText(
                    g,
                    "Nema podataka za prikaz.",
                    Font,
                    new Rectangle(20, 20, 400, 30),
                    Color.DimGray,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                return;
            }

            DateTime axisStart = _items.Min(x => x.PlaniraniPocetak).Date;
            DateTime axisEnd = axisStart.AddDays(Days);

            int scrollX = AutoScrollPosition.X; // negativna vrednost kada se pomeri udesno
            int scrollY = AutoScrollPosition.Y;

            DrawTimelineBackground(g, axisStart, scrollX, scrollY);
            DrawRowsAndBars(g, axisStart, axisEnd, scrollX, scrollY);
            DrawStickyLabels(g, scrollY);
            DrawStickyHeader(g, axisStart, scrollX);
            DrawLegend(g, scrollY);
        }

        private void DrawTimelineBackground(Graphics g, DateTime axisStart, int scrollX, int scrollY)
        {
            int rowsHeight = _items.Count * RowHeight;

            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(LeftWidth, 0, Math.Max(0, ClientSize.Width - LeftWidth), ClientSize.Height), CombineMode.Intersect);

            using (Pen dayLine = new Pen(Color.FromArgb(197, 202, 209)))
            using (Pen minor = new Pen(Color.FromArgb(235, 237, 240)))
            {
                for (int d = 0; d < Days; d++)
                {
                    DateTime day = axisStart.AddDays(d);
                    int x = LeftWidth + (d * DayWidth) + scrollX;

                    if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                    {
                        using (Brush wb = new SolidBrush(WeekendColor))
                        {
                            g.FillRectangle(
                                wb,
                                x,
                                HeaderHeight + scrollY,
                                DayWidth,
                                rowsHeight);
                        }
                    }

                    g.DrawLine(
                        dayLine,
                        x,
                        HeaderHeight + scrollY,
                        x,
                        HeaderHeight + rowsHeight + scrollY);

                    for (int h = 6; h < 24; h += 6)
                    {
                        int hx = x + (int)Math.Round((h / 24.0) * DayWidth);
                        g.DrawLine(
                            minor,
                            hx,
                            HeaderHeight + scrollY,
                            hx,
                            HeaderHeight + rowsHeight + scrollY);
                    }
                }

                int right = LeftWidth + Days * DayWidth + scrollX;
                g.DrawLine(
                    dayLine,
                    right,
                    HeaderHeight + scrollY,
                    right,
                    HeaderHeight + rowsHeight + scrollY);
            }

            g.Restore(state);
        }

        private void DrawRowsAndBars(
            Graphics g,
            DateTime axisStart,
            DateTime axisEnd,
            int scrollX,
            int scrollY)
        {
            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(LeftWidth, HeaderHeight, Math.Max(0, ClientSize.Width - LeftWidth), Math.Max(0, ClientSize.Height - HeaderHeight)), CombineMode.Intersect);

            using (Pen rowLine = new Pen(Color.FromArgb(225, 228, 232)))
            using (Brush alternate = new SolidBrush(Color.FromArgb(252, 252, 253)))
            using (Brush selected = new SolidBrush(SelectedBackColor))
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    int y = HeaderHeight + (i * RowHeight) + scrollY;

                    if (y + RowHeight < HeaderHeight || y > ClientSize.Height)
                        continue;

                    Rectangle timelineRow = new Rectangle(
                        LeftWidth,
                        y,
                        Days * DayWidth,
                        RowHeight);

                    if (i == _selectedIndex)
                        g.FillRectangle(selected, timelineRow);
                    else if (i % 2 == 1)
                        g.FillRectangle(alternate, timelineRow);

                    g.DrawLine(
                        rowLine,
                        LeftWidth,
                        y + RowHeight,
                        LeftWidth + Days * DayWidth + scrollX,
                        y + RowHeight);

                    DrawBar(g, _items[i], axisStart, axisEnd, y, scrollX, i == _selectedIndex);
                }
            }

            g.Restore(state);
        }

        private void DrawStickyLabels(Graphics g, int scrollY)
        {
            using (Pen line = new Pen(Color.FromArgb(225, 228, 232)))
            using (Brush even = new SolidBrush(Color.White))
            using (Brush alternate = new SolidBrush(Color.FromArgb(252, 252, 253)))
            using (Brush selected = new SolidBrush(SelectedBackColor))
            using (Font operationFont = new Font(Font, FontStyle.Bold))
            using (Font detailFont = new Font(Font.FontFamily, 7.5F, FontStyle.Regular))
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    int y = HeaderHeight + (i * RowHeight) + scrollY;
                    if (y + RowHeight < HeaderHeight || y > ClientSize.Height)
                        continue;

                    Rectangle rect = new Rectangle(0, y, LeftWidth, RowHeight);
                    Brush back = i == _selectedIndex ? selected : (i % 2 == 1 ? alternate : even);
                    g.FillRectangle(back, rect);

                    PepeoKorakVremena item = _items[i];

                    TextRenderer.DrawText(
                        g,
                        item.Operacija ?? string.Empty,
                        operationFont,
                        new Rectangle(10, y + 5, LeftWidth - 18, 19),
                        Color.FromArgb(35, 38, 42),
                        TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                    string detalj = (item.Drzava ?? string.Empty) + "  |  Trajanje: " + FormatDuration(item.PlaniraniPocetak, item.PlaniraniKraj);
                    TextRenderer.DrawText(
                        g,
                        detalj,
                        detailFont,
                        new Rectangle(10, y + 25, LeftWidth - 18, 17),
                        Color.DimGray,
                        TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                    g.DrawLine(line, 0, y + RowHeight, LeftWidth, y + RowHeight);
                    g.DrawLine(line, LeftWidth - 1, y, LeftWidth - 1, y + RowHeight);
                }
            }
        }

        private void DrawStickyHeader(Graphics g, DateTime axisStart, int scrollX)
        {
            // Vremenska osa - skroluje se samo horizontalno.
            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(LeftWidth, 0, Math.Max(0, ClientSize.Width - LeftWidth), HeaderHeight), CombineMode.Intersect);

            using (Brush header = new SolidBrush(Color.FromArgb(246, 247, 249)))
            using (Pen line = new Pen(Color.FromArgb(218, 222, 228)))
            using (Font bold = new Font(Font, FontStyle.Bold))
            {
                for (int d = 0; d < Days; d++)
                {
                    DateTime day = axisStart.AddDays(d);
                    int x = LeftWidth + (d * DayWidth) + scrollX;
                    Rectangle dayRect = new Rectangle(x, 0, DayWidth, HeaderHeight);

                    if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                    {
                        using (Brush wb = new SolidBrush(WeekendColor))
                            g.FillRectangle(wb, dayRect);
                    }
                    else
                    {
                        g.FillRectangle(header, dayRect);
                    }

                    g.DrawRectangle(line, dayRect);

                    TextRenderer.DrawText(
                        g,
                        day.ToString("ddd dd.MM."),
                        bold,
                        new Rectangle(x + 4, 4, DayWidth - 8, 20),
                        Color.FromArgb(55, 58, 63),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);

                    for (int h = 0; h <= 18; h += 6)
                    {
                        int hx = x + (int)Math.Round((h / 24.0) * DayWidth);
                        TextRenderer.DrawText(
                            g,
                            h.ToString("00") + ":00",
                            Font,
                            new Rectangle(hx - 23, 31, 46, 18),
                            Color.Gray,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
                    }
                }
            }

            g.Restore(state);

            // Levo zaglavlje je "sticky" i ne pomera se horizontalno.
            using (Brush leftHeader = new SolidBrush(Color.FromArgb(246, 247, 249)))
            using (Pen border = new Pen(Color.FromArgb(218, 222, 228)))
            using (Font bold = new Font(Font, FontStyle.Bold))
            {
                Rectangle left = new Rectangle(0, 0, LeftWidth, HeaderHeight);
                g.FillRectangle(leftHeader, left);
                g.DrawRectangle(border, left);

                TextRenderer.DrawText(
                    g,
                    "Operacija",
                    bold,
                    new Rectangle(12, 0, LeftWidth - 20, HeaderHeight),
                    Color.FromArgb(45, 48, 54),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }

        private void DrawBar(
            Graphics g,
            PepeoKorakVremena item,
            DateTime axisStart,
            DateTime axisEnd,
            int rowY,
            int scrollX,
            bool selected)
        {
            int barY = rowY + ((RowHeight - BarHeight) / 2);

            DateTime planStart = Clip(item.PlaniraniPocetak, axisStart, axisEnd);
            DateTime planEnd = Clip(item.PlaniraniKraj, axisStart, axisEnd);

            int xStart = TimeToX(planStart, axisStart) + scrollX;
            int xPlanEnd = TimeToX(planEnd, axisStart) + scrollX;

            int minX = Math.Min(xStart, xPlanEnd);
            int maxX = Math.Max(xStart, xPlanEnd);

            if (!item.StvarniKraj.HasValue || item.OdstupanjeMinuta == 0)
            {
                FillSegment(g, PlannedColor, xStart, xPlanEnd, barY, BarHeight);
            }
            else
            {
                DateTime actualEnd = Clip(item.StvarniKraj.Value, axisStart, axisEnd);
                int xActualEnd = TimeToX(actualEnd, axisStart) + scrollX;
                minX = Math.Min(minX, xActualEnd);
                maxX = Math.Max(maxX, xActualEnd);

                if (item.OdstupanjeMinuta > 0)
                {
                    FillSegment(g, PlannedColor, xStart, xPlanEnd, barY, BarHeight);
                    FillSegment(g, LateColor, xPlanEnd, xActualEnd, barY, BarHeight);
                }
                else
                {
                    DateTime actualStart = item.StvarniPocetak.HasValue
                        ? item.StvarniPocetak.Value
                        : item.PlaniraniPocetak;

                    actualStart = Clip(actualStart, axisStart, axisEnd);
                    int xActualStart = TimeToX(actualStart, axisStart) + scrollX;
                    minX = Math.Min(minX, xActualStart);
                    maxX = Math.Max(maxX, xActualStart);

                    if (xActualStart > xStart)
                        FillSegment(g, PlannedColor, xStart, xActualStart, barY, BarHeight);

                    FillSegment(g, EarlyColor, xActualStart, xActualEnd, barY, BarHeight);
                    FillSegment(g, PlannedColor, xActualEnd, xPlanEnd, barY, BarHeight);
                }
            }

            if (selected)
            {
                using (Pen p = new Pen(Color.FromArgb(35, 38, 42), 1))
                {
                    p.DashStyle = DashStyle.Dot;
                    int width = Math.Max(4, maxX - minX);
                    g.DrawRectangle(p, minX - 2, barY - 3, width + 4, BarHeight + 6);
                }
            }
        }

        private static void FillSegment(Graphics g, Color color, int x1, int x2, int y, int h)
        {
            int left = Math.Min(x1, x2);
            int width = Math.Max(2, Math.Abs(x2 - x1));

            using (Brush b = new SolidBrush(color))
                g.FillRectangle(b, left, y, width, h);
        }

        private void DrawLegend(Graphics g, int scrollY)
        {
            int y = HeaderHeight + (_items.Count * RowHeight) + 8 + scrollY;
            DrawLegendItem(g, PlannedColor, "Planirano", 12, y);
            DrawLegendItem(g, LateColor, "Kašnjenje", 122, y);
            DrawLegendItem(g, EarlyColor, "Ranije", 242, y);
        }

        private void DrawLegendItem(Graphics g, Color color, string text, int x, int y)
        {
            using (Brush b = new SolidBrush(color))
                g.FillRectangle(b, x, y + 3, 18, 10);

            TextRenderer.DrawText(
                g,
                text,
                Font,
                new Rectangle(x + 24, y, 95, 20),
                Color.DimGray,
                TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            int index = RowIndexFromClientY(e.Y);
            if (index < 0 || index >= _items.Count)
                return;

            // Klik na naziv reda ili direktno na bar selektuje operaciju.
            bool clickedLabel = e.X >= 0 && e.X < LeftWidth;
            bool clickedBar = IsPointOnBar(index, e.Location);

            if (!clickedLabel && !clickedBar)
                return;

            _selectedIndex = index;
            Invalidate();

            if (SelectionChanged != null)
                SelectionChanged(this, EventArgs.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int index = RowIndexFromClientY(e.Y);
            bool validRow = index >= 0 && index < _items.Count;
            bool active = validRow && (e.X < LeftWidth || IsPointOnBar(index, e.Location));

            Cursor = active ? Cursors.Hand : Cursors.Default;

            if (!active)
            {
                if (_lastTooltipIndex != -1)
                    _toolTip.Hide(this);

                _lastTooltipIndex = -1;
                return;
            }

            if (_lastTooltipIndex == index)
                return;

            _lastTooltipIndex = index;
            PepeoKorakVremena k = _items[index];

            string tip =
                k.Operacija + Environment.NewLine +
                "Trajanje: " + FormatDuration(k.PlaniraniPocetak, k.PlaniraniKraj) + Environment.NewLine +
                "Plan: " + k.PlaniraniPocetak.ToString("dd.MM.yyyy HH:mm") +
                " - " + k.PlaniraniKraj.ToString("dd.MM.yyyy HH:mm");

            if (k.StvarniKraj.HasValue)
            {
                string odst = k.OdstupanjeMinuta > 0
                    ? "+" + k.OdstupanjeMinuta + " min"
                    : k.OdstupanjeMinuta + " min";

                tip += Environment.NewLine +
                       "Stvarni kraj: " + k.StvarniKraj.Value.ToString("dd.MM.yyyy HH:mm") +
                       " (" + odst + ")";
            }

            if (!string.IsNullOrWhiteSpace(k.RazlogOdstupanja))
                tip += Environment.NewLine + "Razlog: " + k.RazlogOdstupanja;

            if (!string.IsNullOrWhiteSpace(k.Napomena))
                tip += Environment.NewLine + "Napomena: " + k.Napomena;

            _toolTip.Show(tip, this, e.X + 14, e.Y + 18, 5000);
        }

        private bool IsPointOnBar(int index, Point clientPoint)
        {
            if (index < 0 || index >= _items.Count)
                return false;

            PepeoKorakVremena item = _items[index];
            DateTime axisStart = _items.Min(x => x.PlaniraniPocetak).Date;
            DateTime axisEnd = axisStart.AddDays(Days);

            int scrollX = AutoScrollPosition.X;
            int scrollY = AutoScrollPosition.Y;

            int rowY = HeaderHeight + (index * RowHeight) + scrollY;
            int barY = rowY + ((RowHeight - BarHeight) / 2) - 4;
            int barH = BarHeight + 8;

            DateTime minTime = item.PlaniraniPocetak;
            DateTime maxTime = item.PlaniraniKraj;

            if (item.StvarniPocetak.HasValue && item.StvarniPocetak.Value < minTime)
                minTime = item.StvarniPocetak.Value;

            if (item.StvarniKraj.HasValue)
            {
                if (item.StvarniKraj.Value < minTime)
                    minTime = item.StvarniKraj.Value;
                if (item.StvarniKraj.Value > maxTime)
                    maxTime = item.StvarniKraj.Value;
            }

            minTime = Clip(minTime, axisStart, axisEnd);
            maxTime = Clip(maxTime, axisStart, axisEnd);

            int x1 = TimeToX(minTime, axisStart) + scrollX;
            int x2 = TimeToX(maxTime, axisStart) + scrollX;

            Rectangle hit = new Rectangle(
                Math.Min(x1, x2) - 4,
                barY,
                Math.Max(8, Math.Abs(x2 - x1) + 8),
                barH);

            return hit.Contains(clientPoint);
        }

        private int RowIndexFromClientY(int clientY)
        {
            int contentY = clientY - AutoScrollPosition.Y;
            int relative = contentY - HeaderHeight;

            if (relative < 0)
                return -1;

            return relative / RowHeight;
        }

        private int TimeToX(DateTime value, DateTime axisStart)
        {
            double days = (value - axisStart).TotalDays;
            return LeftWidth + (int)Math.Round(days * DayWidth);
        }

        private static DateTime Clip(DateTime value, DateTime min, DateTime max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static string FormatDuration(DateTime start, DateTime end)
        {
            TimeSpan ts = end - start;
            if (ts < TimeSpan.Zero)
                ts = TimeSpan.Zero;

            if (ts.TotalDays >= 1)
                return ((int)ts.TotalDays).ToString() + "d " + ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00");

            return ((int)ts.TotalHours).ToString("00") + ":" + ts.Minutes.ToString("00");
        }
    }
}
