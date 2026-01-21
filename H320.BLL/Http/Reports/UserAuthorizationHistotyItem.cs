using System.Runtime.Serialization;

namespace BoxAgr.BLL.Http.Reports
{
    /// <summary>
    ///Класс, реализующий структуру массива пользователей 
    /// </summary>
    [DataContract]
    public class UserAuthorizationHistotyItem
    {
        /// <summary>
        /// Время входа в логин
        /// </summary>
        [DataMember]
        public string startTime { get; set; } = string.Empty;

        /// <summary>
        /// Время выхода из логина
        /// </summary>
        [DataMember]
        public string endTime { get; set; } = string.Empty;

        /// <summary>
        /// Указатель на пользователя
        /// </summary>
        [DataMember]
        public string id { get; set; } = string.Empty;
    }
}
