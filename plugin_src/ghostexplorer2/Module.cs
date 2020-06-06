using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SakuraBridge.Library;

namespace GhostExplorer2
{
    public class Module : PluginModule
    {
        /// <summary>
        /// メニューからの実行
        /// </summary>
        public override PluginResponse OnMenuExec(PluginRequest req)
        {
            var res = PluginResponse.OK();
            res.Event = "OnGhostExplorer2Open";

            var ghostId = req.References[3];
            Process.Start(Path.Combine(DLLDirPath, @"gui\GhostExplorer2GUI.exe"), "id:" + ghostId);

            return res;
        }
    }
}
