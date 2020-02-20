using CellexalVR.AnalysisObjects;
using CellexalVR.Menu.Buttons;
using UnityEngine;

public class ToggleLegendButton : CellexalButton
{
    protected override string Description => "Toggle the legend on/off";

    public override void Click()
    {
        LegendManager legendManager = referenceManager.legendManager;
        GameObject mainMenu = referenceManager.mainMenu;
        if (!referenceManager.legendManager.legendActive)
        {
            legendManager.ActivateLegend();
            legendManager.transform.position = mainMenu.transform.position;
            legendManager.transform.rotation = mainMenu.transform.rotation;
            legendManager.transform.Rotate(new Vector3(90f, 0f, 0f), Space.Self);
        }
        else
        {
            legendManager.DeactivateLegend();
            legendManager.DetachLegendFromCube();
        }
    }
}
