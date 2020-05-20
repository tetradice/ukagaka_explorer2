using ExplorerLib;
using ExplorerLib.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShellExplorer2
{
    public partial class MainForm : Form
    {
        protected ShellManager ShellManager;
        protected Bitmap CurrentSakuraSurface;
        protected Bitmap CurrentKeroSurface;
        protected List<string> SurfaceErrorMessages;
        protected string DescriptionText;
        private Font DescriptionFont;
        private RectangleF DescriptionRect;

        // 呼び出し元ゴースト情報
        protected string CallerId;
        protected string CallerSakuraName;
        protected string CallerKeroName;
        protected IntPtr CallerHWnd;
        protected bool CallerLost;

        /// <summary>
        /// 最後に確認できたSSP.exeのパス
        /// </summary>
        protected string LastSSPExePath;

        /// <summary>
        /// Window Message "Sakura" (RegisterWindowMessageで取得)
        /// </summary>
        protected int WMSakuraAPI;

        /// <summary>
        /// Sakura APIコマンド - ゴースト変更通知
        /// </summary>
        /// <remarks>
        /// http://emily.shillest.net/specwiki/?SSP/仕様書/FMO
        /// </remarks>
        public const int SAKURA_API_BROADCAST_GHOSTCHANGE = 1024;

        /// <summary>
        /// FMOから取得したゴースト情報一覧。起動時とSSP側でのゴースト変更時に更新される
        /// </summary>
        protected List<SakuraFMOData> FMOGhostList = new List<SakuraFMOData>();

        /// <summary>
        /// 選択中のゴーストを表示するかどうか
        /// </summary>
        protected bool SelectedGhostSurfaceVisible;

        /// <summary>
        /// リストで選択しているシェル
        /// </summary>
        protected Shell SelectedShell
        {
            get
            {
                //項目が１つも選択されていない場合は、選択シェルなし
                if (lstShell.SelectedItems.Count == 0)
                {
                    return null;
                }

                // 選択インデックスと対応するシェルを取得
                var selectedIndex = lstShell.SelectedItems[0].Index;
                return ShellManager.Shells[selectedIndex];
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            this.SurfaceErrorMessages = new List<string>();
        }

        /// <summary>
        /// 読み込み時処理
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // テキスト関連の情報はロード時に設定
            DescriptionFont = new Font("ＭＳ ゴシック", 9);
            var descSize = picSurface.CreateGraphics().MeasureString("＠＠＠＠＠＠＠＠＠＠＠＠＠＠＠＠＠＠＠＠", DescriptionFont);
            DescriptionRect = new RectangleF(16, 16, descSize.Width, 800);

            // FMOの変更通知を受け取るために、WindowMessage "Sakura" を登録
            // http://emily.shillest.net/specwiki/?SSP/仕様書/FMO
            WMSakuraAPI = Win32API.RegisterWindowMessage("Sakura");
        }

        /// <summary>
        /// FMOからゴースト情報を取得し、ゴースト情報リストとSSPパスを更新、同時に画面に対して必要な更新を行う
        /// </summary>
        protected void UpdateFMOInfoAndUpdateUI()
        {
            UpdateFMOInfo();
            UpdateUIOnFMOChanged();
        }

        /// <summary>
        /// FMOからゴースト情報を取得し、ゴースト情報リストとSSPパスを更新
        /// </summary>
        protected void UpdateFMOInfo()
        {
            var fmo = new SakuraFMO();
            fmo.Update(false);
            FMOGhostList = fmo.GetGhosts();

            // ゴーストが1体以上いるなら、合わせてSSPのパスを取得
            if (FMOGhostList.Any())
            {
                LastSSPExePath = null;
                try
                {
                    // HWndからプロセスIDを取得
                    int procId;
                    Win32API.GetWindowThreadProcessId((IntPtr)FMOGhostList.First().HWnd, out procId);
                    var sspProc = Process.GetProcessById(procId);
                    LastSSPExePath = sspProc.MainModule.FileName;
                }
                catch (Exception ex)
                {
                    Debug.Write(ex.ToString());
                }
            }
        }

        /// <summary>
        /// FMO情報変更時の画面反映処理を行う (呼び出し元ゴーストがいなくなっている判定、UI状態の更新)
        /// </summary>
        protected void UpdateUIOnFMOChanged()
        {
            // 呼び出し元のゴーストがいなくなっている判定
            if (!CallerLost && !FMOGhostList.Any(g => g.Id == CallerId))
            {
                CallerLost = true;
            }

            // UI状態も更新
            UpdateUIState();
        }

        /// <summary>
        /// UIの表示状態更新
        /// </summary>
        protected void UpdateUIState()
        {
            // 説明文設定
            this.DescriptionText = GetDescriptionText();

            // シェル切り替えボタン、および付随する設定チェックボックスは、シェル選択中のみ表示
            BtnChange.Visible = ChkCloseAfterChange.Visible = (this.SelectedShell != null);

            // シェル切り替えボタンは、呼び出し元シェルが残っていないと押下できない
            BtnChange.Enabled = ChkCloseAfterChange.Enabled = !(CallerLost);

            // ランダム選択ボタンは、読み込みが完了していれば表示
            BtnRandomSelect.Visible = true;

            // ゴーストの立ち絵は、シェルを選択している場合のみ表示
            SelectedGhostSurfaceVisible = (this.SelectedShell != null);

            // 立ち絵Paint再発生
            picSurface.Invalidate();

            // デバッグ用
#if DEBUG
            BtnReload.Visible = true;
            BtnOpenShellFolder.Visible = true;
#endif
        }

        /// <summary>
        /// ゴースト一覧の選択切り替え時処理
        /// </summary>
        private void lstGhost_SelectedIndexChanged(object sender, EventArgs e)
        {
            // ゴースト未選択時はスキップ
            if (this.SelectedShell == null) return;

            // エラーメッセージリストを初期化
            SurfaceErrorMessages.Clear();

            // sakura側のサーフェス画像を取得
            try
            {
                CurrentSakuraSurface = ShellManager.DrawSakuraSurface(this.SelectedShell);

                // サーフェスが何らかの原因で見つからなかった場合はエラー扱い
                if (CurrentSakuraSurface == null)
                {
                    SurfaceErrorMessages.Add(@"本体側の立ち絵描画に失敗しました。");
                }
            }
            catch (IllegalImageFormatException ex)
            {
                CurrentSakuraSurface = null;
                Debug.WriteLine(ex.ToString());
                SurfaceErrorMessages.Add("(本体側)" + ex.Message);
            }
            catch (Exception ex)
            {
                CurrentSakuraSurface = null;
                Debug.WriteLine(ex.ToString());
                SurfaceErrorMessages.Add(@"本体側の立ち絵描画に失敗しました。");
            }

            // kero側のサーフェス画像を取得
            try
            {
                CurrentKeroSurface = ShellManager.DrawKeroSurface(this.SelectedShell);
            }
            catch (IllegalImageFormatException ex)
            {
                CurrentKeroSurface = null;
                Debug.WriteLine(ex.ToString());
                SurfaceErrorMessages.Add("(パートナー側)" + ex.Message);
            }
            catch (Exception ex)
            {
                CurrentKeroSurface = null;
                Debug.WriteLine(ex.ToString());
                SurfaceErrorMessages.Add(@"パートナー側の立ち絵描画に失敗しました。");
            }

            // 表示状態切り替え
            UpdateUIState();

            // サーフェスウインドウ再描画
            picSurface.Refresh();
        }

        /// <summary>
        /// リサイズ時処理
        /// </summary>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            // リサイズ時にはサーフェスウインドウを必ず再描画
            picSurface.Invalidate();
        }

        /// <summary>
        /// 描画処理
        /// </summary>
        private void picSurface_Paint(object sender, PaintEventArgs e)
        {
            // sakura側のサーフェスが読み込めており、表示フラグONの場合のみ描画
            if (CurrentSakuraSurface != null && SelectedGhostSurfaceVisible)
            {
                var sakuraRightPadding = 32;
                var keroRightPadding = 32;
                var topPadding = 100;

                // 縮小率を決定
                // sakura側とkero側が両方とも収まるように縮小率を決める
                var requiredWidth = (CurrentKeroSurface != null ? CurrentKeroSurface.Width + keroRightPadding : 0) + CurrentSakuraSurface.Width + sakuraRightPadding;
                var requiredHeight = Math.Max((CurrentKeroSurface != null ? CurrentKeroSurface.Height : 0), CurrentSakuraSurface.Height) + topPadding;
                var scaleRateByWidth = (double)e.ClipRectangle.Width / (double)requiredWidth;
                var scaleRateByHeight = (double)e.ClipRectangle.Height / (double)requiredHeight;
                var scaleRate = Math.Min(Math.Min(1.0, scaleRateByWidth), scaleRateByHeight); // 1.0より大きくならないようにする (拡大しない) 

                // 描画サイズの決定
                var sakuraW = (int)Math.Round(CurrentSakuraSurface.Width * scaleRate);
                var sakuraH = (int)Math.Round(CurrentSakuraSurface.Height * scaleRate);

                var left = e.ClipRectangle.Width - sakuraW - (int)Math.Round(sakuraRightPadding * scaleRate);
                var top = e.ClipRectangle.Height - sakuraH;
                e.Graphics.DrawImage(CurrentSakuraSurface, left, top, sakuraW, sakuraH);

                if (CurrentKeroSurface != null)
                {
                    var keroW = (int)Math.Round(CurrentKeroSurface.Width * scaleRate);
                    var keroH = (int)Math.Round(CurrentKeroSurface.Height * scaleRate);

                    var keroLeft = left - (int)Math.Round(keroRightPadding * scaleRate) - keroW;
                    var keroTop = e.ClipRectangle.Height - keroH;
                    e.Graphics.DrawImage(CurrentKeroSurface, keroLeft, keroTop, keroW, keroH);
                }
            }

            // 説明文の描画
            if (DescriptionText != null)
            {
                e.Graphics.DrawString(DescriptionText, DescriptionFont, Brushes.Black, DescriptionRect);
            }

        }

        /// <summary>
        /// 説明文取得
        /// </summary>
        protected virtual string GetDescriptionText()
        {
            if (SelectedShell != null)
            {
                if (this.SurfaceErrorMessages.Any())
                {
                    return string.Join("\r\n", SurfaceErrorMessages.Select(m => "ERROR: " + m));
                }
                if (SelectedShell.CharacterDescript != null)
                {
                    return SelectedShell.CharacterDescript;
                }
            }

            return null;
        }

        /// <summary>
        /// シェル切り替えボタン押下
        /// </summary>
        private void BtnChange_Click(object sender, EventArgs e)
        {
            var success = SendSSTPScript(@"\![change,shell," + this.SelectedShell.Name + @"]\e");

            // 送信成功した場合、オプションに応じてアプリケーション終了
            if (success && ChkCloseAfterChange.Checked)
            {
                Application.Exit();
            }

            UpdateUIState();
        }

        /// <summary>
        /// SSTP送信
        /// </summary>
        protected bool SendSSTPScript(string script)
        {
            var sstpClient = new SSTPClient();
            var req = new SSTPClient.Send14Request();
            req.Id = CallerId;
            req.Sender = Const.SSTPSender;
            if (CallerLost)
            {
                // 呼び出し元ゴーストがいなくなっている場合、適当なゴースト1体に対して送信
                if (FMOGhostList.Count >= 1)
                {
                    var rand = new Random();
                    var target = FMOGhostList[rand.Next(0, FMOGhostList.Count - 1)];
                    req.IfGhost = Tuple.Create(target.Name, target.KeroName);
                    req.Id = target.Id;
                }
                else
                {
                    MessageBox.Show(this,
                        string.Format("通信先のゴーストが見つかりませんでした。\r\nSSPが終了しているか、ほかの原因によって操作がブロックされている可能性があります。", CallerSakuraName),
                        "エラー",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }
            } else { 
                req.IfGhost = Tuple.Create(CallerSakuraName, CallerKeroName);
            }
            req.Script = script;

            try
            {
                var res = sstpClient.SendRequest(req);

                // エラーレスポンス時
                if (!res.Success)
                {
                    if (res.StatusCode == 404)
                    {
                        MessageBox.Show(this,
                                        string.Format("通信先のゴースト ({0}) が見つかりませんでした。\r\nそのゴーストがいなくなっているか、ほかの原因によって操作がブロックされている可能性があります。", CallerSakuraName),
                                        "エラー",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);

                        UpdateFMOInfoAndUpdateUI();
                        return false;
                    } else
                    {
                        MessageBox.Show(this,
                                        string.Format("SSPとの通信に失敗しました。\r\n({0} {1})", res.StatusCode, res.StatusExplanation),
                                        "エラー",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);
                        return false;
                    }
                }

                // 正常終了
                return true;

            } catch(SocketException ex) {
                Debug.WriteLine(ex.ToString());

                // ソケット例外発生時
                MessageBox.Show(this,
                                "SSPとの通信を行えませんでした。\r\nSSPが終了しているか、ほかの原因によって操作がブロックされている可能性があります。",
                                "エラー",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return false;
            }
        }

        /// <summary>
        /// ランダム選択ボタン押下
        /// </summary>
        private void BtnRandomSelect_Click(object sender, EventArgs e)
        {
            var rand = new Random();
            var i = rand.Next(0, lstShell.Items.Count);
            lstShell.Items[i].Focused = true;
            lstShell.Items[i].Selected = true;
            lstShell.EnsureVisible(i);
        }

        /// <summary>
        /// シェル情報一括読み込み
        /// </summary>
        protected virtual void LoadShells()
        {
            var appDirPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            // 既存情報クリア
            lstShell.Clear();
            imgListFace.Images.Clear();

            // FMO情報更新
            UpdateFMOInfo();

            // コマンドライン引数を解釈
            var args = Environment.GetCommandLineArgs().ToList();
            args.RemoveAt(0); // 先頭はexe名のため削除

            var caller = args.First(); // 呼び出し元ゴースト (id or "unspecified")
            args.RemoveAt(0); // 先頭削除
            var ghostDirPath = args.First().TrimEnd('\\'); // ゴーストフォルダパス

            SakuraFMOData target = null;
            var matched = Regex.Match(caller, @"^id:(.+?)\z");
            if (matched.Success)
            {
                // idが指定された場合、そのidと一致するゴースト情報を探す
                var id = matched.Groups[1].Value.Trim();
                target = FMOGhostList.FirstOrDefault(g => g.Id == id);
            }

            if (caller == "unspecified")
            {
                // "unspecified" が指定された場合、現在起動しているゴーストのうち、ランダムに1体を呼び出し元とする (デバッグ用)
                var rand = new Random();
                target = FMOGhostList[rand.Next(0, FMOGhostList.Count - 1)];
            }

            if (target == null)
            {
                throw new Exception(string.Format("指定されたゴーストが見つかりませんでした。 ({0})", caller));
            }

            // 呼び出し元の情報をプロパティにセット
            CallerId = target.Id;
            CallerSakuraName = target.Name;
            CallerKeroName = target.KeroName;
            CallerHWnd = (IntPtr)target.HWnd;

            // チェックボックスの位置移動
            ChkCloseAfterChange.Left = BtnChange.Left + 1;

            // ゴースト情報読み込み
            var ghost = Ghost.Load(ghostDirPath);

            // シェル情報読み込み
            ShellManager = ShellManager.Load(ghostDirPath, ghost.SakuraDefaultSurfaceId, ghost.KeroDefaultSurfaceId);

            // 最終起動時の情報を読み込む
            var dataDirPath = Path.Combine(appDirPath, "data");
            string lastBootVersion = null;
            var lastBootVersionPath = Path.Combine(dataDirPath, @"lastBootVersion.txt");
            if (File.Exists(lastBootVersionPath))
            {
                lastBootVersion = File.ReadAllText(lastBootVersionPath).Trim();
            }

            // 最終起動時の記録があり、かつ最終起動時とバージョンが異なる場合は、キャッシュをすべて破棄
            if(lastBootVersion != null && Const.Version != lastBootVersion)
            {
                Directory.Delete(ShellManager.CacheDirPath, recursive: true);
            }

            // 最終起動情報を書き込む
            if (!Directory.Exists(dataDirPath)) Directory.CreateDirectory(dataDirPath);
            File.WriteAllText(lastBootVersionPath, Const.Version);

            // ゴーストの顔画像を変換・取得
            var faceImages = ShellManager.GetFaceImages(imgListFace.ImageSize);

            // リストビュー構築処理
            foreach (var shell in ShellManager.Shells)
            {
                // 顔画像を正常に読み込めていれば、イメージリストに追加
                if (faceImages.ContainsKey(shell.DirPath))
                {
                    imgListFace.Images.Add(shell.DirPath, faceImages[shell.DirPath]);
                }

                // リスト項目追加
                var item = lstShell.Items.Add(key: shell.DirPath, text: shell.Name ?? "", imageKey: shell.DirPath);
                item.Tag = shell.DirPath;
            }

            // 読み込み中表示を消す
            lblLoading.Hide();

            // ゴースト情報リストの更新に伴う画面表示更新
            UpdateUIOnFMOChanged();

            // ボタン等表示状態を更新
            UpdateUIState();

            // 現在の使用シェルを選択
            var currentShellFolderName = Path.GetFileName(ghost.CurrentShellRelDirPath);
            foreach(ListViewItem item in lstShell.Items)
            {
                var shellFolderName = Path.GetFileName((string)item.Tag);
                if (shellFolderName == currentShellFolderName)
                {
                    item.Focused = true;
                    item.Selected = true;
                    break;
                }
            }

            // 選択対象のシェルを判別できなかった場合は、1件目を選択
            if (this.SelectedShell == null) {
                lstShell.Items[0].Focused = true;
                lstShell.Items[0].Selected = true;
            }

            // スクロール
            var selectedIndex = lstShell.SelectedItems[0].Index;
            lstShell.EnsureVisible(selectedIndex);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            Application.DoEvents(); // ロード表示を確実に表示させる
            LoadShells();
        }

        private void BtnReload_Click(object sender, EventArgs e)
        {
            //// 顔画像をすべて解放
            //foreach(Image img in imgListFace.Images)
            //{
            //    img.Dispose();
            //}
            //imgListFace.Images.Clear();
            //lstGhost.Clear();

            //// キャッシュフォルダを削除
            //if (Directory.Exists(ShellManager.CacheDirPath)) Directory.Delete(ShellManager.CacheDirPath, recursive: true);

            // 再読み込み
            LoadShells();
        }

        private void BtnOpenShellFolder_Click(object sender, EventArgs e)
        {
            Process.Start(this.SelectedShell.DirPath);
        }

        /// <summary>
        /// ウインドウメッセージ処理
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if(m.Msg == WMSakuraAPI)
            {
                // ゴースト変更通知なら処理
                if((int)m.WParam == SAKURA_API_BROADCAST_GHOSTCHANGE)
                {
                    // プロセスIDからSSPのパスを取得
                    LastSSPExePath = null;
                    try
                    {
                        var sspProc = Process.GetProcessById((int)m.LParam);
                        LastSSPExePath = sspProc.MainModule.FileName;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }

                    // ゴースト情報リストを更新し、画面表示に反映
                    UpdateFMOInfoAndUpdateUI();
                    return;
                }
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// シェルリストのダブルクリック
        /// </summary>
        private void lstShell_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // 変更ボタン押下処理
            BtnChange.PerformClick();
        }
    }
}
