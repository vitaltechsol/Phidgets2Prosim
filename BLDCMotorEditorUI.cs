using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    // "BL DC Motors" category of the I/O Mappings page (PhidgetsBLDCMotorInstances)
    public partial class Form1
    {
        private const string BldcMotorCategory = "BL DC Motors";

        private Panel pnlAdvancedProperties_BLDCMotors;
        private TextBox txtBldcRefTurnOn, txtBldcRefTurnOn2, txtBldcRefCurrentPos, txtBldcRefTargetPos;
        private NumericUpDown nudBldcAccel, nudBldcOffset;
        private CheckBox chkBldcReversed;
        private readonly Dictionary<string, TextBox> _bldcTuning = new Dictionary<string, TextBox>();

        // ── UI construction ───────────────────────────────────────────────────────
        private Panel BuildBldcMotorEditor()
        {
            var host = new Panel { Dock = DockStyle.Fill, Visible = false, AutoScroll = true };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(0, 0, 14, 0),
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Hub and hub port come from the common form above (cboDeviceHub / nudDeviceHubPort)

            DcSection(grid, "ProSim", "DataRefs that drive the motor");
            txtBldcRefTurnOn = new TextBox();
            DcRow(grid, "Turn-on ref", txtBldcRefTurnOn, "ProSim ref that activates the motor (RefTurnOn), e.g. system.gates.B_THROTTLE_SERVO_POWER_RIGHT.");
            txtBldcRefTurnOn2 = new TextBox();
            DcRow(grid, "Turn-on ref 2 (optional)", txtBldcRefTurnOn2, "Second ref that can also activate the motor (RefTurnOn2). The motor moves when either ref is true.");
            txtBldcRefCurrentPos = new TextBox();
            DcRow(grid, "Current position ref", txtBldcRefCurrentPos, "ProSim ref holding the current position (RefCurrentPos), e.g. system.analog.A_THROTTLE_RIGHT.");
            txtBldcRefTargetPos = new TextBox();
            DcRow(grid, "Target position ref", txtBldcRefTargetPos, "ProSim ref the motor follows (RefTargetPos), e.g. system.gauge.G_THROTTLE_RIGHT.");

            DcSection(grid, "Motor", "Direction, offset and acceleration");
            chkBldcReversed = new CheckBox { Text = "Reverse motor direction", AutoSize = true };
            DcRow(grid, "Direction", chkBldcReversed, "Invert the motor direction (Reversed).");
            nudBldcOffset = DcNumber(-100000, 100000, 0, 0);
            DcRow(grid, "Offset", nudBldcOffset, "Position offset. If one motor is not in sync with another, a small offset can even them out.");
            nudBldcAccel = DcNumber(0.1M, 1.0M, 2, 0.8M);
            nudBldcAccel.Increment = 0.1M;
            DcRow(grid, "Acceleration (0.1–1.0)", nudBldcAccel, "Speed acceleration (Acceleration).");

            // Tuning (optional): same keys as the manual's PID section
            _bldcTuning.Clear();
            foreach (var group in DcTuningLayout)
            {
                var fields = group.Fields.Where(f => f.Prop != "SetpointSlewPerTick").ToArray();
                if (fields.Length == 0) continue;
                DcSection(grid, group.Section, "Leave a field empty to use the built-in default");
                foreach (var f in fields)
                {
                    var tb = new TextBox();
                    DcRow(grid, f.Label, tb, f.Tip);
                    _bldcTuning[f.Prop] = tb;
                    tb.HandleCreated += (s, e) => SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, "default");
                }
            }

            host.Controls.Add(grid);
            pnlAdvancedProperties_BLDCMotors = host;
            return host;
        }

        // ── List / editor wiring ──────────────────────────────────────────────────
        private void ResetBldcMotorEditor()
        {
            if (pnlAdvancedProperties_BLDCMotors == null) return;
            txtBldcRefTurnOn.Text = txtBldcRefTurnOn2.Text = txtBldcRefCurrentPos.Text = txtBldcRefTargetPos.Text = "";
            chkBldcReversed.Checked = false;
            nudBldcOffset.Value = 0;
            nudBldcAccel.Value = 0.8M;
            foreach (var tb in _bldcTuning.Values) tb.Text = "";
            pnlAdvancedProperties_BLDCMotors.Visible = false;
        }

        private void ShowBldcMotorPanel()
        {
            SetGeneralPropertiesVisibility(true); // common hub / hub port form
            pnlAdvancedProperties_Inputs.Visible = false;
            pnlAdvancedProperties_MultiInputs.Visible = false;
            pnlAdvancedProperties_Outputs.Visible = false;
            pnlAdvancedProperties_Motors.Visible = false;
            pnlAdvancedProperties_Voltages.Visible = false;
            pnlAdvancedProperties_Simple.Visible = false;
            if (pnlAdvancedProperties_DCMotors != null) pnlAdvancedProperties_DCMotors.Visible = false;
            pnlAdvancedProperties_BLDCMotors.Visible = true;
            btnSaveDevice.Visible = true;
        }

        private void NewBldcMotorEditor()
        {
            var hubs = _hubs.GetAll();
            ResetBldcMotorEditor();
            PopulateHubDropdown(cboDeviceHub, hubs);
            SetHubAndPortChannel(0, 0, 0, hubs);
            lblCardItemTitle.Text = "New BL DC Motor";
            ShowBldcMotorPanel();
        }

        private void LoadBldcMotorEditor(PhidgetsBLDCMotorInst m, List<PhidgetsHubInst> hubs)
        {
            PopulateHubDropdown(cboDeviceHub, hubs);
            SetHubAndPortChannel(m.Serial, m.HubPort, 0, hubs);
            txtBldcRefTurnOn.Text = m.RefTurnOn ?? "";
            txtBldcRefTurnOn2.Text = m.RefTurnOn2 ?? "";
            txtBldcRefCurrentPos.Text = m.RefCurrentPos ?? "";
            txtBldcRefTargetPos.Text = m.RefTargetPos ?? "";
            chkBldcReversed.Checked = m.Reversed;
            nudBldcOffset.Value = ClampToRange(m.Offset, nudBldcOffset.Minimum, nudBldcOffset.Maximum);
            nudBldcAccel.Value = ClampToRange((decimal)(m.Acceleration > 0 ? m.Acceleration : 0.8), nudBldcAccel.Minimum, nudBldcAccel.Maximum);

            var opts = m.Options ?? new MotorTuningOptions();
            foreach (var kv in _bldcTuning)
            {
                var val = typeof(MotorTuningOptions).GetProperty(kv.Key)?.GetValue(opts);
                kv.Value.Text = val == null ? "" : Convert.ToString(val, CultureInfo.InvariantCulture);
            }

            lblCardItemTitle.Text = "Edit BL DC Motor";
            ShowBldcMotorPanel();
        }

        // ── Save ──────────────────────────────────────────────────────────────────
        private void SaveBldcMotor()
        {
            var hub = _hubs.GetAll().FirstOrDefault(h => HubText(h) == cboDeviceHub.Text);
            if (hub == null)
            {
                MessageBox.Show("Select a Phidgets hub for this motor.", "BL DC Motor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtBldcRefTurnOn.Text) || string.IsNullOrWhiteSpace(txtBldcRefCurrentPos.Text) || string.IsNullOrWhiteSpace(txtBldcRefTargetPos.Text))
            {
                MessageBox.Show("Turn-on ref, Current position ref and Target position ref are required.", "BL DC Motor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selected = lstDevices.SelectedItem as DeviceListItem;
            var existing = selected?.Tag as PhidgetsBLDCMotorInst;

            // Start from the existing options so values the form does not expose are kept
            var opts = existing?.Options ?? new MotorTuningOptions();
            foreach (var kv in _bldcTuning)
            {
                string txt = kv.Value.Text.Trim();
                var prop = typeof(MotorTuningOptions).GetProperty(kv.Key);
                if (txt == "") { prop.SetValue(opts, null); continue; }

                bool isInt = prop.PropertyType == typeof(int?);
                if (isInt && int.TryParse(txt, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv))
                    prop.SetValue(opts, iv);
                else if (!isInt && double.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out double dv))
                    prop.SetValue(opts, dv);
                else
                {
                    MessageBox.Show($"'{txt}' is not a valid number for \"{kv.Key}\".", "BL DC Motor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    kv.Value.Focus();
                    return;
                }
            }

            try
            {
                var cfg = _config.Load() ?? new Config();
                cfg.PhidgetsBLDCMotorInstances = cfg.PhidgetsBLDCMotorInstances ?? new List<PhidgetsBLDCMotorInst>();
                var list = cfg.PhidgetsBLDCMotorInstances;

                int index = selected != null && selected.Index >= 0 && selected.Index < list.Count ? selected.Index : -1;
                var inst = index >= 0 ? list[index] : new PhidgetsBLDCMotorInst();

                inst.Serial = hub.Serial;
                inst.HubPort = (int)nudDeviceHubPort.Value;
                inst.Reversed = chkBldcReversed.Checked;
                inst.Offset = (int)nudBldcOffset.Value;
                inst.RefTurnOn = txtBldcRefTurnOn.Text.Trim();
                inst.RefTurnOn2 = txtBldcRefTurnOn2.Text.NullIfEmpty()?.Trim();
                inst.RefCurrentPos = txtBldcRefCurrentPos.Text.Trim();
                inst.RefTargetPos = txtBldcRefTargetPos.Text.Trim();
                inst.Acceleration = (double)nudBldcAccel.Value;
                inst.Options = opts;

                if (index < 0) { list.Add(inst); index = list.Count - 1; }

                WriteConfig(cfg);
                DisplayInfoLog("BL DC motor saved. Restart the app to apply it.");

                // Reload the list and re-select the saved motor so further edits update it instead of adding a duplicate
                LoadDeviceListForCategory(BldcMotorCategory);
                for (int i = 0; i < lstDevices.Items.Count; i++)
                {
                    if ((lstDevices.Items[i] as DeviceListItem)?.Index == index)
                    {
                        lstDevices.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch (Exception ex) { DisplayErrorLog("Save error: " + ex.Message); }
        }
    }
}
