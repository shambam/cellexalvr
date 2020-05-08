using CellexalVR.General;
using CellexalVR.Menu.SubMenus;
using UnityEngine;

namespace CellexalVR.Menu.Buttons
{
    /// <summary>
    /// Represents the tab buttons on top of a tab.
    /// </summary>
    public class TabButton : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public Tab tab;
        public MenuWithTabs Menu;

        protected SteamVR_TrackedObject rightController;
        protected bool controllerInside = false;
        protected SteamVR_Controller.Device device;
        private MeshRenderer meshRenderer;
        private Color standardColor = Color.grey;
        private Color highlightColor = Color.blue;

        private readonly string laserCollider =
            "[VRTK][AUTOGEN][RightControllerScriptAlias][StraightPointerRenderer_Tracer]";

        private bool highlight;

        private int frameCount;

        // private bool laserInside;
        private Transform raycastingSource;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        protected virtual void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material.color = standardColor;
            if (!CrossSceneInformation.Spectator)
            {
                rightController = referenceManager.rightController;
            }

            this.tag = "Menu Controller Collider";
        }

        protected virtual void Update()
        {
            if (!CrossSceneInformation.Normal) return;

            device = SteamVR_Controller.Input((int) rightController.index);
            if (controllerInside && device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
            {
                Menu.TurnOffAllTabs();
                tab.SetTabActive(true);
                highlight = true;
                SetHighlighted(highlight);
            }

            if (!tab.Active && highlight && !controllerInside)
            {
                highlight = false;
                SetHighlighted(highlight);
            }

            if (highlight && meshRenderer.material.color != highlightColor)
            {
                SetHighlighted(highlight);
            }

            CheckForHit();
        }

        /// <summary>
        /// Button sometimes stays active even though ontriggerexit should have been called.
        /// To deactivate button again check every 10th frame if laser pointer collider is colliding.
        /// </summary>
        private void CheckForHit()
        {
            if (!Menu.Active || tab.Active) return;

            if (frameCount % 10 != 0) return;
            // laserInside = false;
            RaycastHit hit;
            raycastingSource = referenceManager.laserPointerController.origin;
            Physics.Raycast(raycastingSource.position, raycastingSource.TransformDirection(Vector3.forward), out hit,
                10);
            if (hit.collider && hit.collider.transform == transform && referenceManager.rightLaser.enabled)
            {
                // laserInside = true;
                frameCount = 0;
                controllerInside = true;
                SetHighlighted(true);
                return;
            }

            controllerInside = false;
            SetHighlighted(false);
            frameCount = 0;
            // if (!hit.collider || !hit.collider.transform == transform)
            // {
            // laserInside = false;
            return;
            // }
            // controllerInside = laserInside;
            // SetHighlighted(controllerInside);
        }

        protected void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name == laserCollider)
            {
                highlight = true;
                controllerInside = true;
                SetHighlighted(highlight);
            }
        }

        protected void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name == laserCollider)
            {
                controllerInside = false;
                if (!tab.Active)
                {
                    highlight = false;
                    SetHighlighted(highlight);
                }
            }
        }

        /// <summary>
        /// Changes the color of the button to either its highlighted color or standard color.
        /// </summary>
        /// <param name="highlight"> True if the button should be highlighted, false otherwise. </param>
        public virtual void SetHighlighted(bool h)
        {
            highlight = h;
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            meshRenderer.material.color = h ? highlightColor : standardColor;
        }
    }
}