using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvangPL.Views.WorkRecord
{
    public class WorkRecord : EvangContentVM
    {
        private string OrderNo = "";  // ワークオーダーNo
        private string ProcessNo = ""; // 工順No
        private string procedureNo = ""; // 手順No
        private string procedureName = ""; // 手順名
        private string Status = "";
        private List<WorkRecordLine> linelist;

        private WorkRecordCardView? ordersCollectionView;

        public WorkRecord(ParamInfo paramInfoTo) : base("strWorkRecord")
        {
            Title = "作業記録一覧";
            BuildUI(paramInfoTo);
        }

        private async void BuildUI(ParamInfo? paramInfoTo)
        {
            if (paramInfoTo == null)
                return;
            await getdata(paramInfoTo);
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

            var HeaderGrid = CreateHeaderGrid();
            mainGrid.Add(HeaderGrid, 0, 0);

            ordersCollectionView = new WorkRecordCardView();
            mainGrid.Add(ordersCollectionView, 0, 1);

            Content = new Border
            {
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#eff1f5"),
                Content = mainGrid
            };
            LoadData();
        }

        public Grid CreateHeaderGrid()
        {
            var headerInfoGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                Padding = new Thickness(0),
                Margin = new Thickness(0, 5, 0, 5)
            };

            var titleLayout = new VerticalStackLayout();
            var titleGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 0,
                Padding = new Thickness(2, 7, 2, 7),
                Margin = new Thickness(10, 0, 10, 0),
                BackgroundColor = Color.FromArgb("#e8e8e8"),
            };

            var WorkOrderLabel = new Label
            {
                Text = OrderNo,
                FontSize = 13,
                TextColor = Colors.Gray
            };
            Grid.SetColumn(WorkOrderLabel, 0);
            titleGrid.Children.Add(WorkOrderLabel);

            var dashLabel = new Label
            {
                Text = " - ",
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center,
                TextColor = Colors.Gray
            };
            Grid.SetColumn(dashLabel, 1);
            titleGrid.Children.Add(dashLabel);

            var ProcessLabel = new Label
            {
                Text = ProcessNo,
                FontSize = 13,
                TextColor = Colors.Gray
            };
            Grid.SetColumn(ProcessLabel, 2);
            titleGrid.Children.Add(ProcessLabel);

            var dash2Label = new Label
            {
                Text = " - ",
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center,
                TextColor = Colors.Gray
            };
            Grid.SetColumn(dash2Label, 3);
            titleGrid.Children.Add(dash2Label);

            var procedureLabel = new Label
            {
                Text = procedureNo,
                FontSize = 13,
                TextColor = Colors.Gray
            };
            Grid.SetColumn(procedureLabel, 4);
            titleGrid.Children.Add(procedureLabel);

            titleLayout.Children.Add(titleGrid);
            Grid.SetRow(titleLayout, 0);
            headerInfoGrid.Children.Add(titleLayout);

            var procedureStack = new VerticalStackLayout
            {
                Spacing = 0,
                Margin = new Thickness(10, 10, 10, 0),
                Padding = new Thickness(0)
            };

            var procedureCard = new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#e0e0e0"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 5 },
                Padding = new Thickness(10, 10),
                Margin = new Thickness(0),
                MinimumHeightRequest = 50,
            };

            var procedureLayout = new VerticalStackLayout();
            var procedureGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 0,
                Margin = new Thickness(0)
            };

            var proNameLabel = new Label
            {
                Text = procedureName,
                FontSize = 13,
                TextColor = Colors.Gray,
                VerticalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(proNameLabel, 0);
            procedureGrid.Children.Add(proNameLabel);

            var statusBorder = new Border
            {
                Stroke = Colors.Transparent,
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#2196F3"),
                StrokeShape = new RoundRectangle { CornerRadius = 5 },
                Padding = new Thickness(8, 5),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                MinimumWidthRequest = 60,
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

            // 4. 先设置文本再获取颜色
            statusBorder.Content = statusLabel;
            statusBorder.BackgroundColor = GetStatusColor(Status);

            Grid.SetColumn(statusBorder, 1);
            procedureGrid.Children.Add(statusBorder);

            procedureLayout.Children.Add(procedureGrid);
            procedureCard.Content = procedureLayout;
            procedureStack.Children.Add(procedureCard);
            Grid.SetRow(procedureStack, 1);
            headerInfoGrid.Children.Add(procedureStack);

            return headerInfoGrid;
        }

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "計画済み" => Color.FromArgb("#2196F3"),  // blue
                "進行中" => Color.FromArgb("#4CAF50"),   // green
                "完了" => Color.FromArgb("#9E9E9E"), // gray
                _ => Color.FromArgb("#757575")           // default
            };
        }

        private void LoadData()
        {
            var records = new List<WorkRecordEntry>();
            for (int i = 0; i < linelist.Count; i++)
            {
                WorkRecordEntry workrecord = new WorkRecordEntry { ValueA = linelist[i].startDate, ValueB = linelist[i].userName, ValueC = linelist[i].itemName, ValueD = linelist[i].curQty };
                records.Add(workrecord);
            }
            //var records = new List<WorkRecordEntry>
            //{
            //    new WorkRecordEntry { ValueA = "AAAA", ValueB = "25", ValueC = "操作CCCC" , ValueD = "100"},
            //    new WorkRecordEntry { ValueA = "操作2024-03-01", ValueB = "BBB", ValueC = "CCC" , ValueD = "100"},
            //    new WorkRecordEntry { ValueA = "2024-03-01", ValueB = "操作25", ValueC = "100" , ValueD = "100"},
            //    new WorkRecordEntry { ValueA = "2024-03-01", ValueB = "25", ValueC = "100" , ValueD = "DDD"},
            //    new WorkRecordEntry { ValueA = "2024-03-01", ValueB = "25", ValueC = "100" , ValueD = "100"},
            //    new WorkRecordEntry { ValueA = "2024-03-01", ValueB = "25", ValueC = "100" , ValueD = "100"},
            //    new WorkRecordEntry { ValueA = "2024-03-01", ValueB = "25", ValueC = "操作100" , ValueD = "100"},
            //    new WorkRecordEntry { ValueA = "2024-03-01", ValueB = "操作25", ValueC = "100" , ValueD = "100"},
            //    new WorkRecordEntry { ValueA = "2024-03-01", ValueB = "25", ValueC = "100" , ValueD = "100"},
            //    new WorkRecordEntry { ValueA = "2024-03-01", ValueB = "25", ValueC = "100" , ValueD = "100"},
            //    new WorkRecordEntry { ValueA = "2024-03-01", ValueB = "25", ValueC = "100" , ValueD = "100"},
            //};

            if (ordersCollectionView != null)
            {
                ordersCollectionView.ItemsSource = records;
            }
        }

        private async Task getdata(ParamInfo paramInfoTo)
        {
            var processinfoTo = paramInfoTo;
            var request = new RequestData<ParamInfo, EvangJsonModel>("GetWorkRecord");
            request.Info = processinfoTo;
            var resultList = await this.Post<ParamInfo, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
            if (resultList == null || resultList.SubData == null)
                return;

            foreach (var item in resultList.SubData)
            {
                switch (item.SubName)
                {
                    case "LF_WORKRECORDHD":
                        if (item == null || item.SubJson == null)
                            return;
                        var hdlist = BaseUtils.JsonToClass<List<WorkRecordHd>>(item.SubJson);
                        if (hdlist == null || hdlist.Count == 0)
                            return;
                        OrderNo = hdlist[0].orderNo;
                        ProcessNo = hdlist[0].workNo;
                        procedureNo = hdlist[0].stepNo;
                        procedureName = hdlist[0].stepName;
                        Status = hdlist[0].statusName;
                        break;
                    case "LF_WORKRECORDLINE":
                        if (item == null || item.SubJson == null)
                            return;
                        linelist = BaseUtils.JsonToClass<List<WorkRecordLine>>(item.SubJson);
                        break;
                }
            }
        }
    }

    public class WorkRecordEntry : EvangJsonModel
    {
        public string ValueA { get; set; }
        public string ValueB { get; set; }
        public string ValueC { get; set; }
        public string ValueD { get; set; }
    }
}