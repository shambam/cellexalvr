using CellexalVR.AnalysisObjects;
using CellexalVR.Menu.Buttons;
using UnityEngine;

public class DetachLegendButton : CellexalButton
{
    protected override string Description => "Detach legend";

    public override void Click()
    {
        LegendManager legendManager = referenceManager.legendManager;
        legendManager.DetachLegendFromCube();
    }
}
