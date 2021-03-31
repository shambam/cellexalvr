﻿using CellexalVR.AnalysisLogic;
using UnityEngine;

namespace CellexalVR.Menu.Buttons.Slicing
{
    public class SliceGraphButton : CellexalButton
    {
        protected override string Description => "Slice Graph";

        private SlicingMenu slicingMenu;


        protected override void Awake()
        {
            base.Awake();
            //SetButtonActivated(false);
            slicingMenu = GetComponentInParent<SlicingMenu>();
        }
        
        public override void Click()
        {
            slicingMenu.SliceGraph();
            
            // TODO: Add multi-user functionality.
        }
    }
}