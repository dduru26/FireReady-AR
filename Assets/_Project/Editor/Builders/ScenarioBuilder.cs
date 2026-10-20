using System;
using FireReady.Core;
using FireReady.Simulation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static FireReady.EditorTools.BuilderUtils;

namespace FireReady.EditorTools
{
    /// <summary>
    /// Builds the S1 "Office waste bin" scenario prefab: training zone, bin with a
    /// Class A fire, and a rack with all five extinguisher types. Also creates the
    /// extinguisher definitions and the pooled impact effects.
    /// </summary>
    internal static class ScenarioBuilder
    {
        const string TexDir = "Assets/_Project/Art/Textures";
        const string MatDir = "Assets/_Project/Art/Materials";
        const string DefDir = "Assets/_Project/ScriptableObjects/Extinguishers";
        const string EffectsDir = "Assets/_Project/Prefabs/Effects";
        public const string S1Prefab = "Assets/_Project/Prefabs/Scenario/S1_OfficeBin.prefab";

        public struct Result
        {
            public GameObject Scenario;
            public PooledEffect Steam;
            public PooledEffect Splash;
        }

        public static Result Build()
        {
            var soft = ImportSoftTexture();
            var flameMat = ParticleFactory.Material(MatDir + "/FX_Flame.mat", soft, additive: true);
            var smokeMat = ParticleFactory.Material(MatDir + "/FX_Smoke.mat", soft, additive: false);
            var sprayMat = ParticleFactory.Material(MatDir + "/FX_Spray.mat", soft, additive: false);

            var defs = BuildDefinitions();
            var steam = BuildPuff("SteamPuff", sprayMat, rise: 0.25f);
            var splash = BuildPuff("SplashPuff", sprayMat, rise: -0.4f);

            var scenario = BuildS1(defs, flameMat, smokeMat, sprayMat);
            AssetDatabase.SaveAssets();
            return new Result { Scenario = scenario, Steam = steam, Splash = splash };
        }

        static Texture2D ImportSoftTexture()
        {
            var path = TexDir + "/SoftParticle.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Default;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---- Extinguisher data ------------------------------------------------------

        static ExtinguisherDefinition[] BuildDefinitions()
        {
            return new[]
            {
                Def(AgentType.Water, "Water", Hex(0xD7261E), 12f, new Color(0.75f, 0.88f, 1f, 0.75f), new Color(1f, 1f, 1f, 0.55f)),
                Def(AgentType.Foam, "Foam", Hex(0xF2E6C9), 12f, new Color(1f, 0.98f, 0.92f, 0.85f), new Color(1f, 1f, 0.95f, 0.7f)),
                Def(AgentType.CO2, "CO2", Hex(0x1A1A1A), 8f, new Color(1f, 1f, 1f, 0.5f), new Color(1f, 1f, 1f, 0.5f)),
                Def(AgentType.DryPowder, "Dry powder", Hex(0x1F5FBF), 9f, new Color(0.95f, 0.95f, 1f, 0.8f), new Color(0.95f, 0.95f, 1f, 0.7f)),
                Def(AgentType.WetChemical, "Wet chemical", Hex(0xF5C518), 10f, new Color(1f, 1f, 0.9f, 0.8f), new Color(1f, 1f, 0.9f, 0.65f)),
            };
        }

        static ExtinguisherDefinition Def(AgentType agent, string name, Color band, float capacity, Color spray, Color impact)
        {
            var path = $"{DefDir}/{agent}.asset";
            var def = LoadOrCreate(path, () => ScriptableObject.CreateInstance<ExtinguisherDefinition>());
            def.agent = agent;
            def.displayName = name;
            def.bandColor = band;
            def.capacitySeconds = capacity;
            def.sprayColor = spray;
            def.impactColor = impact;
            EditorUtility.SetDirty(def);
            return def;
        }

        // ---- Effects -------------------------------------------------------------------

        static PooledEffect BuildPuff(string name, Material mat, float rise)
        {
            EnsureFolder(EffectsDir);
            var root = new GameObject(name);
            var ps = root.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = root.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            ParticleFactory.Puff(ps, rise);
            root.AddComponent<PooledEffect>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{EffectsDir}/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<PooledEffect>();
        }

        // ---- Scenario --------------------------------------------------------------------

        static GameObject BuildS1(ExtinguisherDefinition[] defs, Material flameMat, Material smokeMat, Material sprayMat)
        {
            var zoneMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/TrainingZone.mat");
            var metal = Lit(MatDir + "/Bin_Metal.mat", Hex(0x2B3036), 0.55f, 0.6f);
            var black = Lit(MatDir + "/Extinguisher_Black.mat", Hex(0x15181C), 0.2f, 0.5f);
            var red = Lit(MatDir + "/Extinguisher_Red.mat", Hex(0xC8211B), 0.1f, 0.65f);
            var pinMat = Lit(MatDir + "/Extinguisher_Pin.mat", Hex(0xF5C518), 0.6f, 0.6f);
            var rackMat = Lit(MatDir + "/Rack.mat", Hex(0x3A4048), 0.2f, 0.3f);

            // +Z of the scene faces the trainee.
            var root = new GameObject("S1 Office waste bin");
            root.AddComponent<ScenarioInfo>();
            SetString(root.GetComponent<ScenarioInfo>(), "title", "Office waste bin");

            if (zoneMat != null)
            {
                var zone = GameObject.CreatePrimitive(PrimitiveType.Quad);
                zone.name = "Training Zone";
                UnityEngine.Object.DestroyImmediate(zone.GetComponent<Collider>());
                zone.transform.SetParent(root.transform, false);
                zone.transform.localPosition = new Vector3(0f, 0.003f, 0f);
                zone.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                zone.transform.localScale = Vector3.one * 2f;
                var zr = zone.GetComponent<MeshRenderer>();
                zr.sharedMaterial = zoneMat;
                zr.shadowCastingMode = ShadowCastingMode.Off;
            }

            // Bin with the fire on top.
            var binRoot = new GameObject("Waste Bin").transform;
            binRoot.SetParent(root.transform, false);
            binRoot.localPosition = new Vector3(0.3f, 0f, -0.55f);
            var bin = Primitive(PrimitiveType.Cylinder, "Bin", binRoot, new Vector3(0f, 0.2f, 0f), new Vector3(0.32f, 0.2f, 0.32f), metal);
            Primitive(PrimitiveType.Cylinder, "Rim", binRoot, new Vector3(0f, 0.4f, 0f), new Vector3(0.34f, 0.008f, 0.34f), black);

            var fireGo = new GameObject("Fire (Class A)");
            fireGo.transform.SetParent(binRoot, false);
            var flameBase = new GameObject("Flame Base").transform;
            flameBase.SetParent(fireGo.transform, false);
            flameBase.localPosition = new Vector3(0f, 0.38f, 0f);

            var flames = ParticleFactory.Create("Flames", flameBase, Vector3.zero, flameMat);
            ParticleFactory.Flames(flames);
            var smoke = ParticleFactory.Create("Smoke", flameBase, new Vector3(0f, 0.25f, 0f), smokeMat);
            ParticleFactory.Smoke(smoke);
            var embers = ParticleFactory.Create("Embers", flameBase, Vector3.zero, flameMat);
            ParticleFactory.Embers(embers);

            var visuals = fireGo.AddComponent<FireVisuals>();
            SetRef(visuals, "flames", flames);
            SetRef(visuals, "smoke", smoke);
            SetRef(visuals, "embers", embers);

            var fire = fireGo.AddComponent<ClassAFire>();
            SetRef(fire, "flameBase", flameBase);
            SetRef(fire, "visuals", visuals);

            // Rack on the trainee's right, in front of the bin.
            var rack = new GameObject("Extinguisher Rack").transform;
            rack.SetParent(root.transform, false);
            rack.localPosition = new Vector3(-0.45f, 0f, 0.05f);
            Primitive(PrimitiveType.Cube, "Rack Base", rack, new Vector3(0f, 0.01f, 0f), new Vector3(0.95f, 0.02f, 0.26f), rackMat, keepCollider: false);

            for (int i = 0; i < defs.Length; i++)
            {
                float x = (i - (defs.Length - 1) * 0.5f) * 0.18f;
                BuildExtinguisher(defs[i], rack, new Vector3(x, 0.02f, 0f), red, black, pinMat, sprayMat);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, S1Prefab);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        static void BuildExtinguisher(ExtinguisherDefinition def, Transform parent, Vector3 position,
            Material red, Material black, Material pinMat, Material sprayMat)
        {
            var bandMat = Lit($"{MatDir}/Band_{def.agent}.mat", def.bandColor, 0.1f, 0.55f);

            var root = new GameObject($"Extinguisher - {def.displayName}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            Primitive(PrimitiveType.Cylinder, "Body", root.transform, new Vector3(0f, 0.17f, 0f), new Vector3(0.11f, 0.17f, 0.11f), red, keepCollider: false);
            Primitive(PrimitiveType.Cylinder, "Band", root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.114f, 0.035f, 0.114f), bandMat, keepCollider: false);
            Primitive(PrimitiveType.Cylinder, "Head", root.transform, new Vector3(0f, 0.355f, 0f), new Vector3(0.05f, 0.02f, 0.05f), black, keepCollider: false);
            Primitive(PrimitiveType.Cube, "Handle", root.transform, new Vector3(0f, 0.385f, 0.02f), new Vector3(0.02f, 0.012f, 0.09f), black, keepCollider: false);
            var pin = Primitive(PrimitiveType.Cube, "Pin", root.transform, new Vector3(0.028f, 0.372f, 0f), new Vector3(0.045f, 0.012f, 0.012f), pinMat, keepCollider: false);
            Primitive(PrimitiveType.Cube, "Hose", root.transform, new Vector3(0f, 0.36f, 0.07f), new Vector3(0.016f, 0.016f, 0.1f), black, keepCollider: false);

            var nozzle = new GameObject("Nozzle").transform;
            nozzle.SetParent(root.transform, false);
            nozzle.localPosition = new Vector3(0f, 0.36f, 0.125f);
            var spray = ParticleFactory.Create("Spray", nozzle, Vector3.zero, sprayMat);
            ParticleFactory.Spray(spray, def.sprayColor, def.agent == AgentType.CO2 ? 0f : 0.25f);

            // Label above the extinguisher so the type can be read from a distance.
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.47f, 0f);
            var label = labelGo.AddComponent<TextMeshPro>();
            label.text = def.displayName.ToUpperInvariant();
            label.fontSize = 0.45f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color32(0, 0, 0, 200);
            label.rectTransform.sizeDelta = new Vector2(0.5f, 0.1f);
            labelGo.AddComponent<FaceCamera>();

            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.22f, 0f);
            collider.height = 0.5f;
            collider.radius = 0.08f;

            var item = root.AddComponent<ExtinguisherItem>();
            SetRef(item, "definition", def);
            SetRef(item, "pin", pin);
            SetRef(item, "spray", spray);
            SetRef(item, "nozzle", nozzle);
        }

        // ---- Helpers ---------------------------------------------------------------------

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
            Material mat, bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!keepCollider)
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static Material Lit(string path, Color color, float metallic, float smoothness)
        {
            var mat = LoadOrCreate(path, () => new Material(Shader.Find("Universal Render Pipeline/Lit")));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void SetString(UnityEngine.Object target, string property, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(property) ?? throw new InvalidOperationException($"No field '{property}'");
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
