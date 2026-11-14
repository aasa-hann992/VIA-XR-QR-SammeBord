using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Xrv.EditorTools
{
    /// <summary>Fælles byggeklodser til scenerne: lys, kamera-rig med passthrough og MR Utility Kit.</summary>
    public static class XrvSceneUtil
    {
        const string RigPrefab = "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab";
        const string MrukPrefab = "Packages/com.meta.xr.mrutilitykit/Core/Tools/MRUK.prefab";

        public class Base
        {
            public UnityEngine.SceneManagement.Scene scene;
            public GameObject rig;
            public OVRManager manager;
            public MRUK mruk;
        }

        /// <summary>
        /// Ny scene med lys, OVRCameraRig (passthrough, beder om rumdata-tilladelse) og MRUK.
        /// </summary>
        /// <param name="loadRoomScan">true = MRUK indlæser headsettets rum-scanning ved start (scene understanding).</param>
        /// <param name="qrTracking">true = MRUK leder efter QR-koder.</param>
        /// <param name="worldLock">MRUK's world lock flytter tracking space efter rum-scanningen. Slås fra i QR-øvelsen.</param>
        public static Base CreateBase(bool loadRoomScan, bool qrTracking, bool worldLock)
        {
            var b = new Base { scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single) };

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            b.rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefab));
            b.rig.name = "OVRCameraRig";
            b.manager = b.rig.GetComponent<OVRManager>() ?? b.rig.AddComponent<OVRManager>();
            b.manager.isInsightPassthroughEnabled = true;
            var so = new SerializedObject(b.manager);
            var p = so.FindProperty("requestScenePermissionOnStartup");
            if (p != null) p.boolValue = true;
            else Debug.LogWarning("[VIA XR] Fandt ikke requestScenePermissionOnStartup på OVRManager");
            so.ApplyModifiedPropertiesWithoutUndo();
            b.rig.AddComponent<OVRPassthroughLayer>();

            var cam = b.rig.transform.Find("TrackingSpace/CenterEyeAnchor").GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.nearClipPlane = 0.03f;

            var mrukGo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MrukPrefab));
            mrukGo.name = "MRUK";
            b.mruk = mrukGo.GetComponent<MRUK>();
            b.mruk.EnableWorldLock = worldLock;
            b.mruk.SceneSettings.LoadSceneOnStartup = loadRoomScan;
            b.mruk.SceneSettings.DataSource = MRUK.SceneDataSource.Device;
            var tc = b.mruk.SceneSettings.TrackerConfiguration;
            tc.QRCodeTrackingEnabled = qrTracking;
            b.mruk.SceneSettings.TrackerConfiguration = tc;
            EditorUtility.SetDirty(b.mruk);
            return b;
        }

        /// <summary>
        /// Henter/laver et URP-materiale som asset. Materialer, der ligger som assets og bruges i en scene,
        /// sikrer at shaderen kommer med i APK'en (Shader.Find alene kan blive strippet ved build).
        /// </summary>
        public static Material GetMaterial(string name, Color color, bool unlit = false, bool transparent = false)
        {
            const string dir = "Assets/XRV/Materials/";
            var path = dir + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            System.IO.Directory.CreateDirectory(dir);
            mat = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        public static GameObject Empty(string name, params System.Type[] components)
        {
            var go = new GameObject(name);
            foreach (var c in components) go.AddComponent(c);
            return go;
        }

        /// <summary>Gemmer scenen. Kald den, før næste scene laves (CreateBase lukker den åbne scene).</summary>
        public static void Save(Base b, string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(b.scene, path);
        }

        /// <summary>Sætter Build Settings til netop disse scener (første = start-scenen i APK'en).</summary>
        public static void SetBuildScenes(params string[] paths)
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in paths) list.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
