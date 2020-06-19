using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImageMagick;
using NiseSeriko.Exceptions;

namespace NiseSeriko
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
        public virtual string ProfileDataPath { get { return Path.Combine(DirPath, "profile/shell.dat"); } }

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
        /// 読み込みに失敗した場合、非表示 (ID=-1) の場合はnull
        /// </summary>
        public virtual SurfaceModel SakuraSurfaceModel { get; set; }

        /// <summary>
        /// kero側のサーフェス情報 (使用する画像ファイルパス、座標、合成メソッドなどの情報を含んでいる)
        /// 読み込みに失敗した場合、非表示 (ID=-1) の場合はnull
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

        public virtual bool SerikoUseSelfAlpha { get { return Descript.Get("seriko.use_self_alpha") == "1"; } }

        /// <summary>
        /// sakura側の立ち絵、顔画像の描画に使用するサーフェスID (標準では0)
        /// </summary>
        public virtual int SakuraSurfaceId { get; set; }

        /// <summary>
        /// kero側の立ち絵、顔画像の描画に使用するサーフェスID
        /// </summary>
        public virtual int KeroSurfaceId { get; set; }

        /// <summary>
        /// レイヤ合成時の中間結果などを出力するフォルダ (デバッグ用)
        /// </summary>
        public virtual string InterimOutputDirPathForDebug { get; set; }
        #endregion

        /// <summary>
        /// シェルを読み込み、使用するサーフェスファイルのパスと更新日付を取得
        /// </summary>
        protected static T Load<T>(string dirPath, int sakuraSurfaceId, int keroSurfaceId, string interimOutputDirPathForDebug = null)
        where T : Shell, new()
        {
            var shell = new T()
            {
                DirPath = dirPath,
                SakuraSurfaceId = sakuraSurfaceId,
                KeroSurfaceId = keroSurfaceId,
                InterimOutputDirPathForDebug = interimOutputDirPathForDebug
            };
            shell.Load();
            return shell;
        }

        /// <summary>
        /// シェルを読み込み、使用するサーフェスファイルのパスと更新日付を取得
        /// </summary>
        public static Shell Load(string dirPath, int sakuraSurfaceId, int keroSurfaceId, string interimOutputDirPathForDebug = null)
        {
            return Shell.Load<Shell>(dirPath, sakuraSurfaceId, keroSurfaceId, interimOutputDirPathForDebug);
        }

        /// <summary>
        /// シェルフォルダかどうかを判定
        /// </summary>
        public static bool IsShellDir(string dirPath)
        {
            // descript.txt が存在するならゴーストフォルダとみなす
            var shellDesc = Path.Combine(dirPath, "descript.txt");
            return (File.Exists(shellDesc));
        }

        /// <summary>
        /// シェル情報を読み込み、使用するサーフェスファイルのパスと更新日付を取得 (ファイルの検索は行うが、画像ファイルの中身は読み込まない)
        /// </summary>
        public virtual void Load()
        {
            // シェル関連ファイルの読み込み
            LoadFiles();

            // シェル更新日時の設定
            UpdateLastModified();
        }

        /// <summary>
        /// ファイルを検索してシェル情報を読み込む
        /// </summary>
        protected virtual void LoadFiles()
        {
            // descript.txt 読み込み
            Descript = DescriptText.Load(DescriptPath);

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
                SakuraSurfaceModel = LoadSurfaceModel(SakuraSurfaceId, "sakura", sakuraEnabledBindGroupIds);
            }
            catch (UnhandlableShellException ex)
            {
                ex.Scope = 0; // sakura側のエラー

                Debug.WriteLine(ex.ToString());
                throw ex; // エラーメッセージを表示するため外に投げる
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            try
            {
                KeroSurfaceModel = LoadSurfaceModel(KeroSurfaceId, "kero", keroEnabledBindGroupIds);
            }
            catch (UnhandlableShellException ex)
            {
                ex.Scope = 1; // kero側のエラー

                Debug.WriteLine(ex.ToString());
                throw ex; // エラーメッセージを表示するため外に投げる
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// シェルの更新日時を設定する
        /// (フォルダ更新日付, descript.txt 更新日付, surfaces*.txt 更新日付, 画像ファイル更新日付, profile/shell.dat 更新日付のうち最も新しい日付)
        /// </summary>
        protected virtual void UpdateLastModified()
        {
            // フォルダ更新日付
            LastModified = File.GetLastWriteTime(DirPath);

            // descript.txt 更新日付
            if (Descript.LastWriteTime > LastModified) LastModified = Descript.LastWriteTime; // 新しければセット

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

        /// <summary>
        /// 指定IDのサーフェス情報を読み込む (ファイルの検索は行うが、画像ファイルの中身は読み込まない)
        /// </summary>
        /// <param name="enabledBindGroupIds">有効になっている着せ替えグループIDのコレクション</param>
        /// <param name="targetCharacter">対象のキャラクタ (sakura, kero, char2, char3, ...) 。alias定義を探すときに使う</param>
        /// <param name="alreadyPassedSurfaceIds">読み込み済みのサーフェスIDコレクション (循環参照による無限ループを防ぐために使用)</param>
        /// <param name="parentPatternComposingMethod">animation定義で指定した合成メソッド (animation定義の中で指定されたサーフェスIDの情報を読み込む場合に使用)</param>
        /// <returns>サーフェス情報。非表示ID (ID=-1) が指定された場合や、読み込みに失敗した場合はnull</returns>
        public virtual SurfaceModel LoadSurfaceModel(
              int surfaceId
            , string targetCharacter
            , ISet<int> enabledBindGroupIds
            , ISet<int> alreadyPassedSurfaceIds = null
            , Seriko.ComposingMethodType? parentPatternComposingMethod = null
            , List<string> interimLogs = null
            , int depth = 0
        )
        {
            var isTopLevel = depth == 0;
            var interimLogPrefix = string.Format("[{0}]", surfaceId);
            for (var i = 0; i < depth; i++)
            {
                interimLogPrefix = "  " + interimLogPrefix;
            }

            // 非表示の場合はnullを返す
            if (surfaceId == -1) return null;

            // currentLoadedSurfaceIds が未指定の場合は生成
            alreadyPassedSurfaceIds = (alreadyPassedSurfaceIds ?? new HashSet<int>());

            // interimLogsが未指定の場合は生成
            if (interimLogs == null && InterimOutputDirPathForDebug != null) interimLogs = new List<string>();

            // animation*.pattern* 内で指定されたサーフェスIDと対応するサーフェス情報
            var childSurfaceModels = new Dictionary<int, SurfaceModel>();

            // surface*.txt から、指定IDと対応するalias定義を探す
            foreach (var surfacesText in SurfacesTextList)
            {
                var newId = surfacesText.FindActualSurfaceId(targetCharacter, surfaceId);
                if (newId != surfaceId)
                {
                    surfaceId = newId;
                    break; // 1件見つかったら終了
                }
            }

            // surface*.txt から、指定IDと対応するelement, MAYUNA定義をすべて取得
            var elements = new List<Seriko.Element>();
            var animations = new Dictionary<int, Seriko.Animation>();
            foreach (var surfacesText in SurfacesTextList)
            {
                var defInfo = surfacesText.GetSurfaceDefinitionInfo(surfaceId);

                // 対象の surface*.txt 内に存在するelementを結合
                elements = elements.Concat(defInfo.Elements).ToList();

                // 対象の surface*.txt 内に存在するanimationを結合
                foreach (var pair in defInfo.Animations)
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

                    }
                    else
                    {
                        // 以前の surface*.txt に同じIDのanimationが含まれていなければ、取得したAnimation定義をそのまま格納
                        animations[animId] = anim;
                    }
                }
            }

            // elementとMAYUNA定義を元に、サーフェスモデルを構築
            var surfaceModel = new SurfaceModel(surfaceId);
            {
                // まずはベースサーフィス分のレイヤを登録
                // element指定が1件以上あれば、elementを合成してベースサーフィスとする
                // 1件もなければ、IDと対応する画像ファイルを取得してベースサーフィスとする
                if (elements.Count >= 1)
                {
                    foreach (var elem in elements) // elementはsurfaces.txtで書いた順に処理 (ID順ではない)
                    {
                        var filePath = Path.Combine(DirPath, elem.FileName);
                        if (File.Exists(filePath))
                        {
                            var layer = new SurfaceModel.Layer(filePath, elem.Method)
                            {
                                X = elem.OffsetX,
                                Y = elem.OffsetY
                            };
                            surfaceModel.Layers.Add(layer);
                            if (interimLogs != null) interimLogs.Add(interimLogPrefix + string.Format("element{0} - use {1}", elem.Id, Path.GetFileName(filePath)));
                        }
                        else
                        {
                            if (interimLogs != null) interimLogs.Add(interimLogPrefix + string.Format("ERROR: element{0} image not found ({1})", elem.Id, Path.GetFileName(filePath)));
                        }
                    }
                }
                else
                {
                    // 指定IDのサーフェスファイルを検索
                    var surfacePath = FindSurfaceFile(surfaceId);

                    // 画像がある場合はレイヤとして追加
                    if (surfacePath != null)
                    {
                        if (interimLogs != null) interimLogs.Add(interimLogPrefix + string.Format("use base image ({0})", Path.GetFileName(surfacePath)));
                        var method = (parentPatternComposingMethod.HasValue ? parentPatternComposingMethod.Value : Seriko.ComposingMethodType.Base);
                        surfaceModel.Layers.Add(new SurfaceModel.Layer(surfacePath, method));
                    }
                    else
                    {
                        if (interimLogs != null) interimLogs.Add(interimLogPrefix + string.Format("base image not found"));
                    }
                }

                // それ以降に、初期状態で有効なbindgroupの着せ替えレイヤを重ねる
                foreach (var pair in animations.OrderBy(k => k.Key))
                {
                    var animId = pair.Key;
                    var anim = pair.Value;

                    if (anim.PatternDisplayForStaticImage == Seriko.Animation.PatternDisplayType.No) continue; // 表示対象外の場合はスキップ
                    if (anim.UsingBindGroup && !enabledBindGroupIds.Contains(animId)) continue; // 着せ替え定義の場合、初期状態で有効でないbindGroupはスキップ

                    if (interimLogs != null) interimLogs.Add(interimLogPrefix + string.Format("use animation{0} (display: {1})", animId, anim.PatternDisplayForStaticImage));

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
                    var relative = anim.OffsetInterpriting == Seriko.Animation.OffsetInterpritingType.RelativeFromPreviousFrame;
                    foreach (var pattern in usingPatterns) // IDが小さい順に処理
                    {
                        if (interimLogs != null) interimLogs.Add(interimLogPrefix + string.Format("  pattern{0} (surfaceId={1})", pattern.Id, pattern.SurfaceId));

                        // サーフェスIDが負数なら非表示指定のため無視 (-1, -2など)
                        if (pattern.SurfaceId < 0) continue;

                        // 座標決定
                        if (relative)
                        {
                            // 前コマからのずらし
                            cx = (cx + pattern.OffsetX);
                            cy = (cy + pattern.OffsetY);
                        }
                        else
                        {
                            // 絶対指定
                            cx = pattern.OffsetX;
                            cy = pattern.OffsetY;
                        }

                        // 自分自身のサーフェスIDが指定されたかどうかによって処理を変える
                        // (例: surface0 のブレス内で、SurfaceID = 0を指定した場合)
                        if (pattern.SurfaceId == surfaceId)
                        {
                            // 対象IDのpngファイルを探す
                            var filePath = FindSurfaceFile(pattern.SurfaceId);

                            // 画像が見つかった場合のみレイヤ追加
                            if (filePath != null)
                            {
                                var layer = new SurfaceModel.Layer(filePath, pattern.Method)
                                {
                                    X = cx,
                                    Y = cy
                                };
                                surfaceModel.Layers.Add(layer);

                                if (interimLogs != null) interimLogs.Add(interimLogPrefix + string.Format("    use {0}", Path.GetFileName(filePath)));
                            }
                            else
                            {
                                if (interimLogs != null) interimLogs.Add(interimLogPrefix + string.Format("    ERROR: image not found"));
                            }
                        }
                        else
                        {
                            // 循環定義の場合は無視
                            if (alreadyPassedSurfaceIds.Contains(pattern.SurfaceId)) continue;

                            // まだ定義を読み込んでいないサーフェスIDであれば
                            // 指定されたサーフェスIDと対応するサーフェスモデルを構築
                            if (!childSurfaceModels.ContainsKey(pattern.SurfaceId))
                            {
                                alreadyPassedSurfaceIds.Add(surfaceId);
                                if (interimLogs != null) interimLogs.Add(interimLogPrefix + string.Format("    load child surface model - s{0}", pattern.SurfaceId));
                                childSurfaceModels[pattern.SurfaceId] = LoadSurfaceModel(
                                    pattern.SurfaceId
                                    , targetCharacter
                                    , enabledBindGroupIds
                                    , alreadyPassedSurfaceIds
                                    , pattern.Method
                                    , interimLogs: interimLogs
                                    , depth: depth + 1
                                );
                            }
                            var childSurfaceModel = childSurfaceModels[pattern.SurfaceId];

                            // 指定IDのサーフェスモデルが見つかった (画像が存在し、正しく読み込めた) 場合のみ、
                            // そのサーフェスモデルのベースサーフェス分レイヤを追加
                            if (childSurfaceModel != null)
                            {
                                foreach (var childLayer in childSurfaceModel.Layers)
                                {
                                    var layer = new SurfaceModel.Layer(childLayer.Path, childLayer.ComposingMethod)
                                    {
                                        X = cx + childLayer.X, // patternの処理によって決まった原点座標 + element側でのoffset
                                        Y = cy + childLayer.Y // 同上
                                    };
                                    surfaceModel.Layers.Add(layer);

                                    if (interimLogs != null) interimLogs.Add(interimLogPrefix + string.Format("    use {0}", Path.GetFileName(layer.Path)));
                                }
                            }
                        }
                    }
                }

                // デフォルトサーフェスであるにもかかわらず、レイヤが1枚もない（画像ファイルが見つからなかったなど）場合は描画失敗
                if (isTopLevel && !surfaceModel.Layers.Any())
                {
                    // element定義かanimation定義がある場合は、「定義されているが対応画像が見つからない」状態であるため表示メッセージを変える
                    if (elements.Count >= 1 || animations.Count >= 1)
                    {
                        throw new DefaultSurfaceNotFoundException(string.Format("デフォルトサーフェス (ID={0}) の定義で指定された画像ファイルが見つかりませんでした。", surfaceId));
                    }
                    else
                    {
                        throw new DefaultSurfaceNotFoundException(string.Format("デフォルトサーフェス (ID={0}) が見つかりませんでした。", surfaceId)) { Unsupported = true };

                    }
                }
            }

            // デフォルトサーフェスの構築が終わった場合、中間結果を出力
            if (isTopLevel && interimLogs != null)
            {
                Directory.CreateDirectory(InterimOutputDirPathForDebug);
                var path = Path.Combine(InterimOutputDirPathForDebug, string.Format("surfaceModel_{0:0000}.log", surfaceId));
                File.WriteAllLines(path, interimLogs);
            }

            // 構築したサーフェスモデルを返す
            return surfaceModel;
        }

        /// <summary>
        /// サーフェスを立ち絵として描画し、Imageオブジェクトを返す
        /// </summary>
        /// <param name="trim">画像周辺の余白を削除するかどうか</param>
        public virtual MagickImage DrawSurface(SurfaceModel model, bool trim = true)
        {
            // まずは1枚目のレイヤをベースレイヤとして読み込む
            var sw1st = Stopwatch.StartNew();
            var surface = LoadAndProcessSurfaceFile(model.Layers[0].Path);

            string interimLogPath = null;
            if (InterimOutputDirPathForDebug != null)
            {
                Directory.CreateDirectory(InterimOutputDirPathForDebug);
                interimLogPath = Path.Combine(InterimOutputDirPathForDebug, string.Format(@"s{0:0000}.log", model.Id));
                if (File.Exists(interimLogPath)) File.Delete(interimLogPath);

                surface.Write(Path.Combine(InterimOutputDirPathForDebug, string.Format(@"s{0:0000}_p{1:0000}.png", model.Id, 0)));
                var msg = string.Format("p{0:000} : {1} method={2} x={3} y={4} (rendering time: {5} ms)", 0, Path.GetFileName(model.Layers[0].Path), model.Layers[0].ComposingMethod.ToString(), model.Layers[0].X, model.Layers[0].Y, sw1st.ElapsedMilliseconds);
                File.AppendAllLines(interimLogPath, new[] { msg });
            };

            // 2枚目以降のレイヤが存在するなら、上に重ねていく
            if (model.Layers.Count >= 2)
            {
                for (var i = 1; i < model.Layers.Count; i++)
                {
                    var sw = Stopwatch.StartNew();
                    var layer = model.Layers[i];
                    var layerBmp = LoadAndProcessSurfaceFile(layer.Path);

                    if (InterimOutputDirPathForDebug != null)
                    {
                        layerBmp.Write(Path.Combine(InterimOutputDirPathForDebug, $"s{model.Id:0000}_p{i:0000}_loaded.png"));
                    };

                    // このとき、描画時に元画像のサイズをはみ出すなら、元画像の描画領域を広げる (SSP仕様)
                    if (layer.X + layerBmp.Width > surface.Width
                        || layer.Y + layerBmp.Height > surface.Height)
                    {
                        // 余白追加
                        var backgroundColor = ImageMagick.MagickColor.FromRgba(255, 255, 255, 0);
                        surface.Extent(Math.Max(layer.X + layerBmp.Width, surface.Width), Math.Max(layer.Y + layerBmp.Height, surface.Height),
                                       backgroundColor: backgroundColor);

                        if (InterimOutputDirPathForDebug != null)
                        {
                            surface.Write(Path.Combine(InterimOutputDirPathForDebug, string.Format(@"s{0:0000}_p{1:0000}_extented.png", model.Id, i)));
                        };
                    }

                    // レイヤ描画
                    // メソッドによって処理を分ける
                    if (layer.ComposingMethod == Seriko.ComposingMethodType.Reduce)
                    {
                        // 新規画像のサイズがベース画像より小さい場合の補正
                        if (layerBmp.Width != surface.Width || layerBmp.Height != surface.Height)
                        {
                            // 画像サイズ補正処理
                            SizeAdjustForComposingBitmap(surface, ref layerBmp, layer.X, layer.Y);

                            // 上記処理の後でもまだサイズが異なる場合（縦が大きいが横は小さいような場合）はUNSUPPORTED
                            if (layerBmp.Width != surface.Width || layerBmp.Height != surface.Height)
                            {
                                throw new IllegalImageFormatException("複数の画像を重ねる際に、2つの画像の間でサイズが異なり、かつ縦横のサイズが矛盾しているような画像が存在します。") { Unsupported = true };
                            }
                        }

                        // 合成
                        // すでに画像サイズ調整を行っているため、0原点とする
                        surface.Composite(layerBmp, 0, 0, CompositeOperator.DstIn);
                    }
                    else if (layer.ComposingMethod == Seriko.ComposingMethodType.Interpolate)
                    {
                        // 新規画像のサイズがベース画像より小さい場合の補正
                        if (layerBmp.Width != surface.Width || layerBmp.Height != surface.Height)
                        {
                            // 画像サイズ補正処理
                            SizeAdjustForComposingBitmap(surface, ref layerBmp, layer.X, layer.Y);

                            // 上記処理の後でもまだサイズが異なる場合（縦が大きいが横は小さいような場合）はUNSUPPORTED
                            if (layerBmp.Width != surface.Width || layerBmp.Height != surface.Height)
                            {
                                throw new IllegalImageFormatException("複数の画像を重ねる際に、2つの画像の間でサイズが異なり、かつ縦横のサイズが矛盾しているような画像が存在します。") { Unsupported = true };
                            }
                        }

                        // 合成
                        // すでに画像サイズ調整を行っているため、0原点とする
                        surface.Composite(layerBmp, 0, 0, CompositeOperator.DstOver);
                    }
                    else
                    {
                        // 上記以外はoverlay扱いで、普通に重ねていく
                        // 重ねる際のメソッドにはoverを使用
                        // <https://www.imagemagick.org/Usage/compose/#over>
                        surface.Composite(layerBmp, layer.X, layer.Y, CompositeOperator.Over);
                    }

                    if (InterimOutputDirPathForDebug != null)
                    {
                        surface.Write(Path.Combine(InterimOutputDirPathForDebug, string.Format(@"s{0:0000}_p{1:0000}.png", model.Id, i)));
                        var msg = string.Format("p{0:000} : {1} method={2} x={3} y={4} (rendering time: {5} ms)", i, Path.GetFileName(layer.Path), layer.ComposingMethod.ToString(), layer.X, layer.Y, sw.ElapsedMilliseconds);
                        File.AppendAllLines(interimLogPath, new[] { msg });
                    };
                }
            }

            // 空白があればトリム
            if (trim)
            {
                surface.Trim();
                surface.RePage(); // 切り抜き後の画像サイズ調整
            }

            // 合成後のサーフェスを返す
            return surface;
        }

        /// <summary>
        /// 立ち絵から顔画像を生成し、MagickImageオブジェクトを返す
        /// </summary>
        /// <param name="faceWidth">顔画像の幅</param>
        /// <param name="faceHeight">顔画像の高さ</param>
        public virtual MagickImage DrawFaceImage(SurfaceModel surfaceModel, int faceWidth, int faceHeight)
        {
            return DrawFaceImage(surfaceModel, faceWidth, faceHeight, null);
        }

        /// <summary>
        /// 立ち絵から顔画像を生成し、MagickImageオブジェクトを返す
        /// </summary>
        /// <param name="faceWidth">顔画像の幅</param>
        /// <param name="faceHeight">顔画像の高さ</param>
        /// <param name="faceTrimRange">顔画像の範囲指定 (left, top, width, height) nullを指定した場合は自動処理</param>
        public virtual MagickImage DrawFaceImage(SurfaceModel surfaceModel, int faceWidth, int faceHeight, Tuple<int, int, int, int> faceTrimRange)
        {
            // まずは立ち絵画像を生成
            var surface = DrawSurface(surfaceModel, trim: false); // 余白はこの段階では切らない

            // 顔画像範囲指定があれば、立ち絵をその範囲で切り抜く
            if (faceTrimRange != null)
            {
                // 切り抜き前のチェック
                CheckFaceTrimRangeBeforeDrawing(surface, faceTrimRange);

                surface.Crop(new ImageMagick.MagickGeometry(faceTrimRange.Item1, faceTrimRange.Item2, faceTrimRange.Item3, faceTrimRange.Item4));
                surface.RePage(); // ページ範囲を更新
            }
            else
            {
                // 顔画像範囲指定がなければ、余白の削除のみ行う
                surface.Trim();
                surface.RePage(); // ページ範囲を更新
            }

            // 縮小率を決定 (幅が収まるように縮小する)
            var scaleRate = faceWidth / (double)surface.Width;
            if (scaleRate > 1.0) scaleRate = 1.0; // 拡大はしない

            // リサイズ処理
            surface.Resize((int)Math.Round(surface.Width * scaleRate), (int)Math.Round(surface.Height * scaleRate));

            // 切り抜く
            surface.Crop(faceWidth, faceHeight);

            // 顔画像のサイズに合うように余白追加
            var backgroundColor = ImageMagick.MagickColor.FromRgba(255, 255, 255, 0);
            surface.Extent(faceWidth, faceHeight,
                           gravity: ImageMagick.Gravity.South,
                           backgroundColor: backgroundColor); // (中央下寄せ)

            return surface;
        }

        /// <summary>
        /// 指定したパスのサーフェスを読み込み、必要な透過処理を施す
        /// </summary>
        public virtual MagickImage LoadAndProcessSurfaceFile(string surfacePath)
        {
            // 画像ファイル読み込み
            var surface = new MagickImage(surfacePath);

            // seriko.use_self_alpha が1、かつアルファチャンネルありの画像の場合は、元画像をそのまま返す
            if (SerikoUseSelfAlpha && surface.HasAlpha)
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
                    var mask = new MagickImage(pnaPath);

                    // Alpha copy処理を行い、グレースケール画像を透過マスクに変換する
                    // <https://www.imagemagick.org/Usage/masking/#alpha_copy>
                    mask.Alpha(AlphaOption.Copy);

                    // 透過度をサーフェス画像にコピー
                    surface.Composite(mask, CompositeOperator.CopyAlpha);

                    return surface;
                }
                else
                {
                    // pnaなしの場合は、画像の左上の色を透過色として設定する
                    var pixels = surface.GetPixels();
                    var basePixel = pixels.GetPixel(0, 0);
                    surface.Transparent(basePixel.ToColor());

                    // 32ビットRGBA画像を強制
                    surface.Alpha(AlphaOption.Set);
                    return surface;
                }
            }
        }

        /// <summary>
        /// ベース画像に新規画像を重ねるときに必要な、サイズの補正を行う
        /// </summary>
        /// <param name="baseImg">ベース画像</param>
        /// <param name="newImg">新規画像 (補正が必要であれば変更される)</param>
        protected virtual void SizeAdjustForComposingBitmap(MagickImage baseImg, ref MagickImage newImg, int? newLayerOffsetX = null, int? newLayerOffsetY = null)
        {
            // オフセットが指定されているかどうか
            var newLayerOffsetSpecified = (newLayerOffsetX.HasValue && newLayerOffsetY.HasValue);

            // 新規画像の方が縦横ともに小さい場合、余白を追加して補正
            if (newImg.Width <= baseImg.Width && newImg.Height <= baseImg.Height)
            {
                var backgroundColor = ImageMagick.MagickColor.FromRgba(255, 255, 255, 0);
                if (newLayerOffsetSpecified)
                {
                    var offsetX = newLayerOffsetX.Value;
                    var offsetY = newLayerOffsetY.Value;

                    // オフセット指定がある場合、オフセット分の余白を左上に追加し、その後に右下に余白追加
                    newImg.Extent(offsetX + newImg.Width, offsetY + newImg.Height,
                                  gravity: ImageMagick.Gravity.Southeast, // 右下寄せ
                                  backgroundColor: backgroundColor); // アルファチャンネルで透過色を設定
                    newImg.RePage(); // ページ情報更新
                    newImg.Extent(baseImg.Width, baseImg.Height,
                                  gravity: ImageMagick.Gravity.Northwest, // 左上寄せ
                                  backgroundColor: backgroundColor); // アルファチャンネルで透過色を設定
                }
                else
                {
                    // オフセット指定がなければ、右下に余白追加
                    newImg.Extent(baseImg.Width, baseImg.Height,
                                  gravity: ImageMagick.Gravity.Northwest, // 左上寄せ
                                  backgroundColor: backgroundColor); // アルファチャンネルで透過色を設定

                }
            }

            // 新規画像の方が縦横ともに大きい場合、余分な幅を切る
            if (newImg.Width >= baseImg.Width && newImg.Height >= baseImg.Height)
            {
                newImg.Crop(baseImg.Width, baseImg.Height);
            }
        }

        /// <summary>
        /// 顔画像の描画前に、指定された切り抜き範囲が適切かどうかチェックする処理
        /// </summary>
        /// <param name="surface">切り抜き元の立ち絵画像</param>
        /// <param name="faceWidth">顔画像の幅</param>
        /// <param name="faceHeight">顔画像の高さ</param>
        /// <param name="faceTrimRange">切り抜き範囲</param>
        protected virtual void CheckFaceTrimRangeBeforeDrawing(MagickImage surface, Tuple<int, int, int, int> faceTrimRange)
        {
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
            var regex = new Regex(string.Format(@"\Asurface0*{0}\.(png|dap|ddp|dfp|dgp|gif|jpg|jpeg|bmp)\z", surfaceNo), RegexOptions.IgnoreCase);
            foreach (var path in Directory.GetFiles(DirPath, string.Format("surface*{0}.*", surfaceNo)))
            {
                var fileName = Path.GetFileName(path);
                var matched = regex.Match(fileName);
                if (matched.Success)
                {
                    var ext = matched.Groups[1].Value.ToLower();
                    if (ext.StartsWith("d") && ext.EndsWith("p"))
                    {
                        throw new UnhandlableShellException(string.Format("{0}形式 (暗号化PNG) の画像ファイルは表示できません。", ext)) { Unsupported = true };
                    }

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
            // descript.txt 内の着せ替え情報を取得
            var groups = GetBindGroupsFromDescript(targetCharacter);

            // まずはprofileからの取得を試みる
            var usingIds = GetEnabledBindGroupIdsFromProfile(targetCharacter);

            // profileから取得できない場合、descript.txt の記述から、初期状態で有効なbindgroup IDのコレクションを作成
            if (usingIds == null)
            {
                usingIds = new HashSet<int>();
                foreach (var pair in groups)
                {
                    var id = pair.Key;
                    var group = pair.Value;

                    if (group.Default)
                    {
                        usingIds.Add(id);
                    }
                }
            }


            // addid指定分も追加
            foreach (var pair in groups)
            {
                var id = pair.Key;
                var group = pair.Value;

                if (usingIds.Contains(id))
                {
                    foreach (var addId in group.AddId)
                    {
                        usingIds.Add(addId);
                    }
                }
            }

            return usingIds;
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
            var defaultRegex = new Regex(string.Format(@"\A{0}\.bindgroup(\d+)\.default\z", targetCharacter));
            var addIdRegex = new Regex(string.Format(@"\A{0}\.bindgroup(\d+)\.addid\z", targetCharacter));

            // descript.txt から着せ替え情報取得
            var bindGroups = new Dictionary<int, BindGroup>();
            foreach (var pair in Descript.Values)
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
                    var matched = addIdRegex.Match(pair.Key);
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
        public class SurfaceModel
        {
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
            /// サーフェスID
            /// </summary>
            public virtual int Id { get; set; }

            /// <summary>
            /// レイヤリスト
            /// </summary>
            public virtual IList<Layer> Layers { get; protected set; }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            public SurfaceModel(int id)
            {
                Id = id;
                Layers = new List<Layer>();
            }
        }

        #endregion
    }
}
