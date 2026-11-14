using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Xrv.Qr
{
    /// <summary>
    /// UDVIDELSE A - "To koder er bedre end én".
    /// En enkelt 12 cm kode giver en retning (yaw) med en fejl på måske 1-3 grader. 3 meter væk bliver det til 5-15 cm!
    /// Med to koder et stykke fra hinanden bruger vi kun deres POSITIONER: linjen fra L til R giver retningen.
    /// Jo længere imellem dem, jo mindre vinkelfejl (samme princip som lange "baseline" i landmåling).
    ///
    /// Læg scriptet på samme GameObject som SharedOrigin. Koderne laves med holdets nøgle og indeholder
    /// "id=L;scene=tower;seed=9;h=12" og "id=R" (se ark L og R eller QR-generator-siden).
    /// Rum-koordinater: origin = L, +X = mod R, +Y = op, +Z = X krydset med op.
    /// </summary>
    [RequireComponent(typeof(SharedOrigin))]
    public class TwoQrOrigin : MonoBehaviour
    {
        public int samplesPerCode = 5;
        public float sampleInterval = 0.5f;

        [Tooltip("Mål afstanden mellem kodernes midte med et målebånd og skriv den her (meter) for at se skalafejlen")]
        public float measuredDistance = 1.0f;

        SharedOrigin _origin;
        readonly Dictionary<string, MRUKTrackable> _codes = new();
        readonly Dictionary<string, List<Vector3>> _samples = new() { { "L", new List<Vector3>() }, { "R", new List<Vector3>() } };
        float _nextSample;
        string _payloadL;

        void Awake() => _origin = GetComponent<SharedOrigin>();

        void Start()
        {
            if (MRUK.Instance == null) return;
            MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnAdded);
            MRUK.Instance.SceneSettings.TrackableRemoved.AddListener(OnRemoved);
        }

        void OnDestroy()
        {
            if (MRUK.Instance == null) return;
            MRUK.Instance.SceneSettings.TrackableAdded.RemoveListener(OnAdded);
            MRUK.Instance.SceneSettings.TrackableRemoved.RemoveListener(OnRemoved);
        }

        void OnAdded(MRUKTrackable t)
        {
            if (t.TrackableType != OVRAnchor.TrackableType.QRCode) return;
            if (!SecretCode.TryDecode(t.MarkerPayloadString, _origin.teamKey, out var payload)) return;

            var p = PayloadContent.ParsePayload(payload);
            if (!p.TryGetValue("id", out var id) || !_samples.ContainsKey(id)) return;
            _codes[id] = t;
            if (id == "L") _payloadL = payload;
        }

        void OnRemoved(MRUKTrackable t)
        {
            foreach (var kv in _codes)
                if (kv.Value == t) { _codes.Remove(kv.Key); break; }
        }

        void Update()
        {
            if (_origin.IsLocked)
            {
                foreach (var list in _samples.Values) list.Clear();
                return;
            }
            if (Time.time < _nextSample) return;
            _nextSample = Time.time + sampleInterval;

            foreach (var (id, t) in _codes)
            {
                if (t == null || !t.IsTracked) continue;
                var list = _samples[id];
                list.Add(t.transform.position);
                if (list.Count > samplesPerCode) list.RemoveAt(0);
            }

            if (_samples["L"].Count < samplesPerCode || _samples["R"].Count < samplesPerCode) return;

            Vector3 l = Mean(_samples["L"]);
            Vector3 r = Mean(_samples["R"]);

            Vector3 right = Vector3.ProjectOnPlane(r - l, Vector3.up);
            if (right.magnitude < 0.2f)
            {
                Debug.LogWarning("[TwoQr] Koderne er for tæt på hinanden (< 20 cm) til at give en god retning.");
                return;
            }
            right.Normalize();
            Vector3 forward = Vector3.Cross(right, Vector3.up);

            float seen = Vector3.Distance(l, r);
            Debug.Log($"[TwoQr] Afstand set af headsettet: {seen * 100f:0.0} cm, målt med båndet: {measuredDistance * 100f:0.0} cm " +
                      $"(fejl {(seen - measuredDistance) * 1000f:0} mm)");

            _origin.LockAt(new Pose(l, Quaternion.LookRotation(forward, Vector3.up)), _payloadL);
        }

        static Vector3 Mean(List<Vector3> list)
        {
            var sum = Vector3.zero;
            foreach (var v in list) sum += v;
            return sum / list.Count;
        }
    }
}
