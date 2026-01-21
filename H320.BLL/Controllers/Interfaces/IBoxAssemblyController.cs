using BoxAgr.BLL.Events;
using BoxAgr.BLL.Models;

namespace BoxAgr.BLL.Controllers.Interfaces
{
    public interface IBoxAssemblyController
    {

        BoxWithLayers cBox { get; set; }
        bool ContiniusMode { get; }
        int MaxNoReadInline { get; set; }
        bool ScanEnable { get; set; }

        event AddLayerEvent? AddLayer;
        event MaxNoReadInlineStateEvent? MaxNoReadInlineState;
        event SessionStateEvent? StatusChange;

        bool AddSingleCodeToLayer(string data);
        bool RemoveUnitFromBox(string fullNumber);
        bool RemoveUnitFromLayer(string fullNumber);
        bool ReplaceNumInBox(string removeNum, string newfullNumber);
        void Start();
        void StopCycle();
    }
}