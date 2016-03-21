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

    internal sealed class DibToImage {

        public static Image WithStream(IntPtr dibPtr) {
            MemoryStream _stream=new MemoryStream();
            BinaryWriter _writer=new BinaryWriter(_stream);

            BITMAPINFOHEADER _bmi=(BITMAPINFOHEADER)Marshal.PtrToStructure(dibPtr,typeof(BITMAPINFOHEADER));

            int _extra=0;
            if(_bmi.biCompression==0) {
                int _bytesPerRow=((_bmi.biWidth*_bmi.biBitCount)>>3);
                _extra=Math.Max(_bmi.biHeight*(_bytesPerRow+((_bytesPerRow&0x3)!=0?4-_bytesPerRow&0x3:0))-_bmi.biSizeImage,0);
            }

            int _dibSize=_bmi.biSize+_bmi.biSizeImage+_extra+(_bmi.ClrUsed<<2);

            #region BITMAPFILEHEADER

            _writer.Write((ushort)0x4d42);
            _writer.Write(14+_dibSize);
            _writer.Write(0);
            _writer.Write(14+_bmi.biSize+(_bmi.ClrUsed<<2));

            #endregion

            #region BITMAPINFO and pixel data

            byte[] _data=new byte[_dibSize];
            Marshal.Copy(dibPtr,_data,0,_data.Length);
            _writer.Write(_data);

            #endregion
            
            return Image.FromStream(_stream);
        }

        [StructLayout(LayoutKind.Sequential,Pack=2)]
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

            public int ClrUsed {
                get {
                    return this.IsRequiredCreateColorTable?Convert.ToInt32(Math.Pow(2,this.biBitCount)):this.biClrUsed;
                }
            }

            public bool IsRequiredCreateColorTable {
                get {
                    return this.biClrUsed==0&&this.biBitCount<=8;
                }
            }
        }
    }
}
