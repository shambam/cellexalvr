using Assets.Scripts.SceneObjects;
namespace CellexalVR.Menu.Buttons.Tools
{
    /// <summary>
    /// Represents the button on the culling cube that inverts the culling. Either all points inside it are not rendered
    /// or only the points inside it are.
    /// </summary>
    public class InvertCullingButton : CellexalButton
    {
        public CullingCube cullingCube;

        private bool inverted;

        protected override string Description
        {
            get { return "Invert culling filter"; }
        }

        public override void Click()
        {
            cullingCube.InverseCulling(!inverted);
            inverted = !inverted;
        }
    }
}
