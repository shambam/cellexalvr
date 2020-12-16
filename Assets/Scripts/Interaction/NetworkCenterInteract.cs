﻿using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using System.Collections;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace CellexalVR.Interaction
{
    /// <summary>
    /// Handles what happens when a network center is interacted with.
    /// </summary>
    class NetworkCenterInteract : InteractableObjectBasic
    {
        public ReferenceManager referenceManager;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            InteractableObjectGrabbed += Grabbed;
            InteractableObjectUnGrabbed += UnGrabbed;
        }

        private void Start()
        {
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            GetComponent<Interactable>().highlightOnHover = false;
        }

        private void Grabbed(object sender, Hand hand)
        {
            referenceManager.multiuserMessageSender.SendMessageToggleGrabbable(gameObject.name, false);
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

            CellexalEvents.ObjectUngrabbed.Invoke();
            base.OnInteractableObjectGrabbed(hand);
        }

        private void UnGrabbed(object sender, Hand hand)
        {
            referenceManager.multiuserMessageSender.SendMessageToggleGrabbable(gameObject.name, true);
            NetworkCenter center = gameObject.GetComponent<NetworkCenter>();
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
            referenceManager.multiuserMessageSender.SendMessageNetworkCenterUngrabbed(center.Handler.name, center.name, transform.position, transform.rotation, rigidbody.velocity,
                rigidbody.angularVelocity);
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

            CellexalEvents.ObjectGrabbed.Invoke();
            base.OnInteractableObjectUnGrabbed(hand);
        }


        //private void OnTriggerEnter(Collider other)
        //{
        //    if (referenceManager.controllerModelSwitcher.ActualModel == ControllerModelSwitcher.Model.TwoLasers
        //        || referenceManager.controllerModelSwitcher.ActualModel == ControllerModelSwitcher.Model.Keyboard)
        //    {
        //        if (other.gameObject.name.Equals("ControllerCollider(Clone)"))
        //        {
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
        //        }
        //    }
        //}
    }
}