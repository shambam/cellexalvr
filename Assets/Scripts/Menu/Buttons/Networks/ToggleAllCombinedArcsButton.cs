using CellexalVR.AnalysisObjects;
using UnityEngine.UI;

namespace CellexalVR.Menu.Buttons.Networks
{
    /// <summary>
    /// Represents a buttont that toggle all the combined arcs between all networks ina  graph skeleton,
    /// The combined arcs show the number of arcs that are between two networks.
    /// </summary>
    public class ToggleAllCombinedArcsButton : SliderButton
    {

        // public bool toggleToState;

        private NetworkCenter[] networks;
        private bool storeButtonState;

        protected override string Description => "";

        protected override void ActionsAfterSliding()
        {
            if (networks == null) return;
            var allArcsButton = referenceManager.arcsSubMenu.GetComponentInChildren<ToggleAllArcsButton>();
            if (allArcsButton.CurrentState) allArcsButton.CurrentState = !currentState;

            foreach (NetworkCenter network in networks)
            {
                network.SetCombinedArcsVisible(currentState);
                referenceManager.multiuserMessageSender.SendMessageSetCombinedArcsVisible(currentState, network.name);
            }
        }

        /// <summary>
        /// Sets the networks that this button should deal with.
        /// </summary>
        /// <param name="networks"></param>
        public void SetNetworks(NetworkCenter[] networks)
        {
            //color = network.GetComponent<Renderer>().material.color;
            //GetComponent<Renderer>().material.color = color;
            this.networks = new NetworkCenter[networks.Length];
            networks.CopyTo(this.networks, 0);

        }

        /// <summary>
        /// Show or hides all the combined arcs.
        /// </summary>
        /// <param name="visible"> True if the arcs should be shown, false if hidden. </param>
        public void SetCombinedArcsVisible(bool visible)
        {
            foreach (NetworkCenter network in networks)
            {
                network.SetCombinedArcsVisible(visible);
            }
        }
    }
}