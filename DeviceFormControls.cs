using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    /// <summary>
    /// Reusable controls for common device configuration fields
    /// </summary>
    public class DeviceFormControls
    {
        public ComboBox CboHub { get; }
        public ComboBox CboHubPort { get; }
        public ComboBox CboChannel { get; }
        public TextBox TxtProsimRef { get; }

        public DeviceFormControls(
            ComboBox cboHub,
            ComboBox cboHubPort,
            ComboBox cboChannel,
            TextBox txtProsimRef)
        {
            CboHub = cboHub;
            CboHubPort = cboHubPort;
            CboChannel = cboChannel;
            TxtProsimRef = txtProsimRef;
        }

        public void PopulateHubDropdown(List<PhidgetsHubInst> hubs)
        {
            CboHub.DataSource = null;
            CboHub.Items.Clear();
            foreach (var hub in hubs)
            {
                CboHub.Items.Add(new HubComboBoxItem($"{hub.Name} ({hub.Serial})", hub));
            }
            if (CboHub.Items.Count > 0)
                CboHub.SelectedIndex = 0;
        }

        public void PopulateHubPortDropdown()
        {
            CboHubPort.Items.Clear();
            CboHubPort.Items.Add("No hub");
            for (int i = 0; i <= 8; i++)
                CboHubPort.Items.Add(i.ToString());
            CboHubPort.SelectedIndex = 0;
        }

        public void PopulateChannelDropdown()
        {
            CboChannel.Items.Clear();
            CboChannel.Items.Add("Use Port");
            for (int i = 0; i <= 15; i++)
                CboChannel.Items.Add(i.ToString());
            CboChannel.SelectedIndex = 0;
        }

        public PhidgetsHubInst GetSelectedHub()
        {
            var hubItem = CboHub.SelectedItem as HubComboBoxItem;
            return hubItem?.Hub;
        }

        public int GetHubPort()
        {
            return CboHubPort.SelectedItem.ToString() == "No hub" 
                ? -1 
                : int.Parse(CboHubPort.SelectedItem.ToString());
        }

        public int GetChannel()
        {
            return CboChannel.SelectedItem.ToString() == "Use Port" 
                ? -1 
                : int.Parse(CboChannel.SelectedItem.ToString());
        }

        public string GetProsimRef()
        {
            return TxtProsimRef.Text.Trim();
        }

        public void ClearProsimRef()
        {
            TxtProsimRef.Text = "";
        }

        public class HubComboBoxItem
        {
            public string Display { get; }
            public PhidgetsHubInst Hub { get; }

            public HubComboBoxItem(string display, PhidgetsHubInst hub)
            {
                Display = display;
                Hub = hub;
            }

            public override string ToString() => Display;
        }
    }
}
