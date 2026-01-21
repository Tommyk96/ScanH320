using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;

namespace AgrBox.data
{
    public class ScanCode
    {
        public ScanCode() { }
        public ScanCode(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            string[] fields = line.Split('\t');
            if (fields?.Length != 2)
                return;
            //char[] ch = fields[0].ToArray();
            LineId = fields[0];// (byte)ch[0];
            // Id = $"{PlcId:d3}";
            gs = new GsLabelData(fields[1]);

            GTIN = gs.GTIN;
            Sn = gs.SerialNumber;
            Field93 = gs.CryptoHash;
            FullNum = fields[1];
            timeStamp = DateTime.Now;
            CodeState = NumCodeState.Uncknow;
        }
        public ScanCode(string line, string plcId)
        {
            if (string.IsNullOrEmpty(line))
                return;


            GsLabelData gs = new GsLabelData(line);
            LineId = plcId;
            GTIN = gs.GTIN;
            Sn = gs.SerialNumber;
            Field93 = gs.CryptoHash;
            FullNum = line;
            timeStamp = DateTime.Now;
            CodeState = NumCodeState.Uncknow;
        }

        public byte PlcId { get; set; }
        public string LineId { get; set; }
        // public string Id { get; set; }
        public string GTIN { get; set; }
        public string Sn { get; set; }
        public string Field93 { get; set; }
        public string FullNum { get; set; }
        public NumCodeState CodeState { get; set; }
        public DateTime timeStamp { get; set; }
        public GsLabelData gs { get; set; }

        //public ItemPackDb GetItemPack()
        //{
        //    return new ItemPackDb() { CodeState = CodeState, Num = FullNum, timeStamp = timeStamp, LineId = LineId };
        //}
    }
}
