using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace BoxAgr.BLL.Http.Jobs
{
    public class AggregationOrder   
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

        private DateTime _expDate;
        [Required]
        public string expDate { 
            get => _expDate.ToShortDateString();
            set {
                _expDate = DateTime.Parse(value);   
            }
        }

        [Required]
        public string formatExpDate { get; set; } = string.Empty;

        [Required]
        public string formatManufactureDate { get; set; } = string.Empty;

        private DateTime _manufactureDate;
        [Required]
        public string ManufactureDate
        {
            get => _manufactureDate.ToShortDateString();
            set
            {
                _manufactureDate = DateTime.Parse(value);
            }
        }
        


        [Required]
        public string lineNum { get; set; } = string.Empty;

        public bool lineComplited { get; set; }  

        [Required]
        public string urlLabelBoxTemplate { get; set; } = string.Empty;

        [Required]
        public int numLabelAtBox { get; set; }

        [Required]
        public int numРacksInBox { get; set; }

        [Required]
        public int numLayersInBox { get; set; }

        [Required]
        public int numBoxInPallet { get; set; }

        [Required]
        public int numPacksInSeries { get; set; }


        [DataMember]
        public List<string> productNumbers { get; set; } = [];
        [DataMember]
        public List<string> boxNumbers { get; set; } = [];

    }
}
