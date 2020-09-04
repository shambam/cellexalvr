using CellexalVR.Spatial;
using UnityEngine;

namespace CellexalVR.Menu.Buttons.Slicing
{
    public class SliceAxisToggleButton : SliderButton
    {
        public int axis;
        protected override string Description => "";

        private Slicer slicer;

        private void Awake()
        {
            slicer = GetComponentInParent<Slicer>();
        }
        
        protected override void ActionsAfterSliding()
        {
            if (currentState == false)
            {
                slicer.ChangeAxis(axis);
            }

            else
            {
                slicer.ChangeAxis(-1);
            }
        }
    }
}