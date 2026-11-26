using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    // "Preview" for Voltage In: opens the channel and shows the live reading on the curve editor.
    // The curve is re-evaluated from the editor fields on every change, so edits show immediately.
    // Nothing is written to ProSim and nothing is saved.
    public partial class Form1
    {
        private Button btnVoltagePreview;
        private Phidget22.VoltageRatioInput _previewRatio;
        private Phidget22.VoltageInput _previewRange;
        private bool _previewStarting;
        private double? _previewRaw;
        private readonly List<PhidgetsVoltageInput> _previewReleased = new List<PhidgetsVoltageInput>();

        private void InitializeVoltagePreviewControls()
        {
            btnVoltagePreview = new Button
            {
                Text = "▶  Preview", FlatStyle = FlatStyle.Flat, Size = new Size(250, 32),
                Location = new Point(140, 272), Tag = "in",
                BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            btnVoltagePreview.FlatAppearance.BorderSize = 0;
            pnlAdvancedProperties_Voltages.Controls.Add(btnVoltagePreview);

            btnVoltagePreview.Click += async (s, e) => { if (PreviewRunning) StopVoltagePreview(); else await StartVoltagePreview(); };
            pnlAdvancedProperties_Voltages.VisibleChanged += (s, e) => { if (!pnlAdvancedProperties_Voltages.Visible) StopVoltagePreview(); };
            chkVoltageUseRange.CheckedChanged += (s, e) => StopVoltagePreview();
            FormClosing += (s, e) => StopVoltagePreview();
        }

        private bool PreviewRunning => _previewRatio != null || _previewRange != null;

        private async System.Threading.Tasks.Task StartVoltagePreview()
        {
            if (_previewStarting || PreviewRunning) return;
            var hub = _hubs.GetAll().FirstOrDefault(h => HubText(h) == cboDeviceHub.Text);
            if (hub == null)
            {
                MessageBox.Show("Select a Phidgets hub first.", "Preview", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int port = (int)nudDeviceHubPort.Value;
            int channel = int.TryParse(cboDeviceChannel.Text, out int ch) ? ch : 0;
            bool range = chkVoltageUseRange.Checked;
            int interval = (int)txtVoltageInterval.Value;
            double trigger = (double)txtVoltageMinChange.Value;

            _previewStarting = true;
            btnVoltagePreview.Enabled = false;
            btnVoltagePreview.Text = "Connecting…";
            _previewRaw = null;

            Phidget22.Phidget opened = null;
            try
            {
                // A channel already opened by the running config can't be opened twice, so release it for the preview.
                var live = _devices.VoltageInputs
                    .Where(v => v != null && v.Serial == hub.Serial && v.HubPort == port && v.Channel == channel).ToList();
                await System.Threading.Tasks.Task.Run(() =>
                {
                    foreach (var v in live) { v.Close(); _previewReleased.Add(v); }
                    if (range)
                    {
                        var vi = new Phidget22.VoltageInput();
                        opened = vi;
                        vi.DeviceSerialNumber = hub.Serial;
                        if (port >= 0) { vi.HubPort = port; vi.IsRemote = true; }
                        if (channel > 0) vi.Channel = channel;
                        vi.VoltageChange += (s, e) => OnPreviewValue(e.Voltage);
                        vi.Open(5000);
                        try { vi.DataInterval = interval; vi.VoltageChangeTrigger = trigger; } catch { }
                        _previewRange = vi;
                        try { OnPreviewValue(vi.Voltage); } catch { }
                    }
                    else
                    {
                        var vr = new Phidget22.VoltageRatioInput();
                        opened = vr;
                        vr.DeviceSerialNumber = hub.Serial;
                        if (port >= 0) { vr.HubPort = port; vr.IsRemote = true; }
                        vr.Channel = channel;
                        vr.VoltageRatioChange += (s, e) => OnPreviewValue(e.VoltageRatio);
                        vr.Open(5000);
                        try { vr.DataInterval = interval; vr.VoltageRatioChangeTrigger = trigger; } catch { }
                        _previewRatio = vr;
                        try { OnPreviewValue(vr.VoltageRatio); } catch { }
                    }
                });
                btnVoltagePreview.Text = "■  Stop preview";
                btnVoltagePreview.BackColor = Color.FromArgb(239, 68, 68);
            }
            catch (Exception ex)
            {
                try { opened?.Close(); } catch { }
                _previewRatio = null; _previewRange = null;
                ReconnectVoltageInputsLive();
                btnVoltagePreview.Text = "▶  Preview";
                MessageBox.Show("Could not open input: " + ex.Message, "Preview", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _previewStarting = false;
                btnVoltagePreview.Enabled = true;
            }
        }

        private void StopVoltagePreview()
        {
            var vr = _previewRatio; var vi = _previewRange;
            _previewRatio = null; _previewRange = null;
            try { vr?.Close(); } catch { }
            try { vi?.Close(); } catch { }
            ReconnectVoltageInputsLive();
            curveEditor?.SetLive(null);
            if (btnVoltagePreview != null && !btnVoltagePreview.IsDisposed && !_previewStarting)
            {
                btnVoltagePreview.Text = "▶  Preview";
                btnVoltagePreview.BackColor = Color.FromArgb(16, 185, 129);
            }
        }

        // Hand the channel back to the running inputs once the preview is done
        private void ReconnectVoltageInputsLive()
        {
            var released = _previewReleased.ToList();
            _previewReleased.Clear();
            foreach (var v in released) v.Reconnect();
        }

        // Called from Phidget threads
        private void OnPreviewValue(double raw)
        {
            _previewRaw = raw;
            if (IsHandleCreated && !IsDisposed) BeginInvoke(new Action(RefreshVoltagePreview));
        }

        private void RefreshVoltagePreview()
        {
            if (!PreviewRunning || _previewRaw == null) return;
            curveEditor?.SetLive(_previewRaw.Value);
        }
    }
}
