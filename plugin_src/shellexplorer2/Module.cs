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
        protected override PluginResponse OnMenuExec(PluginRequest req, IList<string> hwnds, string ghostName, string currentShellName, string id, string ghostPath)
        {
            var res = PluginResponse.OK();
            res.Event = "OnShellExplorer2Open";

            Process.Start(Path.Combine(DLLDirPath, @"gui\ShellExplorer2GUI.exe"), "id:" + id + @" """ + ghostPath.TrimEnd('\\') + @"""");

            return res;
        }
    }
}
