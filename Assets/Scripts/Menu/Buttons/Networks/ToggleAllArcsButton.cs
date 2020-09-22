using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using UnityEngine;

namespace CellexalVR.Menu.Buttons.Networks
{
    /// <summary>
    /// Represents a button that toggle all arcs between all networks on a graph skeleton.
    /// </summary>
    public class ToggleAllArcsButton : SliderButton
    {
        // public bool toggleToState;

        public NetworkCenter[] networks;
        // public GameObject[] parentNetworks;

        protected override string Description => "";

        protected override void ActionsAfterSliding()
        {
            if (networks == null) return;
            // foreach (NetworkCenter network in networks)
            // {
            // network.SetCombinedArcsVisible(false);
            // network.SetArcsVisible(currentState);
            // referenceManager.multiuserMessageSender.SendMessageSetArcsVisible(currentState, network.name);
            // }
            ToggleAllCombinedArcsButton allCombArcsButton = referenceManager.arcsSubMenu.GetComponentInChildren<ToggleAllCombinedArcsButton>();
            if (allCombArcsButton.CurrentState) allCombArcsButton.CurrentState = !currentState;
            referenceManager.arcsSubMenu.ToggleAllArcs(currentState);
            referenceManager.multiuserMessageSender.SendMessageToggleAllArcs(currentState);
        }

        /// <summary>
        /// Sets the networks that this button should deal with.
        /// </summary>
        public void SetNetworks(NetworkCenter[] networks)
        {
            //color = network.GetComponent<Renderer>().material.color;
            //GetComponent<Renderer>().material.color = color;
            this.networks = new NetworkCenter[networks.Length];
            networks.CopyTo(this.networks, 0);
        }
    }
}