using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Xrv.Qr;

namespace Xrv.EditorTools
{
    /// <summary>Bygger projektets scener. Menu: VIA XR › Byg scener igen.</summary>
    public static class ProjectScenes
    {
        public const string QrScene = "Assets/Scenes/1_QR_SammeBord.unity";
        public const string PhysicsScene = "Assets/Scenes/2_Fysik_IkkeDeterministisk.unity";

        public static void BuildAll()
        {
            PlayerSettings.companyName = "VIA University College";
            PlayerSettings.productName = "XRV QR Samme Bord";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "dk.via.xr.qrsammebord");

            var mat = XrvSceneUtil.GetMaterial("XRV_Block", Color.white);

            // 1) Øvelsen: QR-kode → fælles nulpunkt → samme indhold på begge headsets
            var b = XrvSceneUtil.CreateBase(loadRoomScan: false, qrTracking: true, worldLock: false);
            XrvSceneUtil.Empty("HUD", typeof(StatusHud));
            XrvSceneUtil.Empty("SceneCycler", typeof(SceneCycler));
            XrvSceneUtil.Empty("QR Debug", typeof(QrDebugVisualizer));
            var origin = XrvSceneUtil.Empty("SharedOrigin", typeof(SharedOrigin), typeof(PayloadContent), typeof(ControllerCalibration));
            origin.GetComponent<ControllerCalibration>().enabled = false; // Quest Pro-varianten: slå til i inspectoren
            origin.GetComponent<PayloadContent>().baseMaterial = mat;
            XrvSceneUtil.Save(b, QrScene);

            // 2) Demoen: samme tårn, samme sted, samme tid – forskelligt fald
            b = XrvSceneUtil.CreateBase(loadRoomScan: false, qrTracking: true, worldLock: false);
            XrvSceneUtil.Empty("HUD", typeof(StatusHud));
            XrvSceneUtil.Empty("SceneCycler", typeof(SceneCycler));
            XrvSceneUtil.Empty("QR Debug", typeof(QrDebugVisualizer));
            origin = XrvSceneUtil.Empty("SharedOrigin", typeof(SharedOrigin), typeof(PhysicsTowerDemo));
            origin.GetComponent<SharedOrigin>().hideUntilLocked = false;
            origin.GetComponent<SharedOrigin>().requireMyId = false; // alle grupper bruger ark P – virker uden opgave 2a
            origin.GetComponent<PhysicsTowerDemo>().baseMaterial = mat;
            XrvSceneUtil.Save(b, PhysicsScene);

            XrvSceneUtil.SetBuildScenes(QrScene, PhysicsScene);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(QrScene);
        }
    }
}
