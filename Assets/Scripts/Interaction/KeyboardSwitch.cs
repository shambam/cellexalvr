using UnityEngine;
namespace CellexalVR.Interaction
{
    /// <summary>
    /// This class turns off the keyboard.
    /// </summary>
    public class KeyboardSwitch : MonoBehaviour
    {

        public bool KeyboardActive { get; set; }

        void Start()
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
                t.gameObject.SetActive(visible);
            }
        }
    }
}