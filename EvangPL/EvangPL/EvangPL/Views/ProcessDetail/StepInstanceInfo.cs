using EvangSol.Mobibrary.EvangModel;

namespace EvangPL.Views.ProcessDetail;

public class StepInstanceInfo : EvangJsonModel
{
    public virtual string strId { get; set; }
    public virtual string operationId { get; set; }
    public virtual string stepSeq { get; set; }
    public virtual string stepName { get; set; }
    public virtual string inputItem { get; set; }
    public virtual string inputItemQty { get; set; }
    public virtual string outputItem { get; set; }
    public virtual string outputItemQty { get; set; }
    public virtual string stepType { get; set; }
    public virtual string condition { get; set; }
    public virtual string memo { get; set; }
    public virtual string needInspection { get; set; }
    public virtual string stdtime { get; set; }
    public virtual string tool { get; set; }
    public virtual string skilltype { get; set; }
    public virtual string statusName { get; set; }
}