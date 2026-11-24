/* ���� ���� �������� ������ ���������� Saraff.Twain.NET
 * � SARAFF SOFTWARE (����������� ������), 2011.
 * Saraff.Twain.NET - ��������� ���������: �� ������ ������������������ �� �/���
 * �������� �� �� �������� ������� ����������� ������������ �������� GNU � ��� ����,
 * � ����� ��� ���� ������������ ������ ���������� ������������ �����������;
 * ���� ������ 3 ��������, ���� (�� ������ ������) ����� ����� �������
 * ������.
 * Saraff.Twain.NET ���������������� � �������, ��� ��� ����� ��������,
 * �� ���� ������ ��������; ���� ��� ������� �������� ��������� ����
 * ��� ����������� ��� ������������ �����. ��������� ��. � ������� �����������
 * ������������ �������� GNU.
 * �� ������ ���� �������� ����� ������� ����������� ������������ �������� GNU
 * ������ � ���� ����������. ���� ��� �� ���, ��.
 * <http://www.gnu.org/licenses/>.)
 * 
 * This file is part of Saraff.Twain.NET.
 * � SARAFF SOFTWARE (Kirnazhytski Andrei), 2011.
 * Saraff.Twain.NET is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * Saraff.Twain.NET is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * You should have received a copy of the GNU Lesser General Public License
 * along with Saraff.Twain.NET. If not, see <http://www.gnu.org/licenses/>.
 * 
 * PLEASE SEND EMAIL TO:  twain@saraff.ru.
 */
using System;
using System.Runtime.InteropServices;
using System.Drawing;
#if !TWAINX
using System.Drawing.Imaging;
#endif
using System.IO;
using System.Reflection;
using System.Collections.Generic;


#if TWAINX
namespace Saraff.TwainX {
#else
namespace Saraff.Twain {
#endif

    internal sealed class DibToImage : _ImageHandler {

        /// <summary>
        /// Convert a block of unmanaged memory to stream.
        /// </summary>
        /// <param name="ptr">The pointer to block of unmanaged memory.</param>
        /// <param name="stream"></param>
        protected override void PtrToStreamCore(IntPtr ptr, Stream stream) {
            BinaryWriter _writer = new BinaryWriter(stream);

            #region BITMAPFILEHEADER

            BITMAPINFOHEADER _header = this.Header;

            _writer.Write((ushort)0x4d42);
            _writer.Write(14 + this.GetSize());
            _writer.Write(0);
            _writer.Write(14 + _header.biSize + (_header.ClrUsed << 2));

            #endregion

            #region BITMAPINFO and pixel data

            base.PtrToStreamCore(ptr, stream);

            #endregion

        }

        /// <summary>
        /// Gets the size of a image data.
        /// </summary>
        /// <returns>
        /// Size of a image data.
        /// </returns>
        protected override int GetSize() {
            if(!this.HandlerState.ContainsKey("DIBSIZE")) {
                BITMAPINFOHEADER _header = this.Header;

                int _extra = 0;
                if(_header.biCompression == 0) {
                    int _bytesPerRow = ((_header.biWidth * _header.biBitCount) >> 3);
                    _extra = Math.Max(_header.biHeight * (_bytesPerRow + ((_bytesPerRow & 0x3) != 0 ? 4 - _bytesPerRow & 0x3 : 0)) - _header.biSizeImage, 0);
                }

                this.HandlerState.Add("DIBSIZE", _header.biSize + _header.biSizeImage + _extra + (_header.ClrUsed << 2));
            }
            return (int)this.HandlerState["DIBSIZE"];
        }

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <value>
        /// The size of the buffer.
        /// </value>
        protected override int BufferSize => 256 * 1024; //256K

        private BITMAPINFOHEADER Header {
            get {
                if(!this.HandlerState.ContainsKey("BITMAPINFOHEADER")) {
                    this.HandlerState.Add("BITMAPINFOHEADER", Marshal.PtrToStructure(this.ImagePointer, typeof(BITMAPINFOHEADER)));
                }
                return this.HandlerState["BITMAPINFOHEADER"] as BITMAPINFOHEADER;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private class BITMAPINFOHEADER {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;

            public int ClrUsed => this.IsRequiredCreateColorTable ? 1 << this.biBitCount : this.biClrUsed;

            public bool IsRequiredCreateColorTable => this.biClrUsed == 0 && this.biBitCount <= 8;
        }
    }
}