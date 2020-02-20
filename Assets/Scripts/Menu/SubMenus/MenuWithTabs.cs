using CellexalVR.Menu.Buttons;
using System.Collections.Generic;
using UnityEngine;
using CellexalVR.General;
using TMPro;

namespace CellexalVR.Menu.SubMenus
{
    /// <summary>
    /// Represents a menu that has tabs. Tabs are meant to function much like tabs in a web browser.
    /// </summary>
    public class MenuWithTabs : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public Tab tabPrefab;
        public GameObject nextPageButton;
        public GameObject prevPageButton;
        public TextMeshPro pageNrText;
        //public Dictionary<CellexalButton, bool> savedButtonStates = new Dictionary<CellexalButton, bool>();

        protected MenuToggler menuToggler;
        protected List<Tab> tabs = new List<Tab>();
        protected Vector3 tabButtonPos = new Vector3(-0.309f, 1f, 0.325f);
        protected Vector3 tabButtonPosOriginal = new Vector3(-0.309f, 1f, 0.325f);
        protected Vector3 tabButtonPosInc = new Vector3(0.2f, 0, 0);

        public CellexalButton prefab;

        protected List<CellexalButton> buttons;


        protected int buttonsPerTab = 20;
        protected string[] names;
        protected string[] categories;

        private int currentPage = 0;
        private int pageCounter = 0;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
                //GetComponent<Renderer>().sharedMaterial = GameObject.Find("Main Menu").GetComponent<MenuRotator>().menuMaterial;
            }
        }

        protected virtual void Start()
        {
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            menuToggler = referenceManager.menuToggler;
            CellexalEvents.GraphsUnloaded.AddListener(DestroyTabs);
        }

        /// <summary>
        /// Finds a button by its name.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <returns>The button, or null if no button was found.</returns>
        public virtual CellexalButton FindButton(string name)
        {
            var button = buttons.Find(x => x.name == name);
            return button;
        }


        public virtual void CreateButtons(string[] categoriesAndNames)
        {
            DestroyTabs();

            if (buttons == null)
                buttons = new List<CellexalButton>();
            foreach (var button in buttons)
            {
                // wait 0.1 seconds so we are out of the loop before we start destroying stuff
                if (button != null)
                {
                    Destroy(button.gameObject, .1f);
                }
            }
            buttons.Clear();
            //TurnOffAllTabs();
            categories = new string[categoriesAndNames.Length];
            names = new string[categoriesAndNames.Length];
            for (int i = 0; i < categoriesAndNames.Length; ++i)
            {
                if (categoriesAndNames[i].Contains("@"))
                {
                    string[] categoryAndName = categoriesAndNames[i].Split('@');
                    categories[i] = categoryAndName[0];
                    names[i] = categoryAndName[1];
                }
                else
                {
                    categories[i] = "";
                    names[i] = categoriesAndNames[i];
                }
            }

            Tab newTab = null;
            for (int i = 0, buttonIndex = 0; i < names.Length; ++i, ++buttonIndex)
            {
                // add a new tab if we encounter a new category, or if the current tab is full
                if (buttonIndex % buttonsPerTab == 0 || i > 0 && categories[i] != categories[i - 1])
                {
                    if (tabs.Count > 0 && tabs.Count % 4 == 0)
                    {
                        nextPageButton.SetActive(true);
                        tabButtonPos = tabButtonPosOriginal;
                        pageCounter++;
                        pageNrText.text = "p. " + (currentPage + 1) + "/" + (pageCounter + 1);
                    }
                    newTab = AddTab(tabPrefab);
                    newTab.TabButton.GetComponentInChildren<TextMeshPro>().text = categories[i];
                    buttonIndex = 0;

                }
                var newButton = Instantiate(prefab, newTab.transform);

                newButton.gameObject.SetActive(true);

                //menuToggler.AddGameObjectToActivate(newButton.gameObject, gameObject);

                //if (buttonIndex < Colors.Length)
                //    newButton.GetComponent<Renderer>().material.color = Colors[buttonIndex];
                buttons.Add(newButton);
                newTab.AddButton(newButton);
            }
            // set the names of the attributes after the buttons have been created.
            //for (int i = 0; i < buttons.Count; ++i)
            //{
            //    var b = buttons[i];
            //    b.referenceManager = referenceManager;
            //    //int colorIndex = i % Colors.Length;
            //    b.SetIndex(names[i]);
            //    b.parentMenu = this;
            //}
            // turn on one of the tabs
            TurnOffAllTabs();
            //newTab.SetTabActive(true);
            //newTab.SetTabActive(GetComponent<Renderer>().enabled);
        }
        /// <summary>
        /// Adds a tab to this menu.
        /// </summary>
        /// <typeparam name="T"> The type of the tab. This type must derive from <see cref="Tab"/>. </typeparam>
        /// <param name="tabPrefab"> The prefab used as template. </param>
        /// <returns> A reference to the created tab. The created tab will have the type T. </returns>
        public virtual T AddTab<T>(T tabPrefab) where T : Tab
        {
            var newTab = Instantiate(tabPrefab, transform);
            if (tabs.Count >= 4)
            {
                newTab.gameObject.SetActive(false);
            }
            else
            {
                newTab.gameObject.SetActive(true);
            }
            //newTab.SetTabActive(false);
            //newTab.transform.parent = transform;
            newTab.TabButton.gameObject.transform.localPosition = tabButtonPos;
            newTab.TabButton.Menu = this;
            tabButtonPos += tabButtonPosInc;
            tabs.Add(newTab);
            if (!menuToggler)
                menuToggler = referenceManager.menuToggler;
            // tell the menu toggler to activate the tab button later if the menu is not active
            //menuToggler.AddGameObjectToActivate(newTab.TabButton.gameObject);
            return newTab;
        }

        /// <summary>
        /// Destroys one tab.
        /// </summary>
        /// <param name=""> The name of the tab to be destroyed (same as the network name corresponding to the tab). </param>
        public virtual void DestroyTab(string networkName)
        {
            Tab t = tabs.Find(tab => tab.gameObject.name.Split('_')[1].Equals(networkName));
            if (t)
            {
                tabs.Remove(t);
                Destroy(t.gameObject, 0.1f);
            }
            //foreach (Tab t in tabs)
            //{
            //    if (t.gameObject.name.Split('_')[1] == networkName)
            //    {
            //        tabs.Remove(t);
            //        Destroy(t.gameObject, 0.1f);

            //    }
            //}
            //ResetTabButtonPosition();
            //tabs.Clear();
        }

        /// <summary>
        /// Destroys all tabs.
        /// </summary>
        public virtual void DestroyTabs()
        {
            foreach (Tab t in tabs)
            {
                Destroy(t.gameObject, 0.1f);
            }
            ResetTabButtonPosition();
            tabs.Clear();
        }

        /// <summary>
        /// Reset the position of where the next tab button should be created.
        /// </summary>
        public virtual void ResetTabButtonPosition()
        {
            tabButtonPos = tabButtonPosOriginal;
        }

        /// <summary>
        /// Turns off all tabs.
        /// </summary>
        public void TurnOffAllTabs()
        {
            foreach (Tab tab in tabs)
            {
                tab.SetTabActive(false);
            }
        }

        public void ChangePage(int dir)
        {
            currentPage += dir;
            pageNrText.text = "p. " + (currentPage + 1) + "/" + (pageCounter + 1);
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].gameObject.SetActive((int)(i / 4) == currentPage);
            }
            if (currentPage == pageCounter)
            {
                nextPageButton.SetActive(false);
            }
            else if (currentPage == 0)
            {
                prevPageButton.SetActive(false);
            }
            else
            {
                nextPageButton.SetActive(true);
                prevPageButton.SetActive(true);
            }

        }
    }
}