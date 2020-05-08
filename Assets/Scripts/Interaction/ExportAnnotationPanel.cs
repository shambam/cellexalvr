using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using UnityEngine;

namespace CellexalVR.Interaction
{
    /// <summary>
    /// 
    /// </summary>
    public class ExportAnnotationPanel : ClickablePanel
    {
        public TMPro.TextMeshPro text;

        private AnnotationManager annotationManager;

        protected override void Start()
        {
            base.Start();
            annotationManager = referenceManager.annotationManager;
        }


        /// <summary>
        /// Click this panel, calling the dump function in the annotation manager which will export the
        /// cell names together with annotations to a text file.
        /// </summary>
        public override void Click()
        {
            referenceManager.multiuserMessageSender.SendMessageExportAnnotations();
            annotationManager.DumpAnnotatedSelectionToTextFile();
        }
    }
}