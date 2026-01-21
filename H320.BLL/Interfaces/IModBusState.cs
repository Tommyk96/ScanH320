namespace BoxAgr.BLL.Interfaces
{
    public interface IModBusState
    {
        public bool Power { get; set; }
        public bool IsBoxSet { get; set; }
        public string Diagnostic {get; set;}
        public bool ClientConnected { get; set; }
    }
}
