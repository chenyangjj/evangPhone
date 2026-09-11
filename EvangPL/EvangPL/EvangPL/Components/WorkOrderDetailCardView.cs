using EvangPL.Utils;
using EvangPL.Views.ActualConfirm;
using EvangPL.Views.HariDashi;
using EvangPL.Views.MaterialsList;
using EvangPL.Views.OutHigh;
using EvangPL.Views.ProcessDetail;
using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvangPL.Components
{
    public class DetailCardView : ContentView
    {
        public event EventHandler<int> CardSelected;

        private bool _isSelected;
        private Grid buttonGrid;
        private Border contentBorder;
        private int _id;
        public DetailCardView(int Id, string Status, string ProcessName, int TotalSteps, string ProductionLine, string MemoText, int ActualQuantity, int BadQuantity)
        {
            _id = Id;
            BuildCard(Id, Status, ProcessName, TotalSteps, ProductionLine, MemoText, ActualQuantity, BadQuantity);
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnCardTapped;
            this.GestureRecognizers.Add(tapGesture);
        }

        private void OnCardTapped(object sender, EventArgs e)
        {
            // 触发卡片选中事件
            CardSelected?.Invoke(this, _id);
        }

        // 设置卡片选中状态
        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;

            // 更新边框样式
            if (contentBorder != null)
            {
                contentBorder.Stroke = isSelected ? Color.FromArgb("#4a90e2") : Color.FromArgb("#e0e0e0");
                contentBorder.StrokeThickness = isSelected ? 2 : 1;
                contentBorder.BackgroundColor = isSelected ? Color.FromArgb("#f0f7ff") : Colors.White;
            }

            // 显示或隐藏按钮
            if (buttonGrid != null)
            {
                buttonGrid.IsVisible = isSelected;
            }
        }

        private void BuildCard(int Id, string Status, string ProcessName, int TotalSteps, string ProductionLine, string MemoText, int ActualQuantity, int BadQuantity)
        {

            var mainFrame = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto }
                },
                    ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Margin = new Thickness(0, 0, 0, 2)
            };

            contentBorder = new Border
            {
                Stroke = Color.FromArgb("#e0e0e0"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 5 },
                BackgroundColor = Colors.White,
                Padding = new Thickness(10, 6),
                HorizontalOptions = LayoutOptions.Fill,
            };

            var mainStack = new VerticalStackLayout
            {
                Spacing = 2
            };

            var titleRow = new Grid
            {
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
            };

            var titleLayout = new HorizontalStackLayout
            {
                Spacing = 2
            };

            var titleLabel = new Label
            {
                Text = ProcessName,
                FontSize = 20,
                TextColor = Color.FromArgb("#1f3854"),
                FontAttributes = FontAttributes.Bold,
            };
            //titleLabel.SetBinding(Label.TextProperty, "ProcessName");
            var titleValue = new Label
            {
                Text = TotalSteps.ToString(),
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.End,
                TextColor = Color.FromArgb("1f3854")
            };
            //titleValue.SetBinding(Label.TextProperty, "TotalSteps");
            titleLayout.Children.Add(titleLabel);
            titleLayout.Children.Add(titleValue);

            Grid.SetColumn(titleLayout, 0);
            titleRow.Children.Add(titleLayout);

            // right status label
            var statusBorder = new Border
            {
                Stroke = Colors.Transparent,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(5) },
                Padding = new Thickness(8, 3),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                MinimumWidthRequest = 60
            };

            var statusLabel = new Label
            {
                Text = Status,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            //statusLabel.SetBinding(Label.TextProperty, "Status");
            var statusColor = GetStatusColor(Status);
            statusBorder.BackgroundColor = statusColor;

            //var statusColorConverter = new StatusColorConverter();
            //statusBorder.SetBinding(Border.BackgroundColorProperty, "Status", converter: statusColorConverter);
            statusBorder.Content = statusLabel;

            Grid.SetColumn(statusBorder, 1);
            titleRow.Children.Add(statusBorder);

            mainStack.Children.Add(titleRow);

            var prodLineLayout = new HorizontalStackLayout();
            // 生産ライン
            var orderLabel = new Label
            {
                Text = "生産ライン：",
                FontSize = 13,
                TextColor = Color.FromArgb("#666666")
            };

            var orderValueLabel = new Label
            {
                Text = ProductionLine,
                FontSize = 13,
                //FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#333333")
            };
            //orderValueLabel.SetBinding(Label.TextProperty, "ProductionLine");

            prodLineLayout.Children.Add(orderLabel);
            prodLineLayout.Children.Add(orderValueLabel);
            mainStack.Children.Add(prodLineLayout);
            
            //if (Status == "進行中")
            
            if (true)
            {
                var statsGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    Margin = new Thickness(0)
                };

                var goodLabel = new Label
                {
                    Text = "実績数量：",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#666666")
                };

                var goodValueLabel = new Label
                {
                    Text = ActualQuantity.ToString(),
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#4CAF50")
                };
                //goodValueLabel.SetBinding(Label.TextProperty, "ActualQuantity");

                var badLabel = new Label
                {
                    Text = "不良数量：",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#666666")
                };

                var badValueLabel = new Label
                {
                    Text = BadQuantity.ToString(),
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#F44336")
                };
                //badValueLabel.SetBinding(Label.TextProperty, "BadQuantity");

                var qualityStack = new HorizontalStackLayout
                {
                    Spacing = 16
                };

                var goodStack = new HorizontalStackLayout
                {
                    Spacing = 2
                };
                goodStack.Children.Add(goodLabel);
                goodStack.Children.Add(goodValueLabel);

                var badStack = new HorizontalStackLayout
                {
                    Spacing = 2
                };
                badStack.Children.Add(badLabel);
                badStack.Children.Add(badValueLabel);

                qualityStack.Children.Add(goodStack);
                qualityStack.Children.Add(badStack);

                statsGrid.Add(qualityStack, 0, 2);
                Grid.SetColumnSpan(qualityStack, 2);
                mainStack.Children.Add(statsGrid);
                var memoLineLayout = new VerticalStackLayout
                {
                    Margin = new Thickness(0, 3, 0, 3),
                    HorizontalOptions = LayoutOptions.Fill
                };

                var memoCombinedLabel = new Label
                {
                    FontSize = 13,
                    LineBreakMode = LineBreakMode.WordWrap,
                    MaxLines = 2,
                    HorizontalOptions = LayoutOptions.Start
                };

                var formattedString = new FormattedString();

                formattedString.Spans.Add(new Span
                {
                    Text = "備考：",
                    TextColor = Color.FromArgb("#666666"),
                    FontSize = 13
                });

                formattedString.Spans.Add(new Span
                {
                    Text = MemoText,
                    TextColor = Color.FromArgb("#333333"),
                    FontSize = 13
                });

                memoCombinedLabel.FormattedText = formattedString;

                memoLineLayout.Children.Add(memoCombinedLabel);
                mainStack.Children.Add(memoLineLayout);

                var separator = new BoxView
                {
                    HeightRequest = 1,
                    BackgroundColor = Color.FromArgb("#EEEEEE"),
                    Margin = new Thickness(3, 3, 3, 5)
                };
                mainStack.Children.Add(separator);

                buttonGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Star }
                    },
                    ColumnSpacing = 5
                };

                //var operationButton = CreateActionButton("所要量一覧");
                var ticketButton = CreateActionButton("所要量一覧", Color.FromArgb("#3498DB"));
                ticketButton.Clicked += async (sender, e) =>
                {
                    ParamInfo paramInfoTo = new ParamInfo(Id.ToString());
                    await Navigation.PushAsync(new MaterialsList(paramInfoTo));
                };
                var heatButton = CreateActionButton("実績確認", Color.FromArgb("#3498DB"));
                heatButton.Clicked += async (sender, e) =>
                {
                    ParamInfo paramInfoTo = new ParamInfo(Id.ToString());
                    await Navigation.PushAsync(new ActualConfirm(paramInfoTo));
                };
                var manualButton = CreateActionButton("実績登録", Color.FromArgb("#1f3854"));
                manualButton.Clicked += async (sender, e) =>
                {
                    ParamInfo paramInfoTo = new ParamInfo(Id.ToString());
                    await Navigation.PushAsync(new OutHigh(paramInfoTo));
                };
                var haraidashiButton = CreateActionButton("払出", Color.FromArgb("#1f3854"));
                haraidashiButton.Clicked += async (sender, e) =>
                {
                    ParamInfo paramInfoTo = new ParamInfo(Id.ToString());
                    await Navigation.PushAsync(new HariDashi(paramInfoTo));
                };

                buttonGrid.Add(ticketButton, 0, 0);
                buttonGrid.Add(heatButton, 1, 0);
                buttonGrid.Add(manualButton, 2, 0);
                buttonGrid.Add(haraidashiButton, 3, 0);
                buttonGrid.IsVisible = false;
                mainStack.Children.Add(buttonGrid);
            }
            //var tapGesture = new TapGestureRecognizer();
            //tapGesture.Tapped += async (sender, e) =>
            //{
            //    ParamInfo paramInfoTo = new ParamInfo(Id.ToString());
            //    await Navigation.PushAsync(new ProcessDetail(paramInfoTo));
            //};
            //contentBorder.GestureRecognizers.Add(tapGesture);
            contentBorder.Content = mainStack;
            Grid.SetColumnSpan(contentBorder, 2);
            mainFrame.Children.Add(contentBorder);

            this.Content = mainFrame;
        }

        private Button CreateActionButton(string text, Color bgColor)
        {
            return new Button
            {
                Text = text,
                FontSize = 11,
                CornerRadius = 5,
                BackgroundColor = bgColor,
                TextColor = Colors.White,
                //BorderWidth = 1,
                HeightRequest = 30,
                //WidthRequest = 65,
                VerticalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
                Padding = new Thickness(0),
                MinimumHeightRequest = 0,
                MinimumWidthRequest = 0,
                CharacterSpacing = 3,
            };
        }
        private async void JumpToMaterialsList(object sender, EventArgs e)
        {
            //await Navigation.PushAsync(new MaterialsList());
        }
        //private async void JumpToMaterialsList(object sender, EventArgs e)
        //{
        //    await Navigation.PushAsync(new MaterialsList());
        //}
        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "計画済み" => Color.FromArgb("#2196F3"),  // blue
                "進行中" => Color.FromArgb("#4CAF50"),   // green
                "リリース済み" => Color.FromArgb("#9E9E9E"), // gray
                _ => Color.FromArgb("#757575")           // default gray
            };
        }
        public class StatusColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var status = value as string;
                return status switch
                {
                    "計画済み" => Color.FromArgb("#2196F3"), // blue
                    "進行中" => Color.FromArgb("#4CAF50"), // green
                    "リリース済み" => Color.FromArgb("#9E9E9E"), // gray
                    _ => Color.FromArgb("#757575")  // default gray
                };
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
    public class ProcessInfoItem : EvangJsonModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string Status { get; set; }
        public string MaterialName { get; set; }
        public string MaterialCode { get; set; }
        public string ProcessName { get; set; }
        public int TotalSteps { get; set; }
        public int CompletedQuantity { get; set; }
        public int RequiredQuantity { get; set; }
        public string ProductionLine { get; set; }
        public string MemoText { get; set; }
        public int ActualQuantity { get; set; }
        public int BadQuantity { get; set; }

        public ProcessInfoItem(int id, string orderNumber, string status, string materialName, string materialCode, string processName,
            int totalSteps, int completedQuantity, int requiredQuantity, string productionLine, string memoText, int actualQuantity, int badQuantity)
        {
            Id = id;
            OrderNumber = orderNumber;
            Status = status;
            MaterialName = materialName;
            MaterialCode = materialCode;
            ProcessName = processName;
            TotalSteps = totalSteps;
            CompletedQuantity = completedQuantity;
            RequiredQuantity = requiredQuantity;
            ProductionLine = productionLine;
            MemoText = memoText;
            ActualQuantity = actualQuantity;
            BadQuantity = badQuantity;
        }
    }

    public class WorkOrderDetailCardView : EvangContentView
    {
        private List<ProcessInfoItem>? allProcessInfo = new List<ProcessInfoItem>();
        //private int currentPage = 0;
        //private int totalPages = 0;
        //private List<List<ProcessInfoItem>>? pages;
        private StackLayout? cardContainer;
        //private Button? prevButton;
        //private Button? nextButton;
        //private Label? pageLabel;
        private Dictionary<int, DetailCardView> _cardViews;
        private int? _selectedCardId;
        public WorkOrderDetailCardView(List<ProcessInfoItem> ProcessInfoData)
        {
            allProcessInfo = ProcessInfoData;
            _cardViews = new Dictionary<int, DetailCardView>();
            InitializeData();
            BuildUI();
            LoadPage(0);
        }
        private void InitializeData()
        {
            if (allProcessInfo == null)
                return;
            //pages = new List<List<ProcessInfoItem>>();
            //for (int i = 0; i < allProcessInfo.Count; i += 5)
            //{
            //    var pageItems = allProcessInfo.Skip(i).Take(5).ToList();
            //    pages.Add(pageItems);
            //}

            //totalPages = pages.Count;
        }
        private void BuildUI()
        {
            var mainLayout = new VerticalStackLayout
            {
                Spacing = 0,
                BackgroundColor = Color.FromArgb("#eff0f0")
            };

            cardContainer = new StackLayout
            {
                Spacing = 0,
                Padding = new Thickness(0),
                //BackgroundColor = Colors.Tomato,
            };
            mainLayout.Children.Add(cardContainer);

            //var pageControlLayout = new HorizontalStackLayout
            //{
            //    HorizontalOptions = LayoutOptions.Center,
            //    Spacing = 20,
            //    Margin = new Thickness(10)
            //};

            //prevButton = new Button
            //{
            //    Text = "◀ 前へ",
            //    FontSize = 14,
            //    BackgroundColor = Colors.White,
            //    TextColor = Color.FromArgb("#2196F3"),
            //    BorderColor = Color.FromArgb("#2196F3"),
            //    BorderWidth = 1,
            //    CornerRadius = 5,
            //    WidthRequest = 100,
            //    HeightRequest = 40
            //};
            //prevButton.Clicked += OnPrevButtonClicked;

            //pageLabel = new Label
            //{
            //    FontSize = 16,
            //    TextColor = Colors.Black,
            //    VerticalOptions = LayoutOptions.Center
            //};

            //nextButton = new Button
            //{
            //    Text = "次へ ▶",
            //    FontSize = 14,
            //    BackgroundColor = Colors.White,
            //    TextColor = Color.FromArgb("#2196F3"),
            //    BorderColor = Color.FromArgb("#2196F3"),
            //    BorderWidth = 1,
            //    CornerRadius = 5,
            //    WidthRequest = 100,
            //    HeightRequest = 40
            //};
            //nextButton.Clicked += OnNextButtonClicked;

            //pageControlLayout.Add(prevButton);
            //pageControlLayout.Add(pageLabel);
            //pageControlLayout.Add(nextButton);
            //mainLayout.Add(pageControlLayout);

            //if (pages == null)
            //    return;
            //var totalCount = pages.Sum(page => page.Count);
            //var statsLabel = new Label
            //{
            //    Text = $"合計: {totalCount}",
            //    FontSize = 14,
            //    TextColor = Colors.Gray,
            //    HorizontalOptions = LayoutOptions.Center,
            //    Margin = new Thickness(0, 10, 0, 20)
            //};
            //mainLayout.Children.Add(statsLabel);

            Content = new ScrollView
            {
                Content = mainLayout
            };
        }
        private void LoadPage(int pageIndex)
        {
            //if (pageIndex < 0 || pageIndex >= totalPages)
            //    return;

            //currentPage = pageIndex;
            cardContainer!.Clear();

            var currentPageData = allProcessInfo!;
            //cardContainer.HeightRequest = 770;

            foreach (var item in currentPageData)
            {
                var card = new DetailCardView(item.Id, item.Status, item.ProcessName, item.TotalSteps, item.ProductionLine, item.MemoText, item.ActualQuantity, item.BadQuantity);
                card.CardSelected += OnCardSelected;
                _cardViews[item.Id] = card;
                cardContainer.Add(card);
            }

            //pageLabel!.Text = $"{pageIndex + 1} / {totalPages}";

            //prevButton!.IsEnabled = pageIndex > 0;
            //nextButton!.IsEnabled = pageIndex < totalPages - 1;

            //prevButton.TextColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            //nextButton.TextColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            //prevButton.BorderColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            //nextButton.BorderColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;

        }

        //private void OnPrevButtonClicked(object? sender, EventArgs e)
        //{
        //    if (currentPage > 0)
        //    {
        //        LoadPage(currentPage - 1);
        //    }
        //}

        //private void OnNextButtonClicked(object? sender, EventArgs e)
        //{
        //    if (currentPage < totalPages - 1)
        //    {
        //        LoadPage(currentPage + 1);
        //    }
        //}
        private void OnCardSelected(object sender, int cardId)
        {
            // 如果点击的是当前已选中的卡片，则取消选中
            if (_selectedCardId == cardId)
            {
                DeselectCard(cardId);
                _selectedCardId = null;
                return;
            }

            // 如果之前有选中的卡片，先取消选中
            if (_selectedCardId.HasValue && _cardViews.ContainsKey(_selectedCardId.Value))
            {
                DeselectCard(_selectedCardId.Value);
            }

            // 选中新卡片
            SelectCard(cardId);
            _selectedCardId = cardId;
        }

        private void SelectCard(int cardId)
        {
            if (_cardViews.TryGetValue(cardId, out var card))
            {
                card.SetSelected(true);
            }
        }

        private void DeselectCard(int cardId)
        {
            if (_cardViews.TryGetValue(cardId, out var card))
            {
                card.SetSelected(false);
            }
        }
    }
}
