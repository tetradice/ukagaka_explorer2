using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace ExplorerLib
{
    public class SSTPClient
    {
        public const string HOST = "127.0.0.1";
        public const int PORT = 9801;

        public abstract class Request
        {
            public static string BuildMessage(string command, IDictionary<string, string> headers)
            {
                var msg = new StringBuilder();
                msg.AppendLine(command);
                foreach (var pair in headers)
                {
                    msg.AppendLine(string.Format("{0}: {1}", pair.Key, pair.Value));
                }
                msg.AppendLine(); // 最後の空行

                return msg.ToString();

            }
        }

        public class Send14Request : Request
        {
            public const string COMMAND_STRING = "SEND SSTP/1.4";
            public virtual string Sender { get; set; }
            public virtual string Id { get; set; }
            public virtual string Script { get; set; }
            public virtual Tuple<string, string> IfGhost { get; set; }

            public override string ToString()
            {
                var headers = new Dictionary<string, string>
                {
                    ["Charset"] = "UTF-8",
                    ["Sender"] = Sender
                };
                if (Id != null)
                {
                    headers["ID"] = Id;
                }

                headers["Sender"] = Sender;
                if (IfGhost != null)
                {
                    headers["IfGhost"] = string.Format("{0},{1}", IfGhost.Item1, IfGhost.Item2);
                }
                headers["Script"] = Script;

                return Request.BuildMessage(COMMAND_STRING, headers);
            }
        }

        public class Execute13Request : Request
        {
            public const string COMMAND_STRING = "EXECUTE SSTP/1.3";
            public virtual string Sender { get; set; }
            public virtual string Command { get; set; }

            public override string ToString()
            {
                var headers = new Dictionary<string, string>
                {
                    ["Charset"] = "UTF-8",
                    ["Sender"] = Sender,
                    ["Command"] = Command
                };

                return Request.BuildMessage(COMMAND_STRING, headers);
            }
        }

        public class Notify11Request : Request
        {
            public const string COMMAND_STRING = "NOTIFY SSTP/1.1";
            public virtual string Sender { get; set; }
            public virtual string Event { get; set; }
            public virtual string Id { get; set; }
            public virtual string Script { get; set; }
            public virtual Tuple<string, string> IfGhost { get; set; }
            public virtual List<string> References { get; set; }

            public Notify11Request()
            {
                References = new List<string>();
            }

            public override string ToString()
            {
                var headers = new Dictionary<string, string>
                {
                    ["Charset"] = "UTF-8",
                    ["Sender"] = Sender,
                    ["Event"] = Event
                };
                if (Id != null)
                {
                    headers["ID"] = Id;
                }

                headers["Sender"] = Sender;
                if (IfGhost != null)
                {
                    headers["IfGhost"] = string.Format("{0},{1}", IfGhost.Item1, IfGhost.Item2);
                }
                if (Script != null)
                {
                    headers["Script"] = Script;
                }

                for (var i = 0; i < References.Count; i++)
                {
                    headers["Reference" + i.ToString()] = References[i];
                }

                return Request.BuildMessage(COMMAND_STRING, headers);
            }
        }


        public class Response
        {
            /// <summary>
            /// ステータスコード
            /// </summary>
            public virtual int StatusCode { get; set; }

            /// <summary>
            /// 説明句
            /// </summary>
            public virtual string StatusExplanation { get; set; }

            /// <summary>
            /// 成功レスポンスか
            /// </summary>
            public virtual bool Success { get { return StatusCode >= 200 && StatusCode <= 299; } }

            /// <summary>
            /// 付加情報 (EXECUTEなどのときに取得可能)
            /// </summary>
            public virtual string AdditionalValue { get; set; }

            /// <summary>
            /// ステータス行パターン
            /// </summary>
            public static Regex ResponseLinePattern = new Regex(@"\ASSTP/[0-9\.]+ ([0-9]+)\s*(.*)");

            /// <summary>
            /// レスポンスメッセージを解析する
            /// </summary>
            /// <returns>成功した場合はResponseオブジェクト、失敗した場合はnull</returns>
            public static Response Parse(string responseText)
            {
                var lines = responseText.Replace("\r\n", "\n").Split('\n');

                // まずは1行目を解析
                var matched = ResponseLinePattern.Match(lines[0]);
                if (matched.Success)
                {
                    var res = new Response
                    {
                        StatusCode = int.Parse(matched.Groups[1].Value),
                        StatusExplanation = matched.Groups[2].Value.TrimEnd()
                    };

                    // 2行目以降があれば続けて解析
                    for (var i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();

                        // 空白でなければ結果を取得
                        if (!string.IsNullOrEmpty(line))
                        {
                            res.AdditionalValue = line;
                        }
                    }


                    return res;
                }

                return null;
            }

        }

        /// <summary>
        /// SSTPリクエストを送信し、レスポンスを得る
        /// </summary>
        /// <param name="req">リクエストオブジェクト</param>
        /// <remarks>
        /// khskさんの作成されたソースを参考にしています。 https://qiita.com/khsk/items/177741a6c573790a9379
        /// </remarks>
        /// <returns>成功した場合はResponseオブジェクト、失敗した場合はnull</returns>
        public virtual Response SendRequest(Request req)
        {
            Debug.WriteLine("[SSTP Request]");
            Debug.WriteLine(req.ToString());

            var data = Encoding.UTF8.GetBytes(req.ToString());

            using (var client = new TcpClient(HOST, PORT))
            {
                using (var ns = client.GetStream())
                {
                    ns.ReadTimeout = 10 * 1000; // 読み込みタイムアウト10秒
                    ns.WriteTimeout = 10 * 1000; // 書き込みタイムアウト10秒

                    // リクエストを送信する
                    ns.Write(data, 0, data.Length); // リクエスト

                    //サーバーから送られたデータを受信する
                    using (var ms = new System.IO.MemoryStream())
                    {
                        var resBytes = new byte[256];
                        var resSize = 0;
                        do
                        {
                            //データの一部を受信する
                            resSize = ns.Read(resBytes, 0, resBytes.Length);

                            //Readが0を返した時はサーバーが切断したと判断
                            if (resSize == 0)
                            {
                                Console.WriteLine("サーバーが切断しました。");
                                break;
                            }

                            //受信したデータを蓄積する
                            ms.Write(resBytes, 0, resSize);

                            //まだ読み取れるデータがあるか、データの最後が\nでない時は、受信を続ける
                        } while (ns.DataAvailable || resBytes[resSize - 1] != '\n');

                        // 受信したデータを文字列に変換
                        var resMsg = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                        // 末尾の\0, \nを削除
                        resMsg = resMsg.TrimEnd('\0').TrimEnd();

                        Debug.WriteLine(resMsg);

                        return Response.Parse(resMsg);
                    }
                }
            }
        }
    }
}
