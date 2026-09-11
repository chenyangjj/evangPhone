using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Utilities.Behavior
{
    //for validation when retrieving data from input controls
    public interface IInputValidate
    {
        public bool Validation(object? obj);
        public string ErrorMessage { get; set; }
    }

    //for decimal format setting
    public interface IDecimalFormat
    {
        //use unit's format
        public void SetUnit(string unit, BindableBrokerView bpvm);
        //use designated format
        public void SetFormat(string format);
    }

    public abstract class EvangValidateBehavior<T> : Behavior<T>, IInputValidate where T : BindableObject
    {
        public const string default_error_message = "There is a validation error but no error message to show for {0}.";

        public EvangValidateBehavior(string label, string? errid = null)
        {
            ControlLabel = label;
            ErrorMessage = GetErrorMessage(errid, ControlLabel);
        }

        public string ControlLabel { get; set; }

        public abstract bool Validation(object? obj);

        public string ErrorMessage { get; set; }

        public string GetErrorMessage(string? errid, params string[] args) =>
            string.Format(errid == null ? default_error_message : BaseUtils.GetCustomString(errid) ?? default_error_message, args);
    }
}
