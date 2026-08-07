using System.Drawing;
using System.Drawing.Drawing2D;
using BudgetManager.BLL.Services;

namespace BudgetManager.UI.Views
{
    /// <summary>
    /// Lightweight line chart (GDI+) that plots a <see cref="ProjectionSeries"/>: a running
    /// net-worth line across a month, with orange markers where recurring items are injected.
    /// </summary>
    public class ProjectionChart : Control
    {
        private ProjectionSeries? _series;

        // Distinct colors for category series (tab10-style palette), cycled if there are more.
        private static readonly Color[] CategoryPalette =
        {
            Color.FromArgb(31, 119, 180), Color.FromArgb(255, 127, 14), Color.FromArgb(148, 103, 189),
            Color.FromArgb(140, 86, 75), Color.FromArgb(227, 119, 194), Color.FromArgb(127, 127, 127),
            Color.FromArgb(188, 189, 34), Color.FromArgb(23, 190, 207), Color.FromArgb(44, 160, 44),
            Color.FromArgb(214, 39, 40)
        };

        private static Color CategoryColor(int index) => CategoryPalette[index % CategoryPalette.Length];

        private Point _mouse;
        private bool _hovering;
        private bool _showCategories = true;

        /// <summary>Whether the per-category series (lines, legend, hover rows) are drawn.</summary>
        public bool ShowCategories
        {
            get => _showCategories;
            set { _showCategories = value; Invalidate(); }
        }

        public ProjectionChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;

            MouseMove += (_, e) => { _mouse = e.Location; _hovering = true; Invalidate(); };
            MouseLeave += (_, _) => { _hovering = false; Invalidate(); };
        }

        public void SetSeries(ProjectionSeries series)
        {
            _series = series;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            if (_series is null || _series.Points.Count < 2)
            {
                TextRenderer.DrawText(g, Loc.T("No data to project."), Font, ClientRectangle, Color.Gray,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            var pts = _series.Points;
            int n = pts.Count;

            bool showCats = _showCategories && _series.Categories.Count > 0;

            const int left = 78, top = 34, right = 18;
            int bottom = showCats ? 82 : 46; // extra room for the category legend
            var plot = new Rectangle(left, top, Math.Max(10, Width - left - right), Math.Max(10, Height - top - bottom));

            // Y range. Always include zero. Only extend below zero when the data actually dips
            // negative; otherwise the axis bottom sits exactly on 0 (no negative padding).
            decimal seriesMin = showCats ? _series.Min : _series.BalanceMin;
            decimal seriesMax = showCats ? _series.Max : _series.BalanceMax;
            bool goesNegative = seriesMin < 0m;
            decimal dMax = Math.Max(0m, seriesMax);
            decimal dMin = goesNegative ? seriesMin : 0m;
            if (dMin == dMax) dMax += 1;
            double range = (double)(dMax - dMin);
            if (range <= 0) range = 1;
            double pad = range * 0.08;
            double yMax = (double)dMax + pad;
            double yMin = goesNegative ? (double)dMin - pad : 0.0;

            float X(int i) => plot.Left + (float)i / (n - 1) * plot.Width;
            float Y(double v) => (float)(plot.Bottom - (v - yMin) / (yMax - yMin) * plot.Height);

            // Trend: does the window end above (growing) or below (shrinking) where it started?
            decimal trend = _series.EndBalance - _series.StartBalance;
            bool growing = trend >= 0;
            Color trendColor = growing ? Color.FromArgb(39, 174, 96) : Color.FromArgb(192, 57, 43);

            using var axisPen = new Pen(Color.FromArgb(210, 210, 210));
            using var gridPen = new Pen(Color.FromArgb(236, 236, 236));
            using var hGridPen = new Pen(Color.FromArgb(216, 216, 216));
            using var zeroPen = new Pen(Color.FromArgb(170, 170, 170)) { DashStyle = DashStyle.Dash };
            using var basePen = new Pen(Color.FromArgb(120, 120, 120), 1f) { DashStyle = DashStyle.Dash };
            using var linePen = new Pen(trendColor, 2f);
            using var lineBrush = new SolidBrush(trendColor);
            using var fillBrush = new SolidBrush(Color.FromArgb(38, trendColor));
            using var recurBrush = new SolidBrush(Color.DarkOrange);

            // Horizontal gridlines at "nice" rounded values (finer resolution than a fixed count).
            double step = NiceStep(yMax - yMin, 10);
            string fmt = step < 1 ? "N2" : "N0";
            double firstTick = Math.Ceiling(yMin / step) * step;
            for (double v = firstTick, guard = 0; v <= yMax && guard < 200; v += step, guard++)
            {
                float y = Y(v);
                if (y < plot.Top - 1 || y > plot.Bottom + 1) continue;
                g.DrawLine(hGridPen, plot.Left, y, plot.Right, y);
                TextRenderer.DrawText(g, ((decimal)v).ToString(fmt), Font,
                    new Rectangle(0, (int)y - 9, left - 6, 18), Color.FromArgb(90, 90, 90),
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }

            // Axes.
            g.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);
            g.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

            // Zero baseline.
            if (yMin < 0 && yMax > 0)
                g.DrawLine(zeroPen, plot.Left, Y(0), plot.Right, Y(0));

            // X date labels (about 6 across).
            int step2 = Math.Max(1, n / 6);
            for (int i = 0; i < n; i += step2)
            {
                float x = X(i);
                g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                TextRenderer.DrawText(g, pts[i].Date.ToString("MM/dd"), Font,
                    new Rectangle((int)x - 30, plot.Bottom + 4, 60, 18), Color.FromArgb(90, 90, 90),
                    TextFormatFlags.HorizontalCenter);
            }

            // Starting-balance reference line (so growth/shrink is visible at a glance).
            float baseY = Y((double)_series.StartBalance);
            g.DrawLine(basePen, plot.Left, baseY, plot.Right, baseY);

            // Balance line + shaded area against the start baseline (green above, red below).
            var linePoints = new PointF[n];
            for (int i = 0; i < n; i++) linePoints[i] = new PointF(X(i), Y((double)pts[i].Balance));

            var area = new PointF[n + 2];
            Array.Copy(linePoints, area, n);
            area[n] = new PointF(X(n - 1), baseY);
            area[n + 1] = new PointF(X(0), baseY);
            g.FillPolygon(fillBrush, area);

            // Category series (drawn under the balance line so the balance stays prominent).
            if (showCats)
            {
                for (int s = 0; s < _series.Categories.Count; s++)
                {
                    var cs = _series.Categories[s];
                    if (cs.Values.Count != n) continue;
                    using var catPen = new Pen(CategoryColor(s), 1.5f);
                    var catPoints = new PointF[n];
                    for (int i = 0; i < n; i++) catPoints[i] = new PointF(X(i), Y((double)cs.Values[i]));
                    g.DrawLines(catPen, catPoints);
                }
            }

            g.DrawLines(linePen, linePoints);

            // Point dots + recurring injection markers.
            for (int i = 0; i < n; i++)
            {
                var p = linePoints[i];
                g.FillEllipse(lineBrush, p.X - 2.5f, p.Y - 2.5f, 5f, 5f);
                if (pts[i].Recurring != 0m)
                    g.FillEllipse(recurBrush, p.X - 4.5f, p.Y - 4.5f, 9f, 9f);
            }

            // Title + legend.
            string title = $"{_series.Start:yyyy-MM-dd} → {_series.End:yyyy-MM-dd}   " +
                           $"{Loc.T("Balance")} {_series.StartBalance:N2} → {_series.EndBalance:N2}";
            TextRenderer.DrawText(g, title, Font, new Point(left, 8), Color.FromArgb(60, 60, 60));

            using var badgeFont = new Font(Font, FontStyle.Bold);
            var titleSize = TextRenderer.MeasureText(g, title, Font);
            string badge = growing ? Loc.F("▲ growing  +{0}", trend.ToString("N2")) : Loc.F("▼ shrinking  {0}", trend.ToString("N2"));
            TextRenderer.DrawText(g, badge, badgeFont, new Point(left + titleSize.Width + 14, 7), trendColor);

            int legendX = plot.Right - 220;
            g.FillRectangle(lineBrush, legendX, 12, 12, 6);
            TextRenderer.DrawText(g, Loc.T("Projected balance"), Font, new Point(legendX + 16, 6), Color.FromArgb(90, 90, 90));
            g.FillEllipse(recurBrush, legendX + 140, 10, 9, 9);
            TextRenderer.DrawText(g, Loc.T("Recurring"), Font, new Point(legendX + 152, 6), Color.FromArgb(90, 90, 90));

            // Category legend along the bottom (wraps; caps at two rows).
            if (showCats)
            {
                int lx = plot.Left;
                int ly = plot.Bottom + 22;
                const int rowHeight = 16;
                int rows = 0;
                for (int s = 0; s < _series.Categories.Count; s++)
                {
                    var cs = _series.Categories[s];
                    string label = cs.IsInterest ? Loc.T("Gained interest") : cs.IsUncategorized ? Loc.T("(uncategorized)") : cs.Name;
                    int itemWidth = 14 + TextRenderer.MeasureText(g, label, Font).Width + 14;

                    if (lx + itemWidth > plot.Right && lx > plot.Left)
                    {
                        lx = plot.Left;
                        ly += rowHeight;
                        if (++rows >= 2) break;
                    }

                    using (var sw = new SolidBrush(CategoryColor(s)))
                        g.FillRectangle(sw, lx, ly + 3, 10, 8);
                    TextRenderer.DrawText(g, label, Font, new Point(lx + 14, ly), Color.FromArgb(90, 90, 90));
                    lx += itemWidth;
                }
            }

            DrawHover(g, plot, pts, n, X, Y);
        }

        private void DrawHover(Graphics g, Rectangle plot, IReadOnlyList<ProjectionPoint> pts, int n,
            Func<int, float> X, Func<double, float> Y)
        {
            if (_series is null || !_hovering || n < 2 || !plot.Contains(_mouse)) return;

            int idx = (int)Math.Round((_mouse.X - plot.Left) / (double)plot.Width * (n - 1));
            idx = Math.Clamp(idx, 0, n - 1);
            float hx = X(idx);

            // Vertical guide + markers on every series at this day.
            using (var guide = new Pen(Color.FromArgb(150, 150, 150)) { DashStyle = DashStyle.Dash })
                g.DrawLine(guide, hx, plot.Top, hx, plot.Bottom);

            float by = Y((double)pts[idx].Balance);
            using (var bb = new SolidBrush(Color.FromArgb(60, 60, 60)))
                g.FillEllipse(bb, hx - 3.5f, by - 3.5f, 7f, 7f);
            if (_showCategories)
            {
                for (int s = 0; s < _series.Categories.Count; s++)
                {
                    var cs = _series.Categories[s];
                    if (cs.Values.Count != n || cs.Values[idx] == 0m) continue;
                    float cy = Y((double)cs.Values[idx]);
                    using var cb = new SolidBrush(CategoryColor(s));
                    g.FillEllipse(cb, hx - 3f, cy - 3f, 6f, 6f);
                }
            }

            // Tooltip rows: date, balance, then each category value at this day.
            var rows = new List<(Color? swatch, string text)>
            {
                (null, pts[idx].Date.ToString("yyyy-MM-dd")),
                (null, $"{Loc.T("Balance")}: {pts[idx].Balance:N2}")
            };
            if (_showCategories)
            {
                var catRows = new List<(decimal value, Color color, string text)>();
                for (int s = 0; s < _series.Categories.Count; s++)
                {
                    var cs = _series.Categories[s];
                    if (cs.Values.Count != n || cs.Values[idx] == 0m) continue;
                    string label = cs.IsInterest ? Loc.T("Gained interest") : cs.IsUncategorized ? Loc.T("(uncategorized)") : cs.Name;
                    catRows.Add((cs.Values[idx], CategoryColor(s), $"{label}: {cs.Values[idx]:N2}"));
                }

                // Largest amounts first (by magnitude) at the hovered day.
                foreach (var cr in catRows.OrderByDescending(cr => cr.value))
                    rows.Add((cr.color, cr.text));
            }

            using var headFont = new Font(Font, FontStyle.Bold);
            const int pad = 8, rowH = 16, swatchW = 14;

            int contentW = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                var f = i == 0 ? headFont : Font;
                int w = TextRenderer.MeasureText(g, rows[i].text, f).Width + (rows[i].swatch is null ? 0 : swatchW);
                contentW = Math.Max(contentW, w);
            }

            int boxW = contentW + pad * 2;
            int boxH = pad * 2 + rowH * rows.Count;
            int bx = _mouse.X + 16;
            if (bx + boxW > Width) bx = _mouse.X - 16 - boxW;
            if (bx < 2) bx = 2;
            int byy = _mouse.Y + 16;
            if (byy + boxH > Height) byy = Math.Max(2, Height - boxH - 2);

            using (var bg = new SolidBrush(Color.FromArgb(250, 255, 255, 255)))
            using (var border = new Pen(Color.FromArgb(200, 200, 200)))
            {
                g.FillRectangle(bg, bx, byy, boxW, boxH);
                g.DrawRectangle(border, bx, byy, boxW, boxH);
            }

            int ty = byy + pad;
            for (int i = 0; i < rows.Count; i++)
            {
                int tx = bx + pad;
                if (rows[i].swatch is Color c)
                {
                    using var sb = new SolidBrush(c);
                    g.FillRectangle(sb, tx, ty + 3, 10, 8);
                    tx += swatchW;
                }
                TextRenderer.DrawText(g, rows[i].text, i == 0 ? headFont : Font, new Point(tx, ty), Color.FromArgb(40, 40, 40));
                ty += rowH;
            }
        }

        /// <summary>Rounds the raw step for a range to a "nice" value (1, 2 or 5 × 10ⁿ).</summary>
        private static double NiceStep(double range, int targetTicks)
        {
            if (range <= 0 || targetTicks < 1) return 1;
            double raw = range / targetTicks;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double norm = raw / magnitude;
            double niceNorm = norm <= 1.5 ? 1 : norm <= 3 ? 2 : norm <= 7 ? 5 : 10;
            return niceNorm * magnitude;
        }
    }
}
