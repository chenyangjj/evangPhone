namespace EvangSol.Mobibrary.Attributes
{
    public class ParallelAttribute : EvangAttribute
    {
        //start a new line
        public bool Start { get; set; } = false;
        //column spacing, only the value of the first view element in one row works
        public int ColumnSpacing { get; set; } = 10;
    }
}
