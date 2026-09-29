using UnityEngine;

namespace VRBase
{
    /// <summary>
    /// Opens/closes a world-space panel with a controller button and places it in front of the player.
    /// Put this on an object that stays active (e.g. the XR Rig), NOT on the panel itself,
    /// otherwise it stops listening once the panel is hidden.
    /// For a Close button on the panel: Button → OnClick → drag this object → MenuPanel.Close.
    /// </summary>
    public class MenuPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _head;
        [SerializeField] private Hand _hand = Hand.Left;
        [SerializeField] private VRButton _button = VRButton.Menu;
        [SerializeField] private float _distance = 1.2f;
        [SerializeField] private float _heightOffset = -0.15f;
        [SerializeField] private bool _openOnStart;

        public bool IsOpen => _panel.activeSelf;

        private void Start()
        {
            if (_openOnStart) Open();
            else Close();
        }

        private void Update()
        {
            if (VRInput.WasPressed(_hand, _button))
                Toggle();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            PlaceInFrontOfHead();
            _panel.SetActive(true);
        }

        public void Close()
        {
            _panel.SetActive(false);
        }

        private void PlaceInFrontOfHead()
        {
            Vector3 forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
            if (forward == Vector3.zero) forward = _head.parent != null ? _head.parent.forward : Vector3.forward;

            _panel.transform.position = _head.position + forward * _distance + Vector3.up * _heightOffset;
            _panel.transform.rotation = Quaternion.LookRotation(forward, Vector3.up); // canvas faces the player
        }
    }
}
