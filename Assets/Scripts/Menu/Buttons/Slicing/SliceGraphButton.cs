using CellexalVR.Spatial;
using Spatial;

namespace CellexalVR.Menu.Buttons.Slicing
{
    public class SliceGraphButton : CellexalButton
    {
        protected override string Description => "Slice Graph";

        private SlicingMenu slicingMenu;


        private void Start()
        {
            SetButtonActivated(false);
            slicingMenu = GetComponentInParent<SlicingMenu>();
        }

        public override void Click()
        {
            slicingMenu.SliceGraph();

            slicingMenu.GetComponentInChildren<ToggleManualSelectionSliceButton>(true).CurrentState = false;
            slicingMenu.GetComponentInChildren<ToggleManualSlicePlaneButton>(true).CurrentState = false;


            // referenceManager.controllerModelSwitcher.TurnOffActiveTool(true);
            // TODO: Add multi-user functionality.
        }

        private void TurnOn()
        {
            SetButtonActivated(true);
        }

        private void TurnOff()
        {
            SetButtonActivated(false);
        }
    }
}