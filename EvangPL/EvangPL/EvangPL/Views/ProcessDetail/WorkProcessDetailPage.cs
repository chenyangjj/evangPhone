using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Shapes;

namespace EvangPL.Views.ProcessDetail
{
    public class ProcessDetail : EvangContentVM
    {
        private VerticalStackLayout? mainLayout;
        private Grid? bottomNavigationGrid;
        private List<StepInstanceInfo>? pages;
        private StepInstanceInfo? stepInstanceInfoTo;
        private Button? prevButton;
        private Button? nextButton;
        private int currentPage = 0;
        private int totalPages = 0;


        public ProcessDetail(ParamInfo paramInfoTo) : base("strProcessDetail")
        {
            //ParamInfo paramInfoTo = new ParamInfo("");
            BuildUI(paramInfoTo, true);
        }

        private async void BuildUI(ParamInfo? paramInfoTo, bool queryFlag)
        {
            if (queryFlag)
            {
                if (paramInfoTo == null)
                    return;
                await getdata(paramInfoTo);
                Title = stepInstanceInfoTo!.operationId;
            }
            // 创建主滚动视图
            var scrollView = new ScrollView();
            // 创建主垂直布局
            mainLayout = new VerticalStackLayout
            {
                Spacing = 15,
                Padding = new Thickness(20),
                BackgroundColor = Color.FromArgb("#f5f5f5")
            };

            // 添加标题部分
            AddTitleSection();

            // 添加拼接信息
            AddJoinInfo();

            // 添加分隔线
            AddSeparator();

            // 添加步骤列表
            AddStepsList();

            // 添加质量检查部分
            AddQualitySection();

            // 添加操作按钮
            AddActionButtons();

            // 添加底部导航
            //AddBottomNavigation();

            scrollView.Content = mainLayout;
            Content = scrollView;
        }

        private void AddTitleSection()
        {
            var orderNumberRow = new Grid
            {
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
            };
            // 主标题
            var mainTitleLabel = new Label
            {
                Text = "",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Start
            };
            mainTitleLabel.Text = stepInstanceInfoTo!.stepSeq + ":" + stepInstanceInfoTo!.stepName;

            Grid.SetColumn(mainTitleLabel, 0);
            orderNumberRow.Children.Add(mainTitleLabel);

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
                Text = "",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            statusLabel.Text = stepInstanceInfoTo!.statusName;
            var statusColor = GetStatusColor(stepInstanceInfoTo!.statusName);
            statusBorder.BackgroundColor = statusColor;
            statusBorder.Content = statusLabel;
            Grid.SetColumn(statusBorder, 1);
            orderNumberRow.Children.Add(statusBorder);
            mainLayout.Children.Add(orderNumberRow);

            // 物料信息
            var materialLabel = new Label
            {
                Text = "",
                FontSize = 14,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Start
            };
            materialLabel.Text = "投入品目／数量：" + stepInstanceInfoTo!.inputItem + "／" + stepInstanceInfoTo!.inputItemQty;
            mainLayout.Children.Add(materialLabel);
        }

        private void AddJoinInfo()
        {
            var joinLabel = new Label
            {
                Text = "",
                FontSize = 14,
                TextColor = Colors.Gray,
                VerticalOptions = LayoutOptions.Center
            };
            joinLabel.Text = "産出品目／数量：" + stepInstanceInfoTo!.outputItem + "／" + stepInstanceInfoTo!.outputItemQty;
            mainLayout.Children.Add(joinLabel);
        }

        private void AddSeparator()
        {
            var separator = new BoxView
            {
                HeightRequest = 3,
                BackgroundColor = Color.FromArgb("#e0e0e0"),
                Margin = new Thickness(0, 1, 0, 1)
            };
            mainLayout.Children.Add(separator);
            //var image = new Image
            //{
            //    Source = ImageSource.FromFile("test.png"),
            //    WidthRequest = 100,
            //    HeightRequest = 100,
            //    Aspect = Aspect.AspectFit
            //};
            //mainLayout.Children.Add(image);
        }

        private void AddStepsList()
        {
            var stepsFrame = new Frame
            {
                CornerRadius = 8,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#e0e0e0"),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 2)
            };

            var stepsLayout = new VerticalStackLayout
            {
                Spacing = 2
            };

            // 步骤1
            var step1Layout = new HorizontalStackLayout
            {
                Spacing = 2
            };
            var step1Text = new Label
            {
                Text = "",
                FontSize = 12,
                TextColor = Colors.Black,
                LineBreakMode = LineBreakMode.WordWrap
            };
            step1Text.Text = stepInstanceInfoTo!.memo;
            step1Layout.Children.Add(step1Text);
            stepsLayout.Children.Add(step1Layout);

            stepsFrame.Content = stepsLayout;
            mainLayout.Children.Add(stepsFrame);
            bool cbflag = false;
            if (stepInstanceInfoTo!.needInspection == "T")
            {
                cbflag = true;
            }
            var cb = new CheckBoxWithLabel("検査要否", cbflag);
            mainLayout.Children.Add(cb);
        }

        private void AddQualitySection()
        {
            var qualityFrame = new Frame
            {
                CornerRadius = 8,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#e0e0e0"),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var qualityLayout = new VerticalStackLayout
            {
                Spacing = 10
            };

            // 质量表格
            var qualityGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnSpacing = 10,
                RowSpacing = 8
            };

            // 第二行 - 合并单元格
            var measurementLabel = new Label
            {
                Text = "",
                FontSize = 14,
                TextColor = Colors.Black
            };
            measurementLabel.Text = stepInstanceInfoTo!.tool;
            Grid.SetColumn(measurementLabel, 0);
            Grid.SetRow(measurementLabel, 0);
            Grid.SetColumnSpan(measurementLabel, 4);
            qualityGrid.Children.Add(measurementLabel);

            // 第三行 - 合并单元格
            var remarkLabel = new Label
            {
                Text = "",
                FontSize = 14,
                TextColor = Colors.Gray,
                LineBreakMode = LineBreakMode.WordWrap
            };
            remarkLabel.Text = stepInstanceInfoTo!.skilltype;
            Grid.SetColumn(remarkLabel, 0);
            Grid.SetRow(remarkLabel, 1);
            Grid.SetColumnSpan(remarkLabel, 4);
            qualityGrid.Children.Add(remarkLabel);

            qualityLayout.Children.Add(qualityGrid);

            // 目标工时
            var timeLayout = new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var timeLabel = new Label
            {
                Text = "目標工数：",
                FontSize = 14,
                TextColor = Colors.Gray
            };

            var timeValue = new Label
            {
                Text = "",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2196F3")
            };
            timeValue.Text = stepInstanceInfoTo!.stdtime;

            timeLayout.Children.Add(timeLabel);
            timeLayout.Children.Add(timeValue);
            qualityLayout.Children.Add(timeLayout);

            qualityFrame.Content = qualityLayout;
            mainLayout.Children.Add(qualityFrame);
        }

        private void AddActionButtons()
        {
            //var buttonsFrame = new Frame
            //{
            //    CornerRadius = 8,
            //    BackgroundColor = Colors.White,
            //    BorderColor = Color.FromArgb("#e0e0e0"),
            //    Padding = new Thickness(15),
            //    Margin = new Thickness(0, 0, 0, 15)
            //};

            //var buttonsLayout = new VerticalStackLayout
            //{
            //    Spacing = 10
            //};

            //// 按钮容器
            //var buttonContainer = new HorizontalStackLayout
            //{
            //    HorizontalOptions = LayoutOptions.Center,
            //    Spacing = 20
            //};

            var buttonsFrame = new HorizontalStackLayout
            {
                Spacing = 5,
                Padding = new Thickness(10),
                BackgroundColor = Color.FromArgb("#f5f5f5")
            };

            prevButton = new Button
            {
                Text = "◀ 前へ",
                FontSize = 10,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#2196F3"),
                BorderColor = Color.FromArgb("#2196F3"),
                BorderWidth = 1,
                WidthRequest = 70,
                HeightRequest = 20,
                CornerRadius = 5
            };
            prevButton.Clicked += OnPrevButtonClicked;

            nextButton = new Button
            {
                Text = "次へ ▶",
                FontSize = 10,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#2196F3"),
                BorderColor = Color.FromArgb("#2196F3"),
                BorderWidth = 1,
                WidthRequest = 70,
                HeightRequest = 20,

                CornerRadius = 5
            };
            nextButton.Clicked += OnNextButtonClicked;

            prevButton!.IsEnabled = currentPage > 0;
            nextButton!.IsEnabled = currentPage < totalPages - 1;

            prevButton.TextColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            nextButton.TextColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            prevButton.BorderColor = prevButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;
            nextButton.BorderColor = nextButton.IsEnabled ? Color.FromArgb("#2196F3") : Colors.Gray;

            var workresume = new Button
            {
                Text = "作業履歴",
                FontSize = 10,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#2196F3"),
                BorderColor = Color.FromArgb("#2196F3"),
                BorderWidth = 1,
                WidthRequest = 70,
                HeightRequest = 20,
                CornerRadius = 5
            };
            workresume.Clicked += OnResumeButtonClicked;

            var workrkbn = new Button
            {
                Text = "",
                FontSize = 10,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#2196F3"),
                BorderColor = Color.FromArgb("#2196F3"),
                BorderWidth = 1,
                WidthRequest = 70,
                HeightRequest = 20,
                CornerRadius = 5
            };
            workrkbn.Text = stepInstanceInfoTo!.stepType;
            workrkbn.Clicked += OnKbnButtonClicked;

            buttonsFrame.Children.Add(prevButton);
            buttonsFrame.Children.Add(nextButton);
            buttonsFrame.Children.Add(workresume);
            buttonsFrame.Children.Add(workrkbn);
            mainLayout.Children.Add(buttonsFrame);
        }

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "未着手" => Color.FromArgb("#2196F3"),  // blue
                "進行中" => Color.FromArgb("#4CAF50"),  // green
                "完了" => Color.FromArgb("#9E9E9E"), // grep
            };
        }

        private async Task getdata(ParamInfo paramInfoTo)
        {
            var processinfoTo = paramInfoTo;
            var request = new RequestData<ParamInfo, EvangJsonModel>("GetStepInstance");
            request.Info = processinfoTo;
            var resultList = await this.Post<ParamInfo, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
            if (resultList == null || resultList.SubData == null)
                return;

            foreach (var item in resultList.SubData)
            {
                switch (item.SubName)
                {
                    case "LF_PROINS":
                        if (item == null || item.SubJson == null)
                            return;
                        var steplist = BaseUtils.JsonToClass<List<StepInstanceInfo>>(item.SubJson);
                        if (steplist == null || steplist.Count == 0)
                            return;
                        pages = steplist;
                        totalPages = pages.Count;
                        bool loopflag = false;
                        for (int i = 0; i < pages.Count; i++)
                        {
                            var listone = pages[i];
                            if (listone.statusName == "未着手" || listone.statusName == "進行中")
                            {
                                loopflag = true;
                                currentPage = i;
                                stepInstanceInfoTo = pages[i];
                                break;
                            }
                        }
                        if (!loopflag)
                        {
                            currentPage = 0;
                            stepInstanceInfoTo = pages[0];
                        }
                        break;
                }
            }
        }

        private void LoadPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= totalPages)
                return;
            currentPage = pageIndex;
            stepInstanceInfoTo = pages![pageIndex];
            BuildUI(null, false);
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

        private async void OnKbnButtonClicked(object? sender, EventArgs e)
        {
            ParamInfo paramInfoTo = new ParamInfo(stepInstanceInfoTo!.strId);
            switch (stepInstanceInfoTo!.stepType)
            {
                case "CHECK 確認":
                    await Navigation.PushAsync(new PreWorkCheck.PreWorkCheck(paramInfoTo));
                    break;
                case "HTR 加温":
                    await Navigation.PushAsync(new Heating.Heating(paramInfoTo));
                    break;
                case "DFR 解凍":
                    await Navigation.PushAsync(new Thaw.Thaw(paramInfoTo));
                    break;
                case "INP 投入":
                    await Navigation.PushAsync(new Investment.Investment(paramInfoTo));
                    break;
                case "OPR 出来高登録":
                    await Navigation.PushAsync(new OutHigh.OutHigh(paramInfoTo));
                    break;
            }
        }

        private async void OnResumeButtonClicked(object? sender, EventArgs e)
        {
            ParamInfo paramInfoTo = new ParamInfo(stepInstanceInfoTo!.strId);
            await Navigation.PushAsync(new WorkRecord.WorkRecord(paramInfoTo));
        }
    }

    public class CheckBoxWithLabel : HorizontalStackLayout
    {
        private readonly Frame _checkBox;
        private readonly Label _checkMark;
        private readonly Label _textLabel;

        public CheckBoxWithLabel(string text, bool IsChecked)
        {
            Spacing = 2;

            // 复选框
            _checkBox = new Frame
            {
                WidthRequest = 24,
                HeightRequest = 24,
                CornerRadius = 4,
                Padding = 0,
                HasShadow = false,
                BorderColor = Colors.Gray,
                BackgroundColor = Colors.White
            };

            // 创建对勾
            _checkMark = new Label
            {
                Text = "✓",
                FontSize = 16,
                TextColor = Colors.Blue,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false
            };
            _checkMark.IsVisible = IsChecked;
            _checkBox.Content = _checkMark;

            // 文本
            _textLabel = new Label
            {
                Text = text,
                TextColor = Colors.Green,
                VerticalOptions = LayoutOptions.Center
            };

            Children.Add(_checkBox);
            Children.Add(_textLabel);
        }
    }

}
