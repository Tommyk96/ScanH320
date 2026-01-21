using PharmaLegaсy.Interfaces;
using System.Windows;
using System.Windows.Controls;

//Серьезность Код Описание Проект  Файл Строка  Состояние подавления
//Ошибка Имя "PageBase" не существует в пространстве имен "clr-namespace:FarmaBoxAgg.Pages".	FarmaBoxAgg D:\ORDER\InfPrj\Farma\FarmaOSR\FarmaBoxAgg\FarmaBoxAgg\Pages\MainPage.xaml	1	

namespace PharmaLegacy.Pages
{
    public class PageBase: Page
    {
        public string pageId;
        protected ShowType type;
        protected IMainFrame owner;

        public data.BaseObj SelectedItem { get; set; }

        public static readonly RoutedEvent BackClickEvent = EventManager.RegisterRoutedEvent(
           "BackClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PageBase));

        // Provide CLR accessors for the event
        public event RoutedEventHandler BackClick
        {
            add { AddHandler(BackClickEvent, value); }
            remove { RemoveHandler(BackClickEvent, value); }
        }

        // This method raises the Tap event
        protected void RaiseBackClickEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(PageBase.BackClickEvent);
            RaiseEvent(newEventArgs);
        }

        public virtual bool InitData() {return true; }
        public virtual void InitData(IMainFrame ownerPointer, ShowType type) {  }

        public PageBase(IMainFrame o, Pages.ShowType t)
        { owner = o;type = t; }
    }


    public enum ShowType
    {
        Edit = 0,
        Select = 1
    }
}
