using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
                foreach(var pair in headers)
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
                var headers = new Dictionary<string, string>();
                headers["Charset"] = "UTF-8";
                headers["Sender"] = Sender;
                if(Id != null) headers["ID"] = Id;
                headers["Sender"] = Sender;
                if (IfGhost != null)
                {
                    headers["IfGhost"] = string.Format("{0},{1}", IfGhost.Item1, IfGhost.Item2);
                }
                headers["Script"] = Script;

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
            public virtual bool Success { get { return StatusCode >= 200 && StatusCode <= 299; } }

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
                var matched = ResponseLinePattern.Match(responseText);
                if (matched.Success)
                {
                    var res = new Response();
                    res.StatusCode = int.Parse(matched.Groups[1].Value);
                    res.StatusExplanation = matched.Groups[2].Value.TrimEnd();

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
            var bytesReceived = new Byte[256];

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
                        // 末尾の\nを削除
                        resMsg = resMsg.TrimEnd('\n');

                        Debug.WriteLine(resMsg);

                        return Response.Parse(resMsg);
                    }
                }
            }
        }
    }
}
