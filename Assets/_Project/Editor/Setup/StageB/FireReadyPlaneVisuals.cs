using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FireReady.EditorSetup
{
    /// <summary>
    /// Makes detected planes easy to see (dotted fill + solid edge) and adds the
    /// development AR readout to the main scene.
    /// </summary>
    public static class FireReadyPlaneVisuals
    {
        const string TexPath = "Assets/_Project/Art/Textures/ARPlaneDots.png";
        const string FillMat = "Assets/_Project/Art/Materials/ARPlaneFill.mat";
        const string EdgeMat = "Assets/_Project/Art/Materials/ARPlaneEdge.mat";
        const string PlanePrefab = "Assets/_Project/Prefabs/AR/AR Default Plane.prefab";
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string LogPath = "Logs/FireReadySetup.log";

        [MenuItem("Tools/FireReady/Upgrade Plane Visuals")]
        public static void Run()
        {
            try
            {
                SetupTexture();
                SetupMaterials();
                SetupPrefab();
                SetupScene();
                AssetDatabase.SaveAssets();
                Log("PLANE VISUALS DONE");
            }
            catch (Exception e)
            {
                Log("PLANE VISUALS FAILED: " + e);
            }
        }

        static void SetupTexture()
        {
            AssetDatabase.ImportAsset(TexPath, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(TexPath);
            ti.textureType = TextureImporterType.Default;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Trilinear;
            ti.mipmapEnabled = true;
            ti.sRGBTexture = true;
            ti.SaveAndReimport();
            Log("texture ok");
        }

        static void SetupMaterials()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
            var fill = AssetDatabase.LoadAssetAtPath<Material>(FillMat);
            // Plane mesh UVs are in metres, so a scale of 8 gives a dot every 12.5 cm.
            fill.SetTexture("_BaseMap", tex);
            fill.SetTextureScale("_BaseMap", new Vector2(8f, 8f));
            fill.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.75f));
            EditorUtility.SetDirty(fill);

            var edge = AssetDatabase.LoadAssetAtPath<Material>(EdgeMat);
            edge.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
            EditorUtility.SetDirty(edge);
            Log("materials ok");
        }

        static void SetupPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PlanePrefab);
            var line = root.GetComponent<LineRenderer>();
            if (line != null)
            {
                line.widthMultiplier = 1f;
                line.startWidth = 0.012f;
                line.endWidth = 0.012f;
                line.useWorldSpace = false;
            }
            var mr = root.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            PrefabUtility.SaveAsPrefabAsset(root, PlanePrefab);
            PrefabUtility.UnloadPrefabContents(root);
            Log("prefab ok");
        }

        static void SetupScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var pm = UnityEngine.Object.FindAnyObjectByType<ARPlaneManager>();
            if (pm == null) { Log("no ARPlaneManager in scene"); return; }
            pm.enabled = true;

            var overlayType = Type.GetType("FireReady.AR.ARDebugOverlay, FireReady.AR");
            if (overlayType == null) { Log("ARDebugOverlay type not compiled yet"); return; }
            Component overlay = pm.GetComponent(overlayType);
            if (overlay == null) overlay = pm.gameObject.AddComponent(overlayType);
            var so = new SerializedObject(overlay);
            so.FindProperty("planeManager").objectReferenceValue = pm;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("scene ok (overlay on " + pm.gameObject.name + ")");
        }

        static void Log(string msg)
        {
            Directory.CreateDirectory("Logs");
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            Debug.Log("[FireReady] " + msg);
        }
    }
}
