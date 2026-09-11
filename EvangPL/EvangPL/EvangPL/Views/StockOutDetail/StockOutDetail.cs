using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.Dialog;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;

namespace EvangPL.Views.StockOutDetail
{
    public class PackageItem
    {
        public string PackageNo { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public int Qty { get; set; }
    }

    public class StockOutDetail : EvangContentVM
    {
        private Grid? _mainGrid;
        private Button? _btnAddPackage;
        private CollectionView? _mainListView;
        private Label? _lblCount;

        // ========== 原生弹窗控件 ==========
        private Frame _popupFrame;
        private Entry _entryPackageNo = new Entry { Placeholder = "梱包No.(スキャン可)" };
        private Entry _entryItemCode = new Entry { Placeholder = "品目(スキャン可)" };
        private Entry _entryQty = new Entry { Keyboard = Keyboard.Numeric, Placeholder = "数量" };
        private Button _btnAddItem;
        private Button _btnFinishPopup;
        private Button _btnClosePopup;
        private CollectionView _popupList;

        public ObservableCollection<PackageItem> AddedPackageList { get; set; }

        public StockOutDetail() : base("strStockOutDetail")
        {
            AddedPackageList = new ObservableCollection<PackageItem>
            {
                new PackageItem { PackageNo = "BOX-0009", ItemCode = "部品B-2020", Qty = 20 },
                new PackageItem { PackageNo = "BOX-0010", ItemCode = "部品C-3030", Qty = 10 }
            };
            this.Appearing += OnPageAppearing;
        }

        private void OnPageAppearing(object? sender, EventArgs e)
        {
            // 关键修复：只首次加载构建UI，避免重复叠加遮罩导致永久灰屏
            if (_mainGrid == null)
            {
                BuildPageUi();
                this.Content = _mainGrid;
            }
        }

        public override void BeforeBaseRendering(string caption, object? viewmodel)
        {
            base.BeforeBaseRendering(caption, viewmodel);
        } 

        private void BuildPageUi()
        {
            // 主页面三层布局：背景内容 + 半透明遮罩 + 弹窗Frame
            _mainGrid = new Grid
            {
                Padding = new Thickness(16),
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                },
                BackgroundColor = Color.FromArgb("#eff0f0")
            };

            #region 主页面原有UI
            _lblCount = new Label
            {
                Text = $"追加済みの梱包({AddedPackageList.Count}件)",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(_lblCount, 0);
            _mainGrid.Children.Add(_lblCount);

            _btnAddPackage = new Button
            {
                Text = "+ 梱包を追加",
                HeightRequest = 60,
                FontSize = 14,
                BackgroundColor = Color.FromArgb("#255499"),
                TextColor = Colors.White,
                Margin = new Thickness(0, 0, 0, 12),
                CornerRadius = 8
            };
            // 打开弹窗：遮罩+弹窗同时显示
            _btnAddPackage.Clicked += (s, e) => _popupFrame.IsVisible = true;
            Grid.SetRow(_btnAddPackage, 1);
            _mainGrid.Children.Add(_btnAddPackage);

            _mainListView = new CollectionView
            {
                ItemsSource = AddedPackageList,
                ItemTemplate = new DataTemplate(() =>
                {
                    var rowGrid = new Grid
                    {
                        Padding = new Thickness(10),
                        ColumnDefinitions =
                        {
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Auto)
                        }
                    };
                    var lbNo = new Label { VerticalOptions = LayoutOptions.Center };
                    lbNo.SetBinding(Label.TextProperty, nameof(PackageItem.PackageNo));
                    var lbItem = new Label { VerticalOptions = LayoutOptions.Center };
                    lbItem.SetBinding(Label.TextProperty, nameof(PackageItem.ItemCode));
                    var lbQty = new Label { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End };
                    lbQty.SetBinding(Label.TextProperty, nameof(PackageItem.Qty));

                    Grid.SetColumn(lbNo, 0);
                    Grid.SetColumn(lbItem, 1);
                    Grid.SetColumn(lbQty, 2);
                    rowGrid.Children.Add(lbNo);
                    rowGrid.Children.Add(lbItem);
                    rowGrid.Children.Add(lbQty);

                    return new Frame
                    {
                        Margin = new Thickness(2, 4),
                        Content = rowGrid,
                        CornerRadius = 6,
                        BackgroundColor = Colors.White,
                        HasShadow = false
                    };
                })
            };
            Grid.SetRow(_mainListView, 2);
            _mainGrid.Children.Add(_mainListView);
            #endregion

            #region 原生弹窗UI（纯MAUI内置控件）
            // 弹窗内部按钮初始化
            _btnAddItem = new Button
            {
                Text = "+ この内容を追加",
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#255499"),
                BorderColor = Color.FromArgb("#255499"),
                BorderWidth = 2,
                CornerRadius = 8,
                Padding = new Thickness(12)
            };
            _btnAddItem.Clicked += OnAddClick;

            _btnFinishPopup = new Button
            {
                Text = "完了",
                BackgroundColor = Color.FromArgb("#255499"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Margin = new Thickness(0, 20, 0, 0)
            };
            // 关闭弹窗：弹窗+遮罩一起隐藏
            _btnFinishPopup.Clicked += (s, e) => _popupFrame.IsVisible = false;

            _btnClosePopup = new Button
            {
                Text = "×",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Gray,
                WidthRequest = 45,
                FontSize = 24
            };
            _btnClosePopup.Clicked += (s, e) => _popupFrame.IsVisible = false;

            // 弹窗标题行
            var titleRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            var titleLabel = new Label
            {
                Text = "梱包を追加",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(titleLabel, 0);
            Grid.SetColumn(_btnClosePopup, 1);
            titleRow.Children.Add(titleLabel);
            titleRow.Children.Add(_btnClosePopup);

            // 表头
            var tableHeader = new Grid
            {
                BackgroundColor = Color.FromArgb("#e6edf7"),
                Padding = new Thickness(8),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            tableHeader.Children.Add(new Label { Text = "梱包No.", FontAttributes = FontAttributes.Bold });
            tableHeader.Children.Add(new Label { Text = "品目", FontAttributes = FontAttributes.Bold });
            var qtyLabel = new Label { Text = "数量", FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End };
            Grid.SetColumn(qtyLabel, 2);
            tableHeader.Children.Add(qtyLabel);

            // 弹窗内列表
            _popupList = new CollectionView
            {
                ItemsSource = AddedPackageList,
                HeightRequest = 200,
                ItemTemplate = new DataTemplate(() =>
                {
                    var row = new Grid
                    {
                        Padding = new Thickness(8),
                        ColumnDefinitions =
                        {
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Auto)
                        }
                    };
                    var lb1 = new Label { VerticalOptions = LayoutOptions.Center };
                    lb1.SetBinding(Label.TextProperty, nameof(PackageItem.PackageNo));
                    var lb2 = new Label { VerticalOptions = LayoutOptions.Center };
                    lb2.SetBinding(Label.TextProperty, nameof(PackageItem.ItemCode));
                    var lb3 = new Label { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End };
                    lb3.SetBinding(Label.TextProperty, nameof(PackageItem.Qty));
                    Grid.SetColumn(lb1, 0);
                    Grid.SetColumn(lb2, 1);
                    Grid.SetColumn(lb3, 2);
                    row.Children.Add(lb1);
                    row.Children.Add(lb2);
                    row.Children.Add(lb3);
                    return row;
                })
            };

            // 弹窗主体布局
            var popupContent = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 15,
                BackgroundColor = Colors.White,
                Children =
                {
                    titleRow,
                    _entryPackageNo,
                    _entryItemCode,
                    _entryQty,
                    _btnAddItem,
                    tableHeader,
                    _popupList,
                    _btnFinishPopup
                }
            };

            // Frame作为弹窗容器，默认隐藏；去掉固定宽高适配手机竖屏
            _popupFrame = new Frame
            {
                IsVisible = false,
                BackgroundColor = Colors.White,
                CornerRadius = 12,
                Padding = 0,
                MaximumWidthRequest = 580,
                Margin = new Thickness(20),
                Content = new ScrollView { Content = popupContent },
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // 半透明遮罩层：绑定弹窗显隐，弹窗开则遮罩开、弹窗关则遮罩关
            var maskLayer = new BoxView
            {
                BackgroundColor = Colors.Black.WithAlpha(0.5f),
                InputTransparent = false
            };
            maskLayer.SetBinding(BoxView.IsVisibleProperty, new Binding(nameof(_popupFrame.IsVisible), source: _popupFrame));

            // 遮罩 + 弹窗叠加到Grid同一层级（覆盖全屏）
            Grid.SetRowSpan(maskLayer, 3);
            Grid.SetRowSpan(_popupFrame, 3);
            _mainGrid.Children.Add(maskLayer);
            _mainGrid.Children.Add(_popupFrame);
            #endregion
        }

        // 原有追加业务逻辑完全保留
        private async void OnAddClick(object? sender, EventArgs e)
        {
            if (!int.TryParse(_entryQty.Text?.Trim(), out int qty) || qty <= 0)
            {
                await DisplayAlert("エラー", "数量は1以上を入力してください。", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(_entryPackageNo.Text) || string.IsNullOrWhiteSpace(_entryItemCode.Text))
            {
                await DisplayAlert("エラー", "梱包No、品目は必須です。", "OK");
                return;
            }
            AddedPackageList.Add(new PackageItem
            {
                PackageNo = _entryPackageNo.Text!.Trim(),
                ItemCode = _entryItemCode.Text!.Trim(),
                Qty = qty
            });
            _entryPackageNo.Text = string.Empty;
            _entryItemCode.Text = string.Empty;
            _entryQty.Text = string.Empty;
            if (_lblCount != null)
            {
                _lblCount.Text = $"追加済みの梱包({AddedPackageList.Count}件)";
            }
        }
    }
}
