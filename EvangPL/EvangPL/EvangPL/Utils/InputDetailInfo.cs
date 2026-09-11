using System;

namespace EvangPL.Utils
{
    public class InputDetailInfo
    {
        public string? OrderId { get; set; }
        public string PoNo { get; set; } = "";              // 订单号
        public string SupplierName { get; set; } = "";      // 供应商名称
        public string ArrivalPlanDate { get; set; } = "";   // 入荷予定日
        public int ItemCount { get; set; }                  // 品目数
        public int TotalQty { get; set; }                   // 总数量
        public string Status { get; set; } = "";            // 状态
        public string? InboundType { get; set; }            // ✅ 入库区分
        public string? ItemCode { get; set; }               // ✅ 品目コード
        public string? ItemName { get; set; }               // ✅ 品目名称

        public InputDetailInfo() { }

        // ✅ 原有构造函数（保持兼容）
        public InputDetailInfo(string poNo, string supplierName, string arrivalPlanDate, int itemCount, int totalQty, string status)
        {
            PoNo = poNo;
            SupplierName = supplierName;
            ArrivalPlanDate = arrivalPlanDate;
            ItemCount = itemCount;
            TotalQty = totalQty;
            Status = status;
        }

        // ✅ 新增完整构造函数（包含所有属性）
        public InputDetailInfo(string poNo, string supplierName, string arrivalPlanDate, int itemCount, int totalQty, string status, string? inboundType, string? itemCode, string? itemName)
        {
            PoNo = poNo;
            SupplierName = supplierName;
            ArrivalPlanDate = arrivalPlanDate;
            ItemCount = itemCount;
            TotalQty = totalQty;
            Status = status;
            InboundType = inboundType;
            ItemCode = itemCode;
            ItemName = itemName;
        }
    }
}