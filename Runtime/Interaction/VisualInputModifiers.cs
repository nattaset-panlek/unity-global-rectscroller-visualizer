using UnityEngine;
#if GV_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GlobalVisualizer
{
    internal static class VisualInputModifiers
    {
        #region [Properties]
        public static bool Additive
        {
            get
            {
#if GV_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null) return keyboard.ctrlKey.isPressed || keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
#else
                return false;
#endif
            }
        }

        public static bool Range
        {
            get
            {
#if GV_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null) return keyboard.shiftKey.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
                return false;
#endif
            }
        }
        #endregion
    }
}
