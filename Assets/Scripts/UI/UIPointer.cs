using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRBase
{
    /// <summary>
    /// A ray from the controller (put it on a TrackedPose Aim object). It finds which UI element on a VRCanvas
    /// it points at and shows a line only while it hits one. VRUIInputModule turns this into hover/click/drag.
    ///
    /// Trick: a hidden, never-rendering camera sits on the controller looking along the ray. The ray is then
    /// always the centre of that camera's "screen", which lets normal uGUI code (Slider, ScrollRect) work unchanged.
    /// </summary>
    [DefaultExecutionOrder(-50)] // raycast before the EventSystem processes input
    [RequireComponent(typeof(LineRenderer))]
    public class UIPointer : BaseRaycaster
    {
        public static readonly List<UIPointer> All = new();

        [SerializeField] private Hand _hand;
        [SerializeField] private float _maxDistance = 10f;
        [Tooltip("Optional dot shown where the ray hits. It must NOT have a collider.")]
        [SerializeField] private Transform _cursor;

        private Camera _eventCamera;
        private LineRenderer _line;

        public Hand Hand => _hand;
        public RaycastResult CurrentHit { get; private set; }
        public bool HasHit => CurrentHit.gameObject != null;
        public Vector3 Direction => transform.forward;
        public Vector2 ScreenCenter => new(_eventCamera.pixelWidth * 0.5f, _eventCamera.pixelHeight * 0.5f);

        public bool WasPressed => VRInput.WasPressed(_hand, VRButton.Trigger);
        public bool WasReleased => VRInput.WasReleased(_hand, VRButton.Trigger);
        public bool IsPressed => VRInput.IsPressed(_hand, VRButton.Trigger);

        public override Camera eventCamera => _eventCamera;

        protected override void Awake()
        {
            base.Awake();

            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = false; // local space: the line follows the controller with no lag
            _line.positionCount = 2;

            var cameraObject = new GameObject("UI Event Camera");
            cameraObject.transform.SetParent(transform, worldPositionStays: false);
            _eventCamera = cameraObject.AddComponent<Camera>();
            _eventCamera.enabled = false; // never renders, only used for maths
            _eventCamera.stereoTargetEye = StereoTargetEyeMask.None;
            _eventCamera.cullingMask = 0;
            _eventCamera.clearFlags = CameraClearFlags.Nothing;
            _eventCamera.nearClipPlane = 0.01f;
            _eventCamera.farClipPlane = _maxDistance;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            All.Add(this);
        }

        protected override void OnDisable()
        {
            All.Remove(this);
            CurrentHit = default;
            SetVisible(false);
            base.OnDisable();
        }

        private void Update() => FindHit();

        private void LateUpdate()
        {
            SetVisible(HasHit);
            if (!HasHit) return;

            Vector3 end = Vector3.forward * CurrentHit.distance;
            _line.SetPosition(0, Vector3.zero);
            _line.SetPosition(1, end);
            if (_cursor != null) _cursor.localPosition = end;
        }

        private void SetVisible(bool visible)
        {
            if (_line != null) _line.enabled = visible;
            if (_cursor != null) _cursor.gameObject.SetActive(visible);
        }

        private void FindHit()
        {
            var ray = new Ray(transform.position, transform.forward);
            Vector2 center = ScreenCenter;

            Graphic best = null;
            float bestDistance = _maxDistance;

            foreach (VRCanvas vrCanvas in VRCanvas.All)
            {
                IList<Graphic> graphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(vrCanvas.Canvas);
                for (int i = 0; i < graphics.Count; i++)
                {
                    Graphic graphic = graphics[i];
                    if (graphic.depth == -1 || graphic.canvasRenderer.cull)
                        continue;

                    RectTransform rect = graphic.rectTransform;

                    // Ignore the back side of the canvas.
                    if (Vector3.Dot(ray.direction, rect.forward) <= 0f)
                        continue;

                    if (!new Plane(rect.forward, rect.position).Raycast(ray, out float distance))
                        continue;

                    // Same distance (same canvas): the one drawn on top wins.
                    bool closer = distance < bestDistance - 0.001f;
                    bool onTop = best != null && Mathf.Abs(distance - bestDistance) <= 0.001f && graphic.depth > best.depth;
                    if (!closer && !onTop)
                        continue;

                    if (!RectTransformUtility.RectangleContainsScreenPoint(rect, center, _eventCamera, graphic.raycastPadding))
                        continue;

                    // Respects masks, CanvasGroup.blocksRaycasts, etc.
                    if (!graphic.Raycast(center, _eventCamera))
                        continue;

                    best = graphic;
                    bestDistance = distance;
                }
            }

            if (best == null)
            {
                CurrentHit = default;
                return;
            }

            CurrentHit = new RaycastResult
            {
                gameObject = best.gameObject,
                module = this,
                distance = bestDistance,
                worldPosition = ray.GetPoint(bestDistance),
                worldNormal = -best.rectTransform.forward,
                screenPosition = center,
                depth = best.depth,
                sortingLayer = best.canvas.sortingLayerID,
                sortingOrder = best.canvas.sortingOrder,
            };
        }

        // Required by BaseRaycaster. Our input module reads CurrentHit directly, so nothing to do here.
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) { }
    }
}
