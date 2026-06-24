using IPA.Loader;
using ScoreSaber.Live.V1;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScoreSaber.Features.Live.Ludus.Services {
    internal static class LudusInstalledMods {
        internal static List<LiveMod> List() {
            try {
                return PluginManager.EnabledPlugins
                    .Select(plugin => Normalize(plugin.Id))
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .OrderBy(id => id)
                    .Select(id => new LiveMod { Id = id })
                    .ToList();
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to collect installed mods for Ludus: {ex.Message}");
                return new List<LiveMod>();
            }
        }

        private static string Normalize(string value) {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
