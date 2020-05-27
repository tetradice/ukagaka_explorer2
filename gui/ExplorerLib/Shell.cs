using ExplorerLib.Exceptions;
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

namespace ExplorerLib
{
    public class Shell
    {

        #region プロパティ

        /// <summary>
        /// シェルのフォルダ
        /// </summary>
        public virtual string DirPath { get; set; }

        /// <summary>
        /// descript.txt の情報
        /// </summary>
        public virtual DescriptText Descript { get; set; }

        /// <summary>
        /// descript.txt のファイルパス
        /// </summary>
        public virtual string DescriptPath { get { return Path.Combine(DirPath, "descript.txt"); } }

        /// <summary>
        /// profile情報のパス
        /// </summary>
        public virtual string ProfileDataPath { get { return Path.Combine(DirPath, @"profile\shell.dat"); } }

        /// <summary>
        /// explorer2\descript.txt の情報
        /// </summary>
        public virtual DescriptText Explorer2Descript { get; set; }

        /// <summary>
        /// explorer2\descript.txt のファイルパス
        /// </summary>
        public virtual string Explorer2DescriptPath { get { return Path.Combine(DirPath, @"explorer2\descript.txt"); } }

        /// <summary>
        /// explorer2\character_descript.txt のファイルパス
        /// </summary>
        public virtual string CharacterDescriptPath { get { return Path.Combine(DirPath, @"explorer2\character_descript.txt"); } }

        /// <summary>
        /// explorer2\character_descript.txt の本文
        /// </summary>
        public virtual string CharacterDescript { get; set; }

        /// <summary>
        /// シェル名
        /// </summary>
        public virtual string Name { get { return Descript.Get("name"); } }

        /// <summary>
        /// surfaces.txt 情報のリスト
        /// </summary>
        public virtual IList<SurfacesText> SurfacesTextList { get; set; }

        /// <summary>
        /// sakura側のサーフェス情報 (使用する画像ファイルパス、座標、合成メソッドなどの情報を含んでいる)
        /// </summary>
        public virtual SurfaceModel SakuraSurfaceModel {get;set;}

        /// <summary>
        /// kero側のサーフェス情報 (使用する画像ファイルパス、座標、合成メソッドなどの情報を含んでいる)
        /// </summary>
        public virtual SurfaceModel KeroSurfaceModel { get; set; }

        /// <summary>
        /// シェルの最終更新日時
        /// </summary>
        /// <remarks>
        /// キャッシュ管理に使用
        /// Load時に、下記の更新日付から最も新しい日付をセットする
        /// 
        /// * shell/master フォルダの更新日時
        /// * descript.txt, surfaces*.txt の更新日時
        /// * (あれば) profile/shell.dat の更新日時
        /// * 読み込み対象となるサーフェス画像ファイルの更新日時
        /// </remarks>
        public virtual DateTime LastModified { get; set; }

        public virtual bool SerikoUseSelfAlpha { get { return Descript.Get("seriko.use_self_alpha") == "1"; }}

        /// <summary>
        /// sakura側の立ち絵、顔画像の描画に使用するサーフェスID (標準では0)
        /// </summary>
        public virtual int SakuraSurfaceId { get; set; }

        /// <summary>
        /// kero側の立ち絵、顔画像の描画に使用するサーフェスID
        /// </summary>
        public virtual int KeroSurfaceId { get; set; }
        #endregion

        /// <summary>
        /// シェルを読み込み、使用するサーフェスファイルのパスと更新日付を取得
        /// </summary>
        public static Shell Load(string dirPath, int sakuraSurfaceId, int keroSurfaceId)
        {
            var shell = new Shell() { DirPath = dirPath, SakuraSurfaceId = sakuraSurfaceId, KeroSurfaceId = keroSurfaceId };
            shell.Load();
            return shell;
        }

        /// <summary>
        /// シェル情報を読み込み、使用するサーフェスファイルのパスと更新日付を取得 (ファイルの検索は行うが、画像ファイルの中身は読み込まない)
        /// </summary>
        public virtual void Load()
        {
            // descript.txt 読み込み
            this.Descript = DescriptText.Load(DescriptPath);

            // explorer2\descript.txt 読み込み (存在すれば)
            this.Explorer2Descript = null;
            if (File.Exists(Explorer2DescriptPath))
            {
                this.Explorer2Descript = DescriptText.Load(Explorer2DescriptPath);
            }
            this.Descript = DescriptText.Load(DescriptPath);

            // character_descript.txt があれば読み込み
            CharacterDescript = null;
            if (File.Exists(CharacterDescriptPath))
            {
                CharacterDescript = File.ReadAllText(CharacterDescriptPath, Encoding.UTF8);
            }

            // sakura側、kero側それぞれのbindgroup情報 (着せ替え情報) 読み込み
            var sakuraEnabledBindGroupIds = GetEnabledBindGroupIds("sakura");
            var keroEnabledBindGroupIds = GetEnabledBindGroupIds("kero");

           // 存在する surfaces*.txt を全て読み込み
            SurfacesTextList = new List<SurfacesText>();
            var surfaceTxtPathList = Directory.GetFiles(DirPath, "surface*.txt").OrderBy(path => path); // ファイル名順ソート
            foreach (var surfaceTextPath in surfaceTxtPathList)
            {
                var surfaceText = SurfacesText.Load(surfaceTextPath);
                SurfacesTextList.Add(surfaceText);
            }

            // descript.txt, surface.txt の情報を元に、sakura側とkero側それぞれの
            // サーフェス情報 (使用する画像のファイルパス、ファイル更新日時など) を読み込む
            try
            {
                SakuraSurfaceModel = LoadSurfaceModel(SakuraSurfaceId, sakuraEnabledBindGroupIds);
            } catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            try
            {
                KeroSurfaceModel = LoadSurfaceModel(KeroSurfaceId, keroEnabledBindGroupIds);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            // シェル更新日時の設定
            // (フォルダ更新日付, descript.txt 更新日付, surfaces*.txt 更新日付, 画像ファイル更新日付, profile/shell.dat 更新日付のうち最も新しい日付)
            {
                // フォルダ更新日付
                LastModified = File.GetLastWriteTime(DirPath);

                // descript.txt 更新日付
                if (Descript.LastWriteTime > LastModified) LastModified = Descript.LastWriteTime; // 新しければセット

                // explorer2/descript.txt 更新日付
                var exp2DescPath = Explorer2DescriptPath;
                if (File.Exists(exp2DescPath) && File.GetLastWriteTime(exp2DescPath) > LastModified) LastModified = File.GetLastWriteTime(exp2DescPath); // 新しければセット

                // explorer2/character_descript.txt 更新日付
                var charDescPath = CharacterDescriptPath;
                if (File.Exists(charDescPath) && File.GetLastWriteTime(charDescPath) > LastModified) LastModified = File.GetLastWriteTime(charDescPath); // 新しければセット

                // surfaces*.txt 更新日付
                foreach (var surfaceText in SurfacesTextList)
                {
                    if (surfaceText.LastWriteTime > LastModified) LastModified = surfaceText.LastWriteTime; // 新しければセット
                }

                // profile/shell.dat 更新日付
                var shellProfPath = ProfileDataPath;
                if (File.Exists(shellProfPath) && File.GetLastWriteTime(shellProfPath) > LastModified) LastModified = File.GetLastWriteTime(shellProfPath); // 新しければセット

                // 読み込む画像ファイルの更新日付
                var imagePaths = new HashSet<string>();
                var allLayers = new List<SurfaceModel.Layer>();
                if (SakuraSurfaceModel != null) allLayers.AddRange(SakuraSurfaceModel.Layers);
                if (KeroSurfaceModel != null) allLayers.AddRange(KeroSurfaceModel.Layers);
                foreach (var layer in allLayers)
                {
                    imagePaths.Add(layer.Path);
                    imagePaths.Add(Path.ChangeExtension(layer.Path, ".pna")); // pna画像パスも追加
                }
                foreach (var imgPath in imagePaths)
                {
                    if (File.Exists(imgPath) && File.GetLastWriteTime(imgPath) > LastModified)
                    {
                        LastModified = File.GetLastWriteTime(imgPath);
                    }
                }
            }
        }

        /// <summary>
        /// 指定IDのサーフェス情報を読み込む (ファイルの検索は行うが、画像ファイルの中身は読み込まない)
        /// </summary>
        /// <param name="enabledBindGroupIds">有効になっている着せ替えグループIDのコレクション</param>
        /// <param name="alreadyPassedSurfaceIds">読み込み済みのサーフェスIDコレクション (循環参照による無限ループを防ぐために使用)</param>
        /// <param name="parentPatternComposingMethod">animation定義で指定した合成メソッド (animation定義の中で指定されたサーフェスIDの情報を読み込む場合に使用)</param>
        /// <returns>サーフェス情報。読み込みに失敗した場合はnull</returns>
        public virtual SurfaceModel LoadSurfaceModel(
              int surfaceId
            , ISet<int> enabledBindGroupIds
            , ISet<int> alreadyPassedSurfaceIds = null
            , Seriko.ComposingMethodType? parentPatternComposingMethod = null
        )
        {
            // currentLoadedSurfaceIds が未指定の場合は生成
            alreadyPassedSurfaceIds = (alreadyPassedSurfaceIds ?? new HashSet<int>());

            // animation*.pattern* 内で指定されたサーフェスIDと対応するサーフェス情報
            var childSurfaceModels = new Dictionary<int, SurfaceModel>();

            // surface*.txt から、指定IDと対応するelementとMAYUNAの定義をすべて取得
            var elements = new List<Seriko.Element>();
            var animations = new Dictionary<int, Seriko.Animation>();
            foreach (var surfacesText in SurfacesTextList)
            {
                var defs = surfacesText.GetElementsAndAnimations(surfaceId);
                var defElems = defs.Item1;
                var defAnims = defs.Item2;

                // 対象の surface*.txt 内に存在するelementを結合
                elements = elements.Concat(defElems).ToList();

                // 対象の surface*.txt 内に存在するanimationを結合
                foreach(var pair in defAnims)
                {
                    var animId = pair.Key;
                    var anim = pair.Value;

                    if (animations.ContainsKey(animId))
                    {
                        // 以前の surface*.txt にすでに同じIDのanimationが含まれているならば、interval情報とpattern指定をマージ
                        if (anim.PatternDisplayForStaticImage != Seriko.Animation.PatternDisplayType.No)
                        {
                            animations[animId].PatternDisplayForStaticImage = anim.PatternDisplayForStaticImage;
                        }
                        anim.Patterns.AddRange(anim.Patterns);

                    } else
                    {
                        // 以前の surface*.txt に同じIDのanimationが含まれていなければ、取得したAnimation定義をそのまま格納
                        animations[animId] = anim;
                    }
                }
            }

            // elementとMAYUNA定義を元に、サーフェスモデルを構築
            var surfaceModel = new SurfaceModel();
            {
                // まずはベースサーフィス分のレイヤを登録
                // element指定が1件以上あれば、elementを合成してベースサーフィスとする
                // 1件もなければ、IDと対応する画像ファイルを取得してベースサーフィスとする
                if (elements.Count >= 1)
                {
                    foreach(var elem in elements) // elementはsurfaces.txtで書いた順に処理 (ID順ではない)
                    {
                        var filePath = Path.Combine(DirPath, elem.FileName);
                        if (File.Exists(filePath))
                        {
                            var layer = new SurfaceModel.Layer(filePath, elem.Method);
                            layer.X = elem.OffsetX;
                            layer.Y = elem.OffsetY;
                            surfaceModel.Layers.Add(layer);
                        }
                    }
                } else
                {
                    // 指定IDのサーフェスファイルを検索
                    var surfacePath = FindSurfaceFile(surfaceId);

                    // 画像がある場合はレイヤとして追加
                    if (surfacePath != null)
                    {
                        var method = (parentPatternComposingMethod.HasValue ? parentPatternComposingMethod.Value : Seriko.ComposingMethodType.Base);
                        surfaceModel.Layers.Add(new SurfaceModel.Layer(surfacePath, method));
                    //} else
                    //{
                    //    // 画像がない場合はその旨を表示
                    //    throw new DefaultSurfaceNotFoundException(string.Format("標準のデフォルトサーフェス (ID={0}) が見つかりませんでした。", surfaceId));
                    }
                }

                // それ以降に、初期状態で有効なbindgroupの着せ替えレイヤを重ねる
                foreach (var pair in animations.OrderBy(k => k.Key))
                {
                    var animId = pair.Key;
                    var anim = pair.Value;

                    if (anim.PatternDisplayForStaticImage == Seriko.Animation.PatternDisplayType.No) continue; // 表示対象外の場合はスキップ
                    if (anim.UsingBindGroup && !enabledBindGroupIds.Contains(animId)) continue; // 着せ替え定義の場合、初期状態で有効でないbindGroupはスキップ

                    // interval指定によっては、全patternを重ね合わせるのではなく、最終patternのみ処理する (例: bind+runonce)
                    var usingPatterns = anim.Patterns.OrderBy(e => e.Id).ToList();
                    if (usingPatterns.Any()
                        && anim.PatternDisplayForStaticImage == Seriko.Animation.PatternDisplayType.LastOnly)
                    {
                        var lastPatterns = new[] { usingPatterns.Last() };
                        usingPatterns = lastPatterns.ToList();
                    }

                    // patternの処理
                    var cx = 0;
                    var cy = 0;
                    foreach(var pattern in usingPatterns) // IDが小さい順に処理
                    {
                        // サーフェスIDが負数なら非表示指定のため無視 (-1, -2など)
                        if (pattern.SurfaceId < 0) continue;

                        // patternでのoffset指定は、「前コマからの座標ずらし分」として扱う
                        cx = (cx + pattern.OffsetX);
                        cy = (cy + pattern.OffsetY);

                        // 自分自身のサーフェスIDが指定されたかどうかによって処理を変える
                        // (例: surface0 のブレス内で、SurfaceID = 0を指定した場合)
                        if (pattern.SurfaceId == surfaceId)
                        {
                            // 対象IDのpngファイルを探す
                            var filePath = FindSurfaceFile(pattern.SurfaceId);

                            // 画像が見つかった場合のみレイヤ追加
                            if (filePath != null)
                            {
                                var layer = new SurfaceModel.Layer(filePath, pattern.Method);
                                layer.X = cx; 
                                layer.Y = cy;
                                surfaceModel.Layers.Add(layer);
                            }
                        } else
                        {
                            // 循環定義の場合は無視
                            if (alreadyPassedSurfaceIds.Contains(pattern.SurfaceId)) continue;

                            // まだ定義を読み込んでいないサーフェスIDであれば
                            // 指定されたサーフェスIDと対応するサーフェスモデルを構築
                            if (!childSurfaceModels.ContainsKey(pattern.SurfaceId))
                            {
                                alreadyPassedSurfaceIds.Add(surfaceId);
                                childSurfaceModels[pattern.SurfaceId] = LoadSurfaceModel(pattern.SurfaceId, enabledBindGroupIds, alreadyPassedSurfaceIds, pattern.Method);
                            }
                            var childSurfaceModel = childSurfaceModels[pattern.SurfaceId];

                            // 指定IDのサーフェスモデルが見つかった (画像が存在し、正しく読み込めた) 場合のみ、
                            // そのサーフェスモデルのベースサーフェス分レイヤを追加
                            if (childSurfaceModel != null)
                            {
                                foreach (var childLayer in childSurfaceModel.Layers)
                                {
                                    var layer = new SurfaceModel.Layer(childLayer.Path, childLayer.ComposingMethod);
                                    layer.X = cx + childLayer.X; // patternの処理によって決まった原点座標 + element側でのoffset
                                    layer.Y = cy + childLayer.Y; // 同上
                                    surfaceModel.Layers.Add(layer);
                                }
                            }
                        }
                    }
                }

                // レイヤが1枚もない（画像ファイルが見つからなかったなど）場合は描画失敗
                if (!surfaceModel.Layers.Any())
                {
                    return null;
                }
            }

            // 構築したサーフェスモデルを返す
            return surfaceModel;
        }

        /// <summary>
        /// サーフェスを立ち絵として描画し、Bitmapオブジェクトを返す
        /// </summary>
        /// <param name="trim">画像周辺の余白を削除するかどうか</param>
        public virtual Bitmap DrawSurface(SurfaceModel model, bool trim = true)
        {
            // まずは1枚目のレイヤをベースレイヤとして読み込む
            var surface = LoadAndProcessSurfaceFile(model.Layers[0].Path);

            // 2枚目以降のレイヤが存在するなら、上に重ねていく
            if (model.Layers.Count >= 2)
            {
                for (var i = 1; i < model.Layers.Count; i++)
                {
                    var layer = model.Layers[i];
                    var layerBmp = LoadAndProcessSurfaceFile(layer.Path);

                    // このとき、描画時に元画像のサイズをはみ出すなら、元画像の描画領域を広げる (SSP仕様)
                    if (layer.X + layerBmp.Width > surface.Width
                        || layer.Y + layerBmp.Height > surface.Height)
                    {
                        using (var mImg = new ImageMagick.MagickImage(surface)) // Magick.NETを使用
                        {
                            // 余白追加
                            mImg.Extent(Math.Max(layer.X + layerBmp.Width, surface.Width), Math.Max(layer.Y + layerBmp.Height, surface.Height),
                                        backgroundColor: ImageMagick.MagickColor.FromRgba(255, 255, 255, 0)); // アルファチャンネルで透過色を設定
                            // Bitmapへ書き戻す
                            surface = mImg.ToBitmap();
                        }
                    }

                    // レイヤ描画
                    // メソッドによって処理を分ける
                    if (layer.ComposingMethod == Seriko.ComposingMethodType.Reduce)
                    {
                        // reduce
                        surface = ComposeBitmaps(surface, layerBmp, (outputData, newBmpData, pos) =>
                        {
                            // ベースレイヤ、新規レイヤ両方の不透明度を取得
                            var baseOpacity = outputData[pos + 3];
                            var newOpacity = newBmpData[pos + 3];

                            // 不透明度を乗算
                            var rate = (baseOpacity / 255.0) * (newOpacity / 255.0); // 0 - 255 の値を 0.0 - 1.0の範囲に変換してから乗算する
                            outputData[pos + 3] = (byte)Math.Round(rate * 255);
                        });
                    }
                    else
                    {
                        // 上記以外はoverlay扱いで、普通に重ねていく
                        using (var g = Graphics.FromImage(surface))
                        {
                            // 描画
                            g.DrawImage(layerBmp, layer.X, layer.Y, layerBmp.Width, layerBmp.Height);
                        }
                    }
                }
            }

            // 空白があればトリム
            if (trim)
            {
                using (var mImg = new ImageMagick.MagickImage(surface)) // Magick.NETを使用
                {
                    // 余白切り抜き処理
                    mImg.Trim();
                    mImg.RePage(); // 切り抜き後の画像サイズ調整

                    // Bitmapへ書き戻す
                    surface = mImg.ToBitmap();
                }
            }

            // 合成後のサーフェスを返す
            return surface;
        }

        /// <summary>
        /// ピクセル単位で画像（レイヤ）の合成処理を行い、結果を返す汎用メソッド
        /// </summary>
        public virtual Bitmap ComposeBitmaps(Bitmap baseBmp, Bitmap newBmp, Action<byte[], byte[], int> pixelProcess)
        {
            Bitmap output;

            // 元画像とマスク画像のサイズが異なる場合はエラーとする
            if (baseBmp.Size != newBmp.Size)
            {
                throw new ArgumentException("合成元レイヤと追加レイヤの画像サイズが異なります。");
            }

            // まずは出力用に、元レイヤをコピーして、アルファチャンネルありの32ビットbmpを生成
            output = baseBmp.Clone(new Rectangle(0, 0, baseBmp.Width, baseBmp.Height), PixelFormat.Format32bppArgb);

            // 新規レイヤをコピーして、アルファチャンネルありの32ビットbmpに変換
            // (インデックスカラーや8ビットカラーなどにも対応できるようにするため)
            newBmp = newBmp.Clone(new Rectangle(0, 0, (int)newBmp.Width, (int)newBmp.Height), PixelFormat.Format32bppArgb);

            // 出力画像の1ピクセルあたりのバイト数を取得する (両方とも32ビットのため4固定)
            var pixelByteSize = 4;

            // 出力bmpとマスクbmpをロック
            BitmapData outputBmpData = output.LockBits(
                new Rectangle(0, 0, output.Width, output.Height),
                ImageLockMode.ReadWrite, output.PixelFormat);
            BitmapData newBmpData = newBmp.LockBits(
                new Rectangle(0, 0, (int)newBmp.Width, (int)newBmp.Height),
                ImageLockMode.ReadOnly, (PixelFormat)newBmp.PixelFormat);

            try
            {
                if (outputBmpData.Stride < 0)
                {
                    throw new IllegalImageFormatException(string.Format("ボトムアップ形式のイメージには対応していません。"));
                }
                if (newBmpData.Stride < 0)
                {
                    throw new IllegalImageFormatException(string.Format("ボトムアップ形式のイメージには対応していません。"));
                }

                // 新規レイヤのピクセルデータをバイト型配列で取得する
                IntPtr newBmpPtr = newBmpData.Scan0;
                var newBmpPixels = new byte[newBmpData.Stride * newBmp.Height];
                System.Runtime.InteropServices.Marshal.Copy(newBmpPtr, newBmpPixels, 0, newBmpPixels.Length);

                // 出力画像のピクセルデータをバイト型配列で取得する
                IntPtr outputPtr = outputBmpData.Scan0;
                var outputPixels = new byte[outputBmpData.Stride * output.Height];
                System.Runtime.InteropServices.Marshal.Copy(outputPtr, outputPixels, 0, outputPixels.Length);

                // 出力画像の全ピクセルに対して処理
                for (var y = 0; y < newBmp.Height; y++)
                {
                    for (var x = 0; x < newBmp.Width; x++)
                    {
                        //ピクセルデータでのピクセル(x,y)の開始位置を計算する
                        var pos = y * newBmpData.Stride + x * pixelByteSize;

                        //ピクセル単位処理を実行
                        pixelProcess.Invoke(outputPixels, newBmpPixels, pos);
                    }
                }

                //ピクセルデータを元に戻す
                System.Runtime.InteropServices.Marshal.Copy(outputPixels, 0, outputPtr, outputPixels.Length);
            }
            finally
            {

                // 画像のロックを解除
                output.UnlockBits(outputBmpData);
                newBmp.UnlockBits(newBmpData);
            }

            return output;
        }

        /// <summary>
        /// 立ち絵から顔画像を生成し、Bitmapオブジェクトを返す
        /// </summary>
        public virtual Bitmap DrawFaceImage(SurfaceModel surfaceModel, int width, int height)
        {
            // シェルフォルダに explorer2/descript.txt があれば読み込む
            DescriptText desc = null;
            if (File.Exists(Explorer2DescriptPath))
            {
                desc = DescriptText.Load(Explorer2DescriptPath);
            }

            // まずは立ち絵画像を生成
            var surface = DrawSurface(surfaceModel, trim: false); // 余白はこの段階では切らない

            using (var mImg = new ImageMagick.MagickImage(surface)) // Magick.NETを使用
            {
                {
                    // descript.txt 内で顔画像範囲指定があれば、立ち絵をその範囲で切り抜く
                    if (desc != null &&
                        (
                            desc.Values.ContainsKey("face.left")
                            || desc.Values.ContainsKey("face.top")
                            || desc.Values.ContainsKey("face.width")
                            || desc.Values.ContainsKey("face.height")
                        ))
                    {
                        // 一部だけが指定された場合はエラー
                        if (
                            !desc.Values.ContainsKey("face.left")
                            || !desc.Values.ContainsKey("face.top")
                            || !desc.Values.ContainsKey("face.width")
                            || !desc.Values.ContainsKey("face.height")
                        )
                        {
                            throw new InvalidDescriptException(@"face.left ～ face.height は4つとも指定する必要があります。");
                        }

                        int left;
                        if (!int.TryParse(desc.Values["face.left"], out left)) throw new InvalidDescriptException(@"face.left の指定が不正です。");
                        if(left < 0) throw new InvalidDescriptException(@"face.left が負数です。");
                        int top;
                        if (!int.TryParse(desc.Values["face.top"], out top)) throw new InvalidDescriptException(@"face.top の指定が不正です。");
                        if (top < 0) throw new InvalidDescriptException(@"face.top が負数です。");
                        int dWidth;
                        if (!int.TryParse(desc.Values["face.width"], out dWidth)) throw new InvalidDescriptException(@"face.width の指定が不正です。");
                        if (width < 0) throw new InvalidDescriptException(@"face.width が負数です。");
                        int dHeight;
                        if (!int.TryParse(desc.Values["face.height"], out dHeight)) throw new InvalidDescriptException(@"face.height の指定が不正です。");
                        if (height < 0) throw new InvalidDescriptException(@"face.height が負数です。");

                        // 画像の範囲を超える場合はエラー
                        if (left + width > mImg.Width)
                        {
                            throw new InvalidDescriptException(@"face.left, face.widthで指定された範囲が、サーフェスの横幅を超えています。");
                        }
                        if (top + height > mImg.Height)
                        {
                            throw new InvalidDescriptException(@"face.top, face.heightで指定された範囲が、サーフェスの縦幅を超えています。");
                        }

                        // 指定範囲を切り抜く
                        mImg.Crop(new ImageMagick.MagickGeometry(left, top, dWidth, dHeight));
                        mImg.RePage(); // ページ範囲を更新
                    } else
                    {
                        // 顔画像範囲指定がなければ、余白の削除のみ行う
                        mImg.Trim();
                        mImg.RePage(); // ページ範囲を更新
                    }

                    // 縮小率を決定 (幅が収まるように縮小する)
                    var scaleRate = (double)width / (double)mImg.Width;
                    if (scaleRate > 1.0) scaleRate = 1.0; // 拡大はしない

                    // リサイズ処理
                    mImg.Resize((int)Math.Round(mImg.Width * scaleRate), (int)Math.Round(mImg.Height * scaleRate));

                    // 切り抜く
                    mImg.Crop(width, height);

                    // 顔画像のサイズに合うように余白追加
                    mImg.Extent(width, height,
                                gravity: ImageMagick.Gravity.South, // 中央下寄せ
                                backgroundColor: ImageMagick.MagickColor.FromRgba(255, 255, 255, 0)); // アルファチャンネルで透過色を設定
                }

                // Bitmapへ書き戻す
                surface = mImg.ToBitmap();
            }

            return surface;
        }

        /// <summary>
        /// 指定したパスのサーフェスを読み込み、必要な透過処理を施す
        /// </summary>
        public virtual Bitmap LoadAndProcessSurfaceFile(string surfacePath)
        {
            // 画像ファイル読み込み
            var surface = new Bitmap(surfacePath);

            // seriko.use_self_alpha が1、かつアルファチャンネルありの画像の場合は、元画像をそのまま返す
            if (SerikoUseSelfAlpha && (surface.PixelFormat.HasFlag(PixelFormat.Alpha) || surface.PixelFormat.HasFlag(PixelFormat.PAlpha)))
            {
                return surface;
            }
            else
            {
                // pnaが存在するかどうかをチェック
                var pnaPath = Path.ChangeExtension(surfacePath, ".pna");
                if (File.Exists(pnaPath))
                {
                    // pnaありの場合は、PNAによる透過処理
                    // from <https://dobon.net/vb/dotnet/graphics/drawnegativeimage.html>

                    // pnaマスク画像の読み込み
                    var maskOrig = new Bitmap(pnaPath);

                    // 元画像とマスク画像のサイズが異なる場合はエラーとする
                    if (maskOrig.Size != surface.Size)
                    {
                        throw new IllegalImageFormatException("pngとpnaのサイズが異なります。");
                    }

                    // 透過実行
                    var output = ComposeBitmaps(surface, maskOrig, (outputData, maskBmpData, pos) =>
                    {
                        // 色を取得
                        var color = Color.FromArgb(maskBmpData[pos + 2], maskBmpData[pos + 1], maskBmpData[pos]);

                        // 輝度を取得し、それをそのまま不透明度として設定する
                        var brightness = (byte)Math.Round(color.GetBrightness() * 255); // 0 - 1.0で表される輝度値を、0 - 255の範囲に変換
                        outputData[pos + 3] = brightness;
                    });

                    return output;
                }
                else
                {
                    // pnaなしの場合は、画像の左上の色を透過色として設定して返す
                    surface.MakeTransparent(surface.GetPixel(0, 0));
                    return surface;
                }
            }
        }

        /// <summary>
        /// 指定した番号のサーフェス画像を、フォルダ内から検索して返す (見つからない場合はnull)
        /// </summary>
        /// <remarks>
        /// 一例として、10番を指定した場合は、surface10.png, surface010.png, surface0010.png などを検索対象とします。<br />
        /// elementの情報は考慮しません。
        /// </remarks>
        public virtual string FindSurfaceFile(int surfaceNo)
        {
            var regex = new Regex(string.Format(@"\Asurface0*{0}\.png\z", surfaceNo), RegexOptions.IgnoreCase);
            foreach (var path in Directory.GetFiles(DirPath, string.Format("surface*{0}.png", surfaceNo)))
            {
                var fileName = Path.GetFileName(path);
                if (regex.IsMatch(fileName))
                {
                    return path;
                }
            }

            // 見つからなかった
            return null;
        }

        #region 着せ替え

        /// <summary>
        /// descript.txt 内で指定された bindgroup (着せ替えグループ) 情報を表すクラス
        /// </summary>
        public class BindGroup
        {
            public virtual bool Default { get; set; }
            public virtual List<int> AddId { get; set; }

            public BindGroup()
            {
                AddId = new List<int>();
            }
        }

        /// <summary>
        /// 有効になっている着せ替えグループID一覧を、profileもしくはdescript.txtの指定から取得
        /// </summary>
        /// <param name="targetCharacter">着せ替え対象のキャラクタ (sakura, kero, char2, char3, ...)</param>
        /// <returns>有効になっている着せ替えグループIDのセット</returns>
        public virtual ISet<int> GetEnabledBindGroupIds(string targetCharacter)
        {
            // まずはprofileからの取得を試みる。取得できたら終了
            var ids = GetEnabledBindGroupIdsFromProfile(targetCharacter);
            if (ids != null) return ids;

            // profileから取得できない場合、descript.txt の記述から、初期状態で有効なbindgroup IDのコレクションを作成
            ids = new HashSet<int>();
            var groups = GetBindGroupsFromDescript(targetCharacter);
            foreach (var pair in groups)
            {
                var id = pair.Key;
                var group = pair.Value;

                if (group.Default)
                {
                    ids.Add(id);

                    // addid指定分も追加
                    foreach (var addId in group.AddId)
                    {
                        ids.Add(addId);
                    }
                }
            }

            return ids;
        }

        /// <summary>
        /// profileに保存された内容から、有効になっている着せ替えグループID一覧を取得
        /// </summary>
        /// <param name="targetCharacter">着せ替え対象のキャラクタ (sakura, kero, char2, char3, ...)</param>
        /// <returns>有効になっている着せ替えグループIDのセット。ファイルが存在しない場合、もしくは着せ替え情報が存在しない場合はnull</returns>
        public virtual ISet<int> GetEnabledBindGroupIdsFromProfile(string targetCharacter)
        {
            ISet<int> enabledIds = null;

            if (targetCharacter == "sakura") targetCharacter = "char0";
            if (targetCharacter == "kero") targetCharacter = "char1";
            var shellProfPath = ProfileDataPath;

            var mark = string.Format("{0}.bind.savearray,", targetCharacter);

            if (File.Exists(shellProfPath))
            {
                try
                {
                    var lines = File.ReadAllLines(shellProfPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith(mark))
                        {
                            // IDを格納して終了
                            enabledIds = new HashSet<int>();
                            var specs = line.TrimEnd().Split(',')[1].Split(' ');
                            foreach (var spec in specs)
                            {
                                var pair = spec.Split('=');
                                if (pair[1] == "1")
                                {
                                    enabledIds.Add(int.Parse(pair[0]));
                                }
                            }
                            return enabledIds;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return enabledIds;
        }


        /// <summary>
        /// descript.txt の定義内容から着せ替えグループ一覧を取得
        /// </summary>
        /// <param name="targetCharacter">着せ替え対象のキャラクタ (sakura, kero, char2, char3, ...)</param>
        public virtual IDictionary<int, BindGroup> GetBindGroupsFromDescript(string targetCharacter)
        {
            var defaultRegex = new Regex(string.Format(@"\A{0}\.bindgroup(\d+).default\z", targetCharacter));
            var addIdRegex = new Regex(string.Format(@"\A{0}\.bindgroup(\d+).addid\z", targetCharacter));

            // descript.txt から着せ替え情報取得
            var bindGroups = new Dictionary<int, BindGroup>();
            foreach (var pair in this.Descript.Values)
            {
                // default指定
                {
                    var matched = defaultRegex.Match(pair.Key);
                    if (matched.Success && pair.Value == "1")
                    {
                        var groupId = int.Parse(matched.Groups[1].Value);
                        if (!bindGroups.ContainsKey(groupId)) bindGroups[groupId] = new BindGroup();

                        bindGroups[groupId].Default = true;

                        continue; // 次の処理へ
                    }
                }

                // addid指定
                {
                    var matched = addIdRegex.Match(pair.Value);
                    if (matched.Success)
                    {
                        var groupId = int.Parse(matched.Groups[1].Value);
                        if (!bindGroups.ContainsKey(groupId)) bindGroups[groupId] = new BindGroup();

                        bindGroups[groupId].AddId = new List<int>();
                        foreach (var idValue in pair.Value.Split(','))
                        {
                            int id;
                            if (int.TryParse(idValue, out id))
                            {
                                bindGroups[groupId].AddId.Add(id);
                            }
                        }

                        continue; // 次の処理へ
                    }
                }
            }

            return bindGroups;
        }


        #endregion

        #region サーフェス定義

        /// <summary>
        /// ID1つあたりのサーフェス情報を表すクラス (ファイルパス, element, MAYUNA着せ替え情報を含む)
        /// </summary>
        public class SurfaceModel {
            /// <summary>
            /// レイヤ。surface*.png 1つ / element定義1つ / animation定義1つと対応する
            /// </summary>
            public class Layer
            {
                /// <summary>
                /// ファイルパス
                /// </summary>
                public virtual string Path { get; set; }

                /// <summary>
                /// 合成メソッド
                /// </summary>
                public virtual Seriko.ComposingMethodType ComposingMethod { get; set; }

                /// <summary>
                /// X座標 (1枚目のベースレイヤに対する相対位置)
                /// </summary>
                public virtual int X { get; set; }

                /// <summary>
                /// Y座標 (1枚目のベースレイヤに対する相対位置)
                /// </summary>
                public virtual int Y { get; set; }

                /// <summary>
                /// コンストラクタ
                /// </summary>
                public Layer(string imageFilePath, Seriko.ComposingMethodType composingMethod = Seriko.ComposingMethodType.Base)
                {
                    ComposingMethod = composingMethod;
                    Path = imageFilePath;
                }
            }

            /// <summary>
            /// レイヤリスト
            /// </summary>
            public virtual IList<Layer> Layers { get; protected set; }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            public SurfaceModel()
            {
                Layers = new List<Layer>();
            }
        }

        #endregion
    }
}
