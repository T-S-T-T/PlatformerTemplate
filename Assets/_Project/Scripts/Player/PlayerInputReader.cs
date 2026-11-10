using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlatformerTemplate
{
    /// <summary>
    /// Gameplay input, built in code so the template needs no .inputactions asset.
    /// Keyboard and gamepad bindings; supports runtime rebinding with persistence.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        const string OverridesKey = "input_overrides";

        InputActionMap map;
        InputAction move, jump, dash, melee, ranged, interact;

        public Vector2 Move { get { return move.ReadValue<Vector2>(); } }
        public bool JumpPressed { get { return jump.WasPressedThisFrame(); } }
        public bool JumpHeld { get { return jump.IsPressed(); } }
        public bool DashPressed { get { return dash.WasPressedThisFrame(); } }
        public bool MeleePressed { get { return melee.WasPressedThisFrame(); } }
        public bool RangedPressed { get { return ranged.WasPressedThisFrame(); } }
        public bool InteractPressed { get { return interact.WasPressedThisFrame(); } }

        void Awake()
        {
            map = new InputActionMap("Gameplay");

            move = map.AddAction("Move", InputActionType.Value, null, null, null, null, "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Gamepad>/dpad/up").With("Down", "<Gamepad>/dpad/down")
                .With("Left", "<Gamepad>/dpad/left").With("Right", "<Gamepad>/dpad/right");

            jump = map.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            dash = map.AddAction("Dash", InputActionType.Button);
            dash.AddBinding("<Keyboard>/leftShift");
            dash.AddBinding("<Gamepad>/buttonWest");

            melee = map.AddAction("Melee", InputActionType.Button);
            melee.AddBinding("<Keyboard>/j");
            melee.AddBinding("<Gamepad>/buttonEast");

            ranged = map.AddAction("Ranged", InputActionType.Button);
            ranged.AddBinding("<Keyboard>/k");
            ranged.AddBinding("<Gamepad>/rightTrigger");

            interact = map.AddAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonNorth");

            LoadOverrides();
        }

        void OnEnable() { if (map != null) map.Enable(); }
        void OnDisable() { if (map != null) map.Disable(); }
        void OnDestroy() { if (map != null) map.Dispose(); }

        public InputAction GetAction(string actionName)
        {
            return map.FindAction(actionName);
        }

        // ------------------------------------------------------------ rebinding

        /// <summary>
        /// Starts an interactive rebind of one binding. Call from a settings UI. onDone(true) on success.
        /// Overrides are saved to PlayerPrefs automatically.
        /// </summary>
        public void StartRebind(string actionName, int bindingIndex, Action<bool> onDone)
        {
            InputAction action = map.FindAction(actionName);
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                if (onDone != null) onDone(false);
                return;
            }

            action.Disable();
            action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op =>
                {
                    op.Dispose();
                    action.Enable();
                    SaveOverrides();
                    if (onDone != null) onDone(true);
                })
                .OnCancel(op =>
                {
                    op.Dispose();
                    action.Enable();
                    if (onDone != null) onDone(false);
                })
                .Start();
        }

        public void ResetBindings()
        {
            map.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(OverridesKey);
        }

        void SaveOverrides()
        {
            PlayerPrefs.SetString(OverridesKey, map.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        void LoadOverrides()
        {
            string json = PlayerPrefs.GetString(OverridesKey, "");
            if (!string.IsNullOrEmpty(json)) map.LoadBindingOverridesFromJson(json);
        }
    }
}
