/* Этот файл является частью библиотеки Saraff.Twain.NET
 * © SARAFF SOFTWARE (Кирножицкий Андрей), 2011.
 * Saraff.Twain.NET - свободная программа: вы можете перераспространять ее и/или
 * изменять ее на условиях Меньшей Стандартной общественной лицензии GNU в том виде,
 * в каком она была опубликована Фондом свободного программного обеспечения;
 * либо версии 3 лицензии, либо (по вашему выбору) любой более поздней
 * версии.
 * Saraff.Twain.NET распространяется в надежде, что она будет полезной,
 * но БЕЗО ВСЯКИХ ГАРАНТИЙ; даже без неявной гарантии ТОВАРНОГО ВИДА
 * или ПРИГОДНОСТИ ДЛЯ ОПРЕДЕЛЕННЫХ ЦЕЛЕЙ. Подробнее см. в Меньшей Стандартной
 * общественной лицензии GNU.
 * Вы должны были получить копию Меньшей Стандартной общественной лицензии GNU
 * вместе с этой программой. Если это не так, см.
 * <http://www.gnu.org/licenses/>.)
 * 
 * This file is part of Saraff.Twain.NET.
 * © SARAFF SOFTWARE (Kirnazhytski Andrei), 2011.
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
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Collections.Generic;


namespace Saraff.Twain {

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