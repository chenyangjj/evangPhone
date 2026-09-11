using EvangSol.Mobibrary.EvangModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EvangPL.Components
{
    public class Product : EvangJsonModel, INotifyPropertyChanged
    {
        private int _id;
        private string _itemId;
        private string _itemName;
        private string _status;
        private int _scheduledQty;
        private int _completedQty;
        private int _parentId;
        private decimal _subQuantity;
        private bool _isParent;
        private bool _isExpanded;
        private ObservableCollection<Product> _subProducts;

        // 1. 无参构造函数（与ProcessInfoItem风格一致）
        public Product()
        {
            _subProducts = new ObservableCollection<Product>();
        }

        // 2. 带参数的构造函数（与ProcessInfoItem风格一致）
        public Product(
            int id,
            string itemId,
            string itemName,
            string status,
            int scheduledQty,
            int completedQty,
            int parentId,
            decimal subQuantity,
            bool isParent
        )
        {
            Id = id;
            ItemId = itemId;
            ItemName = itemName;
            Status = status;
            ScheduledQty = scheduledQty;
            CompletedQty = completedQty;
            ParentId = parentId;
            SubQuantity = subQuantity;
            IsParent = isParent;
            _subProducts = new ObservableCollection<Product>();
        }

        #region 数据属性
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ItemId
        {
            get => _itemId;
            set
            {
                if (_itemId != value)
                {
                    _itemId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ItemName
        {
            get => _itemName;
            set
            {
                if (_itemName != value)
                {
                    _itemName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ScheduledQty
        {
            get => _scheduledQty;
            set
            {
                if (_scheduledQty != value)
                {
                    _scheduledQty = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CompletedQty
        {
            get => _completedQty;
            set
            {
                if (_completedQty != value)
                {
                    _completedQty = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ParentId
        {
            get => _parentId;
            set
            {
                if (_parentId != value)
                {
                    _parentId = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal SubQuantity
        {
            get => _subQuantity;
            set
            {
                if (_subQuantity != value)
                {
                    _subQuantity = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsParent
        {
            get => _isParent;
            set
            {
                if (_isParent != value)
                {
                    _isParent = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region UI控制属性
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ExpandIcon));
                }
            }
        }

        public string ExpandIcon => IsExpanded ? "▲" : "▼";

        public ObservableCollection<Product> SubProducts
        {
            get => _subProducts;
            set
            {
                if (_subProducts != value)
                {
                    _subProducts = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        // 实用方法
        public void AddSubProduct(Product subProduct)
        {
            _subProducts.Add(subProduct);
            OnPropertyChanged(nameof(SubProducts));
        }
    }

    public class ExpandableCardView : ContentView
    {
        private VerticalStackLayout _childrenContainer;
        private Label _iconLabel;

        public ExpandableCardView()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            // 主容器
            var mainStack = new VerticalStackLayout
            {
                Spacing = 0
            };

            // 父卡片容器
            var parentCard = new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#E0E0E0"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(5) },
                Padding = new Thickness(10),
                Margin = new Thickness(10, 0, 10, 5)
            };

            var cardContentLayout = new VerticalStackLayout
            {
                Spacing = 5
            };

            var firstLineGrid = new Grid();
            firstLineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            firstLineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var itemIdLayout = new HorizontalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };

            var itemIdLabel = new Label
            {
                Text = "アイテム：",
                FontSize = 13,
                TextColor = Colors.Gray
            };

            var itemIdValueLabel = new Label();
            itemIdValueLabel.SetBinding(Label.TextProperty, "ItemId");
            itemIdValueLabel.FontSize = 13;
            itemIdValueLabel.TextColor = Colors.Black;
            itemIdValueLabel.LineBreakMode = LineBreakMode.TailTruncation;

            itemIdLayout.Children.Add(itemIdLabel);
            itemIdLayout.Children.Add(itemIdValueLabel);

            Grid.SetColumn(itemIdLayout, 0);
            firstLineGrid.Children.Add(itemIdLayout);

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
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            statusLabel.SetBinding(Label.TextProperty, "Status");

            statusBorder.SetBinding(Border.BackgroundColorProperty,
                new Binding("Status", converter: new StatusColorConverter()));
            statusBorder.Content = statusLabel;

            Grid.SetColumn(statusBorder, 1);
            firstLineGrid.Children.Add(statusBorder);

            cardContentLayout.Children.Add(firstLineGrid);

            var secondLineGrid = new Grid();
            secondLineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            secondLineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var itemNameLayout = new HorizontalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };

            var itemNameLabel = new Label
            {
                Text = "アイテム名：",
                FontSize = 13,
                TextColor = Colors.Gray
            };

            var itemNameValueLabel = new Label();
            itemNameValueLabel.SetBinding(Label.TextProperty, "ItemName");
            itemNameValueLabel.FontSize = 13;
            itemNameValueLabel.TextColor = Colors.Black;
            itemNameValueLabel.LineBreakMode = LineBreakMode.WordWrap;

            itemNameLayout.Children.Add(itemNameLabel);
            itemNameLayout.Children.Add(itemNameValueLabel);

            Grid.SetColumn(itemNameLayout, 0);
            secondLineGrid.Children.Add(itemNameLayout);

            _iconLabel = new Label();
            _iconLabel.SetBinding(Label.TextProperty, "ExpandIcon");
            _iconLabel.FontSize = 16;
            _iconLabel.TextColor = Color.FromArgb("#666666");
            _iconLabel.VerticalOptions = LayoutOptions.Center;
            _iconLabel.HorizontalOptions = LayoutOptions.End;

            Grid.SetColumn(_iconLabel, 1);
            secondLineGrid.Children.Add(_iconLabel);

            cardContentLayout.Children.Add(secondLineGrid);

            var quantityGrid = new Grid();
            quantityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            quantityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var quantityStack = new HorizontalStackLayout
            {
                Spacing = 16,
                VerticalOptions = LayoutOptions.Center
            };

            var scheduledStack = new HorizontalStackLayout
            {
                Spacing = 2
            };

            var scheduledQtyLabel = new Label
            {
                Text = "予定数量：",
                FontSize = 13,
                TextColor = Colors.Gray
            };

            var scheduledQtyValLabel = new Label();
            scheduledQtyValLabel.SetBinding(Label.TextProperty, "ScheduledQty");
            scheduledQtyValLabel.FontSize = 13;
            scheduledQtyValLabel.TextColor = Colors.Black;
            scheduledQtyValLabel.FontAttributes = FontAttributes.Bold;
            scheduledQtyValLabel.VerticalOptions = LayoutOptions.End;

            scheduledStack.Children.Add(scheduledQtyLabel);
            scheduledStack.Children.Add(scheduledQtyValLabel);

            var completedStack = new HorizontalStackLayout
            {
                Spacing = 2
            };

            var completedQtyLabel = new Label
            {
                Text = "完了数量：",
                FontSize = 13,
                TextColor = Colors.Gray
            };

            var completedQtyValLabel = new Label();
            completedQtyValLabel.SetBinding(Label.TextProperty, "CompletedQty");
            completedQtyValLabel.FontSize = 13;
            completedQtyValLabel.TextColor = Color.FromArgb("#4CAF50");
            completedQtyValLabel.FontAttributes = FontAttributes.Bold;
            completedQtyValLabel.VerticalOptions = LayoutOptions.End;

            completedStack.Children.Add(completedQtyLabel);
            completedStack.Children.Add(completedQtyValLabel);

            quantityStack.Children.Add(scheduledStack);
            quantityStack.Children.Add(completedStack);

            Grid.SetColumn(quantityStack, 0);
            quantityGrid.Children.Add(quantityStack);

            //var emptyRightColumn = new BoxView
            //{
            //    Color = Colors.Transparent,
            //    WidthRequest = 40
            //};
            //Grid.SetColumn(emptyRightColumn, 1);
            //quantityGrid.Children.Add(emptyRightColumn);

            cardContentLayout.Children.Add(quantityGrid);

            parentCard.Content = cardContentLayout;

            _childrenContainer = new VerticalStackLayout
            {
                Spacing = 8,
                Padding = new Thickness(20, 0, 10, 5),
                IsVisible = false
            };

            var childCollectionView = new CollectionView
            {
                SelectionMode = SelectionMode.None,
                ItemTemplate = CreateChildDataTemplate()
            };
            childCollectionView.SetBinding(CollectionView.ItemsSourceProperty, "SubProducts");
            _childrenContainer.Children.Add(childCollectionView);

            mainStack.Children.Add(parentCard);
            mainStack.Children.Add(_childrenContainer);

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (sender, e) =>
            {
                if (BindingContext is Product product)
                {
                    product.IsExpanded = !product.IsExpanded;
                    await ToggleChildrenAnimation(_childrenContainer, product.IsExpanded);
                }
            };

            parentCard.GestureRecognizers.Add(tapGesture);

            _childrenContainer.SetBinding(VisualElement.IsVisibleProperty, "IsExpanded");

            this.Content = mainStack;
        }

        private DataTemplate CreateChildDataTemplate()
        {
            return new DataTemplate(() =>
            {
                var childCard = new Border
                {
                    BackgroundColor = Colors.White,
                    Stroke = Color.FromArgb("E0E0E0"),
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(5) },
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 5)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var infoStack = new VerticalStackLayout
                {
                    Spacing = 2
                };

                var nameLabel = new Label();
                nameLabel.SetBinding(Label.TextProperty, "ItemId");
                nameLabel.FontSize = 12;
                nameLabel.TextColor = Color.FromArgb("#333333");

                var descLabel = new Label();
                descLabel.SetBinding(Label.TextProperty, "ItemName");
                descLabel.FontSize = 12;
                descLabel.TextColor = Color.FromArgb("#666666");

                infoStack.Children.Add(nameLabel);
                infoStack.Children.Add(descLabel);

                var priceLabel = new Label();
                priceLabel.SetBinding(Label.TextProperty, "SubQuantity");
                priceLabel.FontSize = 11;
                priceLabel.FontAttributes = FontAttributes.Bold;
                priceLabel.TextColor = Colors.Black;

                Grid.SetColumn(infoStack, 0);
                Grid.SetColumn(priceLabel, 1);

                grid.Children.Add(infoStack);
                grid.Children.Add(priceLabel);

                childCard.Content = grid;

                return childCard;
            });
        }

        private async Task ToggleChildrenAnimation(View container, bool isExpanding)
        {
            if (isExpanding)
            {
                container.TranslationY = -10;
                container.Opacity = 0;
                container.ScaleY = 0.9f;

                await Task.WhenAll(
                    container.FadeTo(1, 300, Easing.CubicOut),
                    container.TranslateTo(0, 0, 300, Easing.SpringOut),
                    container.ScaleYTo(1, 300, Easing.SpringOut)
                );
            }
            else
            {
                await Task.WhenAll(
                    container.FadeTo(0, 250, Easing.CubicIn),
                    container.TranslateTo(0, -10, 250, Easing.SpringIn),
                    container.ScaleYTo(0.9f, 250, Easing.SpringIn)
                );
            }
        }

        public class StatusColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is string status)
                {
                    return status switch
                    {
                        "部分処理" => Color.FromArgb("#FF9800"),
                        "処理中" => Color.FromArgb("#2196F3"),
                        "完了" => Color.FromArgb("#4CAF50"),
                        "未処理" => Color.FromArgb("#F44336"),
                        _ => Color.FromArgb("#9E9E9E")
                    };
                }
                return Color.FromArgb("#9E9E9E");
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}