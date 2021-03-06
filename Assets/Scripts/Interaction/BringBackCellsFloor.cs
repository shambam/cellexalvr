﻿using CellexalVR.SceneObjects;
using CellexalVR.Tutorial;
using UnityEngine;

namespace CellexalVR.Interaction
{
    /// <summary>
    /// This class brings back cells that are tossed too far away.
    /// </summary>
    public class BringBackCellsFloor : MonoBehaviour
    {
        public GameObject throwMarker;
        private CellsToLoad reset;
        private BringBackObj resetObj;

        private void FixedUpdate()
        {
            if (reset != null)
            {
                var marker = Instantiate(throwMarker);
                marker.transform.position = reset.gameObject.transform.position;
                marker.transform.LookAt(Vector3.zero);
                TextMesh textmesh = marker.GetComponentInChildren<TextMesh>();
                float distance = Vector3.Distance(Vector3.zero, marker.transform.position);
                textmesh.text = distance + " m!";
                textmesh.transform.localScale *= (distance / 10f);
                reset.ResetPosition();
                reset = null;
            }
            if (resetObj != null)
            {
                var marker = Instantiate(throwMarker);
                marker.transform.position = resetObj.gameObject.transform.position;
                marker.transform.LookAt(Vector3.zero);
                TextMesh textmesh = marker.GetComponentInChildren<TextMesh>();
                float distance = Vector3.Distance(Vector3.zero, marker.transform.position);
                textmesh.text = distance + " m!";
                textmesh.transform.localScale *= (distance / 10f);
                resetObj.ResetPosition();
                resetObj = null;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            var parent = other.transform.parent;
            var shape = other.transform;
            if (parent != null)
            {
                var cellsToLoad = parent.GetComponent<CellsToLoad>();
                if (cellsToLoad != null)
                {
                    if (parent.GetComponent<Rigidbody>().velocity == Vector3.zero)
                    {
                        reset = cellsToLoad;
                    }
                    return;
                }
                var shapeObj = shape.GetComponent<BringBackObj>();
                if (shapeObj != null)
                {
                    if (parent.GetComponent<Rigidbody>().velocity == Vector3.zero)
                    {
                        resetObj = shapeObj;
                    }
                }
            }
        }
    }
}