using BoxAgr.BLL.Models.Matrix;
using System.Windows;
using System.Windows.Input;

namespace BoxAgr.Windows
{
    /// <summary>
    /// Логика взаимодействия для AddBoxMatrixWindow.xaml
    /// </summary>
    public partial class AddBoxMatrixWindow : Window
    {
        public BoxMatrix Unit { get; }
        public AddBoxMatrixWindow(BoxMatrix u)
        {
            Unit = u;
            InitializeComponent();
            DataContext = Unit;
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
