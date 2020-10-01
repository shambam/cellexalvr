using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using CellexalVR.Menu.SubMenus;
using CellexalVR.Multiuser;
using System;
using System.Diagnostics;
using System.Linq;
using DefaultNamespace;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using VRTK;

namespace CellexalVR.Interaction
{
    /// <summary>
    /// This class sole purpose is to forward collision events to the selection tool handler
    /// </summary>
    public class SelectionToolCollider : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public SelectionManager selectionManager;
        public ParticleSystem particles;
        public Material selectionToolMaterial;
        public Collider[] selectionToolColliders;
        public Color[] Colors;
        public Sprite[] selectionToolShapeButtons;
        public bool hapticFeedbackThisFrame = true;

        private int currentMeshIndex;
        private int tempColorIndex;

        /// <summary>
        /// 0: paddle, 1: bludgeon, 2: smaller bludgeon, 4: stick, 5-8: same shapes but removes selected points instead (white color).
        /// </summary>
        public int CurrentMeshIndex
        {
            get => currentMeshIndex;
            set
            {
                currentMeshIndex = value;
                if (currentMeshIndex >= selectionToolColliders.Length)
                {
                    currentMeshIndex = 0;
                    if (CurrentColorIndex > Colors.Length) CurrentColorIndex = tempColorIndex;
                }
                // Change to remove selection tool: Store old color index and change to white color.
                else if (currentMeshIndex > (selectionToolColliders.Length / 2) - 1 &&
                         CurrentColorIndex <= Colors.Length)
                {
                    tempColorIndex = CurrentColorIndex;
                    CurrentColorIndex = Colors.Length + 1;
                    ParticleSystem.MainModule main = particles.main;
                    main.startColor = Color.white;
                }
                // Change to remove selection tool: Store color index so when we switch back to normal selection tool we get the correct color.
                else if (currentMeshIndex < 0)
                {
                    currentMeshIndex = selectionToolColliders.Length - 1;
                    if (CurrentColorIndex > Colors.Length)
                    {
                        CurrentColorIndex = tempColorIndex;
                    }

                    else
                    {
                        tempColorIndex = CurrentColorIndex;
                        CurrentColorIndex = Colors.Length + 1;
                    }
                }
                // Get back to normal mesh and switch back to color we had before.
                else if ((currentMeshIndex >= 0 && currentMeshIndex <= selectionToolColliders.Length / 2 - 1)
                         && (CurrentColorIndex > Colors.Length))
                {
                    CurrentColorIndex = tempColorIndex;
                }

                UpdateShapeIcons();
            }
        }

        private int currentColorIndex;

        public int CurrentColorIndex
        {
            get => currentColorIndex;
            set
            {
                ParticleSystem.MainModule main;
                currentColorIndex = value;
                if (currentColorIndex > Colors.Length)
                {
                    main = particles.main;
                    main.startColor = Color.white;
                    for (int i = selectionToolColliders.Length / 2; i < selectionToolColliders.Length; i++)
                    {
                        Collider collider = selectionToolColliders[i];
                        Color removalColor = Color.white;
                        removalColor.a = 0.1f;
                        collider.GetComponent<Renderer>().material.color = removalColor;
                    }

                    return;
                }

                Color col = Colors[currentColorIndex];
                col.a = 0.1f;
                for (int i = 0; i < selectionToolColliders.Length / 2; i++)
                {
                    Collider collider = selectionToolColliders[i];
                    collider.GetComponent<Renderer>().material.color = col;
                }

                main = particles.main;
                main.startColor = Colors[currentColorIndex];
            }
        }

        private ControllerModelSwitcher controllerModelSwitcher;
        private GraphManager graphManager;
        private SteamVR_TrackedObject rightController;
        private SteamVR_Controller.Device device;
        private MultiuserMessageSender multiuserMessageSender;
        private bool selActive = false;
        private VRTK_RadialMenu radialMenu;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Awake()
        {
            graphManager = referenceManager.graphManager;
            SetSelectionToolEnabled(false);

            if (CellexalConfig.Config != null)
            {
                UpdateColors();
            }

            CellexalEvents.ConfigLoaded.AddListener(UpdateColors);
        }

        private void Start()
        {
            controllerModelSwitcher = referenceManager.controllerModelSwitcher;
            rightController = referenceManager.rightController;
            multiuserMessageSender = referenceManager.multiuserMessageSender;
            selectionManager = referenceManager.selectionManager;
            if (!CrossSceneInformation.Ghost)
                radialMenu = referenceManager.rightControllerScriptAlias.GetComponentInChildren<VRTK_RadialMenu>();
            UpdateShapeIcons();
        }


        private void Update()
        {
            device = SteamVR_Controller.Input((int) rightController.index);
            if (controllerModelSwitcher.DesiredModel == ControllerModelSwitcher.Model.SelectionTool)
            {
                if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
                {
                    particles.gameObject.SetActive(true);
                    selectionToolMaterial.SetFloat("_SelectionActive", 1);
                    selActive = true;
                }

                if (device.GetPress(SteamVR_Controller.ButtonMask.Trigger))
                {
                    hapticFeedbackThisFrame = true;
                    var activeCollider = selectionToolColliders[CurrentMeshIndex];
                    Vector3 boundsCenter = activeCollider.bounds.center;
                    Vector3 boundsExtents = activeCollider.bounds.extents;
                    World.Active.GetExistingSystem<QuadrantSystem>().selectionActive = true;
                    World.Active.GetExistingSystem<PointSelectionSystem>().selectionActive = true;
                    // foreach (var graph in graphManager.Graphs)
                    // {
                    //     var closestPoints = graph.MinkowskiDetection(activeCollider.transform.position, boundsCenter,
                    //         boundsExtents, currentColorIndex);
                    //     foreach (var point in closestPoints)
                    //     {
                    //         if (CurrentMeshIndex > 3)
                    //         {
                    //             selectionManager.RemoveGraphpointFromSelection(point);
                    //         }
                    //
                    //         else
                    //         {
                    //             selectionManager.AddGraphpointToSelection(point, currentColorIndex, true);
                    //         }
                    //     }
                    // }
                }

                else if (device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
                {
                    World.Active.GetExistingSystem<QuadrantSystem>().selectionActive = false;
                    World.Active.GetExistingSystem<PointSelectionSystem>().selectionActive = false;
                    selActive = false;
                    particles.gameObject.SetActive(false);
                    selectionToolMaterial.SetFloat("_SelectionActive", 0);
                }
            }

            // Sometimes a bug occurs where particles stays active even when selection tool is off. This ensures particles is off 
            // if selection tool is inactive.
            //if (particles && !IsSelectionToolEnabled() && particles.gameObject.activeSelf)
            //{
            //    particles.gameObject.SetActive(false);
            //}
        }


        /// <summary>
        /// Updates <see cref="Colors"/> to <see cref="CellexalConfig.Config.SelectionToolColors"/>.
        /// </summary>
        public void UpdateColors()
        {
            currentColorIndex = 0;
            Colors = CellexalConfig.Config.SelectionToolColors;
            for (int i = 0; i < Colors.Length; i++)
            {
                Colors[i].a = 1;
            }

            if (!CrossSceneInformation.Ghost)
            {
                radialMenu = referenceManager.rightControllerScriptAlias.GetComponentInChildren<VRTK_RadialMenu>();
                try
                {
                    if (Colors.Length > 1)
                    {
                        //radialMenu.RegenerateButtons();
                        radialMenu.menuButtons[1].GetComponentInChildren<Image>().color = Colors[Colors.Length - 1];
                        radialMenu.menuButtons[3].GetComponentInChildren<Image>().color = Colors[1];
                    }
                    else if (Colors.Length > 0)
                    {
                        radialMenu.menuButtons[1].GetComponentInChildren<Image>().color = Colors[0];
                        radialMenu.menuButtons[3].GetComponentInChildren<Image>().color = Colors[0];
                    }
                }
                catch (NullReferenceException)
                {
                    CellexalLog.Log(
                        "Could not recreate buttons on controller. Could be that controllers were inactive at the time");
                    return;
                }

                var main = particles.main;
                main.startColor = Colors[currentColorIndex];
            }
        }

        /// <summary>
        /// Changes the color of the selection tool.
        /// </summary>
        /// <param name="dir"> The direction to move in the array of colors. true for increment, false for decrement </param>
        public void ChangeColor(bool dir)
        {
            if (CurrentColorIndex > Colors.Length) return;
            if (CurrentMeshIndex > 4) return; // Remove selection active: color should be white.
            if (CurrentColorIndex == Colors.Length - 1 && dir)
            {
                CurrentColorIndex = 0;
            }
            else if (CurrentColorIndex == 0 && !dir)
            {
                CurrentColorIndex = Colors.Length - 1;
            }
            else if (dir)
            {
                CurrentColorIndex++;
            }
            else
            {
                CurrentColorIndex--;
            }

            int buttonIndexLeft = CurrentColorIndex == 0 ? Colors.Length - 1 : CurrentColorIndex - 1;
            int buttonIndexRight = CurrentColorIndex == Colors.Length - 1 ? 0 : CurrentColorIndex + 1;
            // VRTK 3.3
            //radialMenu.RegenerateButtons();
            radialMenu.menuButtons[1].GetComponentInChildren<Image>().color = Colors[buttonIndexLeft];
            radialMenu.menuButtons[3].GetComponentInChildren<Image>().color = Colors[buttonIndexRight];
            //radialMenu.buttons[3].color = Colors[buttonIndexRight];
            controllerModelSwitcher.SwitchControllerModelColor(Colors[CurrentColorIndex]);
            //CurrentColorIndex = currentColorIndex;
        }

        /// <summary>
        /// Activates or deactivates the selection tool.
        /// </summary>
        /// <param name="enabled"> True if the selection tool should be activated, false if it should be deactivated. </param>
        public void SetSelectionToolEnabled(bool enabled)
        {
            if (enabled)
            {
                // controllerModelSwitcher.SwitchControllerModelColor(Colors[currentColorIndex]);
                if (CurrentColorIndex > Colors.Length)
                {
                    controllerModelSwitcher.SwitchControllerModelColor(Color.white);
                }
                else
                {
                    controllerModelSwitcher.SwitchControllerModelColor(Colors[CurrentColorIndex]);
                }
            }
            //particles.gameObject.SetActive(enabled && particles != null);

            for (int i = 0; i < selectionToolColliders.Length; ++i)
            {
                // if we are turning on the selection tool, enable the gameobject with the right index and disable the other ones
                selectionToolColliders[i].gameObject.SetActive(enabled && CurrentMeshIndex == i);
            }
        }

        void ActivateSelection(bool sel)
        {
            selActive = sel;
            SetSelectionToolEnabled(true);
        }

        public bool IsSelectionToolEnabled()
        {
            return GetComponentsInChildren<Collider>().Any(x => x.enabled);
        }

        private void UpdateShapeIcons()
        {
            if (radialMenu)
            {
                //radialMenu.RegenerateButtons();
                //print(radialMenu.menuButtons[0] + " " + radialMenu.menuButtons[0].GetComponentInChildren<Image>());
                if (radialMenu.menuButtons[0] && radialMenu.menuButtons[0].GetComponentInChildren<Image>())
                {
                    int buttonIndexUp = currentMeshIndex == selectionToolColliders.Length - 1
                        ? 0
                        : currentMeshIndex + 1;
                    int buttonIndexDown =
                        currentMeshIndex == 0 ? selectionToolColliders.Length - 1 : currentMeshIndex - 1;
                    radialMenu.menuButtons[0].GetComponentInChildren<Image>().sprite =
                        selectionToolShapeButtons[buttonIndexUp];
                    radialMenu.menuButtons[2].GetComponentInChildren<Image>().sprite =
                        selectionToolShapeButtons[buttonIndexDown];
                }
            }
        }

        public int GetColorIndex(string colorString)
        {
            Color[] colorArray; // = new Color[CellexalConfig.Config.SelectionToolColors.Length];
            // if using spectator mode or for other reasons the selectionToolCollider hasnt been activated
            // return the index from the config.
            colorArray = Colors.Length == 0
                ? CellexalConfig.Config.SelectionToolColors
                : referenceManager.selectionToolCollider.Colors;

            for (int i = 0; i < colorArray.Length; i++)
            {
                Color selectionColor = colorArray[i];
                string selectionColorString = "#" + ColorUtility.ToHtmlStringRGB(selectionColor);
                if (selectionColorString.Equals(colorString)) return i;
            }

            return -1;
        }

        public int GetColorIndex(Color color)
        {
            string colorString = ColorUtility.ToHtmlStringRGB(color);
            print(colorString);
            return GetColorIndex(colorString);
        }
    }
}