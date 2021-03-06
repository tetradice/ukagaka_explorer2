﻿using System;
using System.Linq;
using System.Windows.Forms;

namespace ShellExplorer2
{
    internal static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        private static void Main()
        {
            var args = Environment.GetCommandLineArgs().ToList();
            if (args.Count <= 1)
            {
                MessageBox.Show("シェルエクスプローラ通を、単体で起動することはできません。\nゴーストの右クリックメニュー > プラグインから呼び出してください。"
                                , "エラー"
                                , MessageBoxButtons.OK
                                , MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
