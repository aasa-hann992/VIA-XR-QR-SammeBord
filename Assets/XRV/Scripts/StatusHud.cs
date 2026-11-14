using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Xrv.Qr
{
    /// <summary>
    /// En lille tekst, der svæver nederst i synsfeltet og viser, hvad der sker.
    /// Alle scripts kan skrive en linje:  StatusHud.Set("origin", "Låst!");
    /// Venstre thumbstick-klik skjuler/viser teksten.
    /// </summary>
    public class StatusHud : MonoBehaviour
    {
        static readonly SortedDictionary<string, string> Lines = new();

        public static void Set(string key, string text)
        {
            if (string.IsNullOrEmpty(text)) Lines.Remove(key);
            else Lines[key] = text;
        }

        public Vector3 offset = new Vector3(0f, -0.22f, 0.6f);
        public float followSpeed = 4f;

        TextMesh _text;
        Transform _head;
        bool _visible = true;

        void Start()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text = gameObject.AddComponent<TextMesh>();
            _text.font = font;
            GetComponent<MeshRenderer>().sharedMaterial = font.material;
            _text.fontSize = 48;
            _text.characterSize = 0.0028f;
            _text.anchor = TextAnchor.UpperCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = new Color(1f, 0.95f, 0.6f);
        }

        void LateUpdate()
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick)) _visible = !_visible;
            if (_head == null && Camera.main != null) _head = Camera.main.transform;
            if (_head == null) return;

            var sb = new StringBuilder();
            foreach (var kv in Lines) sb.AppendLine(kv.Value);
            _text.text = _visible ? sb.ToString() : "";

            // Følg hovedet blødt – kun yaw, så teksten ikke tipper, når man kigger ned på bordet.
            var fwd = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = transform.forward;
            var target = _head.position + fwd * offset.z + Vector3.up * offset.y;
            float k = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fwd, Vector3.up), k);
        }
    }
}
