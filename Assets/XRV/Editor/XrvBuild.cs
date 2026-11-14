using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Xrv.EditorTools
{
    /// <summary>
    /// Byg-værktøj til undervisningsprojekterne.
    ///
    /// Menu:  VIA XR › Byg scener igen      – genskaber projektets scener (ProjectScenes.BuildAll)
    ///        VIA XR › Byg APK til Quest 3  – bygger Builds/&lt;navn&gt;.apk
    ///
    /// Automatisk: ligger der en fil "XrvAutoBuild.request" i projektmappen, når Unity åbner projektet,
    /// udføres det, der står i den (linjer: "check", "scenes" og/eller "apk"), og resultatet skrives til
    /// "XrvAutoBuild.result.txt". Det bruges, når projektet bygges uden at nogen sidder ved Unity.
    /// </summary>
    [InitializeOnLoad]
    public static class XrvBuild
    {
        const string RequestFile = "XrvAutoBuild.request";
        const string ResultFile = "XrvAutoBuild.result.txt";

        static XrvBuild()
        {
            if (File.Exists(RequestFile)) EditorApplication.delayCall += RunRequested;
        }

        static void RunRequested()
        {
            if (!File.Exists(RequestFile)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunRequested;
                return;
            }

            var request = File.ReadAllText(RequestFile);
            try
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                {
                    Log("Skifter build target til Android …");
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                    EditorApplication.delayCall += RunRequested; // fortsætter efter skiftet (eller efter domain reload)
                    return;
                }

                File.Delete(RequestFile); // før vi bygger, så en fejl ikke giver en uendelig løkke
                if (request.Contains("check")) Log("COMPILE OK");
                if (request.Contains("scenes")) BuildScenes();
                if (request.Contains("apk")) BuildApk();
                Log("FÆRDIG");
            }
            catch (Exception e)
            {
                Log("FEJL: " + e);
                if (File.Exists(RequestFile)) File.Delete(RequestFile);
            }
        }

        [MenuItem("VIA XR/Byg scener igen")]
        public static void BuildScenes()
        {
            var t = Type.GetType("Xrv.EditorTools.ProjectScenes");
            var m = t?.GetMethod("BuildAll");
            if (m == null) { Log("Fandt ikke Xrv.EditorTools.ProjectScenes.BuildAll"); return; }
            m.Invoke(null, null);
            AssetDatabase.SaveAssets();
            Log("Scener bygget: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => s.path)));
        }

        [MenuItem("VIA XR/Byg APK til Quest 3")]
        public static void BuildApk()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) { Log("Ingen scener i Build Settings."); return; }

            Directory.CreateDirectory("Builds");
            var path = Path.Combine("Builds", PlayerSettings.productName.Replace(' ', '_') + ".apk");
            var errors = new List<string>();
            void Collect(string msg, string stack, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception) errors.Add(msg);
            }

            Application.logMessageReceived += Collect;
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes, locationPathName = path, target = BuildTarget.Android, options = BuildOptions.None,
                });
            }
            finally { Application.logMessageReceived -= Collect; }

            var s = report.summary;
            var sb = new StringBuilder();
            sb.AppendLine($"APK: {s.result}  ({path}, {s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:0} s, {s.totalErrors} fejl)");
            foreach (var step in report.steps)
                foreach (var msg in step.messages)
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        sb.AppendLine("  [" + step.name + "] " + msg.content);
            foreach (var e in errors.Distinct().Take(30)) sb.AppendLine("  log: " + e);
            Log(sb.ToString());
        }

        static void Log(string msg)
        {
            Debug.Log("[VIA XR] " + msg);
            File.AppendAllText(ResultFile, $"{DateTime.Now:HH:mm:ss} {msg}\n");
        }
    }
}
