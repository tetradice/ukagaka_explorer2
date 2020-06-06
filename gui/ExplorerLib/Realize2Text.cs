using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExplorerLib
{
    /// <summary>
    /// realize2.txt を表すクラス (ゴーストの使用頻度情報を格納している)
    /// </summary>
    public class Realize2Text
    {
        public virtual string Path { get; set; }
        public virtual IList<Record> GhostRecords { get; set; }
        public virtual IList<Record> BalloonRecords { get; set; }

        public class Record
        {
            /// <summary>
            /// ゴースト名 or バルーン名
            /// </summary>
            public virtual string Name { get; set; }

            /// <summary>
            /// 累計起動時間 (分)
            /// </summary>
            public virtual long TotalBootByMinute { get; set; }

            /// <summary>
            /// 累計起動回数
            /// </summary>
            public virtual long TotalBootCount { get; set; }

            /// <summary>
            /// 最終起動時刻 (秒。起算時がいつかは不明)
            /// </summary>
            public virtual long LastBootSecond { get; set; }
        }

        /// <summary>
        /// 指定したパスの realize2.txt を読み込む
        /// </summary>
        public static Realize2Text Load(string path)
        {
            var realizeText = new Realize2Text() { Path = path };
            realizeText.Load();
            return realizeText;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Realize2Text()
        {
            GhostRecords = new List<Record>();
            BalloonRecords = new List<Record>();
        }

        /// <summary>
        /// realize2.txt を読み込む
        /// </summary>
        public virtual void Load()
        {
            GhostRecords.Clear();
            BalloonRecords.Clear();

            var entryPattern = new Regex(@"(ghost|balloon)\s*,\s*(.+?)\s*\z");

            // 読み込みメイン処理 (エンコーディングはUTF-8で決め打ち)
            var lines = File.ReadLines(Path, encoding: Encoding.UTF8);
            foreach (var line in lines)
            {
                var matched = entryPattern.Match(line);
                if (matched.Success)
                {
                    var type = matched.Groups[1].Value;
                    var value = matched.Groups[2].Value;


                    // 値を1バイト文字で区切る
                    var tokens = value.Split('\u0001');

                    // 値をセット
                    var record = new Record();
                    record.Name = tokens[0];
                    record.TotalBootByMinute = long.Parse(tokens[4]);
                    record.TotalBootCount = long.Parse(tokens[7]);
                    record.LastBootSecond = long.Parse(tokens[5]);

                    // レコード追加
                    if (type == "ghost")
                    {
                        GhostRecords.Add(record);
                    }
                    else if (type == "balloon")
                    {
                        BalloonRecords.Add(record);
                    }
                }
            }
        }
    }
}
