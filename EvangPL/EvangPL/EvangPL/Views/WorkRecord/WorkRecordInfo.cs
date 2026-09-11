using EvangSol.Mobibrary.EvangModel;

namespace EvangPL.Views.WorkRecord;

public class WorkRecordHd : EvangJsonModel
{
    public virtual string orderNo { get; set; }
    public virtual string workNo { get; set; }
    public virtual string stepNo { get; set; }
    public virtual string stepName { get; set; }
    public virtual string statusName { get; set; }
    public WorkRecordHd(string orderno, string workno, string stepno, string stepname, string statusname)
    {
        orderNo = orderno;
        workNo = workno;
        stepNo = stepno;
        stepName = stepname;
        statusName = statusname;
    }
}
public class WorkRecordLine : EvangJsonModel
{
    public virtual string startDate { get; set; }
    public virtual string userName { get; set; }
    public virtual string itemName { get; set; }
    public virtual string curQty { get; set; }
    public WorkRecordLine(string startdate, string username, string itemname, string curqty)
    {
        startDate = startdate;
        userName = username;
        itemName = itemname;
        curQty = curqty;
    }
}