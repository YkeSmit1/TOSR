namespace Tosr
{
    partial class ShuffleRestrictionsForm
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
            this.checkBoxControls = new System.Windows.Forms.CheckBox();
            this.numericUpDownControls = new System.Windows.Forms.NumericUpDown();
            this.checkBoxShape = new System.Windows.Forms.CheckBox();
            this.textBoxShape = new System.Windows.Forms.TextBox();
            this.buttonOk = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownControls)).BeginInit();
            this.SuspendLayout();
            // 
            // checkBoxControls
            // 
            this.checkBoxControls.AutoSize = true;
            this.checkBoxControls.Location = new System.Drawing.Point(13, 12);
            this.checkBoxControls.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.checkBoxControls.Name = "checkBoxControls";
            this.checkBoxControls.Size = new System.Drawing.Size(71, 19);
            this.checkBoxControls.TabIndex = 1;
            this.checkBoxControls.Text = "Controls";
            this.checkBoxControls.UseVisualStyleBackColor = true;
            // 
            // numericUpDownControls
            // 
            this.numericUpDownControls.Location = new System.Drawing.Point(13, 39);
            this.numericUpDownControls.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.numericUpDownControls.Name = "numericUpDownControls";
            this.numericUpDownControls.Size = new System.Drawing.Size(140, 23);
            this.numericUpDownControls.TabIndex = 2;
            // 
            // checkBoxShape
            // 
            this.checkBoxShape.AutoSize = true;
            this.checkBoxShape.Location = new System.Drawing.Point(13, 85);
            this.checkBoxShape.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.checkBoxShape.Name = "checkBoxShape";
            this.checkBoxShape.Size = new System.Drawing.Size(58, 19);
            this.checkBoxShape.TabIndex = 3;
            this.checkBoxShape.Text = "Shape";
            this.checkBoxShape.UseVisualStyleBackColor = true;
            // 
            // textBoxShape
            // 
            this.textBoxShape.Location = new System.Drawing.Point(13, 111);
            this.textBoxShape.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxShape.Name = "textBoxShape";
            this.textBoxShape.Size = new System.Drawing.Size(116, 23);
            this.textBoxShape.TabIndex = 4;
            this.textBoxShape.Text = "5422";
            // 
            // buttonOk
            // 
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOk.Location = new System.Drawing.Point(324, 161);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 5;
            this.buttonOk.Text = "Ok";
            this.buttonOk.UseVisualStyleBackColor = true;
            // 
            // ShuffleRestrictionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(412, 196);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.textBoxShape);
            this.Controls.Add(this.checkBoxShape);
            this.Controls.Add(this.numericUpDownControls);
            this.Controls.Add(this.checkBoxControls);
            this.Name = "ShuffleRestrictionsForm";
            this.Text = "Shuffle Restrictions";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ShuffleRestrictionsForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownControls)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxControls;
        private System.Windows.Forms.NumericUpDown numericUpDownControls;
        private System.Windows.Forms.CheckBox checkBoxShape;
        private System.Windows.Forms.TextBox textBoxShape;
        private System.Windows.Forms.Button buttonOk;
    }
}