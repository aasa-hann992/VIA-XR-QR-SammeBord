using System;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Xrv.Qr
{
    /// <summary>
    /// TRIN 2 – "Et fælles nulpunkt".
    /// Ligger på det GameObject, der er rod for alt delt indhold ("SharedOrigin").
    /// Når en QR-kode med holdets nøgle er set stabilt et par gange, flyttes roden hen på koden og låses.
    ///
    /// Rum-koordinatsystemet:
    ///   origin  = QR-kodens midte
    ///   +Y      = lodret op (tyngdekraften – IKKE kodens egen normal)
    ///   +Z      = mod QR-kodens top (på papiret), dvs. væk fra en der står ved papirets nederste kant
    ///   +X      = mod højre
    /// To headsets, der ser den samme kode, får derfor det samme koordinatsystem – helt uden netværk.
    /// </summary>
    public class SharedOrigin : MonoBehaviour
    {
        public enum Mode
        {
            [Tooltip("Følg hver eneste måling direkte (godt til at SE støjen)")] Follow,
            [Tooltip("Midl nogle målinger, lås og hold fast")] LockWhenStable,
        }

        [Header("Holdets nøgle – kun QR-koder lavet med den bliver brugt")]
        public string teamKey = SecretCode.DefaultKey;

        [Header("QR-kodens akser – find dem i trin 1!")]
        [Tooltip("Den lokale akse på trackablen, der peger UD af papiret")]
        public Vector3 qrLocalNormal = Vector3.forward;
        [Tooltip("Den lokale akse på trackablen, der peger mod kodens TOP (i papirets plan)")]
        public Vector3 qrLocalTop = Vector3.up;

        [Header("Låsning")]
        public Mode mode = Mode.LockWhenStable;
        [Tooltip("Antal målinger der midles, før vi låser")]
        public int samplesToLock = 5;
        [Tooltip("Sekunder mellem to målinger (MRUK opdaterer QR ca. 1 gang i sekundet)")]
        public float sampleInterval = 0.5f;
        [Tooltip("Målingerne må max ligge så langt fra hinanden (cm), før vi stoler på dem")]
        public float maxSpreadCm = 1.5f;
        [Tooltip("Målingerne må max være så mange grader drejet i forhold til hinanden")]
        public float maxSpreadDeg = 3f;
        [Tooltip("Lav en Meta Spatial Anchor, når vi låser, så indholdet bliver stående, når koden er ude af syne")]
        public bool useSpatialAnchor = true;
        [Tooltip("Skjul børn (indholdet), indtil vi har et nulpunkt")]
        public bool hideUntilLocked = true;

        public bool IsLocked { get; private set; }

        /// <summary>Det dekodede indhold af koden, fx "scene=tower;seed=42;h=10".</summary>
        public string Payload { get; private set; }

        /// <summary>Kaldes, hver gang vi låser. Argumentet er det dekodede indhold.</summary>
        public event Action<string> Locked;

        readonly List<MRUKTrackable> _candidates = new();
        readonly List<Pose> _samples = new();
        MRUKTrackable _target;
        string _targetContent;
        float _nextSampleTime;

        void Start()
        {
            if (hideUntilLocked) SetChildrenActive(false);
            StatusHud.Set("2", "Nulpunkt: leder efter en QR-kode med nøglen \"" + teamKey + "\" …");

            if (MRUK.Instance == null)
            {
                Debug.LogError("[SharedOrigin] Der er ingen MRUK-komponent i scenen.");
                return;
            }
            MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnTrackableAdded);
            MRUK.Instance.SceneSettings.TrackableRemoved.AddListener(OnTrackableRemoved);
        }

        void OnDestroy()
        {
            if (MRUK.Instance == null) return;
            MRUK.Instance.SceneSettings.TrackableAdded.RemoveListener(OnTrackableAdded);
            MRUK.Instance.SceneSettings.TrackableRemoved.RemoveListener(OnTrackableRemoved);
        }

        void OnTrackableAdded(MRUKTrackable t)
        {
            if (t.TrackableType != OVRAnchor.TrackableType.QRCode) return;
            // Kun koder lavet med holdets nøgle (opgave 2a) – alle andre QR-koder i lokalet ignoreres.
            if (!SecretCode.TryDecode(t.MarkerPayloadString, teamKey, out _)) return;
            _candidates.Add(t);
        }

        void OnTrackableRemoved(MRUKTrackable t) => _candidates.Remove(t);

        void Update()
        {
            // B-knappen på højre controller: glem nulpunktet, og find det igen.
            if (OVRInput.GetDown(OVRInput.Button.Two)) Unlock();

            if (IsLocked) return;

            var target = NearestTrackedCandidate();
            if (target != _target)
            {
                _target = target;
                _samples.Clear();
                if (_target != null) SecretCode.TryDecode(_target.MarkerPayloadString, teamKey, out _targetContent);
            }
            if (_target == null || Time.time < _nextSampleTime) return;
            _nextSampleTime = Time.time + sampleInterval;

            var pose = GravityAlignedPose(_target.transform);

            if (mode == Mode.Follow)
            {
                // Ingen midling: vi ser præcis, hvor meget målingerne hopper.
                transform.SetPositionAndRotation(pose.position, pose.rotation);
                SetChildrenActive(true);
                StatusHud.Set("2", "Nulpunkt: FØLGER koden (mode = Follow)");
                if (Payload != _targetContent)
                {
                    Payload = _targetContent;
                    Locked?.Invoke(Payload); // byg indholdet første gang, vi ser koden
                }
                return;
            }

            _samples.Add(pose);
            if (_samples.Count > samplesToLock) _samples.RemoveAt(0);
            StatusHud.Set("2", $"Nulpunkt: måler … {_samples.Count}/{samplesToLock}");
            if (_samples.Count < samplesToLock) return;

            var mean = Average(_samples);
            Spread(_samples, mean, out float spreadCm, out float spreadDeg);
            if (spreadCm <= maxSpreadCm && spreadDeg <= maxSpreadDeg)
                LockAt(mean, _targetContent);
            else
                StatusHud.Set("2", $"Nulpunkt: målingerne er uenige ({spreadCm:0.0} cm / {spreadDeg:0.0}°) – stå stille");
        }

        /// <summary>
        /// Tager QR-kodens pose og fjerner hældning (pitch/roll): vi stoler på headsettets tyngdekraft,
        /// ikke på koden. Kun position + drejning om lodret akse (yaw) bliver tilbage = 4 frihedsgrader.
        /// </summary>
        public Pose GravityAlignedPose(Transform qr)
        {
            // TODO OPGAVE 2b – Lås til tyngdekraften: kun position + drejning om lodret akse (4 DoF).
            //   • qr.rotation * qrLocalTop  giver kodens top-retning i world space
            //   • Vector3.ProjectOnPlane(v, Vector3.up)  fjerner den lodrette del
            //   • Quaternion.LookRotation(retning, Vector3.up)  bygger en rotation, hvor op altid er op
            //   • Hænger koden på en væg, forsvinder "top" ved projektionen – brug -normalen i stedet.
            // Midlertidig "forkert" løsning: kodens pose med hældning og det hele.
            return new Pose(qr.position, qr.rotation);
        }

        /// <summary>Lås nulpunktet her. Bruges også af ControllerCalibration og TwoQrOrigin (udvidelser).</summary>
        public void LockAt(Pose pose, string payload)
        {
            RemoveAnchor();
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            IsLocked = true;
            Payload = payload;
            SetChildrenActive(true);

            if (useSpatialAnchor)
                gameObject.AddComponent<OVRSpatialAnchor>(); // headsettet holder nu selv positionen fast

            StatusHud.Set("2", "Nulpunkt: LÅST på \"" + payload + "\"   (B = lås igen)");
            Debug.Log($"[SharedOrigin] Låst på '{payload}' ved {pose.position}");
            Locked?.Invoke(payload);
        }

        public void Unlock()
        {
            RemoveAnchor();
            IsLocked = false;
            _samples.Clear();
            _target = null;
            if (hideUntilLocked) SetChildrenActive(false);
            StatusHud.Set("2", "Nulpunkt: leder igen …");
        }

        void RemoveAnchor()
        {
            var anchor = GetComponent<OVRSpatialAnchor>();
            // DestroyImmediate, fordi LockAt kan tilføje et nyt anker i samme frame.
            if (anchor != null) DestroyImmediate(anchor);
        }

        MRUKTrackable NearestTrackedCandidate()
        {
            var cam = Camera.main;
            if (cam == null) return null;

            MRUKTrackable best = null;
            float bestDist = float.MaxValue;
            foreach (var c in _candidates)
            {
                if (c == null || !c.IsTracked) continue;
                float d = Vector3.Distance(c.transform.position, cam.transform.position);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        static Pose Average(List<Pose> poses)
        {
            // TODO OPGAVE 2c – Midl målingerne.
            // Positioner kan midles som tal, men retninger skal midles som VEKTORER
            // (359° og 1° giver ikke 180°!): læg p.rotation * Vector3.forward sammen, og normalisér.
            // Midlertidig løsning: den nyeste måling.
            return poses[poses.Count - 1];
        }

        static void Spread(List<Pose> poses, Pose mean, out float maxCm, out float maxDeg)
        {
            maxCm = 0f;
            maxDeg = 0f;
            foreach (var p in poses)
            {
                maxCm = Mathf.Max(maxCm, Vector3.Distance(p.position, mean.position) * 100f);
                maxDeg = Mathf.Max(maxDeg, Quaternion.Angle(p.rotation, mean.rotation));
            }
        }

        void SetChildrenActive(bool active)
        {
            foreach (Transform child in transform) child.gameObject.SetActive(active);
        }
    }
}
