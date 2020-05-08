using UnityEngine;
using System.Collections;
using CellexalVR.Interaction;
using CellexalVR.AnalysisObjects;
using System;
using CellexalVR.General;

namespace CellexalVR.Interaction
{
    public class GeneKeyboardHandler : KeyboardHandler
    {
        public PreviousSearchesList previousSearchesList;
        public CorrelatedGenesList correlatedGenesList;

        public override string[][] Layouts { get; protected set; } =
        {
            // lowercase
            new string[]
            {
                "q", "w", "e", "r", "t", "y", "u", "i", "o", "p",
                "Shift", "a", "s", "d", "f", "g", "h", "j", "k", "l",
                "123\n!#%", "z", "x", "c", "v", "b", "n", "m", "Back", "Clear",
                "Enter"
            },
            // uppercase
            new string[]
            {
                "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P",
                "Shift", "A", "S", "D", "F", "G", "H", "J", "K", "L",
                "123\n!#%", "Z", "X", "C", "V", "B", "N", "M", "Back", "Clear",
                "Enter"
            },
            // special
            new string[]
            {
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
                "Shift", "!", "#", "%", "&", "/", "(", ")", "=", "@",
                "ABC\nabc", "\\", "-", "_", ".", ":", ",", ";", "Back", "Clear",
                "Enter"
            }
        };

        /// <summary>
        /// Switches between uppercase and lowercase layout.
        /// </summary>
        public override void Shift()
        {
            if (CurrentLayout == 0)
            {
                SwitchLayout(Layouts[1]);
                CurrentLayout = 1;
            }
            else if (CurrentLayout == 1)
            {
                SwitchLayout(Layouts[0]);
                CurrentLayout = 0;
            }
        }

        /// <summary>
        /// Switches between lowercase and special layout.
        /// </summary>
        public override void NumChar()
        {
            if (CurrentLayout == 2)
            {
                SwitchLayout(Layouts[0]);
                CurrentLayout = 0;
            }
            else
            {
                SwitchLayout(Layouts[2]);
                CurrentLayout = 2;
            }
        }

#if UNITY_EDITOR
        public void BuildKeyboard()
        {
            OpenPrefab(out GameObject outerMostPrefab, out GeneKeyboardHandler scriptOnPrefab);

            UnityEditor.EditorUtility.DisplayProgressBar("Building keyboard", "Building previous searches list", 0.6f);
            var previousSearchesListOnPrefab = outerMostPrefab.GetComponentInChildren<PreviousSearchesList>();
            previousSearchesListOnPrefab.BuildPreviousSearchesList(10, previousSearchesListOnPrefab);

            UnityEditor.EditorUtility.DisplayProgressBar("Building keyboard", "Building correlated genes list", 0.7f);
            var correlatedGenesListOnPrefab = outerMostPrefab.GetComponentInChildren<CorrelatedGenesList>();
            correlatedGenesListOnPrefab.BuildList(10, correlatedGenesListOnPrefab);

            UnityEditor.EditorUtility.DisplayProgressBar("Building keyboard", "Building session history list", 0.7f);
            var sessionHistoryListOnPrefab = outerMostPrefab.GetComponentInChildren<SessionHistoryList>();
            sessionHistoryListOnPrefab.BuildList(10, sessionHistoryListOnPrefab);


            base.BuildKeyboard(scriptOnPrefab);

            ClosePrefab(outerMostPrefab);
        }

#endif
    }
#if UNITY_EDITOR

    /// <summary>
    /// Editor class for the <see cref="GeneKeyboardHandler"/> to add a "Build keyboard" button.
    /// </summary>
    [UnityEditor.CustomEditor(typeof(GeneKeyboardHandler), true)]
    [UnityEditor.CanEditMultipleObjects]
    public class GeneKeyboardHandlerEditor : UnityEditor.Editor
    {
        private GeneKeyboardHandler instance;

        void OnEnable()
        {
            instance = (GeneKeyboardHandler) target;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Build keyboard"))
            {
                instance.BuildKeyboard();
            }

            try
            {
                DrawDefaultInspector();
            }
            catch (ArgumentException)
            {
                // I think this happens because BuildKeyboard opens a prefab using UnityEditor.PrefabUtility.LoadPrefabContents
                // which opens a second (hidden) inspector which glitches out because it's called from OnInspectorGUI.
            }
        }
    }
#endif
}