using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VRBase
{
    /// <summary>
    /// Replaces the default input module on the EventSystem. Sends hover, click and drag events to uGUI
    /// for every UIPointer, using that pointer's hand trigger as the "mouse button".
    /// </summary>
    public class VRUIInputModule : BaseInputModule
    {
        [Tooltip("How far (degrees) the controller must turn while holding the trigger before it counts as a drag. " +
                 "Keeps clicks on buttons inside scroll views working.")]
        [SerializeField] private float _dragAngle = 2f;

        private class PointerState
        {
            public PointerEventData Data;
            public Vector3 PressDirection;
        }

        private readonly Dictionary<UIPointer, PointerState> _states = new();
        private readonly List<UIPointer> _removed = new();

        protected override void OnEnable()
        {
            base.OnEnable();

            // Only one module can run on an EventSystem; turn off the default one.
            foreach (BaseInputModule module in GetComponents<BaseInputModule>())
            {
                if (module == this || !module.enabled) continue;
                module.enabled = false;
                Debug.Log($"VRUIInputModule disabled {module.GetType().Name} on {name}.", this);
            }
        }

        public override void Process()
        {
            for (int i = 0; i < UIPointer.All.Count; i++)
                ProcessPointer(UIPointer.All[i]);

            RemoveDisabledPointers();
        }

        private void ProcessPointer(UIPointer pointer)
        {
            PointerState state = GetState(pointer);
            PointerEventData data = state.Data;
            GameObject target = pointer.CurrentHit.gameObject;

            data.Reset();
            data.pointerCurrentRaycast = pointer.CurrentHit;
            data.position = pointer.ScreenCenter;
            data.delta = Vector2.zero;
            data.button = PointerEventData.InputButton.Left;

            HandlePointerExitAndEnter(data, target);

            if (pointer.WasPressed)
                Press(state, pointer, target);

            Drag(state, pointer);

            if (pointer.WasReleased)
                Release(data, target);
        }

        private void Press(PointerState state, UIPointer pointer, GameObject target)
        {
            PointerEventData data = state.Data;
            state.PressDirection = pointer.Direction;

            data.eligibleForClick = true;
            data.dragging = false;
            data.useDragThreshold = true;
            data.pressPosition = data.position;
            data.pointerPressRaycast = data.pointerCurrentRaycast;

            // Clicking somewhere else deselects the currently selected element (e.g. closes an input field).
            GameObject selectHandler = ExecuteEvents.GetEventHandler<ISelectHandler>(target);
            if (selectHandler != eventSystem.currentSelectedGameObject)
                eventSystem.SetSelectedGameObject(null, data);

            if (target == null)
                return;

            GameObject pressHandler = ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerDownHandler);
            GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);

            data.pointerPress = pressHandler != null ? pressHandler : clickHandler;
            data.rawPointerPress = target;
            data.pointerClick = clickHandler;
            data.clickCount = 1;
            data.clickTime = Time.unscaledTime;

            data.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(target);
            if (data.pointerDrag != null)
                ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.initializePotentialDrag);
        }

        private void Drag(PointerState state, UIPointer pointer)
        {
            PointerEventData data = state.Data;
            if (data.pointerDrag == null || !pointer.IsPressed)
                return;

            if (!data.dragging)
            {
                if (Vector3.Angle(state.PressDirection, pointer.Direction) < _dragAngle)
                    return;

                ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.beginDragHandler);
                data.dragging = true;

                // Dragging something else than what was pressed (e.g. scrolling a list by its button): cancel the click.
                if (data.pointerPress != data.pointerDrag)
                {
                    ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);
                    data.eligibleForClick = false;
                    data.pointerPress = null;
                    data.rawPointerPress = null;
                }
            }

            ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.dragHandler);
        }

        private void Release(PointerEventData data, GameObject target)
        {
            ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);

            GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
            if (data.eligibleForClick && data.pointerClick != null && data.pointerClick == clickHandler)
                ExecuteEvents.Execute(data.pointerClick, data, ExecuteEvents.pointerClickHandler);

            if (data.dragging && data.pointerDrag != null)
            {
                if (target != null)
                    ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.dropHandler);
                ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.endDragHandler);
            }

            data.eligibleForClick = false;
            data.pointerPress = null;
            data.rawPointerPress = null;
            data.pointerClick = null;
            data.pointerDrag = null;
            data.dragging = false;
        }

        private PointerState GetState(UIPointer pointer)
        {
            if (!_states.TryGetValue(pointer, out PointerState state))
            {
                state = new PointerState
                {
                    Data = new PointerEventData(eventSystem) { pointerId = 100 + (int)pointer.Hand },
                };
                _states.Add(pointer, state);
            }
            return state;
        }

        // A pointer that got disabled mid-hover/press must still send exit/up events.
        private void RemoveDisabledPointers()
        {
            _removed.Clear();
            foreach (var pair in _states)
                if (pair.Key == null || !UIPointer.All.Contains(pair.Key))
                    _removed.Add(pair.Key);

            foreach (UIPointer pointer in _removed)
            {
                PointerEventData data = _states[pointer].Data;
                Release(data, null);
                HandlePointerExitAndEnter(data, null);
                _states.Remove(pointer);
            }
        }
    }
}
