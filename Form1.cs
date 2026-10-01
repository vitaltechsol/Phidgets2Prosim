namespace Phidgets2Prosim
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;
    using System.Drawing;
    using System.Collections.Generic;
    using System.IO;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class Form1 : Form
    {
        // ── Services ──────────────────────────────────────────────────────────────
        private readonly ConfigRepository     _config  = new ConfigRepository();
        private readonly ProSimService        _prosim  = new ProSimService();
        private readonly HubManager           _hubs;
        private readonly PhidgetsDeviceService _devices;

        // ── Retained for special / custom devices ────────────────────────────────
        private Custom_TrimWheel    trimWheel    => _devices?.TrimWheel;
        private Custom_ParkingBrake customParkingBrake => _devices?.ParkingBrake;

        // ── UI state ──────────────────────────────────────────────────────────────
        private bool   _configsInsLoaded      = false;
        private string _activeDeviceCategory  = "Outputs";

        // ── Constructor ──────────────────────────────────────────────────────────
        public Form1()
        {
            _hubs    = new HubManager(_config);
            _devices = new PhidgetsDeviceService(_prosim.Connection);

            // Wire service logging to UI
            _config.InfoLog  += DisplayInfoLog;
            _config.ErrorLog += DisplayErrorLog;
            _prosim.InfoLog  += DisplayInfoLog;
            _prosim.ErrorLog += DisplayErrorLog;
            _hubs.InfoLog    += DisplayInfoLog;
            _hubs.ErrorLog   += DisplayErrorLog;
            _devices.InfoLog  += DisplayInfoLog;
            _devices.ErrorLog += DisplayErrorLog;

            // Wire ProSim events
            _prosim.Connected    += OnProSimConnected;
            _prosim.Disconnected += OnProSimDisconnected;
            _prosim.SimPauseChanged += OnSimPauseChanged;

            InitializeComponent();
            this.Icon = Properties.Resources.ph2pr;
            this.Shown      += new EventHandler(Form1_Shown);
            this.FormClosed += new FormClosedEventHandler(Form1_Closed);

            // Wire sidebar navigation
            btnNavDashboard.Click += (s, e) => ShowPage(pnlPageDashboard, btnNavDashboard);
            btnNavHubs.Click      += (s, e) => ShowPage(pnlPageHubs,      btnNavHubs);
            btnNavDevices.Click   += (s, e) => ShowPage(pnlPageDevices,   btnNavDevices);
            btnNavSpecial.Click   += (s, e) => ShowPage(pnlPageSpecial,   btnNavSpecial);
            btnNavSettings.Click  += (s, e) => ShowPage(pnlPageSettings,  btnNavSettings);

            // Wire log buttons
            btnLogClear.Click += BtnLogClear_Click;
            btnLogOk.Click    += BtnLogOk_Click;

            // Wire Dashboard – ProSim connection
            btnSaveProsimIP.Click    += BtnSaveProsimIP_Click;
            btnConnectProsim.Click   += BtnConnectProsim_Click;
            btnDisconnectProsim.Click += BtnDisconnectProsim_Click;

            // Wire Hubs page
            btnAddHub.Click    += BtnAddHub_Click;
            btnDeleteHub.Click += BtnDeleteHub_Click;
            btnScan.Click      += BtnScan_Click;
            dgvConfiguredHubs.SelectionChanged += (s, e) => LoadHubEditorFromSelection();

            // Wire Devices page category filter
            foreach (Control c in flowCategories.Controls)
                if (c is Button catBtn)
                    catBtn.Click += CatBtn_Click;

            // Wire device list & editor buttons
            lstDevices.SelectedIndexChanged += LstDevices_SelectedIndexChanged;
            btnAddDevice.Click    += BtnAddDevice_Click;
            btnDeleteDevice.Click += BtnDeleteDevice_Click;
            btnSaveDevice.Click   += BtnSaveDevice_Click;
            txtDeviceSearch.TextChanged += TxtDeviceSearch_TextChanged;

            // Show Dashboard first
            ShowPage(pnlPageDashboard, btnNavDashboard);
        }

        // ── Navigation ────────────────────────────────────────────────────────────
        private Button _activeNavBtn = null;

        private void ShowPage(Panel page, Button navBtn)
        {
            // Hide all pages
            pnlPageDashboard.Visible = false;
            pnlPageHubs.Visible      = false;
            pnlPageDevices.Visible   = false;
            pnlPageSpecial.Visible   = false;
            pnlPageSettings.Visible  = false;

            // Reset nav button colours
            var inactiveColor = Color.FromArgb(24, 28, 36);
            var activeColor   = Color.FromArgb(51, 65, 85);
            foreach (Control c in pnlSidebar.Controls)
            {
                if (c is Button b)
                {
                    b.BackColor = inactiveColor;
                    b.ForeColor = Color.FromArgb(148, 163, 184);
                }
            }

            // Highlight active nav button
            navBtn.BackColor = activeColor;
            navBtn.ForeColor = Color.White;
            _activeNavBtn = navBtn;

            page.Visible = true;
            page.BringToFront();

            // Lazy-populate on first visit
            if (page == pnlPageHubs)    LoadHubsGrid();
            if (page == pnlPageDevices) LoadDeviceListForCategory(_activeDeviceCategory);
            if (page == pnlPageSettings) LoadSettingsPage();
        }

        // ── ProSim Connection ─────────────────────────────────────────────────────
        private void BtnSaveProsimIP_Click(object sender, EventArgs e)
        {
            string ip = txtProsimIP.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip)) return;
            _config.SaveProSimIP(ip);
        }

        private void BtnConnectProsim_Click(object sender, EventArgs e)
        {
            string ip = txtProsimIP.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";
            connectionStatusLabel.Text = "● CONNECTING TO " + ip;
            _ = _prosim.ConnectAsync(ip);
        }

        private void BtnDisconnectProsim_Click(object sender, EventArgs e)
        {
            _devices.UnloadInputDevices();
            _configsInsLoaded = false;
            _prosim.Disconnect();
            DisplayInfoLog("ProSim input devices unloaded (disconnect requested).");
        }

        private void OnProSimConnected()
        {
            BeginInvoke(new MethodInvoker(UpdateStatusLabel));
            Task.Run(() =>
            {
                try
                {
                    var cfg = _config.Load();
                    _devices.LoadInputDevices(cfg);
                    _configsInsLoaded = true;
                    BeginInvoke(new Action(UpdateTelemetryPanel));
                }
                catch (Exception ex) { DisplayErrorLog("Error loading inputs: " + ex.Message); }
            });
        }

        private void OnProSimDisconnected()
        {
            BeginInvoke(new MethodInvoker(UpdateStatusLabel));
            if (_configsInsLoaded)
                BeginInvoke(new MethodInvoker(() =>
                {
                    _devices.UnloadInputDevices();
                    _configsInsLoaded = false;
                }));
        }

        private void OnSimPauseChanged(bool paused)
        {
            trimWheel?.Pause(paused);
            BeginInvoke(new MethodInvoker(UpdatePauseLabel));
        }

        void UpdateStatusLabel()
        {
            if (_prosim.IsConnected)
            {
                UpdatePauseLabel();
                btnConnectProsim.Enabled    = false;
                btnDisconnectProsim.Enabled = true;
            }
            else
            {
                DisplayInfoLog("ProSim DISCONNECTED");
                connectionStatusLabel.Text      = "● DISCONNECTED";
                connectionStatusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                btnConnectProsim.Enabled        = true;
                btnDisconnectProsim.Enabled     = false;
                UpdateTelemetryPanel();
            }
        }

        void UpdatePauseLabel()
        {
            if (!_prosim.IsConnected) return;
            if (_prosim.IsPaused)
            {
                DisplayInfoLog("ProSim Paused");
                connectionStatusLabel.Text      = "● PAUSED";
                connectionStatusLabel.ForeColor = Color.OrangeRed;
            }
            else
            {
                connectionStatusLabel.Text      = "● CONNECTED";
                connectionStatusLabel.ForeColor = Color.FromArgb(16, 185, 129);
            }
            UpdateTelemetryPanel();
        }

        // ── Telemetry panel (Dashboard card) ─────────────────────────────────────
        private void UpdateTelemetryPanel()
        {
            if (flowTelemetry.InvokeRequired) { flowTelemetry.Invoke(new Action(UpdateTelemetryPanel)); return; }
            flowTelemetry.Controls.Clear();
            AddTelemetryChip("Outputs",   CountNonNull(_devices.Outputs));
            AddTelemetryChip("Inputs",    CountNonNull(_devices.Inputs));
            AddTelemetryChip("Multi-In",  CountNonNull(_devices.MultiInputs));
            AddTelemetryChip("Gates",     CountNonNull(_devices.Gates));
            AddTelemetryChip("Encoders",  CountNonNull(_devices.Encoders));
            AddTelemetryChip("VoltOut",   CountNonNull(_devices.VoltageOutputs));
        }

        private int CountNonNull<T>(T[] arr) => arr.Count(x => x != null);

        private void AddTelemetryChip(string label, int count)
        {
            // Compact horizontal chip: [COUNT  label] — fits in the single-row stats bar
            bool active = count > 0;

            var p = new Panel
            {
                Size      = new Size(118, 44),
                Margin    = new Padding(0, 0, 8, 0),
                BackColor = active ? Color.FromArgb(220, 252, 231) : Color.FromArgb(241, 245, 249)
            };

            // Rounded feel via border
            p.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(active
                    ? Color.FromArgb(167, 243, 208)
                    : Color.FromArgb(226, 232, 240), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                }
            };

            // Big count on the left
            p.Controls.Add(new Label
            {
                Text      = count.ToString(),
                Font      = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = active ? Color.FromArgb(22, 163, 74) : Color.FromArgb(148, 163, 184),
                Location  = new Point(10, 6),
                Size      = new Size(44, 28),
                TextAlign = ContentAlignment.MiddleLeft
            });

            // Label on the right, two lines
            p.Controls.Add(new Label
            {
                Text      = label,
                Font      = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location  = new Point(52, 14),
                Size      = new Size(60, 16),
                TextAlign = ContentAlignment.MiddleLeft
            });

            flowTelemetry.Controls.Add(p);
        }

        // ── Logging ────────────────────────────────────────────────────────────────
        private void DisplayErrorLog(string message)
        {
            FileLogger.LogError(message);
            if (txtLog.InvokeRequired) { txtLog.Invoke(new Action(() => DisplayErrorLog(message))); return; }
            txtLog.ForeColor = Color.FromArgb(239, 68, 68);
            txtLog.AppendText(DateTime.Now.ToLongTimeString() + " - ** ERROR ** : " + message + Environment.NewLine);
        }

        private void DisplayInfoLog(string message)
        {
            FileLogger.LogInfo(message);
            if (txtLog.InvokeRequired) { txtLog.Invoke(new Action(() => DisplayInfoLog(message))); return; }
            txtLog.AppendText(DateTime.Now.ToLongTimeString() + ": " + message + Environment.NewLine);
        }

        private void BtnLogClear_Click(object sender, EventArgs e)
        {
            txtLog.ForeColor = Color.FromArgb(14, 165, 233);
            txtLog.Text = string.Empty;
        }

        private void BtnLogOk_Click(object sender, EventArgs e)
        {
            txtLog.ForeColor = Color.FromArgb(14, 165, 233);
        }

        // ── Form lifecycle ────────────────────────────────────────────────────────
        private void Form1_Load_1(object sender, EventArgs e) { /* layout already handled */ }

        private void Form1_Shown(object sender, EventArgs e)
        {
            string ip = _config.LoadProSimIP();
            if (!string.IsNullOrWhiteSpace(ip)) txtProsimIP.Text = ip;

            bool configReady = CheckAndMigrateConfig();
            if (configReady)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var cfg = _config.Load();
                        _hubs.RegisterHubs(cfg?.PhidgetsHubsInstances);
                        _devices.LoadOutputDevices(cfg);
                        BeginInvoke(new Action(UpdateTelemetryPanel));
                    }
                    catch (Exception ex) { DisplayErrorLog("Error loading config: " + ex.Message); }
                });
            }
            else
            {
                DisplayErrorLog("Config is still schema 1.0 – migration was not completed. Please restart and complete the hub migration before connecting.");
            }

            _prosim.SubscribePause();
            LoadSettingsPage();
        }

        private void Form1_Closed(object sender, EventArgs e)
        {
            _devices.UnloadInputDevices();
            Debug.WriteLine("closed");
        }

        // ── Hub page ──────────────────────────────────────────────────────────────
        private void LoadHubsGrid()
        {
            try
            {
                var hubs = _hubs.GetAll();
                dgvConfiguredHubs.Columns.Clear();
                dgvConfiguredHubs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",    HeaderText = "Name",    DataPropertyName = "Name",    FillWeight = 50 });
                dgvConfiguredHubs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSerial",  HeaderText = "Serial",  DataPropertyName = "Serial",  FillWeight = 30 });
                dgvConfiguredHubs.Columns.Add(new DataGridViewCheckBoxColumn{ Name = "colEnabled", HeaderText = "Enabled", DataPropertyName = "Enabled", FillWeight = 20 });
                dgvConfiguredHubs.DataSource = new BindingList<PhidgetsHubInst>(hubs);
            }
            catch (Exception ex) { DisplayErrorLog("Error loading hubs: " + ex.Message); }
        }

        private void LoadHubEditorFromSelection()
        {
            if (dgvConfiguredHubs.SelectedRows.Count == 0) return;
            var row = dgvConfiguredHubs.SelectedRows[0];
            txtHubName.Text   = row.Cells["colName"].Value?.ToString()   ?? "";
            txtHubSerial.Text = row.Cells["colSerial"].Value?.ToString() ?? "";
            chkHubEnabled.Checked = row.Cells["colEnabled"].Value is bool b && b;
        }

        private void BtnAddHub_Click(object sender, EventArgs e)
        {
            string name = txtHubName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Enter a hub name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!int.TryParse(txtHubSerial.Text.Trim(), out int serial))
            {
                MessageBox.Show("Enter a valid serial number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _hubs.Add(new PhidgetsHubInst { Name = name, Serial = serial, Enabled = chkHubEnabled.Checked });
            LoadHubsGrid();
        }

        private void BtnDeleteHub_Click(object sender, EventArgs e)
        {
            if (dgvConfiguredHubs.SelectedRows.Count == 0) return;
            var name = dgvConfiguredHubs.SelectedRows[0].Cells["colName"].Value?.ToString() ?? "";
            if (MessageBox.Show($"Delete hub '{name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _hubs.Delete(name);
            LoadHubsGrid();
        }

        private void BtnScan_Click(object sender, EventArgs e)
        {
            try
            {
                var hubs = _hubs.GetAll();
                using (var form = new ManageHubsForm(hubs))
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        _hubs.SaveAll(form.Hubs);
                        DisplayInfoLog("Hubs configuration saved.");
                        LoadHubsGrid();
                    }
                }
            }
            catch (Exception ex) { DisplayErrorLog("Error scanning hubs: " + ex.Message); }
        }

        // ── Devices / Mapping Studio page ─────────────────────────────────────────
        private void CatBtn_Click(object sender, EventArgs e)
        {
            if (!(sender is Button btn)) return;
            _activeDeviceCategory = btn.Tag?.ToString() ?? btn.Text;

            // Highlight active category
            foreach (Control c in flowCategories.Controls)
                if (c is Button cb) { cb.BackColor = Color.FromArgb(241, 245, 249); cb.ForeColor = Color.FromArgb(51, 65, 85); }
            btn.BackColor = Color.FromArgb(59, 130, 246);
            btn.ForeColor = Color.White;

            LoadDeviceListForCategory(_activeDeviceCategory);
            ClearDeviceEditor();
        }

        private void LoadDeviceListForCategory(string category)
        {
            lstDevices.Items.Clear();
            lblCardItemTitle.Text = category + " — select or add";
            try
            {
                var cfg = _config.Load();
                if (cfg == null) return;
                var search = txtDeviceSearch.Text.ToLower();

                switch (category)
                {
                    case "Outputs":
                        var outs = cfg?.PhidgetsOutputInstances ?? new List<PhidgetsOutputInst>();
                        foreach (var o in outs.Where(x => search == "" || x.ProsimDataRef?.ToLower().Contains(search) == true))
                            lstDevices.Items.Add(new DeviceListItem { Label = $"Hub:{x_Short(o.Serial)} P:{o.HubPort} Ch:{o.Channel}  → {o.ProsimDataRef}", Tag = o });
                        break;
                    case "Inputs":
                        var ins = cfg?.PhidgetsInputInstances ?? new List<PhidgetsInputInst>();
                        foreach (var i in ins.Where(x => search == "" || x.ProsimDataRef?.ToLower().Contains(search) == true))
                            lstDevices.Items.Add(new DeviceListItem { Label = $"Hub:{x_Short(i.Serial)} P:{i.HubPort} Ch:{i.Channel}  → {i.ProsimDataRef}", Tag = i });
                        break;
                    case "Encoders":
                        var encs = cfg?.PhidgetsEncoderInstances ?? new List<PhidgetsEncoderInst>();
                        foreach (var enc in encs.Where(x => search == "" || x.ProsimDataRef?.ToLower().Contains(search) == true))
                            lstDevices.Items.Add(new DeviceListItem { Label = $"Hub:{x_Short(enc.Serial)} P:{enc.HubPort} Ch:{enc.Channel}  → {enc.ProsimDataRef}", Tag = enc });
                        break;
                    case "Gates":
                        var gates = cfg?.PhidgetsGateInstances ?? new List<PhidgetsGateInst>();
                        foreach (var g in gates.Where(x => search == "" || x.ProsimDataRef?.ToLower().Contains(search) == true))
                            lstDevices.Items.Add(new DeviceListItem { Label = $"Hub:{x_Short(g.Serial)} P:{g.HubPort} Ch:{g.Channel}  → {g.ProsimDataRef}", Tag = g });
                        break;
                    case "Voltage Out":
                        var vouts = cfg?.PhidgetsVoltageOutputInstances ?? new List<PhidgetsVoltageOutputInst>();
                        foreach (var v in vouts.Where(x => search == "" || x.ProsimDataRef?.ToLower().Contains(search) == true))
                            lstDevices.Items.Add(new DeviceListItem { Label = $"Hub:{x_Short(v.Serial)} P:{v.HubPort}  → {v.ProsimDataRef}", Tag = v });
                        break;
                    case "Voltage In":
                        var vins = cfg?.PhidgetsVoltageInputInstances ?? new List<PhidgetsVoltageInputInst>();
                        foreach (var vi in vins.Where(x => search == "" || x.ProsimDataRef?.ToLower().Contains(search) == true))
                            lstDevices.Items.Add(new DeviceListItem { Label = $"Hub:{x_Short(vi.Serial)} P:{vi.HubPort} Ch:{vi.Channel}  → {vi.ProsimDataRef}", Tag = vi });
                        break;
                    case "Multi-Input":
                        var mults = cfg?.PhidgetsMultiInputInstances ?? new List<PhidgetsMultiInputInst>();
                        foreach (var m in mults.Where(x => search == "" || x.ProsimDataRef?.ToLower().Contains(search) == true))
                            lstDevices.Items.Add(new DeviceListItem { Label = $"Hub:{x_Short(m.Serial)} P:{m.HubPort}  → {m.ProsimDataRef}", Tag = m });
                        break;
                    case "Buttons":
                        var btns = cfg?.PhidgetsButtonInstances ?? new List<PhidgetsButtonInst>();
                        foreach (var bt in btns.Where(x => search == "" || x.Name?.ToLower().Contains(search) == true))
                            lstDevices.Items.Add(new DeviceListItem { Label = $"Hub:{x_Short(bt.Serial)} P:{bt.HubPort} Ch:{bt.Channel}  → {bt.Name}", Tag = bt });
                        break;
                }
            }
            catch (Exception ex) { DisplayErrorLog("Error loading device list: " + ex.Message); }
        }

        private static string x_Short(int serial) => serial == 0 ? "?" : serial.ToString();

        private void TxtDeviceSearch_TextChanged(object sender, EventArgs e) => LoadDeviceListForCategory(_activeDeviceCategory);

        private void LstDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstDevices.SelectedItem is DeviceListItem item)
                LoadDeviceEditorFromItem(item);
        }

        private void LoadDeviceEditorFromItem(DeviceListItem item)
        {
            ClearDeviceEditor();
            var hubs = _hubs.GetAll();
            PopulateHubDropdown(cboDeviceHub, hubs);

            switch (item.Tag)
            {
                case PhidgetsOutputInst o:
                    lblCardItemTitle.Text = "Edit Output";
                    SetHubAndPortChannel(o.Serial, o.HubPort, o.Channel, hubs);
                    txtDeviceProsimRef.Text = o.ProsimDataRef ?? "";
                    ShowAdvancedPanel(pnlAdvancedProperties_Outputs);
                    txtOutputRefOff.Text  = o.ProsimDataRefOff ?? "";
                    txtOutputRef2.Text    = ""; // ProsimDataRef2 is not on PhidgetsOutputInst
                    txtOutputUserVar.Text = o.UserVariable      ?? "";
                    chkOutputInverse.Checked      = o.Inverse == true;
                    txtOutputDelayOn.Value         = o.DelayOn  ?? 0;
                    txtOutputMaxTimeOn.Value        = o.MaxTimeOn ?? 0;
                    break;
                case PhidgetsInputInst i:
                    lblCardItemTitle.Text = "Edit Input";
                    SetHubAndPortChannel(i.Serial, i.HubPort, i.Channel, hubs);
                    txtDeviceProsimRef.Text = i.ProsimDataRef ?? "";
                    ShowAdvancedPanel(pnlAdvancedProperties_Inputs);
                    break;
                case PhidgetsEncoderInst enc:
                    lblCardItemTitle.Text = "Edit Encoder";
                    SetHubAndPortChannel(enc.Serial, enc.HubPort, enc.Channel, hubs);
                    txtDeviceProsimRef.Text = enc.ProsimDataRef ?? "";
                    txtEncoderScale.Value   = (decimal)(enc.ScaleFactor != 0 ? enc.ScaleFactor : 1.0);
                    ShowAdvancedPanel(pnlAdvancedProperties_Inputs);
                    break;
                case PhidgetsGateInst g:
                    lblCardItemTitle.Text = "Edit Gate";
                    SetHubAndPortChannel(g.Serial, g.HubPort, g.Channel, hubs);
                    txtDeviceProsimRef.Text = g.ProsimDataRef ?? "";
                    ShowAdvancedPanel(pnlAdvancedProperties_Outputs);
                    break;
                case PhidgetsVoltageOutputInst v:
                    lblCardItemTitle.Text = "Edit Voltage Output";
                    SetHubAndPortChannel(v.Serial, v.HubPort, 0, hubs);
                    txtDeviceProsimRef.Text = v.ProsimDataRef ?? "";
                    ShowAdvancedPanel(pnlAdvancedProperties_Voltages);
                    txtVoltageScale.Value  = (decimal)v.ScaleFactor;
                    txtVoltageOffset.Value = (decimal)v.Offset;
                    txtVoltageInterval.Value = v.Interval;
                    chkVoltageUseSinCos.Checked = v.UseSinCos;
                    break;
                case PhidgetsVoltageInputInst vi:
                    lblCardItemTitle.Text = "Edit Voltage Input";
                    SetHubAndPortChannel(vi.Serial, vi.HubPort, vi.Channel, hubs);
                    txtDeviceProsimRef.Text = vi.ProsimDataRef ?? "";
                    ShowAdvancedPanel(pnlAdvancedProperties_Voltages);
                    break;
                case PhidgetsButtonInst bt:
                    lblCardItemTitle.Text = "Edit Button";
                    SetHubAndPortChannel(bt.Serial, bt.HubPort, bt.Channel, hubs);
                    txtDeviceProsimRef.Text = bt.ProsimDataRef ?? "";
                    txtButtonName.Text      = bt.Name ?? "";
                    ShowAdvancedPanel(pnlAdvancedProperties_Inputs);
                    break;
            }
        }

        private void ShowAdvancedPanel(Panel panel)
        {
            pnlAdvancedProperties_Inputs.Visible   = panel == pnlAdvancedProperties_Inputs;
            pnlAdvancedProperties_Outputs.Visible  = panel == pnlAdvancedProperties_Outputs;
            pnlAdvancedProperties_Motors.Visible   = panel == pnlAdvancedProperties_Motors;
            pnlAdvancedProperties_Voltages.Visible = panel == pnlAdvancedProperties_Voltages;
        }

        private void ClearDeviceEditor()
        {
            lblCardItemTitle.Text = _activeDeviceCategory + " — select or add";
            cboDeviceHub.Items.Clear();
            cboDeviceHubPort.Items.Clear();
            cboDeviceChannel.Items.Clear();
            txtDeviceProsimRef.Text = "";
            pnlAdvancedProperties_Inputs.Visible   = false;
            pnlAdvancedProperties_Outputs.Visible  = false;
            pnlAdvancedProperties_Motors.Visible   = false;
            pnlAdvancedProperties_Voltages.Visible = false;
        }

        private void PopulateHubDropdown(ComboBox cbo, List<PhidgetsHubInst> hubs)
        {
            cbo.Items.Clear();
            foreach (var h in hubs) cbo.Items.Add(h.Name);
            if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;
        }

        private void SetHubAndPortChannel(int serial, int hubPort, int channel, List<PhidgetsHubInst> hubs)
        {
            var hub = hubs.FirstOrDefault(h => h.Serial == serial);
            if (hub != null) cboDeviceHub.Text = hub.Name;

            cboDeviceHubPort.Items.Clear();
            for (int i = 0; i <= 5; i++) cboDeviceHubPort.Items.Add(i.ToString());
            cboDeviceHubPort.Text = hubPort.ToString();

            cboDeviceChannel.Items.Clear();
            for (int i = 0; i <= 7; i++) cboDeviceChannel.Items.Add(i.ToString());
            cboDeviceChannel.Text = channel.ToString();
        }

        private void BtnAddDevice_Click(object sender, EventArgs e)
        {
            var hubs = _hubs.GetAll();
            PopulateHubDropdown(cboDeviceHub, hubs);
            lblCardItemTitle.Text = "New " + _activeDeviceCategory;
            ClearDeviceEditor();
            PopulateHubDropdown(cboDeviceHub, hubs);
            SetHubAndPortChannel(0, 0, 0, hubs);
            switch (_activeDeviceCategory)
            {
                case "Outputs":     ShowAdvancedPanel(pnlAdvancedProperties_Outputs);  break;
                case "Voltage Out": ShowAdvancedPanel(pnlAdvancedProperties_Voltages); break;
                case "Voltage In":  ShowAdvancedPanel(pnlAdvancedProperties_Voltages); break;
                default:            ShowAdvancedPanel(pnlAdvancedProperties_Inputs);   break;
            }
        }

        private void BtnDeleteDevice_Click(object sender, EventArgs e)
        {
            if (!(lstDevices.SelectedItem is DeviceListItem item)) return;
            if (MessageBox.Show("Delete this device mapping?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            DeleteDeviceFromConfig(item.Tag);
            DisplayInfoLog("Device mapping deleted.");
            LoadDeviceListForCategory(_activeDeviceCategory);
        }

        private void BtnSaveDevice_Click(object sender, EventArgs e)
        {
            // Collect common fields
            var hubs = _hubs.GetAll();
            string hubName = cboDeviceHub.Text;
            var hub = hubs.FirstOrDefault(h => h.Name == hubName);
            int serial  = hub?.Serial ?? 0;
            int hubPort = int.TryParse(cboDeviceHubPort.Text, out int hp) ? hp : 0;
            int channel = int.TryParse(cboDeviceChannel.Text, out int ch) ? ch : 0;
            string prosimRef = txtDeviceProsimRef.Text.Trim();

            // Route to type-specific save
            SaveDeviceToConfig(serial, hubPort, channel, prosimRef);
            LoadDeviceListForCategory(_activeDeviceCategory);
            DisplayInfoLog("Device mapping saved.");
        }

        private void SaveDeviceToConfig(int serial, int hubPort, int channel, string prosimRef)
        {
            try
            {
                var cfg = _config.Load() ?? new Config();
                var existing = lstDevices.SelectedItem as DeviceListItem;

                switch (_activeDeviceCategory)
                {
                    case "Outputs":
                        cfg.PhidgetsOutputInstances = cfg.PhidgetsOutputInstances ?? new List<PhidgetsOutputInst>();
                        if (existing?.Tag is PhidgetsOutputInst oldO) cfg.PhidgetsOutputInstances.Remove(oldO);
                        cfg.PhidgetsOutputInstances.Add(new PhidgetsOutputInst
                        {
                            Serial = serial, HubPort = hubPort, Channel = channel, ProsimDataRef = prosimRef,
                            ProsimDataRefOff = txtOutputRefOff.Text.NullIfEmpty(),
                            UserVariable     = txtOutputUserVar.Text.NullIfEmpty(),
                            Inverse          = chkOutputInverse.Checked ? (bool?)true : null,
                            DelayOn          = txtOutputDelayOn.Value > 0 ? (int?)((int)txtOutputDelayOn.Value) : null,
                            MaxTimeOn        = txtOutputMaxTimeOn.Value > 0 ? (int?)((int)txtOutputMaxTimeOn.Value) : null,
                        });
                        break;
                    case "Inputs":
                        cfg.PhidgetsInputInstances = cfg.PhidgetsInputInstances ?? new List<PhidgetsInputInst>();
                        if (existing?.Tag is PhidgetsInputInst oldI) cfg.PhidgetsInputInstances.Remove(oldI);
                        cfg.PhidgetsInputInstances.Add(new PhidgetsInputInst { Serial = serial, HubPort = hubPort, Channel = channel, ProsimDataRef = prosimRef });
                        break;
                    case "Encoders":
                        cfg.PhidgetsEncoderInstances = cfg.PhidgetsEncoderInstances ?? new List<PhidgetsEncoderInst>();
                        if (existing?.Tag is PhidgetsEncoderInst oldE) cfg.PhidgetsEncoderInstances.Remove(oldE);
                        cfg.PhidgetsEncoderInstances.Add(new PhidgetsEncoderInst { Serial = serial, HubPort = hubPort, Channel = channel, ProsimDataRef = prosimRef, ScaleFactor = (double)txtEncoderScale.Value });
                        break;
                    case "Gates":
                        cfg.PhidgetsGateInstances = cfg.PhidgetsGateInstances ?? new List<PhidgetsGateInst>();
                        if (existing?.Tag is PhidgetsGateInst oldG) cfg.PhidgetsGateInstances.Remove(oldG);
                        cfg.PhidgetsGateInstances.Add(new PhidgetsGateInst { Serial = serial, HubPort = hubPort, Channel = channel, ProsimDataRef = prosimRef });
                        break;
                }

                WriteConfig(cfg);
            }
            catch (Exception ex) { DisplayErrorLog("Save error: " + ex.Message); }
        }

        private void DeleteDeviceFromConfig(object tag)
        {
            try
            {
                var cfg = _config.Load();
                if (cfg == null) return;
                if (tag is PhidgetsOutputInst o)      cfg.PhidgetsOutputInstances?.Remove(o);
                else if (tag is PhidgetsInputInst i)   cfg.PhidgetsInputInstances?.Remove(i);
                else if (tag is PhidgetsEncoderInst e) cfg.PhidgetsEncoderInstances?.Remove(e);
                else if (tag is PhidgetsGateInst g)    cfg.PhidgetsGateInstances?.Remove(g);
                WriteConfig(cfg);
            }
            catch (Exception ex) { DisplayErrorLog("Delete error: " + ex.Message); }
        }

        // ── Settings page ─────────────────────────────────────────────────────────
        private void LoadSettingsPage()
        {
            try
            {
                var cfg = _config.Load();
                if (cfg?.GeneralConfig == null) return;

                var gc = cfg.GeneralConfig;
                txtBlinkFast.Value   = gc.OutputBlinkFastIntervalMs > 0 ? gc.OutputBlinkFastIntervalMs : 300;
                txtBlinkSlow.Value   = gc.OutputBlinkSlowIntervalMs > 0 ? gc.OutputBlinkSlowIntervalMs : 600;
                txtDefaultDim.Value  = gc.OutputDefaultDimValue > 0 ? (decimal)gc.OutputDefaultDimValue : 70;

                // Wire save on change (attach once)
                txtBlinkFast.ValueChanged  -= NudSettings_ValueChanged;
                txtBlinkSlow.ValueChanged  -= NudSettings_ValueChanged;
                txtDefaultDim.ValueChanged -= NudSettings_ValueChanged;
                txtBlinkFast.ValueChanged  += NudSettings_ValueChanged;
                txtBlinkSlow.ValueChanged  += NudSettings_ValueChanged;
                txtDefaultDim.ValueChanged += NudSettings_ValueChanged;
            }
            catch (Exception ex) { DisplayErrorLog("Error loading settings: " + ex.Message); }
        }

        private void NudSettings_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                var cfg = _config.Load();
                if (cfg?.GeneralConfig == null) return;
                cfg.GeneralConfig.OutputBlinkFastIntervalMs = (int)txtBlinkFast.Value;
                cfg.GeneralConfig.OutputBlinkSlowIntervalMs = (int)txtBlinkSlow.Value;
                cfg.GeneralConfig.OutputDefaultDimValue     = (double)txtDefaultDim.Value;
                _config.Save(cfg);
                _devices.BlinkFastIntervalMs = cfg.GeneralConfig.OutputBlinkFastIntervalMs;
                _devices.BlinkSlowIntervalMs = cfg.GeneralConfig.OutputBlinkSlowIntervalMs;
                _devices.DefaultDimValue     = cfg.GeneralConfig.OutputDefaultDimValue;
            }
            catch (Exception ex) { DisplayErrorLog("Error saving settings: " + ex.Message); }
        }

        // ── YAML helpers (thin shims for device-editor methods still using WriteConfig) ──
        private void WriteConfig(Config cfg) => _config.Save(cfg);

        // ── Schema migration (delegate to ConfigRepository) ───────────────────────
        private bool CheckAndMigrateConfig()
        {
            return _config.CheckAndMigrate(stubs => new ManageHubsForm(stubs));
        }
    }

    // ── Small helper class for ListBox items ──────────────────────────────────────
    internal class DeviceListItem
    {
        public string Label { get; set; }
        public object Tag   { get; set; }
        public override string ToString() => Label ?? "";
    }

    // ── String extension helper ───────────────────────────────────────────────────
    internal static class StringExtensions
    {
        public static string NullIfEmpty(this string s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
