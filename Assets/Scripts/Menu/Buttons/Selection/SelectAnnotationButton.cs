using CellexalVR.General;
using TMPro;
using UnityEngine;

namespace CellexalVR.Menu.Buttons.Selection
{
    public class SelectAnnotationButton : CellexalButton
    {
        public string Path { get; set; }
        public TextMeshPro buttonDescription;
        private bool toggle;


        protected override string Description => "Add previous annotations.";


        private void Start()
        {
            rightController = referenceManager.rightController;
            CellexalEvents.GraphsReset.AddListener(ResetButton);
            CellexalEvents.AnnotationsCleared.AddListener(ResetButton);
        }

        public override void Click()
        {
            ToggleOutline(!toggle);
            referenceManager.annotationManager.ToggleAnnotationFile(Path, !toggle);
            toggle = !toggle;
        }

        private void ResetButton()
        {
            toggle = false;
            ToggleOutline(false);
        }
        
    }
}