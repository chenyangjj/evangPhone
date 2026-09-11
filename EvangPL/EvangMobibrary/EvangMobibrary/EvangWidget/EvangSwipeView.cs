using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.Utilities.Common;
using System.Collections.ObjectModel;

namespace EvangSol.Mobibrary.EvangWidget
{
    public class EvangSwipeView<S> : ContentView where S : SwipeEntryView, new()
    {
        #region bindable properties
        #region ItemsSource
        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(ObservableCollection<S>), typeof(EvangSwipeView<S>),
                new ObservableCollection<S>(), propertyChanged: OnItemsSourceChanged);
        public ObservableCollection<S> ItemsSource
        {
            get => (ObservableCollection<S>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var esv = bindable as EvangSwipeView<S>;
            if (esv != null)
            {
                if (esv.ItemsSource.Count > 0)
                {
                    S se = esv.ItemsSource[0];
                    esv.RenderEntry(se);
                    esv.Position = -1;
                    esv.Position = 0;
                }
            }
        }
        #endregion

        #region Position
        public static readonly BindableProperty PositionProperty =
            BindableProperty.Create(nameof(Position), typeof(int), typeof(EvangSwipeView<S>), 0,
                BindingMode.TwoWay, propertyChanged: OnCurrentIndexChanged);
        public int Position
        {
            get => (int)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        private static void OnCurrentIndexChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue == oldValue)
                return;
            var esv = bindable as EvangSwipeView<S>;
            if (esv != null)
            {
                if ((int)newValue < 0)
                {
                    esv.prevpos = (int)newValue;
                    return;
                }
                if (esv.ItemsSource.Count > 0)
                    esv.UpdatePosition();
            }
        }
        #endregion

        #region CurrentItem
        public static readonly BindableProperty CurrentItemProperty =
            BindableProperty.Create(nameof(CurrentItem), typeof(S), typeof(EvangSwipeView<S>), new S(),
                BindingMode.TwoWay, propertyChanged: OnCurrentItemChanged);
        public object CurrentItem
        {
            get => (S)GetValue(CurrentItemProperty);
            set => SetValue(CurrentItemProperty, value);
        }

        private static void OnCurrentItemChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue == oldValue)
                return;
            var esv = bindable as EvangSwipeView<S>;
            if (esv != null && esv.ItemsSource.Count > 0)
            {
                var se = newValue as S;
                if (se != null)
                {
                    var ind = esv.ItemsSource.IndexOf(se);
                    if (ind >= 0)
                        esv.RenderEntry(se);
                }
            }
        }
        #endregion
        #endregion

        #region properties
        public bool IsEmpty => ItemsSource == null || ItemsSource.Count < 1;

        public int Count => ItemsSource == null ? 0 : ItemsSource.Count;
        #endregion

        public event EventHandler<SinglePositionChangedEventArgs>? PositionChanged;
        public event EventHandler<AfterRenderingEventArgs<S>>? AfterRendering;
        public const int DebounceTime = 300;

        public object? PrevEntryView { get; set; }
        public object? CurrEntryView { get; set; }
        Func<object?>? ItemTemplate { get; set; }

        public EvangSwipeView(Func<object?>? itemtemplate)
        {
            ItemTemplate = itemtemplate;
            Loaded += OnLoaded;

            SwipeGestureRecognizer leftSwipeGesture = new SwipeGestureRecognizer { Direction = SwipeDirection.Left, Threshold = 100 };
            leftSwipeGesture.Swiped += OnSwiped;
            SwipeGestureRecognizer rightSwipeGesture = new SwipeGestureRecognizer { Direction = SwipeDirection.Right, Threshold = 100 };
            rightSwipeGesture.Swiped += OnSwiped;
            GestureRecognizers.Add(leftSwipeGesture);
            GestureRecognizers.Add(rightSwipeGesture);
        }

        ~EvangSwipeView()
        {
            GestureRecognizers.Clear();
        }

        bool loaded = false;
        S? secache;
        private void OnLoaded(object? sender, EventArgs e)
        {
            if (loaded) return;
            loaded = true;

            if (secache != null)
            {
                RenderEntry(secache);
                BaseUtils.SetTimer(DebounceTime, () =>
                {
                    if (CurrEntryView != null && PrevEntryView != null)
                    {
                        if (CurrEntryView is ScrollView scroll)
                        {
                            scroll.WidthRequest = Width;
                            scroll.WidthRequest = Width;
                        }
                        else if (CurrEntryView is Layout layout)
                        {
                            layout.WidthRequest = Width;
                            layout.WidthRequest = Width;
                        }
                    }
                    if (prevpos != Position)
                        UpdatePosition();
                });
            }
        }

        void OnSwiped(object? sender, SwipedEventArgs e)
        {
            if (isTranslating)
                return;

            switch (e.Direction)
            {
                case SwipeDirection.Left:
                    if (Position < Count - 1)
                        Position++;
                    break;
                case SwipeDirection.Right:
                    if (Position > 0)
                        Position--;
                    break;
            }
        }

        public void RenderEntry(S se)
        {
            if (!loaded)
            {
                secache = se;
                return;
            }

            if (CurrEntryView == null)
            {
                CurrEntryView = ItemTemplate?.Invoke();
                PrevEntryView = ItemTemplate?.Invoke();
                Content = new HorizontalStackLayout
                {
                    Spacing = 0,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    Children =
                    {
                        CurrEntryView as View,
                        PrevEntryView as View,
                    }
                };
            }

            if (CurrEntryView != null && PrevEntryView != null)
            {
                if (CurrEntryView is Layout layout)
                {
                    TraverseViewsBinding(se, layout.Children);
                    layout.BindingContext = se;
                    layout.WidthRequest = Width;
                    layout.IsVisible = true;
                    (PrevEntryView as Layout)!.IsVisible = false;
                }
                else if (CurrEntryView is ScrollView scroll)
                {
                    TraverseViewsBinding(se, (scroll.Content as Layout)!.Children);
                    scroll.BindingContext = se;
                    scroll.WidthRequest = Width;
                    scroll.IsVisible = true;
                    (PrevEntryView as ScrollView)!.IsVisible = false;

                    //for scrollview, add SwipeGestureRecognizer to its child. it's the only way to make swipe working
                    SwipeGestureRecognizer leftSwipeGesture = new SwipeGestureRecognizer { Direction = SwipeDirection.Left, Threshold = 100 };
                    leftSwipeGesture.Swiped += OnSwiped;
                    SwipeGestureRecognizer rightSwipeGesture = new SwipeGestureRecognizer { Direction = SwipeDirection.Right, Threshold = 100 };
                    rightSwipeGesture.Swiped += OnSwiped;
                    scroll.Content.GestureRecognizers.Add(leftSwipeGesture);
                    scroll.Content.GestureRecognizers.Add(rightSwipeGesture);
                }

                AfterRendering?.Invoke(this, new AfterRenderingEventArgs<S> { layout = CurrEntryView, se = se });
            }
        }

        void TraverseViewsBinding(S se, IList<IView> children)
        {
            foreach (IView child in children)
            {
                if (child is EvangContentView ecv)
                {
                    if (!string.IsNullOrEmpty(ecv.ClassId))
                        ConnectViewElement(se, ecv.ClassId, ecv);
                }
                else if (child is View view && !string.IsNullOrEmpty(view.ClassId))
                {
                    ConnectViewElement(se, view.ClassId, view);
                }
                else if (child is Layout sublayout)
                {
                    if (string.IsNullOrEmpty(sublayout.ClassId))
                        TraverseViewsBinding(se, sublayout.Children);
                    else
                        ConnectViewElement(se, sublayout.ClassId, sublayout);
                }
            }
        }

        void ConnectViewElement(S se, string propname, View view)
        {
            var propinfo = ClassMapping.GetMappingType(se.GetType()).GetProperty(propname);
            if (propinfo != null)
            {
                var ve = propinfo.GetValue(se, null) as EvangElement;
                if (ve != null)
                {
                    ve.PropName = propname;
                    ve.ViewModel = se;
                    ve.ElementObject = view;
                    view.IsVisible = ve.IsVisible;

                    if (view is IInputControl iic)
                    {
                        iic.ShowOnly = ve.ShowOnly;
                        iic.Value = ve.Value!;
                    }

                    if (view is EvangCompositeView ecv && ecv.ExtraControl != null && ecv.ExtraControlName != null)
                        ConnectViewElement(se, ecv.ExtraControlName, ecv.ExtraControl);
                }
            }
        }

        void TraverseViewsClear(S se, IList<IView> children)
        {
            foreach (IView child in children)
            {
                if (child is EvangContentView ecv)
                {
                    if (!string.IsNullOrEmpty(ecv.ClassId))
                        ClearViewElement(se, ecv.ClassId, ecv);
                }
                else if (child is View view && !string.IsNullOrEmpty(view.ClassId))
                {
                    ClearViewElement(se, view.ClassId, view);
                }
                else if (child is Layout sublayout)
                {
                    if (string.IsNullOrEmpty(sublayout.ClassId))
                        TraverseViewsClear(se, sublayout.Children);
                    else
                        ClearViewElement(se, sublayout.ClassId, sublayout);
                }
            }
        }

        void ClearViewElement(S se, string propname, View view)
        {
            var propinfo = ClassMapping.GetMappingType(se.GetType()).GetProperty(propname);
            if (propinfo != null)
            {
                var ve = propinfo.GetValue(se, null) as EvangElement;
                if (ve != null)
                {
                    if (ve.ElementObject is IInputControl iic)
                        iic.Blur();
                    ve.PropName = null;
                    ve.ViewModel = null;
                    ve.ElementObject = null;
                }
            }
        }

        //Value change events such as TextChanged also occur when switching sheets.
        //You can determine whether it is time to switch sheets by referring to isTranslating.
        public bool isTranslating = false;
        public int prevpos = -1;
        public void UpdatePosition()
        {
            if (IsEmpty || Count == 0)
                return;

            if (CurrEntryView != null && PrevEntryView != null && CurrEntryView is View currswipe && PrevEntryView is View prevswipe)
            {

                Position = Math.Clamp(Position, 0, Count - 1);
                if (Position != prevpos)
                {
                    isTranslating = true;

                    var se = CurrentItem as S;
                    if (se != null)
                    {
                        if (currswipe is Layout layout)
                            TraverseViewsClear(se, layout.Children);
                        else if (currswipe is ScrollView scroll)
                            TraverseViewsClear(se, (scroll.Content as Layout)!.Children);
                    }
                    currswipe.BindingContext = null;
                    prevswipe.BindingContext = null;

                    if (Position > prevpos)
                    {
                        prevswipe.TranslationX = Width;
                        prevswipe.IsVisible = true;
                        _ = currswipe.TranslateTo(-Width, 0, DebounceTime, Easing.CubicOut);
                        _ = prevswipe.TranslateTo(0, 0, DebounceTime, Easing.CubicOut);
                    }
                    else
                    {
                        prevswipe.TranslationX = -Width;
                        prevswipe.IsVisible = true;
                        _ = currswipe.TranslateTo(Width, 0, DebounceTime, Easing.CubicOut);
                        _ = prevswipe.TranslateTo(0, 0, DebounceTime, Easing.CubicOut);
                    }
                    var temp = PrevEntryView;
                    PrevEntryView = CurrEntryView;
                    CurrEntryView = temp;

                    var args = new SinglePositionChangedEventArgs { PrevPosition = prevpos, CurrPosition = Position };
                    prevpos = Position;
                    if (ItemsSource != null)
                    {
                        currswipe = (CurrEntryView as View)!;
                        currswipe.BindingContext = CurrentItem = ItemsSource[Position];
                        if (CurrEntryView is Layout layout)
                            TraverseViewsBinding((CurrentItem as S)!, layout.Children);
                        else if (CurrEntryView is ScrollView scroll)
                            TraverseViewsBinding((CurrentItem as S)!, (scroll.Content as Layout)!.Children);
                    }

                    BaseUtils.SetTimer(DebounceTime + 10, ()=>
                    {
                        isTranslating = false;
                        PositionChanged?.Invoke(this, args);
                    });
                }
            }
        }

        public void DeleteCurrentSheet()
        {
            if (CurrEntryView != null && PrevEntryView != null && CurrEntryView is View currswipe && PrevEntryView is View prevswipe)
            {
                prevswipe.TranslationX = Width;
                prevswipe.IsVisible = true;
                _ = currswipe.TranslateTo(-Width, 0, DebounceTime, Easing.CubicOut);
                _ = prevswipe.TranslateTo(0, 0, DebounceTime, Easing.CubicOut);

                var temp = PrevEntryView;
                PrevEntryView = CurrEntryView;
                CurrEntryView = temp;
                if (ItemsSource != null)
                    CurrentItem = ItemsSource[Position];
            }
        }

        public void ClearEntryView()
        {
            if (CurrEntryView == null || PrevEntryView == null)
                return;

            var se = CurrentItem as S;
            if (se != null)
            {
                if (CurrEntryView is Layout layout)
                    TraverseViewsClear(se, layout.Children);
                else if (CurrEntryView is ScrollView scroll)
                    TraverseViewsClear(se, (scroll.Content as Layout)!.Children);
            }

            (CurrEntryView as View)!.BindingContext = null;
            (PrevEntryView as View)!.BindingContext = null;
            (CurrEntryView as View)!.IsVisible = false;

        }
    }

    public class SinglePositionChangedEventArgs : EventArgs
    {
        public int PrevPosition { get; set; }
        public int CurrPosition { get; set; }
    }

    public class AfterRenderingEventArgs<S> : EventArgs where S : SwipeEntryView
    {
        public S? se { get; set; }
        public object? layout { get; set; }
    }
}
