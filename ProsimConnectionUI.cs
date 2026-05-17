using System;
using System.IO;
using System.Windows.Forms;
using ProSimSDK;
using YamlDotNet.Serialization;

namespace Phidgets2Prosim
{
    /// <summary>
    /// Manages Prosim connection and IP configuration
    /// </summary>
    public class ProsimConnectionUI
    {
        private readonly TextBox txtProsimIP;
        private readonly Button btnSaveProsimIP;
        private readonly Button btnConnectProsim;
        private readonly Button btnDisconnectProsim;
        private readonly Label lblConnectionStatus;
        private readonly ProSimConnect connection;
        private readonly Action<string> displayInfoLog;
        private readonly Action<string> displayErrorLog;
        private readonly Action updateStatusCallback;

        public string CurrentProsimIP { get; private set; }
        public bool IsConnected => connection?.isConnected ?? false;

        public ProsimConnectionUI(
            TextBox txtProsimIP,
            Button btnSaveProsimIP,
            Button btnConnectProsim,
            Button btnDisconnectProsim,
            Label lblConnectionStatus,
            ProSimConnect connection,
            Action<string> displayInfoLog,
            Action<string> displayErrorLog,
            Action updateStatusCallback)
        {
            this.txtProsimIP = txtProsimIP;
            this.btnSaveProsimIP = btnSaveProsimIP;
            this.btnConnectProsim = btnConnectProsim;
            this.btnDisconnectProsim = btnDisconnectProsim;
            this.lblConnectionStatus = lblConnectionStatus;
            this.connection = connection;
            this.displayInfoLog = displayInfoLog;
            this.displayErrorLog = displayErrorLog;
            this.updateStatusCallback = updateStatusCallback;

            Initialize();
        }

        private void Initialize()
        {
            btnSaveProsimIP.Click += BtnSaveProsimIP_Click;
            btnConnectProsim.Click += BtnConnectProsim_Click;
            btnDisconnectProsim.Click += BtnDisconnectProsim_Click;

            // Set initial button states
            UpdateButtonStates();
        }

        /// <summary>
        /// Loads the Prosim IP from the config file and displays it
        /// </summary>
        public void LoadProsimIPFromConfig(string configPath = "config.yaml")
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    displayInfoLog("Config file not found. Using default IP.");
                    CurrentProsimIP = "127.0.0.1";
                    txtProsimIP.Text = CurrentProsimIP;
                    return;
                }

                string yamlContent = File.ReadAllText(configPath);
                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
                var config = deserializer.Deserialize<Config>(yamlContent);

                CurrentProsimIP = config?.GeneralConfig?.ProSimIP ?? "127.0.0.1";
                txtProsimIP.Text = CurrentProsimIP;

                displayInfoLog($"Loaded Prosim IP: {CurrentProsimIP}");
            }
            catch (Exception ex)
            {
                displayErrorLog($"Error loading Prosim IP from config: {ex.Message}");
                CurrentProsimIP = "127.0.0.1";
                txtProsimIP.Text = CurrentProsimIP;
            }
        }

        /// <summary>
        /// Saves the Prosim IP to the config file
        /// </summary>
        private void BtnSaveProsimIP_Click(object sender, EventArgs e)
        {
            try
            {
                string newIP = txtProsimIP.Text.Trim();

                if (string.IsNullOrWhiteSpace(newIP))
                {
                    MessageBox.Show("Please enter a valid IP address.", "Invalid IP", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SaveProsimIPToConfig(newIP);
                CurrentProsimIP = newIP;

                displayInfoLog($"Prosim IP saved: {newIP}");
                MessageBox.Show($"Prosim IP saved successfully: {newIP}\n\nClick 'Connect' to connect to Prosim.", 
                    "IP Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                displayErrorLog($"Error saving Prosim IP: {ex.Message}");
                MessageBox.Show($"Error saving Prosim IP: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Connects to Prosim using the current IP
        /// </summary>
        private async void BtnConnectProsim_Click(object sender, EventArgs e)
        {
            try
            {
                string ipToConnect = txtProsimIP.Text.Trim();

                if (string.IsNullOrWhiteSpace(ipToConnect))
                {
                    MessageBox.Show("Please enter a valid IP address.", "Invalid IP", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                displayInfoLog($"Connecting to Prosim at {ipToConnect}...");

                // Update label on UI thread
                if (lblConnectionStatus.InvokeRequired)
                {
                    lblConnectionStatus.Invoke(new Action(() => lblConnectionStatus.Text = $"CONNECTING TO {ipToConnect}"));
                }
                else
                {
                    lblConnectionStatus.Text = $"CONNECTING TO {ipToConnect}";
                }

                UpdateButtonStates(connecting: true);

                await System.Threading.Tasks.Task.Run(() => connection.Connect(ipToConnect));

                CurrentProsimIP = ipToConnect;
                displayInfoLog($"Connected to Prosim at {ipToConnect}");

                UpdateButtonStates();
                updateStatusCallback?.Invoke();
            }
            catch (Exception ex)
            {
                displayErrorLog($"Error connecting to Prosim: {ex.Message}");
                MessageBox.Show($"Cannot connect to Prosim at {txtProsimIP.Text}\n\n{ex.Message}", 
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                UpdateButtonStates();
                updateStatusCallback?.Invoke();
            }
        }

        /// <summary>
        /// Disconnects from Prosim
        /// </summary>
        private void BtnDisconnectProsim_Click(object sender, EventArgs e)
        {
            try
            {
                // ProSimConnect doesn't have a Disconnect method
                // The disconnect event is triggered automatically when connection is lost
                displayInfoLog("To disconnect, close ProSim or restart this application.");
                MessageBox.Show("ProSimConnect does not support manual disconnect.\n\nTo disconnect, either:\n- Close ProSim\n- Restart this application", 
                    "Disconnect", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                displayErrorLog($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates button enabled states based on connection status
        /// </summary>
        private void UpdateButtonStates(bool connecting = false)
        {
            // Check if we need to invoke on the UI thread
            if (btnConnectProsim.InvokeRequired)
            {
                btnConnectProsim.Invoke(new Action(() => UpdateButtonStates(connecting)));
                return;
            }

            if (connecting)
            {
                btnConnectProsim.Enabled = false;
                btnDisconnectProsim.Enabled = false;
                btnSaveProsimIP.Enabled = false;
                txtProsimIP.Enabled = false;
            }
            else if (IsConnected)
            {
                btnConnectProsim.Enabled = false;
                btnDisconnectProsim.Enabled = true;
                btnSaveProsimIP.Enabled = false;
                txtProsimIP.Enabled = false;
            }
            else
            {
                btnConnectProsim.Enabled = true;
                btnDisconnectProsim.Enabled = false;
                btnSaveProsimIP.Enabled = true;
                txtProsimIP.Enabled = true;
            }
        }

        /// <summary>
        /// Saves the Prosim IP to the config.yaml file
        /// </summary>
        public void SaveProsimIPToConfig(string prosimIP, string configPath = "config.yaml")
        {
            try
            {
                string content = File.ReadAllText(configPath);

                // Parse existing config to preserve structure
                var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var result = new System.Text.StringBuilder();
                bool inGeneralConfig = false;
                bool prosimIPUpdated = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.TrimStart();

                    // Check if we're entering GeneralConfig section
                    if (trimmed.StartsWith("GeneralConfig:"))
                    {
                        inGeneralConfig = true;
                        result.AppendLine(line);
                        continue;
                    }

                    // If we're in GeneralConfig, look for ProSimIP
                    if (inGeneralConfig)
                    {
                        if (trimmed.StartsWith("ProSimIP:"))
                        {
                            // Get the indentation from the original line
                            int indentLength = line.Length - line.TrimStart().Length;
                            string indent = new string(' ', indentLength);
                            result.AppendLine($"{indent}ProSimIP: {prosimIP}");
                            prosimIPUpdated = true;
                            continue;
                        }

                        // Exit GeneralConfig if we hit a non-indented line or another top-level key
                        if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith(" ") && !line.StartsWith("\t"))
                        {
                            inGeneralConfig = false;
                        }
                    }

                    result.AppendLine(line);
                }

                // If ProSimIP wasn't found, add it to GeneralConfig
                if (!prosimIPUpdated)
                {
                    displayErrorLog("ProSimIP not found in config. Please ensure GeneralConfig section exists.");
                }

                File.WriteAllText(configPath, result.ToString().TrimEnd() + Environment.NewLine);
                displayInfoLog($"Prosim IP saved to {configPath}");
            }
            catch (Exception ex)
            {
                displayErrorLog($"Error saving Prosim IP to config: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Call this when connection state changes externally
        /// </summary>
        public void OnConnectionStateChanged()
        {
            // UpdateButtonStates already handles cross-thread marshalling
            UpdateButtonStates();
        }
    }
}
