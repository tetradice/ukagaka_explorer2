using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SakuraBridge.Library;

namespace ShellExplorer2
{
    public class Module : PluginModule
    {
        /// <summary>
        /// メニューからの実行
        /// </summary>
        public override PluginResponse OnMenuExec(PluginRequest req)
        {
            var res = PluginResponse.OK();
            res.Event = "OnShellExplorer2Open";

            var ghostId = req.References[3];
            var ghostDirPath = req.References[4].TrimEnd('\\');
            Process.Start(Path.Combine(DLLDirPath, @"gui\ShellExplorer2GUI.exe"), "id:" + ghostId + @" """ + ghostDirPath + @"""");

            return res;
        }
    }
}
