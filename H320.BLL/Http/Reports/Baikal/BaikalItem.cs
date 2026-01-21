using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BoxAgr.BLL.Http.Reports.Baikal
{
    [DataContract]
    public class BaikalItem
    {
        /// <summary>
        /// Тип объекта: 0 - продукт, 1 - короб, 2 - палета
        /// </summary>
        [DataMember]
        public int type { get; set; }

        /// <summary>
        /// Номер объекта из задания или составленный самостоятельго (ШК)
        /// </summary>
        [DataMember]
        public string num { get; set; } = string.Empty;

        /// <summary>
        /// Время выпуска объекта
        /// </summary>
        [DataMember]
        public string time { get; set; } = string.Empty;

        /// <summary>
        /// Вложенный список объектов items
        /// </summary>
        [DataMember]
        public List<BaikalItem> items { get; set; } = [];


        public BaikalItem() { }
        //public BaikalItem(Item x)
        //{
        //    if (int.TryParse(x.type, out int t))
        //        type = t;

        //    num = x.num;
        //    time = x.time;

        //    if (x.items.Count > 0)
        //        items.AddRange(x.items.Select(x => new BaikalItem(x)));
        //}
    }
}
