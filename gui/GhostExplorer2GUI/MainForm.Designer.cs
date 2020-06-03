namespace GhostExplorer2
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.lstGhost = new System.Windows.Forms.ListView();
            this.imgListFace = new System.Windows.Forms.ImageList(this.components);
            this.picSurface = new System.Windows.Forms.PictureBox();
            this.BtnChange = new System.Windows.Forms.Button();
            this.BtnCall = new System.Windows.Forms.Button();
            this.BtnRandomSelect = new System.Windows.Forms.Button();
            this.ChkCloseAfterChange = new System.Windows.Forms.CheckBox();
            this.BtnOpenShellFolder = new System.Windows.Forms.Button();
            this.prgLoading = new System.Windows.Forms.ProgressBar();
            this.cmbGhostDir = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.cmbSort = new System.Windows.Forms.ComboBox();
            this.txtFilter = new System.Windows.Forms.TextBox();
            this.BtnReloadShell = new System.Windows.Forms.Button();
            this.BtnAddStartMenu = new System.Windows.Forms.Button();
            this.BtnRemoveStartMenu = new System.Windows.Forms.Button();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblPoweredBy = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picSurface)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // lstGhost
            // 
            this.lstGhost.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstGhost.FullRowSelect = true;
            this.lstGhost.HideSelection = false;
            this.lstGhost.LargeImageList = this.imgListFace;
            this.lstGhost.Location = new System.Drawing.Point(0, 19);
            this.lstGhost.Margin = new System.Windows.Forms.Padding(0);
            this.lstGhost.MultiSelect = false;
            this.lstGhost.Name = "lstGhost";
            this.lstGhost.ShowGroups = false;
            this.lstGhost.ShowItemToolTips = true;
            this.lstGhost.Size = new System.Drawing.Size(318, 447);
            this.lstGhost.SmallImageList = this.imgListFace;
            this.lstGhost.TabIndex = 0;
            this.lstGhost.UseCompatibleStateImageBehavior = false;
            this.lstGhost.View = System.Windows.Forms.View.SmallIcon;
            this.lstGhost.SelectedIndexChanged += new System.EventHandler(this.lstGhost_SelectedIndexChanged);
            // 
            // imgListFace
            // 
            this.imgListFace.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imgListFace.ImageSize = new System.Drawing.Size(120, 120);
            this.imgListFace.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // picSurface
            // 
            this.picSurface.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.picSurface.BackColor = System.Drawing.Color.White;
            this.picSurface.Location = new System.Drawing.Point(316, 0);
            this.picSurface.Name = "picSurface";
            this.picSurface.Size = new System.Drawing.Size(648, 494);
            this.picSurface.TabIndex = 2;
            this.picSurface.TabStop = false;
            this.picSurface.Paint += new System.Windows.Forms.PaintEventHandler(this.picSurface_Paint);
            // 
            // BtnChange
            // 
            this.BtnChange.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnChange.AutoSize = true;
            this.BtnChange.Location = new System.Drawing.Point(749, 12);
            this.BtnChange.Name = "BtnChange";
            this.BtnChange.Padding = new System.Windows.Forms.Padding(16, 0, 16, 0);
            this.BtnChange.Size = new System.Drawing.Size(97, 34);
            this.BtnChange.TabIndex = 3;
            this.BtnChange.Text = "切り替え";
            this.BtnChange.UseVisualStyleBackColor = true;
            this.BtnChange.Visible = false;
            this.BtnChange.Click += new System.EventHandler(this.BtnChange_Click);
            // 
            // BtnCall
            // 
            this.BtnCall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCall.AutoSize = true;
            this.BtnCall.Location = new System.Drawing.Point(861, 12);
            this.BtnCall.Name = "BtnCall";
            this.BtnCall.Padding = new System.Windows.Forms.Padding(16, 0, 16, 0);
            this.BtnCall.Size = new System.Drawing.Size(91, 34);
            this.BtnCall.TabIndex = 4;
            this.BtnCall.Text = "呼び出す";
            this.BtnCall.UseVisualStyleBackColor = true;
            this.BtnCall.Visible = false;
            this.BtnCall.Click += new System.EventHandler(this.BtnCall_Click);
            // 
            // BtnRandomSelect
            // 
            this.BtnRandomSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnRandomSelect.AutoSize = true;
            this.BtnRandomSelect.Location = new System.Drawing.Point(331, 448);
            this.BtnRandomSelect.Name = "BtnRandomSelect";
            this.BtnRandomSelect.Padding = new System.Windows.Forms.Padding(16, 0, 16, 0);
            this.BtnRandomSelect.Size = new System.Drawing.Size(112, 34);
            this.BtnRandomSelect.TabIndex = 7;
            this.BtnRandomSelect.Text = "ランダム選択";
            this.BtnRandomSelect.UseVisualStyleBackColor = true;
            this.BtnRandomSelect.Click += new System.EventHandler(this.BtnRandomSelect_Click);
            // 
            // ChkCloseAfterChange
            // 
            this.ChkCloseAfterChange.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ChkCloseAfterChange.AutoSize = true;
            this.ChkCloseAfterChange.BackColor = System.Drawing.Color.White;
            this.ChkCloseAfterChange.Checked = true;
            this.ChkCloseAfterChange.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChkCloseAfterChange.Location = new System.Drawing.Point(749, 52);
            this.ChkCloseAfterChange.Name = "ChkCloseAfterChange";
            this.ChkCloseAfterChange.Size = new System.Drawing.Size(116, 16);
            this.ChkCloseAfterChange.TabIndex = 9;
            this.ChkCloseAfterChange.Text = "切り替え後に閉じる";
            this.ChkCloseAfterChange.UseVisualStyleBackColor = false;
            this.ChkCloseAfterChange.Visible = false;
            // 
            // BtnOpenShellFolder
            // 
            this.BtnOpenShellFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnOpenShellFolder.AutoSize = true;
            this.BtnOpenShellFolder.Location = new System.Drawing.Point(449, 448);
            this.BtnOpenShellFolder.Name = "BtnOpenShellFolder";
            this.BtnOpenShellFolder.Padding = new System.Windows.Forms.Padding(16, 0, 16, 0);
            this.BtnOpenShellFolder.Size = new System.Drawing.Size(158, 34);
            this.BtnOpenShellFolder.TabIndex = 10;
            this.BtnOpenShellFolder.Text = "★シェルのフォルダを開く";
            this.BtnOpenShellFolder.UseVisualStyleBackColor = true;
            this.BtnOpenShellFolder.Visible = false;
            this.BtnOpenShellFolder.Click += new System.EventHandler(this.BtnOpenShellFolder_Click);
            // 
            // prgLoading
            // 
            this.prgLoading.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.prgLoading.Location = new System.Drawing.Point(12, 411);
            this.prgLoading.Name = "prgLoading";
            this.prgLoading.Size = new System.Drawing.Size(197, 23);
            this.prgLoading.TabIndex = 11;
            // 
            // cmbGhostDir
            // 
            this.cmbGhostDir.DisplayMember = "Label";
            this.cmbGhostDir.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGhostDir.Font = new System.Drawing.Font("ＭＳ ゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.cmbGhostDir.FormattingEnabled = true;
            this.cmbGhostDir.Location = new System.Drawing.Point(0, 0);
            this.cmbGhostDir.Margin = new System.Windows.Forms.Padding(0);
            this.cmbGhostDir.Name = "cmbGhostDir";
            this.cmbGhostDir.Size = new System.Drawing.Size(318, 20);
            this.cmbGhostDir.TabIndex = 13;
            this.cmbGhostDir.ValueMember = "Value";
            this.cmbGhostDir.SelectionChangeCommitted += new System.EventHandler(this.cmbGhostDir_SelectionChangeCommitted);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Controls.Add(this.cmbSort);
            this.panel1.Controls.Add(this.txtFilter);
            this.panel1.Location = new System.Drawing.Point(0, 465);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(318, 29);
            this.panel1.TabIndex = 14;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.ImageLocation = "";
            this.pictureBox1.Location = new System.Drawing.Point(3, 1);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(24, 24);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // cmbSort
            // 
            this.cmbSort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmbSort.DisplayMember = "Label";
            this.cmbSort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSort.FormattingEnabled = true;
            this.cmbSort.Location = new System.Drawing.Point(172, 4);
            this.cmbSort.Name = "cmbSort";
            this.cmbSort.Size = new System.Drawing.Size(137, 20);
            this.cmbSort.TabIndex = 1;
            this.cmbSort.ValueMember = "Value";
            this.cmbSort.SelectionChangeCommitted += new System.EventHandler(this.cmbSort_SelectionChangeCommitted);
            // 
            // txtFilter
            // 
            this.txtFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtFilter.Location = new System.Drawing.Point(29, 4);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(137, 19);
            this.txtFilter.TabIndex = 0;
            this.txtFilter.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtFilter_KeyDown);
            this.txtFilter.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtFilter_KeyPress);
            this.txtFilter.Leave += new System.EventHandler(this.txtFilter_Leave);
            // 
            // BtnReloadShell
            // 
            this.BtnReloadShell.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnReloadShell.AutoSize = true;
            this.BtnReloadShell.Location = new System.Drawing.Point(613, 448);
            this.BtnReloadShell.Name = "BtnReloadShell";
            this.BtnReloadShell.Padding = new System.Windows.Forms.Padding(16, 0, 16, 0);
            this.BtnReloadShell.Size = new System.Drawing.Size(235, 34);
            this.BtnReloadShell.TabIndex = 15;
            this.BtnReloadShell.Text = "★選択中ゴーストのシェルを再読み込み";
            this.BtnReloadShell.UseVisualStyleBackColor = true;
            this.BtnReloadShell.Visible = false;
            this.BtnReloadShell.Click += new System.EventHandler(this.BtnReloadShell_Click);
            // 
            // BtnAddStartMenu
            // 
            this.BtnAddStartMenu.AutoSize = true;
            this.BtnAddStartMenu.Location = new System.Drawing.Point(336, 19);
            this.BtnAddStartMenu.Name = "BtnAddStartMenu";
            this.BtnAddStartMenu.Padding = new System.Windows.Forms.Padding(16, 0, 16, 0);
            this.BtnAddStartMenu.Size = new System.Drawing.Size(317, 34);
            this.BtnAddStartMenu.TabIndex = 17;
            this.BtnAddStartMenu.Text = "Windows スタートメニューにゴーストエクスプローラ通を登録";
            this.BtnAddStartMenu.UseVisualStyleBackColor = true;
            this.BtnAddStartMenu.Visible = false;
            this.BtnAddStartMenu.Click += new System.EventHandler(this.BtnAddStartMenu_Click);
            // 
            // BtnRemoveStartMenu
            // 
            this.BtnRemoveStartMenu.AutoSize = true;
            this.BtnRemoveStartMenu.Location = new System.Drawing.Point(659, 19);
            this.BtnRemoveStartMenu.Name = "BtnRemoveStartMenu";
            this.BtnRemoveStartMenu.Padding = new System.Windows.Forms.Padding(16, 0, 16, 0);
            this.BtnRemoveStartMenu.Size = new System.Drawing.Size(95, 34);
            this.BtnRemoveStartMenu.TabIndex = 18;
            this.BtnRemoveStartMenu.Text = "登録解除";
            this.BtnRemoveStartMenu.UseVisualStyleBackColor = true;
            this.BtnRemoveStartMenu.Visible = false;
            this.BtnRemoveStartMenu.Click += new System.EventHandler(this.BtnRemoveStartMenu_Click);
            // 
            // lblVersion
            // 
            this.lblVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblVersion.AutoSize = true;
            this.lblVersion.BackColor = System.Drawing.Color.White;
            this.lblVersion.Location = new System.Drawing.Point(859, 435);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(70, 12);
            this.lblVersion.TabIndex = 19;
            this.lblVersion.Text = "version: x.x.x";
            this.lblVersion.Visible = false;
            // 
            // lblPoweredBy
            // 
            this.lblPoweredBy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPoweredBy.AutoSize = true;
            this.lblPoweredBy.BackColor = System.Drawing.Color.White;
            this.lblPoweredBy.Location = new System.Drawing.Point(723, 454);
            this.lblPoweredBy.Name = "lblPoweredBy";
            this.lblPoweredBy.Size = new System.Drawing.Size(206, 12);
            this.lblPoweredBy.TabIndex = 20;
            this.lblPoweredBy.Text = "powered by 偽SERIKO and MagicK.NET";
            this.lblPoweredBy.Visible = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(964, 494);
            this.Controls.Add(this.lblPoweredBy);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.BtnRemoveStartMenu);
            this.Controls.Add(this.BtnAddStartMenu);
            this.Controls.Add(this.BtnReloadShell);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.cmbGhostDir);
            this.Controls.Add(this.prgLoading);
            this.Controls.Add(this.BtnOpenShellFolder);
            this.Controls.Add(this.ChkCloseAfterChange);
            this.Controls.Add(this.BtnRandomSelect);
            this.Controls.Add(this.BtnCall);
            this.Controls.Add(this.BtnChange);
            this.Controls.Add(this.lstGhost);
            this.Controls.Add(this.picSurface);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(800, 400);
            this.Name = "MainForm";
            this.Text = "ゴーストエクスプローラ通（β版）";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.ResizeEnd += new System.EventHandler(this.MainForm_ResizeEnd);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.picSurface)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView lstGhost;
        private System.Windows.Forms.PictureBox picSurface;
        private System.Windows.Forms.Button BtnChange;
        private System.Windows.Forms.Button BtnCall;
        private System.Windows.Forms.ImageList imgListFace;
        private System.Windows.Forms.Button BtnRandomSelect;
        private System.Windows.Forms.CheckBox ChkCloseAfterChange;
        private System.Windows.Forms.Button BtnOpenShellFolder;
        private System.Windows.Forms.ProgressBar prgLoading;
        private System.Windows.Forms.ComboBox cmbGhostDir;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox cmbSort;
        private System.Windows.Forms.TextBox txtFilter;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button BtnReloadShell;
        private System.Windows.Forms.Button BtnAddStartMenu;
        private System.Windows.Forms.Button BtnRemoveStartMenu;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblPoweredBy;
    }
}

