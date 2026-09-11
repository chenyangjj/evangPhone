using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvangSol.Mobibrary.Utilities.Common
{
    public class VirtualFilePath
    {
        private string? path;
        private string? filename;
        private string? extension;

        public VirtualFilePath(string vfpath)
        {
            VFPath = vfpath;
            //SIR0175498
            if (string.IsNullOrEmpty(vfpath))
                return;
            //SIR#0187653
            //MAUIアプリの実行OSによってSystem.IO.Pathの挙動が変わってしまうため手動でパスを分解する。
            if (vfpath.LastIndexOf('/') != -1)
            {
                //Linux,android,iOSの場合
                var ind = vfpath.LastIndexOf('/');

                path = vfpath.Substring(0, ind);
                filename = vfpath.Substring(ind + 1);
                extension = filename!.Substring(filename.LastIndexOf('.') + 1);
            }
            else if (vfpath.LastIndexOf('\\') != -1)
            {
                //Windowsの場合
                var ind = vfpath.LastIndexOf('\\');

                path = vfpath.Substring(0, ind);
                filename = vfpath.Substring(ind + 1);
                extension = filename!.Substring(filename.LastIndexOf('.') + 1);
            }


        }

        public string? VFPath { get; set; }
        public string? Path => path;
        public string? FName => filename;
        public string? Ext => extension;
        public virtual string? LocalPath => System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, filename!);
    }
}
