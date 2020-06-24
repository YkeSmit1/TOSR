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
            this.buttonShuffle = new System.Windows.Forms.Button();
            this.buttonClearAuction = new System.Windows.Forms.Button();
            this.buttonGetAuction = new System.Windows.Forms.Button();
            this.buttonBatchBidding = new System.Windows.Forms.Button();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonGenerateHands = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonShuffle
            // 
            this.buttonShuffle.Location = new System.Drawing.Point(26, 358);
            this.buttonShuffle.Name = "buttonShuffle";
            this.buttonShuffle.Size = new System.Drawing.Size(90, 23);
            this.buttonShuffle.TabIndex = 0;
            this.buttonShuffle.Text = "Shuffle";
            this.buttonShuffle.UseVisualStyleBackColor = true;
            this.buttonShuffle.Click += new System.EventHandler(this.buttonShuffleClick);
            // 
            // buttonClearAuction
            // 
            this.buttonClearAuction.Location = new System.Drawing.Point(26, 387);
            this.buttonClearAuction.Name = "buttonClearAuction";
            this.buttonClearAuction.Size = new System.Drawing.Size(90, 23);
            this.buttonClearAuction.TabIndex = 1;
            this.buttonClearAuction.Text = "Clear Auction";
            this.buttonClearAuction.UseVisualStyleBackColor = true;
            this.buttonClearAuction.Click += new System.EventHandler(this.buttonClearAuctionClick);
            // 
            // buttonGetAuction
            // 
            this.buttonGetAuction.Location = new System.Drawing.Point(26, 416);
            this.buttonGetAuction.Name = "buttonGetAuction";
            this.buttonGetAuction.Size = new System.Drawing.Size(90, 23);
            this.buttonGetAuction.TabIndex = 2;
            this.buttonGetAuction.Text = "Get Auction";
            this.buttonGetAuction.UseVisualStyleBackColor = true;
            this.buttonGetAuction.Click += new System.EventHandler(this.buttonGetAuctionClick);
            // 
            // buttonBatchBidding
            // 
            this.buttonBatchBidding.Location = new System.Drawing.Point(174, 416);
            this.buttonBatchBidding.Name = "buttonBatchBidding";
            this.buttonBatchBidding.Size = new System.Drawing.Size(152, 23);
            this.buttonBatchBidding.TabIndex = 3;
            this.buttonBatchBidding.Text = "Batch bid generated hands";
            this.buttonBatchBidding.UseVisualStyleBackColor = true;
            this.buttonBatchBidding.Click += new System.EventHandler(this.buttonBatchBiddingClick);
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(356, 416);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(120, 20);
            this.numericUpDown1.TabIndex = 4;
            this.numericUpDown1.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // listBox1
            // 
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(535, 310);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(241, 134);
            this.listBox1.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(535, 291);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Errors";
            // 
            // buttonGenerateHands
            // 
            this.buttonGenerateHands.Location = new System.Drawing.Point(174, 387);
            this.buttonGenerateHands.Name = "buttonGenerateHands";
            this.buttonGenerateHands.Size = new System.Drawing.Size(152, 23);
            this.buttonGenerateHands.TabIndex = 7;
            this.buttonGenerateHands.Text = "Generate Hands";
            this.buttonGenerateHands.UseVisualStyleBackColor = true;
            this.buttonGenerateHands.Click += new System.EventHandler(this.buttonGenerateHandsClick);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(356, 373);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 8;
            this.textBox1.Text = "5422";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(356, 350);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(57, 17);
            this.checkBox1.TabIndex = 10;
            this.checkBox1.Text = "Shape";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // numericUpDown2
            // 
            this.numericUpDown2.Location = new System.Drawing.Point(356, 310);
            this.numericUpDown2.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown2.Name = "numericUpDown2";
            this.numericUpDown2.Size = new System.Drawing.Size(120, 20);
            this.numericUpDown2.TabIndex = 11;
            this.numericUpDown2.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Checked = true;
            this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox2.Location = new System.Drawing.Point(356, 287);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(64, 17);
            this.checkBox2.TabIndex = 12;
            this.checkBox2.Text = "Controls";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Green;
            this.ClientSize = new System.Drawing.Size(800, 456);
            this.Controls.Add(this.checkBox2);
            this.Controls.Add(this.numericUpDown2);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.buttonGenerateHands);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.numericUpDown1);
            this.Controls.Add(this.buttonBatchBidding);
            this.Controls.Add(this.buttonGetAuction);
            this.Controls.Add(this.buttonClearAuction);
            this.Controls.Add(this.buttonShuffle);
            this.Name = "Form1";
            this.Text = "TOSR";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonShuffle;
        private System.Windows.Forms.Button buttonClearAuction;
        private System.Windows.Forms.Button buttonGetAuction;
        private System.Windows.Forms.Button buttonBatchBidding;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonGenerateHands;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.NumericUpDown numericUpDown2;
        private System.Windows.Forms.CheckBox checkBox2;
    }
}

