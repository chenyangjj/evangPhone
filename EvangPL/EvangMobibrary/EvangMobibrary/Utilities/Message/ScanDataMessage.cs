using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EvangSol.Mobibrary.Utilities.Message
{
    public class ScanDataMessage : ValueChangedMessage<string>
    {
        public bool? is1d { get; set; }

        public ScanDataMessage(string value, bool? is1d = null) : base(value)
        {
            this.is1d = is1d;
        }
    }
}
