using System.Collections.Generic;
using FireReady.Core;
using FireReady.Simulation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using static FireReady.EditorTools.BuilderUtils;

namespace FireReady.EditorTools
{
    /// <summary>
    /// Puts each scenario in its own Addressables group and the assets they share
    /// (extinguishers, effect materials) in a shared group, so loading one scenario
    /// never pulls in another and shared assets are not duplicated per scenario.
    /// </summary>
    internal static class AddressablesSetup
    {
        const string MatDir = "Assets/_Project/Art/Materials";
        const string TexDir = "Assets/_Project/Art/Textures";

        public static void Configure(ScenarioCatalog catalog, ExtinguisherDefinition[] extinguishers)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            var prefabs = new Dictionary<string, string>
            {
                { "scenario/s1", ScenarioBuilder.S1Prefab },
                { "scenario/s2", ScenarioBuilder.S2Prefab },
                { "scenario/s3", ScenarioBuilder.S3Prefab },
            };
            var groupNames = new Dictionary<string, string>
            {
                { "scenario/s1", "Scenario S1 Office bin" },
                { "scenario/s2", "Scenario S2 Kitchen pan" },
                { "scenario/s3", "Scenario S3 Power strip" },
            };

            foreach (var def in catalog.scenarios)
            {
                if (!prefabs.TryGetValue(def.address, out var path))
                    continue;
                Add(settings, Group(settings, groupNames[def.address]), path, def.address);
            }

            var shared = Group(settings, "Shared Training Assets");
            foreach (var ext in extinguishers)
                Add(settings, shared, AssetDatabase.GetAssetPath(ext), $"shared/extinguisher/{ext.agent}");
            foreach (var mat in new[]
                     {
                         "FX_Flame", "FX_Smoke", "FX_Spray", "Extinguisher_Red", "Extinguisher_Black",
                         "Extinguisher_Pin", "Rack", "TrainingZone", "Band_Water", "Band_Foam", "Band_CO2",
                         "Band_DryPowder", "Band_WetChemical"
                     })
                Add(settings, shared, $"{MatDir}/{mat}.mat", $"shared/material/{mat}");
            Add(settings, shared, $"{TexDir}/SoftParticle.png", "shared/texture/SoftParticle");
            Add(settings, shared, $"{TexDir}/TrainingZone.png", "shared/texture/TrainingZone");

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            Log("Addressables groups configured");
        }

        static AddressableAssetGroup Group(AddressableAssetSettings settings, string name)
        {
            var group = settings.FindGroup(name);
            if (group == null)
                group = settings.CreateGroup(name, false, false, true, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            return group;
        }

        static void Add(AddressableAssetSettings settings, AddressableAssetGroup group, string path, string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                Log($"Addressables: missing asset {path}");
                return;
            }
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
        }
    }
}
