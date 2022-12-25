using Solver;
using System.Linq;
using System.Windows;
using Common.Tosr;

namespace Wpf.Tosr
{
    /// <summary>
    /// Interaction logic for ShuffleRestrictionsWindow.xaml
    /// </summary>
    public partial class ShuffleRestrictionsWindow
    {
        private readonly ShufflingDeal shufflingDeal;

        public ShuffleRestrictionsWindow(ShufflingDeal shufflingDeal)
        {
            InitializeComponent();
            this.shufflingDeal = shufflingDeal;
            ObjectToForm();
        }

        private void ObjectToForm()
        {
            CheckBoxControls.IsChecked = shufflingDeal.South.Controls != null;
            CheckBoxShape.IsChecked = shufflingDeal.South.Shape != null;
            if (CheckBoxControls.IsChecked.GetValueOrDefault())
                NumericUpDownControls.Value = shufflingDeal.South.Controls?.Min;
            if (CheckBoxShape.IsChecked.GetValueOrDefault())
                TextBoxShape.Text = shufflingDeal.South.Shape ?? string.Empty;
        }
        private void FormToObject()
        {
            shufflingDeal.South.Controls = CheckBoxControls.IsChecked.GetValueOrDefault()
                ? new MinMax { Min = NumericUpDownControls.Value.GetValueOrDefault(), Max = NumericUpDownControls.Value.GetValueOrDefault() } : null;
            shufflingDeal.South.Shape = CheckBoxShape.IsChecked.GetValueOrDefault() ? TextBoxShape.Text : null;
        }

        private bool Validate()
        {
            if (!CheckBoxShape.IsChecked.GetValueOrDefault())
                return true;
            if (!TextBoxShape.Text.All(char.IsDigit))
                return HandleError("Shape should be all digits");
            else
            if (TextBoxShape.Text.Length != 4)
                return HandleError("Shape should be length 4");
            else
            if (TextBoxShape.Text.ToCharArray().Select(x => int.Parse(x.ToString())).Sum() != 13)
                return HandleError("Sum of suit lengths should be 13");
            else
            if (UtilTosr.IsFreakHand(TextBoxShape.Text.ToCharArray().ToList().Select(x => int.Parse(x.ToString()))))
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
