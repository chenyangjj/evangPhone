using EvangSol.Mobibrary.PlatformControl;

namespace EvangSol.Mobibrary.Utilities.Behavior
{
    public class AutoCompleteValidateBehavior : EvangValidateBehavior<AutoComplete>
    {
        AutoComplete? autocomplete;
        bool allowempty;

        public AutoCompleteValidateBehavior(string label, bool allowempty) : base(label, "errorAutoComplete")
        {
            this.allowempty = allowempty;
        }

        protected override void OnAttachedTo(AutoComplete autocomplete)
        {
            this.autocomplete = autocomplete;
            base.OnAttachedTo(autocomplete);
        }

        protected override void OnDetachingFrom(AutoComplete autocomplete)
        {
            base.OnDetachingFrom(autocomplete);
        }

        public override bool Validation(object? obj)
        {
            var str = obj as string;
            if (string.IsNullOrEmpty(str))
                return allowempty;
            return !string.IsNullOrEmpty(autocomplete!.Value);
        }
    }
}
