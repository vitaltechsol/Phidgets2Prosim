using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Phidget22;

namespace Phidgets2Prosim
{
    public partial class ManageHubsForm : Form
    {
        public List<PhidgetsHubInst> Hubs { get; private set; }

        private ConcurrentDictionary<int, DiscoveredHub> discoveredHubs = new ConcurrentDictionary<int, DiscoveredHub>();

        public ManageHubsForm(List<PhidgetsHubInst> currentHubs)
        {
            InitializeComponent();
            Hubs = currentHubs.Select(h => new PhidgetsHubInst
            {
                Name = h.Name,
                Serial = h.Serial,
                Enabled = h.Enabled
            }).ToList();

            PopulateConfiguredGrid();
            this.Shown += async (s, ev) => btnScan_Click(s, ev);
        }

        private void PopulateConfiguredGrid()
        {
            dgvConfiguredHubs.Rows.Clear();
            foreach (var hub in Hubs.OrderBy(h => h.Name, StringComparer.OrdinalIgnoreCase))
            {
                dgvConfiguredHubs.Rows.Add(hub.Name, hub.Serial.ToString(), hub.Enabled);
            }
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            btnScan.Enabled = false;
            btnScan.Text = "Scanning...";
            lvDiscoveredHubs.Items.Clear();
            discoveredHubs.Clear();

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Net.EnableServerDiscovery(ServerType.DeviceRemote);
                    }
                    catch
                    {
                        // Already enabled or not available
                    }

                    var manager = new Phidget22.Manager();
                    manager.Attach += Manager_Attach;
                    manager.Open();

                    // Wait for devices to be discovered
                    System.Threading.Thread.Sleep(4000);

                    try { manager.Close(); } catch { }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error scanning for hubs: " + ex.Message, "Scan Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            UpdateDiscoveredList();

            btnScan.Enabled = true;
            btnScan.Text = "Scan Network";
        }

        private void Manager_Attach(object sender, Phidget22.Events.ManagerAttachEventArgs e)
        {
            try
            {
                var ch = e.Channel;
                int serial = ch.DeviceSerialNumber;
                if (serial <= 0) return;

                discoveredHubs.TryAdd(serial, new DiscoveredHub
                {
                    Serial = serial,
                    DeviceName = ch.DeviceName ?? "Unknown",
                    ServerName = ch.ServerName ?? "local"
                });
            }
            catch
            {
                // Ignore errors during discovery
            }
        }

        private void UpdateDiscoveredList()
        {
            lvDiscoveredHubs.Items.Clear();
            var configuredSerials = new HashSet<int>(Hubs.Where(h => h.Serial > 0).Select(h => h.Serial));

            foreach (var hub in discoveredHubs.Values.OrderBy(h => h.ServerName, StringComparer.OrdinalIgnoreCase))
            {
                var item = new ListViewItem(hub.Serial.ToString());
                item.SubItems.Add(hub.DeviceName);
                item.SubItems.Add(hub.ServerName);
                item.Tag = hub;

                if (configuredSerials.Contains(hub.Serial))
                {
                    item.ForeColor = System.Drawing.Color.Gray;
                }

                lvDiscoveredHubs.Items.Add(item);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (lvDiscoveredHubs.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a discovered hub to add.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (ListViewItem item in lvDiscoveredHubs.SelectedItems)
            {
                var hub = item.Tag as DiscoveredHub;
                if (hub == null) continue;

                // Check if already configured by serial
                if (Hubs.Any(h => h.Serial == hub.Serial))
                {
                    MessageBox.Show("Hub with serial " + hub.Serial + " is already configured.",
                        "Already Configured", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }

                Hubs.Add(new PhidgetsHubInst
                {
                    Name = hub.ServerName,
                    Serial = hub.Serial,
                    Enabled = true
                });
            }

            PopulateConfiguredGrid();
            UpdateDiscoveredList();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (dgvConfiguredHubs.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a configured hub to remove.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var namesToRemove = new HashSet<string>(
                dgvConfiguredHubs.SelectedRows.Cast<DataGridViewRow>()
                    .Select(r => r.Cells["colName"].Value?.ToString() ?? "")
                    .Where(n => !string.IsNullOrEmpty(n)),
                StringComparer.OrdinalIgnoreCase);

            Hubs.RemoveAll(h => namesToRemove.Contains(h.Name));

            PopulateConfiguredGrid();
            UpdateDiscoveredList();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private class DiscoveredHub
        {
            public int Serial { get; set; }
            public string DeviceName { get; set; }
            public string ServerName { get; set; }
        }
    }
}
