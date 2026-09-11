using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EvangSol.Mobibrary.Utilities.Message
{
    public class HttpTimeoutMessage : ValueChangedMessage<string?>
    {
        public string? csi_method { get; set; }
        public string? csi_service { get; set; }

        public HttpTimeoutMessage(string? value, string? method, string? service) : base(value)
        {
            csi_method = method;
            csi_service = service;
        }
    }
}
