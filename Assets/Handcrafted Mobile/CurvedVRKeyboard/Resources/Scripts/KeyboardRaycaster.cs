﻿using UnityEngine;
namespace CurvedVRKeyboard {

    public class KeyboardRaycaster: KeyboardComponent {

        //------Raycasting----
        [SerializeField, HideInInspector]
        private Transform raycastingSource;

        [SerializeField, HideInInspector]
        private GameObject target;

		//private SteamVR_TrackedObject trackedObject;
		private SteamVR_Controller.Device device1, device2;
		private Valve.VR.EVRButtonId triggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;

        private float rayLength;
        private Ray ray;
        private RaycastHit hit;
        private LayerMask layer;
        private float minRaylengthMultipler = 1.5f;
        //---interactedKeys---
        private KeyboardStatus keyboardStatus;
        private KeyboardItem keyItemCurrent;
		public GraphManager graphManager;

        [SerializeField, HideInInspector]
        private string clickInputName;

        void Start () {
            keyboardStatus = gameObject.GetComponent<KeyboardStatus>();
			keyboardStatus.setGraphManager (graphManager);
            int layerNumber = gameObject.layer;
            layer = 1 << layerNumber;
//			trackedObject = controller.GetComponent<SteamVR_TrackedObject> ();
//			Debug.Log ((int)trackedObject.index);
			device1 = SteamVR_Controller.Input (3);
			device2 = SteamVR_Controller.Input (4);
        }

        void Update () {
            // * sum of all scales so keys are never to far
            rayLength = Vector3.Distance(raycastingSource.position, target.transform.position) * (minRaylengthMultipler + 
                 (Mathf.Abs(target.transform.lossyScale.x) + Mathf.Abs(target.transform.lossyScale.y) + Mathf.Abs(target.transform.lossyScale.z)));
            RayCastKeyboard();
        }

        /// <summary>
        /// Check if camera is pointing at any key. 
        /// If it does changes state of key
        /// </summary>
        private void RayCastKeyboard () {
            ray = new Ray(raycastingSource.position, raycastingSource.forward);
            if(Physics.Raycast(ray, out hit, rayLength, layer)) { // If any key was hit
                KeyboardItem focusedKeyItem = hit.transform.gameObject.GetComponent<KeyboardItem>();
                if(focusedKeyItem != null) { // Hit may occur on item without script
                    ChangeCurrentKeyItem(focusedKeyItem);
                    keyItemCurrent.Hovering();
#if !UNITY_HAS_GOOGLEVR
					//Debug.Log((int)trackedObject.index);
					if(device1.GetPressDown (triggerButton) || device2.GetPressDown(triggerButton)) {// If key clicked
#else
                    if(GvrController.TouchDown) {
#endif
                        keyItemCurrent.Click();
                        keyboardStatus.HandleClick(keyItemCurrent);
                    }
                }
            } else if(keyItemCurrent != null) {// If no target hit and lost focus on item
                ChangeCurrentKeyItem(null);
            }
        }

        private void ChangeCurrentKeyItem ( KeyboardItem key ) {
            if(keyItemCurrent != null) {
                keyItemCurrent.StopHovering();
            }
            keyItemCurrent = key;
        }

        //---Setters---
        public void SetRayLength ( float rayLength ) {
            this.rayLength = rayLength;
        }

        public void SetRaycastingTransform ( Transform raycastingSource ) {
            this.raycastingSource = raycastingSource;
        }

        public void SetClickButton ( string clickInputName ) {
            this.clickInputName = clickInputName;
        }

        public void SetTarget ( GameObject target ) {
            this.target = target;
        }
    }
}