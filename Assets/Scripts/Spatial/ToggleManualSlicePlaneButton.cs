using CellexalVR.Menu.Buttons;
using CellexalVR.Menu.Buttons.Slicing;
using Spatial;
using UnityEngine;

namespace CellexalVR.Spatial
{
    public class ToggleManualSlicePlaneButton : SliderButton
    {
        private Slicer slicer;
        private SlicingMenu slicingMenu;
        protected override string Description { get; }

        protected override void Start()
        {
            base.Start();
            slicer = GetComponentInParent<Slicer>();
            slicingMenu = GetComponentInParent<SlicingMenu>();
        }

        protected override void ActionsAfterSliding()
        {
            if (currentState)
            {
                slicingMenu.ActivateMode(SlicingMenu.SliceMode.Manual);
                slicer.ActivatePlane(-1);
                slicingMenu.GetComponentInChildren<SliceGraphButton>().SetButtonActivated(true);
                GetComponentInParent<SliceManager>().interactableObject.isGrabbable = false;
                ToggleManualSelectionSliceButton otherSliderButton = slicer.slicingMenuParent.GetComponentInChildren<ToggleManualSelectionSliceButton>();
                if (otherSliderButton.CurrentState)
                {
                    otherSliderButton.CurrentState = !currentState;
                }
            }
            else
            {
                slicer.plane.SetActive(false);
                slicingMenu.GetComponentInChildren<SliceGraphButton>().SetButtonActivated(false);
                GetComponentInParent<SliceManager>().interactableObject.isGrabbable = true;
            }
        }
    }
}