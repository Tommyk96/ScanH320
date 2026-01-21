using PharmaLegaсy.Interfaces;
using System;
using System.Windows;
using System.Windows.Input;

namespace PharmaLegacy.Windows
{
    /// <summary>
    /// Логика взаимодействия для HostWindow.xaml
    /// </summary>
    public partial class HostWindow : Window
    {
        private IMainFrame owner;
        private Pages.ShowType sType;
        private Pages.PageBase page;

        public data.BaseObj SelectItem;

        public HostWindow(IMainFrame o, Pages.ShowType st,Type t,data.BaseObj data)
        {
            owner = o;
            sType = st;
            InitializeComponent();

            page = (Pages.PageBase)System.Activator.CreateInstance(t);
            page.BackClick += Page_BackClick;
            page.SelectedItem = data;
            page.InitData(owner, sType);

            frame1.Navigate(page);
        }

        private void Page_BackClick(object sender, RoutedEventArgs e)
        {
            if (page.SelectedItem != null)
                DialogResult = true;
            else
                DialogResult = false;

            SelectItem = page.SelectedItem;
            Close();
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
