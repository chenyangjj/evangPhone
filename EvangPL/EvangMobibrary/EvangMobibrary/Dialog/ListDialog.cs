using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.Utilities.Common;
using System.Reflection;

namespace EvangSol.Mobibrary.Dialog
{
    public class ListDialog<T> : EvangDialog where T : EvangJsonModel
    {
        #region Variables
        private const double CONTENT_HEIGHT = 370;
        private ListView? customListView { get; set; }
        private List<T>? itemlist { get; set; }
        #endregion

        #region Constructor
        public ListDialog(string caption, List<T> list, int width = -1) : base(caption, width, type: DialogType.Normal)
        {
            itemlist = list;
            base.Initialize();
        }

        public ListDialog(string caption, ListView cuslv, List<T> list, int width = -1) : base(caption, width, type: DialogType.Normal)
        {
            customListView = cuslv;
            itemlist = list;
            base.Initialize();
        }

        #endregion

        #region CreateContent
        //create dialog content
        public override View CreateContent()
        {
            //ListViewのみだと高さが無限となるためStackLayoutに内包する
            var layout = new StackLayout
            {
                HeightRequest = CONTENT_HEIGHT,
                Padding = new Thickness(0,0,0,10)
            };

            if(customListView == null)
            {
                int listCount = 0;
                var listView = new ListView
                {
                    SelectionMode = ListViewSelectionMode.None,

                    ItemTemplate = new DataTemplate(() =>
                    {
                        var result = CreateListView(listCount);
                        listCount++;
                        return result;
                    })
                };

                listView.ItemsSource = itemlist;
                layout.Children.Add(listView);
            }
            else
            {
                customListView.ItemsSource = itemlist;
                layout.Children.Add(customListView);
            }

            return layout;
        }
        #endregion

        #region CreateListView
        public virtual ViewCell CreateListView(int listCount) 
        {
            var grid = new Grid
            {
                Padding = new Thickness(10),
                ColumnSpacing = 20,
                RowDefinitions = { new RowDefinition { Height = GridLength.Auto } },
            };

            Type type = typeof(T);

            PropertyInfo[] properties = type.GetProperties();

            int columnIndex = 0;
            foreach (var property in properties)
            {
                //ListAttribute attr = (ListAttribute)property.GetCustomAttribute(typeof(ListAttribute))!;

                //リストダイアログへの表示項目としない属性が付与されている場合はスキップ
                //if (attr != null && !attr.IsShowInListDialog)
                //    continue;

                if (grid.ColumnDefinitions.Count <= columnIndex)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                }

                // Labelを作成してプロパティを表示
                string? text = property.GetValue(itemlist![listCount])?.ToString();
                var label = new Label
                {
                    Text = GetCustomString(text) ?? text,
                    FontSize = CommonViewSetting.LABEL_FONTSIZE,
                    TextColor = GetColor("Black"),
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.NoWrap
                };
                grid.Children.Add(label);
                Grid.SetColumn(label, columnIndex);

                columnIndex++;
            }

            return new ViewCell { View = grid };
        }
        #endregion
    }
}
