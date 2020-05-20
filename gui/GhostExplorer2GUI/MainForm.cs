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

namespace GhostExplorer2
{
    public partial class MainForm : Form
    {
        protected GhostManager GhostManager;
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
        /// 不在アイコンの画像キーリスト
        /// </summary>
        protected List<string> AbsenceImageKeys = new List<string>();

        /// <summary>
        /// ゴースト不在情報。ゴーストのフォルダパスをキーとし、不在時は値に不在画像のキーが入っている
        /// </summary>
        protected Dictionary<string, string> AbsenceInfo = new Dictionary<string, string>();

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
        /// リストで選択しているゴースト
        /// </summary>
        protected GhostWithPrimaryShell SelectedGhost
        {
            get
            {
                //項目が１つも選択されていない場合は、選択ゴーストなし
                if (lstGhost.SelectedItems.Count == 0)
                {
                    return null;
                }

                // 選択インデックスと対応するゴーストを取得
                var selectedIndex = lstGhost.SelectedItems[0].Index;
                return GhostManager.Ghosts[selectedIndex];
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

            // ゴースト不在判定 SSP側で立っているゴーストは画像表示無し
            {
                var rand = new Random();

                // SSP側で立っているゴーストの情報を取得 (sakuraname, keronameのTupleをキーとして、HashSetを生成)
                var standingGhosts = new HashSet<Tuple<string, string>>();
                foreach (var fmoGhost in FMOGhostList)
                {
                    standingGhosts.Add(Tuple.Create(fmoGhost.Name, fmoGhost.KeroName));
                }

                // ゴーストごとに処理
                foreach (var ghost in GhostManager.Ghosts)
                {
                    // 不在情報更新
                    var beforeAbsent = AbsenceInfo.ContainsKey(ghost.DirPath);
                    var currentAbsent = standingGhosts.Contains(Tuple.Create(ghost.SakuraName, ghost.KeroName));

                    // いる→いない に変わった場合、不在画像をランダムに選択して設定
                    if(!beforeAbsent && currentAbsent)
                    {
                        AbsenceInfo[ghost.DirPath] = AbsenceImageKeys[rand.Next(0, AbsenceImageKeys.Count)];
                    }
                    if (!currentAbsent){
                        // いる場合は不在情報削除
                        if (AbsenceInfo.ContainsKey(ghost.DirPath))
                        {
                            AbsenceInfo.Remove(ghost.DirPath);
                        }
                    }

                    // フォルダパスでリスト項目を探す
                    if (lstGhost.Items.ContainsKey(ghost.DirPath))
                    {
                        // 不在判定
                        if (AbsenceInfo.ContainsKey(ghost.DirPath))
                        {
                            // いない
                            lstGhost.Items[ghost.DirPath].ImageKey = AbsenceInfo[ghost.DirPath];
                        } else
                        {
                            // いる
                            lstGhost.Items[ghost.DirPath].ImageKey = ghost.DirPath;
                        }
                    }
                }
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

            // ゴースト切り替え/呼び出しボタン、および付随する設定チェックボックスは、ゴースト選択中のみ表示
            BtnCall.Visible = BtnChange.Visible = ChkCloseAfterChange.Visible = (this.SelectedGhost != null);

            // ゴースト切り替えボタンは、呼び出し元ゴーストが残っていないと押下できない
            BtnChange.Enabled = ChkCloseAfterChange.Enabled = !(CallerLost);

            // ランダム選択ボタンは、読み込みが完了していれば表示
            BtnRandomSelect.Visible = true;

            // ゴーストの立ち絵は、選択されているゴーストがいて、かつ不在でない場合のみ表示
            SelectedGhostSurfaceVisible = (this.SelectedGhost != null && !AbsenceInfo.ContainsKey(this.SelectedGhost.DirPath));

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
            if (this.SelectedGhost == null) return;

            // エラーメッセージリストを初期化
            SurfaceErrorMessages.Clear();

            // sakura側のサーフェス画像を取得
            try
            {
                CurrentSakuraSurface = GhostManager.DrawSakuraSurface(this.SelectedGhost);

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
                CurrentKeroSurface = GhostManager.DrawKeroSurface(this.SelectedGhost);
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
            if (SelectedGhost != null)
            {
                if (this.SurfaceErrorMessages.Any())
                {
                    return string.Join("\r\n", SurfaceErrorMessages.Select(m => "ERROR: " + m));
                }
                if (SelectedGhost.CharacterDescript != null)
                {
                    return SelectedGhost.CharacterDescript;
                }
            }

            return null;
        }

        /// <summary>
        /// ゴースト切り替えボタン押下
        /// </summary>
        private void BtnChange_Click(object sender, EventArgs e)
        {
            var success = SendSSTPScript(@"\![change,ghost," + this.SelectedGhost.Name + @"]\e");

            // 送信成功した場合、オプションに応じてアプリケーション終了
            if (success && ChkCloseAfterChange.Checked)
            {
                Application.Exit();
            }

            UpdateUIState();
        }

        /// <summary>
        /// 呼び出しボタン押下
        /// </summary>
        private void BtnCall_Click(object sender, EventArgs e)
        {
            // ゴースト呼び出しの前にFMO情報の更新を試みる
            UpdateFMOInfo();

            // 立っているゴーストが一人もおらず、かつSSP.exeのパスが特定できるなら、SSP.exeの起動を試みる
            if (!FMOGhostList.Any() && LastSSPExePath != null)
            {
                Process.Start(LastSSPExePath, string.Format(@"/g ""{0}""", this.SelectedGhost.Name));
                return;
            }

            SendSSTPScript(@"\![call,ghost," + this.SelectedGhost.Name + @"]\e");
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
            }
            else
            {
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
                    }
                    else
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

            }
            catch (SocketException ex)
            {
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
            var i = rand.Next(0, lstGhost.Items.Count);
            lstGhost.Items[i].Focused = true;
            lstGhost.Items[i].Selected = true;
            lstGhost.EnsureVisible(i);
        }

        /// <summary>
        /// ゴースト情報一括読み込み
        /// </summary>
        protected virtual void LoadGhosts()
        {
            var appDirPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            // 既存情報クリア
            lstGhost.Clear();
            imgListFace.Images.Clear();

            // FMO情報更新
            UpdateFMOInfo();

            // コマンドライン引数を解釈
            var args = Environment.GetCommandLineArgs().ToList();
            args.RemoveAt(0); // 先頭はexe名のため削除

            var caller = args.First(); // 呼び出し元ゴースト (id or "unspecified")
            args.RemoveAt(0); // 先頭削除
            var ghostDirPaths = args; // 残りの引数はすべてゴーストフォルダのパス

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

            // ボタンラベル変更
            BtnChange.Text = string.Format("{0}から切り替え", CallerSakuraName);

            // チェックボックスの位置移動
            ChkCloseAfterChange.Left = BtnChange.Left + 4;

            // グループ表示のON/OFF
            // フォルダが2つ以上あればグループ表示する
            lstGhost.ShowGroups = (ghostDirPaths.Count >= 2);

            // ゴースト情報読み込み
            GhostManager = GhostManager.Load(ghostDirPaths);

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
                Directory.Delete(GhostManager.CacheDirPath, recursive: true);
            }

            // 最終起動情報を書き込む
            if (!Directory.Exists(dataDirPath)) Directory.CreateDirectory(dataDirPath);
            File.WriteAllText(lastBootVersionPath, Const.Version);

            // ゴーストの顔画像を変換・取得
            var faceImages = GhostManager.GetFaceImages(imgListFace.ImageSize);

            // リストビュー構築処理
            var listGroups = new Dictionary<string, ListViewGroup>();
            foreach (var ghost in GhostManager.Ghosts)
            {
                // 顔画像を正常に読み込めていれば、イメージリストに追加
                if (faceImages.ContainsKey(ghost.DirPath))
                {
                    imgListFace.Images.Add(ghost.DirPath, faceImages[ghost.DirPath]);
                }

                // リスト項目追加
                var item = lstGhost.Items.Add(key: ghost.DirPath, text: ghost.Name ?? "", imageKey: ghost.DirPath);

                // ゴースト格納フォルダのパスを元に、グループも設定
                if (!listGroups.ContainsKey(ghost.GhostBaseDirPath))
                {
                    listGroups[ghost.GhostBaseDirPath] = new ListViewGroup(ghost.GhostBaseDirPath);
                    lstGhost.Groups.Add(listGroups[ghost.GhostBaseDirPath]);
                }
                item.Group = listGroups[ghost.GhostBaseDirPath];
            }

            // イメージリストに不在アイコンを追加
            AbsenceImageKeys.Clear();
            foreach (var path in Directory.GetFiles(Path.Combine(appDirPath, @"res\absence_icon"), "*.png"))
            {
                var fileName = Path.GetFileName(path);
                imgListFace.Images.Add(fileName, new Bitmap(path));
                AbsenceImageKeys.Add(fileName);
            }

            // 読み込み中表示を消す
            lblLoading.Hide();

            // ゴースト情報リストの更新に伴う画面表示更新
            UpdateUIOnFMOChanged();

            // ボタン等表示状態を更新
            UpdateUIState();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            Application.DoEvents(); // ロード表示を確実に表示させる
            LoadGhosts();
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
            //if (Directory.Exists(GhostManager.CacheDirPath)) Directory.Delete(GhostManager.CacheDirPath, recursive: true);

            // 再読み込み
            LoadGhosts();
        }

        private void BtnOpenShellFolder_Click(object sender, EventArgs e)
        {
            Process.Start(this.SelectedGhost.Shell.DirPath);
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
    }
}
