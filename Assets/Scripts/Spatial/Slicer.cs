using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CellexalVR.AnalysisObjects;
using UnityEngine;

namespace CellexalVR.Spatial
{
    public class Slicer : MonoBehaviour
    {
        public Transform t1, t2, t3, t4, t5, t6;
        public Transform[] currentTransforms = new Transform[2];
        public int axis;
        public GameObject blade;
        public GameObject plane;
        public bool sliceAnimationActive;

        private LineRenderer lr;
        private Graph graph;
        private float animationTime = 1f;

        private void Start()
        {
            // graph = GetComponentInParent<Graph>();
            // lr = GetComponentInChildren<LineRenderer>();
            // lr.useWorldSpace = false;
            // lr.SetPositions(new Vector3[] {t1.localPosition, t2.localPosition});
            //
            // switch (axis)
            // {
            //     case 0:
            //         currentTransforms[0] = t1;
            //         currentTransforms[1] = t2;
            //         lr.transform.localRotation = Quaternion.identity;
            //         break;
            //     case 1:
            //         currentTransforms[0] = t3;
            //         currentTransforms[1] = t4;
            //         lr.transform.localRotation = Quaternion.Euler(90, 0, 0);
            //         break;
            //     case 2:
            //         currentTransforms[0] = t5;
            //         currentTransforms[1] = t6;
            //         // lr.transform.localRotation = Quaternion.Euler(0, 0, 180);
            //         break;
            // }
        }


        public IEnumerator sliceAnimation()
        {
            sliceAnimationActive = true;
            float t = 0f;
            float yStart = 0.5f;
            float yEnd = -0.5f;
            Vector3 pos = blade.transform.localPosition;
            float progress;
            while (t < animationTime)
            {
                progress = Mathf.SmoothStep(0, animationTime, t);
                pos.y = Mathf.Lerp(yStart, yEnd, progress);
                t += (Time.deltaTime / animationTime);
                blade.transform.localPosition = pos;
                yield return null;
            }

            yStart = yEnd;
            yEnd = 0.5f;
            t = 0;

            while (t < animationTime)
            {
                progress = Mathf.SmoothStep(0, animationTime, t);
                pos.y = Mathf.Lerp(yStart, yEnd, progress);
                t += (Time.deltaTime / animationTime);
                blade.transform.localPosition = pos;
                yield return null;
            }

            sliceAnimationActive = false;
        }


        public Plane GetPlane()
        {
            return new Plane(plane.transform.forward, plane.transform.position);
        }


        // private void OnTriggerEnter(Collider other)
        // {
        //     print($"slicer collider : {other.gameObject.name}" );
        // }

        private void Update()
        {
            // if (currentTransforms[0].hasChanged || currentTransforms[1].hasChanged)
            // {
            //     lr.SetPositions(new Vector3[] {currentTransforms[0].localPosition, currentTransforms[1].localPosition});
            //     currentTransforms[0].hasChanged = currentTransforms[1].hasChanged = false;
            // }
        }
    }
}