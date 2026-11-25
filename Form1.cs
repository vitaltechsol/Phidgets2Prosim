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
        private readonly HubWorkflowService _hubWorkflow = new HubWorkflowService();
        private List<DiscoveredHubInfo> _discoveredHubs = new List<DiscoveredHubInfo>();
        private bool _isLoadingHubsGrid = false;
        private object _currentEditingDeviceTag = null;
        private bool _suppressMultiChannelsChange = false;

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
            btnNavDevices.Click   += (s, e) => OnDevicesPageShown();
            btnNavSpecial.Click   += (s, e) => ShowPage(pnlPageSpecial,   btnNavSpecial);
            btnNavSettings.Click  += (s, e) => ShowPage(pnlPageSettings,  btnNavSettings);

            // Wire log buttons
            btnLogClear.Click += BtnLogClear_Click;
            btnLogOk.Click    += BtnLogOk_Click;

            // Wire Dashboard – ProSim connection
            btnSaveProsimIP.Click    += BtnSaveProsimIP_Click;
            btnConnectProsim.Click   += BtnConnectProsim_Click;

            // Wire Hubs page
            btnAddHub.Click           += BtnAddHub_Click;
            btnDeleteHub.Click        += BtnDeleteHub_Click;
            btnScan.Click             += BtnScan_Click;
            btnAddFromDiscovered.Click += BtnAddFromDiscovered_Click;
            dgvConfiguredHubs.SelectionChanged += (s, e) => LoadHubEditorFromSelection();
            dgvConfiguredHubs.KeyDown += DgvConfiguredHubs_KeyDown;
            dgvConfiguredHubs.CurrentCellDirtyStateChanged += DgvConfiguredHubs_CurrentCellDirtyStateChanged;
            dgvConfiguredHubs.CellValueChanged += DgvConfiguredHubs_CellValueChanged;

            // Wire Devices page category filter
            foreach (Control c in flowCategories.Controls)
                if (c is Button catBtn)
                    catBtn.Click += CatBtn_Click;

            // Wire device list & editor buttons
            lstDevices.SelectedIndexChanged += LstDevices_SelectedIndexChanged;
            lstDevices.MouseDown += LstDevices_MouseDown;
            lstDevices.MouseClick += (s, e) => LoadSelectedDeviceEditor();
            btnAddDevice.Click    += BtnAddDevice_Click;
            btnDeleteDevice.Click += BtnDeleteDevice_Click;
            btnSaveDevice.Click   += BtnSaveDevice_Click;
            txtDeviceSearch.TextChanged += TxtDeviceSearch_TextChanged;
            if (btnMultiGenerateMappings != null) btnMultiGenerateMappings.Click += (s, e) => RebuildMultiMappingsGrid();
            if (clbMultiChannels != null) clbMultiChannels.ItemCheck += ClbMultiChannels_ItemCheck;

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
            if (_prosim.IsConnected)
            {
                DisconnectProsim();
                return;
            }

            ConnectProsim();
        }

        private void ConnectProsim()
        {
            string ip = txtProsimIP.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";
            connectionStatusLabel.Text = "● CONNECTING TO " + ip;
            connectionStatusLabel.ForeColor = Color.FromArgb(14, 165, 233);
            btnConnectProsim.Text = "Connecting...";
            btnConnectProsim.Enabled = false;
            _ = _prosim.ConnectAsync(ip);
        }

        private void DisconnectProsim()
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
                btnConnectProsim.Text = "Disconnect";
                btnConnectProsim.BackColor = Color.FromArgb(239, 68, 68);
                btnConnectProsim.Enabled = true;
            }
            else
            {
                DisplayInfoLog("ProSim DISCONNECTED");
                connectionStatusLabel.Text      = "● DISCONNECTED";
                connectionStatusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                btnConnectProsim.Text = "Connect";
                btnConnectProsim.BackColor = Color.FromArgb(16, 185, 129);
                btnConnectProsim.Enabled = true;
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
                Font      = new Font("Segoe UI", 13F, FontStyle.Bold),
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
                btnConnectProsim.Text = "Loading...";
                btnConnectProsim.Enabled = false;

                Task.Run(async () =>
                {
                    try
                    {
                        var cfg = _config.Load();
                        _hubs.RegisterHubs(cfg?.PhidgetsHubsInstances);
                        int totalOuts = CountStartupOutputDevices(cfg);
                        string prosimIp = string.IsNullOrWhiteSpace(cfg?.GeneralConfig?.ProSimIP) ? "127.0.0.1" : cfg.GeneralConfig.ProSimIP;

                        DisplayInfoLog("Prosim IP: " + prosimIp);
                        DisplayInfoLog("Opening outputs: " + totalOuts);

                        _devices.LoadOutputDevices(cfg);
                        BeginInvoke(new Action(UpdateTelemetryPanel));

                        await Task.Delay((totalOuts + 10) * 40);
                        DisplayInfoLog("Outputs loaded successfully");
                        DisplayInfoLog("Connecting to Prosim");

                        if (!IsDisposed)
                            BeginInvoke(new Action(ConnectProsim));
                    }
                    catch (Exception ex)
                    {
                        DisplayErrorLog("Error loading config: " + ex.Message);
                        if (!IsDisposed)
                            BeginInvoke(new Action(() =>
                            {
                                btnConnectProsim.Text = "Connect";
                                btnConnectProsim.BackColor = Color.FromArgb(16, 185, 129);
                                btnConnectProsim.Enabled = true;
                            }));
                    }
                });
            }
            else
            {
                DisplayErrorLog("Config is still schema 1.0 – migration was not completed. Please restart and complete the hub migration before connecting.");
            }

            _prosim.SubscribePause();
            LoadSettingsPage();
        }

        private int CountStartupOutputDevices(Config cfg)
        {
            if (cfg == null) return 0;

            int total = 0;
            total += cfg.PhidgetsOutputInstances?.Count ?? 0;
            total += cfg.PhidgetsAudioInstances?.Count ?? 0;
            total += cfg.PhidgetsGateInstances?.Count ?? 0;
            total += cfg.PhidgetsVoltageOutputInstances?.Count ?? 0;
            total += cfg.PhidgetsBLDCMotorInstances?.Count ?? 0;
            total += cfg.PhidgetsDCMotorInstances?.Count ?? 0;
            total += cfg.PhidgetsEncoderInstances?.Count ?? 0;
            total += cfg.PhidgetsButtonInstances?.Count ?? 0;
            total += cfg.PhidgetsLinkInstances?.Count ?? 0;
            if (cfg.CustomTrimWheelInstance != null) total++;
            if (cfg.CustomParkingBrakeInstance != null) total++;

            return total;
        }

        private void Form1_Closed(object sender, EventArgs e)
        {
            _devices.UnloadInputDevices();
            _devices.UnloadLinks();
            Debug.WriteLine("closed");
        }

        // ── Hub page ──────────────────────────────────────────────────────────────
        private void LoadHubsGrid()
        {
            try
            {
                _isLoadingHubsGrid = true;
                var hubs = _hubs.GetAll() ?? new List<PhidgetsHubInst>();

                dgvConfiguredHubs.AutoGenerateColumns = false;
                dgvConfiguredHubs.AllowUserToDeleteRows = false;
                dgvConfiguredHubs.AllowUserToResizeRows = false;
                dgvConfiguredHubs.DataSource = null;

                if (dgvConfiguredHubs.Columns.Count == 0)
                {
                    dgvConfiguredHubs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Name", DataPropertyName = "Name", FillWeight = 50, ReadOnly = true });
                    dgvConfiguredHubs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSerial", HeaderText = "Serial", DataPropertyName = "Serial", FillWeight = 30, ReadOnly = true });
                    dgvConfiguredHubs.Columns.Add(new DataGridViewCheckBoxColumn { Name = "colEnabled", HeaderText = "Enabled", DataPropertyName = "Enabled", FillWeight = 20, ReadOnly = false });
                }

                dgvConfiguredHubs.DataSource = new BindingList<PhidgetsHubInst>(hubs);

                if (dgvConfiguredHubs.Rows.Count > 0)
                {
                    dgvConfiguredHubs.ClearSelection();
                    dgvConfiguredHubs.Rows[dgvConfiguredHubs.Rows.Count - 1].Selected = true;
                }

                UpdateDiscoveredHubsDisplay();
            }
            catch (Exception ex) { DisplayErrorLog("Error loading hubs: " + ex.Message); }
            finally { _isLoadingHubsGrid = false; }
        }

        private void DgvConfiguredHubs_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvConfiguredHubs.IsCurrentCellDirty)
                dgvConfiguredHubs.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DgvConfiguredHubs_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isLoadingHubsGrid || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvConfiguredHubs.Columns[e.ColumnIndex].Name != "colEnabled") return;

            var bound = dgvConfiguredHubs.DataSource as BindingList<PhidgetsHubInst>;
            if (bound == null) return;

            _hubs.SaveAll(bound.ToList());
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

        private void DgvConfiguredHubs_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;
            BtnDeleteHub_Click(sender, EventArgs.Empty);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private async void BtnScan_Click(object sender, EventArgs e)
        {
            btnScan.Enabled = false;
            btnScan.Text = "Scanning...";
            lvDiscoveredHubs.Items.Clear();
            _discoveredHubs = new List<DiscoveredHubInfo>();

            try
            {
                _discoveredHubs = await _hubWorkflow.ScanAsync();
            }
            catch (Exception ex)
            {
                DisplayErrorLog("Error scanning for hubs: " + ex.Message);
            }

            UpdateDiscoveredHubsDisplay();
            btnScan.Enabled = true;
            btnScan.Text = "Scan Network";
        }

        private void UpdateDiscoveredHubsDisplay()
        {
            if (lvDiscoveredHubs.InvokeRequired) { lvDiscoveredHubs.Invoke(new Action(UpdateDiscoveredHubsDisplay)); return; }

            lvDiscoveredHubs.BeginUpdate();
            lvDiscoveredHubs.Items.Clear();
            var configuredSerials = _hubWorkflow.GetConfiguredSerials(_hubs.GetAll());

            foreach (var hub in _discoveredHubs)
            {
                var item = new ListViewItem(hub.Serial.ToString());
                item.SubItems.Add(hub.DeviceName);
                item.SubItems.Add(hub.ServerName);
                item.Tag = hub;
                if (configuredSerials.Contains(hub.Serial))
                    item.ForeColor = Color.Gray;
                lvDiscoveredHubs.Items.Add(item);
            }

            lvDiscoveredHubs.EndUpdate();
        }

        private void BtnAddFromDiscovered_Click(object sender, EventArgs e)
        {
            if (lvDiscoveredHubs.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a discovered hub to add.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool anyAdded = false;
            var configuredHubs = _hubs.GetAll();

            foreach (ListViewItem item in lvDiscoveredHubs.SelectedItems)
            {
                var hub = item.Tag as DiscoveredHubInfo;
                if (hub == null) continue;

                if (!_hubWorkflow.TryAddDiscoveredHub(configuredHubs, hub))
                {
                    MessageBox.Show("Hub " + hub.Serial + " is already configured.",
                        "Already Configured", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }

                anyAdded = true;
            }

            if (anyAdded)
            {
                _hubs.SaveAll(configuredHubs);
                DisplayInfoLog("Hub(s) added from network scan.");
                LoadHubsGrid();
                UpdateDiscoveredHubsDisplay();
            }
        }

        // ── Devices / Mapping Studio page ─────────────────────────────────────────
        private bool _devicesPageInitialized;

        private void OnDevicesPageShown()
        {
            if (!_devicesPageInitialized)
            {
                // First visit: open the first category on its empty "new instance" form
                _devicesPageInitialized = true;
                (flowCategories.Controls.OfType<Button>().FirstOrDefault())?.PerformClick();
                return;
            }

            // Hubs are app-wide and may have changed on the Hubs page: refresh the shared dropdown, keep the selection
            string selected = cboDeviceHub.Text;
            PopulateHubDropdown(cboDeviceHub, _hubs.GetAll());
            if (cboDeviceHub.Items.Contains(selected)) cboDeviceHub.Text = selected;
        }

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

            // Every category opens on the empty "new instance" form
            BtnAddDevice_Click(this, EventArgs.Empty);
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

                // Index = position in the config list, so edits/deletes target the right entry
                void Fill<T>(List<T> list, Func<T, string> key, Func<T, string> label)
                {
                    if (list == null) return;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (search != "" && key(list[i])?.ToLower().Contains(search) != true) continue;
                        lstDevices.Items.Add(new DeviceListItem { Label = label(list[i]), Tag = list[i], Index = i });
                    }
                }

                switch (category)
                {
                    case "Outputs":
                        Fill(cfg.PhidgetsOutputInstances, x => x.ProsimDataRef, x => BuildDeviceListLabel(x.Serial, x.HubPort, x.Channel, x.ProsimDataRef));
                        break;
                    case "Inputs":
                        Fill(cfg.PhidgetsInputInstances, x => x.ProsimDataRef, x => BuildDeviceListLabel(x.Serial, x.HubPort, x.Channel, x.ProsimDataRef));
                        break;
                    case "Encoders":
                        Fill(cfg.PhidgetsEncoderInstances, x => x.ProsimDataRef, x => BuildDeviceListLabel(x.Serial, x.HubPort, x.Channel, x.ProsimDataRef));
                        break;
                    case "Gates":
                        Fill(cfg.PhidgetsGateInstances, x => x.ProsimDataRef, x => BuildDeviceListLabel(x.Serial, x.HubPort, x.Channel, x.ProsimDataRef));
                        break;
                    case "Voltage Out":
                        Fill(cfg.PhidgetsVoltageOutputInstances, x => x.ProsimDataRef, x => BuildDeviceListLabel(x.Serial, x.HubPort, null, x.ProsimDataRef));
                        break;
                    case "Voltage In":
                        Fill(cfg.PhidgetsVoltageInputInstances, x => x.ProsimDataRef, x => BuildDeviceListLabel(x.Serial, x.HubPort, x.Channel, x.ProsimDataRef));
                        break;
                    case "Multi-Input":
                        Fill(cfg.PhidgetsMultiInputInstances, x => x.ProsimDataRef, x => BuildDeviceListLabel(x.Serial, x.HubPort, null, x.ProsimDataRef));
                        break;
                    case DcMotorCategory:
                        LoadDcMotorList(cfg, search);
                        break;
                    case LinkCategory:
                        Fill(cfg.PhidgetsLinkInstances, x => x.Name ?? ("C" + x.Channel), x => LinkLabel(x));
                        break;
                    case "Buttons":
                        Fill(cfg.PhidgetsButtonInstances, x => x.Name, x => BuildDeviceListLabel(x.Serial, x.HubPort, x.Channel, x.Name));
                        break;
                }
            }
            catch (Exception ex) { DisplayErrorLog("Error loading device list: " + ex.Message); }
        }

        private static string x_Short(int serial) => serial == 0 ? "?" : serial.ToString();

        private static string BuildDeviceListLabel(int serial, int hubPort, int? channel, string value)
        {
            var portText = channel.HasValue
                ? $"H:{x_Short(serial)} P:{hubPort} C:{channel.Value}"
                : $"H:{x_Short(serial)} P:{hubPort}";

            return portText + "  " + ShortDeviceValue(value, 16);
        }

        private static string ShortDeviceValue(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "-";
            value = value.Trim();
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 1) + "…";
        }

        private void TxtDeviceSearch_TextChanged(object sender, EventArgs e) => LoadDeviceListForCategory(_activeDeviceCategory);

        private void LstDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSelectedDeviceEditor();
        }

        private void LstDevices_MouseDown(object sender, MouseEventArgs e)
        {
            int index = lstDevices.IndexFromPoint(e.Location);
            if (index >= 0 && index < lstDevices.Items.Count)
            {
                lstDevices.SelectedIndex = index;
                LoadSelectedDeviceEditor();
            }
        }

        private void LoadSelectedDeviceEditor()
        {
            if (lstDevices.SelectedItem is DeviceListItem item)
                LoadDeviceEditorFromItem(item);
        }

        private void LoadDeviceEditorFromItem(DeviceListItem item)
        {
            _currentEditingDeviceTag = item?.Tag;
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
                    txtValueOn.Value  = ClampToRange((decimal)o.ValueOn, txtValueOn.Minimum, txtValueOn.Maximum);
                    txtValueOff.Value = ClampToRange((decimal)o.ValueOff, txtValueOff.Minimum, txtValueOff.Maximum);
                    txtValueDim.Value = ClampToRange((decimal)o.ValueDim, txtValueDim.Minimum, txtValueDim.Maximum);
                    break;
                case PhidgetsInputInst i:
                    lblCardItemTitle.Text = "Edit Input";
                    ShowAdvancedPanel(pnlAdvancedProperties_Inputs);
                    SetHubAndPortChannel(i.Serial, i.HubPort, i.Channel, hubs);
                    txtDeviceProsimRef.Text = i.ProsimDataRef ?? "";
                    txtInputValue.Value = i.InputValue;
                    txtOffInputValue.Value = i.OffInputValue;
                    txtInputRef2.Text = i.ProsimDataRef2 ?? "";
                    txtInputRef3.Text = i.ProsimDataRef3 ?? "";
                    txtInputUserVar.Text = i.UserVariable ?? "";
                    break;
                case PhidgetsMultiInputInst m:
                    lblCardItemTitle.Text = "Edit Multi-Input";
                    ShowAdvancedPanel(pnlAdvancedProperties_MultiInputs);
                    SetHubAndPortChannel(m.Serial, m.HubPort, 0, hubs);
                    txtDeviceProsimRef.Text = m.ProsimDataRef ?? "";
                    SetMultiInputEditorValues(m);
                    break;
                case PhidgetsEncoderInst enc:
                    lblCardItemTitle.Text = "Edit Encoder";
                    SetHubAndPortChannel(enc.Serial, enc.HubPort, enc.Channel, hubs);
                    txtDeviceProsimRef.Text = enc.ProsimDataRef ?? "";
                    ShowAdvancedPanel(pnlAdvancedProperties_Simple);
                    txtEncoderScale.Value   = (decimal)(enc.ScaleFactor != 0 ? enc.ScaleFactor : 1.0);
                    break;
                case PhidgetsGateInst g:
                    lblCardItemTitle.Text = "Edit Gate";
                    SetHubAndPortChannel(g.Serial, g.HubPort, g.Channel, hubs);
                    txtDeviceProsimRef.Text = g.ProsimDataRef ?? "";
                    ShowAdvancedPanel(pnlAdvancedProperties_Outputs);
                    txtOutputRefOff.Text   = g.ProsimDataRefOff ?? "";
                    txtOutputRef2.Text     = g.ProsimDataRef2 ?? "";
                    cboGateOperator.Text   = string.Equals(g.Operator, "AND", StringComparison.OrdinalIgnoreCase) ? "AND" : "OR";
                    chkOutputInverse.Checked = g.Inverse;
                    txtOutputDelayOn.Value   = g.DelayOn ?? 0;
                    txtOutputMaxTimeOn.Value = g.MaxTimeOn ?? 0;
                    break;
                case PhidgetsVoltageOutputInst v:
                    lblCardItemTitle.Text = "Edit Voltage Output";
                    SetHubAndPortChannel(v.Serial, v.HubPort, 0, hubs);
                    txtDeviceProsimRef.Text = v.ProsimDataRef ?? "";
                    ShowAdvancedPanel(pnlAdvancedProperties_Voltages);
                    LoadVoltageOutputFields(v);
                    break;
                case PhidgetsVoltageInputInst vi:
                    lblCardItemTitle.Text = "Edit Voltage Input";
                    SetHubAndPortChannel(vi.Serial, vi.HubPort, vi.Channel, hubs);
                    txtDeviceProsimRef.Text = vi.ProsimDataRef ?? "";
                    ShowAdvancedPanel(pnlAdvancedProperties_Voltages);
                    LoadVoltageInputFields(vi);
                    break;
                case DcMotorRef dc:
                    LoadDcMotorEditor(dc.Inst, hubs);
                    break;
                case PhidgetsLinkInst lk:
                    LoadLinkEditor(lk);
                    break;
                case PhidgetsButtonInst bt:
                    lblCardItemTitle.Text = "Edit Button";
                    SetHubAndPortChannel(bt.Serial, bt.HubPort, bt.Channel, hubs);
                    txtDeviceProsimRef.Text = bt.ProsimDataRef ?? "";
                    ShowAdvancedPanel(pnlAdvancedProperties_Simple);
                    txtButtonName.Text      = bt.Name ?? "";
                    break;
            }
        }

        private void ShowAdvancedPanel(Panel panel)
        {
            SetGeneralPropertiesVisibility(true);
            pnlAdvancedProperties_Inputs.Visible   = panel == pnlAdvancedProperties_Inputs;
            pnlAdvancedProperties_MultiInputs.Visible = panel == pnlAdvancedProperties_MultiInputs;
            pnlAdvancedProperties_Outputs.Visible  = panel == pnlAdvancedProperties_Outputs;
            pnlAdvancedProperties_Motors.Visible   = panel == pnlAdvancedProperties_Motors;
            pnlAdvancedProperties_Voltages.Visible = panel == pnlAdvancedProperties_Voltages;
            pnlAdvancedProperties_Simple.Visible   = panel == pnlAdvancedProperties_Simple;
            if (panel == pnlAdvancedProperties_Voltages) ReflowVoltageFields(_activeDeviceCategory == "Voltage Out");
            if (panel == pnlAdvancedProperties_Outputs) ApplyOutputFieldVisibility(_activeDeviceCategory == "Gates");
            lblEncoderScale.Visible = txtEncoderScale.Visible = _activeDeviceCategory == "Encoders";
            lblButtonName.Visible   = txtButtonName.Visible   = _activeDeviceCategory == "Buttons";
            if (pnlAdvancedProperties_DCMotors != null) pnlAdvancedProperties_DCMotors.Visible = false;
            btnSaveDevice.Visible = true;
        }

        private void ClearDeviceEditor()
        {
            _currentEditingDeviceTag = null;
            SetGeneralPropertiesVisibility(true);
            lblCardItemTitle.Text = _activeDeviceCategory + " — select or add";
            cboDeviceChannel.Items.Clear();
            txtDeviceProsimRef.Text = "";
            nudDeviceHubPort.Value = 0;

            txtInputValue.Value = 1;
            txtOffInputValue.Value = 0;
            txtInputRef2.Text = "";
            txtInputRef3.Text = "";
            txtInputUserVar.Text = "";
            txtEncoderScale.Value = 1;
            txtOutputRefOff.Text = ""; txtOutputRef2.Text = ""; txtOutputUserVar.Text = ""; cboGateOperator.Text = "OR";
            chkOutputInverse.Checked = false; txtOutputDelayOn.Value = 0; txtOutputMaxTimeOn.Value = 0;
            txtValueOn.Value = 1; txtValueOff.Value = 0; txtValueDim.Value = 0.7M;
            txtButtonName.Text = "";
            ResetVoltageFields();

            _suppressMultiChannelsChange = true;
            for (int i = 0; i < clbMultiChannels.Items.Count; i++)
                clbMultiChannels.SetItemChecked(i, false);
            _suppressMultiChannelsChange = false;
            dgvMultiMappings.Rows.Clear();

            pnlAdvancedProperties_Inputs.Visible   = false;
            pnlAdvancedProperties_MultiInputs.Visible = false;
            pnlAdvancedProperties_Outputs.Visible  = false;
            pnlAdvancedProperties_Motors.Visible   = false;
            pnlAdvancedProperties_Voltages.Visible = false;
            pnlAdvancedProperties_Simple.Visible   = false;
            ResetDcMotorEditor();
            ResetLinkEditor();
        }

        private static string HubText(PhidgetsHubInst h) => $"{h.Name} ({h.Serial})";

        // Hub list is app-wide (configured on the Hubs page); every category shares the same dropdown
        private void PopulateHubDropdown(ComboBox cbo, List<PhidgetsHubInst> hubs)
        {
            cbo.Items.Clear();
            foreach (var h in hubs) cbo.Items.Add(HubText(h));
            if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;
        }

        private void SetMultiInputEditorValues(PhidgetsMultiInputInst multi)
        {
            _suppressMultiChannelsChange = true;
            for (int i = 0; i < clbMultiChannels.Items.Count; i++)
                clbMultiChannels.SetItemChecked(i, false);

            if (multi.Channels != null)
            {
                foreach (var channel in multi.Channels)
                {
                    int idx = clbMultiChannels.Items.IndexOf(channel.ToString());
                    if (idx >= 0) clbMultiChannels.SetItemChecked(idx, true);
                }
            }
            _suppressMultiChannelsChange = false;

            RebuildMultiMappingsGrid(multi.Mappings);
        }

        // Shared form at the top of the editor: hub + hub port always; channel and ProSim ref only where the type uses them
        private void SetGeneralPropertiesVisibility(bool visible)
        {
            if (_activeDeviceCategory == LinkCategory) visible = false; // Links carry their own input + output hub selectors
            pnlGeneralProperties.Visible = visible;

            bool showChannel = _activeDeviceCategory != DcMotorCategory
                            && _activeDeviceCategory != "Voltage Out"
                            && _activeDeviceCategory != "Multi-Input";
            bool showRef = _activeDeviceCategory != DcMotorCategory;

            // controls: 0 lblHub, 1 hub, 2 lblPort, 3 port, 4 lblChannel, 5 channel, 6 lblRef, 7 ref
            var c = pnlGeneralProperties.Controls;
            int y = 0;
            void Place(int labelIdx, int inputIdx, bool show)
            {
                c[labelIdx].Visible = c[inputIdx].Visible = show;
                if (!show) return;
                c[labelIdx].Top = y + 5;
                c[inputIdx].Top = y + 2;
                y += 30;
            }
            Place(0, 1, true);
            Place(2, 3, true);
            Place(4, 5, showChannel);
            Place(6, 7, showRef);

            var layout = pnlGeneralProperties.Parent as TableLayoutPanel;
            if (layout == null) return;
            int row = layout.GetRow(pnlGeneralProperties);
            layout.RowStyles[row].SizeType = SizeType.Absolute;
            layout.RowStyles[row].Height = visible ? y + 10 : 0F;
        }

        private static decimal ClampToRange(decimal value, decimal min, decimal max) => value < min ? min : value > max ? max : value;

        private static decimal ClampToRange(int value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void ClbMultiChannels_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_suppressMultiChannelsChange) return;

            int checkedCount = clbMultiChannels.CheckedItems.Count + (e.NewValue == CheckState.Checked ? 1 : -1);
            if (checkedCount < 0) checkedCount = 0;
            if (checkedCount > 8)
            {
                e.NewValue = e.CurrentValue;
                MessageBox.Show("Select up to 8 channels to keep mappings manageable.", "Multi-Input", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BeginInvoke(new Action(() => RebuildMultiMappingsGrid()));
        }

        private List<int> GetCheckedMultiChannelsOrdered()
        {
            var result = new List<int>();
            foreach (var item in clbMultiChannels.CheckedItems)
            {
                if (int.TryParse(item?.ToString(), out int channel))
                    result.Add(channel);
            }
            return result.OrderBy(x => x).ToList();
        }

        private Dictionary<string, int> ReadMultiMappingsFromGrid()
        {
            var mappings = new Dictionary<string, int>();
            foreach (DataGridViewRow row in dgvMultiMappings.Rows)
            {
                if (row.IsNewRow) continue;
                var key = row.Cells[0].Value?.ToString();
                if (string.IsNullOrWhiteSpace(key)) continue;
                int value = 0;
                int.TryParse(row.Cells[1].Value?.ToString(), out value);
                mappings[key.Trim()] = value;
            }
            return mappings;
        }

        private void RebuildMultiMappingsGrid(Dictionary<string, int> existingMappings = null)
        {
            var channels = GetCheckedMultiChannelsOrdered();
            var currentValues = ReadMultiMappingsFromGrid();
            if (existingMappings != null)
            {
                foreach (var kv in existingMappings)
                    currentValues[kv.Key] = kv.Value;
            }

            dgvMultiMappings.Rows.Clear();
            if (channels.Count == 0) return;

            int combinations = 1 << channels.Count;
            for (int i = 0; i < combinations; i++)
            {
                string key = Convert.ToString(i, 2).PadLeft(channels.Count, '0');
                int value = currentValues.ContainsKey(key) ? currentValues[key] : 0;
                dgvMultiMappings.Rows.Add(key, value);
            }
        }

        private bool TryGetMultiInputMappings(out List<int> channels, out Dictionary<string, int> mappings)
        {
            channels = GetCheckedMultiChannelsOrdered();
            mappings = ReadMultiMappingsFromGrid();

            if (channels.Count < 2)
            {
                MessageBox.Show("Select at least 2 channels for Multi-Input.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtDeviceProsimRef.Text))
            {
                MessageBox.Show("ProSim DataRef is required for Multi-Input.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int expected = 1 << channels.Count;
            if (mappings.Count != expected)
            {
                MessageBox.Show("Mappings must include all combinations for the selected channels.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void SetHubAndPortChannel(int serial, int hubPort, int channel, List<PhidgetsHubInst> hubs)
        {
            var hub = hubs.FirstOrDefault(h => h.Serial == serial);
            if (hub != null) cboDeviceHub.Text = HubText(hub);

            nudDeviceHubPort.Value = ClampToRange(hubPort, nudDeviceHubPort.Minimum, nudDeviceHubPort.Maximum);

            cboDeviceChannel.Items.Clear();
            for (int i = 0; i <= 7; i++) cboDeviceChannel.Items.Add(i.ToString());
            cboDeviceChannel.Text = channel.ToString();
        }

        // Opens the empty "new instance" form for the active category (also the default view when a category is selected)
        private void BtnAddDevice_Click(object sender, EventArgs e)
        {
            var hubs = _hubs.GetAll();
            lstDevices.ClearSelected();
            ClearDeviceEditor();
            PopulateHubDropdown(cboDeviceHub, hubs);
            SetHubAndPortChannel(0, 0, 0, hubs);
            lblCardItemTitle.Text = "New " + _activeDeviceCategory;

            switch (_activeDeviceCategory)
            {
                case DcMotorCategory:
                    NewDcMotorEditor();
                    break;
                case LinkCategory:
                    NewLinkEditor();
                    break;
                case "Outputs":
                case "Gates":
                    ShowAdvancedPanel(pnlAdvancedProperties_Outputs);
                    break;
                case "Voltage Out":
                case "Voltage In":
                    ShowAdvancedPanel(pnlAdvancedProperties_Voltages);
                    break;
                case "Multi-Input":
                    ShowAdvancedPanel(pnlAdvancedProperties_MultiInputs);
                    RebuildMultiMappingsGrid();
                    break;
                case "Encoders":
                case "Buttons":
                    ShowAdvancedPanel(pnlAdvancedProperties_Simple);
                    break;
                default: // Inputs
                    ShowAdvancedPanel(pnlAdvancedProperties_Inputs);
                    break;
            }
        }

        private void BtnDeleteDevice_Click(object sender, EventArgs e)
        {
            if (!(lstDevices.SelectedItem is DeviceListItem item)) return;
            if (MessageBox.Show("Delete this device mapping?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            DeleteDeviceFromConfig(item);
            DisplayInfoLog("Device mapping deleted.");
            ApplyDeviceChanges(_activeDeviceCategory);
            LoadDeviceListForCategory(_activeDeviceCategory);
            BtnAddDevice_Click(this, EventArgs.Empty);
        }

        private void BtnSaveDevice_Click(object sender, EventArgs e)
        {
            if (_activeDeviceCategory == DcMotorCategory) { SaveDcMotor(); return; }
            if (_activeDeviceCategory == LinkCategory) { SaveLink(); return; }

            // Common fields (shared form)
            var hub = _hubs.GetAll().FirstOrDefault(h => HubText(h) == cboDeviceHub.Text);
            int serial  = hub?.Serial ?? 0;
            int hubPort = (int)nudDeviceHubPort.Value;
            int channel = int.TryParse(cboDeviceChannel.Text, out int ch) ? ch : 0;
            string prosimRef = txtDeviceProsimRef.Text.Trim();

            if (_activeDeviceCategory == "Multi-Input")
            {
                if (!TryGetMultiInputMappings(out _, out _))
                    return;
            }

            // Route to type-specific save
            SaveDeviceToConfig(serial, hubPort, channel, prosimRef);
            if (_lastSavedIndex < 0) return; // validation failed or nothing saved

            LoadDeviceListForCategory(_activeDeviceCategory);
            DisplayInfoLog("Device mapping saved.");
            ApplyDeviceChanges(_activeDeviceCategory);

            // Re-select the saved entry so the next save updates it instead of adding a new one
            for (int i = 0; i < lstDevices.Items.Count; i++)
            {
                if ((lstDevices.Items[i] as DeviceListItem)?.Index == _lastSavedIndex)
                {
                    lstDevices.SelectedIndex = i;
                    break;
                }
            }
        }

        private int _lastSavedIndex = -1;

        // Applies a saved / deleted mapping to the running devices (no restart) for categories that support it
        private void ApplyDeviceChanges(string category)
        {
            var cfg = _config.Load();
            if (cfg == null) return;
            bool inputsLoaded = _configsInsLoaded;
            Task.Run(() =>
            {
                try
                {
                    bool applied = _devices.ReloadCategory(category, cfg, inputsLoaded);
                    DisplayInfoLog(applied ? category + " applied." : category + " saved. Restart the app to apply it.");
                    if (applied && !IsDisposed) BeginInvoke(new Action(UpdateTelemetryPanel));
                }
                catch (Exception ex) { DisplayErrorLog("Error applying " + category + ": " + ex.Message); }
            });
        }

        // Returns the entry being edited (index into the config list) or appends a new one. Editing in place keeps
        // every property the form does not expose.
        private static T EditTarget<T>(List<T> list, int index, out int at, out bool isNew) where T : new()
        {
            if (index >= 0 && index < list.Count) { at = index; isNew = false; return list[index]; }
            var created = new T();
            list.Add(created);
            at = list.Count - 1;
            isNew = true;
            return created;
        }

        private void SaveDeviceToConfig(int serial, int hubPort, int channel, string prosimRef)
        {
            _lastSavedIndex = -1;
            try
            {
                var cfg = _config.Load() ?? new Config();
                int idx = (lstDevices.SelectedItem as DeviceListItem)?.Index ?? -1;
                int at;
                bool isNew;

                switch (_activeDeviceCategory)
                {
                    case "Outputs":
                    {
                        cfg.PhidgetsOutputInstances = cfg.PhidgetsOutputInstances ?? new List<PhidgetsOutputInst>();
                        var o = EditTarget(cfg.PhidgetsOutputInstances, idx, out at, out isNew);
                        o.Serial = serial; o.HubPort = hubPort; o.Channel = channel; o.ProsimDataRef = prosimRef;
                        o.ProsimDataRefOff = txtOutputRefOff.Text.NullIfEmpty();
                        o.UserVariable     = txtOutputUserVar.Text.NullIfEmpty();
                        o.Inverse          = chkOutputInverse.Checked ? (bool?)true : null;
                        o.DelayOn          = txtOutputDelayOn.Value > 0 ? (int?)((int)txtOutputDelayOn.Value) : null;
                        o.MaxTimeOn        = txtOutputMaxTimeOn.Value > 0 ? (int?)((int)txtOutputMaxTimeOn.Value) : null;
                        o.ValueOn  = (double)txtValueOn.Value;
                        o.ValueOff = (double)txtValueOff.Value;
                        o.ValueDim = (double)txtValueDim.Value;
                        break;
                    }
                    case "Inputs":
                    {
                        cfg.PhidgetsInputInstances = cfg.PhidgetsInputInstances ?? new List<PhidgetsInputInst>();
                        var i = EditTarget(cfg.PhidgetsInputInstances, idx, out at, out isNew);
                        i.Serial = serial; i.HubPort = hubPort; i.Channel = channel; i.ProsimDataRef = prosimRef;
                        i.InputValue = (int)txtInputValue.Value;
                        i.OffInputValue = (int)txtOffInputValue.Value;
                        i.ProsimDataRef2 = txtInputRef2.Text.NullIfEmpty();
                        i.ProsimDataRef3 = txtInputRef3.Text.NullIfEmpty();
                        i.UserVariable = txtInputUserVar.Text.NullIfEmpty();
                        break;
                    }
                    case "Encoders":
                    {
                        cfg.PhidgetsEncoderInstances = cfg.PhidgetsEncoderInstances ?? new List<PhidgetsEncoderInst>();
                        var enc = EditTarget(cfg.PhidgetsEncoderInstances, idx, out at, out isNew);
                        enc.Serial = serial; enc.HubPort = hubPort; enc.Channel = channel; enc.ProsimDataRef = prosimRef;
                        enc.ScaleFactor = (double)txtEncoderScale.Value;
                        break;
                    }
                    case "Gates":
                    {
                        cfg.PhidgetsGateInstances = cfg.PhidgetsGateInstances ?? new List<PhidgetsGateInst>();
                        var g = EditTarget(cfg.PhidgetsGateInstances, idx, out at, out isNew);
                        g.Serial = serial; g.HubPort = hubPort; g.Channel = channel; g.ProsimDataRef = prosimRef;
                        g.ProsimDataRefOff = txtOutputRefOff.Text.NullIfEmpty();
                        g.ProsimDataRef2   = txtOutputRef2.Text.NullIfEmpty();
                        g.Operator         = g.ProsimDataRef2 != null && cboGateOperator.Text == "AND" ? "AND" : "OR";
                        g.Inverse          = chkOutputInverse.Checked;
                        g.DelayOn          = txtOutputDelayOn.Value > 0 ? (int?)((int)txtOutputDelayOn.Value) : null;
                        g.MaxTimeOn        = txtOutputMaxTimeOn.Value > 0 ? (int?)((int)txtOutputMaxTimeOn.Value) : null;
                        break;
                    }
                    case "Buttons":
                    {
                        cfg.PhidgetsButtonInstances = cfg.PhidgetsButtonInstances ?? new List<PhidgetsButtonInst>();
                        var b = EditTarget(cfg.PhidgetsButtonInstances, idx, out at, out isNew);
                        b.Serial = serial; b.HubPort = hubPort; b.Channel = channel; b.ProsimDataRef = prosimRef;
                        b.Name = txtButtonName.Text.NullIfEmpty();
                        if (isNew) b.InputValue = 1;
                        break;
                    }
                    case "Multi-Input":
                    {
                        if (!TryGetMultiInputMappings(out List<int> channels, out Dictionary<string, int> mappings))
                            return;
                        cfg.PhidgetsMultiInputInstances = cfg.PhidgetsMultiInputInstances ?? new List<PhidgetsMultiInputInst>();
                        var m = EditTarget(cfg.PhidgetsMultiInputInstances, idx, out at, out isNew);
                        m.Serial = serial; m.HubPort = hubPort; m.ProsimDataRef = prosimRef;
                        m.Channels = channels;
                        m.Mappings = mappings;
                        break;
                    }
                    case "Voltage Out":
                    {
                        cfg.PhidgetsVoltageOutputInstances = cfg.PhidgetsVoltageOutputInstances ?? new List<PhidgetsVoltageOutputInst>();
                        var v = EditTarget(cfg.PhidgetsVoltageOutputInstances, idx, out at, out isNew);
                        v.Serial = serial; v.HubPort = hubPort; v.ProsimDataRef = prosimRef;
                        ReadVoltageOutputFields(v);
                        break;
                    }
                    case "Voltage In":
                    {
                        if (!TryParseVoltageInputFields(out var vf)) return;
                        cfg.PhidgetsVoltageInputInstances = cfg.PhidgetsVoltageInputInstances ?? new List<PhidgetsVoltageInputInst>();
                        var vi = EditTarget(cfg.PhidgetsVoltageInputInstances, idx, out at, out isNew);
                        vi.Serial = serial; vi.HubPort = hubPort; vi.Channel = channel; vi.ProsimDataRef = prosimRef;
                        ApplyVoltageInputFields(vi, vf);
                        break;
                    }
                    default:
                        return;
                }

                _lastSavedIndex = at;
                WriteConfig(cfg);
            }
            catch (Exception ex) { DisplayErrorLog("Save error: " + ex.Message); }
        }

        private void DeleteDeviceFromConfig(DeviceListItem item)
        {
            try
            {
                if (item.Tag is DcMotorRef dcRef) { DeleteDcMotor(dcRef); return; }

                var cfg = _config.Load();
                if (cfg == null) return;
                int i = item.Index;

                void Remove<T>(List<T> list) { if (list != null && i >= 0 && i < list.Count) list.RemoveAt(i); }

                switch (_activeDeviceCategory)
                {
                    case "Outputs":     Remove(cfg.PhidgetsOutputInstances); break;
                    case "Inputs":      Remove(cfg.PhidgetsInputInstances); break;
                    case "Encoders":    Remove(cfg.PhidgetsEncoderInstances); break;
                    case "Gates":       Remove(cfg.PhidgetsGateInstances); break;
                    case "Buttons":     Remove(cfg.PhidgetsButtonInstances); break;
                    case LinkCategory:  Remove(cfg.PhidgetsLinkInstances); break;
                    case "Multi-Input": Remove(cfg.PhidgetsMultiInputInstances); break;
                    case "Voltage Out": Remove(cfg.PhidgetsVoltageOutputInstances); break;
                    case "Voltage In":  Remove(cfg.PhidgetsVoltageInputInstances); break;
                }
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
            return _config.CheckAndMigrate(stubs =>
            {
                var workingHubs = _hubWorkflow.CloneConfiguredHubs(stubs);

                if (workingHubs.Any(h => h.Serial <= 0))
                {
                    try
                    {
                        var discovered = _hubWorkflow.ScanAsync().GetAwaiter().GetResult();
                        foreach (var hub in discovered)
                        {
                            var match = workingHubs.FirstOrDefault(h => h.Serial <= 0 &&
                                string.Equals(h.Name, hub.ServerName, StringComparison.OrdinalIgnoreCase));
                            if (match != null)
                            {
                                match.Serial = hub.Serial;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DisplayErrorLog("Error scanning hubs during migration: " + ex.Message);
                    }
                }

                return workingHubs;
            });
        }
    }

    internal class DeviceListItem
    {
        public string Label { get; set; }
        public object Tag   { get; set; }
        public int Index    { get; set; } = -1;
        public override string ToString() => Label ?? "";
    }

    // ── String extension helper ───────────────────────────────────────────────────
    internal static class StringExtensions
    {
        public static string NullIfEmpty(this string s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
