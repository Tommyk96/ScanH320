using FluentFTP;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BoxAgr.BLL.Http.Jobs
{
    public class S2Job
    {

        // Должен быть строкой, содержащей GUID
        [Required]
        [RegularExpression(@"^[{(]?[0-9A-Fa-f]{8}[-]?[0-9A-Fa-f]{4}[-]?[0-9A-Fa-f]{4}[-]?[0-9A-Fa-f]{4}[-]?[0-9A-Fa-f]{12}[)}]?$", ErrorMessage = "Id must be a valid GUID.")]
        public string id { get; set; } = string.Empty;

        // Должен быть 14 символов
        [Required]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "Gtin must be exactly 14 characters long.")]
        public string gtin { get; set; } = string.Empty;

        // Не может быть пустым
        [Required]
        public string lotNo { get; set; } = string.Empty;

        // Должен быть строкой, содержащей число типа int
        //[Required]
        //[RegularExpression(@"^\d+$", ErrorMessage = "LineNum must be a valid integer.")]
        //public string LineNum { get; set; } = string.Empty;

        // Не обязателен. Может отсутствовать
        public int numPacksInBox { get; set; }

        // Не может быть пустым
        [Required]
        public string productName { get; set; } = string.Empty;

        // Не обязателен
        public int numPacksInSeries { get; set; }
    }
}
