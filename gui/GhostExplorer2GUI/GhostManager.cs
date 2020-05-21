﻿using ExplorerLib;
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

namespace GhostExplorer2
{
    public class GhostManager
    {
        public virtual IList<string> DirPathList { get; set; }
        public virtual IList<GhostWithPrimaryShell> Ghosts { get; protected set; }

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
            Ghosts = new List<GhostWithPrimaryShell>();
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
                    if (!GhostWithPrimaryShell.IsGhostDir(subDir)) continue;

                    // ゴーストの基本情報を読み込み
                    var ghost = GhostWithPrimaryShell.Load(subDir);

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
        public virtual Bitmap DrawSakuraSurface(GhostWithPrimaryShell targetGhost)
        {
            return DrawSurfaceInternal(targetGhost, targetGhost.Shell.SakuraSurfaceModel, targetGhost.Shell.SakuraSurfaceId);
        }

        /// <summary>
        /// kero側サーフェス画像を取得  （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        public virtual Bitmap DrawKeroSurface(GhostWithPrimaryShell targetGhost)
        {
            return DrawSurfaceInternal(targetGhost, targetGhost.Shell.KeroSurfaceModel, targetGhost.Shell.KeroSurfaceId);
        }

        /// <summary>
        /// サーフェス画像を取得 （element, MAYUNAの合成も行う。またキャッシュがあればキャッシュから取得）
        /// </summary>
        /// <returns>サーフェス画像を取得できた場合はその画像。取得に失敗した場合はnull</returns>
        protected virtual Bitmap DrawSurfaceInternal(GhostWithPrimaryShell targetGhost, Shell.SurfaceModel surfaceModel, int surfaceId)
        {
            var cacheDir = CacheDirPath;
            var targetShell = targetGhost.Shell;
            if (surfaceModel == null) return null;

            // キャッシュフォルダが存在しなければ作成
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            // 立ち絵画像のキャッシュがあり、更新日時がシェルの更新日以降なら、キャッシュを使用
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
        public virtual Bitmap GetFaceImage(GhostWithPrimaryShell ghost, Size faceSize)
        {
            var images = new Dictionary<string, Bitmap>();
            var cacheDir = CacheDirPath;

            // キャッシュフォルダが存在しなければ作成
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            try
            {
                Bitmap face = null;
                var cachePath = Path.Combine(cacheDir, string.Format("{0}_face.png", Path.GetFileName(ghost.DirPath)));

                // 顔画像のキャッシュがあり、更新日時がシェルの更新日以降なら、キャッシュを使用
                if (File.Exists(cachePath) && File.GetLastWriteTime(cachePath) >= ghost.Shell.LastModified)
                {
                    face = new Bitmap(cachePath);
                }
                else
                {
                    // キャッシュがない場合、サーフェス0から顔画像を生成 (サーフェスを読み込めている場合のみ)
                    if (ghost.Shell.SakuraSurfaceModel != null) {
                        face = ghost.Shell.DrawFaceImage(ghost.Shell.SakuraSurfaceModel, faceSize.Width, faceSize.Height);
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
