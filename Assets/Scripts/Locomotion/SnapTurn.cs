using UnityEngine;

namespace VRBase
{
    /// <summary>
    /// Turns the player by a fixed angle when the stick is pushed left/right.
    /// One push = one turn; the stick has to come back to centre before the next turn.
    /// </summary>
    public class SnapTurn : MonoBehaviour
    {
        [SerializeField] private VRRig _rig;
        [SerializeField] private Hand _hand = Hand.Right;
        [SerializeField] private float _angle = 45f;
        [SerializeField, Range(0f, 1f)] private float _pressThreshold = 0.75f;
        [SerializeField, Range(0f, 1f)] private float _releaseThreshold = 0.3f;

        private bool _waitingForRelease;

        private void Awake()
        {
            if (_rig == null) _rig = GetComponent<VRRig>();
        }

        private void Update()
        {
            Vector2 input = VRInput.Stick(_hand);

            if (_waitingForRelease)
            {
                if (Mathf.Abs(input.x) < _releaseThreshold)
                    _waitingForRelease = false;
                return;
            }

            // Must be mostly sideways, so pushing the stick forward for teleport never turns you.
            if (Mathf.Abs(input.x) > _pressThreshold && Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                _rig.RotateAroundHead(Mathf.Sign(input.x) * _angle);
                _waitingForRelease = true;
            }
        }
    }
}
