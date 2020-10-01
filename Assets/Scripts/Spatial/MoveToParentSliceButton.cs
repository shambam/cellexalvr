using System;
using CellexalVR.Menu.Buttons;
using CellexalVR.Spatial;
using UnityEngine;
using VRTK;

namespace Spatial
{
    public class MoveToParentSliceButton : CellexalButton
    {
        private GraphSlice graphSlice;
        public void Start()
        {
            graphSlice = GetComponentInParent<GraphSlice>();
        }

        protected override string Description => "Move to parent slice";
        public override void Click()
        {
            graphSlice.MoveToParentSlice();
        }
    }
}