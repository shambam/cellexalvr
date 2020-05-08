using System;
using System.IO;
using System.Linq;
using CellexalVR.Extensions;
using CellexalVR.General;
using TMPro;
using UnityEngine;

namespace CellexalVR.Interaction
{
    public sealed class ClickableHistoryPanel : ClickablePanel
    {
        public TextMeshPro textMesh;

        public Definitions.HistoryEvent Type { get; protected set; }
        public string NameOfThing { get; protected set; } = "";
        public int _id;

        public int ID
        {
            get => _id;
            set => _id = value;
        }

        private string Text { get; set; }
        private KeyboardHandler parentKeyboard;
        private string lastPartOfName;
        

        protected override void Start()
        {
            base.Start();
            parentKeyboard = gameObject.GetComponentInParent<KeyboardHandler>();
        }


        public void SetText(string entryName, Definitions.HistoryEvent type)
        {
            if (!textMesh)
            {
                textMesh = GetComponentInChildren<TextMeshPro>(true);
            }

            string[] words = entryName.Split(Path.DirectorySeparatorChar);
            Type = type;
            switch (type)
            {
                case Definitions.HistoryEvent.SELECTION:
                    Text = type + ": " + words[words.Length - 1];
                    NameOfThing = entryName;
                    break;
                case Definitions.HistoryEvent.NETWORK:
                case Definitions.HistoryEvent.FACSGRAPH:
                case Definitions.HistoryEvent.HEATMAP:
                    lastPartOfName = words[words.Length - 1];
                    Text = type + ": from " + words[words.Length - 1];
                    NameOfThing = entryName;
                    break;
                case Definitions.HistoryEvent.GENE:
                case Definitions.HistoryEvent.ATTRIBUTE:
                case Definitions.HistoryEvent.ATTRIBUTEGRAPH:
                case Definitions.HistoryEvent.FACS:
                    Text = type + ": " + entryName;
                    NameOfThing = entryName;
                    break;
                case Definitions.HistoryEvent.INVALID:
                    Text = "";
                    NameOfThing = "";
                    ID = -1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            textMesh.text = Text;
        }

        public override void Click()
        {
            string[] words;
            string selectionName;
            switch (Type)
            {
                case Definitions.HistoryEvent.HEATMAP:
                    words = NameOfThing.Split(new string[] {" from "}, StringSplitOptions.None);
                    string heatmapName = words[0].FixFilePath();
                    selectionName = words[1].FixFilePath();
                    referenceManager.heatmapGenerator.LoadHeatmap(heatmapName, selectionName);
                    break;
                case Definitions.HistoryEvent.NETWORK:
                    words = NameOfThing.Split(new string[] {" from "}, StringSplitOptions.None);
                    string networkName = words[0].FixFilePath();
                    selectionName = words[1].FixFilePath();
                    print(ID);
                    referenceManager.inputReader.ReadNetworkFiles(ID, networkName, selectionName);
                    break;
                case Definitions.HistoryEvent.SELECTION:
                    referenceManager.inputReader.ReadSelectionFile(NameOfThing);
                    break;
                case Definitions.HistoryEvent.FACSGRAPH:
                    string[] markers = lastPartOfName.Replace(".txt", "").Split('_');
                    referenceManager.newGraphFromMarkers.markers = markers.ToList();
                    referenceManager.inputReader.ReadGraphFromMarkerFile(CellexalUser.UserSpecificFolder, NameOfThing);
                    break;
                case Definitions.HistoryEvent.ATTRIBUTEGRAPH:
                    words = NameOfThing.Replace(" ", "").Split('-');
                    referenceManager.graphGenerator.CreateSubGraphs(words.ToList());
                    break;
                case Definitions.HistoryEvent.GENE:
                case Definitions.HistoryEvent.ATTRIBUTE:
                case Definitions.HistoryEvent.FACS:
                    parentKeyboard.SetAllOutputs(NameOfThing);
                    parentKeyboard.SubmitOutput(true);
                    referenceManager.geneKeyboard.Clear();
                    referenceManager.autoCompleteList.ClearList();
                    break;
                case Definitions.HistoryEvent.INVALID:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}