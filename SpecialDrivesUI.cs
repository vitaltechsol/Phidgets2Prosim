using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    // "Special Drives" page: Trim Wheel and Parking Brake (single-instance config sections)
    public partial class Form1
    {
        private const string TrimTab  = "Trim Wheel";
        private const string BrakeTab = "Parking Brake";

        private string _activeSpecialTab = TrimTab;
        private bool _specialPageInitialized;

        private void WireSpecialPage()
        {
            foreach (var b in flowSpecialTabs.Controls.OfType<Button>())
                b.Click += SpecialTab_Click;
            btnSaveSpecial.Click   += BtnSaveSpecial_Click;
            btnDeleteSpecial.Click += BtnDeleteSpecial_Click;
        }

        private void OnSpecialPageShown()
        {
            if (!_specialPageInitialized)
            {
                _specialPageInitialized = true;
                flowSpecialTabs.Controls.OfType<Button>().First().PerformClick();
                return;
            }
            LoadSpecialEditor();
        }

        private void SpecialTab_Click(object sender, EventArgs e)
        {
            if (!(sender is Button btn)) return;
            _activeSpecialTab = btn.Tag?.ToString() ?? btn.Text;

            foreach (var cb in flowSpecialTabs.Controls.OfType<Button>())
            {
                cb.BackColor = Color.FromArgb(241, 245, 249);
                cb.ForeColor = Color.FromArgb(51, 65, 85);
            }
            btn.BackColor = Color.FromArgb(59, 130, 246);
            btn.ForeColor = Color.White;

            pnlSpecialTrim.Visible  = _activeSpecialTab == TrimTab;
            pnlSpecialBrake.Visible = _activeSpecialTab == BrakeTab;
            LoadSpecialEditor();
        }

        private static string HubSerialText(List<PhidgetsHubInst> hubs, int serial)
        {
            var hub = hubs.FirstOrDefault(h => h.Serial == serial);
            return hub != null ? HubText(hub) : $"Unknown ({serial})";
        }

        // Hub combo text is "Name (Serial)"; the serial is always the trailing number
        private static int HubSerialFrom(ComboBox cbo)
        {
            var m = Regex.Match(cbo.Text ?? "", @"\((\d+)\)\s*$");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        private void FillSpecialHub(ComboBox cbo, List<PhidgetsHubInst> hubs, int serial)
        {
            PopulateHubDropdown(cbo, hubs);
            if (serial == 0) return;
            string text = HubSerialText(hubs, serial);
            if (!cbo.Items.Contains(text)) cbo.Items.Add(text);
            cbo.SelectedItem = text;
        }

        private void LoadSpecialEditor()
        {
            try
            {
                var cfg = _config.Load();
                var hubs = _hubs.GetAll();

                if (_activeSpecialTab == TrimTab)
                {
                    var t = cfg?.CustomTrimWheelInstance;
                    var v = t ?? new CustomTrimWheelInst();
                    lblSpecialTitle.Text = t == null ? "Trim Wheel — not configured" : "Trim Wheel";

                    FillSpecialHub(cboTrimHub, hubs, v.Serial);
                    nudTrimHubPort.Value = ClampToRange(v.HubPort, nudTrimHubPort.Minimum, nudTrimHubPort.Maximum);
                    chkTrimReversed.Checked   = v.Reversed;
                    chkTrimAccelerate.Checked = v.Accelerate;
                    SetSpecialNud(txtTrimDirtyUp, t == null ? 1 : v.DirtyUp);
                    SetSpecialNud(txtTrimDirtyDown, t == null ? 1 : v.DirtyDown);
                    SetSpecialNud(txtTrimCleanUp, t == null ? 1 : v.CleanUp);
                    SetSpecialNud(txtTrimCleanDown, t == null ? 1 : v.CleanDown);
                    SetSpecialNud(txtTrimAPClean, t == null ? 1 : v.APOnClean);
                    SetSpecialNud(txtTrimAPDirty, t == null ? 1 : v.APOnDirty);
                    var range = v.Range != null && v.Range.Count >= 2 ? v.Range : new List<double> { -1, 1 };
                    SetSpecialNud(nudTrimRangeMin, range[0]);
                    SetSpecialNud(nudTrimRangeMax, range[1]);

                    var enc = v.Encoder;
                    chkTrimEncoder.Checked = enc != null;
                    FillSpecialHub(cboTrimEncHub, hubs, enc?.Serial ?? 0);
                    nudTrimEncHubPort.Value = ClampToRange(enc?.HubPort ?? 0, nudTrimEncHubPort.Minimum, nudTrimEncHubPort.Maximum);
                    nudTrimEncChannel.Value = ClampToRange(enc?.Channel ?? 0, nudTrimEncChannel.Minimum, nudTrimEncChannel.Maximum);
                    SetSpecialNud(nudTrimEncScale, enc?.ScaleFactor ?? 1);
                    chkTrimEncoder.Checked = enc != null; // re-apply to refresh enabled state
                    bool on = enc != null;
                    cboTrimEncHub.Enabled = nudTrimEncHubPort.Enabled = nudTrimEncChannel.Enabled = nudTrimEncScale.Enabled = on;
                }
                else
                {
                    var b = cfg?.CustomParkingBrakeInstance;
                    var v = b ?? new CustomParkingBrakeInst();
                    lblSpecialTitle.Text = b == null ? "Parking Brake — not configured" : "Parking Brake";

                    txtBrakeSwitchVar.Text  = v.SwitchVariable ?? "";
                    txtBrakeRelayVar.Text   = v.RelayVariable ?? "";
                    txtBrakeReleaseVar.Text = v.ReleaseVariable ?? "";
                    SetSpecialNud(txtBrakeToeThresh, v.ToeBrakeThreshold);
                    SetSpecialNud(txtBrakeReleaseThresh, v.ReleaseToeBrakeThreshold);
                    SetSpecialNud(txtBrakeReleaseDelay, v.ReleaseDelayOnMs);
                    SetSpecialNud(txtBrakeReleaseMaxTime, v.ReleaseMaxTimeOnMs);
                }
            }
            catch (Exception ex) { DisplayErrorLog("Error loading special drive: " + ex.Message); }
        }

        private static void SetSpecialNud(NumericUpDown nud, double value) =>
            nud.Value = ClampToRange((decimal)value, nud.Minimum, nud.Maximum);

        private void BtnSaveSpecial_Click(object sender, EventArgs e)
        {
            try
            {
                var cfg = _config.Load() ?? new Config();

                if (_activeSpecialTab == TrimTab)
                {
                    if (nudTrimRangeMin.Value >= nudTrimRangeMax.Value)
                    {
                        MessageBox.Show("Range Min must be lower than Range Max.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Edit in place so properties the form does not expose are kept
                    var t = cfg.CustomTrimWheelInstance ?? new CustomTrimWheelInst();
                    t.Serial = HubSerialFrom(cboTrimHub);
                    t.HubPort = (int)nudTrimHubPort.Value;
                    t.Reversed = chkTrimReversed.Checked;
                    t.Accelerate = chkTrimAccelerate.Checked;
                    t.DirtyUp = (double)txtTrimDirtyUp.Value;
                    t.DirtyDown = (double)txtTrimDirtyDown.Value;
                    t.CleanUp = (double)txtTrimCleanUp.Value;
                    t.CleanDown = (double)txtTrimCleanDown.Value;
                    t.APOnClean = (double)txtTrimAPClean.Value;
                    t.APOnDirty = (double)txtTrimAPDirty.Value;
                    t.Range = new List<double> { (double)nudTrimRangeMin.Value, (double)nudTrimRangeMax.Value };

                    if (chkTrimEncoder.Checked)
                    {
                        var enc = t.Encoder ?? new PhidgetsEncoderInst();
                        enc.Serial = HubSerialFrom(cboTrimEncHub);
                        enc.HubPort = (int)nudTrimEncHubPort.Value;
                        enc.Channel = (int)nudTrimEncChannel.Value;
                        enc.ScaleFactor = (double)nudTrimEncScale.Value;
                        t.Encoder = enc;
                    }
                    else t.Encoder = null;

                    cfg.CustomTrimWheelInstance = t;
                }
                else
                {
                    var b = cfg.CustomParkingBrakeInstance ?? new CustomParkingBrakeInst();
                    b.SwitchVariable = txtBrakeSwitchVar.Text.NullIfEmpty();
                    b.RelayVariable = txtBrakeRelayVar.Text.NullIfEmpty();
                    b.ReleaseVariable = txtBrakeReleaseVar.Text.NullIfEmpty();
                    b.ToeBrakeThreshold = (int)txtBrakeToeThresh.Value;
                    b.ReleaseToeBrakeThreshold = (int)txtBrakeReleaseThresh.Value;
                    b.ReleaseDelayOnMs = (int)txtBrakeReleaseDelay.Value;
                    b.ReleaseMaxTimeOnMs = (int)txtBrakeReleaseMaxTime.Value;
                    cfg.CustomParkingBrakeInstance = b;
                }

                WriteConfig(cfg);
                DisplayInfoLog(_activeSpecialTab + " saved. Restart the app to apply it.");
                LoadSpecialEditor();
            }
            catch (Exception ex) { DisplayErrorLog("Save error: " + ex.Message); }
        }

        private void BtnDeleteSpecial_Click(object sender, EventArgs e)
        {
            try
            {
                var cfg = _config.Load();
                bool exists = _activeSpecialTab == TrimTab
                    ? cfg?.CustomTrimWheelInstance != null
                    : cfg?.CustomParkingBrakeInstance != null;
                if (!exists) return;

                if (MessageBox.Show("Remove the " + _activeSpecialTab + " configuration?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                if (_activeSpecialTab == TrimTab) cfg.CustomTrimWheelInstance = null;
                else cfg.CustomParkingBrakeInstance = null;

                WriteConfig(cfg);
                DisplayInfoLog(_activeSpecialTab + " removed. Restart the app to apply it.");
                LoadSpecialEditor();
            }
            catch (Exception ex) { DisplayErrorLog("Delete error: " + ex.Message); }
        }
    }
}
