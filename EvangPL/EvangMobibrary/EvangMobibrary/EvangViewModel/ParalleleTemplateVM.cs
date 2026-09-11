using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.DataFeed;

namespace EvangSol.Mobibrary.EvangViewModel
{
    public class ParalleleTemplateVM<B> : EvangTemplateVM<B> where B : BindableBrokerView, new()
    {
        public override Grid? LayoutGrid { get; set; }

        public List<StackLayout> Panes { get; set; }

        public ParalleleTemplateVM(
            string caption,         //page caption
            int colcount = 2,       //column count
            int rightwidth = 0,     //when colcount=2, the right pane's width, 0 means equal width
            int bottomheight = 0    //bottom area height, for pages with bottom buttons
            ) : base(caption)
        {
            Panes = new List<StackLayout>();

            try
            {
                vw = (B)ClassMapping.CreateViewModelInstance(typeof(B));

                double _ = 0;
                Grid grid;
                (grid, vmproplist)= RenderMultiColumn<B>(colcount, rightwidth, vw, ref _, Panes);

                if (bottomheight > 0)
                {
                    LayoutGrid = new Grid
                    {
                        ColumnDefinitions = { new ColumnDefinition() },
                        RowDefinitions =
                        {
                            new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                            new RowDefinition { Height = new GridLength(bottomheight, GridUnitType.Absolute) },
                        },
                    };
                    LayoutGrid.Add(grid);
                    LayoutGrid.Add(RenderBottomBar(), row: 1);
                }
                else
                {
                    LayoutGrid = grid;
                }

                Content = LayoutGrid;
            }
            catch (Exception ex)
            {
                ShowException(ex.Message + Environment.NewLine + ex.InnerException?.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public override void BeforeRendering(Layout layout, BindableBrokerView? em)
        {
            BeforeTemplateRendering();
        }

        public override void AfterRendering(Layout layout, BindableBrokerView? em)
        {
            AfterTemplateRendering();
        }

        public virtual Layout RenderBottomBar()
        {
            throw new NotImplementedException();
        }
    }
}
