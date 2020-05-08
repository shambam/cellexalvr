using CellexalVR.General;
using UnityEngine;
namespace CellexalVR.Interaction
{
    /// <summary>
    /// This class turns off the keyboard.
    /// </summary>
    public class KeyboardSwitch : MonoBehaviour
    {
        public bool KeyboardActive { get; set; }

        private void Start()
        {
            SetKeyboardVisible(false);
        }

        /// <summary>
        /// Sets the keyboard to be either visible or invisible.
        /// </summary>
        /// <param name="visible">True if the keyboard should be visible, false for invisible.</param>
        public void SetKeyboardVisible(bool visible)
        {
            KeyboardActive = visible;
            foreach (Transform t in transform)
            {
                // session history list is child to keyboardhandler to handle materials and raycasting but should not be toggled together with other keyboard stuff.
                // if (t.gameObject.GetComponent<SessionHistoryList>()) return;
                t.gameObject.SetActive(visible);
            }
        }
    }
}