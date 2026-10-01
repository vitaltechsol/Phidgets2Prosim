using System;
using System.Collections.Generic;
using Phidget22;

namespace Phidgets2Prosim
{
    /// <summary>
    /// Manages Phidgets hub configuration: CRUD operations (backed by
    /// ConfigRepository) and runtime network-server registration with Phidget22.
    /// </summary>
    internal class HubManager
    {
        public event Action<string> InfoLog;
        public event Action<string> ErrorLog;

        private readonly ConfigRepository _repo;

        public HubManager(ConfigRepository repo)
        {
            _repo = repo;
        }

        // ── CRUD ─────────────────────────────────────────────────────────────────

        public List<PhidgetsHubInst> GetAll() => _repo.LoadHubs();

        public void Add(PhidgetsHubInst hub)
        {
            var hubs = _repo.LoadHubs();
            hubs.Add(hub);
            _repo.SaveHubs(hubs);
            RaiseInfo("Hub added: " + hub.Name);
        }

        public void Delete(string name)
        {
            var hubs = _repo.LoadHubs();
            hubs.RemoveAll(h => h.Name == name);
            _repo.SaveHubs(hubs);
            RaiseInfo("Hub deleted: " + name);
        }

        public void SaveAll(List<PhidgetsHubInst> hubs)
        {
            _repo.SaveHubs(hubs);
        }

        // ── Runtime registration ─────────────────────────────────────────────────

        /// <summary>
        /// Registers enabled hubs with the Phidget22 network layer.
        /// Called once at startup after config is loaded.
        /// </summary>
        public void RegisterHubs(List<PhidgetsHubInst> hubs)
        {
            if (hubs == null) return;
            foreach (var hub in hubs)
            {
                if (!hub.Enabled) continue;
                try
                {
                    Net.AddServer(hub.Name, hub.Name, 5661, "", 0);
                    Net.EnableServer(hub.Name);
                    RaiseInfo("Hub Added: " + hub.Name + " (Serial: " + hub.Serial + ")");
                }
                catch (Exception ex)
                {
                    RaiseError("Cannot find Hub " + hub.Name + ": " + ex.Message);
                }
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void RaiseInfo(string msg)  => InfoLog?.Invoke(msg);
        private void RaiseError(string msg) => ErrorLog?.Invoke(msg);
    }
}
