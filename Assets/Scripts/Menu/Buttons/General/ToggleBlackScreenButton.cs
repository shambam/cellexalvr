using UnityEngine;
using System.Collections;

public class ToggleBlackScreenButton : CellexalVR.Menu.Buttons.CellexalButton
{
    protected override string Description => "Toggle filter creator board";

    public GameObject screen;


    public override void Click()
    {
        screen.SetActive(!screen.activeSelf);
    }

}
