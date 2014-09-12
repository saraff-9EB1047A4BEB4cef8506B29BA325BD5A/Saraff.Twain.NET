/* Saraff.Twain.dll позволят управлять сканером, цифровой или веб камерой, а также любым другим TWAIN совместимым устройством.
 * © SARAFF SOFTWARE (Кирножицкий Андрей), 2011.
 * Данная библиотека является свободным программным обеспечением. 
 * Вы вправе распространять её и/или модифицировать в соответствии 
 * с условиями версии 3 либо по вашему выбору с условиями более поздней 
 * версии Стандартной Общественной Лицензии Ограниченного Применения GNU, 
 * опубликованной Free Software Foundation.
 * Мы распространяем эту библиотеку в надежде на то, что она будет Вам 
 * полезной, однако НЕ ПРЕДОСТАВЛЯЕМ НА НЕЕ НИКАКИХ ГАРАНТИЙ, в том числе 
 * ГАРАНТИИ ТОВАРНОГО СОСТОЯНИЯ ПРИ ПРОДАЖЕ и ПРИГОДНОСТИ ДЛЯ ИСПОЛЬЗОВАНИЯ 
 * В КОНКРЕТНЫХ ЦЕЛЯХ. Для получения более подробной информации ознакомьтесь 
 * со Стандартной Общественной Лицензией Ограниченного Применений GNU.
 * Вместе с данной библиотекой вы должны были получить экземпляр Стандартной 
 * Общественной Лицензии Ограниченного Применения GNU. Если вы его не получили, 
 * сообщите об этом в Software Foundation, Inc., 59 Temple Place — Suite 330, 
 * Boston, MA 02111-1307, USA.
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
