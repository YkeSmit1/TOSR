using Common;
using Solver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Common.Tosr;

namespace Wpf.Tosr
{
    /// <summary>
    /// Interaction logic for ShuffleRestrictionsWindow.xaml
    /// </summary>
    public partial class ShuffleRestrictionsWindow : Window
    {

        private readonly ShufflingDeal shufflingDeal;
        public ShufflingDeal ShufflingDeal => shufflingDeal;

        public ShuffleRestrictionsWindow(ShufflingDeal shufflingDeal)
        {
            InitializeComponent();
            this.shufflingDeal = shufflingDeal;
            ObjectToForm();
        }

        private void ObjectToForm()
        {
            checkBoxControls.IsChecked = shufflingDeal.South.Controls != null;
            checkBoxShape.IsChecked = shufflingDeal.South.Shape != null;
            if (checkBoxControls.IsChecked.Value)
                numericUpDownControls.Value = shufflingDeal.South.Controls.Min;
            if (checkBoxShape.IsChecked.Value)
                textBoxShape.Text = shufflingDeal.South.Shape;
        }
        private void FormToObject()
        {
            shufflingDeal.South.Controls = checkBoxControls.IsChecked.Value ? new MinMax { Min = (int)numericUpDownControls.Value, Max = (int)numericUpDownControls.Value } : null;
            shufflingDeal.South.Shape = checkBoxShape.IsChecked.Value ? textBoxShape.Text : null;
        }

        private bool Validate()
        {
            if (!checkBoxShape.IsChecked.Value)
                return true;
            if (!textBoxShape.Text.All(x => char.IsDigit(x)))
                return HandleError("Shape should be all digits");
            else
            if (textBoxShape.Text.Length != 4)
                return HandleError("Shape should be length 4");
            else
            if (textBoxShape.Text.ToCharArray().Select(x => int.Parse(x.ToString())).Sum() != 13)
                return HandleError("Sum of suit lengths should be 13");
            else
            if (UtilTosr.IsFreakHand(textBoxShape.Text.ToCharArray().ToList().Select(x => int.Parse(x.ToString()))))
                return HandleError("Cannot bid a freak-hand");
            else
            {
                return true;
            }

            static bool HandleError(string error)
            {
                MessageBox.Show(error, "Error");
                return false;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (Validate())
            {
                DialogResult = true;
                FormToObject();
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
