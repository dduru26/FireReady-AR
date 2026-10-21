using System;
using FireReady.Audio;
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
        public const string S2Prefab = "Assets/_Project/Prefabs/Scenario/S2_KitchenPan.prefab";
        public const string S3Prefab = "Assets/_Project/Prefabs/Scenario/S3_PowerStrip.prefab";
        const string ScenarioDefDir = "Assets/_Project/ScriptableObjects/Scenarios";
        public const string CatalogPath = ScenarioDefDir + "/ScenarioCatalog.asset";

        static Material s_Flame, s_Smoke, s_Spray, s_Red, s_Black, s_Pin, s_Rack, s_Zone;
        static ExtinguisherDefinition[] s_Defs;
        static AudioCueSet s_Audio;
        public const string AudioCuesPath = "Assets/_Project/ScriptableObjects/Audio/AudioCueSet.asset";
        const string AudioDir = "Assets/_Project/Audio/Cues";

        public struct Result
        {
            public ScenarioCatalog Catalog;
            public PooledEffect Steam;
            public PooledEffect Splash;
        }

        public static Result Build()
        {
            var soft = ImportSoftTexture();
            var flameMat = ParticleFactory.Material(MatDir + "/FX_Flame.mat", soft, additive: true);
            var smokeMat = ParticleFactory.Material(MatDir + "/FX_Smoke.mat", soft, additive: false);
            var sprayMat = ParticleFactory.Material(MatDir + "/FX_Spray.mat", soft, additive: false);

            s_Defs = BuildDefinitions();
            var steam = BuildPuff("SteamPuff", sprayMat, rise: 0.25f);
            var splash = BuildPuff("SplashPuff", sprayMat, rise: -0.4f);

            s_Flame = flameMat;
            s_Smoke = smokeMat;
            s_Spray = sprayMat;
            s_Red = Lit(MatDir + "/Extinguisher_Red.mat", Hex(0xC8211B), 0.1f, 0.65f);
            s_Black = Lit(MatDir + "/Extinguisher_Black.mat", Hex(0x15181C), 0.2f, 0.5f);
            s_Pin = Lit(MatDir + "/Extinguisher_Pin.mat", Hex(0xF5C518), 0.6f, 0.6f);
            s_Rack = Lit(MatDir + "/Rack.mat", Hex(0x3A4048), 0.2f, 0.3f);
            s_Zone = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/TrainingZone.mat");

            s_Audio = BuildAudioCues();
            BuildS1();
            BuildS2();
            BuildS3();
            var catalog = BuildCatalog();
            AssetDatabase.SaveAssets();
            AddressablesSetup.Configure(catalog, s_Defs, s_Audio);
            return new Result { Catalog = catalog, Steam = steam, Splash = splash };
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

        static Transform NewScenario(string title, out GameObject root)
        {
            // +Z of the scene faces the trainee.
            root = new GameObject(title);
            var info = root.AddComponent<ScenarioInfo>();
            SetString(info, "title", title);

            if (s_Zone != null)
            {
                var zone = GameObject.CreatePrimitive(PrimitiveType.Quad);
                zone.name = "Training Zone";
                UnityEngine.Object.DestroyImmediate(zone.GetComponent<Collider>());
                zone.transform.SetParent(root.transform, false);
                zone.transform.localPosition = new Vector3(0f, 0.003f, 0f);
                zone.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                zone.transform.localScale = Vector3.one * 2f;
                var zr = zone.GetComponent<MeshRenderer>();
                zr.sharedMaterial = s_Zone;
                zr.shadowCastingMode = ShadowCastingMode.Off;
            }

            // Rack on the trainee's right, in front of the hazard.
            var rack = new GameObject("Extinguisher Rack").transform;
            rack.SetParent(root.transform, false);
            rack.localPosition = new Vector3(-0.45f, 0f, 0.05f);
            Primitive(PrimitiveType.Cube, "Rack Base", rack, new Vector3(0f, 0.01f, 0f), new Vector3(0.95f, 0.02f, 0.26f), s_Rack, keepCollider: false);
            for (int i = 0; i < s_Defs.Length; i++)
            {
                float x = (i - (s_Defs.Length - 1) * 0.5f) * 0.18f;
                BuildExtinguisher(s_Defs[i], rack, new Vector3(x, 0.02f, 0f), s_Red, s_Black, s_Pin, s_Spray);
            }

            var hazard = new GameObject("Hazard").transform;
            hazard.SetParent(root.transform, false);
            hazard.localPosition = new Vector3(0.3f, 0f, -0.6f);
            return hazard;
        }

        static T AddFire<T>(Transform parent, float baseHeight, float baseRadius, float startIntensity, float growth)
            where T : FireHazard
        {
            var fireGo = new GameObject($"Fire ({typeof(T).Name})");
            fireGo.transform.SetParent(parent, false);
            var flameBase = new GameObject("Flame Base").transform;
            flameBase.SetParent(fireGo.transform, false);
            flameBase.localPosition = new Vector3(0f, baseHeight, 0f);

            var flames = ParticleFactory.Create("Flames", flameBase, Vector3.zero, s_Flame);
            ParticleFactory.Flames(flames);
            var smoke = ParticleFactory.Create("Smoke", flameBase, new Vector3(0f, 0.25f, 0f), s_Smoke);
            ParticleFactory.Smoke(smoke);
            var embers = ParticleFactory.Create("Embers", flameBase, Vector3.zero, s_Flame);
            ParticleFactory.Embers(embers);
            var core = ParticleFactory.Create("Core", flameBase, Vector3.zero, s_Flame);
            ParticleFactory.Core(core);

            var lightGo = new GameObject("Firelight");
            lightGo.transform.SetParent(flameBase, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.2f);
            light.intensity = 2f;
            light.range = 2f;
            light.shadows = LightShadows.None;

            var visuals = fireGo.AddComponent<FireVisuals>();
            SetRef(visuals, "core", core);
            SetRef(visuals, "fireLight", light);
            SetRef(visuals, "flames", flames);
            SetRef(visuals, "smoke", smoke);
            SetRef(visuals, "embers", embers);

            var fire = fireGo.AddComponent<T>();
            SetRef(fire, "flameBase", flameBase);
            SetRef(fire, "visuals", visuals);
            var so = new SerializedObject(fire);
            so.FindProperty("baseRadius").floatValue = baseRadius;
            so.FindProperty("startIntensity").floatValue = startIntensity;
            so.FindProperty("baseGrowthPerSecond").floatValue = growth;
            so.ApplyModifiedPropertiesWithoutUndo();
            return fire;
        }

        static AudioCueSet BuildAudioCues()
        {
            AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{name}.wav");
            var set = LoadOrCreate(AudioCuesPath, () => ScriptableObject.CreateInstance<AudioCueSet>());
            set.fireLoop = Clip("FireLoop");
            set.sprayLoop = Clip("SprayLoop");
            set.alarmLoop = Clip("AlarmLoop");
            set.warning = Clip("Warning");
            set.success = Clip("Success");
            set.flareUp = Clip("FlareUp");
            set.pinClick = Clip("PinClick");
            set.pickUp = Clip("PickUp");
            set.shock = Clip("Shock");
            EditorUtility.SetDirty(set);
            return set;
        }

        static void Save(GameObject root, string path)
        {
            // The scenario carries its own sounds, so they load and unload with it.
            var fire = root.GetComponentInChildren<FireHazard>();
            var audio = root.AddComponent<ScenarioAudio>();
            SetRef(audio, "cues", s_Audio);
            if (fire != null)
                SetRef(audio, "fireSource", fire.transform.Find("Flame Base"));

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        static void BuildS1()
        {
            var metal = Lit(MatDir + "/Bin_Metal.mat", Hex(0x2B3036), 0.55f, 0.6f);
            var hazard = NewScenario("S1 Office waste bin", out var root);
            Primitive(PrimitiveType.Cylinder, "Bin", hazard, new Vector3(0f, 0.2f, 0f), new Vector3(0.32f, 0.2f, 0.32f), metal);
            Primitive(PrimitiveType.Cylinder, "Rim", hazard, new Vector3(0f, 0.4f, 0f), new Vector3(0.34f, 0.008f, 0.34f), s_Black);
            AddFire<ClassAFire>(hazard, 0.38f, 0.18f, 30f, 0.6f);
            Save(root, S1Prefab);
        }

        static void BuildS2()
        {
            var cabinet = Lit(MatDir + "/Kitchen_Cabinet.mat", Hex(0xD9D6D0), 0.0f, 0.4f);
            var hob = Lit(MatDir + "/Kitchen_Hob.mat", Hex(0x111315), 0.3f, 0.8f);
            var pan = Lit(MatDir + "/Kitchen_Pan.mat", Hex(0x3A3D42), 0.7f, 0.6f);
            var oil = Lit(MatDir + "/Kitchen_Oil.mat", Hex(0xC98A1B), 0.0f, 0.9f);

            var hazard = NewScenario("S2 Kitchen pan", out var root);
            Primitive(PrimitiveType.Cube, "Cooker", hazard, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.55f), cabinet);
            Primitive(PrimitiveType.Cube, "Hob", hazard, new Vector3(0f, 0.905f, 0f), new Vector3(0.56f, 0.01f, 0.5f), hob, keepCollider: false);
            Primitive(PrimitiveType.Cylinder, "Pan", hazard, new Vector3(0f, 0.935f, 0f), new Vector3(0.3f, 0.025f, 0.3f), pan, keepCollider: false);
            Primitive(PrimitiveType.Cube, "Pan Handle", hazard, new Vector3(0.24f, 0.945f, 0f), new Vector3(0.2f, 0.02f, 0.035f), s_Black, keepCollider: false);
            Primitive(PrimitiveType.Cylinder, "Oil", hazard, new Vector3(0f, 0.957f, 0f), new Vector3(0.27f, 0.004f, 0.27f), oil, keepCollider: false);
            AddFire<CookingOilFire>(hazard, 0.96f, 0.15f, 35f, 0.8f);
            Save(root, S2Prefab);
        }

        static void BuildS3()
        {
            var wood = Lit(MatDir + "/Desk_Wood.mat", Hex(0x8A6A4A), 0.0f, 0.35f);
            var plastic = Lit(MatDir + "/Plastic_White.mat", Hex(0xEDEDED), 0.0f, 0.5f);
            var grey = Lit(MatDir + "/Wall_Grey.mat", Hex(0x9AA0A6), 0.0f, 0.3f);
            var switchRed = Lit(MatDir + "/Switch_Red.mat", Hex(0xE53935), 0.0f, 0.6f);

            var hazard = NewScenario("S3 Power strip", out var root);
            Primitive(PrimitiveType.Cube, "Desk", hazard, new Vector3(0f, 0.37f, 0f), new Vector3(1.0f, 0.74f, 0.5f), wood);
            Primitive(PrimitiveType.Cube, "Power Strip", hazard, new Vector3(0f, 0.76f, 0f), new Vector3(0.32f, 0.04f, 0.07f), plastic, keepCollider: false);
            Primitive(PrimitiveType.Cube, "Plug 1", hazard, new Vector3(-0.08f, 0.79f, 0f), new Vector3(0.05f, 0.04f, 0.05f), s_Black, keepCollider: false);
            Primitive(PrimitiveType.Cube, "Plug 2", hazard, new Vector3(0.04f, 0.79f, 0f), new Vector3(0.05f, 0.04f, 0.05f), s_Black, keepCollider: false);
            Primitive(PrimitiveType.Cube, "Cable", hazard, new Vector3(0.36f, 0.76f, -0.1f), new Vector3(0.4f, 0.012f, 0.012f), s_Black, keepCollider: false);
            var fire = AddFire<ElectricalFire>(hazard, 0.79f, 0.16f, 28f, 0.6f);

            // Wall switch: tap it to cut the power before tackling the fire.
            var wall = new GameObject("Wall Switch").transform;
            wall.SetParent(hazard, false);
            wall.localPosition = new Vector3(0.7f, 0f, -0.15f);
            Primitive(PrimitiveType.Cube, "Post", wall, new Vector3(0f, 0.45f, 0f), new Vector3(0.06f, 0.9f, 0.06f), grey, keepCollider: false);
            var panel = Primitive(PrimitiveType.Cube, "Panel", wall, new Vector3(0f, 0.95f, 0.04f), new Vector3(0.16f, 0.16f, 0.03f), plastic);
            panel.GetComponent<BoxCollider>().size = new Vector3(2f, 2f, 3f); // easier to tap
            var rocker = Primitive(PrimitiveType.Cube, "Rocker", panel.transform, new Vector3(0f, 0f, 0.8f), new Vector3(0.35f, 0.45f, 0.6f), switchRed, keepCollider: false);
            var power = panel.AddComponent<PowerSwitch>();
            SetRef(power, "fire", fire);
            SetRef(power, "rocker", rocker.transform);
            SetRef(power, "indicator", rocker.GetComponent<MeshRenderer>());

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(wall, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.12f, 0f);
            var label = labelGo.AddComponent<TextMeshPro>();
            label.text = "POWER\n<size=70%>tap to switch off</size>";
            label.fontSize = 0.5f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color32(0, 0, 0, 200);
            label.rectTransform.sizeDelta = new Vector2(0.7f, 0.2f);
            labelGo.AddComponent<FaceCamera>();

            Save(root, S3Prefab);
        }

        static ScenarioCatalog BuildCatalog()
        {
            var s1 = Scenario("S1_OfficeBin", 1, "Office waste bin", "Burning paper. The place to start.",
                "A waste-paper bin has caught fire beside a desk. It is small now, but it grows every second you wait.",
                FireClass.ClassA, "scenario/s1", "Preparing the office bin fire…",
                new[] { "Waste bin and extinguisher rack", "Fire, smoke and spray effects", "Sounds and captions" });
            var s2 = Scenario("S2_KitchenPan", 2, "Kitchen pan", "Burning cooking oil. Water makes it worse.",
                "A pan of cooking oil has caught fire on the hob. Water makes burning oil erupt, so choose your extinguisher carefully.",
                FireClass.ClassF, "scenario/s2", "Preparing the kitchen pan fire…",
                new[] { "Cooker, pan and extinguisher rack", "Fire, smoke and spray effects", "Sounds and captions" });
            var s3 = Scenario("S3_PowerStrip", 3, "Power strip", "Live electrical fire. Cut the power first.",
                "An overloaded power strip on a desk is on fire and still live. Cut the power at the wall before you tackle it.",
                FireClass.Electrical, "scenario/s3", "Preparing the power strip fire…",
                new[] { "Desk, power strip and wall switch", "Fire, smoke and spray effects", "Sounds and captions" });

            var catalog = LoadOrCreate(CatalogPath, () => ScriptableObject.CreateInstance<ScenarioCatalog>());
            catalog.scenarios.Clear();
            catalog.scenarios.Add(s1);
            catalog.scenarios.Add(s2);
            catalog.scenarios.Add(s3);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static ScenarioDefinition Scenario(string file, int number, string title, string tagline, string briefing,
            FireClass fireClass, string address, string loadingTitle, string[] items)
        {
            var def = LoadOrCreate($"{ScenarioDefDir}/{file}.asset", () => ScriptableObject.CreateInstance<ScenarioDefinition>());
            def.number = number;
            def.title = title;
            def.tagline = tagline;
            def.briefing = briefing;
            def.fireClass = fireClass;
            def.address = address;
            def.loadingTitle = loadingTitle;
            def.loadingItems = items;
            EditorUtility.SetDirty(def);
            return def;
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
