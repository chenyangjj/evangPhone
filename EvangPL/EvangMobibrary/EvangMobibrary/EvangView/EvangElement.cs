using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.EvangWidget;
using System.Collections.ObjectModel;
using System.Reflection;

namespace EvangSol.Mobibrary.EvangView
{
    public class EvangElement
    {
        public string? PropName { get; set; }

        public object? ElementObject { get; set; }

        public BindableBrokerView? ViewModel { get; set; }

        public string? Value { get; set; } = string.Empty;

        public ObservableCollection<TableEntryView>? List { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            if (obj is EvangElement)
                return Value == (obj as EvangElement)!.Value;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void SetValue(string strval)
        {
            Value = strval;
            if (PropName != null && ViewModel != null)
            {
                ViewModel.SetPropMemberValue(PropName, strval);
                //in SingleViewCarousel, the overriden properties can not be set by OnPropertyChanged, so set it directly
                if (ViewModel is SwipeEntryView && ElementObject is IInputControl iic && iic.ShowOnly)
                    iic.Value = strval;
            }
        }

        // for hiding column's content
        public void SetCellVisibility(string name, bool visible)
        {
            if (ViewModel is TableEntryView dgem)
                dgem.SetCellVisibility(name, visible);
        }

        //below for holding control status in CarouselView on Android
        #region control states
        public bool IsVisible { get; set; } = true;
        public void SetVisible(bool isvisible)
        {
            IsVisible = isvisible;
            if (ElementObject is View view)
                view.IsVisible = isvisible;
        }

        public bool ShowOnly { get; set; }
        public void SetShowOnly(bool showonly)
        {
            ShowOnly = showonly;
            if (ElementObject is IInputControl iic)
                iic.ShowOnly = showonly;
        }
        #endregion

        //cache for AutoComplete/UnifiedDropDown in SingleColumnCarouselTemplatePage
        public List<object>? Options { get; set; }

        public string MessageLabel
        {
            get
            {
                var veattr = GetType().GetCustomAttribute<ControlAttribute>();
                if (veattr == null)
                    return string.Empty;
                if (veattr is CompositeAttribute cveattr)
                    return cveattr.MessageLabel ?? veattr.Label;
                return veattr.Label;
            }
        }
    }

    public class EvangElement<V> : EvangElement where V : View
    {
        public V? Control
        {
            get => (V?)ElementObject;
            set => ElementObject = value;
        }

        //public override string ToString()
        //{
        //    return Value ?? "";
        //}
    }

    public class DataGridViewElement<T> : EvangElement where T : TableEntryView, new()
    {
        public EvangDataTable<T>? DataGrid
        {
            get => (EvangDataTable<T>?)ElementObject;
            set => ElementObject = value;
        }
    }

    public class @SubViewElement<T> : EvangElement<T> where T : View
    {
    }
}
