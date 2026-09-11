namespace EvangPL.Utils
{
    public class InventoryTransferInfo
    {
        public string OrderNo { get; set; } = "";           // 订单号
        public string CustomerName { get; set; } = "";      // 顾客名称
        public string ScheduleDate { get; set; } = "";      // 出荷予定日
        public int ItemCount { get; set; }                  // 品目数
        public int TotalQty { get; set; }                   // 总数量
        public string Status { get; set; } = "";            // 状态

        public InventoryTransferInfo() { }

        public InventoryTransferInfo(string orderNo, string customerName, string scheduleDate, int itemCount, int totalQty, string status)
        {
            OrderNo = orderNo;
            CustomerName = customerName;
            ScheduleDate = scheduleDate;
            ItemCount = itemCount;
            TotalQty = totalQty;
            Status = status;
        }
    }
}