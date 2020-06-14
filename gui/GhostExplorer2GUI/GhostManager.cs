using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExplorerLib;
using ImageMagick;
using NiseSeriko;
using NiseSeriko.Exceptions;

namespace GhostExplorer2
{
    public class GhostManager
    {
        public virtual Realize2Text Realize2Text { get; set; }
        public virtual string GhostDirPath { get; set; }
        public virtual string FilterWord { get; set; }
        public virtual string SortType { get; set; }
        public virtual IList<ExplorerGhost> Ghosts { get; protected set; }

        public static GhostManager Load(Realize2Text realize2Text, string ghostDirPath, string filterWord, string sortType)
        {
            var manager = new GhostManager() { Realize2Text = realize2Text, GhostDirPath = ghostDirPath, FilterWord = filterWord, SortType = sortType };

            var sw = new Stopwatch();
            sw.Start();
            manager.Load();
            sw.Stop();
            Debug.WriteLine(string.Format("GhostManager.Load : {0}", sw.Elapsed));
            return manager;
        }

        public GhostManager()
        {
            Ghosts = new List<ExplorerGhost>();
        }

        /// <summary>
        /// ゴースト情報の一括読み込み (シェルはまだ読み込まない)
        /// </summary>
        public virtual void Load()
        {
            // 既存の値はクリア
            Ghosts.Clear();

            // ゴーストフォルダのサブフォルダを列挙
            foreach (var subDir in Directory.GetDirectories(GhostDirPath))
            {
                // ゴーストフォルダでなければスキップ
                if (!ExplorerGhost.IsGhostDir(subDir)) continue;

                // ゴーストの基本情報を読み込み
                var ghost = ExplorerGhost.Load(subDir);

                // キーワードが指定されており、かつキーワードに合致しなければスキップ
                if (!string.IsNullOrWhiteSpace(FilterWord))
                {
                    if (!(ghost.Name.Contains(FilterWord)
                         || ghost.SakuraName.Contains(FilterWord)
                         || ghost.KeroName.Contains(FilterWord)))
                    {
                        continue;
                    }
                }

                // リストに追加
                Ghosts.Add(ghost);
            }

            // ゴースト別の使用頻度情報を抽出
            var totalBootTimes = new Dictionary<string, long>();
            var lastBootSeconds = new Dictionary<string, long>();
            foreach (var ghost in Ghosts)
            {
                Realize2Text.Record rec = null;
                if (Realize2Text != null) rec = Realize2Text.GhostRecords.FirstOrDefault(r => r.Name == ghost.Name);
                if (rec != null)
                {
                    totalBootTimes[ghost.Name] = rec.TotalBootByMinute;
                    lastBootSeconds[ghost.Name] = rec.LastBootSecond;
                }
                else
                {
                    // 情報が見つからなければ0扱い
                    totalBootTimes[ghost.Name] = 0;
                    lastBootSeconds[ghost.Name] = 0;
                }
            }

            // ゴースト別のインストール日付情報を取得 (インストール日時順でソートされた場合のみ)
            var installSeconds = new Dictionary<string, long>();
            if (SortType == Const.SortType.ByRecentInstall)
            {
                foreach (var ghost in Ghosts)
                {
                    // 初期値は0
                    installSeconds[ghost.DirPath] = 0;

                    // profile\var.txt が存在すれば、その中からインストール時刻を取得
                    var varPath = Path.Combine(ghost.DirPath, @"ghost\master\profile\var.txt");
                    if (File.Exists(varPath))
                    {
                        try
                        {
                            var lines = File.ReadAllLines(varPath);
                            foreach (var line in lines)
                            {
                                if (line.StartsWith("install,"))
                                {
                                    var tokens = line.TrimEnd().Split(',');
                                    installSeconds[ghost.DirPath] = long.Parse(tokens[1]);
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }
                }
            }

            // 最後にソート
            switch (SortType)
            {
                case Const.SortType.ByBootTime:
                    Ghosts = Ghosts.OrderByDescending(g => totalBootTimes[g.Name]).ToList();
                    break;

                case Const.SortType.ByRecentBoot:
                    Ghosts = Ghosts.OrderByDescending(g => lastBootSeconds[g.Name]).ToList();
                    break;

                case Const.SortType.ByRecentInstall:
                    Ghosts = Ghosts.OrderByDescending(g => installSeconds[g.DirPath]).ToList();
                    break;

                default:
                    // ゴースト名＋フォルダパス順
                    Ghosts = Ghosts.OrderBy(g => Tuple.Create(g.Name, g.DirPath)).ToList();
                    break;
            }
        }

        /// <summary>
        /// 対象ゴーストのシェル情報読み込み
        /// </summary>
        public virtual ExplorerShell LoadShell(ExplorerGhost ghost)
        {
            // シェルが1つも存在しない場合はエラーとする
            if (ghost.CurrentShellRelDirPath == null)
            {
                throw new ShellNotFoundException(string.Format(@"有効なシェルが1つも存在しません。", ghost.DefaultShellDirName));
            }

            var shellDir = Path.Combine(ghost.DirPath, ghost.CurrentShellRelDirPath);
            return ExplorerShell.Load(shellDir, ghost.SakuraDefaultSurfaceId, ghost.KeroDefaultSurfaceId);
        }

        /// <summary>
        /// sakura側サーフェス画像を取得 （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        public virtual Bitmap DrawSakuraSurface(ExplorerGhost ghost, ExplorerShell shell)
        {
            return DrawSurfaceInternal(ghost, shell, shell.SakuraSurfaceModel, shell.SakuraSurfaceId);
        }

        /// <summary>
        /// kero側サーフェス画像を取得  （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        public virtual Bitmap DrawKeroSurface(ExplorerGhost ghost, ExplorerShell shell)
        {
            return DrawSurfaceInternal(ghost, shell, shell.KeroSurfaceModel, shell.KeroSurfaceId);
        }

        /// <summary>
        /// サーフェス画像を取得 （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        protected virtual Bitmap DrawSurfaceInternal(ExplorerGhost ghost, ExplorerShell shell, Shell.SurfaceModel surfaceModel, int surfaceId)
        {
            var cacheDir = Util.GetCacheDirPath();
            if (surfaceModel == null) return null;

            // キャッシュフォルダが存在しなければ作成
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            // 立ち絵画像のキャッシュがあり、更新日時がシェルの更新日以降なら、キャッシュを使用
            var cachePath = Path.Combine(cacheDir, string.Format("{0}_s{1}.png", Path.GetFileName(ghost.DirPath), surfaceId));
            if (File.Exists(cachePath) && File.GetLastWriteTime(cachePath) >= shell.LastModified)
            {
                return new Bitmap(cachePath);
            }
            else
            {
                // 立ち絵サーフェス画像を生成
                var surface = shell.DrawSurface(surfaceModel);

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
        public virtual Bitmap GetFaceImage(ExplorerGhost ghost, ExplorerShell shell, Size faceSize)
        {
            var cacheDir = Util.GetCacheDirPath();

            // キャッシュフォルダが存在しなければ作成
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            try
            {
                Bitmap face = null;
                var cachePath = Path.Combine(cacheDir, string.Format("{0}_face.png", Path.GetFileName(ghost.DirPath)));

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

                // 画像を返す
                return face;
            }
            catch (InvalidDescriptException ex)
            {
                MessageBox.Show(string.Format("{0} の explorer2\\descript.txt に不正な記述があります。\n{1}", ghost.Name, ex.Message),
                                "エラー",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                Debug.WriteLine(ex.ToString());
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }
    }
}
