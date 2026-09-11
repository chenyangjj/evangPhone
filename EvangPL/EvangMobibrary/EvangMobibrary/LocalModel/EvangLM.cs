using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.Utilities.Common;
using System.Reflection;
using EvangSol.Mobibrary.EvangModel;

namespace EvangSol.Mobibrary.LocalModel
{
    public class EvangLM
    {
        public static T Create<T>(EvangJsonModel jsonmodel) where T : EvangLM, new()
        {
            return BaseUtils.ValueCopyFrom(new T(), jsonmodel);
        }

        public static void CreateLocalTables(List<Assembly> assembles)
        {
            foreach (Assembly asm in assembles)
            {
                var models = from x in asm.GetTypes() where x.IsClass && x.BaseType == typeof(EvangLM) select x;
                var create = typeof(LocalStorage).GetMethod(nameof(LocalStorage.Create));
                if (create != null)
                {
                    foreach (Type T in models)
                    {
                        var method = create.MakeGenericMethod(T);
                        method.Invoke(null, null);
                    }
                }
            }
        }

        public static void ClearLocalTables(List<Assembly> assembles)
        {
            var delete = typeof(LocalStorage).GetMethod(nameof(LocalStorage.DeleteAll));
            if (delete == null)
                return;
            foreach (Assembly asm in assembles)
            {
                var models = from x in asm.GetTypes() where x.IsClass && x.BaseType == typeof(EvangLM) select x;
                foreach (Type T in models)
                {
                    var method = delete.MakeGenericMethod(T);
                    method.Invoke(null, null);
                }
            }
        }
    }
}
