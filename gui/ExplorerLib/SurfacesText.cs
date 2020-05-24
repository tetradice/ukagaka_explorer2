using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExplorerLib
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
            public virtual Dictionary<string, string> Values { get; set; }

            public Scope()
            {
                this.Values = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// SERIKOのバージョン
        /// </summary>
        public virtual Seriko.VersionType SerikoVersion {
            get {
                if(
                    Scopes.ContainsKey("descript")
                    && Scopes["descript"].Values.ContainsKey("version")
                    && Scopes["descript"].Values["version"] == "1"
                )
                {
                    return Seriko.VersionType.V2;
                } else
                {
                    return Seriko.VersionType.V1;
                }
            }
        }

        /// <summary>
        /// animation定義のソート種別
        /// </summary>
        public virtual SortType AnimationSort
        {
            get
            {
                if (
                    Scopes.ContainsKey("descript")
                    && Scopes["descript"].Values.ContainsKey("animation-sort")
                    && Scopes["descript"].Values["animation-sort"] == "ascend"
                )
                {
                    return SortType.Asc;
                }
                else
                {
                    return SortType.Desc;
                }
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
            var entryPattern = new Regex(@"(.+?)\s*,\s*(.+?)\s*\z");

            // 既存の値はクリア
            Scopes.Clear();
            // ファイル更新日時セット
            this.LastWriteTime = File.GetLastWriteTime(Path);

            // まずはエンコーディングの判定を行うために、対象ファイルの内容を1行ずつ読み込む
            var sjis = Encoding.GetEncoding(932);
            var encoding = sjis;
            var preLines = File.ReadLines(Path, encoding: sjis);
            foreach (string line in preLines)
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

            foreach (string line in lines)
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
                        Scopes[currentScope].Values[pair[0].ToLower()] = pair[1]; // 仕様上は不正と思われるが、たまに大文字混じりでキーを指定しているケースがあるためdowncase
                    }
                }

                // 前行として保存
                preTrimmedLine = trimmedLine;
            }
        }

        /// <summary>
        /// 指定したサーフェスIDと対応するelement, 着せ替え定義のリストを取得する
        /// </summary>
        /// <returns></returns>
        public virtual Tuple<IList<Seriko.Element>, IDictionary<int, Seriko.Animation>> GetElementsAndAnimations(int surfaceId)
        {
            var elements = new List<Seriko.Element>();
            var animations = new Dictionary<int, Seriko.Animation> ();

            // パターンの生成
            var elemKeyRegex = new Regex(@"\Aelement(\d+)\z");
            var elemValueRegex = new Regex(@"\A(?<method>[a-z]+),(?<filename>[^,]+),(?<offsetx>\d+),(?<offsety>\d+)\z");

            // SERIKOバージョンでパターンを分ける
            var intervalKeyRegex = (SerikoVersion == Seriko.VersionType.V2
                                    ? new Regex(@"\Aanimation(\d+).interval")
                                    : new Regex(@"\A(\d+)interval"));
            var patKeyRegex = (SerikoVersion == Seriko.VersionType.V2
                               ? new Regex(@"\Aanimation(?<animID>\d+).pattern(?<patID>\d+)\z")
                               : new Regex(@"\A(?<animID>\d+)pattern(?<patID>\d+)\z"));
            var patValueRegex = (SerikoVersion == Seriko.VersionType.V2
                                 ? new Regex(@"\A(?<method>[a-z]+),(?<surfaceID>[\d-]+),[\d-]+(?:,(?<offsetX>[\d-]+),(?<offsetY>[\d-]+))?\z")
                                 : new Regex(@"\A(?<surfaceID>[\d-]+),[\d-]+,(?<method>[a-z]+)(?:,(?<offsetX>[\d-]+),(?<offsetY>[\d-]+))\z"));

            // スコープ1つごとに処理
            foreach (var pair in this.Scopes)
            {
                var scopeName = pair.Key;
                var scopeValues = pair.Value.Values;

                // サーフェスIDとマッチするスコープでなければスキップ
                if (!IsMatchingScope(scopeName, surfaceId)) continue;

                // element指定があれば追加
                foreach (var valuePair in scopeValues)
                {
                    // element処理
                    {
                        var matched = elemKeyRegex.Match(valuePair.Key);
                        if (matched.Success)
                        {
                            var elem = new Seriko.Element { Id = int.Parse(matched.Groups[1].Value) };

                            // 指定書式に従っていればパースして、エレメントリストに追加
                            var matched2 = elemValueRegex.Match(valuePair.Value);
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

                                elem.FileName = matched2.Groups["filename"].Value;
                                if (!elem.FileName.EndsWith(".png")) elem.FileName = elem.FileName + ".png"; // 拡張子が .png でなければ自動補完 (れいちぇるなど対応)
                                elem.OffsetX = int.Parse(matched2.Groups["offsetx"].Value);
                                elem.OffsetY = int.Parse(matched2.Groups["offsety"].Value);

                                elements.Add(elem);
                            }

                            continue;
                        }
                    }

                    // animation.interval処理 (bind, もしくはbind+○○のみを対象とする)
                    {

                        var matched = intervalKeyRegex.Match(valuePair.Key);
                        if (matched.Success)
                        {
                            var id = int.Parse(matched.Groups[1].Value);

                            // animation定義が未登録であれば追加
                            if (!animations.ContainsKey(id))
                            {
                                animations.Add(id, new Seriko.Animation());
                            }

                            // intervalの処理
                            if (valuePair.Value == "bind")
                            {
                                // bind単体の場合は全パターン表示
                                animations[id].PatternDisplayForStaticImage = Seriko.Animation.PatternDisplayType.All;
                                animations[id].UsingBindGroup = true;
                            }
                            else if (valuePair.Value.Contains("sometimes")
                                     || valuePair.Value.Contains("rarely")
                                     || valuePair.Value.Contains("random")
                                     || valuePair.Value.Contains("periodic")
                                     || valuePair.Value.Contains("runonce"))
                            {
                                // 上記指定を含む場合は最終パターンのみ表示
                                animations[id].PatternDisplayForStaticImage = Seriko.Animation.PatternDisplayType.LastOnly;

                                // bindも含む場合はbindgroupも見る
                                if (valuePair.Value.Contains("bind"))
                                {
                                    animations[id].UsingBindGroup = true;
                                }
                            }
                            continue;
                        }
                    }

                    // animation.pattern処理
                    {
                        var matched = patKeyRegex.Match(valuePair.Key);
                        if (matched.Success)
                        {
                            var animId = int.Parse(matched.Groups["animID"].Value);
                            var patId = int.Parse(matched.Groups["patID"].Value);

                            // 値が指定書式に従っていれば、パースしてpattern定義に追加
                            var matched2 = patValueRegex.Match(valuePair.Value);
                            if (matched2.Success)
                            {
                                var methodValue = matched2.Groups["method"].Value;
                                var patternSurfaceId = int.Parse(matched2.Groups["surfaceID"].Value);
                                var offsetX = (matched2.Groups["offsetX"].Success ? int.Parse(matched2.Groups["offsetX"].Value) : 0);
                                var offsetY = (matched2.Groups["offsetY"].Success ? int.Parse(matched2.Groups["offsetY"].Value) : 0);

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


                            continue;
                        }
                    }
                }

            }

            // 結果を返す
            return Tuple.Create<IList<Seriko.Element>, IDictionary<int, Seriko.Animation>>(elements, animations);
        }

        /// <summary>
        /// スコープ名とサーフェスIDを照合して、そのサーフェスIDに関する定義かどうかを判定
        /// </summary>
        public virtual bool IsMatchingScope(string scopeName, int surfaceId)
        {
            // スコープ名が "surface" から始まっていなければマッチしない
            if (!scopeName.StartsWith("surface")) return false;

            // "surface.append" 形式の定義であればフラグを立てる
            var appending = false;
            if (scopeName.Contains(".append"))
            {
                appending = true;
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
                    if(!string.IsNullOrEmpty(matched.Groups[3].Value)) right = int.Parse(matched.Groups[3].Value);

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
