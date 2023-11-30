using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    internal class PhidgetsPort
    {
        public PhidgetsInput[] input;
        public PhidgestOutput[] output;

        public PhidgetsPort() {
          input = new PhidgetsInput[16];
          output = new PhidgestOutput[16];
        }
    }
}
