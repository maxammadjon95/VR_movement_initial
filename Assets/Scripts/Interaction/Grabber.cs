using UnityEngine;

namespace VRBase
{
    /// <summary>
    /// Put on a hand (TrackedPose Grip). Hold the grip button near a Grabbable to pick it up, let go to drop/throw.
    /// </summary>
    public class Grabber : MonoBehaviour
    {
        [SerializeField] private Hand _hand;
        [SerializeField] private float _radius = 0.08f;
        [SerializeField] private LayerMask _grabLayers = ~0;
        [SerializeField] private float _throwMultiplier = 1f;

        private CharacterController _body;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private Vector3 _velocity;
        private Vector3 _angularVelocity;

        public Hand Hand => _hand;
        public Grabbable Held { get; private set; }

        private void Awake()
        {
            _body = GetComponentInParent<CharacterController>();
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
        }

        private void OnDisable() => Drop();

        private void Update()
        {
            TrackVelocity();

            if (VRInput.WasPressed(_hand, VRButton.Grip))
                TryGrab();
            else if (VRInput.WasReleased(_hand, VRButton.Grip))
                Drop();
        }

        private void TryGrab()
        {
            Grabbable closest = null;
            float closestDistance = float.MaxValue;

            foreach (Collider collider in Physics.OverlapSphere(transform.position, _radius, _grabLayers, QueryTriggerInteraction.Collide))
            {
                var grabbable = collider.GetComponentInParent<Grabbable>();
                if (grabbable == null) continue;

                float distance = (grabbable.transform.position - transform.position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closest = grabbable;
                    closestDistance = distance;
                }
            }

            if (closest == null)
                return;

            // Taking it from the other hand.
            if (closest.IsHeld)
                closest.CurrentGrabber.Drop();

            Held = closest;
            IgnoreBodyCollision(Held, true);
            Held.Attach(this);
        }

        public void Drop()
        {
            if (Held == null)
                return;

            Grabbable released = Held;
            Held = null;
            IgnoreBodyCollision(released, false);
            released.Detach(_velocity * _throwMultiplier, _angularVelocity);
        }

        // A held object must not push the player's own body around.
        private void IgnoreBodyCollision(Grabbable grabbable, bool ignore)
        {
            if (_body == null) return;
            foreach (Collider collider in grabbable.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(collider, _body, ignore);
        }

        private void TrackVelocity()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 velocity = (transform.position - _lastPosition) / dt;

            Quaternion delta = transform.rotation * Quaternion.Inverse(_lastRotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            Vector3 angularVelocity = float.IsFinite(axis.x) ? axis * (angle * Mathf.Deg2Rad / dt) : Vector3.zero;

            // Smooth a little so one jittery frame doesn't ruin a throw.
            _velocity = Vector3.Lerp(_velocity, velocity, 0.5f);
            _angularVelocity = Vector3.Lerp(_angularVelocity, angularVelocity, 0.5f);

            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
