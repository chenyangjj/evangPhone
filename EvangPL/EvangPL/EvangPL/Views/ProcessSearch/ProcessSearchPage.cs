using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EvangPL.Views.ProcessSearch
{
    public class ProcessSearch : EvangContentVM
    {
        private SearchBar? searchBar;
        private Picker? statusPicker, managerPicker, kbnPicker;
        private DatePicker? datePicker;
        private ProcessSearchInfo? ProcessSearchInfoHead;
        private Grid? filterGrid, extraFilterGrid;
        private bool isFilterExpanded = false;
        private VerticalStackLayout contentLayout;
        // 分页相关变量
        private List<ProcessEntry>? allProcessData;
        private int currentPage = 0;
        private int pageSize = 5;
        private int totalPages = 0;
        private ProcessSearchCardView? processSearchCardView;

        // 分页控件
        private Grid? paginationLayout;
        private Button? prevButton;
        private Button? nextButton;
        private Label? pageLabel;
        private Label? totalLabel;
        private Label? ordersTitle;

        public ProcessSearch() : base("strProcessSearch")
        {
            Title = "工順検索";
            BuildUI();
        }

        private void BuildUI()
        {
            var mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                }
            };

            var filterFrame = CreateFilterFrame();
            mainGrid.Add(filterFrame, 0, 0);

            var scrollView = CreateContentContainer();
            mainGrid.Add(scrollView, 0, 1);

            Content = new Border
            {
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#eff1f5"),
                Content = mainGrid
            };
        }

        private ScrollView CreateContentContainer()
        {
            contentLayout = new VerticalStackLayout
            {
                Padding = new Thickness(2),
                Margin = new Thickness(0)
            };

            return new ScrollView
            {
                Content = contentLayout
            };
        }

        private Grid CreateFilterFrame()
        {
            var searchFilterGrid = new Grid
            {
                BackgroundColor = Color.FromArgb("#f5f5f5"),
                Padding = new Thickness(2),
                Margin = new Thickness(10, 0, 10, 0)
            };

            var filterLayout = new VerticalStackLayout
            {
                Spacing = 5
            };

            // 搜索行
            var searchRowGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 10,
                HeightRequest = 45
            };

            searchBar = new SearchBar
            {
                Placeholder = "検索工順No/工順名...",
                BackgroundColor = Colors.White,
                CancelButtonColor = Colors.Gray,
                HeightRequest = 40,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center
            };
            searchRowGrid.Children.Add(searchBar);
            Grid.SetColumn(searchBar, 0);

            var searchButton = new Button
            {
                Text = "検索",
                BackgroundColor = Color.FromArgb("#1f3854"),
                TextColor = Colors.White,
                HeightRequest = 40,
                CornerRadius = 5,
                WidthRequest = 65,
                VerticalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
            };
            searchRowGrid.Children.Add(searchButton);
            Grid.SetColumn(searchButton, 1);
            searchButton.Clicked += async (sender, e) => await OnbtnSearchClicked(sender, e);

            filterLayout.Children.Add(searchRowGrid);

            // 第一行筛选器
            filterGrid = new Grid
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

            // 工順区分
            var kbnLayout = new VerticalStackLayout();
            kbnLayout.Children.Add(new Label
            {
                Text = "工順区分:",
                FontSize = 13,
                TextColor = Colors.Gray
            });

            kbnPicker = new Picker
            {
                BackgroundColor = Colors.White,
                TextColor = Colors.Black,
                HeightRequest = 40
            };
            kbnPicker.Items.Add("INHOUSE 内製");
            kbnPicker.Items.Add("OUTSOURCE 外注");
            kbnPicker.Items.Add("INSPECTION 検査");
            kbnPicker.Items.Add("ASSIST 補助／搬送等");
            AddRequiredValidation(kbnPicker, "工順区分");
            kbnLayout.Children.Add(kbnPicker);

            filterGrid.Children.Add(kbnLayout);
            Grid.SetColumn(kbnLayout, 0);

            // 日期
            var dateLayout = new VerticalStackLayout();
            dateLayout.Children.Add(new Label
            {
                Text = "日付:",
                FontSize = 13,
                TextColor = Colors.Gray
            });

            datePicker = new DatePicker
            {
                BackgroundColor = Colors.White,
                HeightRequest = 40,
                Format = "yyyy-MM-dd"
            };
            dateLayout.Children.Add(datePicker);

            filterGrid.Children.Add(dateLayout);
            Grid.SetColumn(dateLayout, 1);

            // 更多按钮
            var moreButton = new Button
            {
                Text = "その他",
                BackgroundColor = Colors.LightGray,
                TextColor = Colors.Black,
                HeightRequest = 40,
                CornerRadius = 5,
                VerticalOptions = LayoutOptions.End
            };
            moreButton.Clicked += OnMoreButtonClicked;

            filterGrid.Children.Add(moreButton);
            Grid.SetColumn(moreButton, 2);

            filterLayout.Children.Add(filterGrid);

            // 额外筛选器（折叠）
            extraFilterGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10,
                Margin = new Thickness(0, 5, 0, 0),
                IsVisible = false
            };

            // 状态筛选
            var statusLayout = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };
            statusLayout.Children.Add(new Label
            {
                Text = "ステータス:",
                FontSize = 13,
                TextColor = Colors.Gray
            });

            statusPicker = new Picker
            {
                Title = "リリース済み",
                BackgroundColor = Colors.White,
                TextColor = Colors.Black,
                HeightRequest = 20,
                FontSize = 13,
            };
            statusPicker.Items.Add("リリース済み");
            statusPicker.Items.Add("処理中");
            statusPicker.Items.Add("計画済み");
            statusLayout.Children.Add(statusPicker);

            extraFilterGrid.Children.Add(statusLayout);
            Grid.SetColumn(statusLayout, 0);

            // 部门筛选
            var managerLayout = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };
            managerLayout.Children.Add(new Label
            {
                Text = "部門:",
                FontSize = 13,
                TextColor = Colors.Gray
            });

            managerPicker = new Picker
            {
                Title = "営業部",
                BackgroundColor = Colors.White,
                TextColor = Colors.Black,
                FontSize = 13,
                HeightRequest = 20
            };
            managerPicker.Items.Add("開発部");
            managerPicker.Items.Add("コンサルティング");
            managerPicker.Items.Add("DM_経理部");
            managerPicker.Items.Add("東京営業部");
            managerPicker.Items.Add("東京開発部");
            managerLayout.Children.Add(managerPicker);

            extraFilterGrid.Children.Add(managerLayout);
            Grid.SetColumn(managerLayout, 1);

            filterLayout.Children.Add(extraFilterGrid);

            // 标题
            var ordersTitle = new Label
            {
                Text = "工順一覧",
                FontSize = 15,
                TextColor = Color.FromArgb("#1f3854"),
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            };
            filterLayout.Children.Add(ordersTitle);

            CreatePaginationControls();
            filterLayout.Children.Add(paginationLayout!);

            searchFilterGrid.Children.Add(filterLayout);
            return searchFilterGrid;
        }
        private void CreatePaginationControls()
        {
            paginationLayout = new Grid
            {
                IsVisible = false,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }, 
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star }
                },
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 0, 0),
                RowSpacing = 3
            };
            var buttonRow = new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Spacing = 25
            };
            // 前へ按钮
            prevButton = new Button
            {
                Text = "◀ 前へ",
                FontSize = 12,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#2196F3"),
                BorderColor = Color.FromArgb("#2196F3"),
                BorderWidth = 1,
                CornerRadius = 5,
                WidthRequest = 75,
                HeightRequest = 35,
                MinimumWidthRequest = 60,
                MinimumHeightRequest = 30,
                IsEnabled = false,
                HorizontalOptions = LayoutOptions.Center,
                Padding = new Thickness(2),
            };
            prevButton.Clicked += OnPrevButtonClicked;

            // 页数标签
            pageLabel = new Label
            {
                FontSize = 12,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(0, 5, 0, 0),
            };

            // 次へ按钮
            nextButton = new Button
            {
                Text = "次へ ▶",
                FontSize = 12,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#2196F3"),
                BorderColor = Color.FromArgb("#2196F3"),
                BorderWidth = 1,
                CornerRadius = 5,
                WidthRequest = 75,
                HeightRequest = 35,
                MinimumWidthRequest = 60,
                MinimumHeightRequest = 30,
                IsEnabled = false,
                HorizontalOptions = LayoutOptions.Center,
                Padding = new Thickness(2),
            };
            nextButton.Clicked += OnNextButtonClicked;

            buttonRow.Children.Add(prevButton);
            buttonRow.Children.Add(pageLabel);
            buttonRow.Children.Add(nextButton);

            totalLabel = new Label
            {
                FontSize = 12,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            };
            Grid.SetRow(buttonRow, 0);
            Grid.SetRow(totalLabel, 1);

            paginationLayout.Children.Add(buttonRow);
            paginationLayout.Children.Add(totalLabel);
        }
        private async Task OnbtnSearchClicked(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateAllFields())
                {
                    await DisplayAlert("エラー", "工順区分の値を入力します", "確認");
                    return;
                }

                ClearContentContainer();
                HidePagination();

                string searchText = searchBar?.Text?.Trim() ?? string.Empty;

                // 收集筛选条件
                string selectedKbn = kbnPicker?.SelectedIndex >= 0
                    ? kbnPicker?.SelectedItem?.ToString() ?? string.Empty
                    : kbnPicker?.Title ?? string.Empty;

                DateTime selectedDate = datePicker?.Date ?? DateTime.Now;
                string formattedDate = selectedDate.ToString("yyyy-MM-dd");

                string selectedStatus = string.Empty;
                if (isFilterExpanded && statusPicker != null)
                {
                    selectedStatus = statusPicker?.SelectedIndex >= 0
                        ? statusPicker?.SelectedItem?.ToString() ?? string.Empty
                        : statusPicker?.Title ?? string.Empty;
                }

                string selectedDepartment = string.Empty;
                if (isFilterExpanded && managerPicker != null)
                {
                    selectedDepartment = managerPicker?.SelectedIndex >= 0
                        ? managerPicker?.SelectedItem?.ToString() ?? string.Empty
                        : managerPicker?.Title ?? string.Empty;
                }

                ProcessSearchInfoHead = new ProcessSearchInfo(
                    searchText,
                    selectedStatus,
                    formattedDate,
                    selectedKbn,
                    selectedDepartment
                );

                var request = new RequestData<ProcessSearchInfo, EvangJsonModel>("GetOperationType");
                request.Info = ProcessSearchInfoHead;
                var resultListInitialization = await this.Post<ProcessSearchInfo, EvangJsonModel, ProcessEntry, EvangJsonModel>(request);

                if (resultListInitialization == null || resultListInitialization!.SubData[0]!.SubJson == null)
                {
                    ShowMessage("データがありません", Colors.Gray);
                    return;
                }

                var dbJson = resultListInitialization!.SubData[0]!.SubJson;
                var dynamicList = BaseUtils.JsonToClass<List<dynamic>>(dbJson);

                if (dynamicList == null || dynamicList.Count == 0)
                {
                    ShowMessage("検索条件に一致する工順はありません。", Colors.Gray);
                    return;
                }

                List<ProcessEntry> orderList = dynamicList.Select(item => new ProcessEntry(
                    GetJsonIntValue(item, "Id"),
                    GetJsonStringValue(item, "OrderNumber"),
                    GetJsonStringValue(item, "Status"),
                    GetJsonStringValue(item, "MaterialName"),
                    GetJsonStringValue(item, "MaterialCode"),
                    GetJsonStringValue(item, "StartDate"),
                    GetJsonIntValue(item, "CompletedQuantity"),
                    GetJsonIntValue(item, "RequiredQuantity"),
                    GetJsonStringValue(item, "ProcessInfo"),
                    GetJsonStringValue(item, "Unit")
                )).ToList();

                ShowData(orderList);
            }
            catch (Exception ex)
            {
                ShowMessage("予期しないエラーが発生しました。管理者にお問い合わせください。", Colors.Red);
            }
        }

        private void ShowData(List<ProcessEntry> orderList)
        {
            ClearContentContainer();

            if (orderList == null || orderList.Count == 0)
            {
                ShowMessage("検索条件に一致する工順はありません。", Colors.Gray);
                HidePagination();
                return;
            }

            allProcessData = orderList;
            currentPage = 0;
            totalPages = (int)Math.Ceiling((double)orderList.Count / pageSize);

            ShowPagination();
            LoadPage(currentPage);
        }

        private void ShowMessage(string message, Color color)
        {
            ClearContentContainer();

            var messageLabel = new Label
            {
                Text = message,
                FontSize = 13,
                TextColor = color,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 20, 0, 20)
            };

            contentLayout.Children.Add(messageLabel);
        }

        private void ClearContentContainer()
        {
            contentLayout.Children.Clear();
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

        private void OnMoreButtonClicked(object? sender, EventArgs e)
        {
            if (sender is not Button toggleButton || filterGrid == null || extraFilterGrid == null)
            {
                return;
            }

            isFilterExpanded = !isFilterExpanded;
            extraFilterGrid.IsVisible = isFilterExpanded;
            toggleButton.BackgroundColor = isFilterExpanded ? Colors.Gray : Colors.LightGray;
        }

        private void AddRequiredValidation(Picker picker, string fieldName)
        {
            picker.SelectedIndexChanged += (sender, e) =>
            {
                if (picker.SelectedIndex != -1)
                {
                    picker.BackgroundColor = Colors.White;
                    picker.TitleColor = Colors.Gray;
                }
            };
        }

        public bool ValidateAllFields()
        {
            bool isValid = true;

            if (kbnPicker?.SelectedIndex == -1)
            {
                kbnPicker.BackgroundColor = Color.FromRgba(255, 0, 0, 0.1);
                kbnPicker.TitleColor = Colors.Red;
                isValid = false;
            }

            return isValid;
        }
        private void LoadPage(int pageIndex)
        {
            if (allProcessData == null || pageIndex < 0 || pageIndex >= totalPages)
                return;

            currentPage = pageIndex;
            var pageData = allProcessData.Skip(pageIndex * pageSize).Take(pageSize).ToList();

            if (processSearchCardView == null)
            {
                processSearchCardView = new ProcessSearchCardView(pageData);
                contentLayout.Children.Add(processSearchCardView);
            }
            else
            {
                processSearchCardView.UpdateData(pageData);
            }

            UpdatePaginationControls();
        }

        private void UpdatePaginationControls()
        {
            if (paginationLayout == null || pageLabel == null || totalLabel == null ||
                prevButton == null || nextButton == null || allProcessData == null)
                return;

            pageLabel.Text = $"{currentPage + 1} / {totalPages}";
            totalLabel.Text = $"合計: {allProcessData.Count}";

            prevButton.IsEnabled = currentPage > 0;
            nextButton.IsEnabled = currentPage < totalPages - 1;

            prevButton.TextColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            nextButton.TextColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            prevButton.BorderColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            nextButton.BorderColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
        }

        private void ShowPagination()
        {
            if (paginationLayout != null)
            {
                paginationLayout.IsVisible = true;
            }
        }

        private void HidePagination()
        {
            if (paginationLayout != null)
            {
                paginationLayout.IsVisible = false;
            }
        }

        private void OnPrevButtonClicked(object sender, EventArgs e)
        {
            if (currentPage > 0)
            {
                LoadPage(currentPage - 1);
            }
        }

        private void OnNextButtonClicked(object sender, EventArgs e)
        {
            if (currentPage < totalPages - 1)
            {
                LoadPage(currentPage + 1);
            }
        }
    }
}