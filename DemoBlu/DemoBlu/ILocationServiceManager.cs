using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBlu
{
    public interface ILocationServiceManager
    {
        bool IsGpsEnabled();

        void GoToGpsSettings();
    }
}
