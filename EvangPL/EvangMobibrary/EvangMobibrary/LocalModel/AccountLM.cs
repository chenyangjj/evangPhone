using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.DataFeed;
using SQLite;
using System.Text.Json.Serialization;

namespace EvangSol.Mobibrary.LocalModel
{
    [Table("Account")]
    public class AccountLM : EvangLM, IDataSelection
    {
        [EvangSequence, JsonIgnore, KeyField]
        public int seq { get; set; }

        public string? Name { get; set; }
        public string? AccountId { get; set; }
        public string? ClientId { get; set; }
        public string? Scope { get; set; }
        public string? HostName { get; set; }
        public string? OAuthPath { get; set; }
        public string? TokenPath { get; set; }
        public string? EntryPath { get; set; }

        [Ignore, JsonIgnore]
        public string? RealmId => AccountId?.ToUpper().Replace('-', '_');
        [Ignore, JsonIgnore]
        public string HostUrl => string.Format(@"https://{0}.{1}", AccountId, HostName);
        [Ignore, JsonIgnore]
        public string OAuthUrl => string.Format(@"https://{0}.{1}/{2}", AccountId, HostName, OAuthPath);
        [Ignore, JsonIgnore]
        public string TokenUrl => string.Format(@"https://{0}.{1}/{2}", AccountId, HostName, TokenPath);
        [Ignore, JsonIgnore]
        public string EntryUrl => string.Format(@"https://{0}.{1}/{2}", AccountId, HostName, EntryPath);

        [Ignore, JsonIgnore]
        public string? Key => Name;
        [Ignore, JsonIgnore]
        public string? Val { get => Name; set => _ = value; }
    }
}
