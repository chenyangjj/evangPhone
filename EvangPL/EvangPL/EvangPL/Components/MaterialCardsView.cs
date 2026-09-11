using EvangPL.Utils;
using EvangPL.Views.ProcessInfo;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.EvangModel;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvangPL.Components
{
    public class MaterialCardView : Border
    {

        public MaterialCardView(int Id, string OrderNumber, string Status, string MaterialName, string MaterialCode,
            string StartDate, int CompletedQuantity, int RequiredQuantity, string ProjectInfo, string Unit, int insCount)
        {
            BuildUI(Id, OrderNumber, Status, MaterialName, MaterialCode, StartDate, CompletedQuantity, RequiredQuantity, ProjectInfo, Unit, insCount);
        }

        private void BuildUI(int Id, string OrderNumber, string Status, string MaterialName, string MaterialCode,
            string StartDate, int CompletedQuantity, int RequiredQuantity, string ProjectInfo, string Unit, int insCount)
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
                Margin = new Thickness(0)
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
                Margin = new Thickness(0, 1, 0, 5)
            };
            var materialLabel = new Label
            {
                Text = "アセンブリ：",
                FontSize = 11,
                TextColor = Colors.Gray,
                VerticalTextAlignment = TextAlignment.Center
            };
            var materialNameLabel = new Label
            {
                FontSize = 11,
                TextColor = Colors.Black,
                Text = MaterialName,
                VerticalTextAlignment = TextAlignment.Center
            };
            var leftParen = new Label
            {
                Text = "(",
                FontSize = 11,
                TextColor = Colors.Gray,
                VerticalTextAlignment = TextAlignment.Center
            };
            var materialCodeLabel = new Label
            {
                FontSize = 11,
                TextColor = Colors.Gray,
                Text = MaterialCode,
                VerticalTextAlignment = TextAlignment.Center
            };
            var rightParen = new Label
            {
                Text = ")",
                FontSize = 11,
                TextColor = Colors.Gray,
                VerticalTextAlignment = TextAlignment.Center
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
                FontSize = 13,
                TextColor = Colors.Gray,
                Text = Unit.ToString()
            };
            quantityLayout.Children.Add(quantityLabel);
            quantityLayout.Children.Add(completedQuantityLabel);
            quantityLayout.Children.Add(slash);
            quantityLayout.Children.Add(requiredQuantityLabel);
            quantityLayout.Children.Add(unit);
            Grid.SetColumn(quantityLayout, 1);
            progressGrid.Children.Add(quantityLayout);

            var projectLayout = new HorizontalStackLayout();
            var projectLabel = new Label
            {
                Text = "工程：",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var projectValue = new Label
            {
                FontSize = 13,
                TextColor = Colors.Black,
                Text = ProjectInfo
            };
            projectLayout.Children.Add(projectLabel);
            projectLayout.Children.Add(projectValue);

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
            orderContent.Children.Add(projectLayout);
            orderContent.Children.Add(progressBar);

            Grid.SetColumnSpan(orderContent, 2);
            mainGrid.Children.Add(orderContent);

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (sender, e) =>
            {

                if (insCount == 0)
                {
                    var currentPage = Application.Current?.MainPage;
                    if (currentPage == null) return;
                    if (currentPage is NavigationPage navPage)
                    {
                        currentPage = navPage.CurrentPage;
                    }
                    await currentPage.DisplayAlert(
                        "",
                        "注文に対する工順情報が取得できません。",
                        "OK"
                    );
                    return;
                }
                ProcessParamInfo paramInfoTo = new ProcessParamInfo(Id, insCount);
                //var request = new RequestData<ProcessParamInfo, EvangJsonModel>("GetProcessInfo");
                //request.Info = paramInfoTo;
                //var resultList = await this.Post<ProcessParamInfo, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                //if (resultList == null || resultList!.SubData[0]!.SubJson == null)      
                await Navigation.PushAsync(new ProcessInfo(paramInfoTo));
            };
            mainGrid.GestureRecognizers.Add(tapGesture);

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
                "計画済み" => Color.FromArgb("#2196F3"),  // blue
                "処理中" => Color.FromArgb("#4CAF50"),   // green
                "リリース済み" => Color.FromArgb("#9E9E9E"), // grep
                _ => Color.FromArgb("#757575")           // default
            };
        }
    }
    
    public class MaterialItem : EvangJsonModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string Status { get; set; }
        public string MaterialName { get; set; }
        public string MaterialCode { get; set; }
        public string StartDate { get; set; }
        public int CompletedQuantity { get; set; }
        public int RequiredQuantity { get; set; }
        public string ProjectInfo { get; set; }
        public string Unit { get; set; }
        public int InstanceCount { get; set; }
        public double Progress => RequiredQuantity > 0 ? CompletedQuantity / (double)RequiredQuantity : 0;

        public MaterialItem(int id, string orderNumber, string status, string materialName, string materialCode, string startDate,
            int completedQuantity, int requiredQuantity, string projectInfo, string unit, int instanceCount)
        {
            Id = id;
            OrderNumber = orderNumber;
            Status = status;
            MaterialName = materialName;
            MaterialCode = materialCode;
            StartDate = startDate;
            CompletedQuantity = completedQuantity;
            RequiredQuantity = requiredQuantity;
            ProjectInfo = projectInfo;
            Unit = unit;
            InstanceCount = instanceCount;
        }
    }

    public class MaterialCardsView : EvangContentView
    {
        private List<MaterialItem>? allMaterials = new List<MaterialItem>();
        private int currentPage = 0;
        private int totalPages = 0;
        private List<List<MaterialItem>>? pages;
        private StackLayout? cardContainer;
        private Button? prevButton;
        private Button? nextButton;
        private Label? pageLabel;

        public MaterialCardsView(List<MaterialItem> allMaterialsval)
        {
            allMaterials = allMaterialsval;
            InitializeData();
            BuildUI();
            LoadPage(0);
        }

        private void InitializeData()
        {
            if (allMaterials == null)
                return;
            pages = new List<List<MaterialItem>>();
            for (int i = 0; i < allMaterials.Count; i += 5)
            {
                var pageItems = allMaterials.Skip(i).Take(5).ToList();
                pages.Add(pageItems);
            }

            totalPages = pages.Count;
        }

        private void BuildUI()
        {
            var mainLayout = new VerticalStackLayout
            {
                Spacing = 0,
                BackgroundColor = Color.FromArgb("#eff0f0")
            };

            var pageControlLayout = new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 20,
                Margin = new Thickness(10)
            };

            prevButton = new Button
            {
                Text = "◀ 前へ",
                FontSize = 14,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#2196F3"),
                BorderColor = Color.FromArgb("#2196F3"),
                BorderWidth = 1,
                CornerRadius = 5,
                WidthRequest = 100,
                HeightRequest = 40
            };
            prevButton.Clicked += OnPrevButtonClicked;

            pageLabel = new Label
            {
                FontSize = 16,
                TextColor = Colors.Black,
                VerticalOptions = LayoutOptions.Center
            };

            nextButton = new Button
            {
                Text = "次へ ▶",
                FontSize = 14,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#2196F3"),
                BorderColor = Color.FromArgb("#2196F3"),
                BorderWidth = 1,
                CornerRadius = 5,
                WidthRequest = 100,
                HeightRequest = 40
            };
            nextButton.Clicked += OnNextButtonClicked;

            pageControlLayout.Add(prevButton);
            pageControlLayout.Add(pageLabel);
            pageControlLayout.Add(nextButton);
            mainLayout.Add(pageControlLayout);

            if (pages == null)
                return;
            var totalCount = pages.Sum(page => page.Count);
            var statsLabel = new Label
            {
                Text = $"合計: {totalCount}",
                FontSize = 14,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 10, 0, 20)
            };
            mainLayout.Children.Add(statsLabel);

            cardContainer = new StackLayout
            {
                Spacing = 0,
                Padding = new Thickness(0),
            };
            mainLayout.Children.Add(cardContainer);

            Content = new ScrollView
            {
                Content = mainLayout
            };
        }

        private void LoadPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= totalPages)
                return;

            currentPage = pageIndex;
            cardContainer!.Clear();

            var currentPageData = pages![pageIndex];
            if (currentPageData.Count == 1)
            {
                cardContainer.HeightRequest = 154;
            }
            else if (currentPageData.Count == 2)
            {
                cardContainer.HeightRequest = 308;
            }
            else if (currentPageData.Count == 3)
            {
                cardContainer.HeightRequest = 462;
            }
            else if (currentPageData.Count == 4)
            {
                cardContainer.HeightRequest = 616;
            }
            else if (currentPageData.Count == 5)
            {
                cardContainer.HeightRequest = 770;
            }

            foreach (var item in currentPageData)
            {
                var card = new MaterialCardView(item.Id, item.OrderNumber, item.Status, item.MaterialName, item.MaterialCode, item.StartDate, item.CompletedQuantity, item.RequiredQuantity, item.ProjectInfo, item.Unit, item.InstanceCount);
                cardContainer.Add(card);
            }

            pageLabel!.Text = $"{pageIndex + 1} / {totalPages}";

            prevButton!.IsEnabled = pageIndex > 0;
            nextButton!.IsEnabled = pageIndex < totalPages - 1;

            prevButton.TextColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            nextButton.TextColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            prevButton.BorderColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            nextButton.BorderColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;

        }

        private void OnPrevButtonClicked(object? sender, EventArgs e)
        {
            if (currentPage > 0)
            {
                LoadPage(currentPage - 1);
            }
        }

        private void OnNextButtonClicked(object? sender, EventArgs e)
        {
            if (currentPage < totalPages - 1)
            {
                LoadPage(currentPage + 1);
            }
        }
    }
}
