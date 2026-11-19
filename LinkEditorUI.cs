using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    // "Links" category: direct input -> PWM output mapping (no ProSim)
    public partial class Form1
    {
        private const string LinkCategory = "Links";

        private Panel pnlAdvancedProperties_Links;
        private TableLayoutPanel pnlLinkGrid;
        private TextBox txtLinkName;
        private ComboBox cboLinkSource, cboLinkInHub, cboLinkOutHub;
        private NumericUpDown nudLinkInPort, nudLinkInChannel, nudLinkOutPort, nudLinkOutChannel;
        private CheckBox chkLinkUseRange, chkLinkInvert;
        private NumericUpDown nudLinkInMin, nudLinkInMax, nudLinkOutMin, nudLinkOutMax, nudLinkCurve, nudLinkMinChange, nudLinkInterval;
        private readonly List<Control> _linkVoltageOnly = new List<Control>();
        private Label lblLinkInHint;

        private Button btnLinkTest;
        private Label lblLinkTestStatus;
        private PhidgetsLink _linkTest;
        private bool _linkTestStarting;
        private readonly List<PhidgetsLink> _linkReleased = new List<PhidgetsLink>();

        private bool LinkIsVoltage => cboLinkSource.SelectedIndex != 1;

        private Panel BuildLinkEditor()
        {
            var host = new Panel { Dock = DockStyle.Fill, Visible = false, AutoScroll = true };
            var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 0, 14, 0) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            DcSection(grid, "Link", "Drive an output directly from an input (no ProSim)");
            txtLinkName = new TextBox();
            DcRow(grid, "Name (optional)", txtLinkName, "A label for this link, e.g. Cabin light dimmer.");

            // ── Input ──
            DcSection(grid, "Input", "What controls the output");
            cboLinkSource = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboLinkSource.Items.AddRange(new object[] { "Voltage Input (potentiometer / sensor)", "Digital Input (switch)" });
            cboLinkSource.SelectedIndex = 0;
            DcRow(grid, "Input type", cboLinkSource, "Voltage Input gives a smooth range; Digital Input is on/off.");
            cboLinkInHub = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            DcRow(grid, "Hub", cboLinkInHub, "Phidgets hub the input is connected to.");
            nudLinkInPort = DcNumber(-1, 16, 0, 0);
            DcRow(grid, "Hub port", nudLinkInPort, "Hub port of the input (-1 for USB).");
            nudLinkInChannel = DcNumber(0, 15, 0, 0);
            DcRow(grid, "Channel", nudLinkInChannel, "Input channel on that port.");

            chkLinkUseRange = new CheckBox { Text = "Read volts instead of ratio (0–1)", AutoSize = true };
            AddLinkVoltageRow(grid, "Units", chkLinkUseRange, "Check to use real volts (VoltageInput) instead of the 0–1 ratio.");
            nudLinkInMin = new NumericUpDown { Minimum = -100, Maximum = 100, DecimalPlaces = 3, Increment = 0.01M, Value = 0 };
            AddLinkVoltageRow(grid, "Input at minimum", nudLinkInMin, "Input reading that should give the minimum output (e.g. pot fully down).");
            nudLinkInMax = new NumericUpDown { Minimum = -100, Maximum = 100, DecimalPlaces = 3, Increment = 0.01M, Value = 1 };
            AddLinkVoltageRow(grid, "Input at maximum", nudLinkInMax, "Input reading that should give the maximum output (e.g. pot fully up).");
            lblLinkInHint = new Label { Text = "Ratio: 0 – 1", AutoSize = false, Height = 20, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(107, 114, 128), Font = new Font("Segoe UI", 8.5F) };
            AddLinkVoltageRow(grid, "", lblLinkInHint, "");
            nudLinkCurve = new NumericUpDown { Minimum = 0.1M, Maximum = 10, DecimalPlaces = 2, Increment = 0.1M, Value = 1 };
            AddLinkVoltageRow(grid, "Response curve", nudLinkCurve, "1 = linear. Above 1 the output starts slowly; below 1 it starts fast.");

            chkLinkInvert = new CheckBox { Text = "Invert (high input = low output)", AutoSize = true };
            DcRow(grid, "Direction", chkLinkInvert, "Swap which end of the input gives the minimum output.");

            // ── Output ──
            DcSection(grid, "Output (PWM)", "Digital output driven by duty cycle");
            cboLinkOutHub = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            DcRow(grid, "Hub", cboLinkOutHub, "Phidgets hub the output is connected to.");
            nudLinkOutPort = DcNumber(-1, 16, 0, 0);
            DcRow(grid, "Hub port", nudLinkOutPort, "Hub port of the output (-1 for USB).");
            nudLinkOutChannel = DcNumber(0, 15, 0, 0);
            DcRow(grid, "Channel", nudLinkOutChannel, "Output channel on that port.");
            nudLinkOutMin = new NumericUpDown { Minimum = 0, Maximum = 100, DecimalPlaces = 1, Value = 0 };
            DcRow(grid, "Output at minimum (%)", nudLinkOutMin, "PWM duty cycle at the minimum input (0 = off).");
            nudLinkOutMax = new NumericUpDown { Minimum = 0, Maximum = 100, DecimalPlaces = 1, Value = 100 };
            DcRow(grid, "Output at maximum (%)", nudLinkOutMax, "PWM duty cycle at the maximum input (100 = fully on).");

            // ── Tuning (voltage only) ──
            DcSection(grid, "Tuning", "How often the input is read");
            nudLinkMinChange = new NumericUpDown { Minimum = 0, Maximum = 10, DecimalPlaces = 4, Increment = 0.001M, Value = 0.002M };
            AddLinkVoltageRow(grid, "Min change", nudLinkMinChange, "Ignore input changes smaller than this.");
            nudLinkInterval = DcNumber(8, 10000, 0, 50);
            AddLinkVoltageRow(grid, "Update interval (ms)", nudLinkInterval, "How often the input is sampled.");

            // ── Test ──
            DcSection(grid, "Test", "Run this link live using the values above (not saved)");
            btnLinkTest = new Button
            {
                Text = "▶  Start test", FlatStyle = FlatStyle.Flat, Height = 34,
                BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            };
            btnLinkTest.FlatAppearance.BorderSize = 0;
            DcRow(grid, "Link", btnLinkTest, "Turn the potentiometer (or flip the switch) and watch the output follow. Stops when you switch item or page.");
            lblLinkTestStatus = new Label { Text = "Stopped", AutoSize = false, Height = 20, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(107, 114, 128), Font = new Font("Segoe UI", 8.5F) };
            DcRow(grid, "Live", lblLinkTestStatus, "");

            cboLinkSource.SelectedIndexChanged += (s, e) => UpdateLinkSourceVisibility();
            chkLinkUseRange.CheckedChanged += (s, e) => UpdateLinkSourceVisibility();
            btnLinkTest.Click += async (s, e) => { if (_linkTest == null) await StartLinkTest(); else StopLinkTest(); };
            grid.VisibleChanged += (s, e) => { if (!grid.Visible) StopLinkTest(); };
            FormClosing += (s, e) => StopLinkTest();

            pnlLinkGrid = grid;
            host.Controls.Add(grid);
            grid.Visible = false;
            pnlAdvancedProperties_Links = host;
            return host;
        }

        // Adds a row and remembers its label + input so they can be hidden for digital inputs
        private void AddLinkVoltageRow(TableLayoutPanel grid, string label, Control input, string tip)
        {
            int before = grid.Controls.Count;
            DcRow(grid, label, input, tip);
            for (int i = before; i < grid.Controls.Count; i++) _linkVoltageOnly.Add(grid.Controls[i]);
        }

        private void UpdateLinkSourceVisibility()
        {
            bool voltage = LinkIsVoltage;
            foreach (var c in _linkVoltageOnly) c.Visible = voltage;
            if (voltage)
            {
                chkLinkInvert.Text = "Invert (high input = low output)";
                lblLinkInHint.Text = chkLinkUseRange.Checked ? "Volts" : "Ratio: 0 – 1";
            }
            else
            {
                chkLinkInvert.Text = "Invert (switch on = minimum output)";
            }
        }

        // ── List / editor wiring ──────────────────────────────────────────────────
        private static string LinkLabel(PhidgetsLinkInst l)
        {
            string src = l.SourceType == LinkSourceType.DigitalInput ? "D" : "V";
            string text = !string.IsNullOrWhiteSpace(l.Name) ? l.Name : $"{src}{l.Channel} → {l.OutChannel}";
            return $"H:{x_Short(l.Serial)} P:{l.HubPort} C:{l.Channel}  " + ShortDeviceValue(text, 16);
        }

        private void ResetLinkEditor()
        {
            if (pnlAdvancedProperties_Links == null) return;
            StopLinkTest();
            txtLinkName.Text = "";
            cboLinkSource.SelectedIndex = 0;
            nudLinkInPort.Value = 0; nudLinkInChannel.Value = 0;
            nudLinkOutPort.Value = 0; nudLinkOutChannel.Value = 0;
            chkLinkUseRange.Checked = false;
            chkLinkInvert.Checked = false;
            nudLinkInMin.Value = 0; nudLinkInMax.Value = 1;
            nudLinkCurve.Value = 1;
            nudLinkOutMin.Value = 0; nudLinkOutMax.Value = 100;
            nudLinkMinChange.Value = 0.002M;
            nudLinkInterval.Value = 50;
            UpdateLinkSourceVisibility();
            pnlLinkGrid.Visible = false;
            pnlAdvancedProperties_Links.Visible = false;
        }

        private void SelectHub(ComboBox cbo, int serial)
        {
            var hub = _hubs.GetAll().FirstOrDefault(h => h.Serial == serial);
            if (hub != null) cbo.Text = HubText(hub);
        }

        private void ShowLinkPanel()
        {
            SetGeneralPropertiesVisibility(false); // Links have their own input + output hub selectors
            pnlAdvancedProperties_Inputs.Visible = false;
            pnlAdvancedProperties_MultiInputs.Visible = false;
            pnlAdvancedProperties_Outputs.Visible = false;
            pnlAdvancedProperties_Motors.Visible = false;
            pnlAdvancedProperties_Voltages.Visible = false;
            pnlAdvancedProperties_Simple.Visible = false;
            if (pnlAdvancedProperties_DCMotors != null) pnlAdvancedProperties_DCMotors.Visible = false;
            pnlAdvancedProperties_Links.Visible = true;
            pnlLinkGrid.Visible = true;
            btnSaveDevice.Visible = true;
            UpdateLinkSourceVisibility();
        }

        private void NewLinkEditor()
        {
            var hubs = _hubs.GetAll();
            ResetLinkEditor();
            PopulateHubDropdown(cboLinkInHub, hubs);
            PopulateHubDropdown(cboLinkOutHub, hubs);
            lblCardItemTitle.Text = "New Link";
            ShowLinkPanel();
        }

        private void LoadLinkEditor(PhidgetsLinkInst l)
        {
            var hubs = _hubs.GetAll();
            ResetLinkEditor();
            PopulateHubDropdown(cboLinkInHub, hubs);
            PopulateHubDropdown(cboLinkOutHub, hubs);
            SelectHub(cboLinkInHub, l.Serial);
            SelectHub(cboLinkOutHub, l.OutSerial);

            txtLinkName.Text = l.Name ?? "";
            cboLinkSource.SelectedIndex = l.SourceType == LinkSourceType.DigitalInput ? 1 : 0;
            nudLinkInPort.Value = ClampToRange(l.HubPort, nudLinkInPort.Minimum, nudLinkInPort.Maximum);
            nudLinkInChannel.Value = ClampToRange(l.Channel, nudLinkInChannel.Minimum, nudLinkInChannel.Maximum);
            nudLinkOutPort.Value = ClampToRange(l.OutHubPort, nudLinkOutPort.Minimum, nudLinkOutPort.Maximum);
            nudLinkOutChannel.Value = ClampToRange(l.OutChannel, nudLinkOutChannel.Minimum, nudLinkOutChannel.Maximum);
            chkLinkUseRange.Checked = l.UseRange;
            chkLinkInvert.Checked = l.InvertInput;
            SetNud(nudLinkInMin, l.InputMin);
            SetNud(nudLinkInMax, l.InputMax);
            SetNud(nudLinkCurve, l.CurvePower > 0 ? l.CurvePower : 1);
            SetNud(nudLinkOutMin, l.OutputMin * 100);
            SetNud(nudLinkOutMax, l.OutputMax * 100);
            SetNud(nudLinkMinChange, l.MinChangeTriggerValue);
            SetNud(nudLinkInterval, l.DataInterval > 0 ? l.DataInterval : 50);
            lblCardItemTitle.Text = "Edit Link";
            ShowLinkPanel();
        }

        // Fills `l` from the form; returns false (with a message) when something is invalid
        private bool TryReadLinkEditor(PhidgetsLinkInst l)
        {
            var hubs = _hubs.GetAll();
            var inHub = hubs.FirstOrDefault(h => HubText(h) == cboLinkInHub.Text);
            var outHub = hubs.FirstOrDefault(h => HubText(h) == cboLinkOutHub.Text);
            if (inHub == null || outHub == null)
            {
                MessageBox.Show("Select a Phidgets hub for both the input and the output.", "Link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (LinkIsVoltage && nudLinkInMin.Value == nudLinkInMax.Value)
            {
                MessageBox.Show("Input at minimum and maximum must be different.", "Link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            l.Name = txtLinkName.Text.NullIfEmpty();
            l.SourceType = LinkIsVoltage ? LinkSourceType.VoltageInput : LinkSourceType.DigitalInput;
            l.Serial = inHub.Serial;
            l.HubPort = (int)nudLinkInPort.Value;
            l.Channel = (int)nudLinkInChannel.Value;
            l.UseRange = LinkIsVoltage && chkLinkUseRange.Checked;
            l.InvertInput = chkLinkInvert.Checked;
            l.InputMin = (double)nudLinkInMin.Value;
            l.InputMax = (double)nudLinkInMax.Value;
            l.CurvePower = (double)nudLinkCurve.Value;
            l.MinChangeTriggerValue = (double)nudLinkMinChange.Value;
            l.DataInterval = (int)nudLinkInterval.Value;
            l.OutSerial = outHub.Serial;
            l.OutHubPort = (int)nudLinkOutPort.Value;
            l.OutChannel = (int)nudLinkOutChannel.Value;
            l.OutputMin = (double)nudLinkOutMin.Value / 100.0;
            l.OutputMax = (double)nudLinkOutMax.Value / 100.0;
            return true;
        }

        private void SaveLink()
        {
            try
            {
                var cfg = _config.Load() ?? new Config();
                cfg.PhidgetsLinkInstances = cfg.PhidgetsLinkInstances ?? new List<PhidgetsLinkInst>();
                int idx = (lstDevices.SelectedItem as DeviceListItem)?.Index ?? -1;
                var l = EditTarget(cfg.PhidgetsLinkInstances, idx, out int at, out bool isNew);
                if (!TryReadLinkEditor(l))
                {
                    return; // nothing is written; the unsaved target is discarded with this config copy
                }
                WriteConfig(cfg);
                DisplayInfoLog("Link saved. Restart the app (or reconnect) to apply it.");

                LoadDeviceListForCategory(LinkCategory);
                for (int i = 0; i < lstDevices.Items.Count; i++)
                {
                    if ((lstDevices.Items[i] as DeviceListItem)?.Index == at) { lstDevices.SelectedIndex = i; break; }
                }
            }
            catch (Exception ex) { DisplayErrorLog("Save error: " + ex.Message); }
        }

        // ── Live test ─────────────────────────────────────────────────────────────
        private async System.Threading.Tasks.Task StartLinkTest()
        {
            if (_linkTestStarting || _linkTest != null) return;
            var inst = new PhidgetsLinkInst();
            if (!TryReadLinkEditor(inst)) return;

            _linkTestStarting = true;
            btnLinkTest.Enabled = false;
            SetLinkTestStatus("Connecting…", Color.FromArgb(217, 119, 6));
            var test = new PhidgetsLink(inst);
            try
            {
                // Release running links that use the same input or output channel so the test can open them
                var live = _devices?.Links.Where(l => l.IsRunning && (l.InputKey == test.InputKey || l.OutputKey == test.OutputKey
                                                                     || l.InputKey == test.OutputKey || l.OutputKey == test.InputKey)).ToList()
                           ?? new List<PhidgetsLink>();
                await System.Threading.Tasks.Task.Run(() => { foreach (var l in live) { l.Disconnect(); _linkReleased.Add(l); } });

                test.Changed += (x, duty) =>
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    string input = inst.SourceType == LinkSourceType.DigitalInput
                        ? (x >= 0.5 ? "ON" : "OFF")
                        : x.ToString("0.000") + (inst.UseRange ? " V" : "");
                    BeginInvoke(new Action(() => SetLinkTestStatus($"Input {input}  →  Output {duty * 100:0.#}%", Color.FromArgb(5, 150, 105))));
                };
                if (await test.Start())
                {
                    _linkTest = test;
                    btnLinkTest.Text = "■  Stop test";
                    btnLinkTest.BackColor = Color.FromArgb(239, 68, 68);
                    SetLinkTestStatus("Running — waiting for input…", Color.FromArgb(5, 150, 105));
                }
                else
                {
                    ReconnectLinksLive();
                    SetLinkTestStatus("Could not open: " + test.LastError, Color.FromArgb(220, 38, 38));
                }
            }
            catch (Exception ex)
            {
                test.Stop();
                ReconnectLinksLive();
                SetLinkTestStatus("Test error: " + ex.Message, Color.FromArgb(220, 38, 38));
            }
            finally
            {
                _linkTestStarting = false;
                btnLinkTest.Enabled = true;
            }
        }

        private void StopLinkTest()
        {
            var t = _linkTest;
            _linkTest = null;
            t?.Stop();
            ReconnectLinksLive();
            if (btnLinkTest != null && !btnLinkTest.IsDisposed)
            {
                btnLinkTest.Text = "▶  Start test";
                btnLinkTest.BackColor = Color.FromArgb(16, 185, 129);
                if (t != null) SetLinkTestStatus("Stopped", Color.FromArgb(107, 114, 128));
            }
        }

        // Hand the channels back to the running links once the test is done
        private void ReconnectLinksLive()
        {
            var released = _linkReleased.ToList();
            _linkReleased.Clear();
            foreach (var l in released) l.Reconnect();
        }

        private void SetLinkTestStatus(string text, Color color)
        {
            if (lblLinkTestStatus == null || lblLinkTestStatus.IsDisposed) return;
            lblLinkTestStatus.Text = text;
            lblLinkTestStatus.ForeColor = color;
        }
    }
}
