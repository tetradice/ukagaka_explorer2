using ExplorerLib;
using ExplorerLib.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShellExplorer2
{
    public class ShellManager
    {
        public virtual string GhostDirPath { get; set; }
        public virtual IList<Shell> Shells { get; protected set; }

        public static ShellManager Load(string ghostDirPath, int sakuraSurfaceId, int keroSurfaceId)
        {
            var manager = new ShellManager() { GhostDirPath = ghostDirPath };

            var sw = new Stopwatch();
            sw.Start();
            manager.Load(sakuraSurfaceId, keroSurfaceId);
            sw.Stop();
            Debug.WriteLine(string.Format("ShellManager.Load : {0}", sw.Elapsed));
            return manager;
        }

        public ShellManager()
        {
            Shells = new List<Shell>();
        }

        /// <summary>
        /// シェル情報の一括読み込み
        /// </summary>
        public virtual void Load(int sakuraSurfaceId, int keroSurfaceId)
        {
            // 既存の値はクリア
            Shells.Clear();

            // シェルフォルダを列挙
            foreach (var subDir in Directory.GetDirectories(Path.Combine(GhostDirPath, "shell")))
            {
                // descript.txt が存在しないならスキップ
                if (!File.Exists(Path.Combine(subDir, "descript.txt"))) continue;

                // シェルを読み込み
                var shell = Shell.Load(subDir, sakuraSurfaceId, keroSurfaceId);

                // リストに追加
                Shells.Add(shell);
            }

            // 最後に名前＋フォルダパス順でソート
            Shells = Shells.OrderBy(s => Tuple.Create(s.Name, s.DirPath)).ToList();
        }

        /// <summary>
        /// sakura側サーフェス画像を取得 （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        public virtual Bitmap DrawSakuraSurface(Shell targetShell)
        {
            return DrawSurfaceInternal(targetShell, targetShell.SakuraSurfaceModel, targetShell.SakuraSurfaceId);
        }

        /// <summary>
        /// kero側サーフェス画像を取得  （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        public virtual Bitmap DrawKeroSurface(Shell targetShell)
        {
            return DrawSurfaceInternal(targetShell, targetShell.KeroSurfaceModel, targetShell.KeroSurfaceId);
        }

        /// <summary>
        /// サーフェス画像を取得 （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        protected virtual Bitmap DrawSurfaceInternal(Shell targetShell, Shell.SurfaceModel surfaceModel, int surfaceId)
        {
            var cacheDir = Util.GetCacheDirPath();
            if (surfaceModel == null) return null;

            // キャッシュフォルダが存在しなければ作成
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            // 立ち絵画像のキャッシュがあり、更新日時がシェルの更新日以降なら、キャッシュを使用
            var cachePath = Path.Combine(cacheDir, string.Format("{0}_s{1}.png", Path.GetFileName(targetShell.DirPath), surfaceId));
            if (File.Exists(cachePath) && File.GetLastWriteTime(cachePath) >= targetShell.LastModified)
            {
                return new Bitmap(cachePath);
            }
            else
            {
                // 立ち絵サーフェス画像を生成
                var surface = targetShell.DrawSurface(surfaceModel);

                // キャッシュとして保存
                surface.Save(cachePath);

                // サーフェス画像を返す
                return surface;
            }
        }

        /// <summary>
        /// 全ゴーストの顔画像の取得・変換を行う (キャッシュ処理も行う)
        /// </summary>
        /// <returns>ゴーストフォルダパスをキー、顔画像 (Bitmap) を値とするDictionary</returns>
        public virtual IDictionary<string, Bitmap> GetFaceImages(Size faceSize)
        {
            var images = new Dictionary<string, Bitmap>();
            var cacheDir = Util.GetCacheDirPath();

            // キャッシュフォルダが存在しなければ作成
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            foreach (var shell in Shells)
            {
                try
                {
                    Bitmap face = null;
                    var cachePath = Path.Combine(cacheDir, string.Format("{0}_face.png", Path.GetFileName(shell.DirPath)));

                    // 顔画像のキャッシュがあり、更新日時がシェルの更新日以降なら、キャッシュを使用
                    if (File.Exists(cachePath) && File.GetLastWriteTime(cachePath) >= shell.LastModified)
                    {
                        face = new Bitmap(cachePath);
                    }
                    else
                    {
                        // キャッシュがない場合、サーフェス0から顔画像を生成 (サーフェスを読み込めている場合のみ)
                        if (shell.SakuraSurfaceModel != null) {
                            face = shell.DrawFaceImage(shell.SakuraSurfaceModel, faceSize.Width, faceSize.Height);
                            if (face != null)
                            {
                                // 顔画像のキャッシュを保存
                                face.Save(cachePath);
                            }
                        }
                    }

                    // 正常に画像を取得できた場合のみ追加
                    if (face != null)
                    {
                        images.Add(shell.DirPath, face);
                    }
                }
                catch (InvalidDescriptException ex)
                {
                    MessageBox.Show(string.Format("{0} の explorer2\\descript.txt に不正な記述があります。\n{1}", shell.Name, ex.Message),
                                    "エラー",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);

                    Debug.WriteLine(ex.ToString());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            return images;
        }
    }
}
