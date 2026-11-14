using UnityEngine;

namespace Xrv.Qr
{
    /// <summary>Drejer objektet rundt om en akse ud fra det fælles ur.</summary>
    public class ClockSpin : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float periodSeconds = 8f;
        public float phaseOffsetSeconds;
        Quaternion _start;

        void Awake() => _start = transform.localRotation;

        void Update()
        {
            float angle = 360f * SharedClock.Phase(periodSeconds, phaseOffsetSeconds);
            transform.localRotation = Quaternion.AngleAxis(angle, axis) * _start;
        }
    }
}
