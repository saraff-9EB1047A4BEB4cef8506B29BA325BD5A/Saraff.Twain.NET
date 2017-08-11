using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Saraff.Twain {

    internal sealed class Pict {
        private const int BufferSize=256*1024; //256K

        private static Pict FromPtr(IntPtr ptr) {
            throw new NotImplementedException();
        }

        public static Stream FromPtrToImage(IntPtr ptr,IStreamProvider provider) {
            Pict _pict=Pict.FromPtr(ptr);
            Stream _stream=provider!=null?provider.GetStream():new MemoryStream();
            BinaryWriter _writer=new BinaryWriter(_stream);

            int _tiffSize=_pict._GetSize();
            byte[] _buffer=new byte[Pict.BufferSize];

            for(int _offset=0, _len=0; _offset<_tiffSize; _offset+=_len) {
                _len=Math.Min(Pict.BufferSize,_tiffSize-_offset);
                Marshal.Copy((IntPtr)(ptr.ToInt64()+_offset),_buffer,0,_len);
                _writer.Write(_buffer,0,_len);
            }

            return _stream;
        }

        private int _GetSize() {
            throw new NotImplementedException();
        }
    }
}
