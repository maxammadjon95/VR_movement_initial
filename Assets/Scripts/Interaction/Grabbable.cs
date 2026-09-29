using UnityEngine;
using UnityEngine.Events;

namespace VRBase
{
    /// <summary>
    /// Makes an object grabbable by a Grabber. While held it becomes kinematic and follows the hand;
    /// on release it gets the hand's velocity, so it can be thrown.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Grabbable : MonoBehaviour
    {
        public UnityEvent OnGrab;
        public UnityEvent OnRelease;

        private Transform _originalParent;
        private bool _wasKinematic;
        private RigidbodyInterpolation _interpolation;

        public Rigidbody Body { get; private set; }
        public Grabber CurrentGrabber { get; private set; }
        public bool IsHeld => CurrentGrabber != null;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
        }

        // Called by Grabber. Use OnGrab / OnRelease for your own logic instead of calling these.
        public void Attach(Grabber grabber)
        {
            CurrentGrabber = grabber;
            _originalParent = transform.parent;
            _wasKinematic = Body.isKinematic;
            _interpolation = Body.interpolation;

            Body.isKinematic = true;
            Body.interpolation = RigidbodyInterpolation.None; // interpolation fights the parent's movement
            transform.SetParent(grabber.transform, worldPositionStays: true);

            OnGrab.Invoke();
        }

        public void Detach(Vector3 velocity, Vector3 angularVelocity)
        {
            transform.SetParent(_originalParent, worldPositionStays: true);
            Body.isKinematic = _wasKinematic;
            Body.interpolation = _interpolation;

            if (!Body.isKinematic)
            {
                Body.linearVelocity = velocity;
                Body.angularVelocity = angularVelocity;
            }

            CurrentGrabber = null;
            OnRelease.Invoke();
        }
    }
}
