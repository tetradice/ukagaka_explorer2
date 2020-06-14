using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NiseSeriko;

namespace NiseSeriko
{
    /// <summary>
    /// ゴースト情報クラス。descript.txt の情報などを保持する
    /// </summary>
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
        public virtual string MasterGhostDirParh { get { return Path.Combine(DirPath, "ghost/master"); } }
        public virtual string MasterGhostDesciptParh { get { return Path.Combine(MasterGhostDirParh, "descript.txt"); } }

        /// <summary>
        /// デフォルトシェルのフォルダ名 (通常はmaster)
        /// </summary>
        public virtual string DefaultShellDirName
        {
            get
            {
                // descript.txt に seriko.defaultsurfacedirectoryname の設定があればそれを優先
                if (!string.IsNullOrEmpty(MasterGhostDescript.Get("seriko.defaultsurfacedirectoryname")))
                {
                    return MasterGhostDescript.Get("seriko.defaultsurfacedirectoryname");
                }
                else
                {
                    return "master";
                }
            }
        }

        public virtual string DefaultShellDirPath { get { return Path.Combine(DirPath, "shell", DefaultShellDirName); } }
        public virtual string DefaultShellDescriptPath { get { return Path.Combine(DefaultShellDirPath, "descript.txt"); } }

        public virtual DescriptText MasterGhostDescript { get; protected set; }

        /// <summary>
        /// ゴーストが持つ descript.txt の最終更新日時
        /// </summary>
        /// <remarks>
        /// シェル関連のキャッシュ管理に使用
        /// Load時に、下記の更新日付から最も新しい日付をセットする
        /// 
        /// * descript.txt の更新日時
        /// * (あれば) explorer2/descript.txt の更新日時
        /// </remarks>
        public virtual DateTime DescriptLastModified { get; set; }

        public virtual string Name { get { return MasterGhostDescript.Get("name"); } }
        public virtual string SakuraName { get { return MasterGhostDescript.Get("sakura.name") ?? ""; } } // 未設定の場合は空文字
        public virtual string KeroName { get { return MasterGhostDescript.Get("kero.name") ?? ""; } } // 未設定の場合は空文字

        /// <summary>
        /// 最終使用シェルの、ゴーストルートフォルダ基準の相対パス (profile.datから読み込む) 。
        /// 未起動の場合はデフォルトシェルとなる。またデフォルトシェルが存在しない場合、ゴーストが持つシェル1つを自動設定する（SSP準拠）
        /// </summary>
        public virtual string CurrentShellRelDirPath { get; set; }

        /// <summary>
        /// sakura側のデフォルトサーフェスID
        /// </summary>
        public virtual int SakuraDefaultSurfaceId
        {
            get
            {
                // descript.txt 内で sakura.seriko.defaultsurface が指定されていればその値
                {
                    int parsed;
                    if (MasterGhostDescript.Values.ContainsKey("sakura.seriko.defaultsurface")
                        && int.TryParse(MasterGhostDescript.Get("sakura.seriko.defaultsurface"), out parsed))
                    {
                        return parsed;
                    }
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
                {
                    int parsed;
                    if (MasterGhostDescript.Values.ContainsKey("kero.seriko.defaultsurface")
                        && int.TryParse(MasterGhostDescript.Get("kero.seriko.defaultsurface"), out parsed))
                    {
                        return parsed;
                    }
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
            // ghost/master/descript.txt が存在するならゴーストフォルダとみなす
            var ghostDesc = Path.Combine(dirPath, "ghost/master/descript.txt");
            return (File.Exists(ghostDesc));
        }

        public virtual void Load()
        {
            // descript.txt 読み込み
            MasterGhostDescript = DescriptText.Load(MasterGhostDesciptParh);


            // 現在シェルの決定
            {
                CurrentShellRelDirPath = null; // 一度初期化

                // profile\ghost.dat が存在すれば、その中から最終選択シェルを取得
                var ghostProfPath = Path.Combine(DirPath, @"ghost/master/profile/ghost.dat");
                if (File.Exists(ghostProfPath))
                {
                    try
                    {
                        var lines = File.ReadAllLines(ghostProfPath);
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("shell,"))
                            {
                                var tokens = line.TrimEnd().Split(',');
                                CurrentShellRelDirPath = tokens[1].TrimEnd('\\').Replace('\\', '/'); // 最後の\を除去し、\を/に変換
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }

                // 最終選択シェルの情報がない場合は、デフォルトシェル (通常は shell\master) を現在シェルとする
                // デフォルトシェルが存在しない場合は、shellフォルダ内にあるシェル1つを現在シェルとする（SSP準拠）。この場合の優先順は不定
                if (CurrentShellRelDirPath == null)
                {
                    if (File.Exists(DefaultShellDescriptPath))
                    {
                        // デフォルトシェルが存在する場合は、デフォルトシェルを選択
                        CurrentShellRelDirPath = "shell/" + DefaultShellDirName;
                    }
                    else
                    {
                        // デフォルトシェルが存在しない場合は、shellフォルダの中を探して最初に見つかったシェルを使う
                        var shellDir = Path.Combine(DirPath, "shell");
                        foreach (var shellSubDir in Directory.GetDirectories(shellDir))
                        {
                            var descriptPath = Path.Combine(shellSubDir, "descript.txt");
                            if (File.Exists(descriptPath))
                            {
                                CurrentShellRelDirPath = "shell/" + Path.GetFileName(shellSubDir);
                                break;
                            }
                        }

                    }
                }
            }

            // descript.txt 更新日時の設定
            UpdateDescriptLastModified();
        }

        /// <summary>
        /// descript.txt 更新日時を設定する
        /// </summary>
        protected virtual void UpdateDescriptLastModified()
        {
            // descript.txt 更新日付
            DescriptLastModified = MasterGhostDescript.LastWriteTime;
        }
    }
}
