using System;
using System.IO;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.Extensions;
using TMPro;

namespace CellexalVR.Interaction
{
    /// <summary>
    /// A clickable panel that contains some text, typically a gene name.
    /// </summary>
    public class ClickableTextPanel : ClickablePanel
    {
        public TextMeshPro textMesh;

        /// <summary>
        /// The entire string of text that is displayed on the panel.
        /// </summary>
        public string Text { get; protected set; }

        /// <summary>
        /// The type of the thing whose name is displayed.
        /// </summary>
        public Definitions.Measurement Type { get; protected set; }

        /// <summary>
        /// The name of the thing that is displayed.
        /// </summary>
        public string NameOfThing { get; protected set; } = "";

        /// <summary>
        /// The method of coloring that is used to color this thing.
        /// </summary>
        public GraphManager.GeneExpressionColoringMethods ColoringMethod { get; set; }


        private KeyboardHandler parentKeyboard;

        protected override void Start()
        {
            base.Start();
            parentKeyboard = gameObject.GetComponentInParent<KeyboardHandler>();
            //textMesh = GetComponentInChildren<TextMesh>();
        }

        /// <summary>
        /// Sets the text of this panel.
        /// </summary>
        /// <param name="name">The new text that should be displayed.</param>
        /// <param name="type">The type (gene, attribute or facs) of the text.</param>
        public virtual void SetText(string entryName, Definitions.Measurement type)
        {
            if (!textMesh)
            {
                textMesh = GetComponentInChildren<TextMeshPro>(true);
            }

            Type = type;
            if (Type != Definitions.Measurement.INVALID)
            {
                Text = type + ": " + entryName;
                NameOfThing = entryName;
            }

            else
            {
                Text = "";
                NameOfThing = "";
            }

            textMesh.text = Text;
        }


        /// <summary>
        /// Click this panel. This will color the graphs according to what is on the panel.
        /// </summary>
        public override void Click()
        {
            parentKeyboard.SetAllOutputs(NameOfThing);
            parentKeyboard.SubmitOutput(true);
            referenceManager.geneKeyboard.Clear();
            referenceManager.autoCompleteList.ClearList();
        }
    }
}

