using FSerialization;
using SQLite;
using System;

namespace BoxAgr.DAL.Models
{

    
    public record SqlBox
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
      
        public long ScanId { get; set; }

        [Indexed(Name = "NumGS1codeIndx", Unique = true)]
        [MaxLength(40)]
        [NotNull]  
        public string Num { get; set; } = "";

        [Indexed(Name = "BoxNumIndx", Unique = false)]
        [MaxLength(40)]
        public string BoxNum { get; set; } = null!;

        public CodeState CodeState { get; set; }

        public DateTime CreatedTime { get; set; }

    }
}
