using UnityEngine;
using CellexalVR.General;
using VRTK;
using CellexalVR.Menu;

namespace CellexalVR.Interaction
{
    /// <summary>
    /// Make events for click on radial menu object since prefab does not keep references to other objects when building the scene.
    /// Depending on the object this script is attached to the events differ. The method names should be explanatory.
    /// </summary>
    public class EventSetter : MonoBehaviour
    {
        public ReferenceManager referenceManager;

        private PushBack pushBack;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            pushBack = GetComponentInParent<PushBack>();
        }

        public void LeftMenuLeftClickEvent()
        {
            referenceManager.menuRotator.RotateRight(1);
        }

        public void LeftMenuRightClickEvent()
        {
            referenceManager.menuRotator.RotateLeft(1);
        }

        public void LeftMenuUpClickEvent()
        {
            referenceManager.teleportLaser.SetActive(!referenceManager.teleportLaser.activeSelf);
            //if (referenceManager.leftController.GetComponent<MenuToggler>().MenuActive)
            //{
            //    referenceManager.leftController.GetComponent<MenuToggler>().ToggleMenu();
            //}
        }

        public void LeftMenuDownClickEvent()
        {
            referenceManager.controllerModelSwitcher.SwitchSelectionToolMesh(false);
        }


        public void RightMenuLeftClickEvent()
        {
            referenceManager.selectionToolCollider.ChangeColor(false);
        }

        public void RightMenuRightClickEvent()
        {
            referenceManager.selectionToolCollider.ChangeColor(true);

        }

        public void RightMenuUpClickEvent()
        {
            if (referenceManager.controllerModelSwitcher.ActualModel == ControllerModelSwitcher.Model.SelectionTool)
            {
                referenceManager.controllerModelSwitcher.SwitchSelectionToolMesh(true);
            }
        }

        public void RightMenuUpHoldEvent()
        {
            if (referenceManager.rightLaser.enabled &&
                referenceManager.controllerModelSwitcher.ActualModel != ControllerModelSwitcher.Model.Menu)
            {
                pushBack.Push();
            }
        }

        public void RightMenuDownHoldEvent()
        {
            if (referenceManager.rightLaser.enabled &&
                referenceManager.controllerModelSwitcher.ActualModel != ControllerModelSwitcher.Model.Menu)
            {
                pushBack.Pull();
            }
        }

        public void RightMenuDownClickEvent()
        {
            if (referenceManager.controllerModelSwitcher.ActualModel == ControllerModelSwitcher.Model.SelectionTool)
            {
                referenceManager.controllerModelSwitcher.SwitchSelectionToolMesh(false);
            }
        }

        public void GeneKeyboardEnterEvent(string geneName)
        {
            referenceManager.cellManager.ColorGraphsByGene(geneName);
            referenceManager.multiuserMessageSender.SendMessageColorGraphsByGene(geneName);
        }

        public void GeneKeyboardEditEvent(string s)
        {
            referenceManager.multiuserMessageSender.SendMessageKeyClicked(s);
        }

        public void GeneKeyboardAnnotateEvent(string s)
        {
            int index = referenceManager.selectionToolCollider.currentColorIndex;
            referenceManager.annotationManager.AddAnnotation(s, index);
            referenceManager.multiuserMessageSender.SendMessageAddAnnotation(s, index);
        }


        public void BrowserKeyboardEditEvent(string s)
        {
            referenceManager.multiuserMessageSender.SendMessageBrowserKeyClicked(s);
        }

        public void FolderKeyboardEditEvent(string filter)
        {
            referenceManager.inputFolderGenerator.GenerateFolders(filter);
        }

    }
}




