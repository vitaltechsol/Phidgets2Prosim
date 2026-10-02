using System.Drawing;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void lblExRefText(Label lbl, string txt, int y)
        {
            lbl.Text = txt;
            lbl.Location = new System.Drawing.Point(0, y + 3);
            lbl.Size = new System.Drawing.Size(130, 20);
            lbl.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            lbl.ForeColor = System.Drawing.Color.FromArgb(51, 65, 85);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pnlSidebar = new System.Windows.Forms.Panel();
            this.lblLogo = new System.Windows.Forms.Label();
            this.lblLogoSub = new System.Windows.Forms.Label();
            this.btnNavDashboard = new System.Windows.Forms.Button();
            this.btnNavHubs = new System.Windows.Forms.Button();
            this.btnNavDevices = new System.Windows.Forms.Button();
            this.btnNavSpecial = new System.Windows.Forms.Button();
            this.btnNavSettings = new System.Windows.Forms.Button();
            this.lblSidebarVersion = new System.Windows.Forms.Label();
            
            this.pnlMainContent = new System.Windows.Forms.Panel();
            this.pnlPageDashboard = new System.Windows.Forms.Panel();
            this.pnlPageHubs = new System.Windows.Forms.Panel();
            this.pnlPageDevices = new System.Windows.Forms.Panel();
            this.pnlPageSpecial = new System.Windows.Forms.Panel();
            this.pnlPageSettings = new System.Windows.Forms.Panel();

            // ── Sidebar Layout ──────────────────────────────────────────────
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(24, 28, 36);
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSidebar.Width = 200;
            this.pnlSidebar.Padding = new System.Windows.Forms.Padding(0, 15, 0, 10);

            this.lblLogo.ForeColor = System.Drawing.Color.White;
            this.lblLogo.Font = new System.Drawing.Font("Segoe UI", 13.5F, System.Drawing.FontStyle.Bold);
            this.lblLogo.Location = new System.Drawing.Point(12, 15);
            this.lblLogo.Size = new System.Drawing.Size(176, 25);
            this.lblLogo.Text = "Phidgets2Prosim";
            this.lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.lblLogoSub.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184);
            this.lblLogoSub.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblLogoSub.Location = new System.Drawing.Point(12, 40);
            this.lblLogoSub.Size = new System.Drawing.Size(176, 18);
            this.lblLogoSub.Text = "HARDWARE HUB";
            this.lblLogoSub.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // Nav Buttons
            int btnY = 85;
            this.btnNavDashboard = CreateSidebarButton("Dashboard", btnY); btnY += 45;
            this.btnNavHubs = CreateSidebarButton("Scan & Hubs", btnY); btnY += 45;
            this.btnNavDevices = CreateSidebarButton("Mappings Studio", btnY); btnY += 45;
            this.btnNavSpecial = CreateSidebarButton("Special Drives", btnY); btnY += 45;
            this.btnNavSettings = CreateSidebarButton("Global Settings", btnY);

            this.lblSidebarVersion.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblSidebarVersion.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblSidebarVersion.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblSidebarVersion.Height = 24;
            this.lblSidebarVersion.Text = "v1.4.0 • Modern UI";
            this.lblSidebarVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.pnlSidebar.Controls.Add(this.lblLogo);
            this.pnlSidebar.Controls.Add(this.lblLogoSub);
            this.pnlSidebar.Controls.Add(this.btnNavDashboard);
            this.pnlSidebar.Controls.Add(this.btnNavHubs);
            this.pnlSidebar.Controls.Add(this.btnNavDevices);
            this.pnlSidebar.Controls.Add(this.btnNavSpecial);
            this.pnlSidebar.Controls.Add(this.btnNavSettings);
            this.pnlSidebar.Controls.Add(this.lblSidebarVersion);

            // ── Main Content Dock ───────────────────────────────────────────
            this.pnlMainContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMainContent.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.pnlMainContent.Controls.Add(this.pnlPageDashboard);
            this.pnlMainContent.Controls.Add(this.pnlPageHubs);
            this.pnlMainContent.Controls.Add(this.pnlPageDevices);
            this.pnlMainContent.Controls.Add(this.pnlPageSpecial);
            this.pnlMainContent.Controls.Add(this.pnlPageSettings);

            // Initialize Pages
            InitializeDashboardPage_Custom();
            InitializeHubsPage_Custom();
            InitializeDevicesPage_Custom();
            InitializeSpecialPage_Custom();
            InitializeSettingsPage_Custom();

            // ── Form Main setup ─────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1040, 760);
            this.MinimumSize = new System.Drawing.Size(1000, 700);
            this.Controls.Add(this.pnlMainContent);
            this.Controls.Add(this.pnlSidebar);
            this.Name = "Form1";
            this.Text = "Phidgets2Prosim - Interface Manager";
            this.Load += new System.EventHandler(this.Form1_Load_1);

            this.pnlSidebar.ResumeLayout(false);
            this.pnlMainContent.ResumeLayout(false);
            this.pnlPageDashboard.ResumeLayout(false);
            this.pnlPageHubs.ResumeLayout(false);
            this.pnlPageDevices.ResumeLayout(false);
            this.pnlPageSpecial.ResumeLayout(false);
            this.pnlPageSettings.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private Button CreateSidebarButton(string text, int top)
        {
            var btn = new Button();
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(39, 45, 56);
            btn.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(51, 65, 85);
            btn.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184);
            btn.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btn.Location = new System.Drawing.Point(0, top);
            btn.Size = new System.Drawing.Size(200, 40);
            btn.Text = "  " + text;
            btn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            btn.UseVisualStyleBackColor = true;
            return btn;
        }

        private void InitializeDashboardPage_Custom()
        {
            this.pnlPageDashboard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPageDashboard.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.pnlPageDashboard.Padding = new System.Windows.Forms.Padding(20, 16, 20, 20);
            this.pnlPageDashboard.AutoScroll = true;
            this.pnlPageDashboard.AutoScrollMargin = new System.Drawing.Size(20, 20);

            var dashboardLayout = new TableLayoutPanel();
            dashboardLayout.Dock = DockStyle.Fill;
            dashboardLayout.BackColor = System.Drawing.Color.Transparent;
            dashboardLayout.ColumnCount = 1;
            dashboardLayout.RowCount = 3;
            dashboardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // ── ROW 1 : Connection Bar ────────────────────────────────────────────
            // Full-width dark strip that holds IP box + status + connect/disconnect
            this.cardProsimConn = new Panel();
            this.cardProsimConn.BackColor = System.Drawing.Color.FromArgb(17, 24, 39);
            this.cardProsimConn.Dock = DockStyle.Fill;
            this.cardProsimConn.Margin = new Padding(0, 0, 0, 12);
            this.cardProsimConn.Height = 58;
            this.cardProsimConn.Padding = new System.Windows.Forms.Padding(16, 0, 16, 0);

            // "IP:" label
            this.lblProsimIPLabelOnDash = new Label();
            this.lblProsimIPLabelOnDash.Text = "ProSim IP:";
            this.lblProsimIPLabelOnDash.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblProsimIPLabelOnDash.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184);
            this.lblProsimIPLabelOnDash.Location = new System.Drawing.Point(16, 20);
            this.lblProsimIPLabelOnDash.Size = new System.Drawing.Size(65, 18);
            this.lblProsimIPLabelOnDash.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // IP text box
            this.txtProsimIP = new TextBox();
            this.txtProsimIP.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtProsimIP.Text = "127.0.0.1";
            this.txtProsimIP.Location = new System.Drawing.Point(82, 17);
            this.txtProsimIP.Size = new System.Drawing.Size(145, 24);
            this.txtProsimIP.BorderStyle = BorderStyle.FixedSingle;

            // Save IP button (small, low-profile)
            this.btnSaveProsimIP = new Button();
            this.btnSaveProsimIP.Text = "Save";
            this.btnSaveProsimIP.FlatStyle = FlatStyle.Flat;
            this.btnSaveProsimIP.FlatAppearance.BorderSize = 1;
            this.btnSaveProsimIP.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(71, 85, 105);
            this.btnSaveProsimIP.BackColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.btnSaveProsimIP.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184);
            this.btnSaveProsimIP.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnSaveProsimIP.Location = new System.Drawing.Point(232, 17);
            this.btnSaveProsimIP.Size = new System.Drawing.Size(52, 24);

            // Status pill (grows to fill space between save and the action buttons)
            this.connectionStatusLabel = new Label();
            this.connectionStatusLabel.Text = "● DISCONNECTED";
            this.connectionStatusLabel.ForeColor = System.Drawing.Color.FromArgb(239, 68, 68);
            this.connectionStatusLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.connectionStatusLabel.Location = new System.Drawing.Point(298, 18);
            this.connectionStatusLabel.Size = new System.Drawing.Size(220, 22);
            this.connectionStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // anchor to right side so it expands with the window
            this.connectionStatusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            // Connect button — right-aligned, green
            this.btnConnectProsim = new Button();
            this.btnConnectProsim.Text = "Connect";
            this.btnConnectProsim.FlatStyle = FlatStyle.Flat;
            this.btnConnectProsim.FlatAppearance.BorderSize = 0;
            this.btnConnectProsim.BackColor = System.Drawing.Color.FromArgb(16, 185, 129);
            this.btnConnectProsim.ForeColor = System.Drawing.Color.White;
            this.btnConnectProsim.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnConnectProsim.Size = new System.Drawing.Size(95, 30);
            this.btnConnectProsim.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnConnectProsim.Location = new System.Drawing.Point(this.cardProsimConn.Width - 210, 14);

            // Disconnect button — right of connect, red
            this.btnDisconnectProsim = new Button();
            this.btnDisconnectProsim.Text = "Disconnect";
            this.btnDisconnectProsim.FlatStyle = FlatStyle.Flat;
            this.btnDisconnectProsim.FlatAppearance.BorderSize = 0;
            this.btnDisconnectProsim.BackColor = System.Drawing.Color.FromArgb(239, 68, 68);
            this.btnDisconnectProsim.ForeColor = System.Drawing.Color.White;
            this.btnDisconnectProsim.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnDisconnectProsim.Size = new System.Drawing.Size(100, 30);
            this.btnDisconnectProsim.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnDisconnectProsim.Location = new System.Drawing.Point(this.cardProsimConn.Width - 108, 14);
            this.btnDisconnectProsim.Enabled = false;

            // Keep buttons stuck to the right edge when the window is resized
            this.cardProsimConn.Resize += (s, e2) =>
            {
                this.btnConnectProsim.Location    = new System.Drawing.Point(this.cardProsimConn.Width - 210, 14);
                this.btnDisconnectProsim.Location = new System.Drawing.Point(this.cardProsimConn.Width - 108, 14);
                this.connectionStatusLabel.Size   = new System.Drawing.Size(this.cardProsimConn.Width - 530, 22);
            };

            // unused but kept for designer field compatibility
            this.lblProsimTitle = new Label();
            this.lblProsimTitle.Visible = false;

            this.cardProsimConn.Controls.Add(this.lblProsimTitle);
            this.cardProsimConn.Controls.Add(this.lblProsimIPLabelOnDash);
            this.cardProsimConn.Controls.Add(this.txtProsimIP);
            this.cardProsimConn.Controls.Add(this.btnSaveProsimIP);
            this.cardProsimConn.Controls.Add(this.connectionStatusLabel);
            this.cardProsimConn.Controls.Add(this.btnConnectProsim);
            this.cardProsimConn.Controls.Add(this.btnDisconnectProsim);
            dashboardLayout.Controls.Add(this.cardProsimConn, 0, 0);

            // ── ROW 2 : Stats / Telemetry ─────────────────────────────────────────
            // A white card below the bar that contains a wrap-panel of stat chips.
            // The card anchors left+right so chips fill the available width.
            this.cardTelemetry = new Panel();
            this.cardTelemetry.BackColor = System.Drawing.Color.White;
            this.cardTelemetry.Dock = DockStyle.Fill;
            this.cardTelemetry.Margin = new Padding(0, 0, 0, 12);
            StyleCard_Custom(this.cardTelemetry);

            this.lblTelemetryTitle = new Label();
            this.lblTelemetryTitle.Text = "Active Devices";
            this.lblTelemetryTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblTelemetryTitle.ForeColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.lblTelemetryTitle.Location = new System.Drawing.Point(14, 10);
            this.lblTelemetryTitle.Size = new System.Drawing.Size(160, 16);
            this.lblTelemetryTitle.AutoSize = false;

            // Wrap panel for stat chips — anchors all four edges so it grows with the card
            this.flowTelemetry = new FlowLayoutPanel();
            this.flowTelemetry.Location = new System.Drawing.Point(14, 32);
            this.flowTelemetry.Size = new System.Drawing.Size(this.cardTelemetry.Width - 28, 52);
            this.flowTelemetry.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.flowTelemetry.FlowDirection = FlowDirection.LeftToRight;
            this.flowTelemetry.WrapContents = false;
            this.flowTelemetry.AutoSize = false;

            // Resize the flow panel to match the card width
            this.cardTelemetry.Resize += (s, e2) =>
            {
                this.flowTelemetry.Size = new System.Drawing.Size(this.cardTelemetry.Width - 28, this.flowTelemetry.Height);
            };

            this.cardTelemetry.Controls.Add(this.lblTelemetryTitle);
            this.cardTelemetry.Controls.Add(this.flowTelemetry);
            dashboardLayout.Controls.Add(this.cardTelemetry, 0, 1);

            // ── ROW 3 : Console Log ───────────────────────────────────────────────
            this.cardLogs = new Panel();
            this.cardLogs.BackColor = System.Drawing.Color.White;
            this.cardLogs.Dock = DockStyle.Fill;
            this.cardLogs.Margin = new Padding(0);
            this.cardLogs.MinimumSize = new System.Drawing.Size(0, 240);
            StyleCard_Custom(this.cardLogs);

            this.lblLogsTitle = new Label();
            this.lblLogsTitle.Text = "Console Logs & Events";
            this.lblLogsTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblLogsTitle.Location = new System.Drawing.Point(15, 13);
            this.lblLogsTitle.Size = new System.Drawing.Size(250, 20);
            this.lblLogsTitle.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);

            this.btnLogClear = new Button();
            this.btnLogClear.Text = "Clear";
            this.btnLogClear.FlatStyle = FlatStyle.Flat;
            this.btnLogClear.FlatAppearance.BorderSize = 0;
            this.btnLogClear.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.btnLogClear.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnLogClear.Size = new System.Drawing.Size(75, 26);
            this.btnLogClear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnLogClear.Location = new System.Drawing.Point(this.cardLogs.Width - 192, 10);

            this.btnLogOk = new Button();
            this.btnLogOk.Text = "Acknowledge";
            this.btnLogOk.FlatStyle = FlatStyle.Flat;
            this.btnLogOk.FlatAppearance.BorderSize = 0;
            this.btnLogOk.BackColor = System.Drawing.Color.FromArgb(219, 234, 254);
            this.btnLogOk.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnLogOk.Size = new System.Drawing.Size(105, 26);
            this.btnLogOk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnLogOk.Location = new System.Drawing.Point(this.cardLogs.Width - 110, 10);

            this.cardLogs.Resize += (s, e2) =>
            {
                this.btnLogClear.Location = new System.Drawing.Point(this.cardLogs.Width - 192, 10);
                this.btnLogOk.Location    = new System.Drawing.Point(this.cardLogs.Width - 110, 10);
            };

            this.txtLog = new TextBox();
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = ScrollBars.Vertical;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.txtLog.ForeColor = System.Drawing.Color.FromArgb(14, 165, 233);
            this.txtLog.BorderStyle = BorderStyle.None;
            this.txtLog.Location = new System.Drawing.Point(15, 46);
            this.txtLog.Size = new System.Drawing.Size(this.cardLogs.Width - 30, this.cardLogs.Height - 60);
            this.txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            this.cardLogs.Controls.Add(this.lblLogsTitle);
            this.cardLogs.Controls.Add(this.btnLogClear);
            this.cardLogs.Controls.Add(this.btnLogOk);
            this.cardLogs.Controls.Add(this.txtLog);
            dashboardLayout.Controls.Add(this.cardLogs, 0, 2);
            this.pnlPageDashboard.Controls.Add(dashboardLayout);
        }

        private void InitializeHubsPage_Custom()
        {
            this.pnlPageHubs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPageHubs.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.pnlPageHubs.Padding = new System.Windows.Forms.Padding(24);
            this.pnlPageHubs.AutoScroll = true;
            this.pnlPageHubs.AutoScrollMargin = new System.Drawing.Size(20, 20);

            var hubsLayout = new TableLayoutPanel();
            hubsLayout.Dock = DockStyle.Fill;
            hubsLayout.BackColor = System.Drawing.Color.Transparent;
            hubsLayout.ColumnCount = 1;
            hubsLayout.RowCount = 2;
            hubsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            hubsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            hubsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label lblHubHeader = new Label();
            lblHubHeader.Text = "Phidgets Network Hubs";
            lblHubHeader.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            lblHubHeader.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            lblHubHeader.Dock = DockStyle.Top;
            lblHubHeader.Margin = new Padding(0, 0, 0, 16);
            lblHubHeader.Size = new System.Drawing.Size(400, 30);
            hubsLayout.Controls.Add(lblHubHeader, 0, 0);

            var hubsContentLayout = new TableLayoutPanel();
            hubsContentLayout.Dock = DockStyle.Fill;
            hubsContentLayout.BackColor = System.Drawing.Color.Transparent;
            hubsContentLayout.ColumnCount = 2;
            hubsContentLayout.RowCount = 1;
            hubsContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360F));
            hubsContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            hubsContentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            hubsLayout.Controls.Add(hubsContentLayout, 0, 1);

            // Left side: Configured Hubs Card
            Panel cardConfigured = new Panel();
            cardConfigured.BackColor = System.Drawing.Color.White;
            cardConfigured.Dock = DockStyle.Fill;
            cardConfigured.Margin = new Padding(0, 0, 20, 0);
            StyleCard_Custom(cardConfigured);

            var configuredLayout = new TableLayoutPanel();
            configuredLayout.Dock = DockStyle.Fill;
            configuredLayout.BackColor = System.Drawing.Color.Transparent;
            configuredLayout.ColumnCount = 1;
            configuredLayout.RowCount = 3;
            configuredLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            configuredLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            configuredLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            configuredLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
            cardConfigured.Controls.Add(configuredLayout);

            Panel pnlConfiguredHeader = new Panel();
            pnlConfiguredHeader.Dock = DockStyle.Fill;

            Label lblConfiguredTitle = new Label();
            lblConfiguredTitle.Text = "Configured Hubs";
            lblConfiguredTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            lblConfiguredTitle.Location = new System.Drawing.Point(15, 10);
            lblConfiguredTitle.Size = new System.Drawing.Size(180, 20);
            pnlConfiguredHeader.Controls.Add(lblConfiguredTitle);
            configuredLayout.Controls.Add(pnlConfiguredHeader, 0, 0);

            Panel pnlHubEditor = new Panel();
            pnlHubEditor.Dock = DockStyle.Fill;

            this.dgvConfiguredHubs = new DataGridView();
            this.dgvConfiguredHubs.BorderStyle = BorderStyle.None;
            this.dgvConfiguredHubs.BackgroundColor = System.Drawing.Color.White;
            this.dgvConfiguredHubs.AllowUserToAddRows = false;
            this.dgvConfiguredHubs.RowHeadersVisible = false;
            this.dgvConfiguredHubs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvConfiguredHubs.Dock = DockStyle.Fill;
            this.dgvConfiguredHubs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            configuredLayout.Controls.Add(this.dgvConfiguredHubs, 0, 1);

            Label lblHubName = new Label(); lblHubName.Text = "Name:"; lblHubName.Location = new System.Drawing.Point(15, 12); lblHubName.Size = new System.Drawing.Size(60, 20);
            lblHubName.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.txtHubName = new TextBox(); this.txtHubName.Location = new System.Drawing.Point(85, 10); this.txtHubName.Size = new System.Drawing.Size(240, 23);
            this.txtHubName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblHubSerial = new Label(); lblHubSerial.Text = "Serial:"; lblHubSerial.Location = new System.Drawing.Point(15, 47); lblHubSerial.Size = new System.Drawing.Size(60, 20);
            lblHubSerial.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.txtHubSerial = new TextBox(); this.txtHubSerial.Location = new System.Drawing.Point(85, 45); this.txtHubSerial.Size = new System.Drawing.Size(240, 23);
            this.txtHubSerial.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.chkHubEnabled = new CheckBox(); this.chkHubEnabled.Text = "Enabled"; this.chkHubEnabled.Location = new System.Drawing.Point(85, 78); this.chkHubEnabled.Size = new System.Drawing.Size(100, 20);
            this.chkHubEnabled.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            this.btnAddHub = new Button();
            this.btnAddHub.Text = "Add Hub";
            this.btnAddHub.FlatStyle = FlatStyle.Flat;
            this.btnAddHub.FlatAppearance.BorderSize = 0;
            this.btnAddHub.BackColor = System.Drawing.Color.FromArgb(59, 130, 246);
            this.btnAddHub.ForeColor = System.Drawing.Color.White;
            this.btnAddHub.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnAddHub.Location = new System.Drawing.Point(15, 114);
            this.btnAddHub.Size = new System.Drawing.Size(150, 32);
            this.btnAddHub.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            this.btnDeleteHub = new Button();
            this.btnDeleteHub.Text = "Delete Active";
            this.btnDeleteHub.FlatStyle = FlatStyle.Flat;
            this.btnDeleteHub.FlatAppearance.BorderSize = 0;
            this.btnDeleteHub.BackColor = System.Drawing.Color.FromArgb(239, 68, 68);
            this.btnDeleteHub.ForeColor = System.Drawing.Color.White;
            this.btnDeleteHub.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnDeleteHub.Location = new System.Drawing.Point(175, 114);
            this.btnDeleteHub.Size = new System.Drawing.Size(150, 32);
            this.btnDeleteHub.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            pnlHubEditor.Controls.Add(lblHubName); pnlHubEditor.Controls.Add(this.txtHubName);
            pnlHubEditor.Controls.Add(lblHubSerial); pnlHubEditor.Controls.Add(this.txtHubSerial);
            pnlHubEditor.Controls.Add(this.chkHubEnabled);
            pnlHubEditor.Controls.Add(this.btnAddHub);
            pnlHubEditor.Controls.Add(this.btnDeleteHub);
            configuredLayout.Controls.Add(pnlHubEditor, 0, 2);
            hubsContentLayout.Controls.Add(cardConfigured, 0, 0);

            // Right side: Active Network Scans Card
            Panel cardDiscovery = new Panel();
            cardDiscovery.BackColor = System.Drawing.Color.White;
            cardDiscovery.Dock = DockStyle.Fill;
            cardDiscovery.Margin = new Padding(0);
            StyleCard_Custom(cardDiscovery);

            var discoveryLayout = new TableLayoutPanel();
            discoveryLayout.Dock = DockStyle.Fill;
            discoveryLayout.BackColor = System.Drawing.Color.Transparent;
            discoveryLayout.ColumnCount = 1;
            discoveryLayout.RowCount = 2;
            discoveryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            discoveryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            discoveryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            cardDiscovery.Controls.Add(discoveryLayout);

            Panel pnlDiscoveryHeader = new Panel();
            pnlDiscoveryHeader.Dock = DockStyle.Fill;

            Label lblDiscoveredTitle = new Label();
            lblDiscoveredTitle.Text = "Autodetect Hubs (In-Network)";
            lblDiscoveredTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            lblDiscoveredTitle.Location = new System.Drawing.Point(15, 10);
            lblDiscoveredTitle.Size = new System.Drawing.Size(220, 20);
            pnlDiscoveryHeader.Controls.Add(lblDiscoveredTitle);

            this.btnScan = new Button();
            this.btnScan.Text = "Scan Network";
            this.btnScan.FlatStyle = FlatStyle.Flat;
            this.btnScan.FlatAppearance.BorderSize = 0;
            this.btnScan.BackColor = System.Drawing.Color.FromArgb(16, 185, 129);
            this.btnScan.ForeColor = System.Drawing.Color.White;
            this.btnScan.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnScan.Location = new System.Drawing.Point(295, 6);
            this.btnScan.Size = new System.Drawing.Size(120, 28);
            this.btnScan.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            pnlDiscoveryHeader.Controls.Add(this.btnScan);
            discoveryLayout.Controls.Add(pnlDiscoveryHeader, 0, 0);

            this.lvDiscoveredHubs = new ListView();
            this.lvDiscoveredHubs.View = View.Details;
            this.lvDiscoveredHubs.FullRowSelect = true;
            this.lvDiscoveredHubs.GridLines = true;
            this.lvDiscoveredHubs.Columns.Add("Serial Number", 90);
            this.lvDiscoveredHubs.Columns.Add("Device Model", 110);
            this.lvDiscoveredHubs.Columns.Add("Network Host", 110);
            this.lvDiscoveredHubs.BorderStyle = BorderStyle.None;
            this.lvDiscoveredHubs.Dock = DockStyle.Fill;
            discoveryLayout.Controls.Add(this.lvDiscoveredHubs, 0, 1);

            hubsContentLayout.Controls.Add(cardDiscovery, 1, 0);
            this.pnlPageHubs.Controls.Add(hubsLayout);
        }

        private void InitializeDevicesPage_Custom()
        {
            this.pnlPageDevices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPageDevices.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.pnlPageDevices.Padding = new System.Windows.Forms.Padding(20);
            this.pnlPageDevices.AutoScroll = true;
            this.pnlPageDevices.AutoScrollMargin = new System.Drawing.Size(20, 20);

            var devicesLayout = new TableLayoutPanel();
            devicesLayout.Dock = DockStyle.Top;
            devicesLayout.BackColor = System.Drawing.Color.Transparent;
            devicesLayout.Height = 744;
            devicesLayout.MinimumSize = new System.Drawing.Size(0, 744);
            devicesLayout.ColumnCount = 2;
            devicesLayout.RowCount = 2;
            devicesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280F));
            devicesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            devicesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
            devicesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var categoriesCard = new Panel();
            categoriesCard.BackColor = System.Drawing.Color.White;
            categoriesCard.Dock = DockStyle.Fill;
            categoriesCard.Margin = new Padding(0, 0, 0, 16);
            StyleCard_Custom(categoriesCard);

            this.flowCategories = new FlowLayoutPanel();
            this.flowCategories.Dock = DockStyle.Fill;
            this.flowCategories.Margin = new Padding(0);
            this.flowCategories.Padding = new Padding(0);
            this.flowCategories.FlowDirection = FlowDirection.LeftToRight;
            this.flowCategories.WrapContents = true;

            // Category filter buttons
            string[] categories = { "Outputs", "Gates", "Inputs", "Multi-Input", "Encoders", "Voltage Out", "Voltage In", "Buttons" };
            foreach (var cat in categories)
            {
                var catBtn = new Button();
                catBtn.Text = cat;
                catBtn.Tag  = cat;
                catBtn.FlatStyle = FlatStyle.Flat;
                catBtn.FlatAppearance.BorderSize = 0;
                catBtn.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
                catBtn.ForeColor = System.Drawing.Color.FromArgb(51, 65, 85);
                catBtn.Font    = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
                catBtn.Size    = new System.Drawing.Size(170, 34);
                catBtn.Margin  = new System.Windows.Forms.Padding(4);
                this.flowCategories.Controls.Add(catBtn);
            }

            categoriesCard.Controls.Add(this.flowCategories);
            devicesLayout.Controls.Add(categoriesCard, 0, 0);
            devicesLayout.SetColumnSpan(categoriesCard, 2);

            // Left Col: Selection of current values
            Panel cardDeviceNav = new Panel();
            cardDeviceNav.BackColor = System.Drawing.Color.White;
            cardDeviceNav.Dock = DockStyle.Fill;
            cardDeviceNav.Margin = new Padding(0, 0, 20, 0);
            StyleCard_Custom(cardDeviceNav);

            var deviceNavLayout = new TableLayoutPanel();
            deviceNavLayout.Dock = DockStyle.Fill;
            deviceNavLayout.BackColor = System.Drawing.Color.Transparent;
            deviceNavLayout.ColumnCount = 1;
            deviceNavLayout.RowCount = 3;
            deviceNavLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            deviceNavLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            deviceNavLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            deviceNavLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            cardDeviceNav.Controls.Add(deviceNavLayout);

            this.txtDeviceSearch = new TextBox();
            this.txtDeviceSearch.Dock = DockStyle.Fill;
            this.txtDeviceSearch.Margin = new Padding(0, 0, 0, 10);
            deviceNavLayout.Controls.Add(this.txtDeviceSearch, 0, 0);

            this.lstDevices = new ListBox();
            this.lstDevices.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lstDevices.Dock = DockStyle.Fill;
            this.lstDevices.Margin = new Padding(0, 0, 0, 10);
            deviceNavLayout.Controls.Add(this.lstDevices, 0, 1);

            var deviceButtonsLayout = new TableLayoutPanel();
            deviceButtonsLayout.Dock = DockStyle.Fill;
            deviceButtonsLayout.Padding = new Padding(0, 6, 0, 0);
            deviceButtonsLayout.ColumnCount = 2;
            deviceButtonsLayout.RowCount = 1;
            deviceButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            deviceButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            deviceButtonsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.btnAddDevice = new Button();
            this.btnAddDevice.Text = "+ Add Mapping";
            this.btnAddDevice.FlatStyle = FlatStyle.Flat;
            this.btnAddDevice.FlatAppearance.BorderSize = 0;
            this.btnAddDevice.BackColor = System.Drawing.Color.FromArgb(59, 130, 246);
            this.btnAddDevice.ForeColor = System.Drawing.Color.White;
            this.btnAddDevice.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnAddDevice.Dock = DockStyle.Fill;
            this.btnAddDevice.Margin = new Padding(0, 0, 6, 0);
            deviceButtonsLayout.Controls.Add(this.btnAddDevice, 0, 0);

            this.btnDeleteDevice = new Button();
            this.btnDeleteDevice.Text = "Delete";
            this.btnDeleteDevice.FlatStyle = FlatStyle.Flat;
            this.btnDeleteDevice.FlatAppearance.BorderSize = 0;
            this.btnDeleteDevice.BackColor = System.Drawing.Color.FromArgb(239, 68, 68);
            this.btnDeleteDevice.ForeColor = System.Drawing.Color.White;
            this.btnDeleteDevice.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnDeleteDevice.Dock = DockStyle.Fill;
            this.btnDeleteDevice.Margin = new Padding(6, 0, 0, 0);
            deviceButtonsLayout.Controls.Add(this.btnDeleteDevice, 1, 0);

            deviceNavLayout.Controls.Add(deviceButtonsLayout, 0, 2);

            devicesLayout.Controls.Add(cardDeviceNav, 0, 1);

            // Right Col: Detail fields cards editor
            this.pnlDeviceEditorCard = new Panel();
            this.pnlDeviceEditorCard.BackColor = System.Drawing.Color.White;
            this.pnlDeviceEditorCard.Dock = DockStyle.Fill;
            this.pnlDeviceEditorCard.Margin = new Padding(0);
            StyleCard_Custom(this.pnlDeviceEditorCard);

            var deviceEditorLayout = new TableLayoutPanel();
            deviceEditorLayout.Dock = DockStyle.Fill;
            deviceEditorLayout.BackColor = System.Drawing.Color.Transparent;
            deviceEditorLayout.ColumnCount = 1;
            deviceEditorLayout.RowCount = 4;
            deviceEditorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            deviceEditorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            deviceEditorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F));
            deviceEditorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            deviceEditorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            this.pnlDeviceEditorCard.Controls.Add(deviceEditorLayout);

            this.lblCardItemTitle = new Label();
            this.lblCardItemTitle.Text = "Configure Hardware Mapping";
            this.lblCardItemTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblCardItemTitle.Dock = DockStyle.Fill;
            this.lblCardItemTitle.Size = new System.Drawing.Size(300, 25);
            this.lblCardItemTitle.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            deviceEditorLayout.Controls.Add(this.lblCardItemTitle, 0, 0);

            // Unified input form
            this.pnlGeneralProperties = new Panel();
            this.pnlGeneralProperties.Dock = DockStyle.Fill;
            this.pnlGeneralProperties.Margin = new Padding(0, 0, 0, 10);

            Label lblExHub = new Label(); lblExHub.Text = "Target Hub Serial:"; lblExHub.Location = new System.Drawing.Point(0, 5); lblExHub.Size = new System.Drawing.Size(120, 18);
            this.cboDeviceHub = new ComboBox(); this.cboDeviceHub.Location = new System.Drawing.Point(130, 2); this.cboDeviceHub.Size = new System.Drawing.Size(260, 23);
            this.cboDeviceHub.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblExPort = new Label(); lblExPort.Text = "Hub Port (-1 = USB):"; lblExPort.Location = new System.Drawing.Point(0, 35); lblExPort.Size = new System.Drawing.Size(120, 18);
            this.cboDeviceHubPort = new ComboBox(); this.cboDeviceHubPort.Location = new System.Drawing.Point(130, 32); this.cboDeviceHubPort.Size = new System.Drawing.Size(260, 23);
            this.cboDeviceHubPort.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblExChan = new Label(); lblExChan.Text = "Port Channel:"; lblExChan.Location = new System.Drawing.Point(0, 65); lblExChan.Size = new System.Drawing.Size(120, 18);
            this.cboDeviceChannel = new ComboBox(); this.cboDeviceChannel.Location = new System.Drawing.Point(130, 62); this.cboDeviceChannel.Size = new System.Drawing.Size(260, 23);
            this.cboDeviceChannel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblExRef = new Label(); lblExRef.Text = "ProSim SDK DataRef:"; lblExRef.Location = new System.Drawing.Point(0, 95); lblExRef.Size = new System.Drawing.Size(120, 18);
            this.txtDeviceProsimRef = new TextBox(); this.txtDeviceProsimRef.Location = new System.Drawing.Point(130, 92); this.txtDeviceProsimRef.Size = new System.Drawing.Size(260, 23);
            this.txtDeviceProsimRef.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.pnlGeneralProperties.Controls.Add(lblExHub); this.pnlGeneralProperties.Controls.Add(this.cboDeviceHub);
            this.pnlGeneralProperties.Controls.Add(lblExPort); this.pnlGeneralProperties.Controls.Add(this.cboDeviceHubPort);
            this.pnlGeneralProperties.Controls.Add(lblExChan); this.pnlGeneralProperties.Controls.Add(this.cboDeviceChannel);
            this.pnlGeneralProperties.Controls.Add(lblExRef); this.pnlGeneralProperties.Controls.Add(this.txtDeviceProsimRef);

            deviceEditorLayout.Controls.Add(this.pnlGeneralProperties, 0, 1);

            var advancedPropertiesHost = new Panel();
            advancedPropertiesHost.Dock = DockStyle.Fill;
            advancedPropertiesHost.Margin = new Padding(0, 0, 0, 10);
            advancedPropertiesHost.AutoScroll = true;

            // Container Panel for category properties
            this.pnlAdvancedProperties_Inputs = new Panel();
            this.pnlAdvancedProperties_Inputs.Dock = DockStyle.Fill;
            this.pnlAdvancedProperties_Inputs.Visible = false;
            InitializeInputsFields_Custom();
            advancedPropertiesHost.Controls.Add(this.pnlAdvancedProperties_Inputs);

            this.pnlAdvancedProperties_Outputs = new Panel();
            this.pnlAdvancedProperties_Outputs.Dock = DockStyle.Fill;
            this.pnlAdvancedProperties_Outputs.Visible = false;
            InitializeOutputsFields_Custom();
            advancedPropertiesHost.Controls.Add(this.pnlAdvancedProperties_Outputs);

            this.pnlAdvancedProperties_Motors = new Panel();
            this.pnlAdvancedProperties_Motors.Dock = DockStyle.Fill;
            this.pnlAdvancedProperties_Motors.Visible = false;
            InitializeMotorsFields_Custom();
            advancedPropertiesHost.Controls.Add(this.pnlAdvancedProperties_Motors);

            this.pnlAdvancedProperties_Voltages = new Panel();
            this.pnlAdvancedProperties_Voltages.Dock = DockStyle.Fill;
            this.pnlAdvancedProperties_Voltages.Visible = false;
            InitializeVoltagesFields_Custom();
            advancedPropertiesHost.Controls.Add(this.pnlAdvancedProperties_Voltages);
            deviceEditorLayout.Controls.Add(advancedPropertiesHost, 0, 2);

            this.btnSaveDevice = new Button();
            this.btnSaveDevice.Text = "Save Mapping";
            this.btnSaveDevice.FlatStyle = FlatStyle.Flat;
            this.btnSaveDevice.FlatAppearance.BorderSize = 0;
            this.btnSaveDevice.BackColor = System.Drawing.Color.FromArgb(16, 185, 129);
            this.btnSaveDevice.ForeColor = System.Drawing.Color.White;
            this.btnSaveDevice.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSaveDevice.Dock = DockStyle.Fill;
            this.btnSaveDevice.Margin = new Padding(0);
            deviceEditorLayout.Controls.Add(this.btnSaveDevice, 0, 3);

            devicesLayout.Controls.Add(this.pnlDeviceEditorCard, 1, 1);
            this.pnlPageDevices.Controls.Add(devicesLayout);
        }

        private void InitializeInputsFields_Custom()
        {
            Label lblVal = new Label(); lblExRefText(lblVal, "Input Active Value:", 0);
            this.txtInputValue = new NumericUpDown(); this.txtInputValue.Location = new System.Drawing.Point(130, 2); this.txtInputValue.Size = new System.Drawing.Size(260, 23);
            this.txtInputValue.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblOffVal = new Label(); lblExRefText(lblOffVal, "Input Inactive Value:", 30);
            this.txtOffInputValue = new NumericUpDown(); this.txtOffInputValue.Location = new System.Drawing.Point(130, 32); this.txtOffInputValue.Size = new System.Drawing.Size(260, 23);
            this.txtOffInputValue.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblRef2 = new Label(); lblExRefText(lblRef2, "Aux DataRef 2:", 60);
            this.txtInputRef2 = new TextBox(); this.txtInputRef2.Location = new System.Drawing.Point(130, 62); this.txtInputRef2.Size = new System.Drawing.Size(260, 23);
            this.txtInputRef2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblRef3 = new Label(); lblExRefText(lblRef3, "Aux DataRef 3:", 90);
            this.txtInputRef3 = new TextBox(); this.txtInputRef3.Location = new System.Drawing.Point(130, 92); this.txtInputRef3.Size = new System.Drawing.Size(260, 23);
            this.txtInputRef3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblVar = new Label(); lblExRefText(lblVar, "User Variable:", 120);
            this.txtInputUserVar = new TextBox(); this.txtInputUserVar.Location = new System.Drawing.Point(130, 122); this.txtInputUserVar.Size = new System.Drawing.Size(260, 23);
            this.txtInputUserVar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.pnlAdvancedProperties_Inputs.Controls.Add(lblVal); this.pnlAdvancedProperties_Inputs.Controls.Add(this.txtInputValue);
            this.pnlAdvancedProperties_Inputs.Controls.Add(lblOffVal); this.pnlAdvancedProperties_Inputs.Controls.Add(this.txtOffInputValue);
            this.pnlAdvancedProperties_Inputs.Controls.Add(lblRef2); this.pnlAdvancedProperties_Inputs.Controls.Add(this.txtInputRef2);
            this.pnlAdvancedProperties_Inputs.Controls.Add(lblRef3); this.pnlAdvancedProperties_Inputs.Controls.Add(this.txtInputRef3);
            this.pnlAdvancedProperties_Inputs.Controls.Add(lblVar); this.pnlAdvancedProperties_Inputs.Controls.Add(this.txtInputUserVar);
        }

        private void InitializeOutputsFields_Custom()
        {
            this.chkOutputInverse = new CheckBox(); this.chkOutputInverse.Text = "Invert Output Signal Logic"; this.chkOutputInverse.Location = new System.Drawing.Point(5, 5); this.chkOutputInverse.Size = new System.Drawing.Size(250, 20);

            Label lblOnD = new Label(); lblExRefText(lblOnD, "Turn-on Delay (ms):", 30);
            this.txtOutputDelayOn = new NumericUpDown(); this.txtOutputDelayOn.Maximum = 10000; this.txtOutputDelayOn.Location = new System.Drawing.Point(140, 32); this.txtOutputDelayOn.Size = new System.Drawing.Size(250, 23);
            this.txtOutputDelayOn.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblOnM = new Label(); lblExRefText(lblOnM, "Max Active Time (ms):", 60);
            this.txtOutputMaxTimeOn = new NumericUpDown(); this.txtOutputMaxTimeOn.Maximum = 20000; this.txtOutputMaxTimeOn.Location = new System.Drawing.Point(140, 62); this.txtOutputMaxTimeOn.Size = new System.Drawing.Size(250, 23);
            this.txtOutputMaxTimeOn.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblOffRef = new Label(); lblExRefText(lblOffRef, "Inactive Trigger Ref:", 90);
            this.txtOutputRefOff = new TextBox(); this.txtOutputRefOff.Location = new System.Drawing.Point(140, 92); this.txtOutputRefOff.Size = new System.Drawing.Size(250, 23);
            this.txtOutputRefOff.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblUVar = new Label(); lblExRefText(lblUVar, "User Variable:", 120);
            this.txtOutputUserVar = new TextBox(); this.txtOutputUserVar.Location = new System.Drawing.Point(140, 122); this.txtOutputUserVar.Size = new System.Drawing.Size(250, 23);
            this.txtOutputUserVar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblOp = new Label(); lblExRefText(lblOp, "Operator (Gates):", 150);
            this.cboGateOperator = new ComboBox(); this.cboGateOperator.Items.Add("OR"); this.cboGateOperator.Items.Add("AND"); this.cboGateOperator.Location = new System.Drawing.Point(140, 152); this.cboGateOperator.Size = new System.Drawing.Size(250, 23);
            this.cboGateOperator.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblR2G = new Label(); lblExRefText(lblR2G, "Gate Altern. Ref 2:", 180);
            this.txtOutputRef2 = new TextBox(); this.txtOutputRef2.Location = new System.Drawing.Point(140, 182); this.txtOutputRef2.Size = new System.Drawing.Size(250, 23);
            this.txtOutputRef2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblVOn = new Label(); lblExRefText(lblVOn, "Custom On Volt Ratio:", 210);
            this.txtValueOn = new NumericUpDown(); this.txtValueOn.DecimalPlaces = 2; this.txtValueOn.Increment = 0.1M; this.txtValueOn.Location = new System.Drawing.Point(140, 212); this.txtValueOn.Size = new System.Drawing.Size(70, 23);
            this.txtValueOn.Value = 1.0M;

            Label lblVOff = new Label(); lblExRefText(lblVOff, "Off Ratio:", 210); lblVOff.Location = new System.Drawing.Point(215, 215); lblVOff.Width = 60;
            this.txtValueOff = new NumericUpDown(); this.txtValueOff.DecimalPlaces = 2; this.txtValueOff.Increment = 0.1M; this.txtValueOff.Location = new System.Drawing.Point(270, 212); this.txtValueOff.Size = new System.Drawing.Size(50, 23);
            this.txtValueOff.Value = 0.0M;

            Label lblVDim = new Label(); lblExRefText(lblVDim, "Dim:", 210); lblVDim.Location = new System.Drawing.Point(325, 215); lblVDim.Width = 35;
            this.txtValueDim = new NumericUpDown(); this.txtValueDim.DecimalPlaces = 2; this.txtValueDim.Increment = 0.1M; this.txtValueDim.Location = new System.Drawing.Point(355, 212); this.txtValueDim.Size = new System.Drawing.Size(40, 23);
            this.txtValueDim.Value = 0.7M;

            this.pnlAdvancedProperties_Outputs.Controls.Add(this.chkOutputInverse);
            this.pnlAdvancedProperties_Outputs.Controls.Add(lblOnD); this.pnlAdvancedProperties_Outputs.Controls.Add(this.txtOutputDelayOn);
            this.pnlAdvancedProperties_Outputs.Controls.Add(lblOnM); this.pnlAdvancedProperties_Outputs.Controls.Add(this.txtOutputMaxTimeOn);
            this.pnlAdvancedProperties_Outputs.Controls.Add(lblOffRef); this.pnlAdvancedProperties_Outputs.Controls.Add(this.txtOutputRefOff);
            this.pnlAdvancedProperties_Outputs.Controls.Add(lblUVar); this.pnlAdvancedProperties_Outputs.Controls.Add(this.txtOutputUserVar);
            this.pnlAdvancedProperties_Outputs.Controls.Add(lblOp); this.pnlAdvancedProperties_Outputs.Controls.Add(this.cboGateOperator);
            this.pnlAdvancedProperties_Outputs.Controls.Add(lblR2G); this.pnlAdvancedProperties_Outputs.Controls.Add(this.txtOutputRef2);
            this.pnlAdvancedProperties_Outputs.Controls.Add(lblVOn); this.pnlAdvancedProperties_Outputs.Controls.Add(this.txtValueOn);
            this.pnlAdvancedProperties_Outputs.Controls.Add(lblVOff); this.pnlAdvancedProperties_Outputs.Controls.Add(this.txtValueOff);
            this.pnlAdvancedProperties_Outputs.Controls.Add(lblVDim); this.pnlAdvancedProperties_Outputs.Controls.Add(this.txtValueDim);
        }

        private void InitializeMotorsFields_Custom()
        {
            this.chkMotorReversed = new CheckBox(); this.chkMotorReversed.Text = "Reverse Motor Direction Angle"; this.chkMotorReversed.Location = new System.Drawing.Point(5, 5); this.chkMotorReversed.Size = new System.Drawing.Size(220, 20);

            Label lblMo = new Label(); lblExRefText(lblMo, "Offset Step Tune:", 30);
            this.txtMotorOffset = new NumericUpDown(); this.txtMotorOffset.Minimum = -1000; this.txtMotorOffset.Maximum = 1000; this.txtMotorOffset.Location = new System.Drawing.Point(140, 32); this.txtMotorOffset.Size = new System.Drawing.Size(250, 23);
            this.txtMotorOffset.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblMf = new Label(); lblExRefText(lblMf, "Speed / Acceleration:", 60);
            this.txtMotorAcceleration = new NumericUpDown(); this.txtMotorAcceleration.DecimalPlaces = 3; this.txtMotorAcceleration.Increment = 0.05M; this.txtMotorAcceleration.Location = new System.Drawing.Point(140, 62); this.txtMotorAcceleration.Size = new System.Drawing.Size(250, 23);
            this.txtMotorAcceleration.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblMoF = new Label(); lblExRefText(lblMoF, "Turn-on Power Gate:", 90);
            this.txtMotorRefTurnOn = new TextBox(); this.txtMotorRefTurnOn.Location = new System.Drawing.Point(140, 92); this.txtMotorRefTurnOn.Size = new System.Drawing.Size(250, 23);
            this.txtMotorRefTurnOn.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblMTo = new Label(); lblExRefText(lblMTo, "Aux Power Gate 2:", 120);
            this.txtMotorRefTurnOn2 = new TextBox(); this.txtMotorRefTurnOn2.Location = new System.Drawing.Point(140, 122); this.txtMotorRefTurnOn2.Size = new System.Drawing.Size(250, 23);
            this.txtMotorRefTurnOn2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblMc = new Label(); lblExRefText(lblMc, "Current Pos Ref:", 150);
            this.txtMotorRefCurrentPos = new TextBox(); this.txtMotorRefCurrentPos.Location = new System.Drawing.Point(140, 152); this.txtMotorRefCurrentPos.Size = new System.Drawing.Size(250, 23);
            this.txtMotorRefCurrentPos.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblMt = new Label(); lblExRefText(lblMt, "Target Pos Ref:", 180);
            this.txtMotorRefTargetPos = new TextBox(); this.txtMotorRefTargetPos.Location = new System.Drawing.Point(140, 182); this.txtMotorRefTargetPos.Size = new System.Drawing.Size(250, 23);
            this.txtMotorRefTargetPos.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblPid = new Label(); lblExRefText(lblPid, "PID (Kp,Ki,Kd):", 210);
            this.txtPidKp = new TextBox(); this.txtPidKp.Location = new System.Drawing.Point(140, 212); this.txtPidKp.Size = new System.Drawing.Size(70, 23); this.txtPidKp.Text = "0.0003";
            this.txtPidKi = new TextBox(); this.txtPidKi.Location = new System.Drawing.Point(220, 212); this.txtPidKi.Size = new System.Drawing.Size(80, 23);
            this.txtPidKi.Text = "0.0001";
            this.txtPidKd = new TextBox(); this.txtPidKd.Location = new System.Drawing.Point(310, 212); this.txtPidKd.Size = new System.Drawing.Size(80, 23);
            this.txtPidKd.Text = "0.0";
            this.txtPidKp.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            Label lblDMc = new Label(); lblExRefText(lblDMc, "DC Limits:", 240);
            this.txtMotorCurrentLimit = new NumericUpDown(); this.txtMotorCurrentLimit.DecimalPlaces = 1; this.txtMotorCurrentLimit.Location = new System.Drawing.Point(140, 242); this.txtMotorCurrentLimit.Size = new System.Drawing.Size(60, 23);
            this.txtMotorCurrentLimit.Value = 4.0M;
            this.txtMotorRefFwd = new TextBox(); this.txtMotorRefFwd.Location = new System.Drawing.Point(210, 242); this.txtMotorRefFwd.Size = new System.Drawing.Size(85, 23);
            this.txtMotorRefFwd.Text = "Fwd Ref";
            this.txtMotorRefBwd = new TextBox(); this.txtMotorRefBwd.Location = new System.Drawing.Point(305, 242); this.txtMotorRefBwd.Size = new System.Drawing.Size(85, 23);
            this.txtMotorRefBwd.Text = "Bwd Ref";

            this.pnlAdvancedProperties_Motors.Controls.Add(this.chkMotorReversed);
            this.pnlAdvancedProperties_Motors.Controls.Add(lblMo); this.pnlAdvancedProperties_Motors.Controls.Add(this.txtMotorOffset);
            this.pnlAdvancedProperties_Motors.Controls.Add(lblMf); this.pnlAdvancedProperties_Motors.Controls.Add(this.txtMotorAcceleration);
            this.pnlAdvancedProperties_Motors.Controls.Add(lblMoF); this.pnlAdvancedProperties_Motors.Controls.Add(this.txtMotorRefTurnOn);
            this.pnlAdvancedProperties_Motors.Controls.Add(lblMTo); this.pnlAdvancedProperties_Motors.Controls.Add(this.txtMotorRefTurnOn2);
            this.pnlAdvancedProperties_Motors.Controls.Add(lblMc); this.pnlAdvancedProperties_Motors.Controls.Add(this.txtMotorRefCurrentPos);
            this.pnlAdvancedProperties_Motors.Controls.Add(lblMt); this.pnlAdvancedProperties_Motors.Controls.Add(this.txtMotorRefTargetPos);
            this.pnlAdvancedProperties_Motors.Controls.Add(lblPid); this.pnlAdvancedProperties_Motors.Controls.Add(this.txtPidKp); this.pnlAdvancedProperties_Motors.Controls.Add(this.txtPidKi); this.pnlAdvancedProperties_Motors.Controls.Add(this.txtPidKd);
            this.pnlAdvancedProperties_Motors.Controls.Add(lblDMc); this.pnlAdvancedProperties_Motors.Controls.Add(this.txtMotorCurrentLimit);
            this.pnlAdvancedProperties_Motors.Controls.Add(this.txtMotorRefFwd); this.pnlAdvancedProperties_Motors.Controls.Add(this.txtMotorRefBwd);
        }

        private void InitializeVoltagesFields_Custom()
        {
            this.chkVoltageUseSinCos = new CheckBox(); this.chkVoltageUseSinCos.Text = "SIN/COS Vector Outputs"; this.chkVoltageUseSinCos.Location = new System.Drawing.Point(5, 5); this.chkVoltageUseSinCos.Size = new System.Drawing.Size(180, 20);
            this.chkVoltageUseRange = new CheckBox(); this.chkVoltageUseRange.Text = "Voltage Range (1-5v)"; this.chkVoltageUseRange.Location = new System.Drawing.Point(195, 5); this.chkVoltageUseRange.Size = new System.Drawing.Size(180, 20);

            Label lblP1 = new Label(); lblExRefText(lblP1, "Input Points Map:", 30);
            this.txtVoltageInputPoints = new TextBox(); this.txtVoltageInputPoints.Location = new System.Drawing.Point(140, 32); this.txtVoltageInputPoints.Size = new System.Drawing.Size(250, 23);
            this.txtVoltageInputPoints.Text = "0,0.2,0.4,0.8,1";
            this.txtVoltageInputPoints.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblP2 = new Label(); lblExRefText(lblP2, "Output Points Map:", 60);
            this.txtVoltageOutputPoints = new TextBox(); this.txtVoltageOutputPoints.Location = new System.Drawing.Point(140, 62); this.txtVoltageOutputPoints.Size = new System.Drawing.Size(250, 23);
            this.txtVoltageOutputPoints.Text = "0,100,500,800,1024";
            this.txtVoltageOutputPoints.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblP3 = new Label(); lblExRefText(lblP3, "Interpolat. Mode:", 90);
            this.cboVoltageInterpolation = new ComboBox(); this.cboVoltageInterpolation.Items.Add("Linear"); this.cboVoltageInterpolation.Items.Add("Curve"); this.cboVoltageInterpolation.Items.Add("Spline");
            this.cboVoltageInterpolation.Location = new System.Drawing.Point(140, 92); this.cboVoltageInterpolation.Size = new System.Drawing.Size(250, 23);
            this.cboVoltageInterpolation.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblP4 = new Label(); lblExRefText(lblP4, "Min Trigger Dev:", 120);
            this.txtVoltageMinChange = new NumericUpDown(); this.txtVoltageMinChange.DecimalPlaces = 4; this.txtVoltageMinChange.Increment = 0.001M; this.txtVoltageMinChange.Location = new System.Drawing.Point(140, 122); this.txtVoltageMinChange.Size = new System.Drawing.Size(95, 23);
            this.txtVoltageMinChange.Value = 0.002M;

            Label lblP5 = new Label(); lblExRefText(lblP5, "Interval (ms):", 120); lblP5.Location = new System.Drawing.Point(240, 125); lblP5.Width = 80;
            this.txtVoltageInterval = new NumericUpDown(); this.txtVoltageInterval.Location = new System.Drawing.Point(320, 122); this.txtVoltageInterval.Size = new System.Drawing.Size(70, 23);
            this.txtVoltageInterval.Value = 50M;

            Label lblP6 = new Label(); lblExRefText(lblP6, "Curve Power Exp:", 150);
            this.txtVoltageCurvePower = new NumericUpDown(); this.txtVoltageCurvePower.DecimalPlaces = 1; this.txtVoltageCurvePower.Location = new System.Drawing.Point(140, 152); this.txtVoltageCurvePower.Size = new System.Drawing.Size(250, 23);
            this.txtVoltageCurvePower.Value = 2.0M;
            this.txtVoltageCurvePower.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblP7 = new Label(); lblExRefText(lblP7, "Scale Out Factor:", 180);
            this.txtVoltageScale = new NumericUpDown(); this.txtVoltageScale.Maximum = 10000; this.txtVoltageScale.Location = new System.Drawing.Point(140, 182); this.txtVoltageScale.Size = new System.Drawing.Size(95, 23);
            this.txtVoltageScale.Value = 500M;

            Label lblP8 = new Label(); lblExRefText(lblP8, "Offset Base:", 180); lblP8.Location = new System.Drawing.Point(240, 185); lblP8.Width = 80;
            this.txtVoltageOffset = new NumericUpDown(); this.txtVoltageOffset.Minimum = -24; this.txtVoltageOffset.Location = new System.Drawing.Point(320, 182); this.txtVoltageOffset.Size = new System.Drawing.Size(70, 23);
            this.txtVoltageOffset.Value = 0M;

            Label lblP9 = new Label(); lblExRefText(lblP9, "Amplitude Volts:", 210);
            this.txtVoltageAmplitude = new NumericUpDown(); this.txtVoltageAmplitude.DecimalPlaces = 1; this.txtVoltageAmplitude.Location = new System.Drawing.Point(140, 212); this.txtVoltageAmplitude.Size = new System.Drawing.Size(95, 23);
            this.txtVoltageAmplitude.Value = 10M;

            this.chkVoltageWrap360 = new CheckBox(); this.chkVoltageWrap360.Text = "Wrap 360 (Gauges)"; this.chkVoltageWrap360.Location = new System.Drawing.Point(245, 212); this.chkVoltageWrap360.Size = new System.Drawing.Size(150, 20);

            // Hideous labels to represent extra fields
            this.pnlAdvancedProperties_Voltages.Controls.Add(this.chkVoltageUseSinCos);
            this.pnlAdvancedProperties_Voltages.Controls.Add(this.chkVoltageUseRange);
            this.pnlAdvancedProperties_Voltages.Controls.Add(lblP1); this.pnlAdvancedProperties_Voltages.Controls.Add(this.txtVoltageInputPoints);
            this.pnlAdvancedProperties_Voltages.Controls.Add(lblP2); this.pnlAdvancedProperties_Voltages.Controls.Add(this.txtVoltageOutputPoints);
            this.pnlAdvancedProperties_Voltages.Controls.Add(lblP3); this.pnlAdvancedProperties_Voltages.Controls.Add(this.cboVoltageInterpolation);
            this.pnlAdvancedProperties_Voltages.Controls.Add(lblP4); this.pnlAdvancedProperties_Voltages.Controls.Add(this.txtVoltageMinChange);
            this.pnlAdvancedProperties_Voltages.Controls.Add(lblP5); this.pnlAdvancedProperties_Voltages.Controls.Add(this.txtVoltageInterval);
            this.pnlAdvancedProperties_Voltages.Controls.Add(lblP6); this.pnlAdvancedProperties_Voltages.Controls.Add(this.txtVoltageCurvePower);
            this.pnlAdvancedProperties_Voltages.Controls.Add(lblP7); this.pnlAdvancedProperties_Voltages.Controls.Add(this.txtVoltageScale);
            this.pnlAdvancedProperties_Voltages.Controls.Add(lblP8); this.pnlAdvancedProperties_Voltages.Controls.Add(this.txtVoltageOffset);
            this.pnlAdvancedProperties_Voltages.Controls.Add(lblP9); this.pnlAdvancedProperties_Voltages.Controls.Add(this.txtVoltageAmplitude);
            this.pnlAdvancedProperties_Voltages.Controls.Add(this.chkVoltageWrap360);
        }

        private void InitializeSpecialPage_Custom()
        {
            this.pnlPageSpecial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPageSpecial.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.pnlPageSpecial.Padding = new System.Windows.Forms.Padding(20);
            this.pnlPageSpecial.AutoScroll = true;
            this.pnlPageSpecial.AutoScrollMargin = new System.Drawing.Size(20, 20);

            var specialLayout = new TableLayoutPanel();
            specialLayout.Dock = DockStyle.Fill;
            specialLayout.BackColor = System.Drawing.Color.Transparent;
            specialLayout.ColumnCount = 1;
            specialLayout.RowCount = 2;
            specialLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            specialLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            specialLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label lblSpecialHeader = new Label();
            lblSpecialHeader.Text = "Dedicated Trim Wheel & Braking Core";
            lblSpecialHeader.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            lblSpecialHeader.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            lblSpecialHeader.Dock = DockStyle.Top;
            lblSpecialHeader.Margin = new Padding(0, 0, 0, 16);
            lblSpecialHeader.Size = new System.Drawing.Size(500, 30);
            specialLayout.Controls.Add(lblSpecialHeader, 0, 0);

            var specialCardsLayout = new TableLayoutPanel();
            specialCardsLayout.Dock = DockStyle.Fill;
            specialCardsLayout.BackColor = System.Drawing.Color.Transparent;
            specialCardsLayout.ColumnCount = 2;
            specialCardsLayout.RowCount = 1;
            specialCardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
            specialCardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            specialCardsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            specialLayout.Controls.Add(specialCardsLayout, 0, 1);

            // Box 1: Custom Trim Wheel Speed Mapping
            Panel cardTrim = new Panel();
            cardTrim.BackColor = System.Drawing.Color.White;
            cardTrim.Dock = DockStyle.Fill;
            cardTrim.Margin = new Padding(0, 0, 20, 0);
            StyleCard_Custom(cardTrim);

            Label lblTrimTitle = new Label();
            lblTrimTitle.Text = "Trim Wheel Control Limits";
            lblTrimTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            lblTrimTitle.Location = new System.Drawing.Point(15, 15);
            lblTrimTitle.Size = new System.Drawing.Size(250, 20);
            cardTrim.Controls.Add(lblTrimTitle);

            this.chkTrimReversed = new CheckBox(); this.chkTrimReversed.Text = "Reverse Speed Direction Axis"; this.chkTrimReversed.Location = new System.Drawing.Point(20, 50); this.chkTrimReversed.Size = new System.Drawing.Size(220, 20);
            cardTrim.Controls.Add(this.chkTrimReversed);
            this.chkTrimAccelerate = new CheckBox(); this.chkTrimAccelerate.Text = "Enable Custom Acceleration Module"; this.chkTrimAccelerate.Location = new System.Drawing.Point(20, 80); this.chkTrimAccelerate.Size = new System.Drawing.Size(260, 20);
            this.chkTrimAccelerate.Checked = true;
            cardTrim.Controls.Add(this.chkTrimAccelerate);

            int tY = 120;
            this.txtTrimDirtyUp = CreateLabelNud_Custom(cardTrim, "Speed Dirty Nose UP:", tY); tY += 50;
            this.txtTrimDirtyDown = CreateLabelNud_Custom(cardTrim, "Speed Dirty Nose DOWN:", tY); tY += 50;
            this.txtTrimCleanUp = CreateLabelNud_Custom(cardTrim, "Speed Clean Nose UP:", tY); tY += 50;
            this.txtTrimCleanDown = CreateLabelNud_Custom(cardTrim, "Speed Clean Nose DOWN:", tY); tY += 50;
            this.txtTrimAPClean = CreateLabelNud_Custom(cardTrim, "Speed Autopilot Clean:", tY); tY += 50;
            this.txtTrimAPDirty = CreateLabelNud_Custom(cardTrim, "Speed Autopilot Dirty:", tY);

            specialCardsLayout.Controls.Add(cardTrim, 0, 0);

            // Box 2: Custom Parking Brake System
            Panel cardBrake = new Panel();
            cardBrake.BackColor = System.Drawing.Color.White;
            cardBrake.Dock = DockStyle.Fill;
            cardBrake.Margin = new Padding(0);
            StyleCard_Custom(cardBrake);

            Label lblBrakeTitle = new Label();
            lblBrakeTitle.Text = "Toe Brake Configuration";
            lblBrakeTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            lblBrakeTitle.Location = new System.Drawing.Point(15, 15);
            lblBrakeTitle.Size = new System.Drawing.Size(250, 20);
            cardBrake.Controls.Add(lblBrakeTitle);

            int bY = 50;
            this.txtBrakeToeThresh = CreateLabelNud_Custom(cardBrake, "Toe Brake Engage Limit:", bY, 700); bY += 50;
            this.txtBrakeReleaseThresh = CreateLabelNud_Custom(cardBrake, "Toe Brake Release Limit:", bY, 900); bY += 50;
            this.txtBrakeReleaseDelay = CreateLabelNud_Custom(cardBrake, "Solenoid Action Delay (ms):", bY, 50); bY += 50;
            this.txtBrakeReleaseMaxTime = CreateLabelNud_Custom(cardBrake, "Solenoid Max On-time (ms):", bY, 1000); bY += 50;

            Label lblBs = new Label(); lblBs.Text = "Switch Variable ID:"; lblBs.Location = new System.Drawing.Point(20, bY); lblBs.Width = 130;
            this.txtBrakeSwitchVar = new TextBox(); this.txtBrakeSwitchVar.Location = new System.Drawing.Point(160, bY - 2); this.txtBrakeSwitchVar.Size = new System.Drawing.Size(160, 23);
            this.txtBrakeSwitchVar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cardBrake.Controls.Add(lblBs); cardBrake.Controls.Add(this.txtBrakeSwitchVar); bY += 40;

            Label lblBr = new Label(); lblBr.Text = "Relay Variable ID:"; lblBr.Location = new System.Drawing.Point(20, bY); lblBr.Width = 130;
            this.txtBrakeRelayVar = new TextBox(); this.txtBrakeRelayVar.Location = new System.Drawing.Point(160, bY - 2); this.txtBrakeRelayVar.Size = new System.Drawing.Size(160, 23);
            this.txtBrakeRelayVar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cardBrake.Controls.Add(lblBr); cardBrake.Controls.Add(this.txtBrakeRelayVar); bY += 40;

            Label lblBv = new Label(); lblBv.Text = "Release Variable ID:"; lblBv.Location = new System.Drawing.Point(20, bY); lblBv.Width = 130;
            this.txtBrakeReleaseVar = new TextBox(); this.txtBrakeReleaseVar.Location = new System.Drawing.Point(160, bY - 2); this.txtBrakeReleaseVar.Size = new System.Drawing.Size(160, 23);
            this.txtBrakeReleaseVar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cardBrake.Controls.Add(lblBv); cardBrake.Controls.Add(this.txtBrakeReleaseVar);

            specialCardsLayout.Controls.Add(cardBrake, 1, 0);
            this.pnlPageSpecial.Controls.Add(specialLayout);
        }

        private NumericUpDown CreateLabelNud_Custom(Panel card, string title, int y, int defaultVal = 0)
        {
            Label lbl = new Label();
            lbl.Text = title;
            lbl.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            lbl.Location = new System.Drawing.Point(20, y + 3);
            lbl.Width = 150;

            NumericUpDown nud = new NumericUpDown();
            nud.Location = new System.Drawing.Point(180, y);
            nud.Size = new System.Drawing.Size(140, 23);
            nud.Minimum = 0;
            nud.Maximum = 65535;
            nud.Value = defaultVal;
            nud.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            if (title.ToLower().Contains("speed"))
            {
                nud.DecimalPlaces = 2;
                nud.Increment = 0.05M;
                nud.Value = 1.0M;
            }

            card.Controls.Add(lbl);
            card.Controls.Add(nud);
            return nud;
        }

        private void InitializeSettingsPage_Custom()
        {
            this.pnlPageSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPageSettings.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
            this.pnlPageSettings.Padding = new System.Windows.Forms.Padding(20);
            this.pnlPageSettings.AutoScroll = true;
            this.pnlPageSettings.AutoScrollMargin = new System.Drawing.Size(20, 20);

            var settingsLayout = new TableLayoutPanel();
            settingsLayout.Dock = DockStyle.Fill;
            settingsLayout.BackColor = System.Drawing.Color.Transparent;
            settingsLayout.ColumnCount = 1;
            settingsLayout.RowCount = 2;
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label lblSettingsHeader = new Label();
            lblSettingsHeader.Text = "Global Device Settings";
            lblSettingsHeader.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            lblSettingsHeader.ForeColor = System.Drawing.Color.FromArgb(17, 24, 39);
            lblSettingsHeader.Dock = DockStyle.Top;
            lblSettingsHeader.Margin = new Padding(0, 0, 0, 16);
            lblSettingsHeader.Size = new System.Drawing.Size(400, 30);
            settingsLayout.Controls.Add(lblSettingsHeader, 0, 0);

            Panel cardMainSettings = new Panel();
            cardMainSettings.BackColor = System.Drawing.Color.White;
            cardMainSettings.Dock = DockStyle.Top;
            cardMainSettings.Size = new System.Drawing.Size(700, 300);
            cardMainSettings.Margin = new Padding(0);
            StyleCard_Custom(cardMainSettings);

            Label lblBlinkTitle = new Label();
            lblBlinkTitle.Text = "Dimmable & Blinking Outputs Configurations";
            lblBlinkTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            lblBlinkTitle.Location = new System.Drawing.Point(15, 15);
            lblBlinkTitle.Size = new System.Drawing.Size(400, 20);
            cardMainSettings.Controls.Add(lblBlinkTitle);

            int sY = 60;
            this.txtBlinkFast = CreateLabelNud_Custom(cardMainSettings, "Fast Blink Interval (ms):", sY, 300); sY += 50;
            this.txtBlinkSlow = CreateLabelNud_Custom(cardMainSettings, "Slow Blink Interval (ms):", sY, 600); sY += 50;
            this.txtDefaultDim = CreateLabelNud_Custom(cardMainSettings, "Default Dim Voltage Ratio:", sY);

            settingsLayout.Controls.Add(cardMainSettings, 0, 1);
            this.pnlPageSettings.Controls.Add(settingsLayout);
        }

        private void StyleCard_Custom(Panel pnl)
        {
            pnl.BorderStyle = BorderStyle.None;
            pnl.BackColor = System.Drawing.Color.White;
            pnl.Padding = new System.Windows.Forms.Padding(15);
        }

        #endregion

        // ── Sidebar Controls ──────────────────────────────────────────────
        private Panel pnlSidebar;
        private Label lblLogo;
        private Label lblLogoSub;
        private Button btnNavDashboard;
        private Button btnNavHubs;
        private Button btnNavDevices;
        private Button btnNavSpecial;
        private Button btnNavSettings;
        private Label lblSidebarVersion;

        // Content panel
        private Panel pnlMainContent;

        // ── Pages Panels ──────────────────────────────────────────────────
        private Panel pnlPageDashboard;
        private Panel pnlPageHubs;
        private Panel pnlPageDevices;
        private Panel pnlPageSpecial;
        private Panel pnlPageSettings;

        // Page Controls & Cards declaration
        // (Dashboard Page Controls)
        private Panel cardProsimConn;
        private Label lblProsimTitle;
        private Label lblProsimIPLabelOnDash;
        private TextBox txtProsimIP;
        private Button btnConnectProsim;
        private Button btnDisconnectProsim;
        private Button btnSaveProsimIP;
        private Label connectionStatusLabel; // Preserve name

        private Panel cardTelemetry;
        private Label lblTelemetryTitle;
        private FlowLayoutPanel flowTelemetry;

        private Panel cardLogs;
        private Label lblLogsTitle;
        private TextBox txtLog; // Preserve name
        private Button btnLogClear; // Preserve name
        private Button btnLogOk; // Preserve name

        // (Hubs Page Controls)
        private DataGridView dgvConfiguredHubs;
        private ListView lvDiscoveredHubs;
        private Button btnScan;
        private Button btnAddHub;
        private Button btnDeleteHub;
        private TextBox txtHubName;
        private TextBox txtHubSerial;
        private CheckBox chkHubEnabled;

        // (Devices Page Controls)
        private FlowLayoutPanel flowCategories;
        private ListBox lstDevices;
        private TextBox txtDeviceSearch;
        private Panel pnlDeviceEditorCard;
        private Button btnAddDevice;
        private Button btnDeleteDevice;
        private Button btnSaveDevice;

        // Core fields for Device Editor
        private Label lblCardItemTitle;
        private ComboBox cboDeviceHub;
        private ComboBox cboDeviceHubPort;
        private ComboBox cboDeviceChannel;
        private TextBox txtDeviceProsimRef;

        // Specific fields shown based on tab/collapsible sections
        private Panel pnlGeneralProperties;
        private Panel pnlAdvancedProperties_Inputs;
        private Panel pnlAdvancedProperties_Outputs;
        private Panel pnlAdvancedProperties_Motors;
        private Panel pnlAdvancedProperties_Voltages;

        // Specific editor fields (Inputs)
        private NumericUpDown txtInputValue;
        private NumericUpDown txtOffInputValue;
        private TextBox txtInputUserVar;
        private TextBox txtInputRef2;
        private TextBox txtInputRef3;

        // Specific editor fields (Outputs & Gates)
        private CheckBox chkOutputInverse;
        private NumericUpDown txtOutputDelayOn;
        private NumericUpDown txtOutputMaxTimeOn;
        private TextBox txtOutputRefOff;
        private TextBox txtOutputRef2;
        private ComboBox cboGateOperator;
        private NumericUpDown txtValueOn;
        private NumericUpDown txtValueOff;
        private NumericUpDown txtValueDim;
        private TextBox txtOutputUserVar;

        // Specific editor fields (Multi-Inputs)
        private TextBox txtMultiInputChannels;
        private TextBox txtMultiInputMappings;

        // Specific editor fields (Voltages In & Out)
        private TextBox txtVoltageInputPoints;
        private TextBox txtVoltageOutputPoints;
        private ComboBox cboVoltageInterpolation;
        private NumericUpDown txtVoltageMinChange;
        private NumericUpDown txtVoltageInterval;
        private NumericUpDown txtVoltageCurvePower;
        private CheckBox chkVoltageUseRange;
        private NumericUpDown txtVoltageScale;
        private NumericUpDown txtVoltageOffset;
        private CheckBox chkVoltageUseSinCos;
        private NumericUpDown txtVoltageAmplitude;
        private CheckBox chkVoltageWrap360;

        // Specific editor fields (Motors & Encoders & Buttons)
        private NumericUpDown txtMotorOffset;
        private CheckBox chkMotorReversed;
        private TextBox txtMotorRefTurnOn;
        private TextBox txtMotorRefTurnOn2;
        private TextBox txtMotorRefCurrentPos;
        private TextBox txtMotorRefTargetPos;
        private NumericUpDown txtMotorAcceleration;
        private NumericUpDown txtMotorCurrentLimit;
        private TextBox txtMotorRefFwd;
        private TextBox txtMotorRefBwd;
        private TextBox txtPidKp;
        private TextBox txtPidKi;
        private TextBox txtPidKd;
        private NumericUpDown txtEncoderScale;
        private TextBox txtButtonName;
        private NumericUpDown txtButtonOnValue;
        private NumericUpDown txtButtonOffValue;

        // Special settings tabs controls
        private CheckBox chkTrimReversed;
        private CheckBox chkTrimAccelerate;
        private NumericUpDown txtTrimDirtyUp;
        private NumericUpDown txtTrimDirtyDown;
        private NumericUpDown txtTrimCleanUp;
        private NumericUpDown txtTrimCleanDown;
        private NumericUpDown txtTrimAPClean;
        private NumericUpDown txtTrimAPDirty;

        private TextBox txtBrakeSwitchVar;
        private TextBox txtBrakeRelayVar;
        private TextBox txtBrakeReleaseVar;
        private NumericUpDown txtBrakeToeThresh;
        private NumericUpDown txtBrakeReleaseThresh;
        private NumericUpDown txtBrakeReleaseDelay;
        private NumericUpDown txtBrakeReleaseMaxTime;

        private NumericUpDown txtBlinkFast;
        private NumericUpDown txtBlinkSlow;
        private NumericUpDown txtDefaultDim;
    }
}
