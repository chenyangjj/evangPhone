using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangWidget;
using EvangSol.Mobibrary.Utilities.Common;
using System.Reflection;

namespace EvangSol.Mobibrary.EvangViewModel
{
    public class SwipableTemplateVM<V, S> : EvangTemplateVM<V> where V : SwipableView<S>, new() where S : SwipeEntryView, new()
    {
        public EvangSwipeView<S>? SwipeView { get; set; }
        public SwipeNavigator? Navigator { get; set; }
        public bool CanNewOnNext { get; set; }

        protected LayoutPattern sheetlayout;
        protected int eleminwidth;
        protected int colcount;
        protected int rightwidth;
        protected int bottomheight;
        protected bool isarrowclick;
        bool Positioning = false;
        public List<(string, PropertyInfo, PropertyInfo?, ControlAttribute)>? entryproplist;  //sheet property list

        public override Grid? LayoutGrid { get; set; }

        public SwipableTemplateVM(
            string caption,             //page caption
            LayoutPattern sheetlayout,  //carousel sheet layout pattern
            int eleminwidth = 0,        //minimal width of view elements in MultiColumn pattern
            int colcount = 2,           //column count
            int rightwidth = 0,         //when colcount=2, the right pane's width, 0 means equal width
            int bottomheight = 0,       //bottom area height, for pages with bottom buttons
            bool cannewonnext = true    //if can new a sheet when tap on the nextarrow button
            ) : base(caption)
        {
            this.sheetlayout = sheetlayout;
            this.eleminwidth = eleminwidth;
            this.colcount = colcount;
            this.rightwidth = rightwidth;
            this.bottomheight = bottomheight;
            CanNewOnNext = cannewonnext;

            vw = (V)ClassMapping.CreateViewModelInstance(typeof(V));
            entryproplist = ClassMapping.GetPropertyList<ControlAttribute>(typeof(S));
            vw.SwipeEntries = new SwipeEntries<S>();
        }

        //in windows, Race Condition: The Loaded event might be firing before all components are properly initialized.
        //so put rendering work in OnAppearing
        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                BeforeTemplateRendering();
                RenderTemplate();
                AfterTemplateRendering();
            }
            catch (Exception ex)
            {
                ShowException(ex.Message + Environment.NewLine + ex.InnerException?.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        protected virtual void RenderTemplate()
        {
            LayoutGrid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } },
                RowDefinitions =
                {
                    new RowDefinition { Height = CommonViewSetting.COMPOSITE_HEIGHT },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = bottomheight },
                }
            };

            Navigator = new SwipeNavigator(new CommonViewSetting { Width = 200 });
            LayoutGrid.Add(Navigator);

            if (bottomheight > 0)
            {
                LayoutGrid.Add(RenderBottomBar(), 0, 2);
            }

            SwipeView = new EvangSwipeView<S>(RenderLayout(sheetlayout))
            {
                ItemsSource = vw.EntryList!,
            };
            SwipeView.SetBinding(EvangSwipeView<S>.ItemsSourceProperty, "EntryList");
            SwipeView.SetBinding(EvangSwipeView<S>.CurrentItemProperty, "CurrEntry");
            SwipeView.PositionChanged += OnPositionChanged;
            SwipeView.AfterRendering += AfterEntryRendering;
            LayoutGrid.Add(SwipeView, 0, 1);

            vw.OnEntryList = count =>
            {
                Navigator.SwipeCount = count;
                Navigator.CurrentSwipe = count == 0 ? 0 : 1;
                Navigator.PrevArrow!.IsEnabled = false;
                Navigator.NextArrow!.IsEnabled = count > 1;
            };

            Navigator.OnPrevious += (sender, e) =>
            {
                isarrowclick = true;
                SwipeView!.Position--;
            };
            Navigator.OnNext += (sender, e) =>
            {
                var pos = SwipeView!.Position;
                //to new an empty sheet
                if (CanNewOnNext && pos == vw.EntryCount - 1 && !CurrSE!.NotSaved)
                {
                    AddNewEntry();
                }
                else
                {
                    isarrowclick = true;
                    SwipeView.Position++;
                }
            };

            Content = LayoutGrid;
        }

        public S NewEmptyEntry => EvangView.EvangView.CreateEmptyValueViewModel<S, ControlAttribute>(entryproplist!);

        public virtual Layout RenderBottomBar()
        {
            throw new NotImplementedException();
        }

        protected void OnPositionChanged(object? sender, SinglePositionChangedEventArgs e)
        {
            if (isarrowclick)
            {
                if (Positioning)
                    return;
                Positioning = true;
                SetTimer(EvangSwipeView<S>.DebounceTime, () =>
                {
                    Positioning = false;
                    isarrowclick = false;
                });
            }

            var pos = SwipeView!.Position;
            Navigator!.CurrentSwipe = pos + 1;
            Navigator.SwipeCount = vw.EntryCount;
            Navigator.PrevArrow!.IsEnabled = pos > 0;
            //when CanNewOnNext is true, if it's the last sheet and it's a saved one, enable the nextarrow
            Navigator.NextArrow!.IsEnabled = (pos < vw.EntryCount - 1) || (CanNewOnNext && pos == vw.EntryCount - 1 && !CurrSE!.NotSaved);

            OnPositionChanged(pos);
        }

        protected Func<object?> RenderLayout(LayoutPattern layout) =>
           layout switch
           {
               LayoutPattern.FlexGrid => RenderFlexGridLayout,
               LayoutPattern.MultiColumn => RenderMultiColumnLayout,
               LayoutPattern.SingleColumn => RenderSingleColumnLayout,
               _ => throw new ArgumentException("Invalid value", nameof(LayoutPattern)),
           };

        object? RenderFlexGridLayout()
        {
            if (vw.EntryList == null)
                return null;
            var se = vw.EntryList[0];
            double _ = 0;
            Grid layout;
            (layout, entryproplist) = RenderGrid<S>(eleminwidth, colcount, rightwidth, ref _, se);
            layout.BindingContext = se;
            return layout;
        }

        object? RenderMultiColumnLayout()
        {
            if (vw.EntryList == null)
                return null;
            var se = vw.EntryList[0];
            double _ = 0;
            Grid layout;
            (layout, entryproplist) = RenderMultiColumn<S>(colcount, rightwidth, se, ref _);
            layout.BindingContext = se;
            return layout;
        }

        object? RenderSingleColumnLayout()
        {
            if (vw.EntryList == null)
                return null;
            var se = vw.EntryList[0];
            double _ = 0;
            View layout;
            (layout, entryproplist) = RenderSingleColumn<S>(se, ref _);
            layout.BindingContext = se;
            return layout;
        }

        public virtual void OnPositionChanged(int position) { }

        public bool DeleteSheet()
        {
            if (vw.EntryList == null || vw.EntryList.Count < 1)
                return true;
            //get current sheet position
            var currpos = SwipeView!.Position;
            //delete this sheet
            vw.EntryList!.RemoveAt(currpos);
            if (vw.EntryList.Count < 1)
            {
                SwipeView.IsVisible = false;
                Navigator!.CurrentSwipe = 0;
                Navigator.SwipeCount = 0;
                SwipeView.Position = -1;
            }
            else
            {
                //update navigator's showing
                if (currpos == vw.EntryCount)
                {
                    Navigator!.CurrentSwipe = currpos;
                    Navigator.SwipeCount = vw.EntryCount;
                    Navigator.PrevArrow!.IsEnabled = vw.EntryCount > 1;
                    Navigator.NextArrow!.IsEnabled = false;
                    SwipeView.Position = Math.Max(currpos - 1, 0);
                }
                else
                {
                    Navigator!.CurrentSwipe = currpos + 1;
                    Navigator.SwipeCount = vw.EntryCount;
                    Navigator.PrevArrow!.IsEnabled = currpos > 0;
                    Navigator.NextArrow!.IsEnabled = (vw.EntryCount > currpos + 1) || (vw.EntryCount == currpos + 1 && !vw.EntryList[currpos].NotSaved);
                    SwipeView.DeleteCurrentSheet();
                }
            }
            //if empty, return true
            return vw.EntryCount == 0;
        }

        public void AddNewEntry(S se)
        {
            if (vw.EntryList != null && Navigator != null && SwipeView != null)
            {
                SwipeView.IsVisible = true;
                vw.EntryList.Add(se);
                Navigator.SwipeCount = Navigator.CurrentSwipe = vw.EntryList.Count;
                var pos = SwipeView.Position = vw.EntryList.Count - 1;
                Navigator.PrevArrow!.IsEnabled = pos > 0;
                Navigator.NextArrow!.IsEnabled = (pos < vw.EntryCount - 1) || (CanNewOnNext && pos == vw.EntryCount - 1 && !se.NotSaved);

                if (SwipeView!.CurrEntryView == null)
                    SwipeView.RenderEntry(se);
                else
                    (SwipeView!.CurrEntryView as View)!.IsVisible = true;
                Navigator.IsVisible = true;
            }
        }

        public void AddNewEntry()
        {
            var nes = NewEmptyEntry;
            nes.NotSaved = true;
            OnNewEntry(nes);

            AddNewEntry(nes);
        }

        public void CurrSheetUpdated()
        {
            if (CurrSE != null)
            {
                CurrSE.NotSaved = false;
                Navigator!.NextArrow!.IsEnabled = true;
            }
        }

        public S? CurrSE => SwipeView?.CurrentItem as S;

        public virtual void OnNewEntry(S se)
        {
        }

        public virtual void AfterEntryRendering(object? sender, AfterRenderingEventArgs<S> args)
        {
        }
    }
}
