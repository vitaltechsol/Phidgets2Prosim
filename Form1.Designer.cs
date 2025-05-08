using System.Drawing;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabGroups = new System.Windows.Forms.TabControl();
            this.tabOut = new System.Windows.Forms.TabPage();
            this.dataGridViewOutputs = new System.Windows.Forms.DataGridView();
            this.tabInputs = new System.Windows.Forms.TabPage();
            this.dataGridViewInputs = new System.Windows.Forms.DataGridView();
            this.tabMultiInputs = new System.Windows.Forms.TabPage();
            this.dataGridViewMultiInputs = new System.Windows.Forms.DataGridView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.dataGridViewGates = new System.Windows.Forms.DataGridView();
            this.tabVoltageOut = new System.Windows.Forms.TabPage();
            this.dataGridViewVoltageOut = new System.Windows.Forms.DataGridView();
            this.tabButtons = new System.Windows.Forms.TabPage();
            this.buttonsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.btnLogClear = new System.Windows.Forms.Button();
            this.btnLogOk = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.tabVoltageIn = new System.Windows.Forms.TabPage();
            this.dataGridViewVoltageIn = new System.Windows.Forms.DataGridView();
            this.connectionStatusLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lblPsIP = new System.Windows.Forms.Label();
            this.tabGroups.SuspendLayout();
            this.tabOut.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOutputs)).BeginInit();
            this.tabInputs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewInputs)).BeginInit();
            this.tabMultiInputs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewMultiInputs)).BeginInit();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewGates)).BeginInit();
            this.tabVoltageOut.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewVoltageOut)).BeginInit();
            this.tabButtons.SuspendLayout();
            this.tabLog.SuspendLayout();
            this.tabVoltageIn.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewVoltageIn)).BeginInit();
            this.SuspendLayout();
            // 
            // tabGroups
            // 
            this.tabGroups.Controls.Add(this.tabOut);
            this.tabGroups.Controls.Add(this.tabInputs);
            this.tabGroups.Controls.Add(this.tabMultiInputs);
            this.tabGroups.Controls.Add(this.tabPage3);
            this.tabGroups.Controls.Add(this.tabVoltageOut);
            this.tabGroups.Controls.Add(this.tabVoltageIn);
            this.tabGroups.Controls.Add(this.tabButtons);
            this.tabGroups.Controls.Add(this.tabLog);
            this.tabGroups.Location = new System.Drawing.Point(16, 73);
            this.tabGroups.Name = "tabGroups";
            this.tabGroups.SelectedIndex = 0;
            this.tabGroups.Size = new System.Drawing.Size(638, 305);
            this.tabGroups.TabIndex = 0;
            // 
            // tabOut
            // 
            this.tabOut.Controls.Add(this.dataGridViewOutputs);
            this.tabOut.Location = new System.Drawing.Point(4, 22);
            this.tabOut.Name = "tabOut";
            this.tabOut.Padding = new System.Windows.Forms.Padding(3);
            this.tabOut.Size = new System.Drawing.Size(630, 279);
            this.tabOut.TabIndex = 0;
            this.tabOut.Text = "Outputs";
            this.tabOut.UseVisualStyleBackColor = true;
            // 
            // dataGridViewOutputs
            // 
            this.dataGridViewOutputs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewOutputs.Location = new System.Drawing.Point(6, 6);
            this.dataGridViewOutputs.Name = "dataGridViewOutputs";
            this.dataGridViewOutputs.Size = new System.Drawing.Size(618, 267);
            this.dataGridViewOutputs.TabIndex = 0;
            // 
            // tabInputs
            // 
            this.tabInputs.Controls.Add(this.dataGridViewInputs);
            this.tabInputs.Location = new System.Drawing.Point(4, 22);
            this.tabInputs.Name = "tabInputs";
            this.tabInputs.Padding = new System.Windows.Forms.Padding(3);
            this.tabInputs.Size = new System.Drawing.Size(630, 279);
            this.tabInputs.TabIndex = 1;
            this.tabInputs.Text = "Inputs";
            this.tabInputs.UseVisualStyleBackColor = true;
            // 
            // dataGridViewInputs
            // 
            this.dataGridViewInputs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewInputs.Location = new System.Drawing.Point(6, 6);
            this.dataGridViewInputs.Name = "dataGridViewInputs";
            this.dataGridViewInputs.Size = new System.Drawing.Size(618, 267);
            this.dataGridViewInputs.TabIndex = 1;
            // 
            // tabMultiInputs
            // 
            this.tabMultiInputs.Controls.Add(this.dataGridViewMultiInputs);
            this.tabMultiInputs.Location = new System.Drawing.Point(4, 22);
            this.tabMultiInputs.Name = "tabMultiInputs";
            this.tabMultiInputs.Padding = new System.Windows.Forms.Padding(3);
            this.tabMultiInputs.Size = new System.Drawing.Size(630, 279);
            this.tabMultiInputs.TabIndex = 6;
            this.tabMultiInputs.Text = "Inputs (Multi)";
            this.tabMultiInputs.UseVisualStyleBackColor = true;
            // 
            // dataGridViewMultiInputs
            // 
            this.dataGridViewMultiInputs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewMultiInputs.Location = new System.Drawing.Point(6, 6);
            this.dataGridViewMultiInputs.Name = "dataGridViewMultiInputs";
            this.dataGridViewMultiInputs.Size = new System.Drawing.Size(618, 267);
            this.dataGridViewMultiInputs.TabIndex = 3;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.dataGridViewGates);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(630, 279);
            this.tabPage3.TabIndex = 3;
            this.tabPage3.Text = "Gates";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // dataGridViewGates
            // 
            this.dataGridViewGates.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewGates.Location = new System.Drawing.Point(6, 6);
            this.dataGridViewGates.Name = "dataGridViewGates";
            this.dataGridViewGates.Size = new System.Drawing.Size(618, 267);
            this.dataGridViewGates.TabIndex = 2;
            // 
            // tabVoltageOut
            // 
            this.tabVoltageOut.Controls.Add(this.dataGridViewVoltageOut);
            this.tabVoltageOut.Location = new System.Drawing.Point(4, 22);
            this.tabVoltageOut.Name = "tabVoltageOut";
            this.tabVoltageOut.Padding = new System.Windows.Forms.Padding(3);
            this.tabVoltageOut.Size = new System.Drawing.Size(630, 279);
            this.tabVoltageOut.TabIndex = 4;
            this.tabVoltageOut.Text = "Voltage Out";
            this.tabVoltageOut.UseVisualStyleBackColor = true;
            // 
            // dataGridViewVoltageOut
            // 
            this.dataGridViewVoltageOut.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewVoltageOut.Location = new System.Drawing.Point(6, 6);
            this.dataGridViewVoltageOut.Name = "dataGridViewVoltageOut";
            this.dataGridViewVoltageOut.Size = new System.Drawing.Size(618, 267);
            this.dataGridViewVoltageOut.TabIndex = 3;
            // 
            // tabButtons
            // 
            this.tabButtons.Controls.Add(this.buttonsFlowLayoutPanel);
            this.tabButtons.Location = new System.Drawing.Point(4, 22);
            this.tabButtons.Name = "tabButtons";
            this.tabButtons.Padding = new System.Windows.Forms.Padding(3);
            this.tabButtons.Size = new System.Drawing.Size(630, 279);
            this.tabButtons.TabIndex = 5;
            this.tabButtons.Text = "Buttons";
            this.tabButtons.UseVisualStyleBackColor = true;
            // 
            // buttonsFlowLayoutPanel
            // 
            this.buttonsFlowLayoutPanel.Location = new System.Drawing.Point(6, 6);
            this.buttonsFlowLayoutPanel.Name = "buttonsFlowLayoutPanel";
            this.buttonsFlowLayoutPanel.Size = new System.Drawing.Size(614, 267);
            this.buttonsFlowLayoutPanel.TabIndex = 0;
            // 
            // tabLog
            // 
            this.tabLog.Controls.Add(this.btnLogClear);
            this.tabLog.Controls.Add(this.btnLogOk);
            this.tabLog.Controls.Add(this.txtLog);
            this.tabLog.Location = new System.Drawing.Point(4, 22);
            this.tabLog.Name = "tabLog";
            this.tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabLog.Size = new System.Drawing.Size(630, 279);
            this.tabLog.TabIndex = 2;
            this.tabLog.Text = "Log";
            this.tabLog.UseVisualStyleBackColor = true;
            // 
            // btnLogClear
            // 
            this.btnLogClear.Location = new System.Drawing.Point(87, 250);
            this.btnLogClear.Name = "btnLogClear";
            this.btnLogClear.Size = new System.Drawing.Size(75, 23);
            this.btnLogClear.TabIndex = 2;
            this.btnLogClear.Text = "Clear";
            this.btnLogClear.UseVisualStyleBackColor = true;
            this.btnLogClear.Click += new System.EventHandler(this.btnLogClear_Click);
            // 
            // btnLogOk
            // 
            this.btnLogOk.Location = new System.Drawing.Point(6, 250);
            this.btnLogOk.Name = "btnLogOk";
            this.btnLogOk.Size = new System.Drawing.Size(75, 23);
            this.btnLogOk.TabIndex = 1;
            this.btnLogOk.Text = "OK";
            this.btnLogOk.UseVisualStyleBackColor = true;
            this.btnLogOk.Click += new System.EventHandler(this.btnLogOk_Click);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(6, 6);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(618, 237);
            this.txtLog.TabIndex = 0;
            // 
            // tabVoltageIn
            // 
            this.tabVoltageIn.Controls.Add(this.dataGridViewVoltageIn);
            this.tabVoltageIn.Location = new System.Drawing.Point(4, 22);
            this.tabVoltageIn.Name = "tabVoltageIn";
            this.tabVoltageIn.Padding = new System.Windows.Forms.Padding(3);
            this.tabVoltageIn.Size = new System.Drawing.Size(630, 279);
            this.tabVoltageIn.TabIndex = 7;
            this.tabVoltageIn.Text = "Voltage In";
            this.tabVoltageIn.UseVisualStyleBackColor = true;
            // 
            // dataGridViewVoltageIn
            // 
            this.dataGridViewVoltageIn.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewVoltageIn.Location = new System.Drawing.Point(6, 6);
            this.dataGridViewVoltageIn.Name = "dataGridViewVoltageIn";
            this.dataGridViewVoltageIn.Size = new System.Drawing.Size(618, 267);
            this.dataGridViewVoltageIn.TabIndex = 4;
            // 
            // connectionStatusLabel
            // 
            this.connectionStatusLabel.AutoSize = true;
            this.connectionStatusLabel.Location = new System.Drawing.Point(12, 46);
            this.connectionStatusLabel.Name = "connectionStatusLabel";
            this.connectionStatusLabel.Size = new System.Drawing.Size(95, 13);
            this.connectionStatusLabel.TabIndex = 4;
            this.connectionStatusLabel.Text = "Prosim Connection";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(603, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "v1.1.0";
            // 
            // lblPsIP
            // 
            this.lblPsIP.AutoSize = true;
            this.lblPsIP.Location = new System.Drawing.Point(13, 30);
            this.lblPsIP.Name = "lblPsIP";
            this.lblPsIP.Size = new System.Drawing.Size(51, 13);
            this.lblPsIP.TabIndex = 6;
            this.lblPsIP.Text = "Prosim IP";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(686, 390);
            this.Controls.Add(this.lblPsIP);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.connectionStatusLabel);
            this.Controls.Add(this.tabGroups);
            this.Name = "Form1";
            this.Text = "Phidgets2Prosim";
            this.Load += new System.EventHandler(this.Form1_Load_1);
            this.tabGroups.ResumeLayout(false);
            this.tabOut.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOutputs)).EndInit();
            this.tabInputs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewInputs)).EndInit();
            this.tabMultiInputs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewMultiInputs)).EndInit();
            this.tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewGates)).EndInit();
            this.tabVoltageOut.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewVoltageOut)).EndInit();
            this.tabButtons.ResumeLayout(false);
            this.tabLog.ResumeLayout(false);
            this.tabLog.PerformLayout();
            this.tabVoltageIn.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewVoltageIn)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TabControl tabGroups;
        private TabPage tabOut;
        private TabPage tabInputs;
        private Button button1;
        private Button button2;
        private Button button3;
        private Label connectionStatusLabel;
        private DataGridView dataGridViewOutputs;
        private DataGridView dataGridViewInputs;
        private TabPage tabLog;
        private TextBox txtLog;
        private Label label1;
        private Label lblPsIP;
        private TabPage tabPage3;
        private DataGridView dataGridViewGates;
        private TabPage tabVoltageOut;
        private DataGridView dataGridViewVoltageOut;
        private TabPage tabButtons;
        private FlowLayoutPanel buttonsFlowLayoutPanel;
        private Button btnLogOk;
        private Button btnLogClear;
        private TabPage tabMultiInputs;
        private DataGridView dataGridViewMultiInputs;
        private TabPage tabVoltageIn;
        private DataGridView dataGridViewVoltageIn;
    }
}