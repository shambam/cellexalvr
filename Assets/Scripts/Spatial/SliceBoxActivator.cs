using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using CellexalVR.General;
using CellexalVR.Interaction;
using CellexalVR.Spatial;
using UnityEngine;

public class SliceBoxActivator : MonoBehaviour
{
    public GameObject slicerBox;
    public SliceManager sliceManager;

    private MeshRenderer meshRenderer;
    private BoxCollider boxCollider;
    private bool controllerInside;
    private SteamVR_TrackedObject rightController;
    private SteamVR_Controller.Device device;
    private ReferenceManager referenceManager;

    // Start is called before the first frame update
    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        boxCollider = GetComponent<BoxCollider>();
        referenceManager = GetComponentInParent<GraphSlice>().referenceManager;
        rightController = referenceManager.rightController;
        sliceManager = GetComponentInParent<SliceManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name.Equals("ControllerCollider(Clone)") && (sliceManager == null || !sliceManager.controllerInsideSlice))
        {
            controllerInside = sliceManager.controllerInsideSlice = true;
            if (!slicerBox.activeSelf)
            {
                meshRenderer.enabled = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name.Equals("ControllerCollider(Clone)"))
        {
            controllerInside = sliceManager.controllerInsideSlice = false;
            if (!slicerBox.activeSelf)
            {
                meshRenderer.enabled = false;
            }
        }
    }

    public void ToggleCollider(bool toggle)
    {
        boxCollider.enabled = toggle;
    }

    // Update is called once per frame
    private void Update()
    {
        device = SteamVR_Controller.Input((int) rightController.index);
        if (controllerInside && device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger)
                             && referenceManager.controllerModelSwitcher.ActualModel != ControllerModelSwitcher.Model.SelectionTool)
        {
            // controllerInside = false;
            meshRenderer.enabled = false;
            slicerBox.SetActive(!slicerBox.activeSelf);
        }
    }
}