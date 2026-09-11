using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.LocalModel;

namespace EvangSol.Mobibrary.DataFeed
{
    public class LocalMemory
    {
        public static AccountLM? Account { get; set; }

        public static Dictionary<string, string?> restlets = new();
        public static Dictionary<string, object?> master = new();

        public static void SetMaster(string key, object? obj)
        {
            master[key] = obj;
        }

        public static object? GetMaster(string key)
        {
            if (!master.ContainsKey(key))
                throw new Exception($"No such master data of name {key}");
            return master[key];
        }
    }

    #region master data structures
    public interface IDataSelection
    {
        public string? Key { get; }
        public string? Val { get; set; }
        public string? Sel => Val;
    }

    public abstract class DataSelect : EvangJsonModel, IDataSelection
    {
        public abstract string? Key { get; } 

        public abstract string? Val { get; set; }
    }

    public class BlankSelect : DataSelect
    {
        public override string? Key => string.Empty;

        public override string? Val
        {
            get => string.Empty;
            set => _ = value;
        }
    }

    public class RestletInfo : BlankSelect
    {
        public string? restlet_id { get; set; }
        public string? restlet_url { get; set; }
    }

    public class MasterParams : BlankSelect
    {
        public string? paramid { get; set; }
        public string? paramval { get; set; }
    }

    public class MasterLocation : DataSelect
    {
        public string? sectCd { get; set; }
        public string? sectSn { get; set; }
        public string? factSite { get; set; }

        public override string? Key => sectCd;
        public override string? Val
        {
            get => sectSn;
            set => sectSn = value;
        }
    }

    public class MasterReason : DataSelect
    {
        public string? rsnCd { get; set; }
        public string? reason { get; set; }
        public string? rsnTyp { get; set; }
        public string? trn1 { get; set; }
        public string? trn2 { get; set; }
        public string? trn3 { get; set; }
        public string? trnTyp { get; set; }

        public override string? Key => rsnCd;
        public override string? Val
        {
            get => reason;
            set => reason = value;
        }
    }

    public class MasterCode : DataSelect
    {
        public string? fldId { get; set; }
        public string? sysDefCd { get; set; }
        public string? cdTxt { get; set; }
        public string? usrDefCd { get; set; }

        public override string? Key => sysDefCd;
        public override string? Val
        {
            get => cdTxt;
            set => cdTxt = value;
        }
    }

    public class MasterContainer : DataSelect
    {
        public string? contnrCd { get; set; }
        public string? prmWhCd { get; set; }
        public string? lctCd { get; set; }
        public string? contnrSize { get; set; }

        public override string? Key => contnrCd;
        public override string? Val
        {
            get => contnrCd;
            set => _ = value;
        }
    }

    public class MasterLocat : DataSelect
    {
        public string? fwhcd { get; set; }
        public string? flctcd { get; set; }
        public string? flctName { get; set; }

        public override string? Key => flctcd;
        public override string? Val
        {
            get => string.IsNullOrEmpty(flctName) ? flctcd : flctcd + " " + flctName;
            set => _ = value;
        }
    }

    public class MasterUnitExchg : BlankSelect
    {
        public string? itemNo { get; set; }
        public string? frUnit { get; set; }
        public string? toUnit { get; set; }
        public string? frQty { get; set; }
        public string? toQty { get; set; }
    }

    public class MasterWeighContnr : DataSelect
    {
        public string? contnrCd { get; set; }
        public string? look { get; set; }
        public string? tareWgt { get; set; }
        public string? wgtUnit { get; set; }
        public string? wgtUnitSn { get; set; }
        public string? tareEditFlg { get; set; }

        public override string? Key => look;
        public override string? Val
        {
            get => contnrCd;
            set => _ = value;
        }
    }

    public class MasterTag : DataSelect
    {
        public string? tagNo { get; set; }
        public string? eqpmtCd { get; set; }
        public string? factSite { get; set; }
        public string? tagName { get; set; }
        public string? lcId { get; set; }
        public string? procTyp { get; set; }
        public string? tagFlg { get; set; }
        public string? unit { get; set; }
        public string? minValue { get; set; }
        public string? maxValue { get; set; }
        public string? dbName { get; set; }
        public string? customSql { get; set; }

        public override string? Key => tagNo;
        public override string? Val
        {
            get => string.IsNullOrEmpty(tagName) ? tagNo : tagNo + " " + tagName;
            set => _ = value;
        }
    }

    public class MasterUnit : DataSelect
    {
        public string? unit { get; set; }
        public string? unitsn { get; set; }
        public string? qtyFormat { get; set; }

        public override string? Key => unit;
        public override string? Val
        {
            get => unitsn;
            set => _ = value;
        }
    }

    public class MasterQCRsltJdgCode : DataSelect
    {
        public string? qcrsltjgcd { get; set; }
        public string? qcrsltjgcdnm { get; set; }
        public string? qcrsltjggrp { get; set; }
        public string? ngflg { get; set; }

        public override string? Key => qcrsltjgcd;
        public override string? Val
        {
            get => qcrsltjgcdnm;
            set => qcrsltjgcdnm = value;
        }
    }
    #endregion
}
