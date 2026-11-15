using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Xrv.Qr
{
    /// <summary>
    /// TRIN 3 - "Samme oplevelse fra samme kode".
    /// Læg scriptet på SAMME GameObject som SharedOrigin. Når origin låser, læses QR-kodens payload,
    /// og indholdet bygges deterministisk ud fra den. Begge headsets læser den samme kode -> bygger det samme.
    ///
    /// Indhold (teksten efter "XRV;"):  id=7;model=tower;seed=42;h=10
    ///   model = tower | orbit | blocks | physics  – hvad der skal stå på koden
    ///   seed  = tal der styrer tilfældigheden (System.Random med samme seed giver samme talrække)
    /// </summary>
    [RequireComponent(typeof(SharedOrigin))]
    public class PayloadContent : MonoBehaviour
    {
        [Tooltip("Kantlængde på en klods i meter")]
        public float blockSize = 0.06f;

        [Tooltip("Hvor står indholdet i forhold til QR-kodens midte (rum-koordinater, meter). " +
                 "+Z er væk fra brugeren, så 0.22 er lige bag A4-arket.")]
        public Vector3 contentOffset = new Vector3(0f, 0f, 0.22f);

        [Tooltip("Krydsene på det printede ark, målt fra QR-kodens midte i meter (x = højre, z = mod kodens top)")]
        public Vector3[] printedCrosses =
        {
            new Vector3(-0.07f, 0f, -0.085f),
            new Vector3(0.07f, 0f, -0.085f),
        };

        public static readonly Color[] Palette =
        {
            new Color(0.95f, 0.30f, 0.25f), new Color(0.25f, 0.65f, 0.95f), new Color(0.98f, 0.80f, 0.20f),
            new Color(0.35f, 0.80f, 0.45f), new Color(0.70f, 0.45f, 0.90f), new Color(0.95f, 0.95f, 0.95f),
        };

        [Tooltip("Materialet klodserne laves af (sættes af scene-byggeren, så shaderen kommer med i APK'en)")]
        public Material baseMaterial;

        /// <summary>Materiale til MakePrimitive. Sættes af PayloadContent / PhysicsTowerDemo.</summary>
        public static Material SharedMaterial { get; set; }

        SharedOrigin _origin;
        Transform _contentRoot;

        void Awake()
        {
            _origin = GetComponent<SharedOrigin>();
            _origin.Locked += Build;
            if (baseMaterial != null) SharedMaterial = baseMaterial;
        }

        void OnDestroy()
        {
            if (_origin != null) _origin.Locked -= Build;
        }

        public static Dictionary<string, string> ParsePayload(string payload)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(payload)) return result;
            foreach (var part in payload.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0) result[part.Substring(0, eq).Trim()] = part.Substring(eq + 1).Trim();
            }
            return result;
        }

        static int GetInt(Dictionary<string, string> p, string key, int fallback) =>
            p.TryGetValue(key, out var s) && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

        void Build(string payload)
        {
            // Byg forfra hver gang vi låser (fx efter B-knappen).
            if (_contentRoot != null) Destroy(_contentRoot.gameObject);
            _contentRoot = new GameObject("Content").transform;
            _contentRoot.SetParent(transform, false);

            var p = ParsePayload(payload);
            int seed = GetInt(p, "seed", 0);
            p.TryGetValue("model", out string model);
            StatusHud.Set("3", $"Indhold: {payload}");

            // Kontrol-kugler oven på de printede kryds: her SER man direkte hvor stor fejlen er.
            foreach (var c in printedCrosses)
            {
                var dot = MakePrimitive(PrimitiveType.Sphere, _contentRoot, c + Vector3.up * 0.005f, Vector3.one * 0.01f, Color.magenta);
                MakePrimitive(PrimitiveType.Cylinder, dot.transform.parent, c + Vector3.up * 0.02f, new Vector3(0.002f, 0.015f, 0.002f), Color.magenta);
            }

            switch (model)
            {
                case "tower": BuildTower(seed, GetInt(p, "h", 10)); break;
                case "orbit": BuildOrbit(seed); break;
                case "blocks": BuildBlocks(p.TryGetValue("b", out var b) ? b : ""); break;
                case "physics": break; // fysik-demoen bygger selv sit tårn (PhysicsTowerDemo)
                default: Debug.LogWarning($"[PayloadContent] Ukendt model '{model}' i '{payload}'"); break;
            }
        }

        /// <summary>Et tårn af klodser hvor seed bestemmer forskydning, drejning og farve.</summary>
        void BuildTower(int seed, int height)
        {
            // TODO OPGAVE 3a – Det SAMME tårn på begge headsets.
            // Lige nu bruges UnityEngine.Random: hvert headset bygger sit EGET tilfældige tårn. Se det med to headsets!
            // Ret det:  var rng = new System.Random(seed);  og brug rng.NextDouble() og rng.Next(...) i stedet.
            // Hvorfor giver samme seed det samme tårn – og hvorfor ikke UnityEngine.Random.InitState(seed)?
            for (int i = 0; i < height; i++)
            {
                float dx = (UnityEngine.Random.value - 0.5f) * blockSize * 0.4f;
                float dz = (UnityEngine.Random.value - 0.5f) * blockSize * 0.4f;
                float yaw = UnityEngine.Random.value * 90f;
                var color = Palette[UnityEngine.Random.Range(0, Palette.Length)];

                var pos = contentOffset + new Vector3(dx, blockSize * (i + 0.5f), dz);
                var block = MakePrimitive(PrimitiveType.Cube, _contentRoot, pos, Vector3.one * blockSize, color);
                block.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
            // Bemærk: ingen Rigidbody. Fysik er ikke deterministisk på tværs af enheder,
            // så et tårn der "vælter" ville vælte forskelligt for de to spillere.
        }

        /// <summary>En lille "sol" med planeter. Bevægelsen styres af det fælles ur, ikke af Time.time.</summary>
        void BuildOrbit(int seed)
        {
            var rng = new System.Random(seed);
            var center = contentOffset + Vector3.up * 0.15f;
            var sun = MakePrimitive(PrimitiveType.Sphere, _contentRoot, center, Vector3.one * 0.06f, new Color(1f, 0.85f, 0.3f));
            sun.AddComponent<ClockBlink>().periodSeconds = 2f;

            int planets = 2 + rng.Next(3);
            for (int i = 0; i < planets; i++)
            {
                var pivot = new GameObject($"Pivot {i}").transform;
                pivot.SetParent(_contentRoot, false);
                pivot.localPosition = center;

                float radius = 0.07f + 0.04f * i;
                float size = 0.015f + (float)rng.NextDouble() * 0.015f;
                MakePrimitive(PrimitiveType.Sphere, pivot, new Vector3(radius, 0f, 0f), Vector3.one * size, Palette[rng.Next(Palette.Length)]);

                var spin = pivot.gameObject.AddComponent<ClockSpin>();
                spin.periodSeconds = 3f + i * 2.5f;
                spin.phaseOffsetSeconds = (float)rng.NextDouble() * spin.periodSeconds;
            }
        }

        /// <summary>
        /// UDVIDELSE C: et level der står direkte i koden.  b=x,y,z,farve/x,y,z,farve/...
        /// x, y, z er heltal i klods-enheder, farve er et bogstav: r g b y v w.
        /// Eksempel på indhold: id=1;model=blocks;b=0,0,0,r/1,0,0,g/0,1,0,b
        /// </summary>
        void BuildBlocks(string spec)
        {
            // UDVIDELSE C – læs spec ("x,y,z,farve/x,y,z,farve/…") og byg klodserne. Se README.
        }

        static Color ColorFromLetter(string s) => s.Trim() switch
        {
            "r" => Palette[0], "b" => Palette[1], "y" => Palette[2],
            "g" => Palette[3], "v" => Palette[4], _ => Palette[5],
        };

        public static GameObject MakePrimitive(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            if (SharedMaterial != null) r.sharedMaterial = SharedMaterial;
            r.material.color = color;
            return go;
        }
    }
}
