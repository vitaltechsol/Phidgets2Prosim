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
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.connectionStatusLabel = new System.Windows.Forms.Label();
            this.btnSpeedBrake = new System.Windows.Forms.Button();
            this.dataGridViewInputs = new System.Windows.Forms.DataGridView();
            this.tabGroups.SuspendLayout();
            this.tabOut.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOutputs)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewInputs)).BeginInit();
            this.SuspendLayout();
            // 
            // tabGroups
            // 
            this.tabGroups.Controls.Add(this.tabOut);
            this.tabGroups.Controls.Add(this.tabPage2);
            this.tabGroups.Location = new System.Drawing.Point(10, 90);
            this.tabGroups.Name = "tabGroups";
            this.tabGroups.SelectedIndex = 0;
            this.tabGroups.Size = new System.Drawing.Size(638, 270);
            this.tabGroups.TabIndex = 0;
            // 
            // tabOut
            // 
            this.tabOut.Controls.Add(this.dataGridViewOutputs);
            this.tabOut.Location = new System.Drawing.Point(4, 22);
            this.tabOut.Name = "tabOut";
            this.tabOut.Padding = new System.Windows.Forms.Padding(3);
            this.tabOut.Size = new System.Drawing.Size(630, 244);
            this.tabOut.TabIndex = 0;
            this.tabOut.Text = "Outputs";
            this.tabOut.UseVisualStyleBackColor = true;
            // 
            // dataGridViewOutputs
            // 
            this.dataGridViewOutputs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewOutputs.Location = new System.Drawing.Point(7, 27);
            this.dataGridViewOutputs.Name = "dataGridViewOutputs";
            this.dataGridViewOutputs.Size = new System.Drawing.Size(611, 211);
            this.dataGridViewOutputs.TabIndex = 0;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dataGridViewInputs);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(630, 244);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Inputs";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // connectionStatusLabel
            // 
            this.connectionStatusLabel.AutoSize = true;
            this.connectionStatusLabel.Location = new System.Drawing.Point(12, 39);
            this.connectionStatusLabel.Name = "connectionStatusLabel";
            this.connectionStatusLabel.Size = new System.Drawing.Size(95, 13);
            this.connectionStatusLabel.TabIndex = 4;
            this.connectionStatusLabel.Text = "Prosim Connection";
            // 
            // btnSpeedBrake
            // 
            this.btnSpeedBrake.Location = new System.Drawing.Point(518, 34);
            this.btnSpeedBrake.Name = "btnSpeedBrake";
            this.btnSpeedBrake.Size = new System.Drawing.Size(114, 23);
            this.btnSpeedBrake.TabIndex = 6;
            this.btnSpeedBrake.Text = "Speed Brake Down";
            this.btnSpeedBrake.UseVisualStyleBackColor = true;
            this.btnSpeedBrake.Click += new System.EventHandler(this.btnSpeedBrake_Click);
            // 
            // dataGridViewInputs
            // 
            this.dataGridViewInputs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewInputs.Location = new System.Drawing.Point(10, 17);
            this.dataGridViewInputs.Name = "dataGridViewInputs";
            this.dataGridViewInputs.Size = new System.Drawing.Size(611, 211);
            this.dataGridViewInputs.TabIndex = 1;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(686, 390);
            this.Controls.Add(this.btnSpeedBrake);
            this.Controls.Add(this.connectionStatusLabel);
            this.Controls.Add(this.tabGroups);
            this.Name = "Form1";
            this.Text = "Phidgets2Prosim v0.1.0";
            this.Load += new System.EventHandler(this.Form1_Load_1);
            this.tabGroups.ResumeLayout(false);
            this.tabOut.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOutputs)).EndInit();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewInputs)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TabControl tabGroups;
        private TabPage tabOut;
        private TabPage tabPage2;
        private Button button1;
        private Button button2;
        private Button button3;
        private Label connectionStatusLabel;
        private Button btnSpeedBrake;
        private DataGridView dataGridViewOutputs;
        private DataGridView dataGridViewInputs;
    }
}