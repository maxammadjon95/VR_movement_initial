using UnityEngine;

namespace VRBase
{
    /// <summary>
    /// Keeps the CharacterController capsule under the head (it follows when you physically walk or crouch)
    /// and applies gravity.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class BodyCollider : MonoBehaviour
    {
        [SerializeField] private VRRig _rig;
        [SerializeField] private float _minHeight = 0.5f;
        [SerializeField] private float _maxHeight = 2.2f;
        [SerializeField] private float _gravity = -9.81f;

        private CharacterController _controller;
        private float _verticalSpeed;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_rig == null) _rig = GetComponent<VRRig>();
        }

        private void Update()
        {
            FitToHead();
            ApplyGravity();
        }

        private void FitToHead()
        {
            Vector3 head = _rig.Head.localPosition;
            float height = Mathf.Clamp(head.y, _minHeight, _maxHeight);

            _controller.height = height;
            _controller.center = new Vector3(head.x, height / 2f + _controller.skinWidth, head.z);
        }

        private void ApplyGravity()
        {
            if (_controller.isGrounded && _verticalSpeed < 0f)
                _verticalSpeed = -1f; // small push down keeps isGrounded stable
            else
                _verticalSpeed += _gravity * Time.deltaTime;

            _controller.Move(Vector3.up * (_verticalSpeed * Time.deltaTime));
        }
    }
}
