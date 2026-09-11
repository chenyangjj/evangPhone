using EvangSol.Mobibrary.EvangModel;

namespace EvangPL.Views.Investment;

public class HariDashiUpdInfo : EvangJsonModel
{
    public virtual string locationId { get; set; }
    public virtual string tranlocaId { get; set; }
    public virtual string ordId { get; set; }
    public virtual string itemId { get; set; }
    public virtual string qty { get; set; }
    public virtual string lotNo { get; set; }
    public virtual string reason { get; set; }
}