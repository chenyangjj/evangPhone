using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.EvangModel;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvangPL.Components
{
    public class ProcessSearchCard : Border
    {
        public ProcessSearchCard(int Id, string OrderNumber, string Status, string MaterialName, string MaterialCode,
            string StartDate, int CompletedQuantity, int RequiredQuantity, string ProcessInfo, string Unit)
        {
            BuildUI(Id, OrderNumber, Status, MaterialName, MaterialCode, StartDate,
                    CompletedQuantity, RequiredQuantity, ProcessInfo, Unit);
        }

        private void BuildUI(int Id, string OrderNumber, string Status, string MaterialName, string MaterialCode,
            string StartDate, int CompletedQuantity, int RequiredQuantity, string ProcessInfo, string Unit)
        {
            var mainGrid = new Grid
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
                Margin = new Thickness(0),
            };

            var orderContent = new VerticalStackLayout
            {
                Spacing = 5
            };

            var orderNumberRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var orderNumberLayout = new HorizontalStackLayout();
            var orderNumberLabel = new Label
            {
                Text = "*Work Order ",
                FontSize = 15,
                TextColor = Color.FromArgb("#1f3854"),
                FontAttributes = FontAttributes.Bold,
            };
            var orderNumberValue = new Label
            {
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1f3854"),
                Text = OrderNumber
            };
            orderNumberLayout.Children.Add(orderNumberLabel);
            orderNumberLayout.Children.Add(orderNumberValue);

            Grid.SetColumn(orderNumberLayout, 0);
            orderNumberRow.Children.Add(orderNumberLayout);

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

            var statusColor = GetStatusColor(Status);
            statusBorder.BackgroundColor = statusColor;
            statusBorder.Content = statusLabel;

            Grid.SetColumn(statusBorder, 1);
            orderNumberRow.Children.Add(statusBorder);

            orderContent.Children.Add(orderNumberRow);

            var materialLayout = new HorizontalStackLayout
            {
                Margin = new Thickness(0)
            };
            var materialLabel = new Label
            {
                Text = "アセンブリ：",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var materialNameLabel = new Label
            {
                FontSize = 13,
                TextColor = Colors.Black,
                Text = MaterialName
            };
            var leftParen = new Label
            {
                Text = "(",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var materialCodeLabel = new Label
            {
                FontSize = 13,
                TextColor = Colors.Gray,
                Text = MaterialCode
            };
            var rightParen = new Label
            {
                Text = ")",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            materialLayout.Children.Add(materialLabel);
            materialLayout.Children.Add(materialNameLabel);
            materialLayout.Children.Add(leftParen);
            materialLayout.Children.Add(materialCodeLabel);
            materialLayout.Children.Add(rightParen);

            var progressGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Margin = new Thickness(0)
            };

            // 左侧：制造开始日
            var startDateLayout = new HorizontalStackLayout();
            var startDateLabel = new Label
            {
                Text = "製造開始日：",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var startDateValue = new Label
            {
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#4CAF50"),
                Text = StartDate
            };
            startDateLayout.Children.Add(startDateLabel);
            startDateLayout.Children.Add(startDateValue);
            Grid.SetColumn(startDateLayout, 0);
            progressGrid.Children.Add(startDateLayout);

            // 右侧：数量
            var quantityLayout = new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.End
            };
            var quantityLabel = new Label
            {
                Text = "数量：",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var completedQuantityLabel = new Label
            {
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#4CAF50"),
                Text = CompletedQuantity.ToString()
            };
            var slash = new Label
            {
                Text = "/",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var requiredQuantityLabel = new Label
            {
                FontSize = 13,
                TextColor = Colors.Black,
                Text = RequiredQuantity.ToString()
            };
            var unit = new Label
            {
                Text = Unit.ToString(),
                FontSize = 13,
                TextColor = Colors.Gray
            };
            quantityLayout.Children.Add(quantityLabel);
            quantityLayout.Children.Add(completedQuantityLabel);
            quantityLayout.Children.Add(slash);
            quantityLayout.Children.Add(requiredQuantityLabel);
            quantityLayout.Children.Add(unit);
            Grid.SetColumn(quantityLayout, 1);
            progressGrid.Children.Add(quantityLayout);

            // ====== 修改点：使用 Grid 替代 HorizontalStackLayout ======
            var projectGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto }, // "工順内容："标签
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } // ProcessInfo 内容
                },
                ColumnSpacing = 5
            };

            var projectLabel = new Label
            {
                Text = "工順内容：",
                FontSize = 13,
                TextColor = Colors.Gray,
                VerticalOptions = LayoutOptions.Start,
                LineBreakMode = LineBreakMode.NoWrap // 标签不换行
            };
            Grid.SetColumn(projectLabel, 0);
            projectGrid.Children.Add(projectLabel);

            var projectValue = new Label
            {
                FontSize = 13,
                TextColor = Colors.Black,
                Text = ProcessInfo,
                LineBreakMode = LineBreakMode.WordWrap, // 允许文本换行
                VerticalOptions = LayoutOptions.Start
            };
            Grid.SetColumn(projectValue, 1);
            projectGrid.Children.Add(projectValue);

            var progress = RequiredQuantity > 0 ? CompletedQuantity / (double)RequiredQuantity : 0;
            var progressBar = new ProgressBar
            {
                ProgressColor = Color.FromArgb("#4CAF50"),
                BackgroundColor = Color.FromArgb("#e0e0e0"),
                HeightRequest = 6,
                Progress = progress
            };

            orderContent.Children.Add(materialLayout);
            orderContent.Children.Add(progressGrid);
            orderContent.Children.Add(projectGrid); // 使用修改后的 projectGrid
            orderContent.Children.Add(progressBar);

            Grid.SetColumnSpan(orderContent, 2);
            mainGrid.Children.Add(orderContent);

            this.Stroke = Color.FromArgb("#e0e0e0");
            this.StrokeThickness = 1;
            this.StrokeShape = new RoundRectangle { CornerRadius = 5 };
            this.BackgroundColor = Colors.White;
            this.Padding = new Thickness(15);
            this.Margin = new Thickness(10, 5);
            this.Content = mainGrid;
        }

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "計画済み" => Color.FromArgb("#2196F3"),
                "処理中" => Color.FromArgb("#4CAF50"),
                "リリース済み" => Color.FromArgb("#9E9E9E"),
                _ => Color.FromArgb("#757575")
            };
        }
    }
    public class ProcessEntry : EvangJsonModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string Status { get; set; }
        public string MaterialName { get; set; }
        public string MaterialCode { get; set; }
        public string StartDate { get; set; }
        public int CompletedQuantity { get; set; }
        public int RequiredQuantity { get; set; }
        public string ProcessInfo { get; set; }
        public string Unit { get; set; }
        public double Progress => RequiredQuantity > 0 ? CompletedQuantity / (double)RequiredQuantity : 0;

        public ProcessEntry(int id, string orderNumber, string status, string materialName, string materialCode, string startDate,
            int completedQuantity, int requiredQuantity, string processInfo, string unit)
        {
            Id = id;
            OrderNumber = orderNumber;
            Status = status;
            MaterialName = materialName;
            MaterialCode = materialCode;
            StartDate = startDate;
            CompletedQuantity = completedQuantity;
            RequiredQuantity = requiredQuantity;
            ProcessInfo = processInfo;
            Unit = unit;
        }
    }

    public class ProcessSearchCardView : EvangContentView
    {
        //private List<ProcessEntry>? allProcess = new List<ProcessEntry>();
        //private int currentPage = 0;
        //private int totalPages = 0;
        //private List<List<ProcessEntry>> pages;
        private StackLayout cardContainer;
        //private Button prevButton;
        //private Button nextButton;
        //private Label pageLabel;

        public ProcessSearchCardView(List<ProcessEntry>? pageData)
        {
            //allProcess = allProcessdata;
            //InitializeData();
            //BuildPagination();
            //LoadPage(0);
            BuildUI(pageData);
        }
        private void BuildUI(List<ProcessEntry>? pageData)
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
            };

            if (pageData != null && pageData.Count > 0)
            {
                foreach (var item in pageData)
                {
                    var card = new ProcessSearchCard(item.Id, item.OrderNumber, item.Status, item.MaterialName,
                        item.MaterialCode, item.StartDate, item.CompletedQuantity, item.RequiredQuantity,
                        item.ProcessInfo, item.Unit);
                    cardContainer.Children.Add(card);
                }
            }

            mainLayout.Children.Add(cardContainer);
            Content = new ScrollView
            {
                Content = mainLayout
            };
        }

        // 提供方法用于更新显示的数据
        public void UpdateData(List<ProcessEntry>? pageData)
        {
            cardContainer.Children.Clear();
            if (pageData != null && pageData.Count > 0)
            {
                foreach (var item in pageData)
                {
                    var card = new ProcessSearchCard(item.Id, item.OrderNumber, item.Status, item.MaterialName,
                        item.MaterialCode, item.StartDate, item.CompletedQuantity, item.RequiredQuantity,
                        item.ProcessInfo, item.Unit);
                    cardContainer.Children.Add(card);
                }
            }
        }
        //private void InitializeData()
        //{
        //    //var allProcess = new List<ProcessEntry>();
        //    ////valueList = 
        //    //for (int i = 1; i <= 20; i++)
        //    //{
        //    //    allProcess.Add(new ProcessEntry(
        //    //        //$"物料：ABC产品(M{i:000})",
        //    //        //$"到期：100个 / 已产：{i * 5}个",
        //    //        //"客件：XYZ公司"
        //    //        $"#WO-20250{i}",
        //    //        "処理中",
        //    //        "ABC产品",
        //    //        $"M-{i:000}",
        //    //        "2025/12/18",
        //    //        i+5,
        //    //        100,
        //    //        "工程A"
        //    //    ));
        //    //}
        //    if (allProcess == null)
        //        return;
        //    pages = new List<List<ProcessEntry>>();
        //    for (int i = 0; i < allProcess.Count; i += 5)
        //    {
        //        var pageItems = allProcess.Skip(i).Take(5).ToList();
        //        pages.Add(pageItems);
        //    }

        //    totalPages = pages.Count;
        //}

        //private void BuildPagination()
        //{
        //    var mainLayout = new VerticalStackLayout
        //    {
        //        Spacing = 0,
        //        BackgroundColor = Color.FromArgb("#eff0f0")
        //    };

        //    cardContainer = new StackLayout
        //    {
        //        Spacing = 0,
        //        Padding = new Thickness(0),  
        //    };
        //    mainLayout.Children.Add(cardContainer);

        //    var pageControlLayout = new HorizontalStackLayout
        //    {
        //        HorizontalOptions = LayoutOptions.Center,
        //        Spacing = 20,
        //        Margin = new Thickness(10)
        //    };

        //    prevButton = new Button
        //    {
        //        Text = "◀ 前へ",
        //        FontSize = 14,
        //        BackgroundColor = Colors.White,
        //        TextColor = Color.FromArgb("#2196F3"),
        //        BorderColor = Color.FromArgb("#2196F3"),
        //        BorderWidth = 1,
        //        CornerRadius = 5,
        //        WidthRequest = 100,
        //        HeightRequest = 40
        //    };
        //    prevButton.Clicked += OnPrevButtonClicked;

        //    pageLabel = new Label
        //    {
        //        FontSize = 16,
        //        TextColor = Colors.Black,
        //        VerticalOptions = LayoutOptions.Center
        //    };

        //    nextButton = new Button
        //    {
        //        Text = "次へ ▶",
        //        FontSize = 14,
        //        BackgroundColor = Colors.White,
        //        TextColor = Color.FromArgb("#2196F3"),
        //        BorderColor = Color.FromArgb("#2196F3"),
        //        BorderWidth = 1,
        //        CornerRadius = 5,
        //        WidthRequest = 100,
        //        HeightRequest = 40
        //    };
        //    nextButton.Clicked += OnNextButtonClicked;

        //    pageControlLayout.Children.Add(prevButton);
        //    pageControlLayout.Children.Add(pageLabel);
        //    pageControlLayout.Children.Add(nextButton);
        //    mainLayout.Children.Add(pageControlLayout);

        //    var totalCount = pages.Sum(page => page.Count);
        //    var statsLabel = new Label
        //    {
        //        Text = $"合計: {totalCount}",
        //        FontSize = 14,
        //        TextColor = Colors.Gray,
        //        HorizontalOptions = LayoutOptions.Center,
        //        Margin = new Thickness(0, 10, 0, 20)
        //    };
        //    mainLayout.Children.Add(statsLabel);

        //    Content = new ScrollView
        //    {
        //        Content = mainLayout
        //    };
        //}

        //private void LoadPage(int pageIndex)
        //{
        //    if (pageIndex < 0 || pageIndex >= totalPages)
        //        return;

        //    currentPage = pageIndex;

        //    cardContainer.Children.Clear();

        //    var currentPageData = pages[pageIndex];

        //    foreach (var item in currentPageData)
        //    {
        //        var card = new ProcessSearchCard(item.Id, item.OrderNumber, item.Status, item.MaterialName, item.MaterialCode, item.StartDate, item.CompletedQuantity, item.RequiredQuantity, item.ProcessInfo, item.Unit);
        //        cardContainer.Children.Add(card);
        //    }

        //    pageLabel.Text = $"{pageIndex + 1} / {totalPages}";

        //    prevButton.IsEnabled = pageIndex > 0;
        //    nextButton.IsEnabled = pageIndex < totalPages - 1;

        //    prevButton.TextColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
        //    nextButton.TextColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
        //    prevButton.BorderColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
        //    nextButton.BorderColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;

        //}

        //private void OnPrevButtonClicked(object sender, EventArgs e)
        //{
        //    if (currentPage > 0)
        //    {
        //        LoadPage(currentPage - 1);
        //    }
        //}

        //private void OnNextButtonClicked(object sender, EventArgs e)
        //{
        //    if (currentPage < totalPages - 1)
        //    {
        //        LoadPage(currentPage + 1);
        //    }
        //}
    }
}
