using EvangPL.Utils;
using EvangPL.Views.Investment;
using EvangPL.Views.OutHigh;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EvangPL.Views.HariDashi
{
    // 仓库类 - Key-Value 形式
    public class Warehouse
    {
        public string Code { get; set; }    // Key
        public string Name { get; set; }    // Value（显示用）

        // 重写ToString，显示Name（Value）
        public override string ToString()
        {
            return Name;
        }
    }

    // 数据模型
    public class InputDetail
    {
        public int PageNumber { get; set; }

        // 不可输入的项目
        public string WorkOrder { get; set; } = "";
        public string Process { get; set; } = "";
        public string AssemblyItem { get; set; } = "";
        public string Component { get; set; } = "";
        public string TicketQuantity { get; set; } = "";
        public string LotNo { get; set; } = "";

        // 可输入的项目 - 仓库保存Code（Key）
        public string TicketNo { get; set; } = "";
        public string SourceWarehouseCode { get; set; } = "";  // 保存Code（Key）
        public string DestinationWarehouseCode { get; set; } = "";  // 保存Code（Key）
        public string PaymentQuantity { get; set; } = "";
        public string Reason { get; set; } = "";

        // 获取仓库显示名称的辅助属性（只读，用于显示）
        [System.Text.Json.Serialization.JsonIgnore]
        public string SourceWarehouseDisplay
        {
            get
            {
                var vm = SimpleHaridashiViewModel.Instance;
                if (vm != null)
                {
                    var warehouse = vm.GetWarehouseByCode(SourceWarehouseCode);
                    return warehouse != null ? warehouse.Name : SourceWarehouseCode;
                }
                return SourceWarehouseCode;
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string DestinationWarehouseDisplay
        {
            get
            {
                var vm = SimpleHaridashiViewModel.Instance;
                if (vm != null)
                {
                    var warehouse = vm.GetWarehouseByCode(DestinationWarehouseCode);
                    return warehouse != null ? warehouse.Name : DestinationWarehouseCode;
                }
                return DestinationWarehouseCode;
            }
        }

        // 复制构造函数
        public InputDetail CopyFrom(InputDetail source)
        {
            this.WorkOrder = source.WorkOrder;
            this.Process = source.Process;
            this.AssemblyItem = source.AssemblyItem;
            this.Component = source.Component;
            this.TicketQuantity = source.TicketQuantity;
            this.LotNo = source.LotNo;
            return this;
        }
    }

    // ViewModel
    public class SimpleHaridashiViewModel : INotifyPropertyChanged
    {
        // 单例实例
        private static SimpleHaridashiViewModel _instance;
        public static SimpleHaridashiViewModel Instance => _instance;

        private ObservableCollection<InputDetail> _pages = new ObservableCollection<InputDetail>();
        private int _currentPageIndex = 0;
        private InputDetail _currentPage;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<InputDetail> Pages
        {
            get => _pages;
            set
            {
                _pages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPage));
            }
        }

        public InputDetail CurrentPage
        {
            get => _currentPage;
            private set
            {
                _currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPageSourceWarehouse));
                OnPropertyChanged(nameof(CurrentPageDestinationWarehouse));
            }
        }

        public int CurrentPageIndex
        {
            get => _currentPageIndex;
            private set
            {
                _currentPageIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
            }
        }

        public bool CanGoPrevious => CurrentPageIndex > 0;
        public bool CanGoNext => CurrentPageIndex < Pages.Count - 1;

        // 仓库列表 - Key-Value 形式
        public ObservableCollection<Warehouse> Warehouses { get; set; }

        // 当前页面的仓库选择属性（用于绑定到Picker）
        public Warehouse CurrentPageSourceWarehouse
        {
            get
            {
                if (CurrentPage != null && !string.IsNullOrEmpty(CurrentPage.SourceWarehouseCode))
                {
                    return GetWarehouseByCode(CurrentPage.SourceWarehouseCode);
                }
                return null;
            }
            set
            {
                if (CurrentPage != null)
                {
                    CurrentPage.SourceWarehouseCode = value?.Code ?? "";
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }

        public Warehouse CurrentPageDestinationWarehouse
        {
            get
            {
                if (CurrentPage != null && !string.IsNullOrEmpty(CurrentPage.DestinationWarehouseCode))
                {
                    return GetWarehouseByCode(CurrentPage.DestinationWarehouseCode);
                }
                return null;
            }
            set
            {
                if (CurrentPage != null)
                {
                    CurrentPage.DestinationWarehouseCode = value?.Code ?? "";
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }

        public SimpleHaridashiViewModel()
        {
            _instance = this;
            InitializeData();
        }

        private void InitializeData()
        {
            // 初始化仓库列表 - Key-Value 形式
            //Warehouses = new ObservableCollection<Warehouse>
            //{
            //    new Warehouse { Code = "WH001", Name = "本社倉庫" },
            //    new Warehouse { Code = "WH002", Name = "工場倉庫" },
            //    new Warehouse { Code = "WH003", Name = "部品倉庫" },
            //    new Warehouse { Code = "WH004", Name = "完成品倉庫" },
            //    new Warehouse { Code = "WH005", Name = "出荷待機倉庫" },
            //    new Warehouse { Code = "WH006", Name = "外部倉庫" },
            //    new Warehouse { Code = "WH007", Name = "一時保管倉庫" }
            //};

            // 初始化第一页
            var firstPage = new InputDetail { PageNumber = 1 };
            Pages.Add(firstPage);
            CurrentPage = firstPage;
        }

        // 根据Code获取仓库对象
        public Warehouse GetWarehouseByCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;

            return Warehouses.FirstOrDefault(w => w.Code == code);
        }

        // 根据Name获取仓库对象
        public Warehouse GetWarehouseByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            return Warehouses.FirstOrDefault(w => w.Name == name);
        }

        public void AddNewPage()
        {
            var newPage = new InputDetail
            {
                PageNumber = Pages.Count + 1
            }.CopyFrom(CurrentPage);

            Pages.Add(newPage);
            GoToPage(Pages.Count - 1);
        }

        public void GoToPreviousPage()
        {
            if (CanGoPrevious)
            {
                SaveCurrentData();
                GoToPage(CurrentPageIndex - 1);
            }
        }

        public void GoToNextPage()
        {
            if (CanGoNext)
            {
                SaveCurrentData();
                GoToPage(CurrentPageIndex + 1);
            }
            else
            {
                AddNewPage();
            }
        }

        private void GoToPage(int pageIndex)
        {
            if (pageIndex >= 0 && pageIndex < Pages.Count)
            {
                CurrentPageIndex = pageIndex;
                CurrentPage = Pages[pageIndex];
            }
        }

        // 保存当前页面的输入数据到ViewModel
        public void SaveCurrentData()
        {
            // 数据通过双向绑定自动保存
        }

        public void RegisterAll()
        {
            // 验证所有页面
            foreach (var page in Pages)
            {
                if (string.IsNullOrEmpty(page.TicketNo) ||
                    string.IsNullOrEmpty(page.SourceWarehouseCode) ||
                    string.IsNullOrEmpty(page.DestinationWarehouseCode) ||
                    string.IsNullOrEmpty(page.PaymentQuantity))
                {
                    // 验证失败
                    return;
                }
            }

            // 这里可以添加实际的注册逻辑
            Console.WriteLine($"注册 {Pages.Count} 页数据");
            Console.WriteLine("仓库数据（显示Key-Value格式）:");
            foreach (var page in Pages)
            {
                var sourceWarehouse = GetWarehouseByCode(page.SourceWarehouseCode);
                var destWarehouse = GetWarehouseByCode(page.DestinationWarehouseCode);

                Console.WriteLine($"页面 {page.PageNumber}:");
                Console.WriteLine($"  移動元倉庫: {page.SourceWarehouseCode} - {(sourceWarehouse != null ? sourceWarehouse.Name : "未選択")}");
                Console.WriteLine($"  移動先倉庫: {page.DestinationWarehouseCode} - {(destWarehouse != null ? destWarehouse.Name : "未選択")}");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // 自定义Picker控件，用于显示Value但保存Key
    public class KeyValuePicker : Picker
    {
        private ObservableCollection<Warehouse> _itemsSource;

        public ObservableCollection<Warehouse> ItemsSource
        {
            get => _itemsSource;
            set
            {
                _itemsSource = value;
                this.Items.Clear();

                if (value != null)
                {
                    foreach (var item in value)
                    {
                        // 只添加Name（Value）到显示列表
                        this.Items.Add(item.Name);
                    }
                }
            }
        }

        // 当前选择的仓库对象（包含Key和Value）
        public Warehouse SelectedWarehouse
        {
            get
            {
                if (SelectedIndex >= 0 && SelectedIndex < ItemsSource?.Count)
                {
                    return ItemsSource[SelectedIndex];
                }
                return null;
            }
            set
            {
                if (value != null && ItemsSource != null)
                {
                    var index = ItemsSource.IndexOf(value);
                    if (index >= 0)
                    {
                        SelectedIndex = index;
                    }
                }
                else
                {
                    SelectedIndex = -1;
                }
            }
        }

        // 当前选择的Key
        public string SelectedKey
        {
            get => SelectedWarehouse?.Code;
            set
            {
                if (!string.IsNullOrEmpty(value) && ItemsSource != null)
                {
                    var warehouse = ItemsSource.FirstOrDefault(w => w.Code == value);
                    SelectedWarehouse = warehouse;
                }
                else
                {
                    SelectedWarehouse = null;
                }
            }
        }

        // 当前选择的Value
        public string SelectedValue
        {
            get => SelectedWarehouse?.Name;
        }
    }

    // 主页面
    public class HariDashi : EvangContentVM
    {
        private SimpleHaridashiViewModel _viewModel;

        // UI控件
        private StackLayout _mainLayout;
        private StackLayout _formLayout;

        // 输入控件
        private Entry _ticketNoEntry;
        private KeyValuePicker _sourceWarehousePicker;
        private KeyValuePicker _destinationWarehousePicker;
        private Entry _paymentQuantityEntry;
        private Entry _reasonEntry;

        // 只读标签
        private Label _workOrderLabel;
        private Label _processLabel;
        private Label _assemblyItemLabel;
        private Label _componentLabel;
        private Label _ticketQuantityLabel;
        private Label _lotNoLabel;

        // 按钮
        private Button _prevButton;
        private Button _nextButton;
        private Button _registerButton;

        private List<DropdownInfo> dropdownlist;
        private HariDashiInfo hariDashiInfoTo;

        public HariDashi(ParamInfo paramInfoTo) : base("strHariDashi")
        {
            _viewModel = new SimpleHaridashiViewModel();
            BindingContext = _viewModel;
            Title = "払出画面";
            BuildUI(paramInfoTo);
        }

        private async void BuildUI(ParamInfo paramInfoTo)
        {
            await getworkdata(paramInfoTo);
            _viewModel.Warehouses = new ObservableCollection<Warehouse>();
            for (int i = 0; i < dropdownlist.Count; i++)
            {
                _viewModel.Warehouses.Add(new Warehouse { Code = dropdownlist[i].code, Name = dropdownlist[i].name });
            }
            _viewModel.CurrentPage.WorkOrder = hariDashiInfoTo.ordName;
            _viewModel.CurrentPage.Process = hariDashiInfoTo.operationName;
            _viewModel.CurrentPage.AssemblyItem = hariDashiInfoTo.itemName;
            _viewModel.CurrentPage.LotNo = "lot1";
            // 设置页面属性
            BackgroundColor = Color.FromArgb("#F5F7FA");

            // 主布局 - 使用Grid确保按钮固定在底部
            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Star },
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star }
                }
            };

            // 表单区域（可滚动）
            var scrollView = new ScrollView();
            _mainLayout = new StackLayout
            {
                Spacing = 0,
                Padding = new Thickness(0, 10, 0, 0)
            };

            // 创建表单卡片
            var formCard = CreateFormCard();
            _mainLayout.Children.Add(formCard);

            scrollView.Content = _mainLayout;

            // 按钮区域
            var buttonLayout = CreateButtonLayout();

            // 添加到Grid
            grid.Add(scrollView, 0, 0);
            grid.Add(buttonLayout, 0, 1);

            Content = grid;
            SetupBindings();
            SetupEvents();

            // 初始更新表单
            UpdateFormWithCurrentPage();
        }

        private Frame CreateFormCard()
        {
            _formLayout = new StackLayout
            {
                Spacing = 12,
                Padding = new Thickness(16, 20)
            };

            // 添加表单字段
            CreateFormFields();

            return new Frame
            {
                Content = _formLayout,
                CornerRadius = 15,
                Padding = 0,
                Margin = new Thickness(16, 8, 16, 8),
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#E0E6ED"),
                HasShadow = true
            };
        }

        private void CreateFormFields()
        {
            // 1. 製造ワークオーダー（只读）
            AddReadOnlyField("製造ワークオーダー", _viewModel.CurrentPage?.WorkOrder);

            // 2. 工程（只读）
            AddReadOnlyField("工程", _viewModel.CurrentPage?.Process);

            // 3. アセンブリアイテム（只读）
            AddReadOnlyField("アセンブリアイテム", _viewModel.CurrentPage?.AssemblyItem);

            // 4. 現品票No（输入）
            AddInputField("現品票No", out _ticketNoEntry, "現品票Noを入力");

            // 5. コンポネント（只读）
            AddReadOnlyField("コンポネント", _viewModel.CurrentPage?.Component);

            // 6. 現品票数量 和 ロットNo（同一行）
            AddTwoColumnField();

            // 7. 移動元倉庫（下拉框）- 使用自定义KeyValuePicker
            AddKeyValuePickerField("移動元倉庫", out _sourceWarehousePicker, _viewModel.Warehouses);

            // 8. 移動先倉庫（下拉框）- 使用自定义KeyValuePicker
            AddKeyValuePickerField("移動先倉庫", out _destinationWarehousePicker, _viewModel.Warehouses);

            // 9. 払出数量（输入）
            AddInputField("払出数量", out _paymentQuantityEntry, "数量を入力", Keyboard.Numeric);

            // 10. 理由（输入）
            AddInputField("理由", out _reasonEntry, "理由を入力（任意）");
        }

        private void AddReadOnlyField(string label, string value)
        {
            var fieldLayout = new StackLayout
            {
                Spacing = 4
            };

            // 标签
            var labelText = new Label
            {
                Text = label,
                FontSize = 13,
                TextColor = Color.FromArgb("#718096"),
                FontAttributes = FontAttributes.Bold
            };

            // 值（只读显示）
            var valueFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F7FAFC"),
                BorderColor = Color.FromArgb("#E2E8F0"),
                CornerRadius = 8,
                Padding = new Thickness(12, 10),
                HasShadow = false
            };

            var valueLabel = new Label
            {
                Text = value,
                FontSize = 15,
                TextColor = Color.FromArgb("#2D3748")
            };

            valueFrame.Content = valueLabel;

            fieldLayout.Children.Add(labelText);
            fieldLayout.Children.Add(valueFrame);

            _formLayout.Children.Add(fieldLayout);

            // 保存只读标签引用（用于更新）
            if (label == "製造ワークオーダー") _workOrderLabel = valueLabel;
            else if (label == "工程") _processLabel = valueLabel;
            else if (label == "アセンブリアイテム") _assemblyItemLabel = valueLabel;
            else if (label == "コンポネント") _componentLabel = valueLabel;
        }

        private void AddInputField(string label, out Entry entry, string placeholder, Keyboard keyboard = null)
        {
            var fieldLayout = new StackLayout
            {
                Spacing = 4
            };

            // 标签
            var labelText = new Label
            {
                Text = label,
                FontSize = 13,
                TextColor = Color.FromArgb("#718096"),
                FontAttributes = FontAttributes.Bold
            };

            // 输入框
            entry = new Entry
            {
                Placeholder = placeholder,
                PlaceholderColor = Color.FromArgb("#A0AEC0"),
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#2D3748"),
                FontSize = 15,
                HeightRequest = 44
            };

            if (keyboard != null)
                entry.Keyboard = keyboard;

            var inputFrame = new Frame
            {
                Content = entry,
                BackgroundColor = Color.FromArgb("#F7FAFC"),
                BorderColor = Color.FromArgb("#E2E8F0"),
                CornerRadius = 8,
                Padding = new Thickness(12, 0),
                HasShadow = false
            };

            fieldLayout.Children.Add(labelText);
            fieldLayout.Children.Add(inputFrame);

            _formLayout.Children.Add(fieldLayout);
        }

        private void AddKeyValuePickerField(string label, out KeyValuePicker picker, ObservableCollection<Warehouse> warehouses)
        {
            var fieldLayout = new StackLayout
            {
                Spacing = 4
            };

            // 标签
            var labelText = new Label
            {
                Text = label,
                FontSize = 13,
                TextColor = Color.FromArgb("#718096"),
                FontAttributes = FontAttributes.Bold
            };

            // 自定义KeyValuePicker
            picker = new KeyValuePicker
            {
                Title = "選択してください",
                TitleColor = Color.FromArgb("#A0AEC0"),
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#2D3748"),
                FontSize = 15,
                HeightRequest = 44
            };

            // 设置数据源
            picker.ItemsSource = warehouses;

            var pickerFrame = new Frame
            {
                Content = picker,
                BackgroundColor = Color.FromArgb("#F7FAFC"),
                BorderColor = Color.FromArgb("#E2E8F0"),
                CornerRadius = 8,
                Padding = new Thickness(12, 0),
                HasShadow = false
            };

            fieldLayout.Children.Add(labelText);
            fieldLayout.Children.Add(pickerFrame);

            _formLayout.Children.Add(fieldLayout);
        }

        private void AddTwoColumnField()
        {
            var fieldLayout = new StackLayout
            {
                Spacing = 4
            };

            // 标签行
            var labelRow = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 20
            };

            var label1 = new Label
            {
                Text = "現品票数量",
                FontSize = 13,
                TextColor = Color.FromArgb("#718096"),
                FontAttributes = FontAttributes.Bold,
                WidthRequest = 120
            };

            var label2 = new Label
            {
                Text = "ロットNo",
                FontSize = 13,
                TextColor = Color.FromArgb("#718096"),
                FontAttributes = FontAttributes.Bold,
                WidthRequest = 120
            };

            labelRow.Children.Add(label1);
            labelRow.Children.Add(label2);

            // 值行
            var valueRow = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 20
            };

            // 現品票数量
            var value1Frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F7FAFC"),
                BorderColor = Color.FromArgb("#E2E8F0"),
                CornerRadius = 8,
                Padding = new Thickness(12, 10),
                HasShadow = false,
                WidthRequest = 120
            };

            _ticketQuantityLabel = new Label
            {
                Text = _viewModel.CurrentPage?.TicketQuantity,
                FontSize = 15,
                TextColor = Color.FromArgb("#2D3748")
            };

            value1Frame.Content = _ticketQuantityLabel;

            // ロットNo
            var value2Frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F7FAFC"),
                BorderColor = Color.FromArgb("#E2E8F0"),
                CornerRadius = 8,
                Padding = new Thickness(12, 10),
                HasShadow = false,
                WidthRequest = 120
            };

            _lotNoLabel = new Label
            {
                Text = _viewModel.CurrentPage?.LotNo,
                FontSize = 15,
                TextColor = Color.FromArgb("#2D3748")
            };

            value2Frame.Content = _lotNoLabel;

            valueRow.Children.Add(value1Frame);
            valueRow.Children.Add(value2Frame);

            fieldLayout.Children.Add(labelRow);
            fieldLayout.Children.Add(valueRow);

            _formLayout.Children.Add(fieldLayout);
        }

        private Frame CreateButtonLayout()
        {
            var buttonLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 10,
                Padding = new Thickness(16, 12, 16, 12),
                BackgroundColor = Colors.White
            };

            // 前页按钮
            _prevButton = new Button
            {
                Text = "前頁",
                CornerRadius = 8,
                BackgroundColor = Color.FromArgb("#4A5568"),
                TextColor = Colors.White,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 48,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            // 下页按钮
            _nextButton = new Button
            {
                Text = "下頁",
                CornerRadius = 8,
                BackgroundColor = Color.FromArgb("#3182CE"),
                TextColor = Colors.White,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 48,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            // 登録按钮
            _registerButton = new Button
            {
                Text = "登録",
                CornerRadius = 8,
                BackgroundColor = Color.FromArgb("#38A169"),
                TextColor = Colors.White,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 48,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            buttonLayout.Children.Add(_prevButton);
            buttonLayout.Children.Add(_nextButton);
            buttonLayout.Children.Add(_registerButton);

            return new Frame
            {
                Content = buttonLayout,
                CornerRadius = 0,
                Padding = 0,
                Margin = 0,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#E2E8F0"),
                HasShadow = true
            };
        }

        private async Task getworkdata(ParamInfo paramInfoTo)
        {
            var processinfoTo = paramInfoTo;
            var request = new RequestData<ParamInfo, EvangJsonModel>("GetHariDashi");
            request.Info = processinfoTo;
            var resultList = await this.Post<ParamInfo, EvangJsonModel, EvangJsonModel, EvangJsonModel>(request);
            if (resultList == null || resultList.SubData == null)
                return;

            foreach (var item in resultList.SubData)
            {
                switch (item.SubName)
                {
                    case "LF_LOCATION":
                        if (item == null || item.SubJson == null)
                            return;
                        var steplist = BaseUtils.JsonToClass<List<DropdownInfo>>(item.SubJson);
                        if (steplist == null || steplist.Count == 0)
                            return;
                        dropdownlist = steplist;
                        break;
                    case "LF_PROCESS":
                        if (item == null || item.SubJson == null)
                            return;
                        var steplist1 = BaseUtils.JsonToClass<List<HariDashiInfo>>(item.SubJson);
                        if (steplist1 == null || steplist1.Count == 0)
                            return;
                        hariDashiInfoTo = steplist1[0];
                        break;
                }
            }
        }

        private void SetupBindings()
        {
            // 按钮状态绑定
            _prevButton.SetBinding(Button.IsEnabledProperty, nameof(_viewModel.CanGoPrevious));
            //_nextButton.SetBinding(Button.IsEnabledProperty, nameof(_viewModel.CanGoNext));

            // 按钮样式根据状态变化
            _prevButton.SetBinding(Button.BackgroundColorProperty, nameof(_viewModel.CanGoPrevious),
                converter: new BoolToColorConverter("#4A5568", "#A0AEC0"));

            _nextButton.SetBinding(Button.BackgroundColorProperty, nameof(_viewModel.CanGoNext),
                converter: new BoolToColorConverter("#3182CE", "#3182CE"));

            // 输入控件绑定
            _ticketNoEntry.SetBinding(Entry.TextProperty, "CurrentPage.TicketNo", BindingMode.TwoWay);
            _paymentQuantityEntry.SetBinding(Entry.TextProperty, "CurrentPage.PaymentQuantity", BindingMode.TwoWay);
            _reasonEntry.SetBinding(Entry.TextProperty, "CurrentPage.Reason", BindingMode.TwoWay);

            // KeyValuePicker绑定
            // 绑定SelectedKey到ViewModel的仓库Code
            _sourceWarehousePicker.SelectedIndexChanged += (s, e) =>
            {
                if (_sourceWarehousePicker.SelectedWarehouse != null)
                {
                    _viewModel.CurrentPageSourceWarehouse = _sourceWarehousePicker.SelectedWarehouse;
                }
            };

            _destinationWarehousePicker.SelectedIndexChanged += (s, e) =>
            {
                if (_destinationWarehousePicker.SelectedWarehouse != null)
                {
                    _viewModel.CurrentPageDestinationWarehouse = _destinationWarehousePicker.SelectedWarehouse;
                }
            };

            // 监听当前页面变化
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.CurrentPage))
                {
                    UpdateFormWithCurrentPage();
                }
            };
        }

        private void SetupEvents()
        {
            _prevButton.Clicked += async (s, e) =>
            {
                _viewModel.SaveCurrentData();
                _viewModel.GoToPreviousPage();
                await AnimatePageTransition(false);
            };

            _nextButton.Clicked += async (s, e) =>
            {
                _viewModel.SaveCurrentData();
                // 验证当前页
            if (string.IsNullOrEmpty(_viewModel.CurrentPage.TicketNo) ||
                string.IsNullOrEmpty(_viewModel.CurrentPage.SourceWarehouseCode) ||
                string.IsNullOrEmpty(_viewModel.CurrentPage.DestinationWarehouseCode) ||
                string.IsNullOrEmpty(_viewModel.CurrentPage.PaymentQuantity))
            {
                await DisplayAlert("入力不足",
                    "必須項目を入力してください:\n• 現品票No\n• 移動元倉庫\n• 移動先倉庫\n• 払出数量",
                    "OK");
                return;
            }

            // 验证数量是否为有效数字
            if (!int.TryParse(_viewModel.CurrentPage.PaymentQuantity, out int quantity) || quantity <= 0)
            {
                await DisplayAlert("入力エラー",
                    "払出数量は正の整数を入力してください",
                    "OK");
                return;
            }
                _viewModel.GoToNextPage();
                await AnimatePageTransition(true);
            };

            _registerButton.Clicked += OnRegisterButtonClicked;
        }

        private async Task AnimatePageTransition(bool forward)
        {
            // 简单的页面切换动画
            if (_mainLayout.Children.Count > 0 && _mainLayout.Children[0] is Frame formCard)
            {
                // 向右滑出（切换到下一页）
                if (forward)
                {
                    await formCard.TranslateTo(-this.Width, 0, 200, Easing.CubicIn);
                    formCard.TranslationX = this.Width;
                    await formCard.TranslateTo(0, 0, 200, Easing.CubicOut);
                }
                // 向左滑出（切换到上一页）
                else
                {
                    await formCard.TranslateTo(this.Width, 0, 200, Easing.CubicIn);
                    formCard.TranslationX = -this.Width;
                    await formCard.TranslateTo(0, 0, 200, Easing.CubicOut);
                }
            }
        }

        private void UpdateFormWithCurrentPage()
        {
            if (_viewModel.CurrentPage == null) return;

            // 更新只读字段
            if (_workOrderLabel != null) _workOrderLabel.Text = _viewModel.CurrentPage.WorkOrder;
            if (_processLabel != null) _processLabel.Text = _viewModel.CurrentPage.Process;
            if (_assemblyItemLabel != null) _assemblyItemLabel.Text = _viewModel.CurrentPage.AssemblyItem;
            if (_componentLabel != null) _componentLabel.Text = _viewModel.CurrentPage.Component;
            if (_ticketQuantityLabel != null) _ticketQuantityLabel.Text = _viewModel.CurrentPage.TicketQuantity;
            if (_lotNoLabel != null) _lotNoLabel.Text = _viewModel.CurrentPage.LotNo;

            // 更新Picker选择
            if (!string.IsNullOrEmpty(_viewModel.CurrentPage.SourceWarehouseCode))
            {
                _sourceWarehousePicker.SelectedKey = _viewModel.CurrentPage.SourceWarehouseCode;
            }
            else
            {
                _sourceWarehousePicker.SelectedIndex = -1;
            }

            if (!string.IsNullOrEmpty(_viewModel.CurrentPage.DestinationWarehouseCode))
            {
                _destinationWarehousePicker.SelectedKey = _viewModel.CurrentPage.DestinationWarehouseCode;
            }
            else
            {
                _destinationWarehousePicker.SelectedIndex = -1;
            }
        }

        private async void OnRegisterButtonClicked(object sender, EventArgs e)
        {
            // 保存当前页
            _viewModel.SaveCurrentData();

            // 验证当前页
            if (string.IsNullOrEmpty(_viewModel.CurrentPage.TicketNo) ||
                string.IsNullOrEmpty(_viewModel.CurrentPage.SourceWarehouseCode) ||
                string.IsNullOrEmpty(_viewModel.CurrentPage.DestinationWarehouseCode) ||
                string.IsNullOrEmpty(_viewModel.CurrentPage.PaymentQuantity))
            {
                await DisplayAlert("入力不足",
                    "必須項目を入力してください:\n• 現品票No\n• 移動元倉庫\n• 移動先倉庫\n• 払出数量",
                    "OK");
                return;
            }

            // 验证数量是否为有效数字
            if (!int.TryParse(_viewModel.CurrentPage.PaymentQuantity, out int quantity) || quantity <= 0)
            {
                await DisplayAlert("入力エラー",
                    "払出数量は正の整数を入力してください",
                    "OK");
                return;
            }

            // 显示确认对话框
            bool confirm = await DisplayAlert("確認",
                $"全 {_viewModel.Pages.Count} ページのデータを登録します。\nよろしいですか？",
                "はい", "いいえ");

            if (confirm)
            {
                _viewModel.RegisterAll();
                List<HariDashiUpdInfo> hariDashiUpdInfos = new List<HariDashiUpdInfo>();
                foreach (var page in _viewModel.Pages)
                {
                    HariDashiUpdInfo hariDashiUpd = new HariDashiUpdInfo();
                    hariDashiUpd.locationId = page.SourceWarehouseCode;
                    hariDashiUpd.tranlocaId = page.DestinationWarehouseCode;
                    hariDashiUpd.ordId = hariDashiInfoTo.ordId;
                    hariDashiUpd.itemId = hariDashiInfoTo.itemId;
                    hariDashiUpd.qty = page.PaymentQuantity;
                    hariDashiUpd.lotNo = page.LotNo;
                    hariDashiUpd.reason = page.Reason;
                    hariDashiUpdInfos.Add(hariDashiUpd);
                }
                var request = new RequestData<EvangJsonModel, HariDashiUpdInfo>("UpdHariDashi");
                request.Data = hariDashiUpdInfos;
                var resultList = await this.Post<EvangJsonModel, HariDashiUpdInfo, EvangJsonModel, EvangJsonModel>(request);
                if (resultList == null)
                    return;
                if (resultList.Success)
                {
                    // 显示成功消息
                    await DisplayAlert("完了",
                        $"{_viewModel.Pages.Count} ページのデータを登録しました。",
                        "OK");
                }
                else
                {
                    // 显示成功消息
                    await DisplayAlert("完了",
                        $"{_viewModel.Pages.Count} ページのデータ更新失敗。",
                        "OK");
                }
                
            }
        }

        // 转换器类
        private class BoolToColorConverter : IValueConverter
        {
            private readonly Color _trueColor;
            private readonly Color _falseColor;

            public BoolToColorConverter(string trueColorHex, string falseColorHex)
            {
                _trueColor = Color.FromArgb(trueColorHex);
                _falseColor = Color.FromArgb(falseColorHex);
            }

            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return (bool)value ? _trueColor : _falseColor;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}