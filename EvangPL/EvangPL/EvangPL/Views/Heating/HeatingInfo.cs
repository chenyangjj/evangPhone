using EvangSol.Mobibrary.EvangModel;

namespace EvangPL.Views.Heating;

public class HeatingInfo : EvangJsonModel
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
    public virtual string inputItem { get; set; }
    public virtual string inputItemQty { get; set; }
    public virtual string inputRst { get; set; }
}