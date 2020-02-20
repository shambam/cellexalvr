using CellexalVR.Menu.Buttons;

namespace CellexalVR.Filters
{

    /// <summary>
    /// Class represents the button on the side of the attribute legend to add or remove an attribute to the culling filter.
    /// </summary>
    public class AttributeFilterButton : CellexalButton
    {
        public string group;
        public bool toggle;

        private CullingFilterManager cullingFilterManager;

        protected override string Description
        {
            get { return "Filter Attr: " + group; }
        }
        public override void Click()
        {
            if (toggle)
            {
                if (referenceManager.legendManager.desiredLegend == AnalysisObjects.LegendManager.Legend.AttributeLegend)
                {
                    cullingFilterManager.RemoveAttributeFromFilter(group);
                }
                else
                {
                    cullingFilterManager.RemoveGroupFromFilter(int.Parse(group));
                }

            }
            else
            {
                if (referenceManager.legendManager.desiredLegend == AnalysisObjects.LegendManager.Legend.AttributeLegend)
                {
                    cullingFilterManager.AddAttributeToFilter(group);
                }
                else
                {
                    cullingFilterManager.AddSelectionGroupToFilter(int.Parse(group));
                }

            }
            toggle = !toggle;
            ToggleOutline(toggle, true);
            //meshRenderer.material.color = toggle ? meshDeactivatedColor : meshStandardColor;
        }

        // Use this for initialization
        protected void Start()
        {
            cullingFilterManager = referenceManager.cullingFilterManager;

        }

    }
}
