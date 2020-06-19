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
        ////[Option("shell", HelpText = "使用するシェルのディレクトリ名")]
        ////public string Shell { get; set; }

        [Option('o', "output", HelpText = "出力先の画像ファイルパス\n省略時はカレントディレクトリに、 --target に従うファイル名で出力\n(pair.png, p0.pngなど)")]
        public string OutputPath { get; set; }

        [Option('t', "target", HelpText = @"
撮影対象。省略時は 'pair'

  pair - sakura側とkero側の立ち絵を並べて1枚の画像を生成
  p0 - sakura側の立ち絵画像を生成
  p1 - kero側の立ち絵画像を生成
  p0face - sakura側の顔画像を生成
  p1face - kero側の顔画像を生成
")]
        public string Target { get; set; } = "pair";

        [Option("pair-margin", HelpText = "target = 'pair' 時に二人の間に入れる空白（ピクセル単位）\n省略時は64")]
        public int PairMargin { get; set; } = 64;

        [Option("padding", HelpText = "画像の周囲に入れる余白の幅（ピクセル単位）\ntop,right,bottom,leftの順で指定\n省略時は '16,32,0,32' (上16px、左右32pxの余白を空ける)")]
        public string Padding { get; set; }

        [Option("debug", HelpText = "合成途中の中間画像ファイルやログファイルを追加出力する\n出力先は、(画像ファイルの出力先ディレクトリ)/_interim")]
        public bool Debug { get; set; }

        [Value(0, Required = true, HelpText = "変換するゴーストのディレクトリパス")]
        public string GhostDirPath { get; set; }

        [Usage()]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new[] {
                    new Example("通常の変換", new Options() {GhostDirPath = "./ghost/sakura"})
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
                    var ghost = Ghost.Load(opt.GhostDirPath);
                    var shellDirPath = Path.Combine(ghost.DirPath, ghost.CurrentShellRelDirPath);

                    var target = opt.Target.ToLower();

                    var outputPath = opt.OutputPath ?? $"./{target}.png";
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                    var interimOutputDirPathForDebug = (opt.Debug ? Path.Combine(Path.GetDirectoryName(outputPath), "_interim") : null);
                    var shell = Shell.Load(shellDirPath, ghost.SakuraDefaultSurfaceId, ghost.KeroDefaultSurfaceId, interimOutputDirPathForDebug: interimOutputDirPathForDebug);


                    MagickImage dest;
                    switch (target)
                    {
                        case "pair":
                            var sakuraImg = shell.DrawSurface(shell.SakuraSurfaceModel);
                            var keroImg = shell.DrawSurface(shell.KeroSurfaceModel);
                            sakuraImg.Extent(sakuraImg.Width + keroImg.Width + opt.PairMargin, sakuraImg.Height,
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

                        case "p0face":
                            dest = shell.DrawFaceImage(shell.SakuraSurfaceModel, 120, 100);
                            break;

                        case "p1face":
                            dest = shell.DrawFaceImage(shell.KeroSurfaceModel, 120, 100);

                            break;

                        default:
                            Console.Error.WriteLine($"ERROR: target '{opt.Target}' は処理できません。");
                            return;
                    }

                    // パディングを入れる
                    IList<int> paddings;
                    if (opt.Padding != null)
                    {
                        var matched = Regex.Match(opt.Padding, @"^(\d{1,5}),(\d{1,5}),(\d{1,5}),(\d{1,5})$");
                        if (matched.Success)
                        {
                            paddings = new[]
                            {
                                  int.Parse(matched.Groups[1].Value)
                                , int.Parse(matched.Groups[2].Value)
                                , int.Parse(matched.Groups[3].Value)
                                , int.Parse(matched.Groups[4].Value)
                            };
                        }
                        else
                        {
                            Console.Error.WriteLine($"ERROR: padding指定 '{opt.Padding}' が正しくありません。 '16,32,0,16' のようなフォーマットで指定してください。");
                            return;
                        }
                    }
                    else
                    {
                        paddings = new[] { 16, 32, 0, 32 };
                    }

                    var topPadding = paddings[0];
                    var rightPadding = paddings[1];
                    var bottomPadding = paddings[2];
                    var leftPadding = paddings[3];
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
