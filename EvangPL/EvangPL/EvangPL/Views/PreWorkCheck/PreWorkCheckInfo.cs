using EvangSol.Mobibrary.EvangModel;

namespace EvangPL.Views.PreWorkCheck;

public class PreWorkCheckInfo : EvangJsonModel
{
    public virtual string stepSeq { get; set; }
    public virtual string stepName { get; set; }
    public virtual string operatorUser { get; set; }
    public virtual string times { get; set; }
    public virtual string tool { get; set; }
    public virtual string equipment { get; set; }
    public virtual string memo { get; set; }
    public virtual string startTime { get; set; }
    public virtual string endTime { get; set; }
    public virtual string workTime { get; set; }
}