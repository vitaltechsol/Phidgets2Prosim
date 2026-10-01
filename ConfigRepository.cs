using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Phidgets2Prosim
{
    /// <summary>
    /// Single-responsibility YAML I/O for config.yaml.
    /// No UI dependencies — returns data, raises events, never shows dialogs.
    /// </summary>
    internal class ConfigRepository
    {
        private const string ConfigFile = "config.yaml";
        private const string BackupFile = "config.yaml.v1.bak";

        public event Action<string> InfoLog;
        public event Action<string> ErrorLog;

        // ── Core load / save ─────────────────────────────────────────────────────

        /// <summary>Deserialises config.yaml. Returns null when the file does not exist.</summary>
        public Config Load()
        {
            if (!File.Exists(ConfigFile)) return null;
            try
            {
                string yaml = File.ReadAllText(ConfigFile);
                var d = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                return d.Deserialize<Config>(yaml);
            }
            catch (Exception ex)
            {
                RaiseError("Error reading config.yaml: " + ex.Message);
                return null;
            }
        }

        /// <summary>Serialises <paramref name="cfg"/> and overwrites config.yaml.</summary>
        public void Save(Config cfg)
        {
            try
            {
                var s = new SerializerBuilder().Build();
                File.WriteAllText(ConfigFile, s.Serialize(cfg));
            }
            catch (Exception ex)
            {
                RaiseError("Error writing config.yaml: " + ex.Message);
            }
        }

        // ── ProSim IP ────────────────────────────────────────────────────────────

        public string LoadProSimIP()
        {
            var cfg = Load();
            return cfg?.GeneralConfig?.ProSimIP ?? string.Empty;
        }

        public void SaveProSimIP(string ip)
        {
            if (!File.Exists(ConfigFile)) return;
            try
            {
                var lines = File.ReadAllLines(ConfigFile);
                var sb = new StringBuilder();
                bool found = false;
                foreach (var line in lines)
                {
                    if (line.TrimStart().StartsWith("ProSimIP:"))
                    {
                        sb.AppendLine("  ProSimIP: " + ip);
                        found = true;
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
                if (!found) sb.AppendLine("  ProSimIP: " + ip);
                File.WriteAllText(ConfigFile, sb.ToString());
                RaiseInfo("ProSim IP saved: " + ip);
            }
            catch (Exception ex)
            {
                RaiseError("Error saving ProSim IP: " + ex.Message);
            }
        }

        // ── Hub section ──────────────────────────────────────────────────────────

        public List<PhidgetsHubInst> LoadHubs()
        {
            var cfg = Load();
            return cfg?.PhidgetsHubsInstances ?? new List<PhidgetsHubInst>();
        }

        /// <summary>
        /// Replaces the PhidgetsHubsInstances YAML section in-place,
        /// preserving all other keys exactly as-is.
        /// </summary>
        public void SaveHubs(List<PhidgetsHubInst> hubs)
        {
            if (!File.Exists(ConfigFile)) return;
            try
            {
                string content = File.ReadAllText(ConfigFile);
                var newBlock = BuildHubsBlock(hubs);

                var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var result = new StringBuilder();
                bool inHubs = false, written = false;

                foreach (var line in lines)
                {
                    string t = line.TrimStart();
                    if (t.StartsWith("PhidgetsHubsInstances:"))
                    {
                        inHubs = true;
                        result.Append(newBlock);
                        written = true;
                        continue;
                    }
                    if (inHubs)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith(" ") || line.StartsWith("\t"))
                            continue;
                        inHubs = false;
                    }
                    result.AppendLine(line);
                }
                if (!written) result.Append(newBlock);
                File.WriteAllText(ConfigFile, result.ToString().TrimEnd() + Environment.NewLine);
                RaiseInfo("Hubs config saved.");
            }
            catch (Exception ex)
            {
                RaiseError("Error saving hubs: " + ex.Message);
            }
        }

        // ── Schema migration ─────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether config.yaml is schema 1.0 and, if so, opens
        /// <paramref name="migrationDialog"/> to let the user confirm hub serials.
        /// Returns true when the file is at schema 2.0 (or doesn't exist).
        /// The caller must supply a factory for the migration dialog because
        /// this class has no UI dependency.
        /// </summary>
        public bool CheckAndMigrate(Func<List<PhidgetsHubInst>, Form> migrationDialogFactory)
        {
            if (!File.Exists(ConfigFile)) return true;
            try
            {
                string yaml = File.ReadAllText(ConfigFile);
                var md = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                var sc = md.Deserialize<ConfigSchemaCheck>(yaml);
                string schema = sc?.GeneralConfig?.Schema ?? "1.0";
                if (schema != "1.0") return true;

                RaiseInfo("Config schema 1.0 detected – migrating to 2.0...");
                var cv1 = md.Deserialize<ConfigV1>(yaml);

                var stubs = new List<PhidgetsHubInst>();
                if (cv1?.PhidgetsHubsInstances != null)
                    foreach (var name in cv1.PhidgetsHubsInstances)
                        if (!string.IsNullOrWhiteSpace(name))
                            stubs.Add(new PhidgetsHubInst { Name = name, Serial = 0, Enabled = true });

                using (var dlg = migrationDialogFactory(stubs))
                {
                    dlg.Text = "Migrate Hubs to Schema 2.0 – Confirm / Scan for Hubs";
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        var manageForm = (ManageHubsForm)dlg;
                        UpgradeToV2(yaml, cv1, manageForm.Hubs);
                        RaiseInfo("Config migrated to schema 2.0.");
                        return true;
                    }
                    RaiseInfo("Hub migration cancelled.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                RaiseError("Error checking config schema: " + ex.Message);
                return false;
            }
        }

        public void UpgradeToV2(string originalYaml, ConfigV1 configV1, List<PhidgetsHubInst> hubs)
        {
            try
            {
                File.WriteAllText(BackupFile, originalYaml);
                var newBlock = BuildHubsBlock(hubs);
                var lines = originalYaml.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var result = new StringBuilder();
                bool inHubs = false, written = false;

                foreach (var line in lines)
                {
                    string t = line.TrimStart();
                    if (t.StartsWith("Schema:"))
                    {
                        result.AppendLine(line.Substring(0, line.IndexOf("Schema:")) + "Schema: 2.0");
                        continue;
                    }
                    if (t.StartsWith("PhidgetsHubsInstances:"))
                    {
                        inHubs = true;
                        result.Append(newBlock);
                        written = true;
                        continue;
                    }
                    if (inHubs)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith(" ") || line.StartsWith("\t"))
                            continue;
                        inHubs = false;
                    }
                    if (!inHubs) result.AppendLine(line);
                }
                if (!written) result.Append(newBlock);
                File.WriteAllText(ConfigFile, result.ToString().TrimEnd() + Environment.NewLine);
                RaiseInfo("config.yaml upgraded to schema 2.0. Backup saved as config.yaml.v1.bak");
            }
            catch (Exception ex)
            {
                RaiseError("Error upgrading config to schema 2.0: " + ex.Message);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static string BuildHubsBlock(List<PhidgetsHubInst> hubs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PhidgetsHubsInstances:");
            foreach (var h in hubs)
            {
                sb.AppendLine("   - Name: "    + h.Name);
                sb.AppendLine("     Serial: "  + h.Serial);
                sb.AppendLine("     Enabled: " + h.Enabled.ToString().ToLower());
            }
            return sb.ToString();
        }

        private void RaiseInfo(string msg)  => InfoLog?.Invoke(msg);
        private void RaiseError(string msg) => ErrorLog?.Invoke(msg);
    }
}
