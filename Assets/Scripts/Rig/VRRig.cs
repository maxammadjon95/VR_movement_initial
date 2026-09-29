using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRBase
{
    /// <summary>
    /// Root of the player. Holds the references other scripts need, moves/rotates the whole rig,
    /// and asks the headset for a floor-level tracking origin (so head height = your real height).
    /// </summary>
    public class VRRig : MonoBehaviour
    {
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightHand;
        [SerializeField] private Transform _leftAim;
        [SerializeField] private Transform _rightAim;

        private readonly List<XRInputSubsystem> _subsystems = new();
        private bool _floorOriginSet;

        public Transform Head => _head;
        public Transform GetHand(Hand hand) => hand == Hand.Left ? _leftHand : _rightHand;
        public Transform GetAim(Hand hand) => hand == Hand.Left ? _leftAim : _rightAim;

        private void Update()
        {
            // XR may start a few frames after the scene, so keep trying until it works.
            if (!_floorOriginSet)
                TrySetFloorOrigin();
        }

        private void TrySetFloorOrigin()
        {
            SubsystemManager.GetSubsystems(_subsystems);
            foreach (XRInputSubsystem subsystem in _subsystems)
            {
                if (subsystem.running && subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                    _floorOriginSet = true;
            }
        }

        /// <summary>Moves the rig so the player's head ends up standing above <paramref name="floorPoint"/>.</summary>
        public void MoveHeadTo(Vector3 floorPoint)
        {
            Vector3 headOffset = _head.position - transform.position;
            headOffset.y = 0f;
            transform.position = floorPoint - headOffset;

            // CharacterController keeps its own copy of the position; without this its next Move() undoes the jump.
            Physics.SyncTransforms();
        }

        /// <summary>Turns the rig around the head, so the player turns in place instead of swinging around the rig centre.</summary>
        public void RotateAroundHead(float degrees)
        {
            transform.RotateAround(_head.position, Vector3.up, degrees);
            Physics.SyncTransforms();
        }
    }
}
