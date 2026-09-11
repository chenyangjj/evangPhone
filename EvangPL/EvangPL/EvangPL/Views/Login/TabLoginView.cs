using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.EvangView;

namespace EvangPL.Views.Login
{
    public class TabLoginView : BindableBrokerView
    {
        #region left
        //[Control(
        //    Height = 150
        //    )]
        //public EvangElement<StackLayout>? stacklayout { get; set; }
        [Control(
            Height = 150
            )]
        public EvangElement<BoxView>? stacklayout { get; set; }

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
        #endregion

        #region right
        [Control(
            Location = 1,
            Height = 150
            )]
        public EvangElement<BoxView>? block { get; set; }

        [Composite(
            Location = 1,
            Label = "strLogin",
            Width = 300,
            TextColor = "White"
            )]
        public EvangElement<Button>? login { get; set; }

        [Composite(
            Location = 1,
            Label = "",
            LabelWidth = 210,
            TextAlignment = "Start"
            )]
        public EvangElement<LabelComposite>? appVer { get; set; }

        //[Composite(
        //    Location = 1,
        //    Label = "lblClientId",
        //    LabelWidth = 210,
        //    TextAlignment = "Start"
        //    )]
        //public EvangElement<LabelComposite>? clientId { get; set; }
        #endregion
    }
}
