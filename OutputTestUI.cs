using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    public partial class Form1
    {
        private Button btnOutputTest;
        private Label lblOutputTestStatus;
        private Phidget22.DigitalOutput _outputTest;
        private bool _outputTestStarting;

        // Builds the test button + status label at the bottom of the Outputs panel
        private void InitializeOutputTestControls()
        {
            var lbl = new Label { Text = "Test (not saved):", AutoSize = true, Location = new Point(5, 250) };
            btnOutputTest = new Button
            {
                Text = "▶  Turn ON", FlatStyle = FlatStyle.Flat, Size = new Size(250, 32),
                Location = new Point(140, 244),
                BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            btnOutputTest.FlatAppearance.BorderSize = 0;
            lblOutputTestStatus = new Label
            {
                Text = "Off", AutoSize = false, Size = new Size(250, 20), Location = new Point(140, 280),
                ForeColor = Color.FromArgb(107, 114, 128), Font = new Font("Segoe UI", 8.5F),
            };
            pnlAdvancedProperties_Outputs.Controls.Add(lbl);
            pnlAdvancedProperties_Outputs.Controls.Add(btnOutputTest);
            pnlAdvancedProperties_Outputs.Controls.Add(lblOutputTestStatus);

            btnOutputTest.Click += async (s, e) => { if (_outputTest == null) await StartOutputTest(); else StopOutputTest(); };
            pnlAdvancedProperties_Outputs.VisibleChanged += (s, e) => { if (!pnlAdvancedProperties_Outputs.Visible) StopOutputTest(); };
            FormClosing += (s, e) => StopOutputTest();
        }

        private async System.Threading.Tasks.Task StartOutputTest()
        {
            if (_outputTestStarting || _outputTest != null) return;
            var hub = _hubs.GetAll().FirstOrDefault(h => HubText(h) == cboDeviceHub.Text);
            if (hub == null)
            {
                MessageBox.Show("Select a Phidgets hub first.", "Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int port = (int)nudDeviceHubPort.Value;
            int channel = int.TryParse(cboDeviceChannel.Text, out int ch) ? ch : 0;
            double duty = Math.Max(0, Math.Min(1, (double)txtValueOn.Value));

            _outputTestStarting = true;
            btnOutputTest.Enabled = false;
            SetOutputTestStatus("Connecting…", Color.FromArgb(217, 119, 6));
            var output = new Phidget22.DigitalOutput();
            try
            {
                // Release running outputs/gates on this channel so the test can open it
                var live = _devices.Outputs.Concat(_devices.Gates)
                    .Where(o => o != null && o.Serial == hub.Serial && o.HubPort == port && o.Channel == channel).ToList();
                await System.Threading.Tasks.Task.Run(() =>
                {
                    foreach (var o in live) { o.Disconnect(); _outputTestReleased.Add(o); }
                    output.DeviceSerialNumber = hub.Serial;
                    if (port >= 0) { output.HubPort = port; output.IsRemote = true; }
                    output.Channel = channel;
                    output.Open(5000);
                    output.DutyCycle = duty;
                });
                _outputTest = output;
                btnOutputTest.Text = "■  Turn OFF";
                btnOutputTest.BackColor = Color.FromArgb(239, 68, 68);
                SetOutputTestStatus($"ON (duty {duty:0.##})", Color.FromArgb(5, 150, 105));
            }
            catch (Exception ex)
            {
                try { output.Close(); } catch { }
                ReconnectOutputsLive();
                SetOutputTestStatus("Could not open output: " + ex.Message, Color.FromArgb(220, 38, 38));
            }
            finally
            {
                _outputTestStarting = false;
                btnOutputTest.Enabled = true;
            }
        }

        private void StopOutputTest()
        {
            var output = _outputTest;
            _outputTest = null;
            if (output != null)
            {
                try { output.DutyCycle = 0; } catch { }
                try { output.Close(); } catch { }
            }
            ReconnectOutputsLive();
            if (btnOutputTest != null && !btnOutputTest.IsDisposed)
            {
                btnOutputTest.Text = "▶  Turn ON";
                btnOutputTest.BackColor = Color.FromArgb(16, 185, 129);
                if (output != null) SetOutputTestStatus("Off", Color.FromArgb(107, 114, 128));
            }
        }

        private readonly System.Collections.Generic.List<PhidgetsOutput> _outputTestReleased = new System.Collections.Generic.List<PhidgetsOutput>();

        // Hand the channel back to the running outputs once the test is done
        private void ReconnectOutputsLive()
        {
            var released = _outputTestReleased.ToList();
            _outputTestReleased.Clear();
            foreach (var o in released) o.Reconnect();
        }

        private void SetOutputTestStatus(string text, Color color)
        {
            if (lblOutputTestStatus == null || lblOutputTestStatus.IsDisposed) return;
            lblOutputTestStatus.Text = text;
            lblOutputTestStatus.ForeColor = color;
        }
    }
}

namespace Phidgets2Prosim
{
    public partial class Form1
    {
        private Label lblUVar, lblOp, lblR2G, lblVOn, lblVOff, lblVDim;

        // Outputs and Gates share one panel; show only the fields each type actually supports and close the gaps.
        private void ApplyOutputFieldVisibility(bool gate)
        {
            lblUVar.Visible = txtOutputUserVar.Visible = !gate;
            lblVOn.Visible = txtValueOn.Visible = lblVOff.Visible = txtValueOff.Visible = lblVDim.Visible = txtValueDim.Visible = !gate;
            lblOp.Visible = cboGateOperator.Visible = lblR2G.Visible = txtOutputRef2.Visible = gate;

            int y = 120; // first row after "Inactive Trigger Ref"
            void Row(Label lbl, params Control[] ctrls)
            {
                lbl.Top = y + 3;
                foreach (var c in ctrls) c.Top = y + 2;
                y += 30;
            }
            if (gate)
            {
                Row(lblR2G, txtOutputRef2);
                Row(lblOp, cboGateOperator);
            }
            else
            {
                Row(lblUVar, txtOutputUserVar);
                lblVOn.Top = y + 3; txtValueOn.Top = y + 2;
                lblVOff.Top = y + 3; txtValueOff.Top = y + 2;
                lblVDim.Top = y + 3; txtValueDim.Top = y + 2;
                y += 30;
            }
            if (btnOutputTest != null)
            {
                btnOutputTest.Top = y + 10;
                lblOutputTestStatus.Top = y + 46;
                pnlAdvancedProperties_Outputs.Controls.OfType<Label>().First(l => l.Text == "Test (not saved):").Top = y + 16;
            }
        }
    }
}
