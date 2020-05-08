using System;
using System.Collections.Generic;
using UnityEngine;
using CellexalVR.AnalysisObjects;
using CellexalVR.SceneObjects;

namespace CellexalVR.Menu.Buttons.Networks
{
    /// <summary>
    /// Represents a button that toggles arcs between networks.
    /// </summary>
    public class RemoveNetworkArcsButton : CellexalButton
    {
        protected override string Description => "" /*Toggle all arcs connected to this network*/;
        private NetworkHandler networkHandler;
        private Color color;

        private void Start()
        {
            meshStandardColor = Color.gray;
        }

        public override void Click()
        {
            referenceManager.arcsSubMenu.DisableNetworkArcsButtonClicked();
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Controller"))
            {
                SetHighlighted(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Controller"))
            {
                SetHighlighted(false);
            }
        }
    }
}