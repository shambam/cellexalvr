using CellexalVR.General;
using UnityEngine;
using UnityEngine.UI;
namespace CellexalVR.AnalysisLogic.H5reader
{
    public class ScrollbarInteractionScript : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        private SteamVR_TrackedObject rightController;
        private SteamVR_Controller.Device device;
        private bool controllerInside;
        public RectTransform handleRect;
        public RectTransform slidingArea;
        public Scrollbar scrollbar;
        public BoxCollider boxCollider;

        // Start is called before the first frame update
        private void Start()
        {
            if (!referenceManager)
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
            rightController = referenceManager.rightController;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name.Equals("ControllerCollider(Clone)"))
            {
                controllerInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name.Equals("ControllerCollider(Clone)"))
            {
                controllerInside = false;
            }
        }

        private void Update()
        {
            device = SteamVR_Controller.Input((int)rightController.index);
            if (controllerInside && device.GetPress(SteamVR_Controller.ButtonMask.Trigger))
            {
                float temp;
                //Horizontal
                if (slidingArea.rect.height > slidingArea.rect.width)
                {
                    float y = slidingArea.transform.InverseTransformPoint(device.transform.pos).y;
                    float height = slidingArea.rect.height - scrollbar.handleRect.rect.height/2f;
                    temp = Mathf.Clamp01(y / height + 0.5f);
                }
                else
                {
                    float x = slidingArea.transform.InverseTransformPoint(device.transform.pos).x;
                    float width = slidingArea.rect.width - scrollbar.handleRect.rect.width / 2f;
                    temp = Mathf.Clamp01(x / width + 0.5f);
                }


                scrollbar.value = temp;

                Vector3 size = handleRect.rect.size;
                size.z = 10;
                boxCollider.size = size;

            }
        }

    }
}


