using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Saraff.Twain {

    internal sealed class Pict : _ImageHandler {

        /// <summary>
        /// Gets the size of a image data.
        /// </summary>
        /// <returns>
        /// Size of a image data.
        /// </returns>
        protected override int GetSize() => throw new NotImplementedException();

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <value>
        /// The size of the buffer.
        /// </value>
        protected override int BufferSize => 256 * 1024; //256K
    }
}
