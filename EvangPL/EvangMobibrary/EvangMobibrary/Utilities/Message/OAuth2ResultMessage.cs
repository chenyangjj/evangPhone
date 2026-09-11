using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EvangSol.Mobibrary.Utilities.Message
{
    public class OAuth2ResultMessage : ValueChangedMessage<bool>
    {
        public string? ErrorMessage { get; set; }

        public OAuth2ResultMessage(bool value) : base(value)
        {
        }
    }
}
