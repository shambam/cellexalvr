﻿using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using System.Collections;
using UnityEngine;
using VRTK;

namespace CellexalVR.Interaction
{
    /// <summary>
    /// Handles what happens when a network center is interacted with.
    /// </summary>
    class NetworkCenterInteract : VRTK_InteractableObject
    {
        public ReferenceManager referenceManager;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
        }

        public override void OnInteractableObjectGrabbed(InteractableObjectEventArgs e)
        {
            referenceManager.multiuserMessageSender.SendMessageToggleGrabbable(gameObject.name, false);
            if (grabbingObjects.Count == 1)
            {
                // moving many triggers really pushes what unity is capable of
                foreach (Collider c in GetComponentsInChildren<Collider>())
                {
                    if (c.gameObject.name != "Ring" && !c.gameObject.name.Contains("Enlarged_Network"))
                    {
                        c.enabled = false;
                    }
                    //else if (c.gameObject.name == "Ring")
                    //{
                    //    ((MeshCollider)c).convex = true;
                    //}
                }
            }
            base.OnInteractableObjectGrabbed(e);
        }

        public override void OnInteractableObjectUngrabbed(InteractableObjectEventArgs e)
        {
            referenceManager.multiuserMessageSender.SendMessageToggleGrabbable(gameObject.name, true);
            NetworkCenter center = gameObject.GetComponent<NetworkCenter>();
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
            referenceManager.multiuserMessageSender.SendMessageNetworkCenterUngrabbed(center.Handler.name, center.name, rigidbody.velocity, rigidbody.angularVelocity);
            if (grabbingObjects.Count == 0)
            {
                foreach (Collider c in GetComponentsInChildren<Collider>())
                {
                    if (c.gameObject.name != "Ring" && !c.gameObject.name.Contains("Enlarged_Network"))
                    {
                        c.enabled = true;
                    }
                    //else if (c.gameObject.name == "Ring")
                    //{
                    //    ((MeshCollider)c).convex = false;
                    //}

                }
            }
            base.OnInteractableObjectUngrabbed(e);
        }


        //private void OnTriggerEnter(Collider other)
        //{
        //    if (referenceManager.controllerModelSwitcher.ActualModel == ControllerModelSwitcher.Model.TwoLasers
        //        || referenceManager.controllerModelSwitcher.ActualModel == ControllerModelSwitcher.Model.Keyboard)
        //    {
        //        if (other.gameObject.name.Equals("ControllerCollider(Clone)"))
        //        {
        //            CellexalEvents.ObjectGrabbed.Invoke();
        //        }
        //    }
        //}

        //private void OnTriggerExit(Collider other)
        //{
        //    if (referenceManager.controllerModelSwitcher.ActualModel == ControllerModelSwitcher.Model.TwoLasers
        //        || referenceManager.controllerModelSwitcher.ActualModel == ControllerModelSwitcher.Model.Keyboard)
        //    {
        //        if (other.gameObject.name.Equals("ControllerCollider(Clone)"))
        //        {
        //            CellexalEvents.ObjectUngrabbed.Invoke();
        //        }
        //    }
        //}
    }
}