using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Platform;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace EvangPL.Views.OrderSearch
{
    public class OrderSearch : EvangContentVM
    {
        private SearchBar? searchBar;
        private Picker? statusPicker;
        private DatePicker? datePicker;
        private CollectionView? ordersCollectionView;
        private Grid? bottomNavigationGrid, filterFrame;
        private MaterialCardsView? materialCardsView;
        private OrderPageInfo OrderPageInfoHead;
        private bool isFilterExpanded = false;
        private Picker? productTypePicker;
        private Picker? managerPicker; 
        private Grid? coreFilterGrid;
        private Grid? extraFilterGrid;
        private Grid? mainGrid;

        public OrderSearch() : base("strOrderSearch")
        {
            BuildUI();
        }

        private void BuildUI()
        {
            // 创建主滚动视图
            //var scrollView = new ScrollView();

            // 创建主垂直布局
            //var mainLayout = new VerticalStackLayout
            //{
            //    Spacing = 20,
            //    Padding = new Thickness(20)
            //};

            // 创建顶部筛选区域
            filterFrame = CreateFilterFrame();
            //mainLayout.Children.Add(filterFrame);

            // 添加订单列表标题
            var ordersTitle = new Label
            {
                Text = "ワーク・オーダー",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black
            };

            // 创建主网格布局
            mainGrid = new Grid
            {
                RowDefinitions =
                    {
                        new RowDefinition { Height = 110 }, // 筛选区域
                        new RowDefinition { Height = GridLength.Star }   // 工单区域
                    },
                ColumnDefinitions = { new ColumnDefinition { } },
                BackgroundColor = Color.FromArgb("#eff0f0"),
                RowSpacing = 0
            };
            Grid.SetRow(filterFrame, 0);
            mainGrid.Children.Add(filterFrame);

            // 创建工单容器
            var ordersContainer = new StackLayout
            {
                Spacing = 5,
                Padding = new Thickness(10),
            };

            // 添加工单标题
            ordersContainer.Children.Add(ordersTitle);

            //添加工单卡片视图
            //var allMaterials = new List<MaterialItem>();
            // for (int i = 1; i <= 9; i++)
            // {

            //     allMaterials.Add(new MaterialItem(
            //     //    //$"物料：ABC产品(M{i:000})",
            //     //    //$"到期：100个 / 已产：{i * 5}个",
            //     //    //"客件：XYZ公司"
            //     i,
            //     $"#WO-20250{i}",
            //         "処理中",
            //         "ABC产品",
            //         $"M-{i:000}",
            //         "2025/12/18",
            //         1,
            //         1,
            //         "工程A",
            //         "a",
            //         1
            //     ));
            // }
            // MaterialCardsView ordersCollectionView = new MaterialCardsView(allMaterials);
            ordersContainer.Children.Add(ordersCollectionView);
            //ordersContainer.Children.Add(new Label { Text = "测试ordersContainer", TextColor = Colors.Black, FontSize = 20 });
            var ordersScrollView = new ScrollView
            {
                Content = ordersContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always,
            };

            // 添加可滚动区域到第二行
            //var boxView = new BoxView { Color = Colors.Green, HeightRequest = 200 };
            //Grid.SetRow(ordersScrollView, 1);
            mainGrid.Add(ordersScrollView, 0, 1);
            Content = mainGrid;
        }
            //mainLayout.Children.Add(ordersTitle);

            // 创建订单列表
            //ordersCollectionView = CreateOrdersCollectionView();
            //    var allMaterials = new List<MaterialItem>();
            //    for (int i = 1; i <= 20; i++)
            //    {

            //        allMaterials.Add(new MaterialItem(
            //        //    //$"物料：ABC产品(M{i:000})",
            //        //    //$"到期：100个 / 已产：{i * 5}个",
            //        //    //"客件：XYZ公司"
            //        $"#WO-20250{i}",
            //            "処理中",
            //            "ABC产品",
            //            $"M-{i:000}",
            //            "2025/12/18",
            //            i + 5,
            //            100,
            //            "工程A"
            //        ));
            //}
            //              202512 remove start
            //MaterialCardsView ordersCollectionView = new MaterialCardsView(allMaterials);
            //              202512 remove end
            //mainLayout.Children.Add(ordersCollectionView);

            //var defaultEmptyMaterials = new List<MaterialItem>();
            //materialCardsView = new MaterialCardsView(defaultEmptyMaterials);

            // 创建底部导航
            //bottomNavigationGrid = CreateBottomNavigation();
            //mainLayout.Children.Add(bottomNavigationGrid);

            //scrollView.Content = mainLayout;
            //Content = new ScrollView
            //{
            //    Content = new VerticalStackLayout
            //    {
            //        Spacing = 8,
            //        Padding = new Thickness(5),
            //        BackgroundColor = Color.FromArgb("#eff0f0"),
            //        Children =
            //        {
            //            filterFrame,
            //            ordersTitle,
            //            materialCardsView
            //        }
            //    }
            //};
        //    Content = new ScrollView
        //    {
        //        Content = new VerticalStackLayout
        //        {
        //            Spacing = 8,
        //            Padding = new Thickness(5),
        //            BackgroundColor = Color.FromArgb("#eff0f0"),
        //            Children =
        //            {
        //                filterFrame,
        //                ordersTitle,
        //                materialCardsView,
        //                bottomNavigationGrid
        //            }
        //        }
        //    };  
        //}

        private Grid CreateFilterFrame()
        {
            var searchFilterGrid = new Grid
            {
                BackgroundColor = Color.FromArgb("#f5f5f5"),
                Padding = new Thickness(2)
            };

            var filterLayout = new VerticalStackLayout
            {
                Spacing = 5
            };

            // === 搜索行（保持原有逻辑不变） ===
            var searchRowGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 8,
                HeightRequest = 20
            };

            searchBar = new SearchBar
            {
                Placeholder = "アセンブリを検索",
                BackgroundColor = Colors.White,
                HeightRequest = 20,
                FontSize = 12,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center
            };
            searchRowGrid.Add(searchBar, 0, 0);

            var searchButton = new Button
            {
                Text = "検索",
                BackgroundColor = Color.FromArgb("#1f3854"),
                TextColor = Colors.White,
                HeightRequest = 20,
                CornerRadius = 4,
                WidthRequest = 60,
                VerticalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
            };
            searchRowGrid.Add(searchButton, 1, 0);
            searchButton.Clicked += async (sender, e) => await OnbtnSearchClicked(sender, e);
            filterLayout.Children.Add(searchRowGrid);

            // === 改造筛选区域：拆分核心筛选Grid和额外筛选Grid ===
            // 2. 核心筛选Grid（初始：状态+日期+更多按钮）
            coreFilterGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10,
                Margin = new Thickness(0, 5, 0, 0)
            };

            // === 原有筛选项：状态筛选（保持不变） ===
            var statusLayout = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };
            var statusLabel = new Label
            {
                Text = "ステータス:",
                FontSize = 11,
                TextColor = Colors.Gray
            };
            statusLayout.Children.Add(statusLabel);

            statusPicker = new Picker
            {
                Title = "リリース済み",
                BackgroundColor = Colors.White,
                TextColor = Colors.Black,
                HeightRequest = 20,
                FontSize = 11,
            };
            statusPicker.Items.Add("リリース済み");
            statusPicker.Items.Add("処理中");
            statusPicker.Items.Add("計画済み");
            statusLayout.Children.Add(statusPicker);
            coreFilterGrid.Add(statusLayout, 0, 0);

            // === 原有筛选项：日期筛选（保持不变） ===
            var dateLayout = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };
            var dateLabel = new Label
            {
                Text = "生産予定日:",
                FontSize = 11,
                TextColor = Colors.Gray
            };
            dateLayout.Children.Add(dateLabel);

            datePicker = new DatePicker
            {
                BackgroundColor = Colors.White,
                HeightRequest = 20,
                FontSize = 11,
                Format = "yyyy-MM-dd"
            };
            dateLayout.Children.Add(datePicker);
            coreFilterGrid.Add(dateLayout, 1, 0);

            // === 改造：展开/收缩按钮 ===
            var moreLayout = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.End
            };

            var moreButton = new Button
            {
                Text = "クリックすると展開",
                BackgroundColor = Colors.LightGray,
                TextColor = Colors.Black,
                HeightRequest = 20,
                CornerRadius = 3,
                FontSize = 10,
                Padding = new Thickness(2),
                Margin = 0,
            };
            // 3. 绑定按钮点击事件（核心：切换展开/收缩）
            moreButton.Clicked += OnMoreButtonClicked;
            moreLayout.Children.Add(moreButton);
            coreFilterGrid.Add(moreLayout, 2, 0);
            //var request = new RequestData<OrderPageInfo, EvangJsonModel>("GetWorkOrder");
            //var resultList = await this.Post<OrderPageInfo, EvangJsonModel, MaterialItem, EvangJsonModel>(request);
            //var customerInfo = resultList!.SubData[1]!.SubJson;
            //var departmentInfo = resultList!.SubData[2]!.SubJson;

            // === 新增：额外筛选项布局（初始隐藏） ===
            extraFilterGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10,
                Margin = new Thickness(0, 5, 0, 0),
                IsVisible = false // 初始收缩，隐藏额外筛选项
            };

            // 额外筛选项1：产品类型
            var productTypeLayout = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };
            var productTypeLabel = new Label
            {
                Text = "顧客:",
                FontSize = 11,
                TextColor = Colors.Gray
            };
            productTypeLayout.Children.Add(productTypeLabel);

            productTypePicker = new Picker
            {
                Title = "個人商店",
                BackgroundColor = Colors.White,
                TextColor = Colors.Black,
                FontSize = 12,
                HeightRequest = 20
            };
            productTypePicker.Items.Add("大型チェーン店");
            productTypePicker.Items.Add("北海道電力");
            productTypeLayout.Children.Add(productTypePicker);
            extraFilterGrid.Add(productTypeLayout, 0, 0);

            // 额外筛选项2：负责人
            var managerLayout = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };
            var managerLabel = new Label
            {
                Text = "部門:",
                FontSize = 11,
                TextColor = Colors.Gray
            };
            managerLayout.Children.Add(managerLabel);
            managerPicker = new Picker
            {
                Title = "営業部",
                BackgroundColor = Colors.White,
                TextColor = Colors.Black,
                FontSize = 11,
                HeightRequest = 20
            };
            managerPicker.Items.Add("開発部");
            managerPicker.Items.Add("コンサルティング");
            managerPicker.Items.Add("DM_経理部");
            managerPicker.Items.Add("東京営業部");
            managerPicker.Items.Add("東京開発部");
            managerLayout.Children.Add(managerPicker);
            extraFilterGrid.Add(managerLayout, 1, 0);

            // === 将筛选布局添加到总布局 ===
            filterLayout.Children.Add(coreFilterGrid);
            filterLayout.Children.Add(extraFilterGrid); // 新增：添加额外筛选布局
            searchFilterGrid.Children.Add(filterLayout);
            // === 新增：额外筛选项布局（初始隐藏，添加收缩按钮） ===

            return searchFilterGrid;
        }

        private Grid CreateBottomNavigation()
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                HeightRequest = 20,
                BackgroundColor = Color.FromArgb("#f8f9fa")
            };

            var homeButton = new Button
            {
                Text = "ホーム",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Gray
            };
            //Grid.SetColumn(homeButton, 0);
            grid.Add(homeButton);

            var newOrderButton = new Button
            {
                Text = "新订单",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#2196F3")
            };
            //Grid.SetColumn(newOrderButton, 1);
            grid.Add(newOrderButton, 1);

            var kanbanButton = new Button
            {
                Text = "我的看板",
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Gray
            };
            //Grid.SetColumn(kanbanButton, 2);
            grid.Add(kanbanButton, 2);

            return grid;
        }
        #region OnbtnSearchClicked
        private async Task OnbtnSearchClicked(object sender, EventArgs e)
        {
            try
            {
                // 获取当前的页面布局
                if (!(Content is Grid mainGrid))
                {
                    // 如果不是Grid布局，可能是加载中页面
                    return;
                }

                // 查找ordersScrollView
                ScrollView ordersScrollView = null;
                StackLayout ordersContainer = null;

                // 遍历Grid的子元素，找到第二行的ScrollView
                foreach (var child in mainGrid.Children)
                {
                    if (child is ScrollView scrollView && mainGrid.GetRow(child) == 1)
                    {
                        ordersScrollView = scrollView;
                        break;
                    }
                }

                if (ordersScrollView == null || !(ordersScrollView.Content is StackLayout container))
                {
                    // 如果没有找到，可能是布局结构不一样，尝试恢复默认布局
                    RestoreDefaultLayout();
                    return;
                }

                ordersContainer = container;

                // 1. 移除原有订单列表（若存在）
                if (materialCardsView != null && ordersContainer.Children.Contains(materialCardsView))
                {
                    ordersContainer.Children.Remove(materialCardsView);
                    materialCardsView = null;
                }

                //2.移除已存在的空提示或错误提示标签
                var labelsToRemove = new List<View>();
                foreach (var child in ordersContainer.Children)
                {
                    if (child is Label label)
                    {
                        if (label.Text == "検索条件に一致する注文はありません。" ||
                            label.Text == "予期しないエラーが発生しました。管理者にお問い合わせください。")
                        {
                            labelsToRemove.Add((View)child);
                        }
                    }
                }

                foreach (var label in labelsToRemove)
                {
                    ordersContainer.Children.Remove(label);
                }

                // 3. 获取搜索条件
                OrderPageInfoHead = new OrderPageInfo("", "", "", "", "");
                string searchText = "";
                if (searchBar == null || searchBar.Text == null)
                {
                }
                else
                {
                    searchText = searchBar?.Text?.Trim();
                }

                // 状态筛选器值
                string selectedStatus = statusPicker?.SelectedIndex >= 0
                    ? statusPicker?.SelectedItem?.ToString() ?? string.Empty
                    : statusPicker?.Title ?? string.Empty;

                // 日期值
                DateTime selectedDate = datePicker?.Date ?? DateTime.Now;
                string formattedDate = selectedDate.ToString("yyyy-MM-dd");

                // 客户筛选器值（处理展开状态）
                string selectedCustomer = string.Empty;
                if (isFilterExpanded && productTypePicker != null)
                {
                    selectedCustomer = productTypePicker?.SelectedIndex >= 0
                        ? productTypePicker?.SelectedItem?.ToString() ?? string.Empty
                        : productTypePicker?.Title ?? string.Empty;
                }

                // 部门筛选器值
                string selectedDepartment = string.Empty;
                if (isFilterExpanded && managerPicker != null)
                {
                    selectedDepartment = managerPicker?.SelectedIndex >= 0
                        ? managerPicker?.SelectedItem?.ToString() ?? string.Empty
                        : managerPicker?.Title ?? string.Empty;
                }

                OrderPageInfoHead.ItemId = searchText;
                OrderPageInfoHead.Status = selectedStatus;
                OrderPageInfoHead.StartDate = formattedDate;
                OrderPageInfoHead.Customer = selectedCustomer;
                OrderPageInfoHead.Department = selectedDepartment;

                // 4. 执行搜索
                var request = new RequestData<OrderPageInfo, EvangJsonModel>("GetWorkOrder");
                request.Info = OrderPageInfoHead;
                var resultListInitialization = await this.Post<OrderPageInfo, EvangJsonModel, MaterialItem, EvangJsonModel>(request);

                if (resultListInitialization == null || resultListInitialization!.SubData[0]!.SubJson == null)
                {
                    ShowEmptyDataTip();
                    return;
                }

                var dbJson = resultListInitialization!.SubData[0]!.SubJson;

                List<MaterialItem> orderList = new List<MaterialItem>();

                var dynamicList = BaseUtils.JsonToClass<List<dynamic>>(dbJson);
                if (dynamicList == null || dynamicList.Count == 0)
                {
                    ShowEmptyDataTip();
                    return;
                }

                foreach (var item in dynamicList)
                {
                    int completedQty = GetJsonIntValue(item, "CompletedQuantity");
                    int requiredQty = GetJsonIntValue(item, "RequiredQuantity");

                    var materialItem = new MaterialItem(
                        GetJsonIntValue(item, "Id"),
                        GetJsonStringValue(item, "OrderNumber").ToString() ?? string.Empty,
                        GetJsonStringValue(item, "Status").ToString() ?? string.Empty,
                        GetJsonStringValue(item, "MaterialName").ToString() ?? string.Empty,
                        GetJsonStringValue(item, "MaterialCode").ToString() ?? string.Empty,
                        GetJsonStringValue(item, "StartDate").ToString() ?? string.Empty,
                        completedQty,
                        requiredQty,
                        GetJsonStringValue(item, "ProjectInfo").ToString() ?? string.Empty,
                        GetJsonStringValue(item, "Unit").ToString() ?? string.Empty,
                        GetJsonIntValue(item, "InstanceCount")
                    );
                    orderList.Add(materialItem);
                }

                // 5. 创建新的MaterialCardsView并添加到容器
                var newMaterialCardsView = new MaterialCardsView(orderList);

                // 找到标题的位置
                var titleIndex = -1;
                for (int i = 0; i < ordersContainer.Children.Count; i++)
                {
                    if (ordersContainer.Children[i] is Label label && label.Text == "ワーク・オーダー")
                    {
                        titleIndex = i;
                        break;
                    }
                }

                if (titleIndex != -1)
                {
                    // 插入到标题后面
                    ordersContainer.Children.Insert(titleIndex + 1, newMaterialCardsView);
                }
                else
                {
                    // 如果找不到标题，添加到末尾
                    ordersContainer.Children.Add(newMaterialCardsView);
                }

                materialCardsView = newMaterialCardsView;
            }
            catch (Exception ex)
            {
                // 错误处理
                if (!(Content is Grid mainGrid))
                {
                    return;
                }

                // 查找ordersScrollView
                ScrollView ordersScrollView = null;
                StackLayout ordersContainer = null;

                foreach (var child in mainGrid.Children)
                {
                    if (child is ScrollView scrollView && mainGrid.GetRow(child) == 1)
                    {
                        ordersScrollView = scrollView;
                        break;
                    }
                }

                if (ordersScrollView == null || !(ordersScrollView.Content is StackLayout container))
                {
                    return;
                }

                ordersContainer = container;

                // 清理原有内容
                if (materialCardsView != null && ordersContainer.Children.Contains(materialCardsView))
                {
                    ordersContainer.Children.Remove(materialCardsView);
                }

                var existingTip = ordersContainer.Children.FirstOrDefault(c => c is Label &&
                                     ((Label)c).Text is "検索条件に一致する注文はありません。" or "予期しないエラーが発生しました。管理者にお問い合わせください。") as Label;
                if (existingTip != null)
                {
                    ordersContainer.Children.Remove(existingTip);
                }

                // 创建错误提示标签
                var errorLabel = new Label
                {
                    Text = "予期しないエラーが発生しました。管理者にお問い合わせください。",
                    FontSize = 13,
                    TextColor = Colors.Red,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                };

                // 插入到合适位置
                var titleIndex = -1;
                for (int i = 0; i < ordersContainer.Children.Count; i++)
                {
                    if (ordersContainer.Children[i] is Label label && label.Text == "ワーク・オーダー")
                    {
                        titleIndex = i;
                        break;
                    }
                }

                if (titleIndex != -1)
                {
                    ordersContainer.Children.Insert(titleIndex + 1, errorLabel);
                }
            }
        }

        // 恢复默认布局的方法（当Grid布局找不到时使用）
        private void RestoreDefaultLayout()
        {
            // 恢复为简单的布局，确保搜索功能可用
            filterFrame = CreateFilterFrame();

            var ordersTitle = new Label
            {
                Text = "ワーク・オーダー",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // 使用ScrollView包装内容
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Padding = new Thickness(5),
                    BackgroundColor = Color.FromArgb("#eff0f0"),
                    Children =
            {
                filterFrame,
                ordersTitle
                // materialCardsView会在搜索后添加
            }
                }
            };
        }
        // 4. 核心：展开/收缩按钮点击事件处理（修复后支持稳定收缩回初始状态）
        private void OnMoreButtonClicked(object? sender, EventArgs e)
        {
            if (sender is not Button toggleButton || coreFilterGrid == null || extraFilterGrid == null)
            {
                return;
            }

            isFilterExpanded = !isFilterExpanded;
            extraFilterGrid.IsVisible = isFilterExpanded;

            // 更新按钮文本
            toggleButton.Text = isFilterExpanded ? "クリックすると収縮" : "クリックすると展開";

            // 可选：更新按钮样式
            toggleButton.BackgroundColor = isFilterExpanded ? Colors.Gray : Colors.LightGray;

            // 保持3列布局不变
            if (coreFilterGrid.ColumnDefinitions.Count != 3)
            {
                coreFilterGrid.ColumnDefinitions.Clear();
                coreFilterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                coreFilterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                coreFilterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }
            mainGrid!.RowDefinitions[0].Height = isFilterExpanded ? 160 : 110;
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
        #endregion

        #region 显示空数据提示
        /// <summary>
        /// 数据为空时，显示“数据为空”提示文字
        /// </summary>
        private void ShowEmptyDataTip()
        {
            if (!(Content is Grid mainGrid))
            {
                return;
            }

            // 查找ordersScrollView
            ScrollView ordersScrollView = null;
            StackLayout ordersContainer = null;

            foreach (var child in mainGrid.Children)
            {
                if (child is ScrollView scrollView && mainGrid.GetRow(child) == 1)
                {
                    ordersScrollView = scrollView;
                    break;
                }
            }

            if (ordersScrollView == null || !(ordersScrollView.Content is StackLayout container))
            {
                return;
            }

            ordersContainer = container;

            // 1. 移除原有订单列表（若存在）
            if (materialCardsView != null && ordersContainer.Children.Contains(materialCardsView))
            {
                ordersContainer.Children.Remove(materialCardsView);
                materialCardsView = null;
            }

            // 2. 移除已存在的空提示标签
            var existingEmptyTip = ordersContainer.Children.FirstOrDefault(c =>
                c is Label label && label.Text == "検索条件に一致する注文はありません。") as Label;
            if (existingEmptyTip != null)
            {
                ordersContainer.Children.Remove(existingEmptyTip);
            }

            // 3. 创建"数据为空"提示标签
            var emptyDataLabel = new Label
            {
                Text = "検索条件に一致する注文はありません。",
                FontSize = 12,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 20, 0, 20)
            };

            // 4. 插入到合适位置
            var titleIndex = -1;
            for (int i = 0; i < ordersContainer.Children.Count; i++)
            {
                if (ordersContainer.Children[i] is Label label && label.Text == "ワーク・オーダー")
                {
                    titleIndex = i;
                    break;
                }
            }

            if (titleIndex != -1)
            {
                ordersContainer.Children.Insert(titleIndex + 1, emptyDataLabel);
            }
            else
            {
                ordersContainer.Children.Add(emptyDataLabel);
            }
        }

        #endregion
    }

}
