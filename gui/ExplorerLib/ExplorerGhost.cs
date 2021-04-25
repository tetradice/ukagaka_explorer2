using System.IO;
using System.Text;
using NiseSeriko;

namespace ExplorerLib
{
    /// <summary>
    /// エクスプローラ通で使用する情報を付加したゴースト情報クラス
    /// </summary>
    public class ExplorerGhost : NiseSeriko.Ghost
    {
        /// <summary>
        /// explorer2\descript.txt の情報
        /// </summary>
        public virtual DescriptText Explorer2Descript { get; set; }

        /// <summary>
        /// explorer2\descript.txt のファイルパス
        /// </summary>
        public virtual string Explorer2DescriptPath { get { return Path.Combine(DirPath, @"ghost\master\explorer2\descript.txt"); } }

        /// <summary>
        /// explorer2\character_descript.txt のファイルパス
        /// </summary>
        public virtual string CharacterDescriptPath { get { return Path.Combine(DirPath, @"ghost\master\explorer2\character_descript.txt"); } }

        /// <summary>
        /// explorer2\character_descript.txt の本文
        /// </summary>
        public virtual string CharacterDescript { get; set; }

        /// <summary>
        /// sakura側のデフォルトサーフェスID
        /// </summary>
        public override int SakuraDefaultSurfaceId
        {
            get
            {
                // explorer2\descript.txt 内で sakura.defaultsurface が指定されていればその値
                {
                    int parsed;
                    if (Explorer2Descript != null
                        && Explorer2Descript.Values.ContainsKey("sakura.defaultsurface")
                        && int.TryParse(Explorer2Descript.Get("sakura.defaultsurface"), out parsed))
                    {
                        return parsed;
                    }
                }

                // 上記以外の場合は親処理 (ゴーストのdescript.txtで指定されたID or 0)
                return base.SakuraDefaultSurfaceId;
            }
        }

        /// <summary>
        /// kero側のデフォルトサーフェスID
        /// </summary>
        public override int KeroDefaultSurfaceId
        {
            get
            {
                // explorer2\descript.txt 内で kero.defaultsurface が指定されていればその値
                {
                    int parsed;
                    if (Explorer2Descript != null
                        && Explorer2Descript.Values.ContainsKey("kero.defaultsurface")
                        && int.TryParse(Explorer2Descript.Get("kero.defaultsurface"), out parsed))
                    {
                        return parsed;
                    }
                }

                // 上記以外の場合は親処理 (ゴーストのdescript.txtで指定されたID or 10)
                return base.KeroDefaultSurfaceId;
            }
        }

        public static new ExplorerGhost Load(string dirPath)
        {
            var ghost = new ExplorerGhost() { DirPath = dirPath };
            ghost.Load();
            return ghost;
        }

        public override void Load()
        {
            base.Load();

            // explorer2\descript.txt 読み込み (存在すれば)
            Explorer2Descript = null;
            if (File.Exists(Explorer2DescriptPath))
            {
                Explorer2Descript = DescriptText.Load(Explorer2DescriptPath);
            }

            // character_descript.txt があれば読み込み
            CharacterDescript = null;
            if (File.Exists(CharacterDescriptPath))
            {
                CharacterDescript = File.ReadAllText(CharacterDescriptPath, Encoding.UTF8);
            }
        }

        /// <summary>
        /// descript.txt 更新日時を設定する
        /// </summary>
        protected override void UpdateDescriptLastModified()
        {
            base.UpdateDescriptLastModified();

            // explorer2/descript.txt 更新日付
            if (Explorer2Descript != null
                && Explorer2Descript.LastWriteTime > DescriptLastModified)
            {
                DescriptLastModified = Explorer2Descript.LastWriteTime; // 新しければセット
            }
        }
    }
}
