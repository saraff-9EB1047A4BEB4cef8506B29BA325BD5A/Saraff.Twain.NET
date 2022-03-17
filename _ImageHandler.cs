using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Saraff.Twain {

    /// <summary>
    /// Base class to processing of a acquired image.
    /// </summary>
    /// <seealso cref="Saraff.Twain.IImageHandler" />
    internal abstract class _ImageHandler : IImageHandler {
        private Dictionary<string, object> _state = null;
        private const string _ImagePointer = "ImagePointer";

        #region IImageHandler

        /// <summary>
        /// Convert a block of unmanaged memory to stream.
        /// </summary>
        /// <param name="ptr">The pointer to block of unmanaged memory.</param>
        /// <param name="provider">The provider of a streams.</param>
        /// <returns>
        /// Stream that contains data of a image.
        /// </returns>
        public Stream PtrToStream(IntPtr ptr, IStreamProvider provider) {
            this._state = new Dictionary<string, object> {
                {_ImageHandler._ImagePointer, ptr}
            };

            Stream _stream = provider != null ? provider.GetStream() : new MemoryStream();
            this.PtrToStreamCore(ptr, _stream);
            return _stream;
        }

        #endregion

        /// <summary>
        /// Convert a block of unmanaged memory to stream.
        /// </summary>
        /// <param name="ptr">The pointer to block of unmanaged memory.</param>
        /// <param name="provider">The provider of a streams.</param>
        protected virtual void PtrToStreamCore(IntPtr ptr, Stream stream) {
            BinaryWriter _writer = new BinaryWriter(stream);

            int _size = this.GetSize();
            byte[] _buffer = new byte[this.BufferSize];

            for(int _offset = 0, _len = 0; _offset < _size; _offset += _len) {
                _len = Math.Min(this.BufferSize, _size - _offset);
                Marshal.Copy((IntPtr)(ptr.ToInt64() + _offset), _buffer, 0, _len);
                _writer.Write(_buffer, 0, _len);
            }
        }

        /// <summary>
        /// Gets the size of a image data.
        /// </summary>
        /// <returns>Size of a image data.</returns>
        protected abstract int GetSize();

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <value>
        /// The size of the buffer.
        /// </value>
        protected abstract int BufferSize { get; }

        /// <summary>
        /// Gets the state of the handler.
        /// </summary>
        /// <value>
        /// The state of the handler.
        /// </value>
        protected Dictionary<string, object> HandlerState => this._state;

        /// <summary>
        /// Gets the pointer to unmanaged memory that contain image data.
        /// </summary>
        /// <value>
        /// The image pointer.
        /// </value>
        protected IntPtr ImagePointer => (IntPtr)this.HandlerState[_ImageHandler._ImagePointer];
    }

    /// <summary>
    /// Provides processing of a acquired image.
    /// </summary>
    public interface IImageHandler {

        /// <summary>
        /// Convert a block of unmanaged memory to stream.
        /// </summary>
        /// <param name="ptr">The pointer to block of unmanaged memory.</param>
        /// <param name="provider">The provider of a streams.</param>
        /// <returns>Stream that contains data of a image.</returns>
        Stream PtrToStream(IntPtr ptr, IStreamProvider provider);
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

    /// <summary>
    /// Provides image factory.
    /// </summary>
    /// <typeparam name="T">Type of image</typeparam>
    public interface IImageFactory<T> {

        /// <summary>
        /// Create and return instance of image.
        /// </summary>
        /// <param name="stream">Image data.</param>
        /// <returns>Image.</returns>
        T Create(Stream stream);
    }
}