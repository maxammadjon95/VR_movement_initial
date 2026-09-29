using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRBase
{
    /// <summary>
    /// The single place all controller input comes from.
    ///
    /// Actions are created in code and enabled before the first scene loads, so nothing has to be set up
    /// in the scene and it does not matter when the controllers connect (Editor over Link or APK on the headset).
    ///
    /// Two ways to use it:
    ///   Polling:  if (VRInput.WasPressed(Hand.Right, VRButton.Primary)) { ... }
    ///   Events:   VRInput.ButtonPressed += (hand, button) => { ... };
    /// </summary>
    public static class VRInput
    {
        public static event Action<Hand, VRButton> ButtonPressed;
        public static event Action<Hand, VRButton> ButtonReleased;

        private static readonly int ButtonCount = Enum.GetValues(typeof(VRButton)).Length;

        private static InputAction[,] _buttons;
        private static InputAction[] _sticks;
        private static InputAction[] _triggers;
        private static InputAction[] _grips;

        public static bool IsPressed(Hand hand, VRButton button) => _buttons[(int)hand, (int)button].IsPressed();
        public static bool WasPressed(Hand hand, VRButton button) => _buttons[(int)hand, (int)button].WasPressedThisFrame();
        public static bool WasReleased(Hand hand, VRButton button) => _buttons[(int)hand, (int)button].WasReleasedThisFrame();

        /// <summary>Thumbstick, -1..1 on both axes.</summary>
        public static Vector2 Stick(Hand hand) => _sticks[(int)hand].ReadValue<Vector2>();

        /// <summary>How far the trigger is pulled, 0..1.</summary>
        public static float Trigger(Hand hand) => _triggers[(int)hand].ReadValue<float>();

        /// <summary>How far the grip is squeezed, 0..1.</summary>
        public static float Grip(Hand hand) => _grips[(int)hand].ReadValue<float>();

        /// <summary>Input System path of a controller, e.g. "&lt;XRController&gt;{LeftHand}".</summary>
        public static string DevicePath(Hand hand) =>
            hand == Hand.Left ? "<XRController>{LeftHand}" : "<XRController>{RightHand}";

        // Usages are generic names every OpenXR controller profile maps its controls to,
        // so these bindings work for Touch (Quest 2), Touch Plus (Quest 3/3S) and Touch Pro.
        private static string Usage(VRButton button) => button switch
        {
            VRButton.Trigger => "{TriggerButton}",
            VRButton.Grip => "{GripButton}",
            VRButton.Primary => "{PrimaryButton}",
            VRButton.Secondary => "{SecondaryButton}",
            VRButton.StickClick => "{Primary2DAxisClick}",
            VRButton.Menu => "{MenuButton}",
            _ => throw new ArgumentOutOfRangeException(nameof(button)),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Clean up the previous play session in case "Enter Play Mode Options" skips domain reload.
            Dispose();
            ButtonPressed = null;
            ButtonReleased = null;

            _buttons = new InputAction[2, ButtonCount];
            _sticks = new InputAction[2];
            _triggers = new InputAction[2];
            _grips = new InputAction[2];

            for (int h = 0; h < 2; h++)
            {
                Hand hand = (Hand)h;
                string device = DevicePath(hand);

                for (int b = 0; b < ButtonCount; b++)
                {
                    VRButton button = (VRButton)b;
                    var action = new InputAction($"{hand} {button}", InputActionType.Button, $"{device}/{Usage(button)}");
                    action.performed += _ => ButtonPressed?.Invoke(hand, button);
                    action.canceled += _ => ButtonReleased?.Invoke(hand, button);
                    action.Enable();
                    _buttons[h, b] = action;
                }

                _sticks[h] = CreateValueAction($"{hand} Stick", $"{device}/{{Primary2DAxis}}");
                _triggers[h] = CreateValueAction($"{hand} Trigger", $"{device}/{{Trigger}}");
                _grips[h] = CreateValueAction($"{hand} Grip", $"{device}/{{Grip}}");
            }

            Application.quitting += Dispose;
        }

        private static InputAction CreateValueAction(string name, string binding)
        {
            var action = new InputAction(name, InputActionType.Value, binding);
            action.Enable();
            return action;
        }

        private static void Dispose()
        {
            Application.quitting -= Dispose;

            if (_buttons != null)
                foreach (InputAction action in _buttons)
                    action?.Dispose();

            DisposeAll(_sticks);
            DisposeAll(_triggers);
            DisposeAll(_grips);
        }

        private static void DisposeAll(InputAction[] actions)
        {
            if (actions == null) return;
            foreach (InputAction action in actions)
                action?.Dispose();
        }
    }
}
