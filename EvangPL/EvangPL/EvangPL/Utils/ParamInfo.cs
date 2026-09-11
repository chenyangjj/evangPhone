using EvangSol.Mobibrary.EvangModel;

namespace EvangPL.Utils;

public class ParamInfo : EvangJsonModel
{
    // Known as JobProcInfo in LF 2.1
    // Renamed to ProcessInfoPKInfo in LF 3.0
    public ParamInfo(string processid)
    {
        processId = processid;
    }
    public virtual string processId { get; set; }
}
public class ProcessParamInfo : EvangJsonModel
{
    public int WorkOrderId { get; set; }
    public int InstanceCount { get; set; }
    public ProcessParamInfo(int id, int instanceCount)
    {
        WorkOrderId = id;
        InstanceCount = instanceCount;
    }
}
public class OrderPageInfo : EvangJsonModel
{
    public string ItemId { get; set; }
    public string Status { get; set; }
    public string StartDate { get; set; }
    public string Customer { get; set; }
    public string Department { get; set; }

    public OrderPageInfo(string itemId, string status, string startDate, string customer, string department)
    {
        ItemId = itemId;
        Status = status;
        StartDate = startDate;
        Customer = customer;
        Department = department;
    }

}
public class ProcessSearchInfo : EvangJsonModel
{
    public string OperationInfo { get; set; }
    public string Status { get; set; }
    public string StartDate { get; set; }
    public string ProcessType { get; set; }
    public string Department { get; set; }

    public ProcessSearchInfo(string operationInfo, string status, string startDate, string processType, string department)
    {
        OperationInfo = operationInfo;
        Status = status;
        StartDate = startDate;
        ProcessType = processType;
        Department = department;
    }

}