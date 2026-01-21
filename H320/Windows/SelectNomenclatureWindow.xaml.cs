using BoxAgr.BLL.Models.Matrix;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace BoxAgr.Windows
{
    /// <summary>
    /// Логика взаимодействия для SelectNomenclatureWindow.xaml
    /// </summary>
    public partial class SelectNomenclatureWindow : Window
    {
        //private CatalogPage catalog = new CatalogPage();
        //public NomenclatureUnit SelectedUnit { get; set; } = new NomenclatureUnit();
        private const int MAIN_ERROR_CODE = 70000;
        private ObservableCollection<BoxMatrix> SelectedCatalog { get;} = new ();
        public BoxMatrix SelectedItem { get; set; } = new();

        private BoxMatrixCatalog boxMatrixCatalog;
        private BoxMatrixCatalog BoxMatrixCatalog
        {
            get
            {
                boxMatrixCatalog ??= BoxMatrixCatalog.Load();
                return boxMatrixCatalog;
            }
        }
        public SelectNomenclatureWindow()
        {
            InitializeComponent();
            DataContext = this;

            foreach (BoxMatrix b in BoxMatrixCatalog.Catalog)
                SelectedCatalog.Add(b);
            lvNomenclatureList.ItemsSource = SelectedCatalog;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        //btn OK
        private void btnSelectNomenclature_Click(object sender, RoutedEventArgs e)
        {
            //if (string.IsNullOrEmpty(catalog.SelectedUnit?.gtin))
            //{
            //    App.ShowMessageOnUpBanner("SNU", "Продукт не выбран!\nВыберите продукт и повторите попытку", Util.EventLogEntryType.Error, MAIN_ERROR_CODE + 121);
            //    return;
            //}

            //SelectedUnit.CopyFrom(catalog.SelectedUnit);
            //if (lvNomenclatureList.SelectedItem is BoxMatrix b)
            //{
            //    SelectedItem = b;
            //    DialogResult = true;
            //    Close();
            //}
            //else
            //{
            //    return;
            //}

            BoxMatrixCatalog.Save();
            DialogResult = true;
            Close();

        }

        private void btnCancelSelect_Click(object sender, RoutedEventArgs e)
        {

            DialogResult = false;
            Close();

        }

        private void lvNomenclatureList_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void lvNomenclatureList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            BoxMatrix unit = new();
            AddBoxMatrixWindow w = new(unit);
            w.Owner = App.MainForm;

            if (w.ShowDialog() == true)
            {
                //if (Windows.MessageBoxEtx.ShowExt(App.MainForm, $"Вы действительно хотите сохранить изменения номенклатуры {w.Unit?.name} ?", Windows.MessageBoxExButton.YesNo) == MessageBoxResult.No)
                //    return;

                SelectedItem.CopyFrom(w.Unit);

                //обновить данные в каталоге
                if (BoxMatrixCatalog.UpdateOrAddNomenclatureUnit(w.Unit))
                    ;// BoxMatrixCatalog.Save();

                unit.CopyFrom(SelectedItem);

                SelectedCatalog.Clear();

                foreach(BoxMatrix b in BoxMatrixCatalog.Catalog)
                    SelectedCatalog.Add(b);

                System.ComponentModel.ICollectionView view = CollectionViewSource.GetDefaultView(lvNomenclatureList.ItemsSource);
                view.Refresh();


            }
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {

        }
        /// <summary>
        /// Get the object from the selected listview item.
        /// </summary>
        /// <param name="LV"></param>
        /// <param name="originalSource"></param>
        /// <returns></returns>
        private object GetListViewItemObject(ListView LV, object originalSource)
        {
            DependencyObject dep = (DependencyObject)originalSource;
            while ((dep != null) && !(dep.GetType() == typeof(ListViewItem)))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep == null)
                return null;
            object obj = (Object)LV.ItemContainerGenerator.ItemFromContainer(dep);
            return obj;
        }
        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            object obj = GetListViewItemObject(lvNomenclatureList, e.OriginalSource);
            if (obj == null)
                return;

            if (obj is BoxMatrix delItem)
            {
                //вывести подтверждение
                if (MessageBox.Show("Вы действительно хотите удалить шаблон " + delItem.Name + " ?", "Подтвердите удаление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {

                    if (BoxMatrixCatalog.DeleteNomenclature(delItem))
                        ;// BoxMatrixCatalog.Save();
                    //обновить данные 
                    SelectedCatalog.Remove(delItem);
                 
                }
            }
        }
    }
}
