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
            this.btnManageHubs = new System.Windows.Forms.Button();
            this.tabGroups = new System.Windows.Forms.TabControl();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.btnLogClear = new System.Windows.Forms.Button();
            this.btnLogOk = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.tabOut = new System.Windows.Forms.TabPage();
            this.dataGridViewOutputs = new System.Windows.Forms.DataGridView();
            this.tabInputs = new System.Windows.Forms.TabPage();
            this.panelAddInput = new System.Windows.Forms.Panel();
            this.lblInputHub = new System.Windows.Forms.Label();
            this.cboInputHub = new System.Windows.Forms.ComboBox();
            this.lblInputHubPort = new System.Windows.Forms.Label();
            this.cboInputHubPort = new System.Windows.Forms.ComboBox();
            this.lblInputChannel = new System.Windows.Forms.Label();
            this.cboInputChannel = new System.Windows.Forms.ComboBox();
            this.lblInputProsimRef = new System.Windows.Forms.Label();
            this.txtInputProsimRef = new System.Windows.Forms.TextBox();
            this.lblInputOnValue = new System.Windows.Forms.Label();
            this.cboInputOnValue = new System.Windows.Forms.ComboBox();
            this.lblInputOffValue = new System.Windows.Forms.Label();
            this.cboInputOffValue = new System.Windows.Forms.ComboBox();
            this.btnAddInput = new System.Windows.Forms.Button();
            this.dataGridViewInputs = new System.Windows.Forms.DataGridView();
            this.tabMultiInputs = new System.Windows.Forms.TabPage();
            this.dataGridViewMultiInputs = new System.Windows.Forms.DataGridView();
            this.tabGates = new System.Windows.Forms.TabPage();
            this.dataGridViewGates = new System.Windows.Forms.DataGridView();
            this.tabVoltageOut = new System.Windows.Forms.TabPage();
            this.dataGridViewVoltageOut = new System.Windows.Forms.DataGridView();
            this.tabVoltageIn = new System.Windows.Forms.TabPage();
            this.dataGridViewVoltageIn = new System.Windows.Forms.DataGridView();
            this.tabDCMotors = new System.Windows.Forms.TabPage();
            this.dataGridDCMotors = new System.Windows.Forms.DataGridView();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtDCMotorIdx = new System.Windows.Forms.TextBox();
            this.btnDCMotor1Go = new System.Windows.Forms.Button();
            this.txtDCMotor1Target = new System.Windows.Forms.TextBox();
            this.tabButtons = new System.Windows.Forms.TabPage();
            this.buttonsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.connectionStatusLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lblPsIP = new System.Windows.Forms.Label();
            this.tabGroups.SuspendLayout();
            this.tabLog.SuspendLayout();
            this.tabOut.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOutputs)).BeginInit();
            this.tabInputs.SuspendLayout();
            this.panelAddInput.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewInputs)).BeginInit();
            this.tabMultiInputs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewMultiInputs)).BeginInit();
            this.tabGates.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewGates)).BeginInit();
            this.tabVoltageOut.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewVoltageOut)).BeginInit();
            this.tabVoltageIn.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewVoltageIn)).BeginInit();
            this.tabDCMotors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridDCMotors)).BeginInit();
            this.tabButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnManageHubs
            // 
            this.btnManageHubs.Location = new System.Drawing.Point(460, 10);
            this.btnManageHubs.Name = "btnManageHubs";
            this.btnManageHubs.Size = new System.Drawing.Size(100, 28);
            this.btnManageHubs.TabIndex = 10;
            this.btnManageHubs.Text = "Manage Hubs";
            this.btnManageHubs.UseVisualStyleBackColor = true;
            this.btnManageHubs.Click += new System.EventHandler(this.btnManageHubs_Click);
            // 
            // tabGroups
            // 
            this.tabGroups.Controls.Add(this.tabLog);
            this.tabGroups.Controls.Add(this.tabOut);
            this.tabGroups.Controls.Add(this.tabInputs);
            this.tabGroups.Controls.Add(this.tabMultiInputs);
            this.tabGroups.Controls.Add(this.tabGates);
            this.tabGroups.Controls.Add(this.tabVoltageOut);
            this.tabGroups.Controls.Add(this.tabVoltageIn);
            this.tabGroups.Controls.Add(this.tabDCMotors);
            this.tabGroups.Controls.Add(this.tabButtons);
            this.tabGroups.Location = new System.Drawing.Point(16, 73);
            this.tabGroups.Name = "tabGroups";
            this.tabGroups.SelectedIndex = 0;
            this.tabGroups.Size = new System.Drawing.Size(638, 305);
            this.tabGroups.TabIndex = 0;
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
            this.tabInputs.Controls.Add(this.panelAddInput);
            this.tabInputs.Controls.Add(this.dataGridViewInputs);
            this.tabInputs.Location = new System.Drawing.Point(4, 22);
            this.tabInputs.Name = "tabInputs";
            this.tabInputs.Padding = new System.Windows.Forms.Padding(3);
            this.tabInputs.Size = new System.Drawing.Size(630, 279);
            this.tabInputs.TabIndex = 1;
            this.tabInputs.Text = "Inputs";
            this.tabInputs.UseVisualStyleBackColor = true;
            // 
            // panelAddInput
            // 
            this.panelAddInput.Controls.Add(this.lblInputHub);
            this.panelAddInput.Controls.Add(this.cboInputHub);
            this.panelAddInput.Controls.Add(this.lblInputHubPort);
            this.panelAddInput.Controls.Add(this.cboInputHubPort);
            this.panelAddInput.Controls.Add(this.lblInputChannel);
            this.panelAddInput.Controls.Add(this.cboInputChannel);
            this.panelAddInput.Controls.Add(this.lblInputProsimRef);
            this.panelAddInput.Controls.Add(this.txtInputProsimRef);
            this.panelAddInput.Controls.Add(this.lblInputOnValue);
            this.panelAddInput.Controls.Add(this.cboInputOnValue);
            this.panelAddInput.Controls.Add(this.lblInputOffValue);
            this.panelAddInput.Controls.Add(this.cboInputOffValue);
            this.panelAddInput.Controls.Add(this.btnAddInput);
            this.panelAddInput.Location = new System.Drawing.Point(6, 6);
            this.panelAddInput.Name = "panelAddInput";
            this.panelAddInput.Size = new System.Drawing.Size(618, 58);
            this.panelAddInput.TabIndex = 2;
            // 
            // lblInputHub
            // 
            this.lblInputHub.AutoSize = true;
            this.lblInputHub.Location = new System.Drawing.Point(3, 7);
            this.lblInputHub.Name = "lblInputHub";
            this.lblInputHub.Size = new System.Drawing.Size(30, 13);
            this.lblInputHub.TabIndex = 0;
            this.lblInputHub.Text = "Hub:";
            // 
            // cboInputHub
            // 
            this.cboInputHub.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInputHub.Location = new System.Drawing.Point(33, 3);
            this.cboInputHub.Name = "cboInputHub";
            this.cboInputHub.Size = new System.Drawing.Size(130, 21);
            this.cboInputHub.TabIndex = 1;
            // 
            // lblInputHubPort
            // 
            this.lblInputHubPort.AutoSize = true;
            this.lblInputHubPort.Location = new System.Drawing.Point(168, 7);
            this.lblInputHubPort.Name = "lblInputHubPort";
            this.lblInputHubPort.Size = new System.Drawing.Size(29, 13);
            this.lblInputHubPort.TabIndex = 2;
            this.lblInputHubPort.Text = "Port:";
            // 
            // cboInputHubPort
            // 
            this.cboInputHubPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInputHubPort.Location = new System.Drawing.Point(198, 3);
            this.cboInputHubPort.Name = "cboInputHubPort";
            this.cboInputHubPort.Size = new System.Drawing.Size(65, 21);
            this.cboInputHubPort.TabIndex = 3;
            // 
            // lblInputChannel
            // 
            this.lblInputChannel.AutoSize = true;
            this.lblInputChannel.Location = new System.Drawing.Point(268, 7);
            this.lblInputChannel.Name = "lblInputChannel";
            this.lblInputChannel.Size = new System.Drawing.Size(23, 13);
            this.lblInputChannel.TabIndex = 4;
            this.lblInputChannel.Text = "Ch:";
            // 
            // cboInputChannel
            // 
            this.cboInputChannel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInputChannel.Location = new System.Drawing.Point(292, 3);
            this.cboInputChannel.Name = "cboInputChannel";
            this.cboInputChannel.Size = new System.Drawing.Size(75, 21);
            this.cboInputChannel.TabIndex = 5;
            // 
            // lblInputProsimRef
            // 
            this.lblInputProsimRef.AutoSize = true;
            this.lblInputProsimRef.Location = new System.Drawing.Point(372, 7);
            this.lblInputProsimRef.Name = "lblInputProsimRef";
            this.lblInputProsimRef.Size = new System.Drawing.Size(61, 13);
            this.lblInputProsimRef.TabIndex = 6;
            this.lblInputProsimRef.Text = "Prosim Ref:";
            // 
            // txtInputProsimRef
            // 
            this.txtInputProsimRef.Location = new System.Drawing.Point(438, 4);
            this.txtInputProsimRef.Name = "txtInputProsimRef";
            this.txtInputProsimRef.Size = new System.Drawing.Size(175, 20);
            this.txtInputProsimRef.TabIndex = 7;
            // 
            // lblInputOnValue
            // 
            this.lblInputOnValue.AutoSize = true;
            this.lblInputOnValue.Location = new System.Drawing.Point(3, 34);
            this.lblInputOnValue.Name = "lblInputOnValue";
            this.lblInputOnValue.Size = new System.Drawing.Size(42, 13);
            this.lblInputOnValue.TabIndex = 8;
            this.lblInputOnValue.Text = "On Val:";
            // 
            // cboInputOnValue
            // 
            this.cboInputOnValue.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInputOnValue.Location = new System.Drawing.Point(52, 30);
            this.cboInputOnValue.Name = "cboInputOnValue";
            this.cboInputOnValue.Size = new System.Drawing.Size(45, 21);
            this.cboInputOnValue.TabIndex = 9;
            // 
            // lblInputOffValue
            // 
            this.lblInputOffValue.AutoSize = true;
            this.lblInputOffValue.Location = new System.Drawing.Point(102, 34);
            this.lblInputOffValue.Name = "lblInputOffValue";
            this.lblInputOffValue.Size = new System.Drawing.Size(42, 13);
            this.lblInputOffValue.TabIndex = 10;
            this.lblInputOffValue.Text = "Off Val:";
            // 
            // cboInputOffValue
            // 
            this.cboInputOffValue.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInputOffValue.Location = new System.Drawing.Point(152, 30);
            this.cboInputOffValue.Name = "cboInputOffValue";
            this.cboInputOffValue.Size = new System.Drawing.Size(45, 21);
            this.cboInputOffValue.TabIndex = 11;
            // 
            // btnAddInput
            // 
            this.btnAddInput.Location = new System.Drawing.Point(210, 29);
            this.btnAddInput.Name = "btnAddInput";
            this.btnAddInput.Size = new System.Drawing.Size(60, 23);
            this.btnAddInput.TabIndex = 12;
            this.btnAddInput.Text = "Add";
            this.btnAddInput.UseVisualStyleBackColor = true;
            // 
            // dataGridViewInputs
            // 
            this.dataGridViewInputs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewInputs.Location = new System.Drawing.Point(6, 68);
            this.dataGridViewInputs.Name = "dataGridViewInputs";
            this.dataGridViewInputs.Size = new System.Drawing.Size(618, 205);
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
            // tabGates
            // 
            this.tabGates.Controls.Add(this.dataGridViewGates);
            this.tabGates.Location = new System.Drawing.Point(4, 22);
            this.tabGates.Name = "tabGates";
            this.tabGates.Padding = new System.Windows.Forms.Padding(3);
            this.tabGates.Size = new System.Drawing.Size(630, 279);
            this.tabGates.TabIndex = 3;
            this.tabGates.Text = "Gates";
            this.tabGates.UseVisualStyleBackColor = true;
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
            // tabDCMotors
            // 
            this.tabDCMotors.Controls.Add(this.dataGridDCMotors);
            this.tabDCMotors.Controls.Add(this.label4);
            this.tabDCMotors.Controls.Add(this.label3);
            this.tabDCMotors.Controls.Add(this.label2);
            this.tabDCMotors.Controls.Add(this.txtDCMotorIdx);
            this.tabDCMotors.Controls.Add(this.btnDCMotor1Go);
            this.tabDCMotors.Controls.Add(this.txtDCMotor1Target);
            this.tabDCMotors.Location = new System.Drawing.Point(4, 22);
            this.tabDCMotors.Name = "tabDCMotors";
            this.tabDCMotors.Padding = new System.Windows.Forms.Padding(3);
            this.tabDCMotors.Size = new System.Drawing.Size(630, 279);
            this.tabDCMotors.TabIndex = 8;
            this.tabDCMotors.Text = "DCMotor";
            this.tabDCMotors.UseVisualStyleBackColor = true;
            // 
            // dataGridDCMotors
            // 
            this.dataGridDCMotors.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridDCMotors.Location = new System.Drawing.Point(6, 124);
            this.dataGridDCMotors.Name = "dataGridDCMotors";
            this.dataGridDCMotors.Size = new System.Drawing.Size(618, 149);
            this.dataGridDCMotors.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(15, 12);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(92, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "TEST MOVING";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(19, 68);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(156, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Go to  (Based on Voltage Input)";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(53, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Motor Index";
            // 
            // txtDCMotorIdx
            // 
            this.txtDCMotorIdx.Location = new System.Drawing.Point(22, 36);
            this.txtDCMotorIdx.Name = "txtDCMotorIdx";
            this.txtDCMotorIdx.Size = new System.Drawing.Size(26, 20);
            this.txtDCMotorIdx.TabIndex = 1;
            this.txtDCMotorIdx.Text = "0";
            // 
            // btnDCMotor1Go
            // 
            this.btnDCMotor1Go.Location = new System.Drawing.Point(90, 83);
            this.btnDCMotor1Go.Name = "btnDCMotor1Go";
            this.btnDCMotor1Go.Size = new System.Drawing.Size(52, 23);
            this.btnDCMotor1Go.TabIndex = 3;
            this.btnDCMotor1Go.Text = "Go";
            this.btnDCMotor1Go.UseVisualStyleBackColor = true;
            this.btnDCMotor1Go.Click += new System.EventHandler(this.btnDCMotor1Go_Click);
            // 
            // txtDCMotor1Target
            // 
            this.txtDCMotor1Target.Location = new System.Drawing.Point(22, 84);
            this.txtDCMotor1Target.Name = "txtDCMotor1Target";
            this.txtDCMotor1Target.Size = new System.Drawing.Size(62, 20);
            this.txtDCMotor1Target.TabIndex = 2;
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
            this.label1.Text = "v1.3.0";
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
            this.Controls.Add(this.btnManageHubs);
            this.Controls.Add(this.lblPsIP);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.connectionStatusLabel);
            this.Controls.Add(this.tabGroups);
            this.Name = "Form1";
            this.Text = "Phidgets2Prosim";
            this.Load += new System.EventHandler(this.Form1_Load_1);
            this.tabGroups.ResumeLayout(false);
            this.tabLog.ResumeLayout(false);
            this.tabLog.PerformLayout();
            this.tabOut.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOutputs)).EndInit();
            this.tabInputs.ResumeLayout(false);
            this.panelAddInput.ResumeLayout(false);
            this.panelAddInput.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewInputs)).EndInit();
            this.tabMultiInputs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewMultiInputs)).EndInit();
            this.tabGates.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewGates)).EndInit();
            this.tabVoltageOut.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewVoltageOut)).EndInit();
            this.tabVoltageIn.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewVoltageIn)).EndInit();
            this.tabDCMotors.ResumeLayout(false);
            this.tabDCMotors.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridDCMotors)).EndInit();
            this.tabButtons.ResumeLayout(false);
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
        private TabPage tabGates;
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
        private TabPage tabDCMotors;
        private Button btnDCMotor1Go;
        private TextBox txtDCMotor1Target;
        private TextBox txtDCMotorIdx;
        private Label label3;
        private Label label2;
        private Label label4;
        private DataGridView dataGridDCMotors;
        private Button btnManageHubs;
        private Panel panelAddInput;
        private Label lblInputHub;
        private ComboBox cboInputHub;
        private Label lblInputHubPort;
        private ComboBox cboInputHubPort;
        private Label lblInputChannel;
        private ComboBox cboInputChannel;
        private Label lblInputProsimRef;
        private TextBox txtInputProsimRef;
        private Label lblInputOnValue;
        private ComboBox cboInputOnValue;
        private Label lblInputOffValue;
        private ComboBox cboInputOffValue;
        private Button btnAddInput;
    }
}