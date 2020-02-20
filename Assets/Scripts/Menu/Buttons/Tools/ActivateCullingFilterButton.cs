using Assets.Scripts.SceneObjects;
using CellexalVR.Tools;
using UnityEngine;
namespace CellexalVR.Menu.Buttons.Tools
{
    /// <summary>
    /// Represents the button that toggles the screenshot tool.
    /// </summary>
    public class ActivateCullingFilterButton : CellexalButton
    {
        public CullingCube cullingCube;

        protected override string Description
        {
            get { return "Applies selection filter to cube"; }
        }

        public override void Click()
        {
            cullingCube.ActivateFilter();
            //SetButtonActivated(false);
        }
    }
}
