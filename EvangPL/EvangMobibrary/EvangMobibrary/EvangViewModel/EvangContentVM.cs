using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.Messaging;
using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.Dialog;
using EvangSol.Mobibrary.TitleBar;
using EvangSol.Mobibrary.Utilities.Common;
using EvangSol.Mobibrary.Utilities.Message;
using System.Collections.ObjectModel;
using static EvangSol.Mobibrary.EvangModel.EvangJsonModel;
using static Microsoft.Maui.Controls.VisualStateManager;
using EvangSol.Mobibrary.EvangModel;

namespace EvangSol.Mobibrary.EvangViewModel
{
    public abstract class EvangContentVM : ContentPage, IMappingBase, IBaseUtils, IRecipient<ScanDataMessage>, IRecipient<NetsuiteTimeoutMessage>
    {
        #region variables
        public IBaseTitleView? TitleView { get; set; }
        public bool IsFromMenu { get; set; }
        public string WinId { get; set; }
#if ANDROID
        public BoxView? TapView { get; set; }
        bool tapviewinserted = false;
#endif
        #endregion

        #region construtor
        public EvangContentVM(string caption, object? viewmodel = null)
        {
            BeforeBaseRendering(caption, viewmodel);

            BindingContext = viewmodel;
            Padding = 5;

            WinId = string.Empty;

#if ANDROID
            //create a transparent overlay view that will catch taps
            TapView = new BoxView
            {
                Color = Colors.Transparent,
                BackgroundColor = Colors.Transparent,
                WidthRequest = 9999,
                HeightRequest = 9999,
                InputTransparent = false    //Important - allows taps to pass through
            };
            var tapgesture = new TapGestureRecognizer();
            tapgesture.Tapped += OnPageTapped;
            TapView.GestureRecognizers.Add(tapgesture);
#endif
        }
        #endregion

        #region override
        protected override void OnAppearing()
        {
            base.OnAppearing();

            //未操作時間のカウンターをリセット
            //Threads.NoActionThread.NoActionCounter = 0;

#if ANDROID
            InsertTapView();
#endif

            //register message recipient
            WeakReferenceMessenger.Default.Register<ScanDataMessage>(this);
            WeakReferenceMessenger.Default.Register<NetsuiteTimeoutMessage>(this);
        }

        protected override void OnDisappearing()
        {
            WeakReferenceMessenger.Default.Unregister<ScanDataMessage>(this);
            WeakReferenceMessenger.Default.Unregister<NetsuiteTimeoutMessage>(this);
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (TitleView != null)
                NavigationPage.SetTitleView(this, TitleView.GetTitleView(width));
        }
        #endregion

        #region page transfer
        //use this function to move to the next page with page type
        bool istransfering = false;
        public async Task<T?> MoveToPage<T>() where T : EvangContentVM, new()
        {
            if (istransfering)
                return null;
            istransfering = true;

            try
            {
                var page = ClassMapping.CreatePageInstance(typeof(T));
                (page as EvangContentVM)!.WinId = typeof(T).Name;
                await Navigation.PushAsync(page);
                return (page as T)!;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null)
                    msg += Environment.NewLine + ex.InnerException.Message;
                ShowError(msg);
            }
            finally
            {
                istransfering = false;
            }
            return null;
        }
        //move to the next page with given page
        public async Task MoveToPage<T>(T page) where T : EvangContentVM
        {
            if (istransfering)
                return;
            istransfering = true;

            (page as EvangContentVM)!.WinId = typeof(T).Name;
            await Navigation.PushAsync(page);
            istransfering = false;
        }

        protected override bool OnBackButtonPressed()
        {
            if (!CanGoBack())
                return true;
            return base.OnBackButtonPressed();
        }

        //override this function and return false can stop moving back to the previous page
        public virtual bool CanGoBack()
        {
            return true;
        }

        public async virtual void NaviPopAsync()
        {
            if (istransfering)
                return;
            istransfering = true;

            await Navigation.PopAsync();
            istransfering = false;
        }
        #endregion

        #region  error functions
        public void ShowError(string errmsg, string? caption = null)
        {
            try
            {
                this.ShowPopup(new MessageDialog(caption ?? BaseUtils.GetCustomString("strError") ?? "Error", errmsg, EvangDialog.DialogType.Error));
            }
            catch { }
        }

        public void ShowException(string errmsg)
        {
            this.ShowPopup(new ExceptionDialog(BaseUtils.GetCustomString("strError") ?? "Error", errmsg));
        }

        public void ErrorProof(Action func)
        {
            try
            {
                func?.Invoke();
            }
            catch (Exception ex)
            {
                ShowException(ex.Message + Environment.NewLine + ex.InnerException?.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        #endregion

        #region handy functions
        // a convenient way to get master data
        public object? this[string key] => LocalMemory.GetMaster(key);

        //this function is mainly for setting the top bar
        public virtual void BeforeBaseRendering(string caption, object? viewmodel) { }

        //create title view
        public T? CreateTitleView<T>(string caption) where T : EvangTitleBar =>
            ClassMapping.CreateTitleViewInstance(typeof(T), Navigation, GetCustomString(caption) ?? GetType().Name) as T;

        //convert json list to designated entry list, for populating grid entry
        public ObservableCollection<E> JsonToEntry<J, E>(List<J> list, FuncSetBGColor? funcsetbgcolor = null) where J : EvangJsonModel where E : TableEntryView, new()
            => EvangJsonModel.CreateEntryModelList<E>(list.Cast<EvangJsonModel>().ToList(), funcsetbgcolor);

        //convert json list to designated sheet list, for populating carousel sheet
        public ObservableCollection<S> JsonToSheet<J, S>(List<J> list, bool showonly = false) where J : EvangJsonModel where S : SwipeEntryView, new()
            => EvangJsonModel.CreateSheetModelList<S>(list.Cast<EvangJsonModel>().ToList(), showonly);

        //concise way to get custom string
        public string S(string str) => GetCustomString(str) ?? str;

        //concise way to create json model instance
        public T NewJM<T>() where T : EvangJsonModel => (ClassMapping.CreateJsonModelInstance(typeof(T)) as T)!;
        #endregion

        #region BaseUtils
        public TextAlignment? GetAlignment(string? align) => BaseUtils.GetAlignment(align);

        public Color? GetColor(string? clr) => BaseUtils.GetColor(clr);

        public FontAttributes? GetFontAttr(string? font) => BaseUtils.GetFontAttr(font);

        public T? GetPropertyValue<T>(object obj, string propname) => BaseUtils.GetPropertyValue<T>(obj, propname);

        public bool SetPropertyValue<T>(object obj, string propname, T? val) => BaseUtils.SetPropertyValue(obj, propname, val);

        public string? GetCustomString(string? key) => BaseUtils.GetCustomString(key);

        public void SetTimer(int interval, Action timeout) => BaseUtils.SetTimer(interval, timeout);

        public void SetInterval(int interval, Func<bool> func) => BaseUtils.SetInterval(interval, func);
        #endregion

        #region IRecipient
        public virtual void Receive(ScanDataMessage message)
        {
        }

        public void Receive(NetsuiteTimeoutMessage message)
        {
            OnHttpTimeout(message);
        }

        //use this function with a better name
        public virtual void OnHttpTimeout(NetsuiteTimeoutMessage message)
        {
            //default error message
            this.ShowPopup(new MessageDialog(S("strError") ?? "Error", S("errNetsuiteTimeout"), EvangDialog.DialogType.Error));
        }
        #endregion

        #region InsertTapView
#if ANDROID
        public virtual void InsertTapView()
        {
            if (!tapviewinserted)
            {
                tapviewinserted = true;
                //if content is a layout, add the tapView as the first child
                if (Content is Layout mainLayout)
                    mainLayout.Children.Insert(0, TapView);
            }
        }

        public void OnPageTapped(object? sender, TappedEventArgs e)
        {
            var focused = FindFocusedView();
            if (focused != null)
            {
                if (focused is ISoftInput isi)
                    isi.SoftInput(false);
            }
        }

        public IView? FindFocusedView(IView? view = null)
        {
            view ??= Content;
            if (view.IsFocused || (view is EvangCompositeView bcv && bcv.InputControl != null && bcv.InputControl.IsFocused))
                return view;

            if (view is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    var focused = FindFocusedView(child);
                    if (focused != null)
                        return focused;
                }
            }
            return null;
        }
#endif
        #endregion
    }

    #region EvangContentVM<VM>
    public abstract class EvangContentVM<B> : EvangContentVM where B : BindableBrokerView
    {
        protected EvangContentVM(string caption, B? viewmodel) : base(caption, viewmodel)
        {
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("BaseCustom", (handler, view) =>
            {
                if (view is Entry)
                {
#if ANDROID
                    handler.PlatformView.SetPadding(10, 15, 10, 5);
                    //handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS || MACCATALYST

#endif
                }
            });

            Style style = new(typeof(Entry))
            {
                Setters =
                {
                    new Setter
                    {
                        Property = VisualStateGroupsProperty,
                        Value = new VisualStateGroupList
                        {
                            new VisualStateGroup
                            {
                                Name = nameof(CommonStates),
                                States =
                                {
                                    new VisualState
                                    {
                                        Name = CommonStates.Disabled,
                                        Setters =
                                        {
                                            new Setter
                                            {
                                                Property = BackgroundColorProperty,
                                                Value = Colors.LightGray
                                            }
                                        }
                                    },
                                    new VisualState
                                    {
                                        Name = CommonStates.Normal
                                    }
                                }
                            }
                        }
                    }
                }
            };
            Resources.Add(style);

            HideSoftInputOnTapped = true;
        }

        public B vw
        {
            get => (B)BindingContext;
            set => BindingContext = value;
        }
    }
    #endregion

    #region ISoftInput
    public interface ISoftInput
    {
        public void SoftInput(bool show);
    }
    #endregion
}
