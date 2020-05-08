using CellexalVR.Menu.SubMenus;
using CellexalVR.General;
using UnityEngine;

namespace CellexalVR.Menu.Buttons
{
    /// <summary>
    /// Represents a button that opens a pop up menu.
    /// </summary>
    public class SubMenuButton : CellexalButton
    {
        public string description;
        public GameObject buttonsToDeactivate;
        public GameObject menu;
        public TMPro.TextMeshPro textMeshToDarken;

        private Tab activeTab;

        protected override string Description
        {
            get { return description; }
        }

        private void Start()
        {
            // The gameobject should be active but the renderers and colliders should be disabled.
            // This makes the buttons in the menu able to receive events while not being shown.
            menu.SetActive(true);
            SetMenuActivated(false);
            CellexalEvents.GraphsUnloaded.AddListener(TurnOff);
            CellexalEvents.GraphsLoaded.AddListener(TurnOn);
        }

        public override void Click()
        {
            OpenMenu();
        }

        public void OpenMenu()
        {
            DeactivateButtonsRecursive(buttonsToDeactivate);
            textMeshToDarken.GetComponent<MeshRenderer>().enabled = false;
            SetMenuActivated(true);
            //if (menu.GetComponent<VelocitySubMenu>() != null)
            //{
            //    menu.GetComponent<VelocitySubMenu>().Active = true;
            //}
            var tabs = menu.GetComponentsInChildren<Tab>();
            if (tabs.Length > 0)
            {
                var firstTab = tabs[0];
                firstTab.SetTabActive(true);
                firstTab.TabButton.SetHighlighted(true);
                // menu.GetComponent<MenuWithTabs>().active = true;
            }

            var subMenuWithouthTabs = menu.GetComponent<MenuWithoutTabs>();
            if (subMenuWithouthTabs != null)
            {
                subMenuWithouthTabs.SetMenuActive(true);
            }

            descriptionText.text = "";
        }

        private void DeactivateButtonsRecursive(GameObject buttonsToDeactivate)
        {
            foreach (Transform t in buttonsToDeactivate.transform)
            {
                if (infoMenu != null)
                {
                    infoMenu.SetActive(false);
                }

                // if this is a button, deactivate it
                CellexalButton b = t.GetComponent<CellexalButton>();
                if (b != null)
                {
                    b.StoreState();
                    b.SetButtonActivated(false);
                }

                // recursive call to include all children of children
                DeactivateButtonsRecursive(t.gameObject);
            }
        }

        /// <summary>
        /// Show or hides the submenu
        /// </summary>
        /// <param name="activate"> True for showing the submenu, false for hiding. </param>
        public void SetMenuActivated(bool activate)
        {
            // Turn on or off the menu it self
            MenuWithTabs menuWithTabs = menu.GetComponent<MenuWithTabs>();
            if (menuWithTabs) menuWithTabs.Active = activate;
            Renderer menuRenderer = menu.GetComponent<Renderer>();
            if (menuRenderer)
                menuRenderer.enabled = activate;
            Collider menuCollider = menu.GetComponent<Collider>();
            if (menuCollider)
                menuCollider.enabled = activate;
            // Go through all the objects in the menu
            foreach (Transform t in menu.transform)
            {
                // For everything that is not a tab, just deal with it normally
                Tab tab = t.GetComponent<Tab>();
                if (!tab)
                {
                    SetGameObjectAndChildrenEnabled(t.gameObject, activate);
                }
                else
                {
                    // for everything that is a tab
                    if (!activate)
                    {
                        // if we are turning off the menu
                        // save the active tab
                        if (tab.Active)
                            activeTab = tab;
                        tab.SetTabActive(false);
                        SetGameObjectAndChildrenEnabled(tab.TabButton.gameObject, false);
                    }
                    else
                    {
                        // if we are turning on the menu
                        // skip the tab prefabs
                        if (menuWithTabs && tab == menuWithTabs.tabPrefab) continue;

                        // if we have a saved tab that should be active, turn on that one and turn off the other ones.
                        // if there is no saved active tab, turn all tabs off
                        if (activeTab != null)
                            tab.SetTabActive(tab == activeTab);
                        else
                        {
                            tab.SetTabActive(false);
                        }

                        SetGameObjectAndChildrenEnabled(tab.TabButton.gameObject, true);
                    }
                }
            }
        }

        private void SetGameObjectAndChildrenEnabled(GameObject obj, bool active)
        {
            foreach (var r in obj.GetComponentsInChildren<Renderer>())
            {
                if (r.gameObject.name.Equals("ActiveOutline"))
                {
                    r.gameObject.SetActive(r.transform.parent.GetComponent<CellexalButton>().storedState);
                }

                r.enabled = active;
            }

            foreach (var c in obj.GetComponentsInChildren<Collider>())
            {
                c.enabled = active;
                if (c.gameObject.name.Equals("ActiveOutline"))
                {
                    c.gameObject.SetActive(c.transform.parent.GetComponent<CellexalButton>().storedState);
                }

                c.enabled = active;
            }
        }

        void TurnOn()
        {
            SetButtonActivated(true);
        }

        void TurnOff()
        {
            SetButtonActivated(false);
        }
    }
}