using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using CellexalVR.Menu.SubMenus;
using CellexalVR.Multiuser;
using System;
using System.Diagnostics;
using System.Linq;
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
        public int currentColorIndex = 0;
        public bool hapticFeedbackThisFrame = true;

        private int currentMeshIndex;
        /// <summary>
        /// 0: paddle, 1: bludgeon, 2: smaller bludgeon, 4: stick
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
                }
                else if (currentMeshIndex < 0)
                {
                    currentMeshIndex = selectionToolColliders.Length - 1;
                }
                UpdateShapeIcons();
            }
        }


        private SelectionFromPreviousMenu previousSelectionMenu;
        private ControllerModelSwitcher controllerModelSwitcher;
        private GraphManager graphManager;
        private SteamVR_TrackedObject rightController;
        private SteamVR_Controller.Device device;
        private MultiuserMessageSender multiuserMessageSender;
        private bool selActive = false;
        private Color selectedColor;
        private VRTK_RadialMenu radialMenu;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        void Awake()
        {
            previousSelectionMenu = referenceManager.selectionFromPreviousMenu;
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
            device = SteamVR_Controller.Input((int)rightController.index);
            if (controllerModelSwitcher.DesiredModel == ControllerModelSwitcher.Model.SelectionTool)
            {
                if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
                {
                    particles.gameObject.SetActive(true);
                    selActive = true;
                }
                if (device.GetPress(SteamVR_Controller.ButtonMask.Trigger))
                {
                    hapticFeedbackThisFrame = true;
                    var activeCollider = selectionToolColliders[CurrentMeshIndex];
                    Vector3 boundsCenter = activeCollider.bounds.center;
                    Vector3 boundsExtents = activeCollider.bounds.extents;
                    foreach (var graph in graphManager.Graphs)
                    {
                        //print(graph.GraphName + graph.GraphActive);
                        var closestPoints = graph.MinkowskiDetection(activeCollider.transform.position, boundsCenter, boundsExtents, currentColorIndex);
                        foreach (var point in closestPoints)
                        {
                            selectionManager.AddGraphpointToSelection(point, currentColorIndex, true);
                        }
                    }
                }

                else if (device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
                {
                    selActive = false;
                    particles.gameObject.SetActive(false);
                }
            }
            // Sometimes a bug occurs where particles stays active even when selection tool is off. This ensures particles is off 
            // if selection tool is inactive.
            //if (particles && !IsSelectionToolEnabled() && particles.gameObject.activeSelf)
            //{
            //    particles.gameObject.SetActive(false);
            //}

        }

        //void OnTriggerEnter(Collider other)
        //{

        //    var cubeOnLine = other.gameObject.GetComponent<Selectable>();
        //    if (cubeOnLine != null && !cubeOnLine.selected)
        //    {
        //        cubeOnLine.selected = true;
        //        selectionManager.AddGraphpointToSelection(cubeOnLine.graphPoint);
        //        foreach (Selectable sel in cubeOnLine.graphPoint.lineBetweenCellsCubes)
        //        {
        //            sel.GetComponent<Renderer>().material.color = Colors[currentColorIndex];
        //        }
        //        referenceManager.multiuserMessageSender.SendMessageCubeColoured(cubeOnLine.graphPoint.parent.name, cubeOnLine.graphPoint.Label,
        //                                                        currentColorIndex, Colors[currentColorIndex]);
        //    }
        //}


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
                    //radialMenu.RegenerateButtons();
                    radialMenu.menuButtons[1].GetComponentInChildren<Image>().color = Colors[Colors.Length - 1];
                    radialMenu.menuButtons[3].GetComponentInChildren<Image>().color = Colors[1];
                }
                catch (NullReferenceException e)
                {
                    CellexalLog.Log("Could not recreate buttons on controller. Could be that controllers were inactive at the time");
                    return;
                }
                selectedColor = Colors[currentColorIndex];
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
            if (currentColorIndex == Colors.Length - 1 && dir)
            {
                currentColorIndex = 0;
            }
            else if (currentColorIndex == 0 && !dir)
            {
                currentColorIndex = Colors.Length - 1;
            }
            else if (dir)
            {
                currentColorIndex++;
            }
            else
            {
                currentColorIndex--;
            }
            int buttonIndexLeft = currentColorIndex == 0 ? Colors.Length - 1 : currentColorIndex - 1;
            int buttonIndexRight = currentColorIndex == Colors.Length - 1 ? 0 : currentColorIndex + 1;
            // VRTK 3.3
            //radialMenu.RegenerateButtons();
            radialMenu.menuButtons[1].GetComponentInChildren<Image>().color = Colors[buttonIndexLeft];
            radialMenu.menuButtons[3].GetComponentInChildren<Image>().color = Colors[buttonIndexRight];
            //radialMenu.buttons[3].color = Colors[buttonIndexRight];
            selectedColor = Colors[currentColorIndex];
            controllerModelSwitcher.SwitchControllerModelColor(Colors[currentColorIndex]);

            var main = particles.main;
            main.startColor = Colors[currentColorIndex];
        }

        /// <summary>
        /// Activates or deactivates the selection tool.
        /// </summary>
        /// <param name="enabled"> True if the selection tool should be activated, false if it should be deactivated. </param>
        public void SetSelectionToolEnabled(bool enabled)
        {
            if (enabled)
            {
                controllerModelSwitcher.SwitchControllerModelColor(Colors[currentColorIndex]);
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
                    int buttonIndexUp = currentMeshIndex == selectionToolColliders.Length - 1 ? 0 : currentMeshIndex + 1;
                    int buttonIndexDown = currentMeshIndex == 0 ? selectionToolColliders.Length - 1 : currentMeshIndex - 1;
                    radialMenu.menuButtons[0].GetComponentInChildren<Image>().sprite = selectionToolShapeButtons[buttonIndexUp];
                    radialMenu.menuButtons[2].GetComponentInChildren<Image>().sprite = selectionToolShapeButtons[buttonIndexDown];
                }
            }
        }

    }
}
