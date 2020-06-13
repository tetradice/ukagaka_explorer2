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
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExplorerLib;
using ExplorerLib.Exceptions;

namespace GhostExplorer2
{
    public partial class MainForm : Form
    {
        public class DropDownItem
        {
            public string Label { get; set; }
            public string Value { get; set; }
        }

        protected GhostManager GhostManager;
        protected Dictionary<string, Shell> Shells = new Dictionary<string, Shell>();
        protected Dictionary<string, List<string>> ErrorMessagesOnShellLoading = new Dictionary<string, List<string>>();
        protected Dictionary<string, Bitmap> FaceImages = new Dictionary<string, Bitmap>();
        protected Bitmap CurrentSakuraSurface;
        protected Bitmap CurrentKeroSurface;
        protected List<string> SurfaceNotificationMessages;
        protected string DescriptionText;
        private Font DescriptionFont;
        private RectangleF DescriptionRect;
        protected string OldFilterText = "";

        // 呼び出し元ゴースト情報
        protected string CallerId;
        protected string CallerSakuraName;
        protected string CallerKeroName;
        protected IntPtr CallerHWnd;
        protected bool CallerLost;

        /// <summary>
        /// SSPのフォルダパス (FMO情報から取得する)
        /// </summary>
        protected string SSPDirPath;

        /// <summary>
        /// realize2.txt の情報
        /// </summary>
        protected Realize2Text Realize2Text;

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
        protected Ghost SelectedGhost
        {
            get
            {
                //項目が１つも選択されていない場合は、選択ゴーストなし
                if (lstGhost.SelectedItems.Count == 0)
                {
                    return null;
                }

                var selectedIndex = lstGhost.SelectedItems[0].Index;

                // オプションの場合はスキップ
                if (OptionSelected) return null;

                // 選択インデックスと対応するゴーストを取得
                return GhostManager.Ghosts[selectedIndex];
            }
        }

        /// <summary>
        /// オプションを選択しているかどうか
        /// </summary>
        protected bool OptionSelected
        {
            get
            {
                if (lstGhost.SelectedItems.Count == 0) return false;
                var selectedIndex = lstGhost.SelectedItems[0].Index;
                return (selectedIndex > GhostManager.Ghosts.Count - 1);
            }
        }

        /// <summary>
        /// Windowsスタートメニューに登録する際のショートカットパス
        /// </summary>
        protected string StartMenuShortcutPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), @"ゴーストエクスプローラ通\ゴーストエクスプローラ通を単体起動.lnk");
            }
        }

        /// <summary>
        /// Windowsスタートメニュー登録済みフラグ (読み込み時、オプションダイアログ表示時などに更新)
        /// </summary>
        protected bool StartMenuShortcutAdded = false;

        /// <summary>
        /// シェル・顔画像読み込みのために実行している非同期タスク
        /// </summary>
        protected Task GhostImageLoadingTask = null;

        /// <summary>
        /// シェル・顔画像読み込みを中断するためのトークンソース
        /// </summary>
        protected CancellationTokenSource GhostImageCancellationTokenSource;

        /// <summary>
        /// プロファイル情報 (設定など)
        /// </summary>
        protected Profile CurrentProfile;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            SurfaceNotificationMessages = new List<string>();
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

            // ソートドロップダウン初期設定
            cmbSort.Items.Add(new DropDownItem() { Value = Const.SortType.ByName, Label = "名前順" });
            cmbSort.Items.Add(new DropDownItem() { Value = Const.SortType.ByRecentBoot, Label = "最近起動した順" });
            cmbSort.Items.Add(new DropDownItem() { Value = Const.SortType.ByRecentInstall, Label = "最近インストールした順" });
            cmbSort.Items.Add(new DropDownItem() { Value = Const.SortType.ByBootTime, Label = "累計起動した順" });

            // バージョン表記設定
            var asm = Assembly.GetExecutingAssembly();
            var ver = asm.GetName().Version;
            lblVersion.Text = $"version: {ver.Major}.{ver.Minor}.{ver.Revision}";

            // Profile読み込み
            CurrentProfile = Util.LoadProfile();
            if (CurrentProfile == null) CurrentProfile = new Profile(); // 存在しなければ生成

            // ウインドウサイズが保存されていれば反映
            if (CurrentProfile.MainWindowWidth >= 1 && CurrentProfile.MainWindowHeight >= 1)
            {
                Width = CurrentProfile.MainWindowWidth;
                Height = CurrentProfile.MainWindowHeight;
            }
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
                SSPDirPath = null;
                try
                {
                    // HWndからプロセスIDを取得
                    int procId;
                    Win32API.GetWindowThreadProcessId((IntPtr)FMOGhostList.First().HWnd, out procId);
                    var sspProc = Process.GetProcessById(procId);
                    SSPDirPath = Path.GetDirectoryName(sspProc.MainModule.FileName);
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
                    if (!beforeAbsent && currentAbsent)
                    {
                        AbsenceInfo[ghost.DirPath] = AbsenceImageKeys[rand.Next(0, AbsenceImageKeys.Count)];
                    }

                    // いない→いるに変わった場合、すでに読み込んだシェル情報をクリアして読み込み直し
                    if (beforeAbsent && !currentAbsent)
                    {
                        LoadShellAndFaceImage(ghost, reload: true);
                    }

                    if (!currentAbsent)
                    {
                        // いる場合は不在情報削除
                        if (AbsenceInfo.ContainsKey(ghost.DirPath))
                        {
                            AbsenceInfo.Remove(ghost.DirPath);
                        }
                    }

                    // ゴーストの顔画像表示を更新
                    UpdateFaceImageKey(ghost);
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
            DescriptionText = GetDescriptionText();

            // ゴースト切り替えボタン、および付随する設定チェックボックスは、SSP起動時かつゴースト選択中のみ表示
            BtnChange.Visible = ChkCloseAfterChange.Visible = (CallerId != null && SelectedGhost != null);

            // ゴースト呼び出しボタンは、ゴースト選択中のみ表示
            BtnCall.Visible = (SelectedGhost != null);

            // ゴースト呼び出しボタンは、通常は常に押下可能
            BtnCall.Enabled = true;

            // ゴースト切り替えボタンは、呼び出し元ゴーストが残っていないと押下できない
            BtnChange.Enabled = ChkCloseAfterChange.Enabled = !(CallerLost);

            // ランダム選択ボタンは、1件以上のゴーストがいる場合のみ押下可能
            BtnRandomSelect.Enabled = (GhostManager.Ghosts.Any());

            // オプション選択時のみ
            BtnAddStartMenu.Visible = OptionSelected;
            BtnRemoveStartMenu.Visible = OptionSelected;
            lblVersion.Visible = OptionSelected;
            lblPoweredBy.Visible = OptionSelected;

            // スタートメニューショートカット削除ボタンは、存在する場合のみ押下可能
            BtnRemoveStartMenu.Enabled = File.Exists(StartMenuShortcutPath);

            // 選択ゴーストがすでに不在の場合は、上記に優先してボタンを無効化
            if (SelectedGhost != null && AbsenceInfo.ContainsKey(SelectedGhost.DirPath))
            {
                BtnCall.Enabled = false;
                BtnChange.Enabled = ChkCloseAfterChange.Enabled = false;
            }

            // ゴーストの立ち絵は、選択されているゴーストがいて、かつ不在でない場合のみ表示
            SelectedGhostSurfaceVisible = (SelectedGhost != null && !AbsenceInfo.ContainsKey(SelectedGhost.DirPath));

            // 立ち絵Paint再発生
            picSurface.Invalidate();

            // デバッグ用
#if DEBUG
            BtnOpenShellFolder.Visible = true;
            BtnReloadShell.Visible = true;
#endif
        }

        /// <summary>
        /// ゴースト一覧の選択切り替え時処理
        /// </summary>
        private void lstGhost_SelectedIndexChanged(object sender, EventArgs e)
        {
            // ゴースト未選択時はスキップ
            if (SelectedGhost == null)
            {
                UpdateUIState();
                return;
            };

            // エラーメッセージリストを初期化
            SurfaceNotificationMessages.Clear();

            // 読み込んだサーフェスを一度解除
            if (CurrentSakuraSurface != null) CurrentSakuraSurface.Dispose();
            CurrentSakuraSurface = null;
            if (CurrentKeroSurface != null) CurrentKeroSurface.Dispose();
            CurrentKeroSurface = null;

            // シェルが読み込み状態かどうかで処理を分ける
            if (Shells.ContainsKey(SelectedGhost.DirPath))
            {
                var shell = Shells[SelectedGhost.DirPath];

                // sakura側のサーフェス画像を取得
                try
                {
                    CurrentSakuraSurface = GhostManager.DrawSakuraSurface(SelectedGhost, shell);

                    // サーフェスが何らかの原因で見つからなかった場合はエラー扱い
                    if (CurrentSakuraSurface == null)
                    {
                        SurfaceNotificationMessages.Add(@"ERROR: 本体側の立ち絵描画に失敗しました。");
                    }
                }
                catch (UnhandlableShellException ex)
                {
                    ex.Scope = 0; // sakura側のエラー

                    CurrentSakuraSurface = null;
                    Debug.WriteLine(ex.ToString());
                    SurfaceNotificationMessages.Add(ex.FriendlyMessage);
                }
                catch (Exception ex)
                {
                    CurrentSakuraSurface = null;
                    Debug.WriteLine(ex.ToString());
                    SurfaceNotificationMessages.Add(@"ERROR: 本体側の立ち絵描画に失敗しました。");
                }

                // kero側のサーフェス画像を取得
                try
                {
                    CurrentKeroSurface = GhostManager.DrawKeroSurface(SelectedGhost, shell);
                }
                catch (UnhandlableShellException ex)
                {
                    ex.Scope = 1; // kero側のエラー

                    CurrentKeroSurface = null;
                    Debug.WriteLine(ex.ToString());
                    SurfaceNotificationMessages.Add(ex.FriendlyMessage);
                }
                catch (Exception ex)
                {
                    CurrentKeroSurface = null;
                    Debug.WriteLine(ex.ToString());
                    SurfaceNotificationMessages.Add(@"ERROR: パートナー側の立ち絵描画に失敗しました。");
                }
            }
            else
            {
                // シェルが未読み込み状態ならエラー
                if (ErrorMessagesOnShellLoading.ContainsKey(SelectedGhost.DirPath))
                {
                    SurfaceNotificationMessages.AddRange(ErrorMessagesOnShellLoading[SelectedGhost.DirPath]);
                }
                else
                {
                    SurfaceNotificationMessages.Add("シェル情報の読み込みが完了していません。もう少し経ってから選択し直してみてください。");
                }
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
                var scaleRateByWidth = e.ClipRectangle.Width / (double)requiredWidth;
                var scaleRateByHeight = e.ClipRectangle.Height / (double)requiredHeight;
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
                if (SurfaceNotificationMessages.Any())
                {
                    return string.Join("\r\n", SurfaceNotificationMessages);
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
            // ゴースト変更
            SendSSTPScript(@"\![change,ghost," + Util.QuoteForSakuraScriptParameter(SelectedGhost.Name) + @",--option=raise-event]\e");

            // ゴースト変更後にアプリケーション終了
            if (ChkCloseAfterChange.Checked)
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

            // 立っているゴーストが一人もおらず、かつSSP.exeのパスが特定できるなら、代わりにSSP.exeの起動を試みる
            if (!FMOGhostList.Any() && SSPDirPath != null)
            {
                Process.Start(Path.Combine(SSPDirPath, "SSP.exe"), string.Format(@"/g ""{0}""", SelectedGhost.Name));
                return;
            }

            // ゴーストを呼ぶ
            SendSSTPScript(@"\![call,ghost," + Util.QuoteForSakuraScriptParameter(SelectedGhost.Name) + @",--option=raise-event]\e");
        }

        /// <summary>
        /// SSTP送信
        /// </summary>
        protected bool SendSSTPScript(string script)
        {
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

            return SendSSTPScript(req);
        }

        /// <summary>
        /// SSTP送信
        /// </summary>
        protected bool SendSSTPScript(SSTPClient.Request req)
        {
            SSTPClient.Response res;
            return SendSSTPScript(req, out res);
        }

        /// <summary>
        /// SSTP送信
        /// </summary>
        protected bool SendSSTPScript(SSTPClient.Request req, out SSTPClient.Response res)
        {
            res = null;
            var sstpClient = new SSTPClient();
            try
            {
                res = sstpClient.SendRequest(req);

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
        /// 起動後の初期設定 (設定読み込み、ゴースト情報読み込みなど)
        /// </summary>
        protected virtual void Setup()
        {
            // 既存情報クリア
            lstGhost.Clear();
            imgListFace.Images.Clear();

            // FMO情報更新
            UpdateFMOInfo();

            // この時点でSSPのパスが特定できない場合は、SSPが起動していないためエラー

            // コマンドライン引数を解釈
            var args = Environment.GetCommandLineArgs().ToList();
            args.RemoveAt(0); // 先頭はexe名のため削除

            var caller = args.FirstOrDefault(); // 呼び出し元ゴースト (id or "unspecified") 省略された場合はunspecified扱い
            if (args.Any()) args.RemoveAt(0); // 先頭削除

            var specifiedSSPDirPath = args.FirstOrDefault(); // SSPが存在するフォルダパス
            if (!string.IsNullOrWhiteSpace(specifiedSSPDirPath))
            {
                if (!Directory.Exists(specifiedSSPDirPath))
                {
                    MessageBox.Show("起動パラメータで指定されたSSPフォルダが見つかりませんでした。\n" + specifiedSSPDirPath
                                  , "エラー"
                                  , MessageBoxButtons.OK
                                  , MessageBoxIcon.Error);
                    Application.Exit();
                    return;
                }

                SSPDirPath = specifiedSSPDirPath;
            }
            if (args.Any()) args.RemoveAt(0); // 先頭削除

            // この時点でSSPパスが特定できていない場合はエラー
            if (SSPDirPath == null)
            {
                MessageBox.Show("SSPが見つかりませんでした。\nゴーストエクスプローラ通は、SSPが起動している状態で実行するか、もしくはWindowsのスタートメニューから実行する必要があります。"
                              , "エラー"
                              , MessageBoxButtons.OK
                              , MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            SakuraFMOData target = null;
            if (!string.IsNullOrWhiteSpace(caller))
            {
                var matched = Regex.Match(caller, @"^id:(.+?)\z");
                if (matched.Success)
                {
                    // idが指定された場合、そのidと一致するゴースト情報を探す
                    var id = matched.Groups[1].Value.Trim();
                    target = FMOGhostList.FirstOrDefault(g => g.Id == id);
                }
            }

            if (caller == "unspecified" || string.IsNullOrWhiteSpace(caller))
            {
                // "unspecified" が指定された場合、現在起動しているゴーストのうち、ランダムに1体を呼び出し元とする
                if (FMOGhostList.Any())
                {
                    var rand = new Random();
                    target = FMOGhostList[rand.Next(0, FMOGhostList.Count - 1)];
                }
            }

            // SSPのapp.datからゴーストフォルダの設定を取得
            var ghostDirPaths = new List<string>();
            var appDataPath = Path.Combine(SSPDirPath, @"data\profile\app.dat");
            if (File.Exists(appDataPath))
            {
                // descript.txtと同じフォーマット
                var appData = DescriptText.Load(appDataPath);

                // path.ghost.Num を取得
                var ghostNumStr = appData.Get("path.ghost.Num");
                int ghostNum;
                if (ghostNumStr != null && int.TryParse(ghostNumStr, out ghostNum))
                {
                    // ゴーストパスを1つずつ取得して追加
                    for (var i = 0; i < ghostNum; i++)
                    {
                        var dirPath = appData.Get(string.Format("path.ghost.{0}", i));
                        if (dirPath != null)
                        {
                            // 最初が "." から始まっている場合は相対パスとみなして、絶対パスに変換
                            if (dirPath.StartsWith("."))
                            {
                                dirPath = Path.GetFullPath(Path.Combine(SSPDirPath, dirPath));
                            }

                            // 存在するなら追加
                            if (Directory.Exists(dirPath))
                            {
                                ghostDirPaths.Add(dirPath);
                            }
                        }
                    }
                }
            }

            // ゴーストフォルダ選択パスの設定
            foreach (var dirPath in ghostDirPaths)
            {
                var trimmedPath = dirPath.TrimEnd('\\');
                var label = string.Format("{0} [{1}]", Path.GetFileName(trimmedPath), trimmedPath);
                cmbGhostDir.Items.Add(new DropDownItem() { Value = trimmedPath, Label = label });
            }

            // パスが複数ない場合は、コンボボックス非表示
            if (ghostDirPaths.Count == 1)
            {
                lstGhost.Height += lstGhost.Top;
                lstGhost.Top = 0;
                cmbGhostDir.Visible = false;
            }

            // 呼び出し元の情報をプロパティにセット
            if (target != null)
            {
                CallerId = target.Id;
                CallerSakuraName = target.Name;
                CallerKeroName = target.KeroName;
                CallerHWnd = (IntPtr)target.HWnd;
            }

            // ボタンラベル変更
            BtnChange.Text = string.Format("{0}から切り替え", CallerSakuraName);

            // チェックボックスの位置移動
            ChkCloseAfterChange.Left = BtnChange.Left + 1;

            // イメージリストに不在アイコンを追加
            AbsenceImageKeys.Clear();
            foreach (var path in Directory.GetFiles(Path.Combine(Util.GetAppDirPath(), @"res\absence_icon"), "*.png"))
            {
                var fileName = Path.GetFileName(path);
                imgListFace.Images.Add(fileName, new Bitmap(path));
                AbsenceImageKeys.Add(fileName);
            }

            // 最終起動時の記録があり、かつ最終起動時とバージョンが異なる場合は、キャッシュをすべて破棄
            if (CurrentProfile.LastBootVersion != null && Util.GetVersion() != CurrentProfile.LastBootVersion)
            {
                Directory.Delete(Util.GetCacheDirPath(), recursive: true);
            }

            // 最終起動情報をセットして、Profileを保存
            CurrentProfile.LastBootVersion = Util.GetVersion();
            Util.SaveProfile(CurrentProfile);

            // ゴーストフォルダ選択ドロップダウンの項目を選択
            // 前回の選択フォルダと一致するものがあればそれを選択
            // なければ先頭項目を選択
            {
                var lastUseIndex = -1;
                for (var i = 0; i < cmbGhostDir.Items.Count; i++)
                {
                    var item = (DropDownItem)cmbGhostDir.Items[i];
                    if (item.Value == CurrentProfile.LastUsePath)
                    {
                        lastUseIndex = i;
                        break;
                    }
                }

                if (lastUseIndex >= 0)
                {
                    cmbGhostDir.SelectedIndex = lastUseIndex;
                }
                else
                {
                    cmbGhostDir.SelectedIndex = 0;
                }
            }

            // ソート選択ドロップダウンの項目を選択
            // 前回の選択フォルダと一致するものがあればそれを選択
            // なければ先頭項目を選択
            {

                var lastSortIndex = -1;
                for (var i = 0; i < cmbSort.Items.Count; i++)
                {
                    var item = (DropDownItem)cmbSort.Items[i];
                    if (item.Value == CurrentProfile.LastSortType)
                    {
                        lastSortIndex = i;
                        break;
                    }
                }

                if (lastSortIndex >= 0)
                {
                    cmbSort.SelectedIndex = lastSortIndex;
                }
                else
                {
                    cmbSort.SelectedIndex = 0;
                }
            }

            // ゴースト情報の読み込みと一覧表示更新
            UpdateGhostList();
        }

        /// <summary>
        /// 検索条件変更
        /// </summary>
        protected void OnSearchConditionChanged(bool saveProfile = true)
        {
            // 現在実行中のシェル読み込みタスクがあれば、キャンセル操作を行い、中断を待つ
            if (GhostImageLoadingTask != null)
            {
                Debug.WriteLine(string.Format("Cancel loading task>>>"));
                GhostImageCancellationTokenSource.Cancel();
                GhostImageLoadingTask.Wait();
                Debug.WriteLine(string.Format("<<<Cancel loading task"));
            }

            // プロファイルに検索条件を書き込む
            if (saveProfile)
            {
                var selectedGhostDirPath = ((DropDownItem)cmbGhostDir.SelectedItem).Value;
                CurrentProfile.LastUsePath = selectedGhostDirPath;
                var sortType = ((DropDownItem)cmbSort.SelectedItem).Value;
                CurrentProfile.LastSortType = sortType;
                Util.SaveProfile(CurrentProfile);
            }

            // ゴースト情報の読み込みと一覧表示更新
            UpdateGhostList();
        }

        /// <summary>
        /// ゴースト情報の読み込みと一覧表示更新
        /// </summary>
        protected virtual void UpdateGhostList()
        {
            var selectedGhostDirPath = ((DropDownItem)cmbGhostDir.SelectedItem).Value;
            var filterWord = txtFilter.Text.Trim();
            var sortType = ((DropDownItem)cmbSort.SelectedItem).Value;

            // Realize2Textを読み込む
            var realize2Path = Path.Combine(SSPDirPath, @"data\profile\realize2.txt");
            if (File.Exists(realize2Path))
            {
                Realize2Text = Realize2Text.Load(realize2Path);
            }

            // ゴースト情報読み込み
            GhostManager = GhostManager.Load(Realize2Text, selectedGhostDirPath, filterWord, sortType);

            // リスト構築
            var listGroups = new Dictionary<string, ListViewGroup>();
            lstGhost.Items.Clear();

            // シェル読み込み時のエラー情報はクリア
            ErrorMessagesOnShellLoading.Clear();

            // 1件以上存在する場合
            if (GhostManager.Ghosts.Any())
            {
                // リスト項目追加
                foreach (var ghost in GhostManager.Ghosts)
                {
                    var item = lstGhost.Items.Add(key: ghost.DirPath, text: ghost.Name ?? "", imageKey: ghost.DirPath);

                    // ゴースト格納フォルダのパスを元に、グループも設定
                    if (!listGroups.ContainsKey(ghost.GhostBaseDirPath))
                    {
                        listGroups[ghost.GhostBaseDirPath] = new ListViewGroup(ghost.GhostBaseDirPath);
                        lstGhost.Groups.Add(listGroups[ghost.GhostBaseDirPath]);
                    }
                    item.Group = listGroups[ghost.GhostBaseDirPath];
                }


                // 最初に1件目のシェル情報、顔画像のみ同期的に読み込んでおく
                var firstGhost = GhostManager.Ghosts[0];
                LoadShellAndFaceImage(firstGhost);

                // 先頭を選択
                lstGhost.Items[0].Focused = true;
                lstGhost.Items[0].Selected = true;
                lstGhost.EnsureVisible(0);
            }

            // 最後にメニュー項目追加
            lstGhost.Items.Add(key: "option", text: "オプション", imageKey: null);

            // ゴーストごとのシェル・画像読み込み処理
            prgLoading.Maximum = GhostManager.Ghosts.Count;
            prgLoading.Value = 0;
            GhostImageCancellationTokenSource = new CancellationTokenSource();
            var token = GhostImageCancellationTokenSource.Token;
            GhostImageLoadingTask = Task.Factory.StartNew(() => LoadGhostShellsAsync(token), token);

            // ゴースト情報リストの更新に伴う画面表示更新
            UpdateUIOnFMOChanged();
        }

        /// <summary>
        /// シェル情報および顔画像を読み込む
        /// </summary>
        public virtual async Task LoadGhostShellsAsync(CancellationToken cToken)
        {
            try
            {
                var progressVisible = (GhostManager.Ghosts.Count >= 200); // プログレスバーはゴーストが200件を超えた場合に初めて表示
                BeginInvoke((MethodInvoker)(() =>
                {
                    prgLoading.Visible = progressVisible;
                }));
                Debug.WriteLine(string.Format("<{0}> GhostImagesLoadAsync Start >>>", Thread.CurrentThread.ManagedThreadId));

                // シェル情報 (surfaces.txt の情報など) の読み込み
                foreach (var ghost in GhostManager.Ghosts)
                {
                    // まだ読み込んでいなければ、シェル情報を読み込む
                    if (!Shells.ContainsKey(ghost.DirPath) || Shells[ghost.DirPath] == null)
                    {
                        LoadShell(ghost);
                    }

                    // プログレスバー進める
                    if (progressVisible)
                    {
                        BeginInvoke((MethodInvoker)(() =>
                        {
                            prgLoading.Increment(1);
                        }));
                    }

                    // キャンセル処理
                    if (cToken.IsCancellationRequested)
                    {
                        Debug.WriteLine(string.Format("<{0}> canceled", Thread.CurrentThread.ManagedThreadId, ghost.Name));
                        return;
                    }
                }

                // シェル情報読み込みまで完了したらプログレスバー非表示
                Invoke((MethodInvoker)(() =>
                {
                    prgLoading.Hide();
                }));


                // 顔画像の取得
                foreach (var ghost in GhostManager.Ghosts)
                {
                    // 顔画像の読み込み
                    LoadFaceImageIfShellLoaded(ghost);

                    // UI反映
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        // 顔画像をUI側に反映
                        ReflectFaceImageToUIIfShellLoaded(ghost);
                    }));

                    // キャンセル処理
                    if (cToken.IsCancellationRequested)
                    {
                        Debug.WriteLine(string.Format("<{0}> canceled", Thread.CurrentThread.ManagedThreadId, ghost.Name));
                        return;
                    }
                }

                Debug.WriteLine(string.Format("<{0}> <<< GhostImagesLoadAsync End", Thread.CurrentThread.ManagedThreadId));

            }
            catch (Exception ex)
            {
                // ロード中にエラーが発生し、補足できなかった場合は、エラーダイアログを表示する
                MessageBox.Show("シェル情報の読み込み中にシステムエラーが発生しました。\nご迷惑をおかけし、申し訳ありません。\n\n" + ex.ToString()
                              , "エラー"
                              , MessageBoxButtons.OK
                              , MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 指定ゴーストのシェルを同期的に読み込む
        /// </summary>
        protected void LoadShellAndFaceImage(Ghost ghost, bool reload = false)
        {
            // リロードフラグONの場合は既存情報を破棄
            if (reload)
            {
                Shells[ghost.DirPath] = null;

                if (FaceImages.ContainsKey(ghost.DirPath))
                {
                    if (FaceImages[ghost.DirPath] != null) FaceImages[ghost.DirPath].Dispose();
                    FaceImages.Remove(ghost.DirPath);
                }
                imgListFace.Images.RemoveByKey(ghost.DirPath);
            }

            // メイン読み込み処理。シェルをまだ読み込んでいない場合、もしくはリロードフラグON時のみ実行
            if (reload
                || !Shells.ContainsKey(ghost.DirPath)
                || Shells[ghost.DirPath] == null)
            {
                // シェル読み込み
                LoadShell(ghost);

                // 顔画像読み込み
                LoadFaceImageIfShellLoaded(ghost);

                // UI側へ反映
                ReflectFaceImageToUIIfShellLoaded(ghost);
            }
        }

        /// <summary>
        /// 指定ゴーストのシェルを読み込む
        /// </summary>
        protected virtual void LoadShell(Ghost ghost)
        {
            // ロック取得 (同期処理と非同期処理が競合しないように)
            lock (GhostManager)
            {
                try
                {
                    Shells[ghost.DirPath] = GhostManager.LoadShell(ghost);
                    Debug.WriteLine(string.Format("<{0}> shell loaded: {1}", Thread.CurrentThread.ManagedThreadId, ghost.Name));
                }
                catch (UnhandlableShellException ex)
                {
                    // 処理不可能なシェル
                    if (!ErrorMessagesOnShellLoading.ContainsKey(ghost.DirPath))
                    {
                        ErrorMessagesOnShellLoading[ghost.DirPath] = new List<string>();
                    }
                    ErrorMessagesOnShellLoading[ghost.DirPath].Add(ex.FriendlyMessage);
                }
            }
        }

        /// <summary>
        /// 指定ゴーストの顔画像を読み込む (シェルの読み込みに成功していれば)
        /// </summary>
        protected virtual void LoadFaceImageIfShellLoaded(Ghost ghost)
        {
            // ロック取得 (同期処理と非同期処理が競合しないように)
            lock (GhostManager)
            {
                // シェルの読み込みに成功している場合のみ
                if (Shells.ContainsKey(ghost.DirPath) && Shells[ghost.DirPath] != null)
                {
                    // まだ読み込んでいなければ、ゴーストの顔画像を変換・取得
                    if (!FaceImages.ContainsKey(ghost.DirPath))
                    {
                        FaceImages[ghost.DirPath] = GhostManager.GetFaceImage(ghost, Shells[ghost.DirPath], imgListFace.ImageSize);
                        Debug.WriteLine(string.Format("<{0}> faceImage loaded: {1}", Thread.CurrentThread.ManagedThreadId, ghost.Name));
                    }

                }
            }
        }

        /// <summary>
        /// 指定ゴーストの顔画像をFormへ反映
        /// </summary>
        protected virtual void ReflectFaceImageToUIIfShellLoaded(Ghost ghost)
        {
            // ロック取得 (同期処理と非同期処理が競合しないように)
            lock (GhostManager)
            {
                // シェルの読み込みに成功している場合のみ
                if (Shells.ContainsKey(ghost.DirPath) && Shells[ghost.DirPath] != null)
                {
                    // 顔画像を正常に読み込めていれば、イメージリストに追加
                    if (FaceImages[ghost.DirPath] != null)
                    {
                        imgListFace.Images.Add(ghost.DirPath, FaceImages[ghost.DirPath]);
                    }

                    // 顔画像表示を更新
                    UpdateFaceImageKey(ghost);
                }
            }
        }

        /// <summary>
        /// 指定ゴーストの顔画像を更新 (不在かどうかに応じて処理を分ける)
        /// </summary>
        protected void UpdateFaceImageKey(Ghost ghost)
        {
            // リスト内に項目がなければ何もしない
            if (!lstGhost.Items.ContainsKey(ghost.DirPath)) return;

            // 不在判定
            if (AbsenceInfo.ContainsKey(ghost.DirPath))
            {
                // いない
                lstGhost.Items[ghost.DirPath].ImageKey = AbsenceInfo[ghost.DirPath];
            }
            else
            {
                // いる
                lstGhost.Items[ghost.DirPath].ImageKey = ghost.DirPath;
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            Setup();
        }

        private void BtnOpenShellFolder_Click(object sender, EventArgs e)
        {
            if (Shells.ContainsKey(SelectedGhost.DirPath))
            {
                Process.Start(Shells[SelectedGhost.DirPath].DirPath);
            }
            else
            {
                Process.Start(SelectedGhost.DirPath);
            }
        }

        /// <summary>
        /// ウインドウメッセージ処理
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WMSakuraAPI)
            {
                // ゴースト変更通知なら処理
                if ((int)m.WParam == SAKURA_API_BROADCAST_GHOSTCHANGE)
                {
                    // プロセスIDからSSPのパスを取得
                    SSPDirPath = null;
                    try
                    {
                        var sspProc = Process.GetProcessById((int)m.LParam);
                        SSPDirPath = Path.GetDirectoryName(sspProc.MainModule.FileName);
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
        /// ゴーストフォルダ変更
        /// </summary>
        private void cmbGhostDir_SelectionChangeCommitted(object sender, EventArgs e)
        {
            OnSearchConditionChanged();
        }

        /// <summary>
        /// ソート変更
        /// </summary>
        private void cmbSort_SelectionChangeCommitted(object sender, EventArgs e)
        {
            OnSearchConditionChanged();
        }

        private void txtFilter_Leave(object sender, EventArgs e)
        {
            // 前回値と変更されている場合のみゴースト一覧更新
            if (txtFilter.Text != OldFilterText)
            {
                OnSearchConditionChanged();
            }
            OldFilterText = txtFilter.Text;
        }

        private void txtFilter_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void txtFilter_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                // 前回値と変更されている場合のみゴースト一覧更新
                if (txtFilter.Text != OldFilterText)
                {
                    OnSearchConditionChanged();
                }
                OldFilterText = txtFilter.Text;
                e.Handled = true;

            }
        }

        /// <summary>
        /// リサイズ完了
        /// </summary>
        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            // リサイズ完了時には、ウインドウサイズを保存
            CurrentProfile.MainWindowWidth = Width;
            CurrentProfile.MainWindowHeight = Height;
            Util.SaveProfile(CurrentProfile);
        }

        private void BtnReloadShell_Click(object sender, EventArgs e)
        {
            LoadShellAndFaceImage(SelectedGhost, reload: true);
        }

        private void BtnAddStartMenu_Click(object sender, EventArgs e)
        {
            //作成するショートカットのパス
            var shortcutPath = StartMenuShortcutPath;
            // フォルダを作成
            Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath));

            //WshShellを作成
            var shell = new IWshRuntimeLibrary.WshShell();
            //ショートカットのパスを指定して、WshShortcutを作成
            var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
            //リンク先
            shortcut.TargetPath = Application.ExecutablePath;
            //コマンドパラメータ 「リンク先」の後ろに付く
            shortcut.Arguments = @"unspecified """ + SSPDirPath + @"""";
            //作業フォルダ
            shortcut.WorkingDirectory = Application.StartupPath;
            //コメント
            shortcut.Description = "テストのアプリケーション";
            //アイコンのパス 自分のEXEファイルのインデックス0のアイコン
            shortcut.IconLocation = Application.ExecutablePath + ",1";

            //ショートカットを作成
            shortcut.Save();

            //後始末
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);

            UpdateUIState();

            MessageBox.Show("スタートメニューに登録しました。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRemoveStartMenu_Click(object sender, EventArgs e)
        {
            // 削除するショートカットフォルダのパス
            var shortcutDirPath = Path.GetDirectoryName(StartMenuShortcutPath);

            // 削除
            if (Directory.Exists(shortcutDirPath))
            {
                Directory.Delete(shortcutDirPath, recursive: true);
                UpdateUIState();
                MessageBox.Show("スタートメニューから削除しました。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("すでに削除されています。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
