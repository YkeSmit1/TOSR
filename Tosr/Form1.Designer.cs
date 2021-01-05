namespace Tosr
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.buttonGetAuction = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItemLoadSet = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemSaveSet = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem12 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem13 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemGoToBoard = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem10 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemUseSolver = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton4 = new System.Windows.Forms.ToolStripButton();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.panelNorth = new System.Windows.Forms.Panel();
            this.panelSouth = new System.Windows.Forms.Panel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
            this.toolStripMenuItemOneBoard = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonGetAuction
            // 
            this.buttonGetAuction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonGetAuction.Location = new System.Drawing.Point(403, 484);
            this.buttonGetAuction.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonGetAuction.Name = "buttonGetAuction";
            this.buttonGetAuction.Size = new System.Drawing.Size(105, 27);
            this.buttonGetAuction.TabIndex = 2;
            this.buttonGetAuction.Text = "Get Auction";
            this.buttonGetAuction.UseVisualStyleBackColor = true;
            this.buttonGetAuction.Click += new System.EventHandler(this.ButtonGetAuctionClick);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem4,
            this.toolStripMenuItem12,
            this.toolStripMenuItem5,
            this.toolStripMenuItem6});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(730, 24);
            this.menuStrip1.TabIndex = 14;
            this.menuStrip1.Text = "File";
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem11,
            this.toolStripSeparator2,
            this.toolStripMenuItemLoadSet,
            this.toolStripMenuItemSaveSet});
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(37, 20);
            this.toolStripMenuItem4.Text = "File";
            // 
            // toolStripMenuItem11
            // 
            this.toolStripMenuItem11.Name = "toolStripMenuItem11";
            this.toolStripMenuItem11.ShortcutKeys = System.Windows.Forms.Keys.F7;
            this.toolStripMenuItem11.Size = new System.Drawing.Size(173, 22);
            this.toolStripMenuItem11.Text = "Rules database";
            this.toolStripMenuItem11.Click += new System.EventHandler(this.ToolStripMenuItem11Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(170, 6);
            // 
            // toolStripMenuItemLoadSet
            // 
            this.toolStripMenuItemLoadSet.Name = "toolStripMenuItemLoadSet";
            this.toolStripMenuItemLoadSet.Size = new System.Drawing.Size(173, 22);
            this.toolStripMenuItemLoadSet.Text = "Load set from PBN";
            this.toolStripMenuItemLoadSet.Click += new System.EventHandler(this.ToolStripMenuItemLoadSetClick);
            // 
            // toolStripMenuItemSaveSet
            // 
            this.toolStripMenuItemSaveSet.Name = "toolStripMenuItemSaveSet";
            this.toolStripMenuItemSaveSet.Size = new System.Drawing.Size(173, 22);
            this.toolStripMenuItemSaveSet.Text = "Save set to PBN";
            this.toolStripMenuItemSaveSet.Click += new System.EventHandler(this.ToolStripMenuItemSaveSetClick);
            // 
            // toolStripMenuItem12
            // 
            this.toolStripMenuItem12.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem13,
            this.toolStripMenuItemGoToBoard});
            this.toolStripMenuItem12.Name = "toolStripMenuItem12";
            this.toolStripMenuItem12.Size = new System.Drawing.Size(44, 20);
            this.toolStripMenuItem12.Text = "View";
            // 
            // toolStripMenuItem13
            // 
            this.toolStripMenuItem13.Name = "toolStripMenuItem13";
            this.toolStripMenuItem13.ShortcutKeys = System.Windows.Forms.Keys.F8;
            this.toolStripMenuItem13.Size = new System.Drawing.Size(162, 22);
            this.toolStripMenuItem13.Text = "View bidding";
            this.toolStripMenuItem13.Click += new System.EventHandler(this.ViewAuctionClick);
            // 
            // toolStripMenuItemGoToBoard
            // 
            this.toolStripMenuItemGoToBoard.Name = "toolStripMenuItemGoToBoard";
            this.toolStripMenuItemGoToBoard.Size = new System.Drawing.Size(162, 22);
            this.toolStripMenuItemGoToBoard.Text = "Go To Board";
            this.toolStripMenuItemGoToBoard.Click += new System.EventHandler(this.ToolStripMenuItemGoToBoardClick);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem7,
            this.toolStripMenuItem10});
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(56, 20);
            this.toolStripMenuItem5.Text = "Shuffle";
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.ShortcutKeys = System.Windows.Forms.Keys.F3;
            this.toolStripMenuItem7.Size = new System.Drawing.Size(185, 22);
            this.toolStripMenuItem7.Text = "Shuffle new hand";
            this.toolStripMenuItem7.Click += new System.EventHandler(this.ButtonShuffleClick);
            // 
            // toolStripMenuItem10
            // 
            this.toolStripMenuItem10.Name = "toolStripMenuItem10";
            this.toolStripMenuItem10.ShortcutKeys = System.Windows.Forms.Keys.F6;
            this.toolStripMenuItem10.Size = new System.Drawing.Size(185, 22);
            this.toolStripMenuItem10.Text = "Restrictions";
            this.toolStripMenuItem10.Click += new System.EventHandler(this.ToolStripButton4Click);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem8,
            this.toolStripMenuItem9,
            this.toolStripMenuItemUseSolver,
            this.toolStripMenuItemOneBoard});
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(49, 20);
            this.toolStripMenuItem6.Text = "Batch";
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            this.toolStripMenuItem8.ShortcutKeys = System.Windows.Forms.Keys.F4;
            this.toolStripMenuItem8.Size = new System.Drawing.Size(234, 22);
            this.toolStripMenuItem8.Text = "Generate Hands";
            this.toolStripMenuItem8.Click += new System.EventHandler(this.ButtonGenerateHandsClick);
            // 
            // toolStripMenuItem9
            // 
            this.toolStripMenuItem9.Name = "toolStripMenuItem9";
            this.toolStripMenuItem9.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.toolStripMenuItem9.Size = new System.Drawing.Size(234, 22);
            this.toolStripMenuItem9.Text = "Batch bid generated hands";
            this.toolStripMenuItem9.Click += new System.EventHandler(this.ButtonBatchBiddingClickAsync);
            // 
            // toolStripMenuItemUseSolver
            // 
            this.toolStripMenuItemUseSolver.CheckOnClick = true;
            this.toolStripMenuItemUseSolver.Name = "toolStripMenuItemUseSolver";
            this.toolStripMenuItemUseSolver.Size = new System.Drawing.Size(234, 22);
            this.toolStripMenuItemUseSolver.Text = "Use Solver";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(37, 20);
            this.toolStripMenuItem1.Text = "File";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(56, 20);
            this.toolStripMenuItem2.Text = "Shuffle";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(49, 20);
            this.toolStripMenuItem3.Text = "Batch";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            this.toolStripSeparator1,
            this.toolStripButton2,
            this.toolStripButton3,
            this.toolStripButton4});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(730, 25);
            this.toolStrip1.TabIndex = 15;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton1.Text = "Shuffle";
            this.toolStripButton1.Click += new System.EventHandler(this.ButtonShuffleClick);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton2.Text = "Generate hands";
            this.toolStripButton2.Click += new System.EventHandler(this.ButtonGenerateHandsClick);
            // 
            // toolStripButton3
            // 
            this.toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton3.Image")));
            this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton3.Name = "toolStripButton3";
            this.toolStripButton3.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton3.Text = "Batch";
            this.toolStripButton3.Click += new System.EventHandler(this.ButtonBatchBiddingClickAsync);
            // 
            // toolStripButton4
            // 
            this.toolStripButton4.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton4.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton4.Image")));
            this.toolStripButton4.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton4.Name = "toolStripButton4";
            this.toolStripButton4.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton4.Text = "Restrictions";
            this.toolStripButton4.Click += new System.EventHandler(this.ToolStripButton4Click);
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.numericUpDown1.Location = new System.Drawing.Point(529, 487);
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(120, 23);
            this.numericUpDown1.TabIndex = 16;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(529, 461);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(134, 15);
            this.label1.TabIndex = 17;
            this.label1.Text = "Number of hands to bid";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "db3";
            this.openFileDialog1.FileName = "Tosr.db3";
            // 
            // panelNorth
            // 
            this.panelNorth.BackColor = System.Drawing.Color.Green;
            this.panelNorth.Location = new System.Drawing.Point(41, 77);
            this.panelNorth.Name = "panelNorth";
            this.panelNorth.Size = new System.Drawing.Size(311, 95);
            this.panelNorth.TabIndex = 18;
            // 
            // panelSouth
            // 
            this.panelSouth.BackColor = System.Drawing.Color.Green;
            this.panelSouth.Location = new System.Drawing.Point(41, 418);
            this.panelSouth.Name = "panelSouth";
            this.panelSouth.Size = new System.Drawing.Size(311, 95);
            this.panelSouth.TabIndex = 18;
            // 
            // statusStrip1
            // 
            this.statusStrip1.BackColor = System.Drawing.SystemColors.Control;
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 540);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(730, 22);
            this.statusStrip1.TabIndex = 19;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.DefaultExt = "pbn";
            // 
            // openFileDialog2
            // 
            this.openFileDialog2.DefaultExt = "pbn";
            this.openFileDialog2.FileName = "openFileDialog2";
            // 
            // toolStripMenuItemOneBoard
            // 
            this.toolStripMenuItemOneBoard.Name = "toolStripMenuItemOneBoard";
            this.toolStripMenuItemOneBoard.Size = new System.Drawing.Size(234, 22);
            this.toolStripMenuItemOneBoard.Text = "Batch bid one board";
            this.toolStripMenuItemOneBoard.Click += new System.EventHandler(this.ToolStripMenuItemOneBoardClick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Green;
            this.ClientSize = new System.Drawing.Size(730, 562);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.panelSouth);
            this.Controls.Add(this.panelNorth);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numericUpDown1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.buttonGetAuction);
            this.Controls.Add(this.menuStrip1);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "Form1";
            this.Text = "TOSR";
            this.Load += new System.EventHandler(this.Form1LoadAsync);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonGetAuction;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem8;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem9;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.ToolStripButton toolStripButton3;
        private System.Windows.Forms.ToolStripButton toolStripButton4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem10;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem11;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem12;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem13;
        private System.Windows.Forms.Panel panelNorth;
        private System.Windows.Forms.Panel panelSouth;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemUseSolver;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemLoadSet;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemSaveSet;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.OpenFileDialog openFileDialog2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemGoToBoard;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemOneBoard;
    }
}

