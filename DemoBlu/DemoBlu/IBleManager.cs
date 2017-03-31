using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBlu
{
    public interface IBleManager
    {
        bool IsBleEnabled();

        void GoToBleSettings();
    }
}
