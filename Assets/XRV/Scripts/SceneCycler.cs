using UnityEngine;
using UnityEngine.SceneManagement;

namespace Xrv.Qr
{
    /// <summary>X-knappen på venstre controller skifter til næste scene i Build Settings.</summary>
    public class SceneCycler : MonoBehaviour
    {
        void Start()
        {
            var s = SceneManager.GetActiveScene();
            StatusHud.Set("9", $"Scene: {s.name}   (X = næste scene)");
        }

        void Update()
        {
            if (!OVRInput.GetDown(OVRInput.Button.Three)) return;
            int next = (SceneManager.GetActiveScene().buildIndex + 1) % SceneManager.sceneCountInBuildSettings;
            SceneManager.LoadScene(next);
        }
    }
}
