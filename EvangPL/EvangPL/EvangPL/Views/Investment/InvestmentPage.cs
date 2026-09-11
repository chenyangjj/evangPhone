using EvangPL.Utils;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Layouts;

namespace EvangPL.Views.Investment
{
    public class Investment : EvangContentVM
    {
        private Label? statusLabel;
        private Entry? standardTimeEntry;
        private Entry? toolsEntry;
        Entry? equipmentEntry;
        private Editor? workContentEditor;
        private CheckBox? okCheckBox;
        private CheckBox? ngCheckBox;
        private CheckBox? endCheckBox;
        private Entry? startTimeEntry;
        private Entry? endTimeEntry;
        private Entry? durationEntry;
        private Entry? genhinticketEntry;
        private Entry? lotnoEntry;
        private Entry? quantityEntry;
        private Entry? insatuEntry;
        private Editor? commentEditor;
        private Label? bottomTimeLabel;
        private Button? timeMeasureButton;
        private Button? reportButton;
        private Button? updateButton;
        private InvestmentInfo? investmentInfoTo;
        private string? strcomment;
        private string? strprocessid;

        public Investment(ParamInfo paramInfoTo) : base("strInvestment")
        {
            BackgroundColor = Color.FromArgb("#F8F9FA");
            Title = "投入";
            strprocessid = paramInfoTo.processId;
            BuildUI(paramInfoTo);
        }

        private async void BuildUI(ParamInfo paramInfoTo)
        {
            await getworkdata(paramInfoTo);
            InitializeComponents();
            SetupEventHandlers();
        }

        private void InitializeComponents()
        {
            // 主容器（添加阴影效果）
            var mainContainer = new Frame
            {
                CornerRadius = 10,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#E0E0E0"),
                HasShadow = true,
                Padding = 0,
                Margin = new Thickness(15, 10, 15, 15),
                Content = CreateMainContent()
            };

            // 主布局
            var mainLayout = new ScrollView
            {
                Content = new StackLayout
                {
                    Spacing = 0,
                    Children =
                    {
                        mainContainer
                    }
                }
            };

            Content = mainLayout;
        }

        private async Task getworkdata(ParamInfo paramInfoTo)
        {
            var processinfoTo = paramInfoTo;
            var request = new RequestData<ParamInfo, EvangJsonModel>("GetPreCheck");
            request.Info = processinfoTo;
            var resultList = await this.Post<ParamInfo, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
            if (resultList == null || resultList.SubData == null)
                return;

            foreach (var item in resultList.SubData)
            {
                switch (item.SubName)
                {
                    case "LF_PRECHECK":
                        if (item == null || item.SubJson == null)
                            return;
                        var steplist = BaseUtils.JsonToClass<List<InvestmentInfo>>(item.SubJson);
                        if (steplist == null || steplist.Count == 0)
                            return;
                        investmentInfoTo = steplist[0];
                        break;
                }
            }
        }

        private StackLayout CreateMainContent()
        {
            // 手順信息（带状态徽章）
            var procedureInfoCard = new Frame
            {
                CornerRadius = 8,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#E0E0E0"),
                Padding = new Thickness(15, 10),
                Margin = new Thickness(10, 5),
                Content = new StackLayout
                {
                    Children =
                    {
                        new StackLayout
                        {
                            Orientation = StackOrientation.Horizontal,
                            Children =
                            {
                                new Label
                                {
                                    Text = $"{investmentInfoTo!.stepSeq + ":" + investmentInfoTo!.stepName}",
                                    FontSize = 14,
                                    TextColor = Color.FromArgb("#34495E"),
                                    HorizontalOptions = LayoutOptions.StartAndExpand,
                                    VerticalOptions = LayoutOptions.Center
                                },
                            }
                        }
                    }
                }
            };
            // 实施担当者
            var implementerCard = new Frame
            {
                CornerRadius = 8,
                BackgroundColor = Color.FromArgb("#FFF8E1"),
                BorderColor = Color.FromArgb("#FFECB3"),
                Padding = new Thickness(15, 10),
                Margin = new Thickness(10, 5),
                Content = new StackLayout
                {
                    Children =
                    {
                        new Label
                        {
                            Text = $"実施担当者：{investmentInfoTo!.operatorUser}",
                            FontSize = 14,
                            TextColor = Color.FromArgb("#E65100"),
                            FontAttributes = FontAttributes.Bold
                        }
                    }
                }
            };

            // 输入区域容器
            var inputContainer = new StackLayout
            {
                Spacing = 5,
                Margin = new Thickness(10, 5),
                Children =
                {
                    CreateInputField("標準時間：", "標準時間（分）", out standardTimeEntry, Keyboard.Numeric),
                    CreateInputField("使用工具：", "使用工具", out toolsEntry),
                    CreateInputField("設備：", "設備", out equipmentEntry)
                }
            };
            standardTimeEntry.Text = investmentInfoTo!.times + "分";
            toolsEntry.Text = investmentInfoTo!.tool;
            equipmentEntry.Text = investmentInfoTo!.equipment;
            // 作业内容（只读）
            var workContentCard = new Frame
            {
                CornerRadius = 8,
                BackgroundColor = Color.FromArgb("#F5F5F5"),
                BorderColor = Color.FromArgb("#E0E0E0"),
                Padding = new Thickness(15, 10),
                Margin = new Thickness(10, 10),
                Content = new StackLayout
                {
                    Children =
                    {
                        new Label
                        {
                            Text = "作業内容：",
                            FontSize = 14,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#2C3E50"),
                            Margin = new Thickness(0, 0, 0, 5)
                        },
                        new Label
                        {
                            Text = $"{investmentInfoTo!.memo}",
                            FontSize = 13,
                            TextColor = Color.FromArgb("#546E7A"),
                            LineBreakMode = LineBreakMode.WordWrap
                        }
                    }
                }
            };

            // OK/NG选择（美化版）
            var okNgCard = new Frame
            {
                CornerRadius = 8,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#E0E0E0"),
                Padding = new Thickness(15, 10),
                Margin = new Thickness(10, 5),
                Content = CreateOkNgSelection()
            };

            var recordCard = new Frame
            {
                CornerRadius = 8,
                BackgroundColor = Color.FromArgb("#E8F5E9"),
                BorderColor = Color.FromArgb("#C8E6C9"),
                Padding = new Thickness(15, 10),
                Margin = new Thickness(10, 10),
                Content = CreateRecordCardSection()
            };

            // 作业时间实绩（美化卡片）
            var timeRecordCard = new Frame
            {
                CornerRadius = 8,
                BackgroundColor = Color.FromArgb("#E8F5E9"),
                BorderColor = Color.FromArgb("#C8E6C9"),
                Padding = new Thickness(15, 10),
                Margin = new Thickness(10, 10),
                Content = CreateTimeRecordSection()
            };

            var endCard = new Frame
            {
                CornerRadius = 8,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#E0E0E0"),
                Padding = new Thickness(15, 10),
                Margin = new Thickness(10, 0, 10, 5),
                Content = CreateEndSelection()
            };

            // 操作按钮（水平排列）
            var buttonContainer = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 15,
                Margin = new Thickness(0, 15),
                Children =
                {
                    CreateStyledButton("開始", Color.FromArgb("#3498DB"), Color.FromArgb("#2980B9"), out timeMeasureButton),
                    CreateStyledButton("レポート", Color.FromArgb("#27AE60"), Color.FromArgb("#229954"), out reportButton),
                    CreateStyledButton("更新", Color.FromArgb("#F39C12"), Color.FromArgb("#D68910"), out updateButton)
                }
            };

            // 主内容布局
            return new StackLayout
            {
                Spacing = 0,
                Padding = new Thickness(0, 10),
                Children =
                {
                    procedureInfoCard,
                    implementerCard,
                    inputContainer,
                    workContentCard,
                    okNgCard,
                    recordCard,
                    timeRecordCard,
                    endCard,
                    buttonContainer
                }
            };
        }

        private StackLayout CreateEndSelection()
        {
            var okLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.End,
                Spacing = 10
            };

            endCheckBox = new CheckBox
            {
                Color = Color.FromArgb("#27AE60"),
                VerticalOptions = LayoutOptions.Center,
                Scale = 1.2
            };

            var endLabel = new Label
            {
                Text = "完了",
                FontSize = 16,
                TextColor = Color.FromArgb("#27AE60"),
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };

            okLayout.Children.Add(endCheckBox);
            okLayout.Children.Add(endLabel);
            return okLayout;
        }

        private StackLayout CreateInputField(string label, string placeholder, out Entry entry, Keyboard keyboard = null)
        {
            var layout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.Center
            };

            var labelView = new Label
            {
                Text = label,
                FontSize = 14,
                TextColor = Color.FromArgb("#2C3E50"),
                WidthRequest = 80,
                HeightRequest = 20,
                Margin = new Thickness(0, -5, 0, -5),
                VerticalOptions = LayoutOptions.Center
            };

            entry = new Entry
            {
                Placeholder = placeholder,
                PlaceholderColor = Color.FromArgb("#95A5A6"),
                TextColor = Color.FromArgb("#2C3E50"),
                BackgroundColor = Color.FromArgb("#F8F9FA"),
                HorizontalOptions = LayoutOptions.FillAndExpand,
                HeightRequest = 20,
                Margin = new Thickness(0, -5, 0, -5),
                Keyboard = keyboard ?? Keyboard.Default
            };
            entry.IsEnabled = true;
            entry.IsReadOnly = true;
            layout.Children.Add(labelView);
            layout.Children.Add(entry);

            return layout;
        }

        private StackLayout CreateLabelField(string label1, string label2)
        {
            var layout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.Center
            };

            var labelView1 = new Label
            {
                Text = label1,
                FontSize = 14,
                TextColor = Color.FromArgb("#2C3E50"),
                WidthRequest = 80,
                VerticalOptions = LayoutOptions.Center
            };

            var labelView2 = new Label
            {
                Text = label2,
                FontSize = 14,
                TextColor = Color.FromArgb("#2C3E50"),
                WidthRequest = 140,
                VerticalOptions = LayoutOptions.Start
            };

            layout.Children.Add(labelView1);
            layout.Children.Add(labelView2);

            return layout;
        }

        private StackLayout CreateOkNgSelection()
        {
            var layout = new StackLayout
            {
                Spacing = 5,
                Margin = new Thickness(10, 5),
                Children =
                {
                    CreateLabelField("アイテム：", $"{investmentInfoTo!.inputItem}"),
                    CreateLabelField("予定数量：", $"{investmentInfoTo!.inputItemQty}"),
                    CreateLabelField("実際数量：", $"{investmentInfoTo!.inputRst}")
                }
            };

            return layout;
        }

        private StackLayout CreateTimeRecordSection()
        {
            var layout = new StackLayout
            {
                Spacing = 5
            };

            var titleLabel = new Label
            {
                Text = "作業時間実績",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2C3E50"),
                Margin = new Thickness(0, 0, 0, 5)
            };

            layout.Children.Add(titleLabel);
            layout.Children.Add(CreateTimeInputField("開始時間：", "09:00", out startTimeEntry, true));
            layout.Children.Add(CreateTimeInputField("終了時間：", "17:00", out endTimeEntry, true));
            layout.Children.Add(CreateTimeInputField("所要時間(分)：", "自動計算", out durationEntry, false, Keyboard.Numeric));

            // 添加一个小的帮助文本
            //var helpLabel = new Label
            //{
            //    Text = "※開始時間と終了時間を入力すると所要時間が自動計算されます",
            //    FontSize = 11,
            //    TextColor = Color.FromArgb("#7F8C8D"),
            //    Margin = new Thickness(0, 5, 0, 0)
            //};

            //layout.Children.Add(helpLabel);

            return layout;
        }

        private StackLayout CreateRecordCardSection()
        {
            var layout = new StackLayout
            {
                Spacing = 5
            };

            layout.Children.Add(CreateTimeInputField("現品票：", "現品票", out genhinticketEntry, false));
            layout.Children.Add(CreateTimeInputField("ロットNo：", "ロットNo", out lotnoEntry, false));
            layout.Children.Add(CreateTimeInputField("数量：", "数量", out quantityEntry, false, Keyboard.Numeric));
            layout.Children.Add(CreateTimeInputField("印刷枚数：", "印刷枚数", out insatuEntry, false, Keyboard.Numeric));

            return layout;
        }

        private StackLayout CreateTimeInputField(string label, string placeholder, out Entry entry, bool flag, Keyboard keyboard = null)
        {
            var layout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.Center
            };

            var labelView = new Label
            {
                Text = label,
                FontSize = 14,
                TextColor = Color.FromArgb("#2C3E50"),
                WidthRequest = 100,
                HeightRequest = 20,
                Margin = new Thickness(0, -5, 0, -5),
                VerticalOptions = LayoutOptions.Center
            };

            entry = new Entry
            {
                Placeholder = placeholder,
                PlaceholderColor = Color.FromArgb("#95A5A6"),
                TextColor = Color.FromArgb("#2C3E50"),
                BackgroundColor = Colors.White,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                HeightRequest = 20,
                Margin = new Thickness(0, -5, 0, -5),
                Keyboard = Keyboard.Text
            };
            if (keyboard != null)
                entry.Keyboard = keyboard;
            if (flag)
            {
                entry.IsEnabled = true;
                entry.IsReadOnly = true;
            }
            layout.Children.Add(labelView);
            layout.Children.Add(entry);

            return layout;
        }

        private Button CreateStyledButton(string text, Color bgColor, Color pressedColor, out Button button)
        {
            button = new Button
            {
                Text = text,
                BackgroundColor = bgColor,
                TextColor = Colors.White,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
                WidthRequest = 95,
                HeightRequest = 44,
                Padding = new Thickness(5, 0)
            };

            // 添加按下效果
            //button.Pressed += (s, e) => button.BackgroundColor = pressedColor;
            //button.Released += (s, e) => button.BackgroundColor = bgColor;

            return button;
        }

        private void SetupEventHandlers()
        {
            // 时间计算逻辑
            startTimeEntry.TextChanged += (s, e) => CalculateDuration();
            endTimeEntry.TextChanged += (s, e) => CalculateDuration();

            // 按钮点击事件
            timeMeasureButton.Clicked += (s, e) => ShowTimeMeasureDialog();
            reportButton.Clicked += async (s, e) => await ShowReportDialog();
            updateButton.Clicked += async (s, e) => await ShowUpdateConfirmation();
        }

        private void CalculateDuration()
        {
            if (!string.IsNullOrEmpty(startTimeEntry.Text) && !string.IsNullOrEmpty(endTimeEntry.Text))
            {
                try
                {
                    var startParts = startTimeEntry.Text.Split(':');
                    var endParts = endTimeEntry.Text.Split(':');

                    if (startParts.Length >= 2 && endParts.Length >= 2)
                    {
                        int startHour = int.Parse(startParts[0]);
                        int startMinute = int.Parse(startParts[1]);
                        int endHour = int.Parse(endParts[0]);
                        int endMinute = int.Parse(endParts[1]);

                        int totalStartMinutes = startHour * 60 + startMinute;
                        int totalEndMinutes = endHour * 60 + endMinute;

                        if (totalEndMinutes >= totalStartMinutes)
                        {
                            int totalMinutes = totalEndMinutes - totalStartMinutes;
                            durationEntry.Text = totalMinutes.ToString();
                            //int hours = totalMinutes / 60;
                            //int minutes = totalMinutes % 60;

                            //if (hours > 0)
                            //{
                            //    durationEntry.Text = $"{hours}時間{minutes}分";
                            //}
                            //else
                            //{
                            //    durationEntry.Text = $"{minutes}分";
                            //}
                        }
                        else
                        {
                            durationEntry.Text = "時間エラー";
                        }
                    }
                }
                catch
                {
                    durationEntry.Text = "無効な時間";
                }
            }
        }

        private void ShowTimeMeasureDialog()
        {
            if (string.IsNullOrEmpty(startTimeEntry.Text))
            {
                startTimeEntry.Text = getCurTime();
                timeMeasureButton.Text = "終了";
            }
            else if (string.IsNullOrEmpty(endTimeEntry.Text))
            {
                endTimeEntry.Text = getCurTime();
                timeMeasureButton.Text = "開始";
            }
            else if (!string.IsNullOrEmpty(startTimeEntry.Text) && !string.IsNullOrEmpty(endTimeEntry.Text))
            {
                startTimeEntry.Text = getCurTime();
                endTimeEntry.Text = "";
                timeMeasureButton.Text = "終了";
            }
        }

        private async Task ShowReportDialog()
        {
            var editor = new Editor
            {
                HeightRequest = 80,
                BackgroundColor = Color.FromArgb("#FAFAFA"),
                TextColor = Color.FromArgb("#333333"),
                Placeholder = "コメントを入力してください...",
                PlaceholderColor = Color.FromArgb("#888888"),
                FontSize = 13,
                AutoSize = EditorAutoSizeOption.TextChanges
            };
            // 创建报告对话框
            var dialog = new Frame
            {
                CornerRadius = 15,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#27AE60"),
                Padding = 25,
                WidthRequest = 350,
                HeightRequest = 320,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = new StackLayout
                {
                    Children =
                    {
                        new Label
                        {
                            Text = "コメント：",
                            FontSize = 14,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#2C3E50"),
                            Margin = new Thickness(0, 0, 0, 5)
                        },
                        editor,
                        new Button
                        {
                            Text = "閉じる",
                            BackgroundColor = Color.FromArgb("#95A5A6"),
                            TextColor = Colors.White,
                            CornerRadius = 8,
                            HorizontalOptions = LayoutOptions.Center,
                            WidthRequest = 120,
                            Command = new Command(() =>
                            {
                                strcomment = editor.Text;
                                Application.Current.MainPage.Navigation.PopModalAsync();
                            })
                        }
                    }
                }
            };

            var overlay = new BoxView
            {
                BackgroundColor = Color.FromArgb("#80000000"),
                Opacity = 0.7
            };

            var dialogContainer = new AbsoluteLayout();
            AbsoluteLayout.SetLayoutBounds(overlay, new Rect(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(overlay, AbsoluteLayoutFlags.All);
            AbsoluteLayout.SetLayoutBounds(dialog, new Rect(0.5, 0.5, 350, 320));
            AbsoluteLayout.SetLayoutFlags(dialog, AbsoluteLayoutFlags.PositionProportional);

            dialogContainer.Children.Add(overlay);
            dialogContainer.Children.Add(dialog);

            var dialogPage = new ContentPage
            {
                BackgroundColor = Colors.Transparent,
                Content = dialogContainer
            };

            await Application.Current.MainPage.Navigation.PushModalAsync(dialogPage);
        }

        private async Task ShowUpdateConfirmation()
        {
            var result = await Application.Current.MainPage.DisplayAlert(
                "更新確認",
                "工データを更新しますか？",
                "更新",
                "キャンセル");

            if (result)
            {
                InvestmentUpdInfo investmentUpdParam = new InvestmentUpdInfo();
                investmentUpdParam.id = strprocessid!;
                investmentUpdParam.operatorUser = investmentInfoTo!.operatorUser;
                investmentUpdParam.equipment = investmentInfoTo!.equipment;
                investmentUpdParam.qty = quantityEntry!.Text;
                investmentUpdParam.memo = strcomment!;
                investmentUpdParam.startTime = startTimeEntry!.Text;
                investmentUpdParam.endTime = endTimeEntry!.Text;
                investmentUpdParam.workTime = durationEntry!.Text;
                investmentUpdParam.endFlag = endCheckBox!.IsChecked;
                var request = new RequestData<InvestmentUpdInfo, EvangJsonModel>("UpdInvestment");
                request.Info = investmentUpdParam;
                var resultList = await this.Post<InvestmentUpdInfo, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
                if (resultList == null)
                    return;
                if (resultList.Success)
                {
                    Application.Current.MainPage.DisplayAlert("成功", "データが正常に更新されました。", "OK");
                }
                else
                {
                    Application.Current.MainPage.DisplayAlert("失敗", "データ更新失敗。", "OK");
                }
            }
            //if (result)
            //{
            //    // 模拟更新成功
            //    await Application.Current.MainPage.DisplayAlert("成功", "データが正常に更新されました。", "OK");

            //    // 更新状态徽章
            //    var statusFrame = (statusLabel.Parent as StackLayout)?.Parent as Frame;
            //    if (statusFrame != null)
            //    {
            //        statusFrame.BackgroundColor = Color.FromArgb("#F39C12");
            //        ((statusFrame.Content as StackLayout)?.Children[0] as StackLayout)?.Children[1]?.FindByName<Label>("statusText")?.SetValue(Label.TextProperty, "更新済み");
            //    }
            //}
        }

        private string getCurTime()
        {
            DateTime now = System.DateTime.Now;
            DateTime nowadd = now.AddHours(9);
            return nowadd.ToString("HH:mm");
        }
    }
}