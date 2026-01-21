using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PharmaLegacy.Windows
{
    /// <summary>
    /// Логика взаимодействия для WaitWindow1.xaml
    /// </summary>
    public partial class WaitWindow1 : Window
    {
        public Action Worker { get; set; }
        private CancellationTokenSource CancelTokenSource { get; set; }
        private CancellationToken token;

        public WaitWindow1(Action worker, CancellationTokenSource cancelTokenSource)
        {
            InitializeComponent();
            Worker = worker ?? throw new ArgumentNullException(nameof(worker));
            CancelTokenSource = cancelTokenSource ?? throw new ArgumentNullException(nameof(cancelTokenSource));
            token = CancelTokenSource.Token;
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            // Task.Factory.
            CancelTokenSource.Cancel();
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(Worker).ContinueWith(t => { this.Close(); },  TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
