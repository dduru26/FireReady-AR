// FireReady AR - one-time project setup, stage B.
// Compiles only after AR Foundation, ARKit and Addressables are installed
// (see defineConstraints in the asmdef). Configures XR, player settings,
// URP, Addressables and builds the Main AR scene. Safe to run more than once:
// every step checks before it changes anything.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace FireReady.EditorSetup
{
    [InitializeOnLoad]
    public static class FireReadySetupStageB
    {
        const string DoneMarker = "ProjectSettings/FireReadySetup.done";
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Main.unity";
        const string PlanePrefabPath = Root + "/Prefabs/AR/AR Default Plane.prefab";

        static FireReadySetupStageB()
        {
            if (!File.Exists(DoneMarker)) EditorApplication.delayCall += Run;
        }

        [MenuItem("Tools/FireReady/Run Project Setup")]
        public static void Run()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run;
                return;
            }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                Log("Waiting for the iOS platform switch");
                return;
            }

            Log("Stage B started");
            int fails = 0;
            fails += Step("Folders", Folders);
            fails += Step("Player settings", PlayerSettingsStep);
            fails += Step("XR loaders (ARKit on iOS, XR Simulation in editor)", XrLoaders);
            fails += Step("ARKit requirement", ArkitRequired);
            fails += Step("URP AR Background Renderer Feature", UrpFeature);
            fails += Step("Addressables settings", AddressablesStep);
            fails += Step("TextMeshPro essential resources", TmpStep);
            fails += Step("Template cleanup", Cleanup);
            fails += Step("Main AR scene", SceneStep);
            AssetDatabase.SaveAssets();

            if (fails == 0)
            {
                File.WriteAllText(DoneMarker, DateTime.Now.ToString("u"));
                Log("SETUP COMPLETE");
            }
            else
            {
                Log("Setup finished with " + fails + " failed step(s). Fix them, then run Tools > FireReady > Run Project Setup.");
            }
        }

        [MenuItem("Tools/FireReady/Build iOS Xcode Project")]
        public static void BuildIOS()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/iOS",
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.Development
            };
            Log("iOS build started");
            var report = BuildPipeline.BuildPlayer(options);
            Log("iOS build finished: " + report.summary.result + ", errors " + report.summary.totalErrors +
                ", size " + (report.summary.totalSize / (1024 * 1024)) + " MB, time " + report.summary.totalTime);
        }

        [MenuItem("Tools/FireReady/Open Xcode Project")]
        public static void OpenXcode()
        {
            string proj = Path.GetFullPath("Builds/iOS/Unity-iPhone.xcodeproj");
            if (!Directory.Exists(proj)) { Log("Xcode project not found; build first"); return; }
            System.Diagnostics.Process.Start("open", "\"" + proj + "\"");
            Log("Opened " + proj);
        }

        // ---------------------------------------------------------------- helpers
        static int Step(string name, Action action)
        {
            try { action(); Log("OK   " + name); return 0; }
            catch (Exception e) { Log("FAIL " + name + ": " + e.Message + "\n" + e.StackTrace); return 1; }
        }

        static void Log(string message)
        {
            Directory.CreateDirectory("Logs");
            File.AppendAllText("Logs/FireReadySetup.log", DateTime.Now.ToString("HH:mm:ss") + " B " + message + "\n");
            Debug.Log("[FireReadySetup] " + message);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void Menu(string path)
        {
            if (!EditorApplication.ExecuteMenuItem(path)) throw new Exception("Menu item not found: " + path);
        }

        static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName, false);
                if (t != null) return t;
            }
            return null;
        }

        // ---------------------------------------------------------------- steps
        static void Folders()
        {
            string[] folders =
            {
                "Scenes", "Scripts/Core", "Scripts/AR", "Scripts/Simulation", "Scripts/Training",
                "Scripts/Data", "Scripts/UI", "Prefabs/AR", "Prefabs/Scenario", "ScriptableObjects",
                "Art/Materials", "Art/Models", "Art/VFX", "Audio", "UI"
            };
            foreach (var f in folders)
            {
                string p = Root + "/" + f;
                EnsureFolder(p);
                string keep = p + "/.gitkeep"; // hidden from Unity, keeps empty folders in Git
                if (!File.Exists(keep)) File.WriteAllText(keep, "");
            }
        }

        static void PlayerSettingsStep()
        {
            PlayerSettings.companyName = "Duru Donald Onyebuchi";
            PlayerSettings.productName = "FireReady AR";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.dduru26.fireready");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.cameraUsageDescription = "FireReady AR uses the camera to place the training scene on your floor.";
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        }

        static void XrLoaders()
        {
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
            {
                EnsureFolder("Assets/XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }
            AssignLoader(perTarget, BuildTargetGroup.iOS, "UnityEngine.XR.ARKit.ARKitLoader");
            AssignLoader(perTarget, BuildTargetGroup.Standalone, "UnityEngine.XR.Simulation.SimulationLoader");
            EditorUtility.SetDirty(perTarget);
        }

        static void AssignLoader(XRGeneralSettingsPerBuildTarget perTarget, BuildTargetGroup group, string loaderType)
        {
            if (!perTarget.HasSettingsForBuildTarget(group))
            {
                var general = ScriptableObject.CreateInstance<XRGeneralSettings>();
                general.name = group + " Settings";
                var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = group + " Providers";
                general.Manager = manager;
                AssetDatabase.AddObjectToAsset(general, perTarget);
                AssetDatabase.AddObjectToAsset(manager, perTarget);
                perTarget.SetSettingsForBuildTarget(group, general);
            }
            var settings = perTarget.SettingsForBuildTarget(group);
            settings.InitManagerOnStart = true;
            if (!XRPackageMetadataStore.IsLoaderAssigned(loaderType, group))
            {
                if (!XRPackageMetadataStore.AssignLoader(settings.Manager, loaderType, group))
                    throw new Exception("Could not assign " + loaderType + " for " + group);
            }
            Log("  " + group + ": " + loaderType + " assigned");
        }

        static void ArkitRequired()
        {
            // Done by reflection so a renamed API can't stop the whole setup from compiling.
            var t = FindType("UnityEditor.XR.ARKit.ARKitSettings");
            if (t == null) { Log("  ARKitSettings type not found; default requirement kept"); return; }
            object settings = t.GetMethod("GetOrCreateSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null)
                              ?? t.GetProperty("currentSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            var prop = t.GetProperty("requirement", BindingFlags.Public | BindingFlags.Instance);
            if (settings == null || prop == null) { Log("  ARKit settings not reachable; default requirement kept"); return; }
            prop.SetValue(settings, Enum.Parse(prop.PropertyType, "Required"));
            if (settings is UnityEngine.Object o) EditorUtility.SetDirty(o);
        }

        static void UrpFeature()
        {
            var featureType = TypeCache.GetTypesDerivedFrom<ScriptableRendererFeature>()
                .FirstOrDefault(t => t.Name == "ARBackgroundRendererFeature");
            if (featureType == null) throw new Exception("ARBackgroundRendererFeature type not found");

            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (data == null) continue;
                if (data.rendererFeatures.Any(f => f != null && f.GetType() == featureType)) { Log("  already on " + path); continue; }

                var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(featureType);
                feature.name = featureType.Name;
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

                var so = new SerializedObject(data);
                var list = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(data);
                Log("  added to " + path);
            }
        }

        static void AddressablesStep()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
            EditorUtility.SetDirty(settings);
        }

        static void TmpStep()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) { Log("  already imported"); return; }
            var t = FindType("TMPro.TMP_PackageResourceImporter");
            var m = t?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .FirstOrDefault(x => x.Name == "ImportResources" && x.GetParameters().Length == 3);
            if (m == null) { Log("  importer not found; use Window > TextMeshPro > Import TMP Essential Resources"); return; }
            m.Invoke(null, new object[] { true, false, false });
            Log("  import started");
        }

        static void Cleanup()
        {
            if (AssetDatabase.IsValidFolder("Assets/TutorialInfo")) AssetDatabase.DeleteAsset("Assets/TutorialInfo");
            if (File.Exists("Assets/Readme.asset")) AssetDatabase.DeleteAsset("Assets/Readme.asset");
        }

        static void SceneStep()
        {
            if (!File.Exists(ScenePath))
            {
                if (File.Exists("Assets/Scenes/SampleScene.unity"))
                {
                    string err = AssetDatabase.MoveAsset("Assets/Scenes/SampleScene.unity", ScenePath);
                    if (!string.IsNullOrEmpty(err)) throw new Exception(err);
                }
                else
                {
                    var fresh = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    EditorSceneManager.SaveScene(fresh, ScenePath);
                }
            }
            if (AssetDatabase.IsValidFolder("Assets/Scenes") && AssetDatabase.FindAssets("", new[] { "Assets/Scenes" }).Length == 0)
                AssetDatabase.DeleteAsset("Assets/Scenes");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            if (UnityEngine.Object.FindFirstObjectByType<XROrigin>() == null)
            {
                foreach (var root in scene.GetRootGameObjects())
                    if (root.name == "Main Camera" || root.name == "Global Volume")
                        UnityEngine.Object.DestroyImmediate(root);
                Selection.activeObject = null;
                Menu("GameObject/XR/XR Origin (Mobile AR)");
            }
            if (UnityEngine.Object.FindFirstObjectByType<ARSession>() == null)
            {
                Selection.activeObject = null;
                Menu("GameObject/XR/AR Session");
            }

            var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (origin == null) throw new Exception("XR Origin was not created");
            var go = origin.gameObject;
            go.transform.SetParent(null);

            var planes = go.GetComponent<ARPlaneManager>();
            if (planes == null) planes = go.AddComponent<ARPlaneManager>();
            if (go.GetComponent<ARRaycastManager>() == null) go.AddComponent<ARRaycastManager>();
            if (go.GetComponent<ARAnchorManager>() == null) go.AddComponent<ARAnchorManager>();
            planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            planes.planePrefab = GetPlanePrefab();

            var cam = origin.Camera;
            if (cam != null)
            {
                var camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData != null) camData.renderPostProcessing = false;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Log("  scene roots: " + string.Join(", ", scene.GetRootGameObjects().Select(r => r.name)));
        }

        static GameObject GetPlanePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefabPath);
            if (existing != null) return existing;

            Selection.activeObject = null;
            Menu("GameObject/XR/AR Default Plane");
            var instance = Selection.activeGameObject;
            if (instance == null || instance.GetComponent<ARPlane>() == null)
            {
                var plane = UnityEngine.Object.FindFirstObjectByType<ARPlane>();
                instance = plane != null ? plane.gameObject : null;
            }
            if (instance == null) throw new Exception("AR Default Plane was not created");

            var fill = MakeTransparentMaterial("ARPlaneFill", new Color(1f, 1f, 1f, 0.18f));
            var edge = MakeTransparentMaterial("ARPlaneEdge", new Color(1f, 1f, 1f, 0.85f));
            var meshRenderer = instance.GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.sharedMaterial = fill;
            var line = instance.GetComponent<LineRenderer>();
            if (line != null) line.sharedMaterial = edge;

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PlanePrefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        static Material MakeTransparentMaterial(string name, Color color)
        {
            string path = Root + "/Art/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new Exception("URP Unlit shader not found");
            var m = new Material(shader) { name = name };
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
