using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    public class EncodersUI
    {
        private readonly DeviceFormControls deviceControls;
        private readonly TextBox txtEncoderScaleFactor;
        private readonly Button btnAddEncoder;
        private readonly DataGridView dataGridViewEncoders;
        private readonly Action<string> displayInfoLog;
        private readonly Action<string> displayErrorLog;
        private readonly Func<List<PhidgetsHubInst>> getHubs;

        public BindingList<PhidgetsEncoderInst> PhidgetsEncoderInstances { get; private set; }

        public EncodersUI(
            ComboBox cboEncoderHub,
            ComboBox cboEncoderHubPort,
            ComboBox cboEncoderChannel,
            TextBox txtEncoderProsimRef,
            TextBox txtEncoderScaleFactor,
            Button btnAddEncoder,
            DataGridView dataGridViewEncoders,
            Action<string> displayInfoLog,
            Action<string> displayErrorLog,
            Func<List<PhidgetsHubInst>> getHubs)
        {
            this.deviceControls = new DeviceFormControls(
                cboEncoderHub,
                cboEncoderHubPort,
                cboEncoderChannel,
                txtEncoderProsimRef);
            this.txtEncoderScaleFactor = txtEncoderScaleFactor;
            this.btnAddEncoder = btnAddEncoder;
            this.dataGridViewEncoders = dataGridViewEncoders;
            this.displayInfoLog = displayInfoLog;
            this.displayErrorLog = displayErrorLog;
            this.getHubs = getHubs;

            Initialize();
        }

        private void Initialize()
        {
            PopulateEncoderFormDropdowns();
            btnAddEncoder.Click += BtnAddEncoder_Click;
            dataGridViewEncoders.CellEndEdit += DataGridViewEncoders_CellEndEdit;
        }

        public void LoadEncodersFromConfig(string configPath)
        {
            try
            {
                string yamlContent = File.ReadAllText(configPath);
                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
                var config = deserializer.Deserialize<Config>(yamlContent);
                var list = config.PhidgetsEncoderInstances != null 
                    ? new BindingList<PhidgetsEncoderInst>(config.PhidgetsEncoderInstances) 
                    : new BindingList<PhidgetsEncoderInst>();
                PhidgetsEncoderInstances = list;
                dataGridViewEncoders.DataSource = PhidgetsEncoderInstances;
            }
            catch (Exception ex)
            {
                displayErrorLog("Error loading encoders from config: " + ex.Message);
            }
        }

        public void PopulateEncoderHubDropdown(List<PhidgetsHubInst> hubs)
        {
            deviceControls.PopulateHubDropdown(hubs);
        }

        private void PopulateEncoderFormDropdowns()
        {
            deviceControls.PopulateHubPortDropdown();
            deviceControls.PopulateChannelDropdown();
            txtEncoderScaleFactor.Text = "1";
        }

        private void BtnAddEncoder_Click(object sender, EventArgs e)
        {
            try
            {
                var hub = deviceControls.GetSelectedHub();
                if (hub == null)
                {
                    MessageBox.Show("Please select a hub.", "Missing Hub", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int hubPort = deviceControls.GetHubPort();
                int channel = deviceControls.GetChannel();
                string prosimRef = deviceControls.GetProsimRef();

                if (string.IsNullOrWhiteSpace(prosimRef))
                {
                    MessageBox.Show("Please enter a Prosim DataRef.", "Missing DataRef", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double scaleFactor;
                if (!double.TryParse(txtEncoderScaleFactor.Text, out scaleFactor))
                {
                    MessageBox.Show("Please enter a valid scale factor.", "Invalid Scale Factor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var newEncoder = new PhidgetsEncoderInst
                {
                    Serial = hub.Serial,
                    HubPort = hubPort,
                    Channel = channel,
                    ProsimDataRef = prosimRef,
                    ScaleFactor = scaleFactor
                };

                if (PhidgetsEncoderInstances == null)
                {
                    PhidgetsEncoderInstances = new BindingList<PhidgetsEncoderInst>();
                    dataGridViewEncoders.DataSource = PhidgetsEncoderInstances;
                }

                PhidgetsEncoderInstances.Add(newEncoder);
                SaveEncodersToConfig();

                deviceControls.ClearProsimRef();
                txtEncoderScaleFactor.Text = "1";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding encoder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DataGridViewEncoders_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            SaveEncodersToConfig();
        }

        public void SaveEncodersToConfig()
        {
            try
            {
                string content = File.ReadAllText("config.yaml");

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("PhidgetsEncoderInstances:");
                foreach (var encoder in PhidgetsEncoderInstances)
                {
                    sb.AppendLine("  - Serial: " + encoder.Serial);
                    sb.AppendLine("    HubPort: " + encoder.HubPort);
                    sb.AppendLine("    Channel: " + encoder.Channel);
                    sb.AppendLine("    ProsimDataRef: " + encoder.ProsimDataRef);
                    sb.AppendLine("    ScaleFactor: " + encoder.ScaleFactor);
                }

                var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var result = new System.Text.StringBuilder();
                bool inSection = false;
                bool sectionWritten = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.TrimStart();

                    if (trimmed.StartsWith("PhidgetsEncoderInstances:"))
                    {
                        inSection = true;
                        result.Append(sb.ToString());
                        sectionWritten = true;
                        continue;
                    }

                    if (inSection)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith(" ") || line.StartsWith("\t"))
                        {
                            continue;
                        }
                        else
                        {
                            inSection = false;
                        }
                    }

                    if (!inSection)
                    {
                        result.AppendLine(line);
                    }
                }

                if (!sectionWritten)
                {
                    result.Append(sb.ToString());
                }

                File.WriteAllText("config.yaml", result.ToString().TrimEnd() + Environment.NewLine);
                displayInfoLog("Encoders config saved to config.yaml");
            }
            catch (Exception ex)
            {
                displayErrorLog("Error saving encoders config: " + ex.Message);
            }
        }
    }
}
