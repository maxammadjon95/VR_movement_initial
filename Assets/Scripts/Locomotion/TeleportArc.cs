using UnityEngine;

namespace VRBase
{
    /// <summary>Draws the teleport arc with a LineRenderer and shows a marker where you will land.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class TeleportArc : MonoBehaviour
    {
        [Tooltip("Object shown at the landing point. It must NOT have a collider.")]
        [SerializeField] private GameObject _marker;
        [SerializeField] private Color _validColor = new(0.2f, 0.9f, 0.3f);
        [SerializeField] private Color _invalidColor = new(0.9f, 0.2f, 0.2f);

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;

            // A collider on the marker would be hit by the arc itself and push the marker towards the player.
            if (_marker != null)
                foreach (Collider collider in _marker.GetComponentsInChildren<Collider>())
                    Destroy(collider);

            Hide();
        }

        public void Show(Vector3[] points, int count, bool valid, Vector3 target)
        {
            _line.enabled = true;
            _line.positionCount = count;
            for (int i = 0; i < count; i++)
                _line.SetPosition(i, points[i]);

            Color color = valid ? _validColor : _invalidColor;
            _line.startColor = color;
            _line.endColor = color;

            if (_marker != null)
            {
                _marker.SetActive(valid);
                if (valid) _marker.transform.position = target;
            }
        }

        public void Hide()
        {
            _line.enabled = false;
            if (_marker != null) _marker.SetActive(false);
        }
    }
}
