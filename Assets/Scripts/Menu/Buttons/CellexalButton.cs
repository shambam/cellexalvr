using UnityEngine;
using VRTK;
using CellexalVR.General;
using CellexalVR.Menu.Buttons.General;
using CellexalVR.Menu.SubMenus;
using UnityEditor;

namespace CellexalVR.Menu.Buttons
{
    /// <summary>
    /// Abstract general purpose class that represents a button on the menu.
    /// </summary>
    public abstract class CellexalButton : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public TMPro.TextMeshPro descriptionText;
        public GameObject infoMenu;
        public GameObject activeOutline;

        public readonly string laserColliderName =
            "[VRTK][AUTOGEN][RightControllerScriptAlias][StraightPointerRenderer_Tracer]";

        private int frameCount;

        // all buttons must override this variable's get property
        /// <summary>
        /// A string that briefly explains what this button does.
        /// </summary>
        abstract protected string Description { get; }

        // These are drawn in the inspector through CellexalButtonEditor.cs
        [HideInInspector] public Color meshStandardColor = Color.black;
        [HideInInspector] public Color meshHighlightColor = Color.white;
        [HideInInspector] public Color meshDeactivatedColor = Color.grey;
        [HideInInspector] public Sprite standardTexture = null;
        [HideInInspector] public Sprite highlightedTexture = null;
        [HideInInspector] public Sprite deactivatedTexture = null;
        [HideInInspector] public int popupChoice = 0;

        protected SteamVR_TrackedObject rightController;
        protected SteamVR_Controller.Device device;
        protected SpriteRenderer spriteRenderer;
        protected MeshRenderer meshRenderer;
        [HideInInspector] public bool buttonActivated = true;
        public bool storedState;
        public bool controllerInside = false;
        private Transform raycastingSource;
        private int layerMaskNetwork;
        private int layerMaskGraph;
        private int layerMaskMenu;
        private int layerMaskKeyboard;
        private int layermaskEnvironmentButton;
        private int layerMask;
        private bool laserInside;


        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        protected virtual void Awake()
        {
            if (referenceManager == null)
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }

            rightController = referenceManager.rightController;
            device = SteamVR_Controller.Input((int) rightController.index);
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
            //this.tag = "Menu Controller Collider";
            layerMaskNetwork = LayerMask.NameToLayer("NetworkLayer");
            layerMaskKeyboard = 1 << LayerMask.NameToLayer("KeyboardLayer");
            layerMaskMenu = 1 << LayerMask.NameToLayer("MenuLayer");
            layermaskEnvironmentButton = 1 << LayerMask.NameToLayer("EnvironmentButtonLayer");
            layerMask = layerMaskMenu | layerMaskKeyboard | layerMaskNetwork | layermaskEnvironmentButton;
        }

        protected virtual void Update()
        {
            frameCount++;
            if (CrossSceneInformation.Normal)
            {
                CheckForClick();
                CheckForHit();
            }
        }

        private void CheckForClick()
        {
            device = SteamVR_Controller.Input((int) rightController.index);
            if (controllerInside && device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
            {
                Click();
            }

            if (controllerInside && device.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad) &&
                device.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0).y < 0.5f)
            {
                HelpClick();
            }
        }

        /// <summary>
        /// Button sometimes stays active even though ontriggerexit should have been called.
        /// To deactivate button again check every 10th frame if laser pointer collider is colliding.
        /// </summary>
        private void CheckForHit()
        {
            if (!buttonActivated) return;
            if (frameCount % 10 == 0)
            {
                laserInside = false;
                RaycastHit hit;
                raycastingSource = referenceManager.laserPointerController.origin;
                Physics.Raycast(raycastingSource.position, raycastingSource.TransformDirection(Vector3.forward),
                    out hit, 10, layerMask);
                //if (hit.collider) print(hit.collider.transform.gameObject.name);
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
                // if (hit.collider && hit.collider.transform == transform && referenceManager.rightLaser.enabled && buttonActivated)
                // {
                //     laserInside = true;
                //     frameCount = 0;
                //     controllerInside = laserInside;
                //     SetHighlighted(laserInside);
                //     return;
                // }
                // if (!(hit.collider || hit.transform == transform))
                // {
                //     laserInside = false;
                //     controllerInside = laserInside;
                //     SetHighlighted(laserInside);
                //     //if (infoMenu) infoMenu.SetActive(inside);
                // }
                // controllerInside = laserInside;
                // SetHighlighted(laserInside);
                //summertwerk

                if (descriptionText.text == Description && !laserInside)
                {
                    descriptionText.text = "";
                }

                frameCount = 0;
            }
        }

        /// <summary>
        /// Handles what happens when the user points the controller towards the button and presses the trigger.
        /// </summary>
        public abstract void Click();

        protected virtual void HelpClick()
        {
            if (!infoMenu) return;

            infoMenu.GetComponent<VideoButton>().StartVideo();
        }

        public virtual void SetButtonActivated(bool activate)
        {
            //print(name + " setbuttonactivated");
            if (!activate)
            {
                descriptionText.text = "";
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = deactivatedTexture;
                }
                else if (meshRenderer != null)
                {
                    meshRenderer.material.color = meshDeactivatedColor;
                }
            }

            if (activate)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = standardTexture;
                }
                else if (meshRenderer != null)
                {
                    meshRenderer.material.color = meshStandardColor;
                }
            }

            buttonActivated = activate;
            controllerInside = false;
        }

        public void StoreState()
        {
            storedState = buttonActivated;
        }

        /// <summary>
        /// To synchronise the outline in multi-user mode. So the outline doesnt get active unless the other users menu or tab is active.
        /// </summary>
        public void ToggleOutline(bool toggle, bool legend = false)
        {
            var tab = transform.parent.GetComponent<Tab>();
            var menuNoTab = transform.parent.GetComponent<MenuWithoutTabs>();
            if (tab != null && tab.Active)
            {
                activeOutline.SetActive(toggle);
            }
            else if (menuNoTab != null || legend)
            {
                activeOutline.SetActive(toggle);
            }

            storedState = toggle;
        }

        // protected void OnTriggerEnter(Collider other)
        // {
        //     if (!buttonActivated) return;
        //     //print(name + " ontriggerenter");
        //     if (other.gameObject.name == laserColliderName)
        //     {
        //         descriptionText.text = Description;
        //         controllerInside = true;
        //         SetHighlighted(true);
        //     }
        // }

        // In case OnTriggerExit doesnt get called by laser pointer we need to manually do the unhighlighting.
        protected void Exit()
        {
            //print(name + " exit");
            if (descriptionText.text == Description)
            {
                descriptionText.text = "";
            }

            controllerInside = false;
            if (buttonActivated)
            {
                SetHighlighted(false);
            }
            else
            {
                SetButtonActivated(false);
            }

            if (infoMenu)
            {
                infoMenu.SetActive(false);
            }
        }

        // protected void OnTriggerExit(Collider other)
        // {
        //     if (!buttonActivated || laserInside) return;
        //     //print(name + " ontriggerexit");
        //     if (other.gameObject.name == laserColliderName)
        //     {
        //         if (descriptionText.text == Description)
        //         {
        //             descriptionText.text = "";
        //         }
        //
        //         controllerInside = false;
        //         SetHighlighted(false);
        //         //if (infoMenu && !infoMenu.GetComponent<InfoMenu>().active)
        //         //{
        //         //    infoMenu.SetActive(false);
        //         //}
        //     }
        // }

        public virtual void SetHighlighted(bool highlight)
        {
            if (highlight)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = highlightedTexture;
                }
                else if (meshRenderer != null)
                {
                    meshRenderer.material.color = meshHighlightColor;
                }

                if (descriptionText.text == "")
                {
                    descriptionText.text = Description;
                }
            }

            if (!highlight)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = standardTexture;
                }
                else if (meshRenderer != null)
                {
                    meshRenderer.material.color = meshStandardColor;
                }
            }

            if (infoMenu)
            {
                infoMenu.SetActive(highlight);
            }

            controllerInside = highlight;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// A custom inspector for all buttons to help show only fields that are used.
    /// </summary>
    [CustomEditor(typeof(CellexalButton), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    public class CellexalButtonEditor : Editor
    {
        public string[] buttonTypeOptions = new string[] {"Mesh", "Sprite"};

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var buttonScript = target as CellexalButton;
            buttonScript.popupChoice = EditorGUILayout.Popup("Button Type", buttonScript.popupChoice, buttonTypeOptions,
                EditorStyles.popup);
            if (buttonScript.popupChoice == 0)
            {
                //EditorGUILayout.PrefixLabel("Mesh options");
                buttonScript.meshStandardColor =
                    EditorGUILayout.ColorField("Standard Color", buttonScript.meshStandardColor);
                buttonScript.meshHighlightColor =
                    EditorGUILayout.ColorField("Highlighted Color", buttonScript.meshHighlightColor);
                buttonScript.meshDeactivatedColor =
                    EditorGUILayout.ColorField("Deactivated Color", buttonScript.meshDeactivatedColor);
            }
            else if (buttonScript.popupChoice == 1)
            {
                //EditorGUILayout.PrefixLabel("Sprite options");
                buttonScript.standardTexture = (UnityEngine.Sprite) EditorGUILayout.ObjectField("Standard texture",
                    buttonScript.standardTexture, typeof(UnityEngine.Sprite), true);
                buttonScript.highlightedTexture = (UnityEngine.Sprite) EditorGUILayout.ObjectField(
                    "Highlighted texture", buttonScript.highlightedTexture, typeof(UnityEngine.Sprite), true);
                buttonScript.deactivatedTexture = (UnityEngine.Sprite) EditorGUILayout.ObjectField(
                    "Deactivated texture", buttonScript.deactivatedTexture, typeof(UnityEngine.Sprite), true);
            }

            //EditorUtility.SetDirty(buttonScript);
            base.OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}