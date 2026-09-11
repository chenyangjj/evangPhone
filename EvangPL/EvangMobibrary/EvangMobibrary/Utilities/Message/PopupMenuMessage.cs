using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EvangSol.Mobibrary.Utilities.Message
{
    public class PopupMenuMessage : ValueChangedMessage<string>
    {
        public PopupMenuMessage(string value) : base(value)
        {
        }
    }
}
