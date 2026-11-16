using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Xrv.Qr
{
    /// <summary>
    /// TRIN 1 - "Hvad ser headsettet egentlig?"
    /// Sætter et akse-kryds (rød = X, grøn = Y, blå = Z) og en tekst på hver QR-kode som MRUK finder.
    /// Teksten viser payload, om koden er tracked, hvor mange gange posen er blevet opdateret og hvor ofte.
    ///
    /// Opsætning: læg scriptet på et tomt GameObject i en scene der har en MRUK-komponent
    /// med "QR Code Tracking Enabled" slået til (MRUK > Scene Settings > Tracker Configuration).
    /// </summary>
    public class QrDebugVisualizer : MonoBehaviour
    {
        [Tooltip("Længde af akse-pilene i meter")]
        public float axisLength = 0.10f;

        [Tooltip("Tykkelse af akse-pilene i meter")]
        public float axisThickness = 0.006f;

        class Visual
        {
            public GameObject root;
            public TextMesh label;
            public int updates;
            public Vector3 lastPos;
            public Quaternion lastRot;
            public float lastUpdateTime;
            public float interval;
        }

        readonly Dictionary<MRUKTrackable, Visual> _visuals = new();
        readonly List<Transform> _letters = new();
        Font _font;

        void Start()
        {
            if (MRUK.Instance == null)
            {
                Debug.LogError("[QrDebug] Der er ingen MRUK-komponent i scenen.");
                enabled = false;
                return;
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnTrackableAdded);
            MRUK.Instance.SceneSettings.TrackableRemoved.AddListener(OnTrackableRemoved);
            Debug.Log($"[QrDebug] QR-tracking understøttet på denne enhed: {MRUK.Instance.QRCodeTrackingSupported}");
        }

        void OnDestroy()
        {
            if (MRUK.Instance == null) return;
            MRUK.Instance.SceneSettings.TrackableAdded.RemoveListener(OnTrackableAdded);
            MRUK.Instance.SceneSettings.TrackableRemoved.RemoveListener(OnTrackableRemoved);
        }

        void OnTrackableAdded(MRUKTrackable trackable)
        {
            if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode) return;

            Debug.Log($"[QrDebug] Ny QR-kode: '{trackable.MarkerPayloadString}'");

            var v = new Visual { root = new GameObject("QR debug") };
            v.root.transform.SetParent(trackable.transform, false);

            MakeAxis(v.root.transform, new Vector3(axisLength, axisThickness, axisThickness), Vector3.right, Color.red);
            MakeAxis(v.root.transform, new Vector3(axisThickness, axisLength, axisThickness), Vector3.up, Color.green);
            MakeAxis(v.root.transform, new Vector3(axisThickness, axisThickness, axisLength), Vector3.forward, Color.blue);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(v.root.transform, false);
            v.label = labelGo.AddComponent<TextMesh>();
            v.label.font = _font;
            labelGo.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            v.label.fontSize = 48;
            v.label.characterSize = 0.0025f;
            v.label.anchor = TextAnchor.LowerCenter;
            v.label.color = Color.yellow;

            v.lastPos = trackable.transform.position;
            v.lastRot = trackable.transform.rotation;
            v.lastUpdateTime = Time.time;
            _visuals[trackable] = v;
        }

        void OnTrackableRemoved(MRUKTrackable trackable)
        {
            if (_visuals.TryGetValue(trackable, out var v))
            {
                Destroy(v.root);
                _visuals.Remove(trackable);
            }
        }

        void Update()
        {
            if (MRUK.Instance != null)
                StatusHud.Set("0", (MRUK.Instance.QRCodeTrackingSupported ? "QR-tracking: understøttet" : "QR-tracking: IKKE understøttet (Quest 3/3S? rumdata-tilladelse?)")
                                   + $"   ·   QR-koder set: {_visuals.Count}");
            var cam = Camera.main;
            if (cam != null)
                foreach (var l in _letters)
                    if (l != null) l.rotation = Quaternion.LookRotation(l.position - cam.transform.position, Vector3.up);

            foreach (var (trackable, v) in _visuals)
            {
                if (trackable == null) continue;
                var t = trackable.transform;

                // MRUK flytter trackable-objektet når der kommer en ny måling. Vi tæller hvor tit.
                bool moved = Vector3.Distance(t.position, v.lastPos) > 0.0005f ||
                             Quaternion.Angle(t.rotation, v.lastRot) > 0.2f;
                if (moved)
                {
                    v.updates++;
                    v.interval = Time.time - v.lastUpdateTime;
                    v.lastUpdateTime = Time.time;
                    v.lastPos = t.position;
                    v.lastRot = t.rotation;
                }

                // Ligger koden fladt eller hænger den på en væg? (antager at lokal Z er normalen - TJEK DET!)
                float upDot = Vector3.Dot(t.forward, Vector3.up);
                string lie = Mathf.Abs(upDot) > 0.7f ? "ligger fladt" : "står/hænger";

                string size = trackable.PlaneRect.HasValue
                    ? $"{trackable.PlaneRect.Value.width * 100f:0.0} x {trackable.PlaneRect.Value.height * 100f:0.0} cm"
                    : "ukendt";

                var origin = FindAnyObjectByType<SharedOrigin>();
                string raw = trackable.MarkerPayloadString;
                string decoded;
                if (!QrCode.TryReadAny(raw, out _)) decoded = "Ikke en XRV-kode – ignoreres";
                else if (origin == null || origin.Accepts(raw, out _)) decoded = "Til os";
                else decoded = $"Ikke til os (vores id er {origin.myId})";

                v.label.text =
                    $"{trackable.MarkerPayloadString}\n{decoded}\n" +
                    $"Tracked: {trackable.IsTracked}   ({lie})\n" +
                    $"Opdateringer: {v.updates}   sidst efter {v.interval:0.00} s\n" +
                    $"Størrelse: {size}\n" +
                    $"Pos: {t.position.x:0.000}, {t.position.y:0.000}, {t.position.z:0.000}";

                // Placér teksten over koden (i verdens-op) og vend den mod kameraet så den kan læses.
                v.label.transform.position = t.position + Vector3.up * (axisLength + 0.03f);
                if (cam != null)
                    v.label.transform.rotation = Quaternion.LookRotation(v.label.transform.position - cam.transform.position, Vector3.up);
            }
        }

        void MakeAxis(Transform parent, Vector3 scale, Vector3 dir, Color color)
        {
            // Samme materiale som indholdet (PayloadContent.SharedMaterial): et materiale, der ligger som asset,
            // kommer med i APK'en. Standard-materialet fra CreatePrimitive kan blive strippet, så akserne er usynlige.
            float len = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            var go = PayloadContent.MakePrimitive(PrimitiveType.Cube, parent, dir * (len * 0.5f), scale, color);
            go.name = $"Axis {dir}";

            // Bogstav for enden af aksen: X, Y eller Z
            var letter = new GameObject("Bogstav");
            letter.transform.SetParent(parent, false);
            letter.transform.localPosition = dir * (len + 0.012f);
            var tm = letter.AddComponent<TextMesh>();
            tm.font = _font;
            letter.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            tm.text = dir == Vector3.right ? "X" : dir == Vector3.up ? "Y" : "Z";
            tm.fontSize = 60;
            tm.characterSize = 0.003f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;
            _letters.Add(letter.transform);
        }
    }
}
