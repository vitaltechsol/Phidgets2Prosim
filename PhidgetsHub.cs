using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    internal class PhidgetsHub
    {
        public PhidgetsPort[] port;

        public PhidgetsHub() {

            port = new PhidgetsPort[6];
            for (int i = 0; i < 6; i++)
            {
                port[i] = new PhidgetsPort();
            }
        }
    }
}
