using CellexalVR.AnalysisLogic;
using CellexalVR.General;
using CellexalVR.Menu.SubMenus;

namespace CellexalVR.Menu.Buttons.Facs
{
    /// <summary>
    /// Represents a button that colors all graphs according to an index.
    /// </summary>
    public class ColorByIndexButton : CellexalButton
    {
        public TMPro.TextMeshPro descriptionOnButton;
        public ColorByIndexMenu parentMenu;

        private CellManager cellManager;
        private string indexName;
        

        protected override string Description
        {
            get { return "Color by: " + this.indexName; }
        }

        protected void Start()
        {
            cellManager = referenceManager.cellManager;
            CellexalEvents.GraphsColoredByIndex.AddListener(TurnOn);
            CellexalEvents.GraphsResetKeepSelection.AddListener(TurnOn);
        }

        public override void Click()
        {
            cellManager.ColorByIndex(indexName);
            TurnOff();
            referenceManager.multiuserMessageSender.SendMessageColorByIndex(indexName);
        }

        /// <summary>
        /// Sets which index this button should show when pressed.
        /// </summary>
        /// <param name="indexName"> The name of the index. </param>
        public void SetIndex(string indexName)
        {
            //color = network.GetComponent<Renderer>().material.color;
            //GetComponent<Renderer>().material.color = color;
            //meshStandardColor = meshStandardColor;
            this.indexName = indexName;
            descriptionOnButton.text = indexName;
        }

        public void TurnOn()
        {
            SetButtonActivated(true);
        }

        public void TurnOff()
        {
            SetButtonActivated(false);
        }
    }
}