using SakuraBridge.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostExplorer2
{
    public class Module : PluginModule
    {
        /// <summary>
        /// プラグインのバージョン (versionリクエストに対して返す値)
        /// </summary>
        public override string Version
        {
            get { return "GhostExplorer-0.4.0"; }
        }

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
