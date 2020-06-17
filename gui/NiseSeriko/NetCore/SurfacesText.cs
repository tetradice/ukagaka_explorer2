using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NiseSeriko
{
    /// <summary>
    /// surfaces.txt を表すクラス
    /// </summary>
    public class SurfacesText
    {
        /// <summary>
        /// ソート種別
        /// </summary>
        public enum SortType
        {
            Asc, Desc
        }

        /// <summary>
        /// スコープ定義
        /// </summary>
        public class Scope
        {
            public virtual List<Tuple<string, string>> Entries { get; set; }

            public Scope()
            {
                Entries = new List<Tuple<string, string>>();
            }
        }

        /// <summary>
        /// SERIKOのバージョン
        /// </summary>
        public virtual Seriko.VersionType SerikoVersion
        {
            get
            {
                if (Scopes.ContainsKey("descript"))
                {
                    var version = Scopes["descript"].Entries.FirstOrDefault(p => p.Item1 == "version");
                    if (version != null && version.Item2 == "1")
                    {
                        return Seriko.VersionType.V2;
                    }
                }
                return Seriko.VersionType.V1;
            }
        }

        /// <summary>
        /// animation定義のソート種別
        /// </summary>
        public virtual SortType AnimationSort
        {
            get
            {
                if (Scopes.ContainsKey("descript"))
                {
                    var animationSort = Scopes["descript"].Entries.FirstOrDefault(p => p.Item1 == "animation-sort");
                    if (animationSort != null && animationSort.Item2 == "ascend")
                    {
                        return SortType.Asc;
                    }
                }
                return SortType.Desc;
            }
        }

        public virtual string Path { get; set; }
        public virtual IDictionary<string, Scope> Scopes { get; protected set; }
        public virtual DateTime LastWriteTime { get; protected set; }

        /// <summary>
        /// 指定したパスの surfaces.txt を読み込む
        /// </summary>
        public static SurfacesText Load(string path)
        {
            var surfaces = new SurfacesText() { Path = path };
            surfaces.Load();
            return surfaces;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SurfacesText()
        {
            Scopes = new Dictionary<string, Scope>();
        }

        /// <summary>
        /// surfaces.txt を読み込む
        /// </summary>
        public virtual void Load()
        {
            var charsetPattern = new Regex(@"charset\s,\s*(.+?)\s*\z");

            // 既存の値はクリア
            Scopes.Clear();
            // ファイル更新日時セット
            LastWriteTime = File.GetLastWriteTime(Path);

            // まずはエンコーディングの判定を行うために、対象ファイルの内容を1行ずつ読み込む
            var sjis = Encoding.GetEncoding(932);
            var encoding = sjis;
            var preLines = File.ReadLines(Path, encoding: sjis);
            foreach (var line in preLines)
            {
                // charset行が見つかった場合は、文字コードを設定してループ終了
                var matched = charsetPattern.Match(line);
                if (matched.Success)
                {
                    var charset = matched.Groups[1].Value;
                    encoding = Encoding.GetEncoding(charset);
                    break;
                }

                // 空行でない行が見つかった場合は、文字コードを設定せずにループ終了
                if (!string.IsNullOrEmpty(line.Trim()))
                {
                    break;
                }
            }

            // エンコーディングが確定したら、読み込みメイン処理
            var lines = File.ReadLines(Path, encoding: encoding);
            string preTrimmedLine = null; // 前行
            string currentScope = null; // 現在のスコープ 
            var valueSeparators = new[] { ',' };

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // コメントは無視 (仕様にはないが、行の途中からコメントが始まっている場合も許容)
                if (trimmedLine.Contains("//"))
                {
                    trimmedLine = trimmedLine.Remove(trimmedLine.IndexOf("//")).Trim();
                }

                if (string.IsNullOrEmpty(trimmedLine))  // 空行
                {
                    // 空行の場合はスキップ
                    continue;
                }
                else if (trimmedLine == "{")  // 開きブレス処理
                {

                    // 直前行がない場合は不正としてスキップ
                    if (preTrimmedLine == null) continue;

                    // スコープを設定
                    currentScope = preTrimmedLine; // 直前行の記載内容をスコープ名とする
                    continue;
                }
                else if (trimmedLine.EndsWith("{"))  // 開きブレス かつスコープ名がある場合の処理 (仕様上は不正)
                {
                    // スコープを設定

                    currentScope = trimmedLine.Replace("{", "").Trim(); // ブレスを削除し、前後の空白を削除した後の結果をスコープ名とする
                    continue;
                }

                else if (trimmedLine == "}")  // 閉じブレス
                {
                    // スコープ内にいない場合は不正としてスキップ
                    if (currentScope == null) continue;

                    // スコープをクリア
                    currentScope = null;
                    continue;
                }
                else if (trimmedLine.Contains(","))   // 上記以外でカンマを含む
                {
                    var pair = trimmedLine.Split(valueSeparators, 2);

                    // スコープ内にいれば、対象スコープの値として追加
                    if (currentScope != null)
                    {
                        if (!Scopes.ContainsKey(currentScope)) Scopes[currentScope] = new Scope();
                        Scopes[currentScope].Entries.Add(Tuple.Create(pair[0].Trim(), pair[1].Trim())); // 仕様上は不正と思われるが、たまに大文字混じりでキーを指定しているケースがあるためdowncase
                    }
                }

                // 前行として保存
                preTrimmedLine = trimmedLine;
            }
        }

        /// <summary>
        /// 指定したサーフェスIDについて、alias定義を辿り、実際のサーフェスIDを取得
        /// </summary>
        /// <param name="targetCharacter">対象のキャラクタ (sakura, kero, char2, char3, ...)</param>
        public virtual int FindActualSurfaceId(string targetCharacter, int surfaceId)
        {
            // alias定義が存在し、その中に指定したIDと対応する定義があるかどうかチェック
            var name = string.Format("{0}.surface.alias", targetCharacter);
            if (Scopes.ContainsKey(name))
            {
                var entry = Scopes[name].Entries.FirstOrDefault(e => e.Item1 == surfaceId.ToString());
                if (entry != null)
                {
                    // エイリアス先IDを取得
                    var targetIds = entry.Item2.Trim(new[] { '[', ']' }).Split(',');

                    // 1件以上存在し、かつ先頭が数値であれば、そのエイリアス先IDを返す
                    int targetSurfaceId;
                    if (targetIds.Any() && int.TryParse(targetIds[0], out targetSurfaceId))
                    {
                        return targetSurfaceId;
                    }
                }
            }

            // 上記で対応するalias定義が見つからなければ、指定したサーフェスIDをそのまま返す
            return surfaceId;
        }

        /// <summary>
        /// サーフェス定義情報。GetSurfaceDefinitionInfoでサーフェスIDを指定して取得する
        /// </summary>
        public class SurfaceDefinitionInfo
        {
            public IList<Seriko.Element> Elements = new List<Seriko.Element>();
            public IDictionary<int, Seriko.Animation> Animations = new Dictionary<int, Seriko.Animation>();
        }

        /// <summary>
        /// 指定したサーフェスIDと対応するelement, 着せ替え定義を取得する
        /// </summary>
        /// <returns></returns>
        public virtual SurfaceDefinitionInfo GetSurfaceDefinitionInfo(int surfaceId)
        {
            var defInfo = new SurfaceDefinitionInfo();

            // パターンの生成
            var elemKeyRegex = new Regex(@"\Aelement(\d+)\z");
            var elemValueRegex = new Regex(@"\A(?<method>[a-z]+)\s*,\s*(?<filename>[^,]+)\s*,\s*(?<offsetx>\d+)\s*,\s*(?<offsety>\d+)\z");

            // SERIKO V1, V2のパターンを両方とも解釈 (version表記と矛盾していても読む)
            var intervalKeyRegexV2 = new Regex(@"\Aanimation(\d+).interval");
            var intervalKeyRegexV1 = new Regex(@"\A(\d+)interval");
            var patKeyRegexV2 = new Regex(@"\Aanimation(?<animID>\d+).pattern(?<patID>\d+)\z");
            var patKeyRegexV1 = new Regex(@"\A(?<animID>\d+)pattern(?<patID>\d+)\z");
            var patValueRegexV2 = new Regex(@"\A(?<method>[a-z]+)\s*,\s*(?<surfaceID>[\d-]+)\s*,\s*[\d-]+(?:,(?<offsetX>[^,]+)\s*,\s*(?<offsetY>[^,]+))?\z");
            var patValueRegexV1 = new Regex(@"\A(?<surfaceID>[\d-]+)\s*,\s*[\d-]+\s*,\s*(?<method>[a-z]+)(?:,(?<offsetX>[^,]+)\s*,\s*(?<offsetY>[^,]+))\z");

            // スコープ1つごとに処理
            foreach (var pair in Scopes)
            {
                var scopeName = pair.Key;
                var scopeValues = pair.Value.Entries;

                // サーフェスIDとマッチするスコープでなければスキップ
                if (!IsMatchingScope(scopeName, surfaceId)) continue;

                // element指定があれば追加
                foreach (var valuePair in scopeValues)
                {
                    // element処理
                    {
                        var matched = elemKeyRegex.Match(valuePair.Item1);
                        if (matched.Success)
                        {
                            var elem = new Seriko.Element { Id = int.Parse(matched.Groups[1].Value) };

                            // 指定書式に従っていればパースして、エレメントリストに追加
                            var matched2 = elemValueRegex.Match(valuePair.Item2);
                            if (matched2.Success)
                            {
                                var methodValue = matched2.Groups["method"].Value;

                                elem.Method = Seriko.ComposingMethodType.Overlay;
                                if (methodValue == "asis") elem.Method = Seriko.ComposingMethodType.Asis;
                                if (methodValue == "base") elem.Method = Seriko.ComposingMethodType.Base;
                                if (methodValue == "interpolate") elem.Method = Seriko.ComposingMethodType.Interpolate;
                                if (methodValue == "overlay") elem.Method = Seriko.ComposingMethodType.Overlay;
                                if (methodValue == "overlayfast") elem.Method = Seriko.ComposingMethodType.OverlayFast;
                                if (methodValue == "reduce") elem.Method = Seriko.ComposingMethodType.Reduce;
                                if (methodValue == "replace") elem.Method = Seriko.ComposingMethodType.Replace;

                                elem.FileName = matched2.Groups["filename"].Value.ToLower().Trim();
                                if (!elem.FileName.EndsWith(".png")) elem.FileName = elem.FileName + ".png"; // 拡張子が .png でなければ自動補完 (れいちぇるなど対応)
                                var offsetX = 0;
                                if (matched2.Groups["offsetx"].Success) int.TryParse(matched2.Groups["offsetx"].Value, out offsetX);
                                elem.OffsetX = offsetX;
                                var offsetY = 0;
                                if (matched2.Groups["offsety"].Success) int.TryParse(matched2.Groups["offsety"].Value, out offsetY);
                                elem.OffsetY = offsetY;

                                defInfo.Elements.Add(elem);
                            }

                            continue;
                        }
                    }

                    // animation.interval処理
                    {
                        // V1, V2両方の形式を読み込み対象とする
                        var id = -1;
                        var matched = intervalKeyRegexV2.Match(valuePair.Item1);
                        if (matched.Success)
                        {
                            id = int.Parse(matched.Groups[1].Value);
                        }
                        else
                        {
                            var matchedV1 = intervalKeyRegexV1.Match(valuePair.Item1);
                            if (matchedV1.Success)
                            {
                                id = int.Parse(matchedV1.Groups[1].Value);
                            }
                        }

                        if (id >= 0)
                        {
                            // animation定義が未登録であれば追加
                            if (!defInfo.Animations.ContainsKey(id))
                            {
                                defInfo.Animations.Add(id, new Seriko.Animation());
                            }

                            // intervalの処理
                            if (valuePair.Item2 == "bind")
                            {
                                // bind単体の場合は全パターン表示、かつOffset指定を絶対座標とみなす
                                defInfo.Animations[id].PatternDisplayForStaticImage = Seriko.Animation.PatternDisplayType.All;
                                defInfo.Animations[id].OffsetInterpriting = Seriko.Animation.OffsetInterpritingType.Absolute;
                                defInfo.Animations[id].UsingBindGroup = true;
                            }
                            else if (valuePair.Item2.Contains("sometimes")
                                     || valuePair.Item2.Contains("rarely")
                                     || valuePair.Item2.Contains("random")
                                     || valuePair.Item2.Contains("periodic")
                                     || valuePair.Item2.Contains("runonce")
                                     || valuePair.Item2.Contains("always"))
                            {
                                // bindでなく上記指定を含む場合は最終パターンのみ表示、かつOffset指定を相対座標とみなす
                                defInfo.Animations[id].PatternDisplayForStaticImage = Seriko.Animation.PatternDisplayType.LastOnly;
                                defInfo.Animations[id].OffsetInterpriting = Seriko.Animation.OffsetInterpritingType.RelativeFromPreviousFrame;

                                // bindも含む場合はbindgroupも見る
                                if (valuePair.Item2.Contains("bind"))
                                {
                                    defInfo.Animations[id].UsingBindGroup = true;
                                }
                            }
                            continue;
                        }
                    }


                    // animation.pattern処理 (V2形式)
                    {
                        var matched = patKeyRegexV2.Match(valuePair.Item1);
                        if (matched.Success)
                        {
                            var animId = int.Parse(matched.Groups["animID"].Value);
                            var patId = int.Parse(matched.Groups["patID"].Value);

                            // 値が指定書式に従っていれば、パースしてpattern定義に追加
                            var matched2 = patValueRegexV2.Match(valuePair.Item2);
                            if (matched2.Success)
                            {
                                var methodValue = matched2.Groups["method"].Value;
                                var patternSurfaceId = int.Parse(matched2.Groups["surfaceID"].Value);
                                var offsetX = 0;
                                if (matched2.Groups["offsetX"].Success) int.TryParse(matched2.Groups["offsetX"].Value, out offsetX);
                                var offsetY = 0;
                                if (matched2.Groups["offsetY"].Success) int.TryParse(matched2.Groups["offsetY"].Value, out offsetY);

                                addAnimationPattern(
                                    defInfo.Animations
                                    , animId
                                    , patId
                                    , methodValue
                                    , patternSurfaceId
                                    , offsetX
                                    , offsetY
                                );
                            }

                            continue;
                        }
                    }

                    // animation.pattern処理 (V1形式)
                    {
                        var matched = patKeyRegexV1.Match(valuePair.Item1);
                        if (matched.Success)
                        {
                            var animId = int.Parse(matched.Groups["animID"].Value);
                            var patId = int.Parse(matched.Groups["patID"].Value);

                            // 値が指定書式に従っていれば、パースしてpattern定義に追加
                            var matched2 = patValueRegexV1.Match(valuePair.Item2);
                            if (matched2.Success)
                            {
                                var methodValue = matched2.Groups["method"].Value;
                                var patternSurfaceId = int.Parse(matched2.Groups["surfaceID"].Value);
                                var offsetX = 0;
                                if (matched2.Groups["offsetX"].Success) int.TryParse(matched2.Groups["offsetX"].Value, out offsetX);
                                var offsetY = 0;
                                if (matched2.Groups["offsetY"].Success) int.TryParse(matched2.Groups["offsetY"].Value, out offsetY);

                                addAnimationPattern(
                                    defInfo.Animations
                                    , animId
                                    , patId
                                    , methodValue
                                    , patternSurfaceId
                                    , offsetX
                                    , offsetY
                                );
                            }

                            continue;
                        }
                    }
                }

            }

            // 結果を返す
            return defInfo;
        }

        /// <summary>
        /// アニメーションパターンの追加処理
        /// </summary>
        protected static void addAnimationPattern(
            IDictionary<int, Seriko.Animation> animations
            , int animId
            , int patId
            , string methodValue
            , int patternSurfaceId
            , int offsetX
            , int offsetY
        )
        {
            // animation定義が未登録であれば追加
            if (!animations.ContainsKey(animId))
            {
                animations.Add(animId, new Seriko.Animation());
            }
            var anim = animations[animId];

            // patternを生成して追加
            var pat = new Seriko.Animation.Pattern() { Id = patId };
            pat.Method = Seriko.ComposingMethodType.Overlay;
            if (methodValue == "add") pat.Method = Seriko.ComposingMethodType.Add;
            if (methodValue == "asis") pat.Method = Seriko.ComposingMethodType.Asis;
            if (methodValue == "base") pat.Method = Seriko.ComposingMethodType.Base;
            if (methodValue == "bind") pat.Method = Seriko.ComposingMethodType.Bind;
            if (methodValue == "insert") pat.Method = Seriko.ComposingMethodType.Insert;
            if (methodValue == "interpolate") pat.Method = Seriko.ComposingMethodType.Interpolate;
            if (methodValue == "overlay") pat.Method = Seriko.ComposingMethodType.Overlay;
            if (methodValue == "overlayfast") pat.Method = Seriko.ComposingMethodType.OverlayFast;
            if (methodValue == "reduce") pat.Method = Seriko.ComposingMethodType.Reduce;
            if (methodValue == "replace") pat.Method = Seriko.ComposingMethodType.Replace;

            pat.SurfaceId = patternSurfaceId;
            pat.OffsetX = offsetX;
            pat.OffsetY = offsetY;

            anim.Patterns.Add(pat);
        }


        /// <summary>
        /// スコープ名とサーフェスIDを照合して、そのサーフェスIDに関する定義かどうかを判定
        /// </summary>
        public virtual bool IsMatchingScope(string scopeName, int surfaceId)
        {
            // スコープ名が "surface" から始まっていなければマッチしない
            if (!scopeName.StartsWith("surface")) return false;
            if (scopeName.Contains(".append"))
            {
            }

            // スコープ名から "surface" および "surface.append" を除去し、半角カンマで分割
            var idSpecs = scopeName.Replace("surface", "").Replace(".append", "").Split(',');

            // ID指定1つずつ処理  (!指定を優先するために辞書順ソートしてから処理する)
            foreach (var idSpec in idSpecs.OrderBy(x => x))
            {
                // 正規表現で必要な値をキャプチャ
                var matched = Regex.Match(idSpec, @"(\!)?(\d+)(?:-(\d+))?");
                if (matched.Success)
                {
                    var negative = !string.IsNullOrEmpty(matched.Groups[1].Value);
                    var left = int.Parse(matched.Groups[2].Value);
                    int? right = null;
                    if (!string.IsNullOrEmpty(matched.Groups[3].Value)) right = int.Parse(matched.Groups[3].Value);

                    // 範囲指定かどうかで処理を分ける
                    if (right != null)
                    {
                        // 範囲指定
                        // 範囲の中に収まっていればマッチ
                        if (left <= surfaceId && surfaceId <= right)
                        {
                            // 通常なら「マッチ成功」とみなし、!指定なら「マッチ失敗」とみなす
                            return !negative;
                        }
                    }
                    else
                    {
                        // 個別指定
                        // 指定値と一致するならマッチ
                        if (surfaceId == left)
                        {
                            // 通常なら「マッチ成功」とみなし、!指定なら「マッチ失敗」とみなす
                            return !negative;
                        }
                    }
                }
            }

            // どの指定ともマッチしなければ、マッチ失敗
            return false;
        }
    }
}
