using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostExplorer2
{
    public static class Util
    {
        /// <summary>
        /// exeファイルがあるフォルダのパスを取得
        /// </summary>
        public static string GetAppDirPath()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        /// <summary>
        /// dataフォルダのパスを取得
        /// </summary>
        public static string GetDataDirPath()
        {
            return Path.Combine(GetAppDirPath(), "data");
        }

        /// <summary>
        /// cacheフォルダのパスを取得
        /// </summary>
        public static string GetCacheDirPath()
        {
            return Path.Combine(GetDataDirPath(), "cache");
        }

        /// <summary>
        /// profile.json のパスを取得
        /// </summary>
        public static string GetProfilePath()
        {
            return Path.Combine(GetDataDirPath(), "profile.json");
        }
    }
}
