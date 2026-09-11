using EvangSol.Mobibrary.Attributes;
using SQLite;

namespace EvangSol.Mobibrary.LocalModel
{
    [Table("Config")]
    public class ConfigLM : EvangLM
    {
        [KeyField]
        public long version { get; set; }
        public string? Scanner { get; set; }
        [Ignore]
        public List<AccountLM>? Accounts { get; set; }
    }
}
