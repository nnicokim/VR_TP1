using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Minigolf
{
    /// <summary>
    /// Shared confirm / charge input for touch, mouse, and VR BOX style Bluetooth controllers.
    /// </summary>
    public static class VrShotInput
    {
        public static bool IsPressed()
        {
            if (TouchPressed())
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;
            return GamepadPressed(press: true, down: false, up: false);
        }

        public static bool WasPressedThisFrame()
        {
            if (TouchDown())
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            return GamepadPressed(press: false, down: true, up: false);
        }

        public static bool WasReleasedThisFrame()
        {
            if (TouchUp())
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                return true;
            return GamepadPressed(press: false, down: false, up: true);
        }

        static bool TouchPressed()
        {
            var touch = Touchscreen.current;
            return touch != null && touch.primaryTouch.press.isPressed;
        }

        static bool TouchDown()
        {
            var touch = Touchscreen.current;
            return touch != null && touch.primaryTouch.press.wasPressedThisFrame;
        }

        static bool TouchUp()
        {
            var touch = Touchscreen.current;
            return touch != null && touch.primaryTouch.press.wasReleasedThisFrame;
        }

        static bool GamepadPressed(bool press, bool down, bool up)
        {
            var pad = Gamepad.current;
            if (pad != null)
            {
                if (CheckButton(pad.buttonSouth, press, down, up)) return true;
                if (CheckButton(pad.buttonEast, press, down, up)) return true;
                if (CheckButton(pad.buttonWest, press, down, up)) return true;
                if (CheckButton(pad.buttonNorth, press, down, up)) return true;
                if (CheckButton(pad.rightShoulder, press, down, up)) return true;
                if (CheckButton(pad.leftShoulder, press, down, up)) return true;
                if (CheckButton(pad.rightTrigger, press, down, up)) return true;
                if (CheckButton(pad.leftTrigger, press, down, up)) return true;
            }

            var joy = Joystick.current;
            if (joy != null && CheckButton(joy.trigger, press, down, up))
                return true;

            // Generic HID / rare VR BOX mappings
            if (Joystick.all.Count > 0)
            {
                foreach (var j in Joystick.all)
                {
                    if (j.trigger != null && CheckButton(j.trigger, press, down, up))
                        return true;
                }
            }

            return false;
        }

        static bool CheckButton(ButtonControl button, bool press, bool down, bool up)
        {
            if (button == null)
                return false;
            if (press && button.isPressed) return true;
            if (down && button.wasPressedThisFrame) return true;
            if (up && button.wasReleasedThisFrame) return true;
            return false;
        }
    }
}
