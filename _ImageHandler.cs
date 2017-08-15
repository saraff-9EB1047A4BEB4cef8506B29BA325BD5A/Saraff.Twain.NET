using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Saraff.Twain {

    internal abstract class _ImageHandler:IImageHandler {
        private Dictionary<string,object> _state = null;
        private const string _ImagePointer = "ImagePointer";

        #region IImageHandler

        public Stream PtrToStream(IntPtr ptr,IStreamProvider provider) {
            this._state = new Dictionary<string,object> {
                {_ImageHandler._ImagePointer, ptr}
            };

            Stream _stream = provider != null ? provider.GetStream() : new MemoryStream();
            this.PtrToStreamCore(ptr,_stream);
            return _stream;
        }

        #endregion

        protected virtual void PtrToStreamCore(IntPtr ptr,Stream stream) {
            BinaryWriter _writer = new BinaryWriter(stream);

            int _size = this.GetSize();
            byte[] _buffer = new byte[this.BufferSize];

            for(int _offset = 0, _len = 0; _offset < _size; _offset += _len) {
                _len = Math.Min(this.BufferSize,_size - _offset);
                Marshal.Copy((IntPtr)(ptr.ToInt64() + _offset),_buffer,0,_len);
                _writer.Write(_buffer,0,_len);
            }
        }

        protected abstract int GetSize();

        protected abstract int BufferSize {
            get;
        }

        protected Dictionary<string,object> HandlerState {
            get {
                return this._state;
            }
        }

        protected IntPtr ImagePointer {
            get {
                return (IntPtr)this.HandlerState[_ImageHandler._ImagePointer];
            }
        }
    }

    public interface IImageHandler {

        Stream PtrToStream(IntPtr ptr,IStreamProvider provider);
    }

    /// <summary>
    /// Provides instances of the <see cref="System.IO.Stream"/> for data writing.
    /// </summary>
    public interface IStreamProvider {

        /// <summary>
        /// Gets the stream.
        /// </summary>
        /// <returns>The stream.</returns>
        Stream GetStream();
    }
}
