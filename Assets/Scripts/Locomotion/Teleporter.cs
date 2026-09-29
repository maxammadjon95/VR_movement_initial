using UnityEngine;

namespace VRBase
{
    /// <summary>
    /// Push the stick forward to aim an arc, release it to teleport to where the arc lands.
    /// Only surfaces on the teleport layers that are flat enough are valid targets.
    /// This script only decides where to go; TeleportArc draws it.
    /// </summary>
    public class Teleporter : MonoBehaviour
    {
        [SerializeField] private VRRig _rig;
        [SerializeField] private TeleportArc _arc;
        [SerializeField] private Hand _hand = Hand.Right;

        [Header("Targets")]
        [Tooltip("Layers you can teleport onto (e.g. Teleport).")]
        [SerializeField] private LayerMask _teleportLayers;
        [Tooltip("Layers that stop the arc (walls etc.). Untick Grabbable and the rig's own layer.")]
        [SerializeField] private LayerMask _blockingLayers = Physics.DefaultRaycastLayers;
        [SerializeField] private float _maxSlope = 30f;

        [Header("Arc")]
        [SerializeField] private float _arcSpeed = 8f;
        [SerializeField] private int _maxSegments = 40;
        [SerializeField] private float _segmentTime = 0.05f;

        [Header("Stick")]
        [SerializeField, Range(0f, 1f)] private float _pressThreshold = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _releaseThreshold = 0.3f;

        private Vector3[] _points;
        private bool _hasTarget;
        private Vector3 _target;

        public bool IsAiming { get; private set; }

        private void Awake()
        {
            if (_rig == null) _rig = GetComponent<VRRig>();
            _points = new Vector3[_maxSegments + 1];
        }

        private void Update()
        {
            float stickY = VRInput.Stick(_hand).y;

            if (!IsAiming && stickY > _pressThreshold)
            {
                IsAiming = true;
            }
            else if (IsAiming && stickY < _releaseThreshold)
            {
                if (_hasTarget)
                    _rig.MoveHeadTo(_target);

                IsAiming = false;
                _hasTarget = false;
                _arc.Hide();
            }

            if (IsAiming)
                Aim();
        }

        private void Aim()
        {
            Transform aim = _rig.GetAim(_hand);
            Vector3 position = aim.position;
            Vector3 velocity = aim.forward * _arcSpeed;

            _points[0] = position;
            int count = 1;
            _hasTarget = false;

            // Simulate a thrown ball in small steps and stop at the first thing it hits.
            for (int i = 0; i < _maxSegments; i++)
            {
                Vector3 next = position + velocity * _segmentTime;
                velocity += Physics.gravity * _segmentTime;

                if (Physics.Linecast(position, next, out RaycastHit hit, _blockingLayers, QueryTriggerInteraction.Ignore))
                {
                    _points[count++] = hit.point;
                    _hasTarget = IsValidTarget(hit);
                    _target = hit.point;
                    break;
                }

                _points[count++] = next;
                position = next;
            }

            _arc.Show(_points, count, _hasTarget, _target);
        }

        private bool IsValidTarget(RaycastHit hit)
        {
            bool onTeleportLayer = (_teleportLayers.value & (1 << hit.collider.gameObject.layer)) != 0;
            return onTeleportLayer && Vector3.Angle(hit.normal, Vector3.up) <= _maxSlope;
        }
    }
}
