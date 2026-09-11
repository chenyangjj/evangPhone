using EvangSol.Mobibrary.EvangModel;

namespace EvangPL.Views.ActualConfirm;

public class ActualConfirmHd : EvangJsonModel
{
    public virtual string orderNo { get; set; }
    public virtual string workNo { get; set; }
    public virtual string workName { get; set; }
    public virtual string statusName { get; set; }
    public ActualConfirmHd(string orderno, string workno, string workname, string statusname)
    {
        orderNo = orderno;
        workNo = workno;
        workName = workname;
        statusName = statusname;
    }
}
public class ActualConfirmLine : EvangJsonModel
{
    public virtual string startDate { get; set; }
    public virtual string userName { get; set; }
    public virtual string itemName { get; set; }
    public virtual string curQty { get; set; }
    public ActualConfirmLine(string startdate, string username, string itemname, string curqty)
    {
        startDate = startdate;
        userName = username;
        itemName = itemname;
        curQty = curqty;
    }
}