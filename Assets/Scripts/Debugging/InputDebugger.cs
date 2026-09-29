using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRBase
{
    /// <summary>
    /// Logs every button press/release (short or long) and which XR devices are connected.
    /// Optionally swaps a renderer's material while its button is held, to see input inside the headset.
    /// On the Quest, read the logs with: adb logcat -s Unity
    /// </summary>
    public class InputDebugger : MonoBehaviour
    {
        [Serializable]
        private class ButtonHighlight
        {
            public Hand Hand;
            public VRButton Button;
            public Renderer Renderer;
        }

        [SerializeField] private float _longPressTime = 0.5f;
        [SerializeField] private Material _pressedMaterial;
        [SerializeField] private ButtonHighlight[] _highlights = Array.Empty<ButtonHighlight>();

        private readonly Dictionary<(Hand, VRButton), float> _pressTimes = new();
        private readonly Dictionary<Renderer, Material> _originalMaterials = new();

        private void OnEnable()
        {
            VRInput.ButtonPressed += OnPressed;
            VRInput.ButtonReleased += OnReleased;
            InputSystem.onDeviceChange += OnDeviceChange;

            foreach (InputDevice device in InputSystem.devices)
                if (device is TrackedDevice)
                    Debug.Log($"[VRInput] Connected: {device.displayName} ({device.layout}) usages: {string.Join(",", device.usages)}");
        }

        private void OnDisable()
        {
            VRInput.ButtonPressed -= OnPressed;
            VRInput.ButtonReleased -= OnReleased;
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void OnPressed(Hand hand, VRButton button)
        {
            _pressTimes[(hand, button)] = Time.unscaledTime;
            Debug.Log($"[VRInput] {GetName(hand, button)} pressed");
            SetHighlight(hand, button, true);
        }

        private void OnReleased(Hand hand, VRButton button)
        {
            float held = _pressTimes.TryGetValue((hand, button), out float start) ? Time.unscaledTime - start : 0f;
            string kind = held >= _longPressTime ? "long" : "short";
            Debug.Log($"[VRInput] {GetName(hand, button)} released ({kind} press, {held:0.00}s)");
            SetHighlight(hand, button, false);
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is TrackedDevice)
                Debug.Log($"[VRInput] {change}: {device.displayName} ({device.layout})");
        }

        private void SetHighlight(Hand hand, VRButton button, bool pressed)
        {
            if (_pressedMaterial == null) return;

            foreach (ButtonHighlight highlight in _highlights)
            {
                if (highlight.Hand != hand || highlight.Button != button || highlight.Renderer == null)
                    continue;

                Renderer renderer = highlight.Renderer;
                if (pressed)
                {
                    _originalMaterials.TryAdd(renderer, renderer.sharedMaterial);
                    renderer.sharedMaterial = _pressedMaterial;
                }
                else if (_originalMaterials.TryGetValue(renderer, out Material original))
                {
                    renderer.sharedMaterial = original;
                }
            }
        }

        private static string GetName(Hand hand, VRButton button) => button switch
        {
            VRButton.Primary => hand == Hand.Left ? "X" : "A",
            VRButton.Secondary => hand == Hand.Left ? "Y" : "B",
            _ => $"{hand} {button}",
        };
    }
}
