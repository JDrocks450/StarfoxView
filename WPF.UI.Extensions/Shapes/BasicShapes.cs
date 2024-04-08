namespace WPF.UI.Extensions.Shapes
{
    public class NormalSphere : SphereGeometry3D
    {
        public NormalSphere()
        {
            Radius = 1;
            Separators = 5;
        }
    }

    public class NormalDisc : DiscGeometry3D
    {
        public NormalDisc()
        {
            Radius = 1;
            Separators = 10;
        }
    }
}
