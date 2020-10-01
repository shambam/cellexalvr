using System;
using CellexalVR.Menu.Buttons;
using CellexalVR.Spatial;
using UnityEngine;
using VRTK;

namespace Spatial
{
    public class MoveToReferenceButton : CellexalButton
    {
        private GraphSlice graphSlice;


        public void Start()
        {
            graphSlice = GetComponentInParent<GraphSlice>();
        }

        protected override string Description => "Move slice to reference mesh";
        public override void Click()
        {
            graphSlice.MoveToReference();
        }
    }
}