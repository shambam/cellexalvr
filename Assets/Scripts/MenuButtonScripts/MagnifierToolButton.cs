﻿/// <summary>
/// Represents the button that toggles the magnifier tool.
/// </summary>
class MagnifierToolButton : CellexalButton
{
    private ControllerModelSwitcher controllerModelSwitcher;

    protected override string Description
    {
        get { return "Toggle magnifier tool"; }
    }

    private void Start()
    {
        controllerModelSwitcher = referenceManager.controllerModelSwitcher;
        SetButtonActivated(false);
        CellexalEvents.GraphsLoaded.AddListener(TurnOn);
        CellexalEvents.GraphsUnloaded.AddListener(TurnOff);
    }

    protected override void Click()
    {
        bool magnifierToolActivated = controllerModelSwitcher.DesiredModel == ControllerModelSwitcher.Model.Magnifier;
        if (magnifierToolActivated)
        {
            controllerModelSwitcher.TurnOffActiveTool(true);
            //controllerModelSwitcher.SwitchToDesiredModel();
            //controllerModelSwitcher.DesiredModel = ControllerModelSwitcher.Model.Normal;
        }
        else
        {
            controllerModelSwitcher.DesiredModel = ControllerModelSwitcher.Model.Magnifier;
            controllerModelSwitcher.ActivateDesiredTool();
        }
    }

    private void TurnOn()
    {
        SetButtonActivated(true);
    }

    private void TurnOff()
    {
        SetButtonActivated(false);
    }
}

