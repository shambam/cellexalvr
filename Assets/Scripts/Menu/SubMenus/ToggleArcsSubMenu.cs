using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using CellexalVR.Menu.Buttons.Networks;
using CellexalVR.SceneObjects;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

namespace CellexalVR.Menu.SubMenus
{
    /// <summary>
    /// This class represents the sub menu that pops up when the ToggleArcsButton is pressed.
    /// </summary>
    public class ToggleArcsSubMenu : MenuWithTabs
    {
        public GameObject buttonPrefab;
        public GameObject wirePrefab;
        public GameObject attachPoint;

        // hard coded positions :)
        private Vector3 buttonPos = new Vector3(-0.38f, 2.40f, 0.22f);
        private Vector3 buttonPosInc = new Vector3(0.25f, 0, 0);
        private Vector3 buttonPosNewRowInc = new Vector3(0, 0, -0.15f);

        private Color[] colors;
        private GameObject previewWire;
        private bool buttonClickedThisFrame;
        private ToggleArcsButton previouslyClickedButton;
        private SteamVR_TrackedObject rightController;
        private SteamVR_Controller.Device device;
        public List<ToggleArcsButton> toggleArcButtonList = new List<ToggleArcsButton>();


        /// <summary>
        /// Initializes the arc menu.
        /// </summary>
        private void Init()
        {
            colors = CellexalConfig.Config.SelectionToolColors;
            previewWire = Instantiate(wirePrefab, this.transform);
            previewWire.SetActive(false);
        }

        private void Start()
        {
            StartCoroutine(SetControllers());
            attachPoint = GameObject.Find("[VRTK_Scripts]/RightControllerScriptAlias/AttachPoint");
        }

        private IEnumerator SetControllers()
        {
            yield return new WaitForSeconds(2);
            rightController = referenceManager.rightController;
            device = SteamVR_Controller.Input((int) rightController.index);
        }

        private void Update()
        {
            if (device == null)
            {
                rightController = referenceManager.rightController;
                device = SteamVR_Controller.Input((int) rightController.index);
            }
            else if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
            {
                // print(toggleArcButtonList.Count.ToString() + IsInsideButton());
                if (!IsInsideButton()) UndoSelectedNetwork();
            }
        }

        private bool IsInsideButton()
        {
            return toggleArcButtonList == null || toggleArcButtonList.Any(button => button.controllerInside);
        }

        /// <summary>
        /// When one network button is clicked a line is spawned to be connected to another.
        /// Once a second network button is clicked the two are connected and a line is spawned between them.
        /// </summary>
        /// <param name="clickedButton"></param>
        public void NetworkArcsButtonClicked(ToggleArcsButton clickedButton)
        {
            clickedButton.selected = true;
            buttonClickedThisFrame = true;
            if (previouslyClickedButton == null)
            {
                previewWire.SetActive(true);
                previewWire.GetComponent<Renderer>().enabled = true;
                attachPoint.SetActive(true);
                attachPoint.transform.position =
                    (referenceManager.laserPointerController.origin.transform.position) +
                    (referenceManager.laserPointerController.origin.transform.forward / 25f);
                LineRendererFollowTransforms follow = previewWire.GetComponent<LineRendererFollowTransforms>();
                follow.bendLine = true;
                follow.transform1 = attachPoint.transform;
                follow.transform2 = clickedButton.transform;
                previouslyClickedButton = clickedButton;
            }
            else
            {
                previewWire.SetActive(false);
                attachPoint.SetActive(false);
                clickedButton.ConnectTo(previouslyClickedButton);
                previouslyClickedButton = null;
            }
        }

        /// <summary>
        /// A network button that removes all connections from the previously clicked network.
        /// </summary>
        public void DisableNetworkArcsButtonClicked()
        {
            buttonClickedThisFrame = true;
            if (previouslyClickedButton == null && !previewWire.activeSelf) return;
            previouslyClickedButton.ClearArcs();
            previewWire.SetActive(false);
            attachPoint.SetActive(false);
            NetworkHandler networkHandler = previouslyClickedButton.GetComponentInParent<NetworkHandler>();
            if (networkHandler == null)
            {
                // Clear wire from skeleton(network handler) as well.
                ToggleArcsButton[] handlerButtons =
                    previouslyClickedButton.network.Handler.GetComponentsInChildren<ToggleArcsButton>();
                ToggleArcsButton handlerButton =
                    handlerButtons.First(x => x.network == previouslyClickedButton.network);
                handlerButton.ClearArcs();
            }
            else
            {
                ToggleArcsButton[] subMenuButtons = GetComponentsInChildren<ToggleArcsButton>();
                ToggleArcsButton subMenuButton = subMenuButtons.First(x => x.network == previouslyClickedButton.network);
                subMenuButton.ClearArcs();
            }
            previouslyClickedButton = null;
        }

        /// <summary>
        /// Connects/Disconnects all networks. Combined arcs can not be active at the same time so they are disabled.
        /// </summary>
        /// <param name="toggle"></param>
        public void ToggleAllArcs(bool toggle)
        {
            if (toggle)
            {
                for (int i = 0; i < toggleArcButtonList.Count / 2 - 1; i++)
                {
                    ToggleArcsButton button = toggleArcButtonList[i];
                    referenceManager.multiuserMessageSender.SendMessageSetArcsVisible(toggle, button.network.name);
                    button.network.SetCombinedArcsVisible(false);
                    for (int j = i + 1; j < toggleArcButtonList.Count / 2; j++)
                    {
                        ToggleArcsButton nextButton = toggleArcButtonList[j];
                        button.ConnectTo(nextButton);
                    }
                }

                ToggleArcsButton lastButton = toggleArcButtonList[toggleArcButtonList.Count / 2 - 1];
                lastButton.network.SetCombinedArcsVisible(false);
                GetComponentInChildren<ToggleAllCombinedArcsButton>().CurrentState = false;
                referenceManager.multiuserMessageSender.SendMessageSetArcsVisible(toggle, lastButton.network.name);
            }

            else
            {
                foreach (ToggleArcsButton button in toggleArcButtonList)
                {
                    button.ClearArcs();
                }

                // GetComponentInChildren<ToggleAllArcsButton>().CurrentState = false;
            }
        }

        /// <summary>
        /// If user clicks trigger when not close to another network button the selection is canceled and wire destroyed.
        /// </summary>
        private void UndoSelectedNetwork()
        {
            buttonClickedThisFrame = true;
            if (previouslyClickedButton == null)
            {
                return;
            }

            previewWire.SetActive(false);
            attachPoint.SetActive(false);
            previouslyClickedButton = null;
        }


        /// <summary>
        /// Creates new buttons for toggling arcs.
        /// </summary>
        /// <param name="networks"> An array containing the networks. </param>
        public void CreateToggleArcsButtons(NetworkCenter[] networks)
        {
            if (networks.Length < 1)
            {
                return;
            }

            TurnOffAllTabs();
            var newTab = AddTab(tabPrefab);
            // The prefab contains some buttons that needs some variables set.
            ArcsTabButton tabButton = newTab.GetComponentInChildren<ArcsTabButton>();
            tabButton.referenceManager = referenceManager;
            tabButton.tab = newTab;
            tabButton.Handler = networks[0].Handler;
            string tabName = networks[0].Handler.gameObject.name.Split('_')[1];
            newTab.gameObject.name = "Tab_" + tabName;
            newTab.TabName.text = tabName;
            //newTab.tab = newTab.transform.parent.gameObject;
            if (colors == null)
            {
                Init();
            }
            //foreach (GameObject button in buttons)
            //{
            //    // wait 0.1 seconds so we are out of the loop before we start destroying stuff
            //    Destroy(button.gameObject, .1f);
            //    buttonPos = new Vector3(-.39f, .77f, .282f);
            //}

            var toggleAllArcsButtonsInPrefab = newTab.GetComponentsInChildren<ToggleAllArcsButton>();
            foreach (ToggleAllArcsButton b in toggleAllArcsButtonsInPrefab)
            {
                b.SetNetworks(networks);
            }

            var toggleAllCombindedArcsInPrefab = newTab.GetComponentsInChildren<ToggleAllCombinedArcsButton>();
            foreach (ToggleAllCombinedArcsButton b in toggleAllCombindedArcsInPrefab)
            {
                b.SetNetworks(networks);
            }

            Vector3 buttonPosOrigin = buttonPos;
            for (int i = 0; i < networks.Length; ++i)
            {
                var network = networks[i];
                var newButton = Instantiate(buttonPrefab, newTab.transform);
                var toggleArcButton = newButton.GetComponent<ToggleArcsButton>();
                toggleArcButtonList.Add(toggleArcButton);
                newButton.transform.localPosition = buttonPos;
                newButton.name = "Network" + i + "_ArcButton";
                newButton.gameObject.SetActive(true);
                Color color = network.GetComponent<Renderer>().material.color;
                toggleArcButton.ButtonColor = color;
                toggleArcButton.combinedNetworksButton =
                    newTab.GetComponentInChildren<ToggleAllCombinedArcsButton>();
                toggleArcButton.SetNetwork(network);

                // position the buttons in a 4 column grid.
                if ((i + 1) % 4 == 0)
                {
                    buttonPos -= buttonPosInc * 3;
                    buttonPos += buttonPosNewRowInc;
                }
                // offset every other a bit to make the arcs between them clearer.
                else if ((i + 1) % 2 != 0)
                {
                    buttonPos += new Vector3(buttonPosInc.x, buttonPosInc.y, -0.07f);
                }
                else
                {
                    buttonPos += new Vector3(buttonPosInc.x, buttonPosInc.y, 0.07f);
                    // buttonPos += buttonPosInc;
                }
            }

            TurnOffAllTabs();
            //newTab.SetTabActive(GetComponent<Renderer>().enabled);
            buttonPos = buttonPosOrigin;
        }

        /// <summary>
        /// Removes all tab buttons and creates new ones from the list of network handlers active in the scene.
        /// 
        /// </summary>
        public void RefreshTabs()
        {
            var nhs = referenceManager.networkGenerator.networkList.FindAll(nh => nh != null);
            tabButtonPos = tabButtonPosOriginal;
            foreach (NetworkHandler nh in nhs)
            {
                DestroyTab(nh.name.Split('_')[1]);
                CreateToggleArcsButtons(nh.networks.ToArray());
            }
        }
    }
}