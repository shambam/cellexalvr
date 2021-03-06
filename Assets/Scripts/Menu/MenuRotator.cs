using UnityEngine;
using System.Collections;

namespace CellexalVR.Menu
{

    /// <summary>
    /// Holds the logic for rotating the menu.
    /// </summary>
    public class MenuRotator : MonoBehaviour
    {
        public Rotation SideFacingPlayer { get; set; }
        public Material menuMaterial;

        private bool isRotating = false;
        private Vector3 fromAngle;
        private float rotatedTotal;
        public enum Rotation { Front, Right, Back, Left }

        void Start()
        {
            // Reset rotation in case it is changed in the editor.
            transform.localRotation = Quaternion.identity;
            SideFacingPlayer = Rotation.Front;
        }
        /// <summary>
        /// Editor script to ensure all menus and submenus have the same material without having to populate manually. 
        /// </summary>
        public void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                foreach (Renderer rend in GetComponentsInChildren<Renderer>())
                {
                    string[] name = rend.gameObject.name.Split(null);
                    if (name[name.Length - 1].Equals("Menu") || name[name.Length - 1].Equals("Box"))
                    {
                        rend.material = menuMaterial;
                    }
                }
            }
        }

        /// <summary>
        /// Rotates the menu 90 degrees to the right.
        /// </summary>
        public void RotateRight(int times = 1)
        {
            if (!isRotating)
            {
                switch (SideFacingPlayer)
                {
                    case Rotation.Front:
                        SideFacingPlayer = Rotation.Left;
                        break;
                    case Rotation.Left:
                        SideFacingPlayer = Rotation.Back;
                        break;
                    case Rotation.Back:
                        SideFacingPlayer = Rotation.Right;
                        break;
                    case Rotation.Right:
                        SideFacingPlayer = Rotation.Front;
                        break;
                }

                StartCoroutine(RotateMe(-90f * times, 0.15f));
            }
        }

        /// <summary>
        /// Rotates the menu 90 degrees to the left.
        /// </summary>
        public void RotateLeft(int times = 1)
        {
            if (!isRotating)
            {
                switch (SideFacingPlayer)
                {
                    case Rotation.Front:
                        SideFacingPlayer = Rotation.Right;
                        break;
                    case Rotation.Right:
                        SideFacingPlayer = Rotation.Back;
                        break;
                    case Rotation.Back:
                        SideFacingPlayer = Rotation.Left;
                        break;
                    case Rotation.Left:
                        SideFacingPlayer = Rotation.Front;
                        break;
                }
                StartCoroutine(RotateMe(90f * times, 0.15f));
            }
        }


        protected virtual void OnDisable()
        {
            if (isRotating)
            {
                if (rotatedTotal < 0)
                {
                    transform.Rotate(0, 0, -180 - rotatedTotal);
                }
                else
                {
                    transform.Rotate(0, 0, 180 - rotatedTotal);
                }
                isRotating = false;
            }
        }
        /// <summary>
        /// Rotates the menu.
        /// </summary>
        /// <param name="zAngles"> The amount of degrees it should be rotated. Positive values rotate the menu clockwise, negative values rotate it counter-clockwise. </param>
        /// <param name="inTime"> THe number of seconds it should take the menu to rotate the specified degrees. </param>
        IEnumerator RotateMe(float zAngles, float inTime)
        {
            isRotating = true;
            // how much we have rotated so far
            rotatedTotal = 0;
            float zAnglesAbs = Mathf.Abs(zAngles);
            // how much we should rotate each frame
            float rotationPerFrame = zAngles / (zAnglesAbs * inTime);
            while (rotatedTotal < zAnglesAbs && rotatedTotal > -zAnglesAbs)
            {
                rotatedTotal += rotationPerFrame;
                // if we are about to rotate it too far
                if (rotatedTotal > zAnglesAbs || rotatedTotal < -zAnglesAbs)
                {
                    // only rotate the menu as much as there is left to rotate
                    transform.Rotate(0, 0, rotationPerFrame - (rotatedTotal - zAngles));
                }
                else
                {
                    transform.Rotate(0, 0, rotationPerFrame);
                }
                yield return null;
            }
            isRotating = false;
        }
    }

}