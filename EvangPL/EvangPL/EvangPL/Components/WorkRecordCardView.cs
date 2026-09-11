using EvangSol.Mobibrary.EvangModel;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvangPL.Components
{
    public class WorkRecordCardView : ContentView
    {
        private CollectionView _collectionView;

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable),
                typeof(WorkRecordCardView), null, propertyChanged: OnItemsSourceChanged);

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is WorkRecordCardView view && view._collectionView != null)
            {
                view._collectionView.ItemsSource = newValue as IEnumerable;
            }
        }
        public WorkRecordCardView()
        {
            BuildUI();
        }
        private void BuildUI()
        {
            _collectionView = new CollectionView
            {
                ItemTemplate = new DataTemplate(() =>
                {
                    var mainGrid = new Grid
                    {
                        RowDefinitions =
                        {
                            new RowDefinition { Height = GridLength.Auto }
                        },
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                        },
                        Margin = new Thickness(10, 0, 10, 5)
                    };

                    var contentBorder = new Border
                    {
                        Stroke = Color.FromArgb("#e0e0e0"),
                        StrokeThickness = 1,
                        StrokeShape = new RoundRectangle { CornerRadius = 5 },
                        BackgroundColor = Colors.White,
                        Padding = new Thickness(10),
                        HorizontalOptions = LayoutOptions.Fill
                    };

                    var orderContent = new VerticalStackLayout
                    {
                        Spacing = 5
                    };

                    var firstLineGrid = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Star },
                        },
                        Margin = new Thickness(0, 0, 0, 0)
                    };

                    var firstStartLayout = new HorizontalStackLayout();
                    var firstStartLabel = new Label
                    {
                        Text = "作成日：",
                        FontSize = 13,
                        TextColor = Colors.Gray
                    };
                    var firstStartValue = new Label
                    {
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#000000"),
                        VerticalTextAlignment = TextAlignment.End
                    };
                    firstStartValue.SetBinding(Label.TextProperty, "ValueA");
                    firstStartLayout.Children.Add(firstStartLabel);
                    firstStartLayout.Children.Add(firstStartValue);
                    Grid.SetColumn(firstStartLayout, 0);
                    firstLineGrid.Children.Add(firstStartLayout);

                    var firstEndLayout = new HorizontalStackLayout
                    {
                        HorizontalOptions = LayoutOptions.Start
                    };
                    var firstEndLabel = new Label
                    {
                        Text = "操作者：",
                        FontSize = 13,
                        TextColor = Colors.Gray
                    };
                    var firstEndValue = new Label
                    {
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#000000"),
                        VerticalTextAlignment = TextAlignment.End
                    };
                    firstEndValue.SetBinding(Label.TextProperty, "ValueB");
                    firstEndLayout.Children.Add(firstEndLabel);
                    firstEndLayout.Children.Add(firstEndValue);

                    Grid.SetColumn(firstEndLayout, 1);
                    firstLineGrid.Children.Add(firstEndLayout);

                    orderContent.Children.Add(firstLineGrid);
                    contentBorder.Content = orderContent;

                    //second line

                    var secondLineGrid = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Star },
                        },
                        Margin = new Thickness(0, 0, 0, 0)
                    };

                    var secondStartLayout = new HorizontalStackLayout();
                    var secondStartLabel = new Label
                    {
                        Text = "品目：",
                        FontSize = 13,
                        TextColor = Colors.Gray
                    };
                    var secondStartValue = new Label
                    {
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#000000"),
                        VerticalTextAlignment = TextAlignment.End
                    };
                    secondStartValue.SetBinding(Label.TextProperty, "ValueC");
                    secondStartLayout.Children.Add(secondStartLabel);
                    secondStartLayout.Children.Add(secondStartValue);
                    Grid.SetColumn(secondStartLayout, 0);
                    secondLineGrid.Children.Add(secondStartLayout);

                    var secondEndLayout = new HorizontalStackLayout
                    {
                        HorizontalOptions = LayoutOptions.Start
                    };
                    var secondEndLabel = new Label
                    {
                        Text = "数量：",
                        FontSize = 13,
                        TextColor = Colors.Gray
                    };
                    var secondEndValue = new Label
                    {
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#000000"),
                        VerticalTextAlignment = TextAlignment.End
                    };
                    secondEndValue.SetBinding(Label.TextProperty, "ValueD");
                    secondEndLayout.Children.Add(secondEndLabel);
                    secondEndLayout.Children.Add(secondEndValue);

                    Grid.SetColumn(secondEndLayout, 1);
                    secondLineGrid.Children.Add(secondEndLayout);

                    orderContent.Children.Add(secondLineGrid);
                    contentBorder.Content = orderContent;

                    mainGrid.Children.Add(contentBorder);

                    return mainGrid;
                })
            };
            this.Content = _collectionView;
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
