using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CellexalVR.General;
namespace CellexalVR.AnalysisLogic.H5reader
{
    public class ExpandButtonScript : MonoBehaviour
    {
        private ReferenceManager referenceManager;
        public H5ReaderAnnotatorTextBoxScript parentScript;
        private SteamVR_TrackedObject rightController;
        private SteamVR_Controller.Device device;
        private GameObject controller;
        public Image image;
        public bool isExpanded;
        public Sprite plusImage;
        public Sprite minusImage;
        public Sprite circleImage;
        public bool anchorInside;

        // Start is called before the first frame update
        void Start()
        {
            if (!referenceManager)
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
            rightController = referenceManager.rightController;
            parentScript = GetComponentInParent<H5ReaderAnnotatorTextBoxScript>();
            if (parentScript.isBottom)
            {
                image.sprite = circleImage;
            }
            else
            {
                image.sprite = plusImage;
                isExpanded = false;
            }

        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name.Equals("ControllerCollider(Clone)"))
            {
                controller = other.gameObject;

            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject == controller)
            {
                controller = null;
            }
        }

        public void pressButton()
        {
            AnchorScript anchorScript = GetComponentInChildren<AnchorScript>();
            AnchorScript anchor = rightController.GetComponentInChildren<AnchorScript>();
            if (!parentScript.isBottom)
            {
                isExpanded = !isExpanded;
                if (isExpanded)
                {
                    image.sprite = minusImage;
                }
                else
                {
                    image.sprite = plusImage;
                }

                foreach (H5ReaderAnnotatorTextBoxScript key in parentScript.subkeys.Values)
                {
                    key.gameObject.SetActive(!key.gameObject.activeSelf);
                }
                H5ReaderAnnotatorTextBoxScript parent = parentScript;
                while (!parent.isTop)
                    parent = parent.transform.parent.GetComponent<H5ReaderAnnotatorTextBoxScript>();
                parent.GetComponentInParent<H5ReaderAnnotater>().resizeDisplay(parent.UpdatePosition(10f));
            }
            else if (anchorScript)
            {
                anchorScript.transform.parent = rightController.transform;
                anchorScript.transform.position = rightController.transform.position;
                anchorScript.transform.localPosition = new Vector3(0, -0.01f, 0.02f);
                anchorScript.isAttachedToHand = true;
                ProjectionObjectScript projectionObjectScript = anchorScript.anchorA.GetComponentInParent<ProjectionObjectScript>();
                if (projectionObjectScript)
                {
                    projectionObjectScript.RemoveFromPaths(anchorScript.line.type);
                }
                else
                {
                    H5ReaderAnnotater h5ReaderAnnotater = anchorScript.anchorA.GetComponentInParent<H5ReaderAnnotater>();


                    if (anchorScript.line.type == "attrs")
                    {

                        h5ReaderAnnotater.RemoveFromConfig("attr_" + parentScript.GetName());
                    }
                    else
                    {
                        h5ReaderAnnotater.RemoveFromConfig(anchorScript.line.type);
                    }
                }
            }else if (anchor)
            {
                anchor.transform.parent = transform;
                anchor.transform.localPosition = Vector3.zero;
                anchor.isAttachedToHand = false;

                string path = parentScript.GetPath();
                char dataType = parentScript.GetDataType();
                string name = parentScript.GetName();

                ProjectionObjectScript projectionObjectScript = anchor.anchorA.GetComponentInParent<ProjectionObjectScript>();
                if (projectionObjectScript)
                {
                    if (anchor.line.type == "X")
                    {
                        anchor.anchorA.GetComponentInParent<ProjectionObjectScript>().ChangeName(name);
                    }
                    projectionObjectScript.AddToPaths(anchor.line.type, path, dataType);

                }
                else
                {
                    H5ReaderAnnotater h5ReaderAnnotater = anchor.anchorA.GetComponentInParent<H5ReaderAnnotater>();
                    if (anchor.line.type == "attrs")
                    {
                        h5ReaderAnnotater.AddToConfig("attr_" + name, path, dataType);
                    }
                    else
                    {
                        h5ReaderAnnotater.AddToConfig(anchor.line.type, path, dataType);
                    }

                }
            }
        }

        private void Update()
        {

        }
    }
}
