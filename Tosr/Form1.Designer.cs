﻿namespace Tosr
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
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
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
            this.buttonGetAuction.Text = "GetAuction";
            this.buttonGetAuction.UseVisualStyleBackColor = true;
            this.buttonGetAuction.Click += new System.EventHandler(this.buttonGetAuctionClick);
            // 
            // buttonBatchBidding
            // 
            this.buttonBatchBidding.Location = new System.Drawing.Point(174, 416);
            this.buttonBatchBidding.Name = "buttonBatchBidding";
            this.buttonBatchBidding.Size = new System.Drawing.Size(101, 23);
            this.buttonBatchBidding.TabIndex = 3;
            this.buttonBatchBidding.Text = "Batch bidding";
            this.buttonBatchBidding.UseVisualStyleBackColor = true;
            this.buttonBatchBidding.Click += new System.EventHandler(this.buttonBatchBiddingClick);
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(295, 419);
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
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Green;
            this.ClientSize = new System.Drawing.Size(800, 456);
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
    }
}

