using System;
using System.Runtime.InteropServices;

namespace GhostExplorer2
{
    public class Win32API
    {
        /// <summary>
        /// Windowメッセージの登録
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int RegisterWindowMessage(string lpString);

        /// <summary>
        /// ウインドウを作成したスレッドID、プロセスIDを得る
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    }
}
