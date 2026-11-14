using UnityEngine;

namespace Xrv.Qr
{
    /// <summary>
    /// Skifter farve hvert hele sekund. Sæt to headsets ved siden af hinanden (eller se dem på partnerens
    /// cast) og se om de blinker samtidig. Forskellen er urets fejl mellem de to enheder.
    /// </summary>
    public class ClockBlink : MonoBehaviour
    {
        public Color a = Color.white;
        public Color b = new Color(1f, 0.3f, 0.1f);
        public float periodSeconds = 1f;
        Renderer _renderer;

        void Awake() => _renderer = GetComponent<Renderer>();

        void Update()
        {
            if (_renderer == null) return;
            _renderer.material.color = SharedClock.Phase(periodSeconds) < 0.5f ? a : b;
        }
    }
}
