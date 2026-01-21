using FSerialization;
using System.Runtime.Serialization;

namespace PharmaLegaсy.Interfaces
{
    public interface IJob
    {
       
        public JobStates JobState { get; set; }
        public int numРacksInBox { get; set; }
        public int numLayersInBox { get; set; }
        public int numPacksInLayer { get; set; }
        public string GTIN { get; set; }
        public NewPartAggregate1СOrder? order1C { get; set; }

        public string AcceptOrderToWork(NewPartAggregate1СOrder o, IConfig settings);
        public string AcceptSerializeOrderToWork(object o, IConfig settings);
        public bool SaveOrder();
        public int GetVerufyQueueSize();
    }
}
