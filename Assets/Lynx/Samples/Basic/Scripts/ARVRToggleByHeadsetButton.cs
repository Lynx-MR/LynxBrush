using System.Collections.Generic;
using UnityEngine;

namespace Lynx
{
    public class ARVRToggleByHeadsetButton : MonoBehaviour
    {
#if UNITY_EDITOR
        public const KeyCode LYNX_BUTTON = KeyCode.Space;
#else
        public const KeyCode LYNX_BUTTON = KeyCode.JoystickButton0;
#endif
        private void Update()
        {
            if (Input.GetKeyUp(LYNX_BUTTON))
            {
                LynxAPI.ToggleAROnly();
            }
        }
    }
}