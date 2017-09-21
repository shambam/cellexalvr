﻿using System.IO;
using UnityEngine;

class SaveHeatmapButton : StationaryButton
{
    protected override string Description
    {
        get { return "Save heatmap image to disk"; }
    }

    protected override void Awake()
    {
        referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
        rightController = GameObject.Find("Controller (right)").GetComponent<SteamVR_TrackedObject>();
        base.Awake();
    }

    void Update()
    {
        device = SteamVR_Controller.Input((int)rightController.index);
        if (controllerInside && device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
        {
            gameObject.GetComponentInParent<Heatmap>().SaveImage();
        }
    }
}