using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellexalVR.General;
namespace CellexalVR.AnalysisLogic.H5reader
{
    public class AnchorScript : MonoBehaviour
    {

        public ReferenceManager referenceManager;
        private SteamVR_TrackedObject rightController;

        public bool isAnchorA;
        public RectTransform rect;
        public AnchorScript anchorA;
        public AnchorScript anchorB;
        public LineScript line;

        public ExpandButtonScript expandButtonScript;
        public bool isAttachedToHand = false;
        
        // Start is called before the first frame update
        void Start()
        {
            if (!referenceManager)
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
            rightController = referenceManager.rightController;
        }

        public void AttachAnchorBToHand()
        {
            if (!rightController.GetComponentInChildren<AnchorScript>())
            {
                if (line.IsExpanded() && line.isMulti)
                {
                    LineScript newLine = line.AddLine();
                    newLine.AnchorB.transform.parent = rightController.transform;
                    newLine.AnchorB.transform.position = rightController.transform.position;
                    newLine.AnchorB.transform.localPosition = new Vector3(0, -0.01f, 0.02f);
                    newLine.AnchorB.isAttachedToHand = true;
                }
                else
                {
                    if (expandButtonScript)
                    {
                        expandButtonScript.pressButton();
                    }

                    transform.parent = rightController.transform;
                    transform.position = rightController.transform.position;
                    transform.localPosition = new Vector3(0, -0.01f, 0.02f);
                    isAttachedToHand = true;


                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            SteamVR_Controller.Device device = SteamVR_Controller.Input((int)rightController.index);
            if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger) && !isAnchorA && isAttachedToHand && DistBetweenAnchors() > 0.10f && !expandButtonScript) //Pressing in free space return to hand
            {
                transform.parent = anchorA.rect.parent;
                transform.localPosition = anchorA.rect.localPosition;
                isAttachedToHand = false;
            }

        }

        private void OnTriggerEnter(Collider other)
        {
            ExpandButtonScript ebs = other.gameObject.GetComponent<ExpandButtonScript>();
            if (ebs && ebs.parentScript.isBottom)
            {
                expandButtonScript = ebs;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            ExpandButtonScript ebs = other.gameObject.GetComponent<ExpandButtonScript>();
            if (ebs == expandButtonScript)
            {
                expandButtonScript = null;
            }
        }

        public float DistBetweenAnchors()
        {
            return Vector3.Distance(anchorA.rect.position, anchorB.transform.position);
        }
    }
}
