using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CellexalVR.AnalysisObjects;
using CellexalVR.Menu.Buttons;
using CellexalVR.Menu.Buttons.Slicing;
using UnityEngine;
using Valve.VR;

namespace CellexalVR.Spatial
{
    public class Slicer : MonoBehaviour
    {
        // public Transform t1, t2, t3, t4, t5, t6;
        // public Transform[] currentTransforms = new Transform[2];
        // public int axis;
        public GameObject blade;
        public GameObject plane;
        public GraphSlicer graphSlicer;
        public GameObject slicingMenuParent;
        [HideInInspector] public bool sliceAnimationActive;

        public int Axis { get; set; }

        public bool Automatic { get; set; }

        private int axis;
        private LineRenderer lr;
        private Graph graph;
        private CellexalButton sliceGraphButton;

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


        public void SliceGraph()
        {
            StartCoroutine(graphSlicer.SliceGraph(Automatic, axis, true));
        }

        public void ToggleManualSlicer(bool toggle)
        {
            Automatic = !toggle;
            plane.SetActive(toggle);
            Axis = 2;
        }

        public void ChangeAxis(int axis)
        {
            Axis = axis;
            if (axis == -1)
            {
                sliceGraphButton.SetButtonActivated(false);
            }
            else
            {
                foreach (SliceAxisToggleButton sb in GetComponentsInChildren<SliceAxisToggleButton>())
                {
                    if (sb.axis == axis) return;
                    sb.CurrentState = false;
                }
                
                sliceGraphButton.SetButtonActivated(true);
            }
        }


        public IEnumerator sliceAnimation()
        {
            sliceAnimationActive = true;
            float t = 0f;
            float yStart = 0.5f;
            float yEnd = -0.5f;
            // const float animationTime = 1f;
            Vector3 pos = blade.transform.localPosition;
            float progress;
            plane.SetActive(true);
            float animationTime = 2.0f; // / cutPositions.Length;
            Vector3 bladePos = blade.transform.localPosition;
            Vector3 startPos = new Vector3(bladePos.x, 0.5f, bladePos.z);
            Vector3 endPos = new Vector3(bladePos.x, -0.5f, bladePos.z);
            float speed = 2f;
            float step = speed * Time.deltaTime;

            while (t < animationTime)
            {
                bladePos = Vector3.MoveTowards(blade.transform.localPosition, endPos, step);
                blade.transform.localPosition = bladePos;
                t += Time.deltaTime;
                yield return null;
            }

            t = 0f;
            startPos = new Vector3(bladePos.x, -0.5f, bladePos.z);
            endPos = new Vector3(bladePos.x, 0.5f, bladePos.z);

            while (t < animationTime)
            {
                bladePos = Vector3.MoveTowards(blade.transform.localPosition, endPos, step);
                blade.transform.localPosition = bladePos;
                t += Time.deltaTime;
                yield return null;
            }

            plane.SetActive(false);
            sliceAnimationActive = false;
        }

        public IEnumerator sliceAnimation(Vector3[] cutPositions, int axis)
        {
            sliceAnimationActive = true;

            switch (axis)
            {
                case 0:
                    plane.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    break;
                case 1:
                    plane.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                default:
                    plane.transform.localRotation = Quaternion.identity;
                    break;
            }

            float animationTime = 4.0f / cutPositions.Length;
            Vector3 bladePos = blade.transform.localPosition;
            Vector3 planePos = Vector3.zero; // plane.transform.localPosition;
            Vector3 startPos = new Vector3(bladePos.x, 0.5f, bladePos.z);
            Vector3 endPos = new Vector3(bladePos.x, -0.5f, bladePos.z);
            float speed = cutPositions.Length / 4f;
            float step = speed * Time.deltaTime;
            plane.SetActive(true);
            for (int i = 0; i < cutPositions.Length; i++)
            {
                float t = 0f;
                planePos[axis] = cutPositions[i][axis]; // / plane.transform.localScale.z;
                plane.transform.localPosition = planePos; //cutPositions[i];
                blade.transform.localPosition = startPos;

                while (t < animationTime)
                {
                    bladePos = Vector3.MoveTowards(blade.transform.localPosition, endPos, step);
                    blade.transform.localPosition = bladePos;
                    t += Time.deltaTime;
                    yield return null;
                }
            }

            plane.SetActive(false);
            plane.transform.localRotation = Quaternion.identity;
            plane.transform.localPosition = Vector3.zero;
            blade.transform.localPosition = startPos;
            sliceAnimationActive = false;
        }


        public Plane GetPlane()
        {
            return new Plane(plane.transform.forward, plane.transform.position);
        }


        private void OnTriggerEnter(Collider other)
        {
            // controllerInside = true;
            print($"slicer collider : {other.gameObject.name}" );
        }

        private void OnTriggerExit(Collider other)
        {
            // controllerInside = false;
        }

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