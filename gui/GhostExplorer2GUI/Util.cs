﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
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

        /// <summary>
        /// profileを読み込む
        /// </summary>
        public static Profile LoadProfile()
        {
            // JSONシリアライザーを生成
            var serializer = new DataContractJsonSerializer(typeof(Profile));

            // ファイルが存在するなら読み込む
            var profPath = GetProfilePath();
            if (File.Exists(profPath))
            {
                using (var input = new FileStream(profPath, FileMode.Open))
                {
                    return (Profile)serializer.ReadObject(input);
                }
            }

            // 存在しなければnull
            return null;
        }

        /// <summary>
        /// profileの保存
        /// </summary>
        public static void SaveProfile(Profile profile)
        {
            // JSONシリアライザーを生成
            var serializer = new DataContractJsonSerializer(typeof(Profile));

            // 書き込み
            using (var output = new FileStream(GetProfilePath(), FileMode.Create))
            {
                serializer.WriteObject(output, profile);
            }
        }
    }
}