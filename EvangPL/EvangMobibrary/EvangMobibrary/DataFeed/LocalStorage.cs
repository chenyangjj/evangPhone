using EvangSol.Mobibrary.Attributes;
using EvangSol.Mobibrary.LocalModel;
using SQLite;

namespace EvangSol.Mobibrary.DataFeed
{
    public static class LocalStorage
    {
        // local SQLite storage
        private static SQLiteConnection EvangSQLite = new SQLiteConnection(Path.Combine(FileSystem.Current.AppDataDirectory, "EvangMobibrary.db3"));
        // hold configuration
        public static ConfigLM? config;
        public static string? configFileName;

        public static void Create<T>()
        {
            EvangSQLite.CreateTable<T>(CreateFlags.ImplicitPK | CreateFlags.AutoIncPK);
        }

        public static void Insert<T>(T entry)
        {
            EvangSQLite.Insert(entry);
        }

        public static TableQuery<T> Read<T>() where T : new()
        {
            return EvangSQLite.Table<T>();
        }

        public static List<T> Query<T>(string sql) where T : new()
        {
            return EvangSQLite.Query<T>(sql);
        }

        public static T ExecuteScalar<T>(string sql)
        {
            return EvangSQLite.ExecuteScalar<T>(sql);
        }

        public static int Execute(string sql)
        {
            return EvangSQLite.Execute(sql);
        }

        public static void DeleteAll<T>()
        {
            EvangSQLite.DeleteAll<T>();
        }

        public static int LocalInsert<T>(T item) where T : EvangLM
        {
            var table = GetTableName(item);
            if (table == null)
                return 0;
            return CheckInsert(table, item);
        }

        public static int LocalInsert<T>(List<T> items) where T : EvangLM
        {
            // check list length
            if (items.Count == 0)
                return 0;
            // check table attribute and get table name
            var table = GetTableName(items[0]);
            if (table == null)
                return 0;
            // save data
            int cnt = 0;
            foreach (var item in items)
            {
                if (CheckInsert(table, item) > 0)
                    cnt++;
            }
            return cnt;
        }

        private static string? GetTableName<T>(T item) where T : EvangLM
        {
            // check table attribute and get table name
            var attrs = item.GetType().GetCustomAttributes(typeof(TableAttribute), false);
            if (attrs.Length == 0)
                return null;
            return (attrs[0] as TableAttribute)?.Name;
        }

        private static int CheckInsert<T>(string table, T item) where T : EvangLM
        {
            // check data existance
            var list = new List<string>();
            foreach (var info in item.GetType().GetProperties())
            {
                var attrs = info.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    if (attr is KeyFieldAttribute)
                    {
                        if (info.PropertyType == typeof(int) || info.PropertyType == typeof(long))
                            list.Add(string.Format(@"{0}={1}", info.Name, info.GetValue(item, null)));
                        else
                            list.Add(string.Format(@"{0}='{1}'", info.Name, info.GetValue(item, null)));
                    }
                    else if (attr is EvangSequenceAttribute)
                    {
                        //get maximum value of this field
                        int sequence = ExecuteScalar<int>(string.Format(@"select max({0}) from {1}", info.Name, table));
                        //increase and set sequence
                        sequence++;
                        info.SetValue(item, sequence);
                    }
                }
            }
            //check redundancy
            var redun = ExecuteScalar<int>(string.Format(@"select count(*) from {0} where {1}", table, string.Join(" and ", list)));
            if (redun > 0)
                return 0;
            // save data
            Insert(item);
            // get new entry's id
            int lastid = ExecuteScalar<int>(@"select last_insert_rowid()");
            return lastid;
        }

        public static bool LocalUpdate<T>(T item) where T : EvangLM
        {
            var table = GetTableName(item);
            if (table == null)
                return false;
            //
            var cond = new List<string>();
            var sets = new List<string>();
            foreach (var info in item.GetType().GetProperties())
            {
                var attrs = info.GetCustomAttributes(false);
                bool isfld = true;
                foreach (var attr in attrs)
                {
                    //key field as condition
                    if (attr is KeyFieldAttribute)
                    {
                        if (info.PropertyType == typeof(int) || info.PropertyType == typeof(long))
                            cond.Add(string.Format(@"{0}={1}", info.Name, info.GetValue(item, null)));
                        else
                            cond.Add(string.Format(@"{0}='{1}'", info.Name, info.GetValue(item, null)));
                        isfld = false;
                        continue;
                    }
                    //ignore
                    else if (attr is IgnoreAttribute)
                    {
                        isfld = false;
                        continue;
                    }
                }
                if (isfld)
                    sets.Add(string.Format(@"{0}='{1}'", info.Name, info.GetValue(item, null)));
            }
            //search the data entry with keyfields
            var where = string.Join(" and ", cond);
            var cnt = ExecuteScalar<int>(string.Format(@"select count(*) from {0} where {1}", table, where));
            if (cnt != 1)
                return false;
            //update the data entry
            EvangSQLite.Execute(string.Format(@"update {0} set {1} where {2}", table, string.Join(", ", sets), where));
            return true;
        }
    }
}
