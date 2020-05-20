using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShellExplorer2
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
