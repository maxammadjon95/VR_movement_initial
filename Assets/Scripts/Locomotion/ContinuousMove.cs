using UnityEngine;

namespace VRBase
{
    /// <summary>Smooth walking with a thumbstick, in the direction the head is looking.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class ContinuousMove : MonoBehaviour
    {
        [SerializeField] private VRRig _rig;
        [SerializeField] private Hand _hand = Hand.Left;
        [SerializeField] private float _speed = 2f;
        [SerializeField] private float _deadZone = 0.15f;

        private CharacterController _controller;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_rig == null) _rig = GetComponent<VRRig>();
        }

        private void Update()
        {
            Vector2 input = Vector2.ClampMagnitude(VRInput.Stick(_hand), 1f);
            if (input.magnitude < _deadZone)
                return;

            Vector3 forward = Vector3.ProjectOnPlane(_rig.Head.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 move = (forward * input.y + right * input.x) * _speed;

            _controller.Move(move * Time.deltaTime);
        }
    }
}
