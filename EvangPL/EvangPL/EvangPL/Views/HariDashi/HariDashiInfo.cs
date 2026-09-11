using EvangSol.Mobibrary.EvangModel;

namespace EvangPL.Views.OutHigh;

public class HariDashiInfo : EvangJsonModel
{
    public virtual string ordId { get; set; }
    public virtual string ordName { get; set; }
    public virtual string operationName { get; set; }
    public virtual string itemId { get; set; }
    public virtual string itemName { get; set; }
}