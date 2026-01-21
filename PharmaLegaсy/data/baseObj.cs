using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaLegacy.data
{

    public class BaseObj : ICloneable, INotifyPropertyChanged
    {
        [Category("Information")]
        [DisplayName("Наименование")]
        [Description("This property uses a TextBox as the default editor.")]
        public string Name { get; set; }
        [DisplayName("Описание")]
        public string Description { get; set; }
        [DisplayName("ID ")]
        public decimal Id { get { return id; } }// set { id = value; } }

        protected decimal id;

        public BaseObj()
        {
            Name = "";
            Description = "";
            id = -1;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public void SetId(decimal ID)
        { id = ID; }

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }

        #endregion
    }

}
