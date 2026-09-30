using System.Collections.Generic;
using UnityEngine;

namespace VRBase
{
    /// <summary>
    /// Put on a hand (TrackedPose Grip). Hold the grip button near a Grabbable to pick it up, let go to drop/throw.
    /// </summary>
    public class Grabber : MonoBehaviour
    {
        private const int VelocitySamples = 5;

        [SerializeField] private Hand _hand;
        [SerializeField] private float _radius = 0.08f;
        [SerializeField] private LayerMask _grabLayers = ~0;
        [SerializeField] private float _throwMultiplier = 1f;
        [SerializeField] private float _maxThrowSpeed = 12f;

        private CharacterController _body;
        private Transform _rig;

        // Hand motion is measured relative to the rig, so turning/teleporting/walking doesn't count as throwing.
        private readonly Vector3[] _velocities = new Vector3[VelocitySamples];
        private readonly Vector3[] _angularVelocities = new Vector3[VelocitySamples];
        private int _sampleIndex;
        private Vector3 _lastLocalPosition;
        private Quaternion _lastLocalRotation;

        // Released objects keep ignoring the body until they are out of it, otherwise physics shoots them away.
        private readonly List<Grabbable> _leavingBody = new();

        public Hand Hand => _hand;
        public Grabbable Held { get; private set; }

        private void Awake()
        {
            _body = GetComponentInParent<CharacterController>();
            _rig = _body != null ? _body.transform : transform.parent;
        }

        private void OnEnable()
        {
            _lastLocalPosition = LocalPosition();
            _lastLocalRotation = LocalRotation();
        }

        private void OnDisable() => Drop();

        private void Update()
        {
            TrackVelocity();
            RestoreBodyCollisions();

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

            _leavingBody.Remove(closest);
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
            _leavingBody.Add(released);
            released.Detach(ThrowVelocity(), AverageOf(_angularVelocities));
        }

        private Vector3 ThrowVelocity()
        {
            Vector3 velocity = AverageOf(_velocities) * _throwMultiplier;
            return Vector3.ClampMagnitude(velocity, _maxThrowSpeed);
        }

        private void RestoreBodyCollisions()
        {
            if (_body == null) return;

            for (int i = _leavingBody.Count - 1; i >= 0; i--)
            {
                Grabbable grabbable = _leavingBody[i];
                if (grabbable != null && grabbable.IsHeld)
                {
                    _leavingBody.RemoveAt(i);
                    continue;
                }

                if (grabbable == null || !OverlapsBody(grabbable))
                {
                    if (grabbable != null) IgnoreBodyCollision(grabbable, false);
                    _leavingBody.RemoveAt(i);
                }
            }
        }

        private bool OverlapsBody(Grabbable grabbable)
        {
            foreach (Collider collider in grabbable.GetComponentsInChildren<Collider>())
                if (collider.bounds.Intersects(_body.bounds))
                    return true;
            return false;
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

            Vector3 localPosition = LocalPosition();
            Quaternion localRotation = LocalRotation();

            Vector3 velocity = (localPosition - _lastLocalPosition) / dt;

            Quaternion delta = localRotation * Quaternion.Inverse(_lastLocalRotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            Vector3 angularVelocity = float.IsFinite(axis.x) ? axis * (angle * Mathf.Deg2Rad / dt) : Vector3.zero;

            // Stored in world space (rig's current orientation) and averaged over the last frames,
            // so one jittery tracking frame doesn't ruin a throw.
            _velocities[_sampleIndex] = RigToWorld(velocity);
            _angularVelocities[_sampleIndex] = RigToWorld(angularVelocity);
            _sampleIndex = (_sampleIndex + 1) % VelocitySamples;

            _lastLocalPosition = localPosition;
            _lastLocalRotation = localRotation;
        }

        private Vector3 LocalPosition() => _rig != null ? _rig.InverseTransformPoint(transform.position) : transform.position;
        private Quaternion LocalRotation() => _rig != null ? Quaternion.Inverse(_rig.rotation) * transform.rotation : transform.rotation;
        private Vector3 RigToWorld(Vector3 direction) => _rig != null ? _rig.TransformDirection(direction) : direction;

        private static Vector3 AverageOf(Vector3[] samples)
        {
            Vector3 sum = Vector3.zero;
            foreach (Vector3 sample in samples) sum += sample;
            return sum / samples.Length;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
