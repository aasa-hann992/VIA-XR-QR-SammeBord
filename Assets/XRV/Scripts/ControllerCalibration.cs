using UnityEngine;

namespace Xrv.Qr
{
    /// <summary>
    /// QUEST PRO-VARIANT / UDVIDELSE B - "Kalibrér med controlleren".
    /// Quest Pro kan ikke læse QR-koder med MRUK. Men man kan stadig dele et nulpunkt uden netværk:
    /// begge spillere rører de to printede kryds på det SAMME ark med spidsen af højre controller.
    /// Det er den måde mange location-based VR-steder kalibrerer på.
    ///
    /// Tryk A med controllerens spids på VENSTRE kryds, derefter A på HØJRE kryds.
    /// Læg scriptet på samme GameObject som SharedOrigin.
    /// </summary>
    [RequireComponent(typeof(SharedOrigin))]
    public class ControllerCalibration : MonoBehaviour
    {
        [Tooltip("Hvor sidder controllerens 'spids' i forhold til controller-ankeret? Justér til den lilla kugle sidder hvor du rører.")]
        public Vector3 tipOffset = new Vector3(0f, -0.02f, 0.05f);

        [Tooltip("Krydsenes position på arket, målt fra QR-kodens midte (samme tal som i PayloadContent)")]
        public Vector3 crossLeft = new Vector3(-0.07f, 0f, -0.085f);
        public Vector3 crossRight = new Vector3(0.07f, 0f, -0.085f);

        [Tooltip("Uden QR-læsning må begge spillere aftale indholdet - skriv det samme her (det dekodede indhold, uden XRV1:)")]
        public string manualPayload = "id=1;model=tower;seed=1;h=8";

        SharedOrigin _origin;
        Transform _hand;
        Transform _tip;
        Vector3? _left;

        void Awake() => _origin = GetComponent<SharedOrigin>();

        void Start()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig == null)
            {
                Debug.LogError("[ControllerCalibration] Fandt ingen OVRCameraRig.");
                enabled = false;
                return;
            }
            _hand = rig.rightControllerAnchor;

            // En lille kugle der viser hvor "spidsen" er, så man kan justere tipOffset.
            _tip = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            Destroy(_tip.GetComponent<Collider>());
            _tip.localScale = Vector3.one * 0.008f;
            _tip.GetComponent<Renderer>().material.color = new Color(0.7f, 0.3f, 1f);
        }

        void Update()
        {
            if (_hand == null) return;
            Vector3 tip = _hand.TransformPoint(tipOffset);
            _tip.position = tip;

            if (!OVRInput.GetDown(OVRInput.Button.One)) return;

            if (_left == null)
            {
                _left = tip;
                Debug.Log("[ControllerCalibration] Venstre kryds gemt. Rør nu det højre kryds og tryk A.");
                return;
            }

            Vector3 l = _left.Value;
            Vector3 r = tip;
            _left = null;

            Vector3 right = Vector3.ProjectOnPlane(r - l, Vector3.up).normalized;
            Vector3 forward = Vector3.Cross(right, Vector3.up);
            var rot = Quaternion.LookRotation(forward, Vector3.up);

            // Midtpunktet mellem krydsene kender vi i rum-koordinater -> regn tilbage til QR-kodens midte.
            Vector3 midRoom = (crossLeft + crossRight) * 0.5f;
            Vector3 midWorld = (l + r) * 0.5f;
            Vector3 origin = midWorld - rot * midRoom;

            float seen = Vector3.Distance(l, r);
            float expected = Vector3.Distance(crossLeft, crossRight);
            Debug.Log($"[ControllerCalibration] Afstand mellem krydsene: {seen * 100f:0.0} cm (forventet {expected * 100f:0.0} cm)");

            _origin.LockAt(new Pose(origin, rot), manualPayload);
        }
    }
}
