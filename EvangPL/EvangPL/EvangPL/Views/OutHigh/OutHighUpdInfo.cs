using EvangSol.Mobibrary.EvangModel;

namespace EvangPL.Views.OutHigh;

public class OutHighUpdInfo : EvangJsonModel
{
    public virtual string id { get; set; }
    public virtual string operatorUser { get; set; }
    public virtual string qty { get; set; }
    public virtual string equipment { get; set; }
    public virtual string memo { get; set; }
    public virtual string startTime { get; set; }
    public virtual string endTime { get; set; }
    public virtual string workTime { get; set; }
    public virtual bool endFlag { get; set; }
}