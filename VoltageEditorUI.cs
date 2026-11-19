using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    // Voltage Out / Voltage In editor fields (shared panel, rows shown per category)
    public partial class Form1
    {
        private sealed class VoltageInputFields
        {
            public bool UseRange;
            public List<double> InputPoints, OutputPoints;
            public InterpolationMode Mode;
            public double MinChange, CurvePower;
            public int Interval;
        }

        private Dictionary<Control, int> _voltageOrigTop;

        private static void SetNud(NumericUpDown nud, double value)
        {
            decimal v = (decimal)value;
            nud.Value = Math.Max(nud.Minimum, Math.Min(nud.Maximum, v));
        }

        private static string JoinPoints(IEnumerable<double> pts) =>
            string.Join(",", (pts ?? new List<double>()).Select(d => d.ToString(CultureInfo.InvariantCulture)));

        private static bool TryParsePoints(string text, out List<double> result)
        {
            result = new List<double>();
            foreach (var part in text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!double.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return false;
                result.Add(d);
            }
            return true;
        }

        // Show only the rows that apply to Voltage Out or Voltage In and close the gaps
        private void ReflowVoltageFields(bool isOutput)
        {
            var controls = pnlAdvancedProperties_Voltages.Controls.Cast<Control>().ToList();
            if (_voltageOrigTop == null) _voltageOrigTop = controls.ToDictionary(c => c, c => c.Top);

            string show = isOutput ? "out" : "in";
            foreach (var c in controls) c.Visible = c.Tag == null || (string)c.Tag == show;

            // original rows are 30px apart; control tops are offset by 2-5px, so bucket by (top - 2) / 30
            int newRow = 0;
            foreach (var group in controls.GroupBy(c => (_voltageOrigTop[c] - 2) / 30).OrderBy(g => g.Key))
            {
                if (!group.Any(c => c.Visible)) continue;
                foreach (var c in group) c.Top = _voltageOrigTop[c] + (newRow - group.Key) * 30;
                newRow++;
            }
        }

        private void ResetVoltageFields()
        {
            chkVoltageUseSinCos.Checked = false;
            chkVoltageUseRange.Checked = false;
            chkVoltageWrap360.Checked = false;
            txtVoltageInputPoints.Text = "0,0.2,0.4,0.8,1";
            txtVoltageOutputPoints.Text = "0,100,500,800,1024";
            cboVoltageInterpolation.Text = "Linear";
            txtVoltageMinChange.Value = 0.002M;
            txtVoltageInterval.Value = 50M;
            txtVoltageCurvePower.Value = 2.0M;
            txtVoltageScale.Value = 500M;
            txtVoltageOffset.Value = 0M;
            txtVoltageAmplitude.Value = 10M;
        }

        // ── Voltage Out ───────────────────────────────────────────────────────────
        private void LoadVoltageOutputFields(PhidgetsVoltageOutputInst v)
        {
            SetNud(txtVoltageScale, v.ScaleFactor);
            SetNud(txtVoltageOffset, v.Offset);
            SetNud(txtVoltageInterval, v.Interval);
            SetNud(txtVoltageAmplitude, v.AmplitudeVolts);
            chkVoltageUseSinCos.Checked = v.UseSinCos;
            chkVoltageWrap360.Checked = v.WrapDegrees360;
        }

        private void ReadVoltageOutputFields(PhidgetsVoltageOutputInst v)
        {
            v.ScaleFactor = (double)txtVoltageScale.Value;
            v.Offset = (double)txtVoltageOffset.Value;
            v.Interval = (int)txtVoltageInterval.Value;
            v.AmplitudeVolts = (double)txtVoltageAmplitude.Value;
            v.UseSinCos = chkVoltageUseSinCos.Checked;
            v.WrapDegrees360 = chkVoltageWrap360.Checked;
        }

        // ── Voltage In ────────────────────────────────────────────────────────────
        private void LoadVoltageInputFields(PhidgetsVoltageInputInst vi)
        {
            chkVoltageUseRange.Checked = vi.UseRange;
            txtVoltageInputPoints.Text = JoinPoints(vi.InputPoints);
            txtVoltageOutputPoints.Text = JoinPoints(vi.OutputPoints);
            cboVoltageInterpolation.Text = vi.InterpolationMode.ToString();
            SetNud(txtVoltageMinChange, vi.MinChangeTriggerValue);
            SetNud(txtVoltageInterval, vi.DataInterval);
            SetNud(txtVoltageCurvePower, vi.CurvePower);
        }

        private bool TryParseVoltageInputFields(out VoltageInputFields f)
        {
            f = null;
            if (!TryParsePoints(txtVoltageInputPoints.Text, out var inPts) || !TryParsePoints(txtVoltageOutputPoints.Text, out var outPts)
                || inPts.Count < 2 || inPts.Count != outPts.Count)
            {
                MessageBox.Show("Input points and output points must be comma-separated numbers, at least 2, with the same count.",
                    "Voltage Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!Enum.TryParse(cboVoltageInterpolation.Text, true, out InterpolationMode mode)) mode = InterpolationMode.Linear;

            f = new VoltageInputFields
            {
                UseRange = chkVoltageUseRange.Checked,
                InputPoints = inPts,
                OutputPoints = outPts,
                Mode = mode,
                MinChange = (double)txtVoltageMinChange.Value,
                CurvePower = (double)txtVoltageCurvePower.Value,
                Interval = (int)txtVoltageInterval.Value,
            };
            return true;
        }

        private static void ApplyVoltageInputFields(PhidgetsVoltageInputInst vi, VoltageInputFields f)
        {
            vi.UseRange = f.UseRange;
            vi.InputPoints = f.InputPoints;
            vi.OutputPoints = f.OutputPoints;
            vi.InterpolationMode = f.Mode;
            vi.MinChangeTriggerValue = f.MinChange;
            vi.CurvePower = f.CurvePower;
            vi.DataInterval = f.Interval;
        }
    }
}
