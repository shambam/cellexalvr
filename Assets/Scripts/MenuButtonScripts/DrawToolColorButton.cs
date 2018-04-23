﻿using UnityEngine;
/// <summary>
/// Represents the buttons that make up the color wheel for choosing the draw tool's color.
/// </summary>
public class DrawToolColorButton : CellexalButton
{
    protected override string Description
    {
        get { return "Change the color of the draw tool"; }
    }

    public SpriteRenderer buttonRenderer;
    public Color color;

    private Color tintedColor;
    private DrawTool drawTool;

    private void OnValidate()
    {
        buttonRenderer.color = color;
        Color oldColor = buttonRenderer.color;
        float newr = oldColor.r - oldColor.r / 2f;
        float newg = oldColor.g - oldColor.g / 2f;
        float newb = oldColor.b - oldColor.b / 2f;
        tintedColor = new Color(newr, newg, newb);
    }

    private void Start()
    {
        drawTool = referenceManager.drawTool;
        buttonRenderer = GetComponent<SpriteRenderer>();
        OnValidate();
    }

    protected override void Click()
    {
        drawTool.SkipNextDraw();
        drawTool.LineColor = color;
    }
}
