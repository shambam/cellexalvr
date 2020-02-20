using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using System.Collections;
using UnityEngine;
using VRTK;

namespace CellexalVR.Interaction
{
    /// <summary>
    /// Handles what happens when a graph is interacted with.
    /// </summary>
    class GraphInteract : VRTK_InteractableObject
    {
        public ReferenceManager referenceManager;

        //private Coroutine runningCoroutine;

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
            //StopPositionSync();
            base.OnInteractableObjectGrabbed(e);
        }

        public override void OnInteractableObjectUngrabbed(InteractableObjectEventArgs e)
        {
            referenceManager.multiuserMessageSender.SendMessageToggleGrabbable(gameObject.name, true);
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
            referenceManager.multiuserMessageSender.SendMessageGraphUngrabbed(gameObject.name, rigidbody.velocity, rigidbody.angularVelocity);
            //referenceManager.rightLaser.enabled = true;
            //referenceManager.controllerModelSwitcher.ActivateDesiredTool();
            //runningCoroutine = StartCoroutine(KeepGraphPositionSynched());
            base.OnInteractableObjectUngrabbed(e);
        }

    }
}