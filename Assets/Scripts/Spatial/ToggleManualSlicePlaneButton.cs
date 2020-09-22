using CellexalVR.Menu.Buttons;
using CellexalVR.Menu.Buttons.Slicing;
using UnityEngine;

namespace CellexalVR.Spatial
{
    public class ToggleManualSlicePlaneButton : SliderButton
    {
        private Slicer slicer;
        protected override string Description { get; }

        protected override void Start()
        {
            base.Start();
            slicer = GetComponentInParent<Slicer>();
        }

        protected override void ActionsAfterSliding()
        {
            if (currentState)
            {
                slicer.ActivatePlane(-1);
                slicer.slicingMenuParent.GetComponentInChildren<SliceGraphButton>().SetButtonActivated(true);
                GetComponentInParent<SliceManager>().interactableObject.isGrabbable = false;
            }
            else
            {
                slicer.plane.SetActive(false);
                slicer.slicingMenuParent.GetComponentInChildren<SliceGraphButton>().SetButtonActivated(false);
                GetComponentInParent<SliceManager>().interactableObject.isGrabbable = true;
            }
        }
    }
}