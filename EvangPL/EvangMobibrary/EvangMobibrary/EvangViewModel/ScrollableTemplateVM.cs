using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.DataFeed;

namespace EvangSol.Mobibrary.EvangViewModel
{
    public class ScrollableTemplateVM<B> : EvangTemplateVM<B> where B : BindableBrokerView, new()
    {
        public override Grid? LayoutGrid { get; set; }

        int topheight;
        int bottomheight;

        public ScrollableTemplateVM(
            string caption,         //page caption
            int topheight = 0,      //top pane height
            int bottomheight = 0    //bottom button pane height
            ) : base(caption)
        {
            this.topheight = topheight;
            this.bottomheight = bottomheight;
            Loaded += OnLoaded;
        }

        //move initialization to Loaded event handler to allow child class to do some variable transfering
        private void OnLoaded(object? sender, EventArgs e)
        {
            if (vw != null)
                return;

            try
            {
                CreateViewModel();

                RowDefinitionCollection rows = new();
                if (topheight > 0)
                    rows.Add(new RowDefinition { Height = topheight });
                rows.Add(new RowDefinition());
                if (bottomheight > 0)
                    rows.Add(new RowDefinition { Height = bottomheight });

                LayoutGrid = new Grid
                {
                    RowDefinitions = rows,
                    ColumnDefinitions = { new ColumnDefinition() }
                };

                StackLayout? topstack = null;
                if (topheight > 0)
                {
                    topstack = new StackLayout();
                    LayoutGrid.Add(topstack);
                }

                BeforeTemplateRendering();

                double _ = 0;
                ScrollView scroll;
                (scroll, vmproplist) = RenderSingleColumn<B>(vw!, ref _, topstack);
                LayoutGrid.Add(scroll, row: topheight > 0 ? 1 : 0);
                AdditionalRendering();

                if (bottomheight > 0)
                {
                    LayoutGrid.Add(RenderBottomBar(), row: topheight > 0 ? 2 : 1);
                }

                AfterTemplateRendering();

                Content = LayoutGrid;
            }
            catch (Exception ex)
            {
                ShowException(ex.Message + Environment.NewLine + ex.InnerException?.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public virtual void CreateViewModel()
        {
            vw = (B)ClassMapping.CreateViewModelInstance(typeof(B));
        }

        public virtual Layout RenderBottomBar()
        {
            throw new NotImplementedException();
        }

        protected virtual void AdditionalRendering()
        {
        }
    }
}
