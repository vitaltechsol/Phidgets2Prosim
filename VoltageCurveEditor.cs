using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    // Visual editor for a Voltage Input curve. Output is always 0-255; the input axis is the sensor range.
    // Two points = a simple linear range. Click the line to add points, drag to shape, right-click to remove.
    internal class VoltageCurveEditor : UserControl
    {
        private const double OutMax = 255;
        private const double Eps = 0.001;
        private const int HitRadius = 9;

        private static readonly Color Green = Color.FromArgb(16, 185, 129);
        private static readonly Color GreenDark = Color.FromArgb(5, 150, 105);
        private static readonly Color Gray = Color.FromArgb(107, 114, 128);
        private static readonly Color Orange = Color.FromArgb(234, 88, 12);

        private readonly List<double> _xs = new List<double> { 0, 1 };
        private readonly List<double> _ys = new List<double> { 0, OutMax };
        private int _selected = -1, _drag = -1, _hover = -1;
        private double? _live;
        private bool _updating;
        private double _axisMax = 1;
        private Rectangle _plot;

        private readonly NumericUpDown nudMin, nudMax, nudSelX, nudSelY;
        private readonly CheckBox chkInvert;
        private readonly Button btnReset, btnAdd, btnRemove;
        private readonly Label lblHint;
        private readonly Label[] _selRowLabels;

        public event EventHandler PointsChanged;

        public InterpolationMode Mode { get => _mode; set { _mode = value; Invalidate(); } }
        private InterpolationMode _mode = InterpolationMode.Linear;

        public double CurvePower { get => _power; set { _power = value; Invalidate(); } }
        private double _power = 2.0;

        // Full range of the sensor (1 for ratio, 5 for voltage range)
        public double AxisMax { get => _axisMax; set { _axisMax = value; Invalidate(); } }

        public double[] InputPoints => _xs.ToArray();
        public double[] OutputPoints => _ys.ToArray();

        public VoltageCurveEditor()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Font = new Font("Segoe UI", 8.5F);
            Size = new Size(390, 300);

            Label L(string t, int x, int w) => new Label { Text = t, AutoSize = false, Location = new Point(x, 0), Width = w, Height = 18, ForeColor = Gray };
            NumericUpDown N(int x, int w, int dec, decimal min, decimal max, decimal inc) =>
                new NumericUpDown { Location = new Point(x, 0), Width = w, DecimalPlaces = dec, Minimum = min, Maximum = max, Increment = inc };
            Button B(string t, int x, int w) => new Button { Text = t, Location = new Point(x, 0), Size = new Size(w, 24), FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = true };

            // Top row: min / max / invert / reset
            var lMin = L("Min", 0, 32); lMin.Top = 7;
            nudMin = N(34, 72, 3, -10, 10, 0.01M); nudMin.Top = 3;
            var lMax = L("Max", 114, 34); lMax.Top = 7;
            nudMax = N(150, 72, 3, -10, 10, 0.01M); nudMax.Top = 3;
            chkInvert = new CheckBox { Text = "Invert", AutoSize = true, Location = new Point(230, 5) };
            btnReset = B("Reset linear", 292, 90); btnReset.Top = 2;
            btnReset.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Selected-point row (positioned in Relayout)
            var lSel = L("Point:", 0, 38);
            var lIn = L("In", 40, 18);
            nudSelX = N(58, 68, 3, -10, 10, 0.01M);
            var lOut = L("Out", 130, 34);
            nudSelY = N(166, 54, 0, 0, 255, 1);
            btnAdd = B("+ Add point", 222, 84);
            btnRemove = B("Remove", 310, 72);
            _selRowLabels = new[] { lSel, lIn, lOut };

            lblHint = new Label { AutoSize = false, Height = 16, ForeColor = Gray, Font = new Font("Segoe UI", 8F), Location = new Point(0, 0) };

            Controls.AddRange(new Control[] { lMin, nudMin, lMax, nudMax, chkInvert, btnReset, lSel, lIn, nudSelX, lOut, nudSelY, btnAdd, btnRemove, lblHint });

            nudMin.ValueChanged += (s, e) => { if (_updating) return; MoveTo(0, (double)nudMin.Value, 0); Changed(); };
            nudMax.ValueChanged += (s, e) => { if (_updating) return; MoveTo(_xs.Count - 1, (double)nudMax.Value, 0); Changed(); };
            nudSelX.ValueChanged += (s, e) => { if (_updating || !SelValid) return; MoveTo(_selected, (double)nudSelX.Value, _ys[_selected]); Changed(); };
            nudSelY.ValueChanged += (s, e) => { if (_updating || !SelValid) return; _ys[_selected] = (double)nudSelY.Value; Changed(); };
            chkInvert.CheckedChanged += (s, e) =>
            {
                if (_updating) return;
                for (int i = 0; i < _ys.Count; i++) _ys[i] = OutMax - _ys[i];
                Changed();
            };
            btnReset.Click += (s, e) =>
            {
                _xs.Clear(); _ys.Clear();
                _xs.Add(0); _xs.Add(_axisMax); _ys.Add(0); _ys.Add(OutMax);
                _selected = -1;
                Changed();
            };
            btnAdd.Click += (s, e) => AddPointInLargestGap();
            btnRemove.Click += (s, e) => RemovePoint(_selected);

            Relayout();
            Sync();
        }

        private bool SelValid => _selected >= 0 && _selected < _xs.Count;

        // Programmatic load (does not raise PointsChanged)
        public void SetPoints(IList<double> xs, IList<double> ys)
        {
            if (xs == null || ys == null || xs.Count < 2 || xs.Count != ys.Count) return;
            var pairs = xs.Zip(ys, (x, y) => new { x, y }).OrderBy(p => p.x).ToList();
            _xs.Clear(); _ys.Clear();
            foreach (var p in pairs) { _xs.Add(p.x); _ys.Add(p.y); }
            _selected = -1;
            Sync();
            Invalidate();
        }

        // Shows the live sensor reading (raw input value) on the curve; null hides it
        public void SetLive(double? raw)
        {
            _live = raw;
            Invalidate(_plot);
        }

        private void Changed()
        {
            Sync();
            Invalidate();
            PointsChanged?.Invoke(this, EventArgs.Empty);
        }

        // ── Layout ────────────────────────────────────────────────────────────────
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout();
        }

        private void Relayout()
        {
            if (lblHint == null) return;
            int plotH = Math.Max(60, Height - 34 - 18 - 34 - 20);
            _plot = new Rectangle(38, 34, Math.Max(40, Width - 38 - 14), plotH);
            int selY = _plot.Bottom + 22;
            foreach (var c in new Control[] { nudSelX, nudSelY, btnAdd, btnRemove }) c.Top = selY;
            foreach (var l in _selRowLabels) l.Top = selY + 4;
            btnAdd.Top = btnRemove.Top = selY - 1;
            lblHint.SetBounds(0, selY + 30, Width, 16);
            Invalidate();
        }

        private void Sync()
        {
            _updating = true;
            try
            {
                int n = _xs.Count;
                SetNud(nudMin, _xs[0]);
                SetNud(nudMax, _xs[n - 1]);
                chkInvert.Checked = _ys[0] > _ys[n - 1];
                bool sel = SelValid;
                nudSelX.Enabled = nudSelY.Enabled = sel;
                btnRemove.Enabled = sel && _selected > 0 && _selected < n - 1;
                SetNud(nudSelX, sel ? _xs[_selected] : 0);
                SetNud(nudSelY, sel ? _ys[_selected] : 0);
                lblHint.Text = n == 2
                    ? "Linear range. Click the line to add points and shape the curve."
                    : "Drag points to shape · click the line to add · right-click a point to remove.";
            }
            finally { _updating = false; }
        }

        private static void SetNud(NumericUpDown nud, double v)
        {
            decimal d = (decimal)Math.Round(v, nud.DecimalPlaces);
            nud.Value = Math.Max(nud.Minimum, Math.Min(nud.Maximum, d));
        }

        // ── Geometry ──────────────────────────────────────────────────────────────
        private void Axis(out double min, out double max)
        {
            min = Math.Min(0, _xs[0]);
            max = Math.Max(_axisMax, _xs[_xs.Count - 1]);
            if (max - min < Eps) max = min + 1;
        }

        private float XToPx(double x) { Axis(out double a, out double b); return (float)(_plot.Left + (x - a) / (b - a) * _plot.Width); }
        private float YToPx(double y) => (float)(_plot.Bottom - Math.Max(0, Math.Min(OutMax, y)) / OutMax * _plot.Height);
        private double PxToX(float px) { Axis(out double a, out double b); return a + (px - _plot.Left) / _plot.Width * (b - a); }
        private double PxToY(float py) => (_plot.Bottom - py) / _plot.Height * OutMax;

        private double Eval(double x)
        {
            try { return VoltageInterpolator.Interpolate(x, _xs.ToArray(), _ys.ToArray(), _mode, _power); }
            catch { return 0; }
        }

        private PointF PointPx(int i) => new PointF(XToPx(_xs[i]), YToPx(_ys[i]));

        private int HitPoint(Point p)
        {
            int best = -1; double bestD = HitRadius;
            for (int i = 0; i < _xs.Count; i++)
            {
                var c = PointPx(i);
                double d = Math.Sqrt((c.X - p.X) * (c.X - p.X) + (c.Y - p.Y) * (c.Y - p.Y));
                if (d <= bestD) { bestD = d; best = i; }
            }
            return best;
        }

        private bool NearCurve(Point p)
        {
            if (!_plot.Contains(p)) return false;
            return Math.Abs(YToPx(Eval(PxToX(p.X))) - p.Y) <= 10;
        }

        // ── Editing ───────────────────────────────────────────────────────────────
        private void MoveTo(int i, double x, double y)
        {
            int n = _xs.Count;
            double lo = i == 0 ? Math.Min(0, _xs[0]) : _xs[i - 1] + Eps;
            double hi = i == n - 1 ? Math.Max(_axisMax, _xs[i]) : _xs[i + 1] - Eps;
            _xs[i] = Math.Round(Math.Max(lo, Math.Min(hi, x)), 3);
            if (i > 0 && i < n - 1) _ys[i] = Math.Round(Math.Max(0, Math.Min(OutMax, y)));
        }

        private int InsertPoint(double x)
        {
            x = Math.Round(x, 3);
            for (int i = 1; i < _xs.Count; i++)
            {
                if (x < _xs[i])
                {
                    if (x - _xs[i - 1] < Eps || _xs[i] - x < Eps) return -1;
                    _xs.Insert(i, x);
                    _ys.Insert(i, Math.Round(Math.Max(0, Math.Min(OutMax, Eval(x)))));
                    return i;
                }
            }
            return -1;
        }

        private void AddPointInLargestGap()
        {
            int gap = 0; double best = -1;
            for (int i = 0; i < _xs.Count - 1; i++)
                if (_xs[i + 1] - _xs[i] > best) { best = _xs[i + 1] - _xs[i]; gap = i; }
            int idx = InsertPoint((_xs[gap] + _xs[gap + 1]) / 2);
            if (idx < 0) return;
            _selected = idx;
            Changed();
        }

        private void RemovePoint(int i)
        {
            if (i <= 0 || i >= _xs.Count - 1) return;
            _xs.RemoveAt(i); _ys.RemoveAt(i);
            _selected = -1; _hover = -1;
            Changed();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            int hit = HitPoint(e.Location);
            if (e.Button == MouseButtons.Right) { RemovePoint(hit); return; }
            if (e.Button != MouseButtons.Left) return;

            if (hit < 0 && NearCurve(e.Location)) hit = InsertPoint(PxToX(e.X));
            _selected = hit;
            _drag = hit;
            Changed();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_drag >= 0)
            {
                MoveTo(_drag, PxToX(e.X), PxToY(e.Y));
                Changed();
                return;
            }
            int hit = HitPoint(e.Location);
            Cursor = hit >= 0 ? Cursors.Hand : NearCurve(e.Location) ? Cursors.Cross : Cursors.Default;
            if (hit != _hover) { _hover = hit; Invalidate(_plot); }
        }

        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _drag = -1; }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hover != -1) { _hover = -1; Invalidate(_plot); }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Delete) RemovePoint(_selected);
        }

        // ── Painting ──────────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Axis(out double axMin, out double axMax);

            g.FillRectangle(Brushes.White, _plot);
            using (var grid = new Pen(Color.FromArgb(235, 237, 240)))
            using (var txt = new SolidBrush(Gray))
            {
                var right = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                foreach (int v in new[] { 0, 64, 128, 192, 255 })
                {
                    float y = YToPx(v);
                    g.DrawLine(grid, _plot.Left, y, _plot.Right, y);
                    g.DrawString(v.ToString(), Font, txt, new RectangleF(0, y - 8, _plot.Left - 4, 16), right);
                }
                var center = new StringFormat { Alignment = StringAlignment.Center };
                for (int i = 0; i <= 4; i++)
                {
                    double xv = axMin + (axMax - axMin) * i / 4.0;
                    float x = XToPx(xv);
                    g.DrawLine(grid, x, _plot.Top, x, _plot.Bottom);
                    g.DrawString(xv.ToString("0.##"), Font, txt, new RectangleF(x - 25, _plot.Bottom + 3, 50, 16), center);
                }
            }
            using (var border = new Pen(Color.FromArgb(209, 213, 219))) g.DrawRectangle(border, _plot);

            // curve
            int steps = Math.Max(10, _plot.Width / 2);
            var xs = _xs.ToArray(); var ys = _ys.ToArray();
            var pts = new PointF[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                double x = axMin + (axMax - axMin) * i / steps;
                double y;
                try { y = VoltageInterpolator.Interpolate(x, xs, ys, _mode, _power); } catch { y = 0; }
                y = Math.Max(-20, Math.Min(OutMax + 20, y));
                pts[i] = new PointF(XToPx(x), (float)(_plot.Bottom - y / OutMax * _plot.Height));
            }
            var state = g.Save();
            g.SetClip(_plot);
            var fill = new List<PointF>(pts) { new PointF(_plot.Right, _plot.Bottom), new PointF(_plot.Left, _plot.Bottom) };
            using (var br = new SolidBrush(Color.FromArgb(32, Green))) g.FillPolygon(br, fill.ToArray());
            using (var pen = new Pen(Green, 2.5f) { LineJoin = LineJoin.Round }) g.DrawLines(pen, pts);

            // live marker
            if (_live.HasValue)
            {
                float lx = XToPx(_live.Value);
                if (lx >= _plot.Left && lx <= _plot.Right)
                {
                    using (var pen = new Pen(Orange, 1.5f) { DashStyle = DashStyle.Dash }) g.DrawLine(pen, lx, _plot.Top, lx, _plot.Bottom);
                    float ly = YToPx(Eval(_live.Value));
                    using (var br = new SolidBrush(Orange)) g.FillEllipse(br, lx - 5, ly - 5, 10, 10);
                }
            }
            g.Restore(state);
            // points
            for (int i = 0; i < _xs.Count; i++)
            {
                var c = PointPx(i);
                bool sel = i == _selected, hot = i == _hover || i == _drag;
                float r = sel || hot ? 7 : 6;
                using (var br = new SolidBrush(sel ? Green : Color.White)) g.FillEllipse(br, c.X - r, c.Y - r, r * 2, r * 2);
                using (var pen = new Pen(hot ? GreenDark : Green, 2f)) g.DrawEllipse(pen, c.X - r, c.Y - r, r * 2, r * 2);
            }

            // readouts
            using (var txt = new SolidBrush(Gray))
            {
                int shown = _drag >= 0 ? _drag : _hover >= 0 ? _hover : _selected;
                if (shown >= 0 && shown < _xs.Count)
                    g.DrawString($"In {_xs[shown]:0.###}  →  Out {_ys[shown]:0} ({_ys[shown] / OutMax * 100:0.0}%)", Font, txt, _plot.Left + 6, _plot.Top + 4);
            }
            if (_live.HasValue)
            {
                using (var br = new SolidBrush(Orange))
                {
                    var far = new StringFormat { Alignment = StringAlignment.Far };
                    g.DrawString($"Live: {_live.Value:0.###}  →  {Eval(_live.Value):0} ({Eval(_live.Value) / OutMax * 100:0.0}%)", Font, br,
                        new RectangleF(_plot.Left, _plot.Bottom - 20, _plot.Width - 6, 16), far);
                }
            }
        }
    }
}
