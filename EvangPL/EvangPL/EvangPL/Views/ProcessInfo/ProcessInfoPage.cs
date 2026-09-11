using EvangPL.Components;
using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using System.Text.Json;
using ProcessParamInfo = EvangPL.Utils.ProcessParamInfo;

namespace EvangPL.Views.ProcessInfo
{
    public class ProcessInfo : EvangContentVM
    {
        private Grid? pageHeaderInfo;
        private string headId;
        private string headItemCode;
        private string headItemName;
        private int odrcommitted;
        private int odrqty;
        private ProcessParamInfo paramInfoToNext;
        public ProcessInfo(ProcessParamInfo paramInfoTo) : base("strProcessInfo")
        {
            paramInfoToNext = paramInfoTo;
            BuildUI();
        }
        private async void BuildUI()
        {
            var request = new RequestData<ProcessParamInfo, EvangJsonModel>("GetProcessInfo");
            request.Info = paramInfoToNext;
            var resultList = await this.Post<ProcessParamInfo, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
            if (resultList == null || resultList!.SubData[0]!.SubJson == null)
                return;
            var dbJson = resultList!.SubData[0]!.SubJson;

            var allProcessInfoItem = new List<ProcessInfoItem>();
            var headerInfo = new List<ProcessInfoItem>();
            try
            {
                var dynamicList = BaseUtils.JsonToClass<List<dynamic>>(dbJson);
                foreach (var item in dynamicList)
                {
                    headId = "#" + GetJsonStringValue(item, "OrderNumber").ToString() ?? string.Empty;
                    headItemCode = GetJsonStringValue(item, "MaterialCode").ToString() ?? string.Empty;
                    headItemName = GetJsonStringValue(item, "MaterialName").ToString() ?? string.Empty;
                    odrqty = GetJsonIntValue(item, "CompletedQuantity");
                    odrcommitted = GetJsonIntValue(item, "RequiredQuantity");


                    int totalSteps = GetJsonIntValue(item, "TotalSteps");
                    int completedQuantity = GetJsonIntValue(item, "CompletedQuantity");
                    int requiredQuantity = GetJsonIntValue(item, "RequiredQuantity");
                    int actualQuantity = GetJsonIntValue(item, "ActualQuantity");
                    int badQuantity = GetJsonIntValue(item, "BadQuantity");
                    string memoText = GetJsonStringValue(item, "MemoText").ToString() ?? string.Empty;

                    var processInfoItem = new ProcessInfoItem(
                        GetJsonIntValue(item, "Id"),
                        GetJsonStringValue(item, "OrderNumber").ToString() ?? string.Empty,
                        GetJsonStringValue(item, "Status").ToString() ?? string.Empty,
                        GetJsonStringValue(item, "MaterialName").ToString() ?? string.Empty,
                        GetJsonStringValue(item, "MaterialCode").ToString() ?? string.Empty,
                        GetJsonStringValue(item, "ProcessName").ToString() ?? string.Empty,
                        totalSteps,
                        completedQuantity,
                        actualQuantity,
                        GetJsonStringValue(item, "ProductionLine").ToString() ?? string.Empty,
                        memoText,
                        actualQuantity,
                        badQuantity
                    );
                    allProcessInfoItem.Add(processInfoItem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON解析失败：{ex.Message}");
                return;
            }
            WorkOrderDetailCardView ordersCollectionView = new WorkOrderDetailCardView(allProcessInfoItem);

            pageHeaderInfo = BuildHeader(headId, headItemName, headItemCode, odrcommitted, odrqty, "");
            var mainGrid = new Grid
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
            mainGrid.Add(pageHeaderInfo, 0, 0);
            var processContainer = new StackLayout
            {
                Spacing = 5,
                Padding = new Thickness(10),
            };

            // 添加工单标题
            processContainer.Children.Add(ordersCollectionView);
            var processScrollView = new ScrollView
            {
                Content = processContainer,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always,
            };
            mainGrid.Add(processScrollView, 0, 1);
            Content = mainGrid;
            //Content = new ScrollView
            //{
            //    Content = new VerticalStackLayout
            //    {
            //        Spacing = 8,
            //        Padding = new Thickness(5),
            //        BackgroundColor = Color.FromArgb("#eff0f0"),
            //        Children =
            //        {
            //            pageHeaderInfo,
            //            ordersCollectionView
            //        }
            //    }
            //};
        }
        private Grid BuildHeader(string OrderNumber, string MaterialName, string MaterialCode, int CompletedQuantity, int RequiredQuantity, string UnitText)
        {
            var headerInfoGrid = new Grid
            {
                //BackgroundColor = Color.FromArgb("#eff0f0"),
                Padding = new Thickness(2)
            };

            var headerLayout = new VerticalStackLayout
            {
                Spacing = 3
            };

            var orderNumberLayout = new HorizontalStackLayout();
            var orderNumberLabel = new Label
            {
                Text = "ワーク・オーダー ",
                FontSize = 15,
                TextColor = Color.FromArgb("#1f3854"),
                FontAttributes = FontAttributes.Bold,
            };
            var orderNumberValue = new Label
            {
                Text = OrderNumber,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1f3854")
            };
            //TODO
            //orderNumberValue.SetBinding(Label.TextProperty, "OrderNumber");
            orderNumberLayout.Children.Add(orderNumberLabel);
            orderNumberLayout.Children.Add(orderNumberValue);

            //Grid.SetColumn(orderNumberLayout, 0);
            headerLayout.Children.Add(orderNumberLayout);

            var materialLayout = new HorizontalStackLayout
            {
                Margin = new Thickness(0)
            };
            var materialLabel = new Label
            {
                Text = "アセンブリ:",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var materialName = new Label
            {
                Text = MaterialName,
                FontSize = 13,
                TextColor = Colors.Black
            };
            //materialName.SetBinding(Label.TextProperty, "MaterialName");
            var leftParen = new Label
            {
                Text = "(",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var materialCode = new Label
            {
                Text = MaterialCode,
                FontSize = 13,
                TextColor = Colors.Gray
            };
            //materialCode.SetBinding(Label.TextProperty, "MaterialCode");
            var rightParen = new Label
            {
                Text = ")",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            materialLayout.Children.Add(materialLabel);
            materialLayout.Children.Add(materialName);
            materialLayout.Children.Add(leftParen);
            materialLayout.Children.Add(materialCode);
            materialLayout.Children.Add(rightParen);

            headerLayout.Children.Add(materialLayout);

            var quantityLayout = new HorizontalStackLayout();
            var quantityLabel = new Label
            {
                Text = "数量（実／予）:",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var requiredQuantity = new Label
            {
                Text = RequiredQuantity.ToString(),
                FontSize = 13,
                TextColor = Colors.Black
            };
            //completedQuantity.SetBinding(Label.TextProperty, "CompletedQuantity");
            var slash = new Label
            {
                Text = "/",
                FontSize = 13,
                TextColor = Colors.Gray
            };
            var completedQuantity = new Label
            {
                Text = CompletedQuantity.ToString(),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#4CAF50")
            };
            //requiredQuantity.SetBinding(Label.TextProperty, "RequiredQuantity");
            var unit = new Label
            {
                Text = UnitText,
                FontSize = 13,
                TextColor = Colors.Gray
            };
            //unit.SetBinding(Label.TextProperty, "UnitText");

            quantityLayout.Children.Add(quantityLabel);
            quantityLayout.Children.Add(completedQuantity);
            quantityLayout.Children.Add(slash);
            quantityLayout.Children.Add(requiredQuantity);
            quantityLayout.Children.Add(unit);
            headerLayout.Children.Add(quantityLayout);


            var subtitleLayout = new HorizontalStackLayout
            {
                Margin = new Thickness(0, 8, 0, 1),
            };
            var titleLabel = new Label
            {
                Text = "工順一覧",
                FontSize = 15,
                TextColor = Color.FromArgb("#1f3854"),
                FontAttributes = FontAttributes.Bold,
            };
            subtitleLayout.Children.Add(titleLabel);
            headerLayout.Children.Add(subtitleLayout);
            //var separatorLayout = new HorizontalStackLayout();

            var separatorLine = new BoxView
            {
                HeightRequest = 2,
                //BackgroundColor = Color.FromArgb("#828282"),
                BackgroundColor = Colors.LightGray,
            };
            //separatorLayout.Children.Add(separatorLine);
            headerLayout.Children.Add(separatorLine);


            headerInfoGrid.Children.Add(headerLayout);
            return headerInfoGrid;
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

    }

}
