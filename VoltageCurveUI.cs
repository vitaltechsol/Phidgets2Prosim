using System;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    // Hosts the curve editor in the Voltage In panel. The (hidden) point text boxes stay the source of truth
    // for load/save, so the editor and the config code stay in sync through them.
    public partial class Form1
    {
        private VoltageCurveEditor curveEditor;
        private bool _curveSync;

        private void InitializeVoltageCurveEditor()
        {
            curveEditor = new VoltageCurveEditor
            {
                Location = new System.Drawing.Point(5, 242),
                Size = new System.Drawing.Size(385, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Tag = "in",
            };
            pnlAdvancedProperties_Voltages.Controls.Add(curveEditor);

            curveEditor.PointsChanged += (s, e) =>
            {
                _curveSync = true;
                try
                {
                    txtVoltageInputPoints.Text = JoinPoints(curveEditor.InputPoints);
                    txtVoltageOutputPoints.Text = JoinPoints(curveEditor.OutputPoints);
                }
                finally { _curveSync = false; }
                RefreshVoltagePreview();
            };
            EventHandler fromText = (s, e) => { if (!_curveSync) ApplyPointsTextToEditor(); };
            txtVoltageInputPoints.TextChanged += fromText;
            txtVoltageOutputPoints.TextChanged += fromText;

            cboVoltageInterpolation.TextChanged += (s, e) => ApplyCurveSettings();
            txtVoltageCurvePower.ValueChanged += (s, e) => ApplyCurveSettings();
            chkVoltageUseRange.CheckedChanged += (s, e) => ApplyCurveSettings();

            ApplyCurveSettings();
            ApplyPointsTextToEditor();
        }

        private void ApplyPointsTextToEditor()
        {
            if (TryParsePoints(txtVoltageInputPoints.Text, out var xs) && TryParsePoints(txtVoltageOutputPoints.Text, out var ys)
                && xs.Count >= 2 && xs.Count == ys.Count)
                curveEditor.SetPoints(xs, ys);
        }

        private void ApplyCurveSettings()
        {
            if (!Enum.TryParse(cboVoltageInterpolation.Text, true, out InterpolationMode mode)) mode = InterpolationMode.Linear;
            curveEditor.Mode = mode;
            // Power exponent is only relevant (and only shown) in Curve mode
            bool curve = mode == InterpolationMode.Curve && pnlAdvancedProperties_Voltages.Visible && _activeDeviceCategory != "Voltage Out";
            foreach (Control c in pnlAdvancedProperties_Voltages.Controls)
                if ((string)c.Tag == "in-curve") c.Visible = curve;
            curveEditor.CurvePower = (double)txtVoltageCurvePower.Value;
            curveEditor.AxisMax = chkVoltageUseRange.Checked ? 5 : 1;
        }
    }
}
