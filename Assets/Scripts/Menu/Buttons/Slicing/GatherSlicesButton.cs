using CellexalVR.Spatial;
using UnityEngine;

namespace CellexalVR.Menu.Buttons.Slicing
{
    public class GatherSlicesButton : CellexalButton
    {
        protected override string Description => "Gather Slices to Parent";

        private SliceManager sliceManager;


        private void Awake()
        {
            SetButtonActivated(false);
            sliceManager = GetComponentInParent<SliceManager>();
            if (sliceManager)
            {
                SetButtonActivated(true);
            }
        }
        
        public override void Click()
        {
            sliceManager.ActivateSlices(false);
        }
    }
}