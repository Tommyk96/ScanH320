using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Utilite.Slutsk
{
    [DataContract]
    public class Report
    {
        [DataMember]
        public string Type { get; set; } = "Mark";

        [DataMember]
        public List<MarkListNoAgr> MarkList { get; set; } = new List<MarkListNoAgr> { };
        //[DataMember]
        //public MarkListNoAgr  MarkList { get; set; }
    }

    [DataContract]
    public class ReportSlutsk
    {
        [DataMember]
        public string Type { get; set; } = "Mark";

        [DataMember] 
        public List<MarkList> MarkList { get; set; } = new List<MarkList> { };
    }

    [DataContract]
    public class MarkList
    {
        [DataMember]
        public string DocId { get; set; } = "";
        [DataMember]
        public string StartDate { get; set; } = "";//"2021-12-13"
        [DataMember]
        public string EndDate { get; set; } = "";//021-12-13"
        [DataMember]
        public string Gtin { get; set; } = "";
        [DataMember]
        public string ManufactureDate { get; set; } = "";// "2021-12-13"
        [DataMember]
        public string BBD { get; set; } = "";//"2022-02-16",
        [DataMember]
        public int Batch { get; set; }
        [DataMember]
        public string OrderIdentifier { get; set; } = "";
        [DataMember]
        public List<UnitS> Units { get; set; } = new List<UnitS> { };
    }

    [DataContract]
    public class MarkListNoAgr
    {
        [DataMember]
        public string DocId { get; set; } = "";
        [DataMember]
        public string StartDate { get; set; } = "";//"2021-12-13"
        [DataMember]
        public string EndDate { get; set; } = "";//021-12-13"
        [DataMember]
        public string Gtin { get; set; } = "";
        [DataMember]
        public string ManufactureDate { get; set; } = "";// "2021-12-13"
        [DataMember]
        public string BBD { get; set; } = "";//"2022-02-16",
        [DataMember]
        public int Batch { get; set; }
        [DataMember]
        public string OrderIdentifier { get; set; } = "";
        [DataMember]
        public List<string> Units { get; set; } = new List<string> { };
    }

    [DataContract]
    public class UnitS
    {
        [DataMember]
        public Levels Level { get; set; }
        [DataMember]
        public string Aggregate { get; set; } //"010481072901102931030040001003\u001d11211213210001"
        [DataMember]
        public List<string> Labels { get; set; } = new List<string> { };
        
    }

    public enum Levels 
    {
        NoAgr = 0,
        Box = 1,
        Pallete = 2
    }
}

//
