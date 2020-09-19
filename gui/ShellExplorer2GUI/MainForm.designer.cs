namespace ShellExplorer2
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
            this.lstShell = new System.Windows.Forms.ListView();
            this.imgListFace = new System.Windows.Forms.ImageList(this.components);
            this.picSurface = new System.Windows.Forms.PictureBox();
            this.BtnChange = new System.Windows.Forms.Button();
            this.lblLoading = new System.Windows.Forms.Label();
            this.BtnRandomSelect = new System.Windows.Forms.Button();
            this.ChkCloseAfterChange = new System.Windows.Forms.CheckBox();
            this.BtnOpenShellFolder = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picSurface)).BeginInit();
            this.SuspendLayout();
            // 
            // lstShell
            // 
            this.lstShell.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstShell.FullRowSelect = true;
            this.lstShell.HideSelection = false;
            this.lstShell.LargeImageList = this.imgListFace;
            this.lstShell.Location = new System.Drawing.Point(0, 0);
            this.lstShell.MultiSelect = false;
            this.lstShell.Name = "lstShell";
            this.lstShell.ShowItemToolTips = true;
            this.lstShell.Size = new System.Drawing.Size(318, 494);
            this.lstShell.SmallImageList = this.imgListFace;
            this.lstShell.TabIndex = 0;
            this.lstShell.UseCompatibleStateImageBehavior = false;
            this.lstShell.View = System.Windows.Forms.View.SmallIcon;
            this.lstShell.SelectedIndexChanged += new System.EventHandler(this.lstGhost_SelectedIndexChanged);
            this.lstShell.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lstShell_MouseDoubleClick);
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
            this.BtnChange.Location = new System.Drawing.Point(837, 12);
            this.BtnChange.Name = "BtnChange";
            this.BtnChange.Padding = new System.Windows.Forms.Padding(16, 0, 16, 0);
            this.BtnChange.Size = new System.Drawing.Size(97, 34);
            this.BtnChange.TabIndex = 3;
            this.BtnChange.Text = "切り替え";
            this.BtnChange.UseVisualStyleBackColor = true;
            this.BtnChange.Visible = false;
            this.BtnChange.Click += new System.EventHandler(this.BtnChange_Click);
            // 
            // lblLoading
            // 
            this.lblLoading.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLoading.AutoSize = true;
            this.lblLoading.BackColor = System.Drawing.Color.LemonChiffon;
            this.lblLoading.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblLoading.Font = new System.Drawing.Font("メイリオ", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblLoading.Location = new System.Drawing.Point(760, 403);
            this.lblLoading.Name = "lblLoading";
            this.lblLoading.Padding = new System.Windows.Forms.Padding(16, 8, 16, 8);
            this.lblLoading.Size = new System.Drawing.Size(174, 42);
            this.lblLoading.TabIndex = 5;
            this.lblLoading.Text = "読み込み中です...";
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
            this.BtnRandomSelect.Visible = false;
            this.BtnRandomSelect.Click += new System.EventHandler(this.BtnRandomSelect_Click);
            // 
            // ChkCloseAfterChange
            // 
            this.ChkCloseAfterChange.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ChkCloseAfterChange.AutoSize = true;
            this.ChkCloseAfterChange.BackColor = System.Drawing.Color.White;
            this.ChkCloseAfterChange.Checked = true;
            this.ChkCloseAfterChange.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChkCloseAfterChange.Location = new System.Drawing.Point(837, 52);
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
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(964, 494);
            this.Controls.Add(this.BtnOpenShellFolder);
            this.Controls.Add(this.ChkCloseAfterChange);
            this.Controls.Add(this.BtnRandomSelect);
            this.Controls.Add(this.lblLoading);
            this.Controls.Add(this.BtnChange);
            this.Controls.Add(this.lstShell);
            this.Controls.Add(this.picSurface);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(800, 400);
            this.Name = "MainForm";
            this.Text = "シェルエクスプローラ通";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.ResizeEnd += new System.EventHandler(this.MainForm_ResizeEnd);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.picSurface)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView lstShell;
        private System.Windows.Forms.PictureBox picSurface;
        private System.Windows.Forms.Button BtnChange;
        private System.Windows.Forms.ImageList imgListFace;
        private System.Windows.Forms.Label lblLoading;
        private System.Windows.Forms.Button BtnRandomSelect;
        private System.Windows.Forms.CheckBox ChkCloseAfterChange;
        private System.Windows.Forms.Button BtnOpenShellFolder;
    }
}

