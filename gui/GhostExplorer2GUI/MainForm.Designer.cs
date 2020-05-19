﻿namespace GhostExplorer2
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
            this.lstGhost = new System.Windows.Forms.ListView();
            this.imgListFace = new System.Windows.Forms.ImageList(this.components);
            this.picSurface = new System.Windows.Forms.PictureBox();
            this.BtnChange = new System.Windows.Forms.Button();
            this.BtnCall = new System.Windows.Forms.Button();
            this.lblLoading = new System.Windows.Forms.Label();
            this.BtnRandomSelect = new System.Windows.Forms.Button();
            this.BtnReload = new System.Windows.Forms.Button();
            this.ChkCloseAfterChange = new System.Windows.Forms.CheckBox();
            this.BtnOpenShellFolder = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picSurface)).BeginInit();
            this.SuspendLayout();
            // 
            // lstGhost
            // 
            this.lstGhost.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstGhost.FullRowSelect = true;
            this.lstGhost.HideSelection = false;
            this.lstGhost.LargeImageList = this.imgListFace;
            this.lstGhost.Location = new System.Drawing.Point(0, 0);
            this.lstGhost.MultiSelect = false;
            this.lstGhost.Name = "lstGhost";
            this.lstGhost.ShowItemToolTips = true;
            this.lstGhost.Size = new System.Drawing.Size(318, 494);
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
            // lblLoading
            // 
            this.lblLoading.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLoading.AutoSize = true;
            this.lblLoading.BackColor = System.Drawing.Color.LemonChiffon;
            this.lblLoading.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblLoading.Font = new System.Drawing.Font("メイリオ", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblLoading.Location = new System.Drawing.Point(396, 218);
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
            // BtnReload
            // 
            this.BtnReload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnReload.AutoSize = true;
            this.BtnReload.Location = new System.Drawing.Point(449, 448);
            this.BtnReload.Name = "BtnReload";
            this.BtnReload.Padding = new System.Windows.Forms.Padding(16, 0, 16, 0);
            this.BtnReload.Size = new System.Drawing.Size(121, 34);
            this.BtnReload.TabIndex = 8;
            this.BtnReload.Text = "★リロード";
            this.BtnReload.UseVisualStyleBackColor = true;
            this.BtnReload.Visible = false;
            this.BtnReload.Click += new System.EventHandler(this.BtnReload_Click);
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
            this.BtnOpenShellFolder.Location = new System.Drawing.Point(576, 448);
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
            this.Controls.Add(this.BtnReload);
            this.Controls.Add(this.BtnRandomSelect);
            this.Controls.Add(this.lblLoading);
            this.Controls.Add(this.BtnCall);
            this.Controls.Add(this.BtnChange);
            this.Controls.Add(this.lstGhost);
            this.Controls.Add(this.picSurface);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(800, 400);
            this.Name = "MainForm";
            this.Text = "ゴーストエクスプローラ通（α版）";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.picSurface)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView lstGhost;
        private System.Windows.Forms.PictureBox picSurface;
        private System.Windows.Forms.Button BtnChange;
        private System.Windows.Forms.Button BtnCall;
        private System.Windows.Forms.ImageList imgListFace;
        private System.Windows.Forms.Label lblLoading;
        private System.Windows.Forms.Button BtnRandomSelect;
        private System.Windows.Forms.Button BtnReload;
        private System.Windows.Forms.CheckBox ChkCloseAfterChange;
        private System.Windows.Forms.Button BtnOpenShellFolder;
    }
}

