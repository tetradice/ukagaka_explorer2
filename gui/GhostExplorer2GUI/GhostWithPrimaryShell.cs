using ExplorerLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GhostExplorer2
{
    /// <summary>
    /// ゴースト情報クラス (GhostExplorer2 用に表示対象シェルの読み込み機能を追加)
    /// </summary>
    public class GhostWithPrimaryShell : ExplorerLib.Ghost
    {
        /// <summary>
        /// 表示対象のシェル
        /// </summary>
        public virtual Shell Shell { get; protected set; }

        public static new GhostWithPrimaryShell Load(string dirPath)
        {
            var ghost = new GhostWithPrimaryShell() { DirPath = dirPath };
            ghost.Load();
            return ghost;
        }

        public override void Load()
        {
            base.Load();

            // シェルを読み込み
            this.Shell = Shell.Load(Path.Combine(DirPath, CurrentShellRelDirPath), SakuraDefaultSurfaceId, KeroDefaultSurfaceId);
        }
    }
}
