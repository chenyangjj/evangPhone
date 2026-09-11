using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

namespace EvangPL.Views.MaterialsList
{
    public class MaterialsList : EvangContentVM
    {
        public string ProcessId { get; set; }
        private Grid? filterFrame;
        private CollectionView? ordersCollectionView;
        private CollectionView _collectionView;
        private ObservableCollection<Product> _products;
        private ObservableCollection<Product> _filteredOrders;
        private ParamInfo ParamTo;

        public enum FilterStatus
        {
            All,
            Completed,
            Ongoing,
            Pending,
            Abnormal,

        }

        private FilterStatus _currentFilter = FilterStatus.All;
        private Border _allStatusBorder;
        private Border _completedStatusBorder;
        private Border _ongoingStatusBorder;
        private Border _pendingStatusBorder;
        private Border _abnormalStatusBorder;

        public MaterialsList(ParamInfo ParamTo) : base("strMaterialsList")
        {
            Title = "所要量一覧";
            BuildUI(ParamTo);
            
            InitializeFilter();
            LoadData(ParamTo);
        }

        private async void BuildUI(ParamInfo ParamTo)
        {
            var mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                },
                BackgroundColor = Color.FromArgb("#eff1f5"),
                RowSpacing = 10,
                Margin = new Thickness(0),
            };
            filterFrame = HeaderFilter();
            mainGrid.Add(filterFrame, 0, 0);

            var scrollableContent = CreateItemCollectionView();
            mainGrid.Add(scrollableContent, 0, 1);

            //Content = mainGrid;
            Content = new Border
            {
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#eff1f5"),
                Content = mainGrid

            };
        }

        private Grid HeaderFilter()
        {
            var filterGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 0,
                Margin = new Thickness(10, 0, 10, 0),
            };

            _allStatusBorder = CreateFilterCell("全部");
            _completedStatusBorder = CreateFilterCell("完了");
            _ongoingStatusBorder = CreateFilterCell("進行中");
            _pendingStatusBorder = CreateFilterCell("未着手");
            _abnormalStatusBorder = CreateFilterCell("異常");

            AddTapGesture(_allStatusBorder, FilterStatus.All);
            AddTapGesture(_completedStatusBorder, FilterStatus.Completed);
            AddTapGesture(_ongoingStatusBorder, FilterStatus.Ongoing);
            AddTapGesture(_pendingStatusBorder, FilterStatus.Pending);
            AddTapGesture(_abnormalStatusBorder, FilterStatus.Abnormal);

            Grid.SetColumn(_allStatusBorder, 0);
            filterGrid.Children.Add(_allStatusBorder);

            Grid.SetColumn(_completedStatusBorder, 1);
            filterGrid.Children.Add(_completedStatusBorder);

            Grid.SetColumn(_ongoingStatusBorder, 2);
            filterGrid.Children.Add(_ongoingStatusBorder);

            Grid.SetColumn(_pendingStatusBorder, 3);
            filterGrid.Children.Add(_pendingStatusBorder);

            Grid.SetColumn(_abnormalStatusBorder, 4);
            filterGrid.Children.Add(_abnormalStatusBorder);
            return filterGrid;
        }

        private Border CreateFilterCell(string text)
        {
            var FilterBorder = new Border
            {
                Stroke = Colors.Transparent,
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#607799"),
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(0) },
                Padding = new Thickness(8, 3),
                Margin = new Thickness(0),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center,
                MinimumWidthRequest = 60,
                MinimumHeightRequest = 30,
                HeightRequest = 40
            };

            var statusLabel = new Label
            {
                Text = text,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                CharacterSpacing = 3,
            };
            FilterBorder.Content = statusLabel;
            return FilterBorder;
        }

        private void AddTapGesture(Border border, FilterStatus status)
        {
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (sender, e) => OnFilterTapped(status);
            border.GestureRecognizers.Add(tapGesture);
        }

        private void InitializeFilter()
        {
            SetFilterSelected(FilterStatus.All);
        }

        private void OnFilterTapped(FilterStatus status)
        {
            if (_currentFilter == status) return;

            SetFilterSelected(status);
            ApplyFilter(status);
        }

        private void SetFilterSelected(FilterStatus status)
        {
            ResetAllFilters();

            switch (status)
            {
                case FilterStatus.All:
                    SetFilterCellStyle(_allStatusBorder, true);
                    break;
                case FilterStatus.Completed:
                    SetFilterCellStyle(_completedStatusBorder, true);
                    break;
                case FilterStatus.Ongoing:
                    SetFilterCellStyle(_ongoingStatusBorder, true);
                    break;
                case FilterStatus.Pending:
                    SetFilterCellStyle(_pendingStatusBorder, true);
                    break;
                case FilterStatus.Abnormal:
                    SetFilterCellStyle(_abnormalStatusBorder, true);
                    break;

            }

            _currentFilter = status;
        }

        private void ResetAllFilters()
        {
            SetFilterCellStyle(_allStatusBorder, false);
            SetFilterCellStyle(_completedStatusBorder, false);
            SetFilterCellStyle(_ongoingStatusBorder, false);
            SetFilterCellStyle(_pendingStatusBorder, false);
            SetFilterCellStyle(_abnormalStatusBorder, false);
        }

        private void SetFilterCellStyle(Border border, bool isSelected)
        {
            if (isSelected)
            {
                border.BackgroundColor = Color.FromArgb("#607799");

                if (border.Content is Label label)
                {
                    label.TextColor = Colors.White;
                    label.FontAttributes = FontAttributes.Bold;
                }
            }
            else
            {
                border.BackgroundColor = Colors.White;
                border.Stroke = Color.FromArgb("#E0E0E0");
                border.StrokeThickness = 1;

                if (border.Content is Label label)
                {
                    label.TextColor = Color.FromArgb("#607799");
                    label.FontAttributes = FontAttributes.None;
                }
            }

            SetFilterCellCorners(border, isSelected);
        }

        private void SetFilterCellCorners(Border border, bool isSelected)
        {
            if (border == _allStatusBorder)
            {
                border.StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(5, 0, 0, 0)
                };
            }
            else if (border == _completedStatusBorder)
            {
                border.StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(0, 5, 0, 0)
                };
            }
            else if (border == _ongoingStatusBorder)
            {
                border.StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(0, 5, 0, 0)
                };
            }
            else if (border == _pendingStatusBorder)
            {
                border.StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(0, 5, 0, 0)
                };
            }
            else if (border == _abnormalStatusBorder)
            {
                border.StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(0, 5, 0, 0)
                };
            }
            else
            {
                border.StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(0, 0, 0, 0)
                };
            }
        }

        private void ApplyFilter(FilterStatus status)
        {
            if (_filteredOrders == null)
            {
                _filteredOrders = new ObservableCollection<Product>();
            }

            if (_filteredOrders != null)
            {
                _filteredOrders.Clear();
            }

            var filteredData = status switch
            {
                FilterStatus.All => _products,
                FilterStatus.Completed => _products.Where(o => o.Status == "完了"),
                FilterStatus.Ongoing => _products.Where(o => o.Status == "進行中"),
                FilterStatus.Pending => _products.Where(o => o.Status == "未着手"),
                FilterStatus.Abnormal => _products.Where(o => o.Status == "異常"),
                _ => _products
            };

            foreach (var item in filteredData)
            {
                _filteredOrders.Add(item);
            }

            if (ordersCollectionView != null)
            {
                ordersCollectionView.ItemsSource = _filteredOrders;
            }

            if (!_filteredOrders.Any())
            {
                ShowNoDataMessage();
            }
        }

        private void ShowNoDataMessage()
        {
            Console.WriteLine("一致するデータがありません");
        }

        private ScrollView CreateItemCollectionView()
        {
            var scrollView = new ScrollView
            {
                Orientation = ScrollOrientation.Vertical,
                Content = CreateContentInsideScrollView()
            };

            return scrollView;
        }

        private View CreateContentInsideScrollView()
        {
            var contentLayout = new VerticalStackLayout
            {
                Spacing = 0
            };

            ordersCollectionView = CreateOrdersListView();
            contentLayout.Children.Add(ordersCollectionView);

            return contentLayout;
        }

        private CollectionView CreateOrdersListView()
        {
            _collectionView = new CollectionView
            {
                SelectionMode = SelectionMode.None,
                ItemTemplate = CreateDataTemplate()
            };

            return _collectionView;
        }

        private DataTemplate CreateDataTemplate()
        {
            return new DataTemplate(() =>
            {
                var expandableCard = new ExpandableCardView();
                return expandableCard;
            });
        }

        private async void LoadData(ParamInfo ParamTo)
        {
            var productDataList = new List<Product>();
            var request = new RequestData<ParamInfo, EvangJsonModel>("GetRequiredInfo");
            request.Info = ParamTo;
            var resultList = await this.Post<ParamInfo, EvangJsonModel, Product, EvangJsonModel>(request);
            if (resultList == null || resultList!.SubData[0]!.SubJson == null)
            {
                return;
            }
            var dbJson = resultList!.SubData[0]!.SubJson;
            try
            {
                List<MaterialItem> orderList = new List<MaterialItem>();

                var dynamicList = BaseUtils.JsonToClass<List<dynamic>>(dbJson);
                if (dynamicList == null || dynamicList.Count == 0)
                {
                    return;
                }

                // 创建父产品列表（使用构造函数）

                foreach (var item in dynamicList)
                {
                    var productItem = new Product(
                        GetJsonIntValue(item, "Id"),
                        GetJsonStringValue(item, "ItemId").ToString() ?? string.Empty,
                        GetJsonStringValue(item, "ItemName").ToString() ?? string.Empty,
                        GetJsonStringValue(item, "Status").ToString() ?? string.Empty,
                        GetJsonIntValue(item, "ScheduledQty"),
                        GetJsonIntValue(item, "CompletedQty"),
                        GetJsonIntValue(item, "ParentId"),
                        GetJsonDecimalValue(item, "SubQuantity"),
                        false
                    );
                    productDataList.Add(productItem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON解析失败：{ex.Message}");
                return;
            }

            //// 创建子产品数据（使用构造函数）
            //var subProductsData = new List<Product>
            //{
            //    // 为产品1（スマートフォン）的子产品
            //    new Product(101, "ELEC-001-A", "液晶パネル", "子項目", 0, 0, 1, 50, false),
            //    new Product(102, "ELEC-001-B", "バッテリー", "子項目", 0, 0, 1, 50, false),
            //    new Product(103, "ELEC-001-C", "カメラモジュール", "子項目", 0, 0, 1, 50, false),

            //    // 为产品2（ソファセット）的子产品
            //    new Product(201, "HOME-001-A", "ソファフレーム", "子項目", 0, 0, 2, 20, false),
            //    new Product(202, "HOME-001-B", "クッション", "子項目", 0, 0, 2, 20, false),

            //    // 为产品3（プログラミング書籍）的子产品
            //    new Product(301, "BOOK-001-A", "C#入門", "子項目", 0, 0, 3, 50, false),
            //    new Product(302, "BOOK-001-B", "ASP.NET Core指南", "子項目", 0, 0, 3, 50, false),

            //    // 为产品4（ノートパソコン）的子产品
            //    new Product(401, "ELEC-002-A", "キーボード", "子項目", 0, 0, 4, 30, false),
            //    new Product(402, "ELEC-002-B", "バッテリーパック", "子項目", 0, 0, 4, 30, false),

            //    // 为产品5（ダイニングテーブル）的子产品
            //    new Product(501, "HOME-002-A", "テーブル天板", "子項目", 0, 0, 5, 15, false),
            //    new Product(502, "HOME-002-B", "テーブル脚", "子項目", 0, 0, 5, 15, false),

            //    // 为产品6（ワイヤレスイヤホン）的子产品
            //    new Product(601, "ELEC-003-A", "イヤホン本体", "子項目", 0, 0, 6, 80, false),
            //    new Product(602, "ELEC-003-B", "充電ケース", "子項目", 0, 0, 6, 80, false),

            //    // 为产品7（子供向け絵本）的子产品
            //    new Product(701, "BOOK-002-A", "アニメ絵本", "子項目", 0, 0, 7, 60, false),
            //    new Product(702, "BOOK-002-B", "塗り絵", "子項目", 0, 0, 7, 60, false)
            //};

            //// 将子产品添加到对应的父产品
            //foreach (var parent in productDataList)
            //{
            //    var children = subProductsData.Where(sp => sp.ParentId == parent.Id).ToList();
            //    foreach (var child in children)
            //    {
            //        parent.AddSubProduct(child);
            //    }
            //}

            // 转换为 ObservableCollection
            _products = new ObservableCollection<Product>(productDataList);
            _filteredOrders = new ObservableCollection<Product>(_products);

            // 设置 CollectionView 的数据源
            if (ordersCollectionView != null)
            {
                ordersCollectionView.ItemsSource = _filteredOrders;
            }
            else if (_collectionView != null)
            {
                _collectionView.ItemsSource = _filteredOrders;
            }
        }
        private string GetJsonStringValue(JsonElement jsonElement, string propertyName)
        {
            try
            {
                if (jsonElement.TryGetProperty(propertyName, out JsonElement propertyValue))
                {
                    return propertyValue.GetString() ?? string.Empty;
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private int GetJsonIntValue(JsonElement jsonElement, string propertyName)
        {
            try
            {
                if (jsonElement.TryGetProperty(propertyName, out JsonElement propertyValue))
                {
                    if (propertyValue.ValueKind == JsonValueKind.Number)
                    {
                        return propertyValue.GetInt32();
                    }
                    else if (propertyValue.ValueKind == JsonValueKind.String)
                    {
                        string strValue = propertyValue.GetString() ?? "0";
                        int.TryParse(strValue, out int intValue);
                        return intValue;
                    }
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        private decimal GetJsonDecimalValue(dynamic item, string propertyName)
        {
            try
            {
                // 适配dynamic类型的item，先尝试转换为JsonElement（保持与原有方法一致性）
                if (item is JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty(propertyName, out JsonElement propertyValue))
                    {
                        // 情况1：属性值是数字类型（直接转换）
                        if (propertyValue.ValueKind == JsonValueKind.Number)
                        {
                            return propertyValue.GetDecimal();
                        }
                        // 情况2：属性值是字符串类型（尝试解析为decimal）
                        else if (propertyValue.ValueKind == JsonValueKind.String)
                        {
                            string strValue = propertyValue.GetString() ?? "0";
                            // 忽略千位分隔符、适配小数点格式，保证解析容错性
                            if (decimal.TryParse(strValue, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out decimal decValue))
                            {
                                return decValue;
                            }
                        }
                    }
                }
                // 兼容直接传入字符串/数字的dynamic场景
                else
                {
                    string strValue = item?.ToString() ?? "0";
                    if (decimal.TryParse(strValue, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal decValue))
                    {
                        return decValue;
                    }
                }

                // 所有解析失败场景返回默认值0
                return 0m;
            }
            catch
            {
                // 捕获未知异常，避免程序崩溃，返回默认值0
                return 0m;
            }
        }
    }


}