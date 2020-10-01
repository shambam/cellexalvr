using CellexalVR.Interaction;
using CellexalVR.Menu.Buttons;
using CellexalVR.Menu.Buttons.Slicing;
using CellexalVR.Spatial;

namespace Spatial
{
    public class ToggleManualSelectionSliceButton : SliderButton
    {
        private SlicingMenu slicingMenu;

        protected override void Start()
        {
            base.Start();
            slicingMenu = GetComponentInParent<SlicingMenu>();
        }


        protected override string Description { get; }

        protected override void ActionsAfterSliding()
        {
            if (currentState)
            {
                slicingMenu.ActivateMode(SlicingMenu.SliceMode.Freehand);
                referenceManager.controllerModelSwitcher.DesiredModel = ControllerModelSwitcher.Model.SelectionTool;
                referenceManager.controllerModelSwitcher.ActivateDesiredTool();
                slicingMenu.GetComponentInChildren<SliceGraphButton>().SetButtonActivated(true);
                ToggleManualSlicePlaneButton otherSliderButton = slicingMenu.GetComponentInChildren<ToggleManualSlicePlaneButton>();
                if (otherSliderButton.CurrentState)
                {
                    otherSliderButton.CurrentState = !currentState;
                }
            }

            else
            {
                slicingMenu.ActivateMode(SlicingMenu.SliceMode.Manual);
                slicingMenu.GetComponentInChildren<SliceGraphButton>().SetButtonActivated(false);
                print("turn off selection tool");
                referenceManager.controllerModelSwitcher.TurnOffActiveTool(true);
                // referenceManager.controllerModelSwitcher.DesiredModel = ControllerModelSwitcher.Model.Normal;
                // referenceManager.controllerModelSwitcher.ActivateDesiredTool();
            }
        }
    }
}