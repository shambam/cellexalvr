using CellexalVR.General;
using CellexalVR.Menu.SubMenus;
using UnityEngine;

namespace CellexalVR.Menu.Buttons
{
    /// <summary>
    /// Represents a button that closes a menu that is opened on top of the main menu.
    /// </summary>
    public class CloseMenuButton : CellexalButton
    {
        public GameObject buttonsToActivate;
        public GameObject menuToClose;
        public TMPro.TextMeshPro textMeshToUndarken;

        public bool deactivateMenu = false;

        protected override string Description
        {
            get
            {
                return "Close menu";
            }
        }

        public override void Click()
        {
            spriteRenderer.sprite = standardTexture;
            controllerInside = false;
            descriptionText.text = "";
            CloseMenu();
        }

        public void CloseMenu()
        {
            if (deactivateMenu)
            {
                menuToClose.SetActive(false);
                menuToClose.GetComponent<MenuWithoutTabs>().SetMenuActive(false);
            }

            else
            {
                foreach (Renderer r in menuToClose.GetComponentsInChildren<Renderer>())
                    r.enabled = false;
                foreach (Collider c in menuToClose.GetComponentsInChildren<Collider>())
                    c.enabled = false;
            }
            //textMeshToUndarken.GetComponent<Renderer>().material.SetColor("_Color", Color.white);
            MenuWithTabs subMenu = menuToClose.GetComponent<MenuWithTabs>();
            
            textMeshToUndarken.GetComponent<MeshRenderer>().enabled = true;
            foreach (CellexalButton b in buttonsToActivate.GetComponentsInChildren<CellexalButton>())
            {
                b.SetButtonActivated(b.storedState);
            }

            if (subMenu) subMenu.Active = false;
            CellexalEvents.MenuClosed.Invoke();
        }
    }
}