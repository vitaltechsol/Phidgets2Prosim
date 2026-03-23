using System.Drawing;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    partial class ManageHubsForm
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblConfigured = new System.Windows.Forms.Label();
            this.dgvConfiguredHubs = new System.Windows.Forms.DataGridView();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSerial = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.btnRemove = new System.Windows.Forms.Button();
            this.lblDiscovered = new System.Windows.Forms.Label();
            this.lvDiscoveredHubs = new System.Windows.Forms.ListView();
            this.colDiscSerial = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDiscDevice = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDiscHostname = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnScan = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvConfiguredHubs)).BeginInit();
            this.SuspendLayout();
            // 
            // lblConfigured
            // 
            this.lblConfigured.AutoSize = true;
            this.lblConfigured.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.lblConfigured.Location = new System.Drawing.Point(12, 9);
            this.lblConfigured.Name = "lblConfigured";
            this.lblConfigured.Size = new System.Drawing.Size(120, 15);
            this.lblConfigured.Text = "Configured Hubs:";
            // 
            // dgvConfiguredHubs
            // 
            this.dgvConfiguredHubs.AllowUserToAddRows = false;
            this.dgvConfiguredHubs.AllowUserToDeleteRows = false;
            this.dgvConfiguredHubs.ReadOnly = true;
            this.dgvConfiguredHubs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvConfiguredHubs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvConfiguredHubs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colName,
                this.colSerial,
                this.colEnabled});
            this.dgvConfiguredHubs.Location = new System.Drawing.Point(12, 28);
            this.dgvConfiguredHubs.Name = "dgvConfiguredHubs";
            this.dgvConfiguredHubs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvConfiguredHubs.Size = new System.Drawing.Size(540, 160);
            this.dgvConfiguredHubs.TabIndex = 0;
            // 
            // colName
            // 
            this.colName.HeaderText = "Name";
            this.colName.Name = "colName";
            this.colName.Width = 200;
            // 
            // colSerial
            // 
            this.colSerial.HeaderText = "Serial";
            this.colSerial.Name = "colSerial";
            this.colSerial.Width = 120;
            // 
            // colEnabled
            // 
            this.colEnabled.HeaderText = "Enabled";
            this.colEnabled.Name = "colEnabled";
            this.colEnabled.Width = 70;
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(12, 194);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(120, 28);
            this.btnRemove.TabIndex = 1;
            this.btnRemove.Text = "Remove Selected";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // lblDiscovered
            // 
            this.lblDiscovered.AutoSize = true;
            this.lblDiscovered.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.lblDiscovered.Location = new System.Drawing.Point(12, 232);
            this.lblDiscovered.Name = "lblDiscovered";
            this.lblDiscovered.Size = new System.Drawing.Size(200, 15);
            this.lblDiscovered.Text = "Discovered Hubs on Network:";
            // 
            // lvDiscoveredHubs
            // 
            this.lvDiscoveredHubs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.lvDiscoveredHubs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colDiscSerial,
                this.colDiscDevice,
                this.colDiscHostname});
            this.lvDiscoveredHubs.FullRowSelect = true;
            this.lvDiscoveredHubs.GridLines = true;
            this.lvDiscoveredHubs.HideSelection = false;
            this.lvDiscoveredHubs.Location = new System.Drawing.Point(12, 252);
            this.lvDiscoveredHubs.Name = "lvDiscoveredHubs";
            this.lvDiscoveredHubs.Size = new System.Drawing.Size(540, 150);
            this.lvDiscoveredHubs.TabIndex = 2;
            this.lvDiscoveredHubs.UseCompatibleStateImageBehavior = false;
            this.lvDiscoveredHubs.View = System.Windows.Forms.View.Details;
            // 
            // colDiscSerial
            // 
            this.colDiscSerial.Text = "Serial";
            this.colDiscSerial.Width = 120;
            // 
            // colDiscDevice
            // 
            this.colDiscDevice.Text = "Device Name";
            this.colDiscDevice.Width = 200;
            // 
            // colDiscHostname
            // 
            this.colDiscHostname.Text = "Hostname";
            this.colDiscHostname.Width = 180;
            // 
            // btnScan
            // 
            this.btnScan.Location = new System.Drawing.Point(12, 408);
            this.btnScan.Name = "btnScan";
            this.btnScan.Size = new System.Drawing.Size(120, 28);
            this.btnScan.TabIndex = 3;
            this.btnScan.Text = "Scan Network";
            this.btnScan.UseVisualStyleBackColor = true;
            this.btnScan.Click += new System.EventHandler(this.btnScan_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(140, 408);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(150, 28);
            this.btnAdd.TabIndex = 4;
            this.btnAdd.Text = "Add Selected to Config";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(372, 408);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(88, 28);
            this.btnSave.TabIndex = 5;
            this.btnSave.Text = "Save && Close";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(466, 408);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(86, 28);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // ManageHubsForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(564, 448);
            this.Controls.Add(this.lblConfigured);
            this.Controls.Add(this.dgvConfiguredHubs);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.lblDiscovered);
            this.Controls.Add(this.lvDiscoveredHubs);
            this.Controls.Add(this.btnScan);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManageHubsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manage Phidgets Hubs";
            ((System.ComponentModel.ISupportInitialize)(this.dgvConfiguredHubs)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Label lblConfigured;
        private DataGridView dgvConfiguredHubs;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colSerial;
        private DataGridViewCheckBoxColumn colEnabled;
        private Button btnRemove;
        private Label lblDiscovered;
        private ListView lvDiscoveredHubs;
        private ColumnHeader colDiscSerial;
        private ColumnHeader colDiscDevice;
        private ColumnHeader colDiscHostname;
        private Button btnScan;
        private Button btnAdd;
        private Button btnSave;
        private Button btnCancel;
    }
}
