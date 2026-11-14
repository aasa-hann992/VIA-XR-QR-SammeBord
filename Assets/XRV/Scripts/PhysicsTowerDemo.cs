using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Xrv.Qr
{
    /// <summary>
    /// DEMO – "Fysik er ikke deterministisk".
    ///
    /// Begge headsets bygger det SAMME skæve tårn (samme seed fra QR-koden) på det SAMME sted (samme nulpunkt)
    /// og slipper det på det SAMME tidspunkt (det fælles ur). Alligevel vælter det forskelligt, fordi
    /// fysik-simuleringen ikke giver præcis samme resultat på to enheder: hvert headset regner i sine egne
    /// world-koordinater, og frames og fysik-steps ligger ikke helt ens. Et kaotisk system forstærker
    /// de små forskelle, til de kan ses.
    ///
    /// Efter faldet viser HUD'en, hvor den øverste klods landede. Sammenlign tallene med din makker.
    /// Læg scriptet på samme GameObject som SharedOrigin.
    /// </summary>
    [RequireComponent(typeof(SharedOrigin))]
    public class PhysicsTowerDemo : MonoBehaviour
    {
        [Tooltip("Hele forløbet gentages med denne periode (sekunder). Starter på hele multipla af uret.")]
        public float cycleSeconds = 20f;
        [Tooltip("Hvor mange sekunder inde i perioden tårnet slippes")]
        public float releaseAt = 6f;
        [Tooltip("Hvornår (efter slip) landingspunktet vises")]
        public float reportAfter = 5f;
        public int defaultHeight = 12;
        public float blockSize = 0.06f;
        [Tooltip("Hvor står tårnet i forhold til QR-kodens midte (rum-koordinater, meter)")]
        public Vector3 towerOffset = new Vector3(0f, 0f, 0.22f);
        [Tooltip("Materialet klodserne laves af (sættes af scene-byggeren)")]
        public Material baseMaterial;

        SharedOrigin _origin;
        Transform _root;
        readonly List<Rigidbody> _bodies = new();
        int _seed, _height;
        long _cycle = long.MinValue;
        bool _released, _reported, _ready;

        void Awake()
        {
            _origin = GetComponent<SharedOrigin>();
            _origin.Locked += OnLocked;
            if (baseMaterial != null) PayloadContent.SharedMaterial = baseMaterial;
        }

        void OnDestroy()
        {
            if (_origin != null) _origin.Locked -= OnLocked;
        }

        void OnLocked(string payload)
        {
            var p = PayloadContent.ParsePayload(payload);
            _seed = p.TryGetValue("seed", out var s) && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
            _height = p.TryGetValue("h", out var hs) && int.TryParse(hs, NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) ? Mathf.Clamp(h, 4, 20) : defaultHeight;
            _ready = true;
            _cycle = long.MinValue; // byg forfra med det samme
        }

        void Update()
        {
            if (!_ready || !_origin.IsLocked) return;

            double t = SharedClock.Seconds;
            long cycle = (long)Math.Floor(t / cycleSeconds);
            double inCycle = t - cycle * (double)cycleSeconds;

            if (cycle != _cycle)
            {
                _cycle = cycle;
                Rebuild();
            }

            if (!_released)
            {
                if (inCycle >= releaseAt) Release();
                else StatusHud.Set("4", $"Fysik: tårnet slippes om {releaseAt - inCycle:0.0} s");
            }
            else if (!_reported && inCycle >= releaseAt + reportAfter)
            {
                _reported = true;
                var top = _bodies[_bodies.Count - 1].transform.position;
                var local = _origin.transform.InverseTransformPoint(top);
                StatusHud.Set("4", $"Øverste klods landede:  x = {local.x * 100f:0.0} cm   z = {local.z * 100f:0.0} cm\n" +
                                   "Sammenlign med din makker! (nyt forsøg om lidt)");
            }
        }

        void Rebuild()
        {
            if (_root != null) Destroy(_root.gameObject);
            _bodies.Clear();
            _released = false;
            _reported = false;

            // Tårnet er IKKE barn af SharedOrigin: et spatial anchor flytter sin transform en anelse hver frame,
            // og det må ikke skubbe til dynamiske rigidbodies. Vi kopierer posen én gang.
            _root = new GameObject("PhysicsTower").transform;
            _root.SetPositionAndRotation(_origin.transform.position, _origin.transform.rotation);

            // Et usynligt bord: en flad box-collider i højde med QR-koden.
            var floor = new GameObject("Bordflade").AddComponent<BoxCollider>();
            floor.transform.SetParent(_root, false);
            floor.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            floor.size = new Vector3(1.6f, 0.02f, 1.6f);

            // Samme seed → samme skæve tårn på begge headsets. Det læner sig mere og mere til den ene side.
            var rng = new System.Random(_seed);
            float x = 0f, z = 0f;
            for (int i = 0; i < _height; i++)
            {
                x += (float)(rng.NextDouble() * 0.55 - 0.15) * blockSize;
                z += (float)(rng.NextDouble() * 0.3 - 0.15) * blockSize;
                var pos = towerOffset + new Vector3(x, blockSize * (i + 0.5f), z);
                var color = PayloadContent.Palette[rng.Next(PayloadContent.Palette.Length)];
                var block = PayloadContent.MakePrimitive(PrimitiveType.Cube, _root, pos, Vector3.one * blockSize, color);
                block.AddComponent<BoxCollider>(); // MakePrimitive fjerner collideren – her skal vi bruge den
                var rb = block.AddComponent<Rigidbody>();
                rb.mass = 0.2f;
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                _bodies.Add(rb);
            }
        }

        void Release()
        {
            _released = true;
            foreach (var rb in _bodies) rb.isKinematic = false;
            StatusHud.Set("4", "Fysik: SLIPPER!");
        }
    }
}
