using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CommandLine;
using CommandLine.Text;
using NiseSeriko;

namespace SerikoCamera
{
    internal class Options
    {
        ////[Option("shell", HelpText = "使用するシェルのディレクトリ名")]
        ////public string Shell { get; set; }

        [Option('o', "output", HelpText = "出力するディレクトリパス。省略時は (ゴーストディレクトリ)/photo")]
        public string OutputDirPath { get; set; }

        [Option("subdir", HelpText = "指定したディレクトリの1階層下ゴーストをすべて変換対象とする")]
        public bool SubDir { get; set; }

        [Value(0, Required = true, HelpText = "ゴーストのディレクトリパス\n(--subdir オプションを指定した場合は、ゴーストのディレクトリ\n複数を含むディレクトリのパス)")]
        public string GhostDirPath { get; set; }

        [Usage()]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new[] {
                    new Example("通常の変換", new Options() {GhostDirPath = "./ghost/sakura"}),
                    new Example("ghostフォルダ以下をまとめて変換", new Options() {GhostDirPath = "./ghost", SubDir = true}),
                    new Example("出力先指定", new Options() {GhostDirPath = "./ghost/sakura", OutputDirPath = "./out"}),
               };
            }
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opt =>
                {
                    if (opt.SubDir)
                    {
                        foreach (var subDirPath in Directory.GetDirectories(opt.GhostDirPath))
                        {
                            if (NiseSeriko.Ghost.IsGhostDir(subDirPath))
                            {
                                var outputDirPath = (opt.OutputDirPath != null ? Path.Combine(opt.OutputDirPath, Path.GetFileName(subDirPath)) : null);
                                Convert(subDirPath, outputDirPath);
                            }
                        }
                    }
                    else
                    {
                        Convert(opt.GhostDirPath, opt.OutputDirPath);
                    }


                })
                .WithNotParsed(err =>
                {
                    Debug.WriteLine(err);
                });
        }

        protected static void Convert(string ghostDirPath, string outputDirPath)
        {
            var ghost = Ghost.Load(ghostDirPath);
            var shellDirPath = Path.Combine(ghost.DirPath, ghost.CurrentShellRelDirPath);

            var outputDir = outputDirPath != null ? Path.GetFullPath(outputDirPath) : Path.Combine(ghost.DirPath, @"photo");
            Directory.CreateDirectory(outputDir);

            //var shell = Shell.Load(shellDirPath, ghost.SakuraDefaultSurfaceId, ghost.KeroDefaultSurfaceId);
            var shell = Shell.Load(shellDirPath, ghost.SakuraDefaultSurfaceId, ghost.KeroDefaultSurfaceId, interimOutputDirPathForDebug: Path.Combine(outputDir, "interim"));

            var sakuraBitmap = shell.DrawSurface(shell.SakuraSurfaceModel);
            var sakuraOutputPath = Path.Combine(outputDir, @"p0.png");
            sakuraBitmap.Write(sakuraOutputPath);
            Console.WriteLine($"output -> {sakuraOutputPath}");

            var keroBitmap = shell.DrawSurface(shell.KeroSurfaceModel);
            var keroOutputPath = Path.Combine(outputDir, @"p1.png");
            keroBitmap.Write(keroOutputPath);
            Console.WriteLine($"output -> {keroOutputPath}");
        }
    }
}
