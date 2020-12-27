using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Tosr
{
    public partial class GoToBoardForm : Form
    {
        public int BoardNumber { get; set; } = 1;

        public GoToBoardForm(int maxBoardNumber)
        {
            InitializeComponent();
            numericUpDown1.Maximum = maxBoardNumber;
        }

        private void GoToBoardFormFormClosed(object sender, FormClosedEventArgs e)
        {
            BoardNumber = (int)numericUpDown1.Value;
        }
    }
}
