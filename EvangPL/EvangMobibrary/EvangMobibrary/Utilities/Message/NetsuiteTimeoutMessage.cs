using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EvangSol.Mobibrary.Utilities.Message
{
    public class NetsuiteTimeoutMessage : ValueChangedMessage<string?>
    {
        public string? restlet_id { get; set; }

        public NetsuiteTimeoutMessage(string? value, string? id) : base(value)
        {
            restlet_id = id;
        }
    }
}
