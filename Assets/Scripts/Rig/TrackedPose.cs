using UnityEngine;
using UnityEngine.InputSystem;

namespace VRBase
{
    /// <summary>
    /// Moves this transform to the tracked pose of the headset or a controller.
    /// Put it on a direct child of the VRRig: poses are in tracking space, so they are applied as local position/rotation.
    ///
    /// Grip = where the hand holds the controller (use for hands and grabbing).
    /// Aim  = the pointing ray coming out of the front of the controller (use for rays: UI, teleport).
    /// </summary>
    public class TrackedPose : MonoBehaviour
    {
        public enum Source
        {
            Head,
            LeftGrip,
            RightGrip,
            LeftAim,
            RightAim,
        }

        [SerializeField] private Source _source;

        private InputAction _position;
        private InputAction _rotation;

        private void Awake()
        {
            (string position, string rotation) = GetBindings(_source);
            _position = new InputAction($"{_source} Position", InputActionType.Value, position, expectedControlType: "Vector3");
            _rotation = new InputAction($"{_source} Rotation", InputActionType.Value, rotation, expectedControlType: "Quaternion");
        }

        private void OnEnable()
        {
            _position.Enable();
            _rotation.Enable();
            Application.onBeforeRender += UpdatePose;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= UpdatePose;
            _position.Disable();
            _rotation.Disable();
        }

        private void OnDestroy()
        {
            _position.Dispose();
            _rotation.Dispose();
        }

        // Updated twice per frame: in Update for gameplay code,
        // and right before rendering so the camera and hands use the freshest pose (less lag).
        private void Update() => UpdatePose();

        private void UpdatePose()
        {
            // Device not connected yet: keep the last pose instead of snapping to zero.
            if (_position.controls.Count == 0 || _rotation.controls.Count == 0)
                return;

            Quaternion rotation = _rotation.ReadValue<Quaternion>();
            if (Quaternion.Dot(rotation, rotation) < 0.5f) // all zeros = not tracked yet
                return;

            transform.SetLocalPositionAndRotation(_position.ReadValue<Vector3>(), rotation);
        }

        private static (string position, string rotation) GetBindings(Source source)
        {
            switch (source)
            {
                case Source.Head:
                    return ("<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");
                case Source.LeftGrip:
                    return Controller(Hand.Left, "device");
                case Source.RightGrip:
                    return Controller(Hand.Right, "device");
                case Source.LeftAim:
                    return Controller(Hand.Left, "pointer");
                default:
                    return Controller(Hand.Right, "pointer");
            }
        }

        private static (string, string) Controller(Hand hand, string pose)
        {
            string device = VRInput.DevicePath(hand);
            return ($"{device}/{pose}Position", $"{device}/{pose}Rotation");
        }
    }
}
