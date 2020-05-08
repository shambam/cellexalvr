using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CellexalVR.AnalysisObjects;
using CellexalVR.Menu.SubMenus;
using CellexalVR.SceneObjects;
using JetBrains.Annotations;
using UnityEngine.Experimental.PlayerLoop;

namespace CellexalVR.Menu.Buttons.Networks
{
    /// <summary>
    /// Represents a button that toggles arcs between networks.
    /// </summary>
    public class ToggleArcsButton : CellexalButton
    {
        public bool toggleToState;
        public List<GameObject> wires;

        // public Color color;

        [HideInInspector] public ToggleAllCombinedArcsButton combinedNetworksButton;

        public NetworkCenter network;
        // public GameObject parentNetwork;

        private List<ToggleArcsButton> connectedButtons = new List<ToggleArcsButton>();
        private ToggleArcsSubMenu arcsSubMenu;
        private bool cellsHighlighted;

        public Color ButtonColor
        {
            get => color;
            set
            {
                color = value / 1.5f;
                meshStandardColor = color;
                foreach (Renderer rend in GetComponentsInChildren<Renderer>())
                {
                    rend.material.color = color;
                }
            }
        }

        protected override string Description => "" /*Toggle all arcs connected to this network*/;

        // private NetworkHandler networkHandler;
        private Color color;
        public bool selected;


        private void Start()
        {
            // networkHandler = network.Handler;
            arcsSubMenu = referenceManager.arcsSubMenu;
        }


        public override void Click()
        {
            // combinedNetworksButton.SetCombinedArcsVisible(false);
            // network.SetArcsVisible(toggleToState);
            // referenceManager.multiuserMessageSender.SendMessageSetArcsVisible(toggleToState, network.name);
            arcsSubMenu.NetworkArcsButtonClicked(this);
        }


        public bool ConnectTo(ToggleArcsButton other)
        {
            if (other.network == network || connectedButtons.Contains(other)) return false;

            NetworkHandler networkHandler = GetComponentInParent<NetworkHandler>();
            Transform parentTransform;
            GameObject wire = AddWire(this, other,
                networkHandler == null ? arcsSubMenu.transform : networkHandler.transform);
            if (networkHandler == null)
            {
                // Add corresponding wire to skeleton(network handler) as well.
                parentTransform = network.Handler.transform;
                ToggleArcsButton[] buttonsOnHandler = network.Handler.GetComponentsInChildren<ToggleArcsButton>();
                ToggleArcsButton buttonOnHandler = buttonsOnHandler.First(x => x.network == network);
                ToggleArcsButton otherHandlerButton =
                    buttonsOnHandler.First(x => x.network == other.network);
                GameObject extraWire = AddWire(buttonOnHandler, otherHandlerButton, parentTransform);
                extraWire.GetComponent<LineRenderer>().enabled = true;
            }
            else
            {
                // Add corresponding wire to arcs menu as well.
                parentTransform = arcsSubMenu.transform;
                ToggleArcsButton[] buttonsOnMenu = arcsSubMenu.GetComponentsInChildren<ToggleArcsButton>();
                ToggleArcsButton buttonOnMenu = buttonsOnMenu.First(x => x.network == network);
                ToggleArcsButton otherButtonOnMenu =
                    buttonsOnMenu.First(x => x.network == other.network);
                GameObject extraWire = AddWire(buttonOnMenu, otherButtonOnMenu, parentTransform);
                extraWire.GetComponent<LineRenderer>().enabled = arcsSubMenu.Active;
                wire.GetComponent<LineRenderer>().enabled = true;
            }


            return true;
        }

        private GameObject AddWire(ToggleArcsButton button1, ToggleArcsButton button2, Transform parentTransform)
        {
            GameObject newWire = Instantiate(arcsSubMenu.wirePrefab);
            newWire.transform.parent = parentTransform;
            LineRendererFollowTransforms line = newWire.GetComponent<LineRendererFollowTransforms>();
            line.bendLine = true;
            line.transform1 = button1.transform.transform;
            line.transform2 = button2.transform.transform;
            wires.Add(newWire);
            button2.wires.Add(newWire);
            network.SetArcsVisible(true, button2.network);
            connectedButtons.Add(button2);
            button2.connectedButtons.Add(this);
            newWire.SetActive(true);
            selected = false;
            return newWire;
        }

        public void ClearArcs()
        {
            foreach (GameObject wire in wires)
            {
                Destroy(wire.gameObject);
            }

            foreach (ToggleArcsButton button in connectedButtons)
            {
                button.connectedButtons.Remove(this);
            }

            wires.Clear();
            connectedButtons.Clear();
            network.SetArcsVisible(false);
        }

        /// <summary>
        /// Sets which network's arcs this button should toggle.
        /// </summary>
        public void SetNetwork(NetworkCenter network)
        {
            this.network = network;
        }

        public override void SetHighlighted(bool highlight)
        {
            if (cellsHighlighted == highlight) return;
            base.SetHighlighted(highlight);
            cellsHighlighted = highlight;
            referenceManager.cellManager.HighlightCells(network.cellsInGroup, highlight);
        }

    }
}