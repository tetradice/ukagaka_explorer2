using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostExplorer2
{
    public class GhostManager
    {
        public virtual IList<string> DirPathList { get; set; }
        public virtual IList<Ghost> Ghosts { get; protected set; }

        public virtual string CacheDirPath
        {
            get
            {
                var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                return Path.Combine(Path.GetDirectoryName(appPath), @"data\cache");
            }
        }

        public static GhostManager Load(IList<string> dirPathList)
        {
            var manager = new GhostManager() { DirPathList = dirPathList };

            var sw = new Stopwatch();
            sw.Start();
            manager.Load();
            sw.Stop();
            Debug.WriteLine(string.Format("GhostManager.Load : {0}", sw.Elapsed));
            return manager;
        }

        public GhostManager()
        {
            Ghosts = new List<Ghost>();
        }

        /// <summary>
        /// ゴースト情報の一括読み込み
        /// </summary>
        public virtual void Load()
        {
            // 既存の値はクリア
            Ghosts.Clear();

            // フォルダ1つずつ処理
            foreach(var ghostDir in DirPathList)
            {
                // ゴーストフォルダのサブフォルダを列挙
                foreach (var subDir in Directory.GetDirectories(ghostDir))
                {
                    // ゴーストフォルダでなければスキップ
                    if (!Ghost.IsGhostDir(subDir)) continue;

                    // ゴーストの基本情報を読み込み
                    var ghost = Ghost.Load(subDir);

                    // リストに追加
                    Ghosts.Add(ghost);
                }
            }

            // 最後に名前＋フォルダパス順でソート
            Ghosts = Ghosts.OrderBy(g => Tuple.Create(g.Name, g.DirPath)).ToList();
        }

        /// <summary>
        /// sakura側サーフェス画像を取得 （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        public virtual Bitmap DrawSakuraSurface(Ghost targetGhost)
        {
            return DrawSurfaceInternal(targetGhost, targetGhost.Shell.SakuraSurfaceModel, 0);
        }

        /// <summary>
        /// kero側サーフェス画像を取得  （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        public virtual Bitmap DrawKeroSurface(Ghost targetGhost)
        {
            return DrawSurfaceInternal(targetGhost, targetGhost.Shell.KeroSurfaceModel, 10);
        }

        /// <summary>
        /// サーフェス画像を取得 （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        protected virtual Bitmap DrawSurfaceInternal(Ghost targetGhost, Shell.SurfaceModel surfaceModel, int surfaceId)
        {
            var cacheDir = CacheDirPath;
            var targetShell = targetGhost.Shell;
            if (surfaceModel == null) return null;

            // キャッシュフォルダが存在しなければ作成
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            // 立ち絵画像のキャッシュがあり、更新日時がマスターシェルの更新日以降なら、キャッシュを使用
            var cachePath = Path.Combine(cacheDir, string.Format("{0}_s{1}.png", Path.GetFileName(targetGhost.DirPath), surfaceId));
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
            var cacheDir = CacheDirPath;

            // キャッシュフォルダが存在しなければ作成
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            foreach (var ghost in Ghosts)
            {
                try
                {
                    Bitmap face = null;
                    var cachePath = Path.Combine(cacheDir, string.Format("{0}_face.png", Path.GetFileName(ghost.DirPath)));

                    // 顔画像のキャッシュがあり、更新日時がマスターシェルの更新日以降なら、キャッシュを使用
                    if (File.Exists(cachePath) && File.GetLastWriteTime(cachePath) >= ghost.Shell.LastModified)
                    {
                        face = new Bitmap(cachePath);
                    }
                    else
                    {
                        // キャッシュがない場合、サーフェス0から顔画像を生成
                        var surface0 = DrawSakuraSurface(ghost);
                        if (surface0 != null)
                        {
                            var faceWidth = faceSize.Width;
                            var faceHeight = faceSize.Height;
                            using (var mImg = new ImageMagick.MagickImage(surface0)) // Magick.NETを使用
                            {
                                {
                                    // 余白切り抜き処理
                                    mImg.Trim();
                                    mImg.RePage(); // 切り抜き後の画像サイズ調整

                                    // 縮小率を決定 (幅が収まるように縮小する)
                                    var scaleRate = (double)faceWidth / (double)mImg.Width;
                                    if (scaleRate > 1.0) scaleRate = 1.0; // 拡大はしない

                                    // リサイズ処理
                                    mImg.Resize((int)Math.Round(mImg.Width * scaleRate), (int)Math.Round(mImg.Height * scaleRate));

                                    // 切り抜く
                                    mImg.Crop(faceWidth, faceHeight);

                                    // 顔画像のサイズに合うように余白追加
                                    mImg.Extent(faceWidth, faceHeight,
                                                gravity: ImageMagick.Gravity.South, // 中央下寄せ
                                                backgroundColor: ImageMagick.MagickColor.FromRgba(255, 255, 255, 0)); // アルファチャンネルで透過色を設定
                                }

                                // Bitmapへ書き戻す
                                face = mImg.ToBitmap();

                                // 顔画像のキャッシュを保存
                                face.Save(cachePath);
                            }

                        }
                    }

                    // 正常に画像を取得できた場合のみ追加
                    if (face != null)
                    {
                        images.Add(ghost.DirPath, face);
                    }
                } catch(Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            return images;
        }
    }
}
