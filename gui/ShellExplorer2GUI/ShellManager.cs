using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExplorerLib;
using ImageMagick;
using NiseSeriko;
using NiseSeriko.Exceptions;

namespace ShellExplorer2
{
    public class ShellManager
    {
        /// <summary>
        /// 画面に表示する項目
        /// </summary>
        public class ListItem
        {
            public ExplorerShell Shell = null;
            public string Name;
            public string DirPath;
            public string ErrorMessage = null;
        }

        public virtual string GhostDirPath { get; set; }
        public virtual IList<ListItem> ListItems { get; protected set; }

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
            ListItems = new List<ListItem>();
        }

        /// <summary>
        /// シェル情報の一括読み込み
        /// </summary>
        public virtual void Load(int sakuraSurfaceId, int keroSurfaceId)
        {
            // 既存の値はクリア
            ListItems.Clear();

            // シェルフォルダを列挙
            foreach (var subDir in Directory.GetDirectories(Path.Combine(GhostDirPath, "shell")))
            {
                var descriptPath = Path.Combine(subDir, "descript.txt");
                // descript.txt が存在しないならスキップ
                if (!File.Exists(descriptPath))
                {
                    continue;
                }

                var item = new ListItem() { DirPath = subDir };
                try
                {
                    // シェルを読み込み
                    item.Shell = ExplorerShell.Load(subDir, sakuraSurfaceId, keroSurfaceId);
                    item.Name = item.Shell.Name;
                }
                catch (UnhandlableShellException ex)
                {
                    // 処理不可能なシェル
                    item.ErrorMessage = ex.FriendlyMessage;
                }

                // シェルの読み込みに失敗した場合、Descript.txtのみ読み込み、名前の取得を試みる
                if (item.Shell == null)
                {
                    var descript = DescriptText.Load(descriptPath);
                    item.Name = descript.Get("name");
                }

                ListItems.Add(item);
            }

            // 最後に名前＋フォルダパス順でソート
            ListItems = ListItems.OrderBy(s => Tuple.Create(s.Name, s.DirPath)).ToList();
        }

        /// <summary>
        /// sakura側サーフェス画像を取得 （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        public virtual Bitmap DrawSakuraSurface(ExplorerShell targetShell)
        {
            return DrawSurfaceInternal(targetShell, targetShell.SakuraSurfaceModel, targetShell.SakuraSurfaceId);
        }

        /// <summary>
        /// kero側サーフェス画像を取得  （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        public virtual Bitmap DrawKeroSurface(ExplorerShell targetShell)
        {
            return DrawSurfaceInternal(targetShell, targetShell.KeroSurfaceModel, targetShell.KeroSurfaceId);
        }

        /// <summary>
        /// サーフェス画像を取得 （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        protected virtual Bitmap DrawSurfaceInternal(ExplorerShell targetShell, Shell.SurfaceModel surfaceModel, int surfaceId)
        {
            var cacheDir = Util.GetCacheDirPath(Path.GetFileName(GhostDirPath));
            if (surfaceModel == null)
            {
                return null;
            }

            // キャッシュフォルダが存在しなければ作成
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

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
                surface.Write(cachePath);

                // サーフェス画像をBitmap形式に変換
                var surfaceBmp = surface.ToBitmap();

                // 元画像を即時破棄
                surface.Dispose();

                // サーフェス画像をBitmap形式に変換して返す
                return surfaceBmp;
            }
        }

        /// <summary>
        /// 全ゴーストの顔画像の取得・変換を行う (キャッシュ処理も行う)
        /// </summary>
        /// <returns>ゴーストフォルダパスをキー、顔画像 (Bitmap) を値とするDictionary</returns>
        public virtual IDictionary<string, Bitmap> GetFaceImages(Size faceSize)
        {
            var images = new Dictionary<string, Bitmap>();
            var cacheDir = Util.GetCacheDirPath(Path.GetFileName(GhostDirPath));

            // キャッシュフォルダが存在しなければ作成
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            foreach (var item in ListItems)
            {
                // シェルが読み込めなかった場合はスキップ
                if (item.Shell == null)
                {
                    continue;
                }

                var shell = item.Shell;

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
                        if (shell.SakuraSurfaceModel != null)
                        {
                            using (var faceImg = shell.DrawFaceImage(shell.SakuraSurfaceModel, faceSize.Width, faceSize.Height))
                            {
                                face = faceImg.ToBitmap();
                            }

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
