using ExplorerLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GhostExplorer2
{
    public class Ghost
    {
        /// <summary>
        /// ゴーストのルートフォルダ (直下 ghost, shell が入っているフォルダ) のパス 
        /// </summary>
        public virtual string DirPath { get; set; }

        /// <summary>
        /// ゴースト達を格納しているフォルダ のパス 
        /// </summary>
        public virtual string GhostBaseDirPath { get { return Path.GetDirectoryName(DirPath); } }


        public virtual string MasterGhostDirParh { get { return Path.Combine(DirPath, @"ghost\master"); } }
        public virtual string MasterGhostDesciptParh { get { return Path.Combine(MasterGhostDirParh, "descript.txt"); } }
        public virtual string MasterShellDirPath { get { return Path.Combine(DirPath, @"shell\master"); } }
        public virtual string MasterShellDesciptPath { get { return Path.Combine(MasterShellDirPath, "descript.txt"); } }

        public virtual string CharacterDescriptPath { get { return Path.Combine(DirPath, @"ghost\master\explorer2\character_descript.txt"); } }
        public virtual string CharacterDescript { get; set; }


        public virtual DescriptText MasterGhostDescript { get; protected set; }
        public virtual Shell Shell { get; protected set; }

        public virtual string Name { get { return this.MasterGhostDescript.Get("name"); } }
        public virtual string SakuraName { get { return this.MasterGhostDescript.Get("sakura.name") ?? ""; } } // 未設定の場合は空文字
        public virtual string KeroName { get { return this.MasterGhostDescript.Get("kero.name") ?? ""; } } // 未設定の場合は空文字

        /// <summary>
        /// sakura側のデフォルトサーフェスID
        /// </summary>
        public virtual int SakuraDefaultSurfaceId
        {
            get
            {
                // descript.txt 内で sakura.seriko.defaultsurface が指定されていればその値
                int parsed;
                if (MasterGhostDescript.Values.ContainsKey("sakura.seriko.defaultsurface")
                    && int.TryParse(MasterGhostDescript.Get("sakura.seriko.defaultsurface"), out parsed))
                {
                    return parsed;
                }

                // 上記以外の場合は標準
                return 0;
            }
        }

        /// <summary>
        /// kero側のデフォルトサーフェスID
        /// </summary>
        public virtual int KeroDefaultSurfaceId
        {
            get
            {
                // descript.txt 内で kero.seriko.defaultsurface が指定されていればその値
                int parsed;
                if (MasterGhostDescript.Values.ContainsKey("kero.seriko.defaultsurface")
                    && int.TryParse(MasterGhostDescript.Get("kero.seriko.defaultsurface"), out parsed))
                {
                    return parsed;
                }

                // 上記以外の場合は標準
                return 10;
            }
        }


        public static Ghost Load(string dirPath)
        {
            var ghost = new Ghost() { DirPath = dirPath };
            ghost.Load();
            return ghost;
        }
    
        public static bool IsGhostDir(string dirPath)
        {
            // ghost/master/descript.txt と shell/master/descript.txt が存在するなら有効なゴーストフォルダとみなす
            var ghostDesc = Path.Combine(dirPath, "ghost/master/descript.txt");
            var shellDesc = Path.Combine(dirPath, "shell/master/descript.txt");
            return (File.Exists(ghostDesc) && File.Exists(shellDesc));
        }

        public virtual void Load()
        {
            // descript.txt 読み込み
            this.MasterGhostDescript = DescriptText.Load(this.MasterGhostDesciptParh);

            // character_descript.txt があれば読み込み
            CharacterDescript = null;
            if (File.Exists(CharacterDescriptPath))
            {
                CharacterDescript = File.ReadAllText(CharacterDescriptPath, Encoding.UTF8);
            }

            // profile\ghost.dat が存在すれば、その中から最終選択シェルを取得
            var shellRelPath = @"shell\master\";
            var ghostProfPath = Path.Combine(DirPath, "ghost/master/profile/ghost.dat");
            if (File.Exists(ghostProfPath))
            {
                try
                {
                    var lines = File.ReadAllLines(ghostProfPath);
                    foreach(var line in lines)
                    {
                        if (line.StartsWith("shell,"))
                        {
                            var tokens = line.TrimEnd().Split(',');
                            shellRelPath = tokens[1];
                            break;
                        }
                    }
                } catch(Exception)
                {
                }
            }

            // シェルを読み込み
            this.Shell = Shell.Load(Path.Combine(DirPath, shellRelPath), SakuraDefaultSurfaceId, KeroDefaultSurfaceId);
        }
    }
}
