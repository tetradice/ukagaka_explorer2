using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GhostExplorer2
{
    public class Win32API
    {
        /// <summary>
        /// 指定したウインドルハンドルのWindowが存在するかどうかをチェック
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);
    }
}
