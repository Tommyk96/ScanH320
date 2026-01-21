using BoxAgr.BLL.Models;
using PharmaLegaсy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxAgr.BLL.Interfaces
{
    public interface IMainPage
    {
        public void SetStop(bool Checked);
        public void UpdateView();
        public void UpdateBoxView();
        public void AddLayer(int id, int layer,bool manualAdd, BoxAddStatus state, Unit[] barcodes, BoxWithLayers box);
        public void BlurImage();
        public void ShowEmptyMatrix();


    }
}
