using Assets.Scripts.SceneObjects;
using CellexalVR.Filters;
using CellexalVR.General;
using CellexalVR.Tools;
using System.Collections.Generic;
using UnityEngine;
namespace CellexalVR.Menu.Buttons.Tools
{
    /// <summary>
    /// Represents the button that spawns or removes a culling cube. A maximum of two can be spawned.
    /// </summary>
    public class CullingCubeButton : CellexalButton
    {
        public GameObject cullingCubePrefab;
        public bool remove;
        public CullingCubeButton otherButton;

        private CullingFilterManager cullingFilterManager;

        protected override string Description
        {
            get { return remove ?  "Remove Culling Cube" : "Spawn Culling Cube"; }
        }

        private void Start()
        {
            cullingFilterManager = referenceManager.cullingFilterManager;
            if (remove)
                SetButtonActivated(false);
            CellexalEvents.CullingCubeSpawned.AddListener(UpdateButtons);
            CellexalEvents.CullingCubeRemoved.AddListener(UpdateButtons);
        }

        public override void Click()
        {
            if (remove)
            {
                cullingFilterManager.RemoveCube();
                referenceManager.multiuserMessageSender.SendMessageRemoveCullingCube();
            }
            else
            {
                cullingFilterManager.AddCube();
                referenceManager.multiuserMessageSender.SendMessageAddCullingCube();
            }

        }

        private void UpdateButtons()
        {
            int counter = cullingFilterManager.cubeCounter;
            if (remove && counter == 0)
            {
                SetButtonActivated(false);
            }
            else if (!remove && counter == 2)
            {
                SetButtonActivated(false);
            }
            else if (remove && counter > 0)
            {
                SetButtonActivated(true);
            }
            else if (!remove && counter < 2)
            {
                SetButtonActivated(true);
            }
        }
    }
}
