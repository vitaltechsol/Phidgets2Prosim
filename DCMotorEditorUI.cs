using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    /// <summary>List-item payload for the DC Motors category: the instance plus its index in the config list.</summary>
    internal class DcMotorRef
    {
        public int Index { get; set; }
        public PhidgetsDCMotorInst Inst { get; set; }
    }

    public partial class Form1
    {
        private const string DcMotorCategory = "DC Motors";

        private Panel pnlAdvancedProperties_DCMotors;
        private TableLayoutPanel pnlDcGrid;
        private int DcHubPort => (int)nudDeviceHubPort.Value;
        private TextBox txtDcRefTarget, txtDcRefFwd, txtDcRefBwd;
        private NumericUpDown nudDcAccel, nudDcCurrentLimit;
        private CheckBox chkDcReversed, chkDcFeedback;
        private Panel pnlDcFeedbackFields;
        private NumericUpDown nudDcViHubPort, nudDcViChannel;
        private TextBox txtDcViInPts, txtDcViOutPts, txtDcViMinChange;
        private readonly Dictionary<string, TextBox> _dcTuning = new Dictionary<string, TextBox>();
        private readonly ToolTip _dcTip = new ToolTip { AutoPopDelay = 12000 };

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        // Tuning fields grouped by section: property name -> friendly label + tooltip
        private static readonly (string Section, (string Prop, string Label, string Tip)[] Fields)[] DcTuningLayout =
        {
            ("Speed & Motion", new[]
            {
                ("MaxVelocity",         "Max velocity",        "Top speed, 0–1 (fraction of full power)."),
                ("MinVelocity",         "Min velocity",        "Lowest speed that still moves the motor, 0–1."),
                ("VelocityBand",        "Slow-down band",      "Distance from the target where the motor starts to slow down."),
                ("CurveGamma",          "Curve gamma",         "Shapes the slow-down curve. Lower = brakes later."),
                ("MaxVelStepPerTick",   "Max speed step/tick", "Limits how fast the speed can change each tick (smoothness)."),
                ("SetpointSlewPerTick", "Target slew/tick",    "Limits how fast the target moves each tick."),
            }),
            ("PID & Accuracy", new[]
            {
                ("Kp",                  "Kp (proportional)",   "Proportional gain."),
                ("Ki",                  "Ki (integral)",       "Integral gain."),
                ("Kd",                  "Kd (derivative)",     "Derivative gain."),
                ("IOnBand",             "Integral on-band",    "Integral only accumulates within this distance of the target."),
                ("IntegralLimit",       "Integral limit",      "Clamp for the integral term."),
                ("DeadbandEnter",       "Deadband enter",      "Stop when this close to the target."),
                ("DeadbandExit",        "Deadband exit",       "Restart moving when farther than this from the target."),
                ("PositionFilterAlpha", "Position filter",     "Smoothing of the sensor reading, 0–1 (1 = no smoothing)."),
                ("TickMs",              "Control tick (ms)",   "Control loop period in milliseconds."),
            }),
        };

        // ── UI construction ───────────────────────────────────────────────────────
        private Panel BuildDCMotorEditor()
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

            // ProSim
            DcSection(grid, "ProSim", "DataRefs that drive the motor");
            txtDcRefTarget = new TextBox();
            DcRow(grid, "Target position ref", txtDcRefTarget, "ProSim DataRef the motor follows (config key: RefTargetPos), e.g. system.gauge.G_PED_ELEV_TRIM.");
            txtDcRefFwd = new TextBox();
            DcRow(grid, "Forward ref (optional)", txtDcRefFwd, "Optional DataRef that drives the motor forward.");
            txtDcRefBwd = new TextBox();
            DcRow(grid, "Backward ref (optional)", txtDcRefBwd, "Optional DataRef that drives the motor backward.");

            // Motor
            DcSection(grid, "Motor", "Electrical limits and direction");
            nudDcAccel = DcNumber(1, 100, 0, 50);
            DcRow(grid, "Acceleration (1–100)", nudDcAccel, "How quickly the controller ramps speed up/down.");
            nudDcCurrentLimit = DcNumber(0.1M, 20, 1, 4);
            nudDcCurrentLimit.Increment = 0.5M;
            DcRow(grid, "Current limit (A)", nudDcCurrentLimit, "Maximum motor current in amps.");
            chkDcReversed = new CheckBox { Text = "Reverse motor direction", AutoSize = true };
            DcRow(grid, "Direction", chkDcReversed, "Flip the motor's rotation direction.");

            // Feedback
            DcSection(grid, "Position Feedback", "Optional potentiometer / sensor on a Voltage Input");
            chkDcFeedback = new CheckBox { Text = "Use a voltage input as position sensor", AutoSize = true };
            DcRow(grid, "Enable", chkDcFeedback, "Required for closed-loop positioning to the target ref.");

            pnlDcFeedbackFields = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0) };
            var fb = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
            fb.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            fb.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            nudDcViHubPort = DcNumber(-1, 16, 0, 0);
            DcRow(fb, "Sensor hub port", nudDcViHubPort, "Hub port of the voltage input (same hub as the motor).");
            nudDcViChannel = DcNumber(0, 16, 0, 0);
            DcRow(fb, "Sensor channel", nudDcViChannel, "Channel on the voltage input.");
            txtDcViInPts = new TextBox();
            DcRow(fb, "Input points", txtDcViInPts, "Sensor readings, comma separated (e.g. 0.164,0.292,0.411).");
            txtDcViOutPts = new TextBox();
            DcRow(fb, "Output points", txtDcViOutPts, "Position values matching each input point (e.g. 0,5,10).");
            txtDcViMinChange = new TextBox { Text = "0.005" };
            DcRow(fb, "Min change", txtDcViMinChange, "Ignore sensor changes smaller than this.");
            pnlDcFeedbackFields.Controls.Add(fb);
            grid.Controls.Add(pnlDcFeedbackFields, 0, grid.RowCount++);
            grid.SetColumnSpan(pnlDcFeedbackFields, 2);
            chkDcFeedback.CheckedChanged += (s, e) => pnlDcFeedbackFields.Enabled = chkDcFeedback.Checked;
            pnlDcFeedbackFields.Enabled = false;

            // Tuning
            _dcTuning.Clear();
            foreach (var group in DcTuningLayout)
            {
                DcSection(grid, group.Section, "Leave a field empty to use the built-in default");
                foreach (var f in group.Fields)
                {
                    var tb = new TextBox();
                    DcRow(grid, f.Label, tb, f.Tip);
                    _dcTuning[f.Prop] = tb;
                    tb.HandleCreated += (s, e) => SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, "default");
                }
            }

            // Test
            DcSection(grid, "Test", "Run the motor manually using the values above (not saved)");
            trkDcTestSpeed = new TrackBar { Minimum = -100, Maximum = 100, Value = 30, TickFrequency = 25, SmallChange = 5, LargeChange = 10 };
            DcRow(grid, "Speed (%)", trkDcTestSpeed, "Negative = backward. Moves live while the test is running.");
            lblDcTestSpeed = new Label { Text = "30%", AutoSize = false, Height = 20, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            DcRow(grid, "", lblDcTestSpeed, "");
            btnDcTest = new Button
            {
                Text = "▶  Turn ON", FlatStyle = FlatStyle.Flat, Height = 34,
                BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            };
            btnDcTest.FlatAppearance.BorderSize = 0;
            DcRow(grid, "Motor", btnDcTest, "Starts/stops the motor at the selected speed. It also stops when you switch motor or page.");
            lblDcTestStatus = new Label { Text = "Stopped", AutoSize = false, Height = 20, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(107, 114, 128), Font = new Font("Segoe UI", 8.5F) };
            DcRow(grid, "Status", lblDcTestStatus, "");

            trkDcTestSpeed.ValueChanged += (s, e) => { lblDcTestSpeed.Text = trkDcTestSpeed.Value + "%"; ApplyDcTestVelocity(); };
            chkDcReversed.CheckedChanged += (s, e) => ApplyDcTestVelocity();
            btnDcTest.Click += async (s, e) => { if (_dcTestMotor == null) await StartDcTest(); else StopDcTest(); };
            grid.VisibleChanged += (s, e) => { if (!grid.Visible) StopDcTest(); };
            FormClosing += (s, e) => StopDcTest();

            pnlDcGrid = grid;
            host.Controls.Add(grid);
            grid.Visible = false;
            pnlAdvancedProperties_DCMotors = host;
            return host;
        }

        private static NumericUpDown DcNumber(decimal min, decimal max, int decimals, decimal value)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, DecimalPlaces = decimals, Value = value };
        }

        private void DcSection(TableLayoutPanel grid, string title, string subtitle)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Height = 40, Margin = new Padding(0, 14, 0, 4) };
            pnl.Controls.Add(new Label
            {
                Text = subtitle, Dock = DockStyle.Bottom, Height = 16,
                Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(107, 114, 128),
            });
            pnl.Controls.Add(new Label
            {
                Text = title.ToUpperInvariant(), Dock = DockStyle.Bottom, Height = 20,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235),
            });
            // keep title above subtitle
            pnl.Controls.SetChildIndex(pnl.Controls[1], 0);
            pnl.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240)), 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);
            grid.Controls.Add(pnl, 0, grid.RowCount++);
            grid.SetColumnSpan(pnl, 2);
        }

        private void DcRow(TableLayoutPanel grid, string label, Control input, string tip)
        {
            var lbl = new Label
            {
                Text = label, AutoSize = false, Dock = DockStyle.Fill, Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(51, 65, 85),
                Margin = new Padding(0, 2, 0, 2),
            };
            input.Dock = input is CheckBox ? DockStyle.None : DockStyle.Fill;
            input.Margin = new Padding(0, 3, 0, 3);
            if (input is CheckBox) input.Anchor = AnchorStyles.Left;
            if (input is TextBox || input is ComboBox || input is NumericUpDown) input.Font = new Font("Segoe UI", 9F);

            int row = grid.RowCount++;
            grid.Controls.Add(lbl, 0, row);
            grid.Controls.Add(input, 1, row);
            _dcTip.SetToolTip(lbl, tip);
            _dcTip.SetToolTip(input, tip);
        }

        // ── Manual test ───────────────────────────────────────────────────────────
        private TrackBar trkDcTestSpeed;
        private Label lblDcTestSpeed, lblDcTestStatus;
        private Button btnDcTest;
        private Phidget22.DCMotor _dcTestMotor;
        private bool _dcTestStarting;

        private async System.Threading.Tasks.Task StartDcTest()
        {
            if (_dcTestStarting || _dcTestMotor != null) return;
            var hub = _hubs.GetAll().FirstOrDefault(h => h.Name == cboDeviceHub.Text);
            if (hub == null)
            {
                MessageBox.Show("Select a Phidgets hub first.", "Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _dcTestStarting = true;
            btnDcTest.Enabled = false;
            SetDcTestStatus("Connecting…", Color.FromArgb(217, 119, 6));
            int port = DcHubPort;
            double accel = (double)nudDcAccel.Value, limit = (double)nudDcCurrentLimit.Value;
            var motor = new Phidget22.DCMotor();
            try
            {
                // Release the running instance (if any) so the channel is free for the test
                var live = _devices?.DCMotors.FirstOrDefault(m => m != null && m.Serial == hub.Serial && m.HubPort == port);
                await System.Threading.Tasks.Task.Run(() =>
                {
                    if (live != null) { live.Disconnect(); _dcTestReleased = live; }
                    motor.DeviceSerialNumber = hub.Serial;
                    motor.HubPort = port;
                    motor.Channel = 0;
                    if (port >= 0) motor.IsRemote = true;
                    motor.Open(5000);
                    motor.Acceleration = Math.Max(motor.MinAcceleration, Math.Min(motor.MaxAcceleration, accel));
                    motor.CurrentLimit = Math.Max(motor.MinCurrentLimit, Math.Min(motor.MaxCurrentLimit, limit));
                });
                _dcTestMotor = motor;
                btnDcTest.Text = "■  Turn OFF";
                btnDcTest.BackColor = Color.FromArgb(239, 68, 68);
                ApplyDcTestVelocity();
            }
            catch (Exception ex)
            {
                try { motor.Close(); } catch { }
                ReconnectDcLive();
                SetDcTestStatus("Could not open motor: " + ex.Message, Color.FromArgb(220, 38, 38));
            }
            finally
            {
                _dcTestStarting = false;
                btnDcTest.Enabled = true;
            }
        }

        private void ApplyDcTestVelocity()
        {
            if (_dcTestMotor == null) return;
            try
            {
                double v = trkDcTestSpeed.Value / 100.0;
                if (chkDcReversed.Checked) v = -v;
                _dcTestMotor.TargetVelocity = v;
                SetDcTestStatus($"Running at {trkDcTestSpeed.Value}%" + (chkDcReversed.Checked ? " (reversed)" : ""), Color.FromArgb(5, 150, 105));
            }
            catch (Exception ex) { SetDcTestStatus("Error: " + ex.Message, Color.FromArgb(220, 38, 38)); }
        }

        private void StopDcTest()
        {
            var motor = _dcTestMotor;
            _dcTestMotor = null;
            if (motor != null)
            {
                try { motor.TargetVelocity = 0; } catch { }
                try { motor.Close(); } catch { }
            }
            ReconnectDcLive();
            if (btnDcTest != null && !btnDcTest.IsDisposed)
            {
                btnDcTest.Text = "▶  Turn ON";
                btnDcTest.BackColor = Color.FromArgb(16, 185, 129);
                if (motor != null) SetDcTestStatus("Stopped", Color.FromArgb(107, 114, 128));
            }
        }

        private PhidgetsDCMotor _dcTestReleased;

        // Hand the channel back to the running motor once the test is done
        private void ReconnectDcLive()
        {
            var live = _dcTestReleased;
            _dcTestReleased = null;
            live?.Reconnect();
        }

        private void SetDcTestStatus(string text, Color color)
        {
            if (lblDcTestStatus == null || lblDcTestStatus.IsDisposed) return;
            lblDcTestStatus.Text = text;
            lblDcTestStatus.ForeColor = color;
        }

        // ── List / editor wiring ──────────────────────────────────────────────────
        private void LoadDcMotorList(Config cfg, string search)
        {
            var list = cfg.PhidgetsDCMotorInstances ?? new List<PhidgetsDCMotorInst>();
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (search != "" && !((m.RefTargetPos ?? "").ToLower().Contains(search) || m.Serial.ToString().Contains(search)))
                    continue;
                lstDevices.Items.Add(new DeviceListItem
                {
                    Label = BuildDeviceListLabel(m.Serial, m.HubPort, null, m.RefTargetPos),
                    Tag = new DcMotorRef { Index = i, Inst = m },
                });
            }
        }

        private void ResetDcMotorEditor()
        {
            if (pnlAdvancedProperties_DCMotors == null) return;
            StopDcTest();
            txtDcRefTarget.Text = txtDcRefFwd.Text = txtDcRefBwd.Text = "";
            nudDcAccel.Value = 50;
            nudDcCurrentLimit.Value = 4;
            chkDcReversed.Checked = false;
            chkDcFeedback.Checked = false;
            nudDcViHubPort.Value = 0;
            nudDcViChannel.Value = 0;
            txtDcViInPts.Text = "";
            txtDcViOutPts.Text = "";
            txtDcViMinChange.Text = "0.005";
            foreach (var tb in _dcTuning.Values) tb.Text = "";
            pnlDcGrid.Visible = false;
            pnlAdvancedProperties_DCMotors.Visible = false;
        }

        private void LoadDcMotorEditor(PhidgetsDCMotorInst m, List<PhidgetsHubInst> hubs)
        {
            PopulateHubDropdown(cboDeviceHub, hubs);
            SetHubAndPortChannel(m.Serial, m.HubPort, 0, hubs);
            txtDcRefTarget.Text = m.RefTargetPos ?? "";
            txtDcRefFwd.Text = m.prosimDataRefFwd ?? "";
            txtDcRefBwd.Text = m.prosimDataRefBwd ?? "";
            nudDcAccel.Value = (decimal)Math.Min(100, Math.Max(1, m.Acceleration > 0 ? m.Acceleration : 50));
            nudDcCurrentLimit.Value = (decimal)Math.Min(20, Math.Max(0.1, m.CurrentLimit > 0 ? m.CurrentLimit : 4));
            chkDcReversed.Checked = m.Reversed;

            var vi = m.VoltageInput;
            chkDcFeedback.Checked = vi != null;
            if (vi != null)
            {
                nudDcViHubPort.Value = ClampToRange(vi.HubPort, nudDcViHubPort.Minimum, nudDcViHubPort.Maximum);
                nudDcViChannel.Value = ClampToRange(vi.Channel, nudDcViChannel.Minimum, nudDcViChannel.Maximum);
                txtDcViInPts.Text = string.Join(", ", (vi.InputPoints ?? new List<double>()).Select(d => d.ToString(CultureInfo.InvariantCulture)));
                txtDcViOutPts.Text = string.Join(", ", (vi.OutputPoints ?? new List<double>()).Select(d => d.ToString(CultureInfo.InvariantCulture)));
                txtDcViMinChange.Text = vi.MinChangeTriggerValue.ToString(CultureInfo.InvariantCulture);
            }

            var opts = m.Options ?? new MotorTuningOptions();
            foreach (var kv in _dcTuning)
            {
                var val = typeof(MotorTuningOptions).GetProperty(kv.Key)?.GetValue(opts);
                kv.Value.Text = val == null ? "" : Convert.ToString(val, CultureInfo.InvariantCulture);
            }

            lblCardItemTitle.Text = "Edit DC Motor";
            ShowDcMotorPanel();
        }

        private void ShowDcMotorPanel()
        {
            SetGeneralPropertiesVisibility(true); // common hub / hub port form
            pnlAdvancedProperties_Inputs.Visible = false;
            pnlAdvancedProperties_MultiInputs.Visible = false;
            pnlAdvancedProperties_Outputs.Visible = false;
            pnlAdvancedProperties_Motors.Visible = false;
            pnlAdvancedProperties_Voltages.Visible = false;
            pnlAdvancedProperties_DCMotors.Visible = true;
            pnlDcGrid.Visible = true;
            btnSaveDevice.Visible = true;
        }

        private void NewDcMotorEditor()
        {
            var hubs = _hubs.GetAll();
            ResetDcMotorEditor();
            PopulateHubDropdown(cboDeviceHub, hubs);
            SetHubAndPortChannel(0, 0, 0, hubs);
            lblCardItemTitle.Text = "New DC Motor";
            ShowDcMotorPanel();
        }

        // ── Save / delete ─────────────────────────────────────────────────────────
        private static bool TryParseDoubleList(string text, out List<double> result)
        {
            result = new List<double>();
            foreach (var part in text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!double.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return false;
                result.Add(d);
            }
            return true;
        }

        private bool TryBuildDcMotorFromEditor(PhidgetsDCMotorInst existing, out PhidgetsDCMotorInst inst)
        {
            inst = null;
            var hubs = _hubs.GetAll();
            var hub = hubs.FirstOrDefault(h => h.Name == cboDeviceHub.Text);
            if (hub == null)
            {
                MessageBox.Show("Select a Phidgets hub for this motor.", "DC Motor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Tuning options (blank = default)
            var opts = new MotorTuningOptions();
            foreach (var kv in _dcTuning)
            {
                string txt = kv.Value.Text.Trim();
                if (txt == "") continue;
                var prop = typeof(MotorTuningOptions).GetProperty(kv.Key);
                bool isInt = prop.PropertyType == typeof(int?);
                if (isInt && int.TryParse(txt, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv))
                    prop.SetValue(opts, iv);
                else if (!isInt && double.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out double dv))
                    prop.SetValue(opts, dv);
                else
                {
                    MessageBox.Show($"'{txt}' is not a valid number for \"{kv.Key}\".", "DC Motor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    kv.Value.Focus();
                    return false;
                }
            }

            PhidgetsVoltageInputInst vi = null;
            if (chkDcFeedback.Checked)
            {
                if (!TryParseDoubleList(txtDcViInPts.Text, out var inPts) || !TryParseDoubleList(txtDcViOutPts.Text, out var outPts)
                    || inPts.Count < 2 || inPts.Count != outPts.Count)
                {
                    MessageBox.Show("Input points and output points must be comma-separated numbers, at least 2, with the same count.",
                        "DC Motor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (!double.TryParse(txtDcViMinChange.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double minChange))
                {
                    MessageBox.Show("Min change must be a number.", "DC Motor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                vi = existing?.VoltageInput ?? new PhidgetsVoltageInputInst();
                vi.Serial = hub.Serial;
                vi.HubPort = (int)nudDcViHubPort.Value;
                vi.Channel = (int)nudDcViChannel.Value;
                vi.InputPoints = inPts;
                vi.OutputPoints = outPts;
                vi.MinChangeTriggerValue = minChange;
            }

            inst = new PhidgetsDCMotorInst
            {
                Serial = hub.Serial,
                HubPort = DcHubPort,
                Channel = existing?.Channel ?? 0,
                ProsimDataRef = existing?.ProsimDataRef,
                RefTargetPos = txtDcRefTarget.Text.Trim(),
                prosimDataRefFwd = txtDcRefFwd.Text.Trim(),
                prosimDataRefBwd = txtDcRefBwd.Text.Trim(),
                Acceleration = (double)nudDcAccel.Value,
                CurrentLimit = (double)nudDcCurrentLimit.Value,
                Reversed = chkDcReversed.Checked,
                VoltageInput = vi,
                Options = opts,
            };
            return true;
        }

        private void SaveDcMotor()
        {
            var selected = (lstDevices.SelectedItem as DeviceListItem)?.Tag as DcMotorRef;
            if (!TryBuildDcMotorFromEditor(selected?.Inst, out var inst)) return;

            try
            {
                var cfg = _config.Load() ?? new Config();
                cfg.PhidgetsDCMotorInstances = cfg.PhidgetsDCMotorInstances ?? new List<PhidgetsDCMotorInst>();
                int index;
                if (selected != null && selected.Index >= 0 && selected.Index < cfg.PhidgetsDCMotorInstances.Count)
                {
                    index = selected.Index;
                    cfg.PhidgetsDCMotorInstances[index] = inst;
                }
                else
                {
                    cfg.PhidgetsDCMotorInstances.Add(inst);
                    index = cfg.PhidgetsDCMotorInstances.Count - 1;
                }
                WriteConfig(cfg);
                DisplayInfoLog("DC motor saved.");

                // Reload the list and re-select the saved motor so further edits update it instead of adding a duplicate
                LoadDeviceListForCategory(DcMotorCategory);
                for (int i = 0; i < lstDevices.Items.Count; i++)
                {
                    if (((lstDevices.Items[i] as DeviceListItem)?.Tag as DcMotorRef)?.Index == index)
                    {
                        lstDevices.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch (Exception ex) { DisplayErrorLog("Save error: " + ex.Message); }
        }

        private void DeleteDcMotor(DcMotorRef r)
        {
            var cfg = _config.Load();
            var list = cfg?.PhidgetsDCMotorInstances;
            if (list == null || r.Index < 0 || r.Index >= list.Count) return;
            list.RemoveAt(r.Index);
            WriteConfig(cfg);
        }
    }
}
