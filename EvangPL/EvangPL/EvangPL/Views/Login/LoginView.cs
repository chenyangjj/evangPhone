using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.EvangView;

namespace EvangPL.Views.Login
{
    public class LoginView : BindableBrokerView
    {
        [Composite(
            Label = "strAccount",
            LabelWidth = 200,
            Required = true
            )]
        public EvangElement<DropDownSelectComposite>? suitename { get; set; }

        [Composite(
            Label = "strAccountID",
            LabelWidth = 200
            )]
        public EvangElement<LabelComposite>? accountid { get; set; }

        [Composite(
            Label = "URL",
            LabelWidth = 200
            )]
        public EvangElement<LabelComposite>? hosturl { get; set; }

        [Composite(
            Label = "strLogin",
            Width = 300,
            TextColor = "White"
            )]
        public EvangElement<Button>? login { get; set; }

        [Composite(
            Label = "",
            LabelWidth = 210,
            TextAlignment = "Start"
            )]
        public EvangElement<LabelComposite>? appVer { get; set; }
    }
}
