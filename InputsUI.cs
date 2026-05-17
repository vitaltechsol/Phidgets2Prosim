using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    public class InputsUI
    {
        private readonly DeviceFormControls deviceControls;
        private readonly ComboBox cboInputOnValue;
        private readonly ComboBox cboInputOffValue;
        private readonly Button btnAddInput;
        private readonly DataGridView dataGridViewInputs;
        private readonly Action<string> displayInfoLog;
        private readonly Action<string> displayErrorLog;
        private readonly Func<List<PhidgetsHubInst>> getHubs;

        public BindingList<PhidgetsInputInst> PhidgetsInputInstances { get; private set; }

        public InputsUI(
            ComboBox cboInputHub,
            ComboBox cboInputHubPort,
            ComboBox cboInputChannel,
            TextBox txtInputProsimRef,
            ComboBox cboInputOnValue,
            ComboBox cboInputOffValue,
            Button btnAddInput,
            DataGridView dataGridViewInputs,
            Action<string> displayInfoLog,
            Action<string> displayErrorLog,
            Func<List<PhidgetsHubInst>> getHubs)
        {
            this.deviceControls = new DeviceFormControls(
                cboInputHub,
                cboInputHubPort,
                cboInputChannel,
                txtInputProsimRef);
            this.cboInputOnValue = cboInputOnValue;
            this.cboInputOffValue = cboInputOffValue;
            this.btnAddInput = btnAddInput;
            this.dataGridViewInputs = dataGridViewInputs;
            this.displayInfoLog = displayInfoLog;
            this.displayErrorLog = displayErrorLog;
            this.getHubs = getHubs;

            Initialize();
        }

        private void Initialize()
        {
            PopulateInputFormDropdowns();
            btnAddInput.Click += BtnAddInput_Click;
            dataGridViewInputs.CellEndEdit += DataGridViewInputs_CellEndEdit;
        }

        public void LoadInputsFromConfig(string configPath)
        {
            try
            {
                string yamlContent = File.ReadAllText(configPath);
                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
                var config = deserializer.Deserialize<Config>(yamlContent);
                var list = config.PhidgetsInputInstances != null ? new BindingList<PhidgetsInputInst>(config.PhidgetsInputInstances) : new BindingList<PhidgetsInputInst>();
                PhidgetsInputInstances = list;
                dataGridViewInputs.DataSource = PhidgetsInputInstances;
            }
            catch (Exception ex)
            {
                displayErrorLog("Error loading inputs from config: " + ex.Message);
            }
        }

        public void PopulateInputHubDropdown(List<PhidgetsHubInst> hubs)
        {
            deviceControls.PopulateHubDropdown(hubs);
        }

        private void PopulateInputFormDropdowns()
        {
            deviceControls.PopulateHubPortDropdown();
            deviceControls.PopulateChannelDropdown();

            cboInputOnValue.Items.Clear();
            for (int i = 0; i <= 10; i++)
                cboInputOnValue.Items.Add(i.ToString());
            cboInputOnValue.SelectedIndex = 1;

            cboInputOffValue.Items.Clear();
            for (int i = 0; i <= 10; i++)
                cboInputOffValue.Items.Add(i.ToString());
            cboInputOffValue.SelectedIndex = 0;
        }

        private void BtnAddInput_Click(object sender, EventArgs e)
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
                int onValue = int.Parse(cboInputOnValue.SelectedItem.ToString());
                int offValue = int.Parse(cboInputOffValue.SelectedItem.ToString());

                var newInput = new PhidgetsInputInst
                {
                    Serial = hub.Serial,
                    HubPort = hubPort,
                    Channel = channel,
                    ProsimDataRef = prosimRef,
                    InputValue = onValue,
                    OffInputValue = offValue
                };

                if (PhidgetsInputInstances == null)
                {
                    PhidgetsInputInstances = new BindingList<PhidgetsInputInst>();
                    dataGridViewInputs.DataSource = PhidgetsInputInstances;
                }

                PhidgetsInputInstances.Add(newInput);
                SaveInputsToConfig();

                deviceControls.ClearProsimRef();
                cboInputOnValue.SelectedIndex = 1;
                cboInputOffValue.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding input: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DataGridViewInputs_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            SaveInputsToConfig();
        }

        public void SaveInputsToConfig()
        {
            try
            {
                string content = File.ReadAllText("config.yaml");

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("PhidgetsInputInstances:");
                foreach (var input in PhidgetsInputInstances)
                {
                    sb.AppendLine("  - Serial: " + input.Serial);
                    sb.AppendLine("    HubPort: " + input.HubPort);
                    sb.AppendLine("    Channel: " + input.Channel);
                    sb.AppendLine("    ProsimDataRef: " + input.ProsimDataRef);
                    sb.AppendLine("    InputValue: " + input.InputValue);
                    sb.AppendLine("    OffInputValue: " + input.OffInputValue);
                    if (!string.IsNullOrEmpty(input.UserVariable))
                        sb.AppendLine("    UserVariable: " + input.UserVariable);
                    if (!string.IsNullOrEmpty(input.ProsimDataRef2))
                        sb.AppendLine("    ProsimDataRef2: " + input.ProsimDataRef2);
                    if (!string.IsNullOrEmpty(input.ProsimDataRef3))
                        sb.AppendLine("    ProsimDataRef3: " + input.ProsimDataRef3);
                }

                var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var result = new System.Text.StringBuilder();
                bool inSection = false;
                bool sectionWritten = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.TrimStart();

                    if (trimmed.StartsWith("PhidgetsInputInstances:"))
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
                displayInfoLog("Inputs config saved to config.yaml");
            }
            catch (Exception ex)
            {
                displayErrorLog("Error saving inputs config: " + ex.Message);
            }
        }
    }
}
