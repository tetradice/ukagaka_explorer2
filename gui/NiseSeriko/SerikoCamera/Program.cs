using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using CommandLine;
using NiseSeriko;

namespace SerikoCamera
{
    internal class Options
    {
        [Option("shell", HelpText = "使用するシェルのフォルダ名")]
        public string Shell { get; set; }

        //[Option('p', "char", HelpText = "出力するキャラ側", Default = new[] {0, 1}, Separator = ',')]
        //public IEnumerable<int> Characters { get; set; }

        [Option('o', "output", HelpText = "出力するフォルダパス。省略時はゴーストフォルダ以下のphoto")]
        public string OutputDirPath { get; set; }

        [Value(0, Required = true, HelpText = "ゴーストのフォルダパス")]
        public string GhostDirPath { get; set; }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opt =>
                {
                    Debug.WriteLine(opt);

                    var ghost = Ghost.Load(opt.GhostDirPath);
                    var shellDirPath = Path.Combine(ghost.DirPath, ghost.CurrentShellRelDirPath);

                    var outputDir = opt.OutputDirPath != null ? Path.GetFullPath(opt.OutputDirPath) : Path.Combine(ghost.DirPath, @"photo");
                    Directory.CreateDirectory(outputDir);

                    var shell = Shell.Load(shellDirPath, ghost.SakuraDefaultSurfaceId, ghost.KeroDefaultSurfaceId);
                    //var shell = Shell.Load(shellDirPath, ghost.SakuraDefaultSurfaceId, ghost.KeroDefaultSurfaceId, interimOutputDirPathForDebug: Path.Combine(outputDir, "interim")); var shell = Shell.Load(shellDirPath, ghost.SakuraDefaultSurfaceId, ghost.KeroDefaultSurfaceId, interimOutputDirPathForDebug: Path.Combine(outputDir, "interim"));

                    var sakuraBitmap = shell.DrawSurface(shell.SakuraSurfaceModel);
                    var sakuraOutputPath = Path.Combine(outputDir, @"p0.png");
                    sakuraBitmap.Save(sakuraOutputPath);
                    Console.WriteLine($"output -> {sakuraOutputPath}");
                    var keroBitmap = shell.DrawSurface(shell.KeroSurfaceModel);
                    var keroOutputPath = Path.Combine(outputDir, @"p1.png");
                    keroBitmap.Save(keroOutputPath);
                    Console.WriteLine($"output -> {keroOutputPath}");
                })
                .WithNotParsed(err =>
                {
                    Debug.WriteLine(err);
                });
        }
    }
}
