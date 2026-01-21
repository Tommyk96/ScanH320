using BoxAgr.BLL.Events;
using System.Threading;
using System.Threading.Tasks;

namespace BoxAgr.BLL.Interfaces
{
    public interface IBusControl
    {
        // Input 0 is true
        public event BoxInPositionHandler? BoxInPosition;
        // Input 1 is false
        public event PowerLossHandler? PowerLoss;
        //
        public event SessionStateEvent? StatusChange;

        public bool IsBoxInPosition { get; }
        public bool IsPowerLoss { get; }
        public bool IsGreenLightActive { get; }
        //
        public void Start(CancellationToken cancelationToken);

        //Write output 0 to true
        public Task<bool> StartScan();

        //Write output 1 to true
        public Task<bool> StartGreenSpot();

        //Write output 2 to true
        public Task<bool> StartRedBlink();


        public Task<bool> OnGreenLight();
        public Task<bool> OffGreenLight();

        public Task<bool> OnRedLight();
        public Task<bool> OffRedLight();
    }
}
