using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;
using ImageMagick;
using NiseSeriko;

namespace SerikoCamera
{
    internal class Options
    {
        [Option('o', "output", HelpText = "出力先の画像ファイルパス\n省略時はカレントディレクトリに、 --target に基づく\nファイル名で出力する (pair.png, p0.pngなど)")]
        public string OutputPath { get; set; }

        [Option('t', "target", HelpText = @"
撮影対象。省略時は 'pair'

  pair - sakura側とkero側の立ち絵を並べて1枚の画像を生成
  p0 - sakura側の立ち絵画像を生成
  p1 - kero側の立ち絵画像を生成
  p0-face - sakura側の顔画像を生成
  p1-face - kero側の顔画像を生成")]
        public string Target { get; set; }

        [Option("pair-margin", HelpText = "target = 'pair' 時に二人の間に空ける間隔（ピクセル単位）\nただし、kero側の立ち絵がダミー画像と思われる場合は\n間隔を入れない。省略時は64")]
        public int? PairMargin { get; set; }

        [Option("padding", HelpText = "画像の周囲に入れる余白の幅（ピクセル単位）\ntop,right,bottom,leftの順で指定\n省略時は、顔画像は余白なし、それ以外は '16,32,0,32'\n(上16px、左右32pxの余白を空ける)")]
        public string Padding { get; set; }

        [Option("face-size", HelpText = "target = 'p0-face', 'p1-face' 時の\n顔画像サイズ（ピクセル単位）。width,heightの順で指定\n省略時は '120,120'\n(縦横ともに120px、ゴーストエクスプローラ通準拠)")]
        public string FaceSize { get; set; }

        [Option("debug", HelpText = "合成途中の中間画像ファイルやログファイルを追加出力する\n出力先は、(画像ファイルの出力先ディレクトリ)/_interim")]
        public bool Debug { get; set; }

        [Value(0, Required = true, HelpText = "変換するゴースト or シェルのディレクトリパス")]
        public string TargetDirPath { get; set; }

        [Usage()]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new[] {
                    new Example("通常の変換", new Options() {TargetDirPath = "./ghost/sakura"})
                    , new Example("出力先を指定", new Options() { OutputPath = "./out/sakura_p0.png", TargetDirPath = "./ghost/sakura"})
                    , new Example("kero側の画像のみ出力", new Options() { Target = "p1", TargetDirPath = "./ghost/sakura"})
                    , new Example("顔画像を120x100サイズで出力", new Options() { Target = "p0-face", FaceSize = "120,100", TargetDirPath = "./ghost/sakura"})
                    , new Example("シェルを指定して変換", new Options() {TargetDirPath = "./ghost/sakura/shell/summer_dress"})
                    , new Example("余白を指定", new Options() {PairMargin=30, Padding="20,40,0,40", TargetDirPath = "./ghost/sakura"})
               };
            }
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
#if NETCOREAPP
            // .NET Core Appの場合、Shift JISなども扱えるようにするためにエンコーディングプロバイダを追加
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opt =>
                {
                    // オプションチェック
                    var target = (opt.Target == null ? "pair" : opt.Target.ToLower());
                    if (opt.PairMargin != null && target != "pair")
                    {
                        Console.Error.WriteLine($"ERROR: pair marginは target = 'pair' の場合のみ指定できます。");
                        return;
                    }

                    if (opt.FaceSize != null && target != "p0-face" && target != "p1-face")
                    {
                        Console.Error.WriteLine($"ERROR: face sizeは target = 'p0-face' or 'p1-face' の場合のみ指定できます。");
                        return;
                    }

                    var pairMargin = opt.PairMargin.GetValueOrDefault(64);
                    if (opt.PairMargin < 0)
                    {
                        Console.Error.WriteLine($"ERROR: pair margin指定 '{opt.PairMargin}' が正しくありません。 0以上の整数で指定してください。");
                        return;
                    }

                    Tuple<int, int> faceSizes;
                    if (opt.Padding != null)
                    {
                        var matched = Regex.Match(opt.Padding, @"^(\d{1,5}),(\d{1,5})$");
                        if (matched.Success)
                        {
                            faceSizes = Tuple.Create(
                                  int.Parse(matched.Groups[1].Value)
                                , int.Parse(matched.Groups[2].Value)
                            );
                        }
                        else
                        {
                            Console.Error.WriteLine($"ERROR: face size指定 '{opt.Padding}' が正しくありません。 '120,100' のようなフォーマットで指定してください。");
                            return;
                        }
                    }
                    else
                    {
                        faceSizes = Tuple.Create(120, 120);
                    }

                    // 出力先決定
                    var outputPath = opt.OutputPath ?? $"./{target}.png";
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                    // シェルを読み込む
                    Shell shell;
                    var interimOutputDirPathForDebug = (opt.Debug ? Path.Combine(Path.GetDirectoryName(outputPath), "_interim") : null);
                    if (Ghost.IsGhostDir(opt.TargetDirPath))
                    {
                        // ゴーストフォルダのルートが指定された場合
                        var ghost = Ghost.Load(opt.TargetDirPath);
                        var shellDirPath = Path.Combine(ghost.DirPath, ghost.CurrentShellRelDirPath);
                        shell = Shell.Load(shellDirPath, ghost.SakuraDefaultSurfaceId, ghost.KeroDefaultSurfaceId, interimOutputDirPathForDebug: interimOutputDirPathForDebug);
                    }
                    else if (Shell.IsShellDir(opt.TargetDirPath))
                    {
                        // シェルフォルダが指定された場合
                        var shellDirPath = opt.TargetDirPath;
                        shell = Shell.Load(shellDirPath, 0, 10, interimOutputDirPathForDebug: interimOutputDirPathForDebug); // サーフェス番号はデフォルトの0, 10とする
                    }
                    else
                    {
                        Console.Error.WriteLine($"ERROR: 指定したフォルダ '{opt.TargetDirPath}' が、ゴーストフォルダでもシェルフォルダでもありませんでした。");
                        return;
                    }

                    // 出力
                    MagickImage dest;
                    switch (target)
                    {
                        case "pair":
                            var sakuraImg = shell.DrawSurface(shell.SakuraSurfaceModel);
                            var keroImg = shell.DrawSurface(shell.KeroSurfaceModel);

                            // kero側がダミー画像でないかどうかを判定
                            var keroIsDummy = false;
                            if (keroImg.Width <= 2 && keroImg.Height <= 2) keroIsDummy = true; // 縦横2px以下の画像はダミー

                            sakuraImg.Extent(sakuraImg.Width + keroImg.Width + (keroIsDummy ? 0 : pairMargin), sakuraImg.Height,
                                             gravity: Gravity.East,
                                             backgroundColor: MagickColor.FromRgba(255, 255, 255, 0));
                            sakuraImg.Composite(keroImg, Gravity.Southwest, CompositeOperator.Over);
                            dest = sakuraImg;

                            break;

                        case "p0":
                            dest = shell.DrawSurface(shell.SakuraSurfaceModel);
                            break;

                        case "p1":
                            dest = shell.DrawSurface(shell.KeroSurfaceModel);
                            break;

                        case "p0-face":
                            dest = shell.DrawFaceImage(shell.SakuraSurfaceModel, faceSizes.Item1, faceSizes.Item2);
                            break;

                        case "p1-face":
                            dest = shell.DrawFaceImage(shell.KeroSurfaceModel, faceSizes.Item1, faceSizes.Item2);

                            break;

                        default:
                            Console.Error.WriteLine($"ERROR: target '{opt.Target}' は処理できません。");
                            return;
                    }

                    // パディングを入れる
                    Tuple<int, int, int, int> paddings;
                    if (opt.Padding != null)
                    {
                        var matched = Regex.Match(opt.Padding, @"^(\d{1,5}),(\d{1,5}),(\d{1,5}),(\d{1,5})$");
                        if (matched.Success)
                        {
                            paddings = Tuple.Create(
                                  int.Parse(matched.Groups[1].Value)
                                , int.Parse(matched.Groups[2].Value)
                                , int.Parse(matched.Groups[3].Value)
                                , int.Parse(matched.Groups[4].Value)
                            );
                        }
                        else
                        {
                            Console.Error.WriteLine($"ERROR: padding指定 '{opt.Padding}' が正しくありません。 '16,32,0,16' のようなフォーマットで指定してください。");
                            return;
                        }
                    }
                    else
                    {
                        if (target == "p0-face" || target == "p1-face")
                        {
                            paddings = Tuple.Create(0, 0, 0, 0);
                        }
                        else
                        {
                            paddings = Tuple.Create(16, 32, 0, 32);
                        }
                    }

                    var topPadding = paddings.Item1;
                    var rightPadding = paddings.Item2;
                    var bottomPadding = paddings.Item3;
                    var leftPadding = paddings.Item4;
                    dest.Extent(dest.Width + rightPadding, dest.Height + topPadding,
                                gravity: Gravity.Southwest,
                                backgroundColor: MagickColor.FromRgba(255, 255, 255, 0));
                    dest.Extent(dest.Width + leftPadding, dest.Height + bottomPadding,
                                gravity: Gravity.Northeast,
                                backgroundColor: MagickColor.FromRgba(255, 255, 255, 0));
                    dest.RePage();

                    dest.Write(outputPath);
                    Console.WriteLine($"output -> {outputPath}");

                })
                .WithNotParsed(err =>
                {
                    Debug.WriteLine(err);
                });
        }
    }
}
