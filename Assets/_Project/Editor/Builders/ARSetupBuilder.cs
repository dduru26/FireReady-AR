using System;
using FireReady.App;
using FireReady.AR;
using FireReady.Simulation;
using FireReady.UI;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using static FireReady.EditorTools.BuilderUtils;

namespace FireReady.EditorTools
{
    /// <summary>
    /// Builds the Phase 2 AR setup: textures, materials, the reticle and placeholder
    /// scene prefabs, the systems object with the Bootstrapper, and the setup HUD.
    /// Safe to run again: it rebuilds what it owns and keeps everything else.
    /// </summary>
    public static class ARSetupBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string TexDir = "Assets/_Project/Art/Textures";
        const string MatDir = "Assets/_Project/Art/Materials";
        const string ReticleTex = TexDir + "/PlacementReticle.png";
        const string ZoneTex = TexDir + "/TrainingZone.png";
        const string RoundedSprite = "Assets/_Project/UI/Sprites/RoundedRect.png";
        const string PlaneFillMat = MatDir + "/ARPlaneFill.mat";
        const string ReticleMat = MatDir + "/PlacementReticle.mat";
        const string ZoneMat = MatDir + "/TrainingZone.mat";
        const string BinMat = MatDir + "/PlaceholderBin.mat";
        const string SettingsPath = "Assets/_Project/ScriptableObjects/PlacementSettings.asset";
        const string ReticlePrefab = "Assets/_Project/Prefabs/AR/PlacementReticle.prefab";
        const string ScenePrefab = "Assets/_Project/Prefabs/Scenario/TrainingScenePlaceholder.prefab";

        [MenuItem("Tools/FireReady/Build AR Setup")]
        public static void Build()
        {
            try
            {
                ImportTextures();
                var reticleMat = UnlitMaterial(ReticleMat, ReticleTex, Color.white);
                var zoneMat = UnlitMaterial(ZoneMat, ZoneTex, new Color(1f, 1f, 1f, 0.9f));
                var binMat = LitMaterial(BinMat, Hex(0x5B6470));
                var settings = LoadOrCreate(SettingsPath, () => ScriptableObject.CreateInstance<PlacementSettings>());
                var reticle = BuildReticlePrefab(reticleMat);
                var scene = BuildScenePrefab(zoneMat, binMat);
                AssetDatabase.SaveAssets();
                Log("AR setup assets ok");

                WireScene(settings, reticle, scene);
                Log("AR SETUP DONE");
            }
            catch (Exception e)
            {
                Log("AR SETUP FAILED: " + e);
            }
        }

        // ---- Assets ----------------------------------------------------------

        static void ImportTextures()
        {
            foreach (var path in new[] { ReticleTex, ZoneTex })
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType = TextureImporterType.Default;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }

            AssetDatabase.ImportAsset(RoundedSprite, ImportAssetOptions.ForceSynchronousImport);
            var si = (TextureImporter)AssetImporter.GetAtPath(RoundedSprite);
            si.textureType = TextureImporterType.Sprite;
            si.spriteImportMode = SpriteImportMode.Single;
            si.alphaIsTransparency = true;
            si.mipmapEnabled = false;
            si.wrapMode = TextureWrapMode.Clamp;
            si.spritePixelsPerUnit = 100;
            si.spriteBorder = new Vector4(64, 64, 64, 64);
            var s = new TextureImporterSettings();
            si.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            si.SetTextureSettings(s);
            si.SaveAndReimport();
        }

        static Material UnlitMaterial(string path, string texPath, Color color)
        {
            // Start from the plane fill material so the URP Unlit transparent setup carries over.
            var mat = LoadOrCreate(path, () => new Material(AssetDatabase.LoadAssetAtPath<Material>(PlaneFillMat)));
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetTextureScale("_BaseMap", Vector2.one);
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material LitMaterial(string path, Color color)
        {
            var mat = LoadOrCreate(path, () => new Material(Shader.Find("Universal Render Pipeline/Lit")));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.3f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject FlatQuad(string name, Transform parent, Material mat, float size, float height)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = new Vector3(0f, height, 0f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // face up
            quad.transform.localScale = Vector3.one * size;
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return quad;
        }

        static PlacementReticle BuildReticlePrefab(Material mat)
        {
            var root = new GameObject("PlacementReticle");
            var ring = FlatQuad("Ring", root.transform, mat, 0.3f, 0.005f);
            var reticle = root.AddComponent<PlacementReticle>();
            SetRef(reticle, "ringRenderer", ring.GetComponent<MeshRenderer>());
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ReticlePrefab);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<PlacementReticle>();
        }

        static GameObject BuildScenePrefab(Material zoneMat, Material binMat)
        {
            // Placeholder until the scenario prefabs load through Addressables:
            // a 1.6 m training zone with a bin at the far side (+Z faces the trainee).
            var root = new GameObject("Training Scene");
            FlatQuad("Training Zone", root.transform, zoneMat, 1.6f, 0.003f);

            var bin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bin.name = "Bin (placeholder)";
            bin.transform.SetParent(root.transform, false);
            bin.transform.localPosition = new Vector3(0f, 0.25f, -0.45f);
            bin.transform.localScale = new Vector3(0.36f, 0.25f, 0.36f);
            bin.GetComponent<MeshRenderer>().sharedMaterial = binMat;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ScenePrefab);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        // ---- Scene -----------------------------------------------------------

        static void WireScene(PlacementSettings settings, PlacementReticle reticle, GameObject scenePrefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var session = UnityEngine.Object.FindAnyObjectByType<ARSession>();
            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            if (session == null || origin == null)
                throw new InvalidOperationException("Main scene needs an AR Session and an XR Origin.");

            var planes = origin.GetComponent<ARPlaneManager>();
            var raycasts = origin.GetComponent<ARRaycastManager>();
            var anchors = GetOrAdd<ARAnchorManager>(origin.gameObject);
            var cam = origin.Camera;

            var overlay = origin.GetComponent<ARDebugOverlay>();
            if (overlay != null)
                SetRef(overlay, "session", session);

            var systems = GameObject.Find("Systems") ?? new GameObject("Systems");
            var bootstrapper = GetOrAdd<Bootstrapper>(systems);
            var simulation = GetOrAdd<SimulationManager>(systems);
            var placement = GetOrAdd<PlacementController>(systems);
            var planeVisibility = GetOrAdd<PlaneVisibilityController>(systems);
            var tracking = GetOrAdd<TrackingMonitor>(systems);
            var taps = GetOrAdd<ScreenTapDetector>(systems);

            SetRef(placement, "settings", settings);
            SetRef(placement, "reticlePrefab", reticle);
            SetRef(placement, "scenePrefab", scenePrefab);

            EnsureEventSystem();
            var hud = SetupHudBuilder.Build(AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSprite));

            SetRef(bootstrapper, "arSession", session);
            SetRef(bootstrapper, "raycastManager", raycasts);
            SetRef(bootstrapper, "planeManager", planes);
            SetRef(bootstrapper, "anchorManager", anchors);
            SetRef(bootstrapper, "arCamera", cam);
            SetRef(bootstrapper, "simulation", simulation);
            SetRef(bootstrapper, "placement", placement);
            SetRef(bootstrapper, "planeVisibility", planeVisibility);
            SetRef(bootstrapper, "trackingMonitor", tracking);
            SetRef(bootstrapper, "tapDetector", taps);
            SetRef(bootstrapper, "setupHud", hud);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("scene wired: Systems + Setup HUD");
        }

        static void EnsureEventSystem()
        {
            var es = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
                es = new GameObject("EventSystem").AddComponent<EventSystem>();

            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null)
                UnityEngine.Object.DestroyImmediate(legacy);

            var module = GetOrAdd<InputSystemUIInputModule>(es.gameObject);
            if (module.actionsAsset == null)
                module.AssignDefaultActions();
        }
    }
}
