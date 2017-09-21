﻿using UnityEngine;
using System.Collections;


namespace Com.MyCompany.MyGame
{
    public class PlayerAnimatorManager : Photon.MonoBehaviour
    {
        #region PUBLIC PROPERTIES
        //public float DirectionDampTime = 5f;
        public Transform target;
        public Transform cameraPos;
        

        private GraphPoint selectedPoint;
        #endregion


        #region Private Variables

        #endregion

        #region MONOBEHAVIOUR MESSAGES


        // Use this for initialization
        void Start()
        {
            cameraPos = GameObject.Find("Camera (eye)").GetComponent<Transform>();
            target = GetComponent<Transform>();
            if (!target)
            {
                Debug.LogError("PlayerAnimatorManager is Missing Animator Component", this);
            }
          

        }


        // Update is called once per frame
        void Update()
        {
            
            if (photonView.isMine == false && PhotonNetwork.connected == true)
            {
                return;
            }
            if (!target)
            {
                return;
            }

			if (cameraPos == null) {
				cameraPos = GameObject.Find ("Camera (eye)").GetComponent<Transform> ();

			}

            target.position = cameraPos.position;
            target.rotation = cameraPos.rotation;
            target.Rotate(90, 0, 0);
            // deal with Jumping
            
            // only allow jumping if we are running.

        }


        #endregion
    }
}