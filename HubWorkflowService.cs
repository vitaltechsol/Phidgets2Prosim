using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Phidget22;

namespace Phidgets2Prosim
{
    internal class DiscoveredHubInfo
    {
        public int Serial { get; set; }
        public string DeviceName { get; set; }
        public string ServerName { get; set; }
    }

    internal class HubWorkflowService
    {
        public List<PhidgetsHubInst> CloneConfiguredHubs(IEnumerable<PhidgetsHubInst> hubs)
        {
            return (hubs ?? Enumerable.Empty<PhidgetsHubInst>())
                .Select(h => new PhidgetsHubInst
                {
                    Name = h.Name,
                    Serial = h.Serial,
                    Enabled = h.Enabled
                })
                .ToList();
        }

        public async Task<List<DiscoveredHubInfo>> ScanAsync()
        {
            var discovered = new ConcurrentDictionary<int, DiscoveredHubInfo>();

            await Task.Run(() =>
            {
                try { Net.EnableServerDiscovery(ServerType.DeviceRemote); } catch { }

                var manager = new Manager();
                manager.Attach += (sender, e) =>
                {
                    try
                    {
                        var ch = e.Channel;
                        int serial = ch.DeviceSerialNumber;
                        if (serial <= 0) return;

                        discovered.TryAdd(serial, new DiscoveredHubInfo
                        {
                            Serial = serial,
                            DeviceName = ch.DeviceName ?? "Unknown",
                            ServerName = ch.ServerName ?? "local"
                        });
                    }
                    catch
                    {
                    }
                };

                manager.Open();
                System.Threading.Thread.Sleep(4000);
                try { manager.Close(); } catch { }
            });

            return discovered.Values
                .OrderBy(h => h.ServerName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(h => h.Serial)
                .ToList();
        }

        public bool TryAddDiscoveredHub(List<PhidgetsHubInst> hubs, DiscoveredHubInfo discoveredHub)
        {
            if (hubs == null || discoveredHub == null) return false;
            if (hubs.Any(h => h.Serial == discoveredHub.Serial)) return false;

            hubs.Add(new PhidgetsHubInst
            {
                Name = discoveredHub.ServerName,
                Serial = discoveredHub.Serial,
                Enabled = true
            });
            return true;
        }

        public bool RemoveConfiguredHubsByName(List<PhidgetsHubInst> hubs, IEnumerable<string> names)
        {
            if (hubs == null || names == null) return false;

            var namesToRemove = new HashSet<string>(
                names.Where(n => !string.IsNullOrWhiteSpace(n)),
                StringComparer.OrdinalIgnoreCase);

            if (namesToRemove.Count == 0) return false;

            int before = hubs.Count;
            hubs.RemoveAll(h => namesToRemove.Contains(h.Name));
            return hubs.Count != before;
        }

        public HashSet<int> GetConfiguredSerials(IEnumerable<PhidgetsHubInst> hubs)
        {
            return new HashSet<int>((hubs ?? Enumerable.Empty<PhidgetsHubInst>())
                .Where(h => h.Serial > 0)
                .Select(h => h.Serial));
        }
    }
}
