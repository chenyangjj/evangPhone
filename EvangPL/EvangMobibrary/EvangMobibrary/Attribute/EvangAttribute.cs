namespace EvangSol.Mobibrary.Attributes
{
    public abstract class EvangAttribute : Attribute
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}
