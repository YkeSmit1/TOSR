using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Common;
using Solver;

namespace Tosr
{
    public partial class ShuffleRestrictionsForm : Form
    {
        private readonly ShufflingDeal shufflingDeal;
        public ShufflingDeal ShufflingDeal => shufflingDeal;

        public ShuffleRestrictionsForm(ShufflingDeal shufflingDeal)
        {
            InitializeComponent();
            this.shufflingDeal = shufflingDeal;
            ObjectToForm();
        }

        private void ShuffleRestrictionsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            FormToObject();
        }
        private void ObjectToForm()
        {
            checkBoxControls.Checked = shufflingDeal.South.Controls != null;
            checkBoxShape.Checked = shufflingDeal.South.Shape != null;
            if (checkBoxControls.Checked)
                numericUpDownControls.Value = shufflingDeal.South.Controls.Min;
            if (checkBoxShape.Checked)
                textBoxShape.Text = shufflingDeal.South.Shape;
        }
        private void FormToObject()
        {
            shufflingDeal.South.Controls = checkBoxControls.Checked ? new MinMax { Min = (int)numericUpDownControls.Value, Max = (int)numericUpDownControls.Value } : null;
            shufflingDeal.South.Shape = checkBoxShape.Checked ? textBoxShape.Text : null;
        }

        private void TextBoxShapeValidating(object sender, CancelEventArgs e)
        {
            if (!checkBoxShape.Checked)
                return;
            if (!textBoxShape.Text.All(x => char.IsDigit(x)))
                HandleError("Shape should be all digits");
            else
            if (textBoxShape.Text.Length != 4)
                HandleError("Shape should be length 4");
            else
            if (textBoxShape.Text.ToCharArray().Select(x => int.Parse(x.ToString())).Sum() != 13)
                HandleError("Sum of suit lengths should be 13");
            else
            if (Util.IsFreakHand(textBoxShape.Text.ToCharArray().ToList().Select(x => int.Parse(x.ToString()))))
                HandleError("Cannot bid a freak-hand");
            else
            {
                e.Cancel = false;
                errorProvider1.SetError(textBoxShape, "");
            }

            void HandleError(string error)
            {
                e.Cancel = true;
                textBoxShape.Focus();
                errorProvider1.SetError(textBoxShape, error);
            }
        }

        private void ButtonOkClick(object sender, EventArgs e)
        {
            if (!ValidateChildren())
                DialogResult = DialogResult.None;
        }
    }
}
