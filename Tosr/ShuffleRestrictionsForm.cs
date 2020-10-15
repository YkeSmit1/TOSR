using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Common;

namespace Tosr
{
    public partial class ShuffleRestrictionsForm : Form
    {
        private readonly ShuffleRestrictions shuffleRestrictions;
        public ShuffleRestrictions ShuffleRestrictions => shuffleRestrictions;

        public ShuffleRestrictionsForm(ShuffleRestrictions shuffleRestrictions)
        {
            InitializeComponent();
            this.shuffleRestrictions = shuffleRestrictions;
            ObjectToForm(shuffleRestrictions);
        }

        private void ShuffleRestrictionsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            FormToObject();
        }
        private void ObjectToForm(ShuffleRestrictions shuffleRestrictions)
        {
            checkBoxControls.Checked = shuffleRestrictions.restrictControls;
            checkBoxShape.Checked = shuffleRestrictions.restrictShape;
            numericUpDownControls.Value = shuffleRestrictions.minControls;
            textBoxShape.Text = shuffleRestrictions.shape;
        }
        private void FormToObject()
        {
            shuffleRestrictions.restrictControls = checkBoxControls.Checked;
            shuffleRestrictions.restrictShape = checkBoxShape.Checked;
            shuffleRestrictions.minControls = (int)numericUpDownControls.Value;
            shuffleRestrictions.maxControls = (int)numericUpDownControls.Value;
            shuffleRestrictions.shape = textBoxShape.Text;
        }

    }
}
