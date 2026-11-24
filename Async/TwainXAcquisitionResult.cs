using System;
using System.Collections.Generic;
using System.IO;

namespace Saraff.TwainX {

    /// <summary>
    /// Result of an asynchronous acquisition. The result owns the returned streams.
    /// </summary>
    public sealed class TwainXAcquisitionResult : IDisposable {
        private readonly List<Stream> _streams;
        private readonly List<TwainX.ImageInfo> _imageInfos;
        private bool _disposed;

        internal TwainXAcquisitionResult(List<Stream> streams, List<TwainX.ImageInfo> imageInfos) {
            this._streams = streams;
            this._imageInfos = imageInfos;
        }

        public IReadOnlyList<Stream> Streams => this._streams;

        public IReadOnlyList<TwainX.ImageInfo> ImageInfos => this._imageInfos;

        public void Dispose() {
            if(this._disposed) {
                return;
            }
            this._disposed = true;
            foreach(Stream _stream in this._streams) {
                _stream.Dispose();
            }
            this._streams.Clear();
            this._imageInfos.Clear();
        }
    }
}
