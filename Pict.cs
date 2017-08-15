using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Saraff.Twain {

    internal sealed class Pict:_ImageHandler {

        protected override int GetSize() {
            throw new NotImplementedException();
        }

        protected override int BufferSize {
            get {
                return 256 * 1024; //256K
            }
        }
    }
}
