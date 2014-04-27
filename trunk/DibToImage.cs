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

/* **************************************************************************
             Converting memory DIB to .NET 'Bitmap' object
                  EXPERIMENTAL, USE AT YOUR OWN RISK     
                       http://dnetmaster.net/
*****************************************************************************/
//
// The 'DibToImage' class provides three different methods [Stream/scan0/HBITMAP alive]
//
// The parameter 'IntPtr dibPtr' is a pointer to
// a classic GDI 'packed DIB bitmap', starting with a BITMAPINFOHEADER
//
// Note, all this methods will use MUCH memory! 
//   (multiple copies of pixel datas)
//
// Whatever I used, all Bitmap/Image constructors
// return objects still beeing backed by the underlying Stream/scan0/HBITMAP.
// Thus you would have to keep the Stream/scan0/HBITMAP alive!
//
// So I tried to make an exact copy/clone of the Bitmap:
// But e.g. Bitmap.Clone() doesn't make a stand-alone duplicate.
// The working method I used here is :   Bitmap copy = new Bitmap( original );
// Unfortunately, the returned Bitmap will always have a pixel-depth of 32bppARGB !
// But this is a pure GDI+/.NET problem... maybe somebody else can help?
// 
//
//             ----------------------------
// Note, Microsoft should really wrap GDI+ 'GdipCreateBitmapFromGdiDib' in .NET!
// This would be very useful!
//
// There is a :
//        Bitmap Image.FromHbitmap( IntPtr hbitmap )
// so there is NO reason to not add a:
//        Bitmap Image.FromGdiDib( IntPtr dibptr )
//
// PLEASE SEND EMAIL TO:  netfwsdk@microsoft.com
//   OR  mswish@microsoft.com
//   OR  http://register.microsoft.com/mswish/suggestion.asp
// ------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Collections.Generic;


namespace Saraff.Twain {

    internal sealed class DibToImage {
        private static Dictionary<short,PixelFormat> _pixels=new Dictionary<short,PixelFormat> {
            {1,PixelFormat.Format1bppIndexed},
            {4,PixelFormat.Format4bppIndexed},
            {8,PixelFormat.Format8bppIndexed},
            {16,PixelFormat.Format16bppRgb555},
            {24,PixelFormat.Format24bppRgb},
            {32,PixelFormat.Format32bppRgb},
            {48,PixelFormat.Format48bppRgb},
            {64,PixelFormat.Format64bppArgb}
        };

        /// <summary>
        /// Get .NET 'Bitmap' object from memory DIB via 'scan0' constructor.
        /// </summary>
        /// <param name="dibPtr">Pointer to memory DIB, starting with BITMAPINFOHEADER.</param>
        public static Bitmap WithScan0(IntPtr dibPtr) {
            BITMAPINFOHEADER _bmi=(BITMAPINFOHEADER)Marshal.PtrToStructure(dibPtr,typeof(BITMAPINFOHEADER));
            if(_bmi.biCompression!=0) {
                throw new ArgumentException("Invalid bitmap format (non-RGB)","BITMAPINFOHEADER.biCompression");
            }

            PixelFormat _fmt=PixelFormat.Undefined;
            if(DibToImage._pixels.ContainsKey(_bmi.biBitCount)) {
                _fmt=DibToImage._pixels[_bmi.biBitCount];
            }

            long _scan0=(dibPtr.ToInt64())+_bmi.biSize+(_bmi.biClrUsed*4);	// pointer to pixels
            int _stride=(((_bmi.biWidth*_bmi.biBitCount)+31)&~31)>>3;	// bytes/line
            if(_bmi.biHeight>0) {									// bottom-up
                _scan0+=_stride*(_bmi.biHeight-1);
                _stride=-_stride;
            }
            using(Bitmap _tmp_bitmap=new Bitmap(_bmi.biWidth,Math.Abs(_bmi.biHeight),_stride,_fmt,(IntPtr)_scan0)) {// '_tmp' is wired to scan0 (unfortunately)
                if(_tmp_bitmap.Palette.Entries.Length>0) {
                    ColorPalette _palette=_tmp_bitmap.Palette;
                    for(long i=0,_ptr=dibPtr.ToInt64()+_bmi.biSize;i<_palette.Entries.Length;i++,_ptr+=4) {
                        _palette.Entries[i]=((RGBQUAD)Marshal.PtrToStructure((IntPtr)_ptr,typeof(RGBQUAD))).ToColor();
                    }
                    _tmp_bitmap.Palette=_palette;
                }

                return new Bitmap(_tmp_bitmap); // 'result' is a copy (stand-alone)
            }
        }

        public static Image WithStream(IntPtr dibPtr) {
            MemoryStream _stream=new MemoryStream();
            BinaryWriter _writer=new BinaryWriter(_stream);

            BITMAPINFOHEADER _bmi=(BITMAPINFOHEADER)Marshal.PtrToStructure(dibPtr,typeof(BITMAPINFOHEADER));

            #region BITMAPFILEHEADER

            _writer.Write((ushort)0x4d42);
            _writer.Write(14+_bmi.biSize+_bmi.biSizeImage+(_bmi.biClrUsed*4));
            _writer.Write(0);
            _writer.Write(14+_bmi.biSize);

            #endregion

            #region BITMAPINFO and pixel data

            byte[] _data=new byte[_bmi.biSize+_bmi.biSizeImage+(_bmi.biClrUsed*4)];
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
        }

        [StructLayout(LayoutKind.Sequential,Pack=1)]
        private class RGBQUAD {
            public byte Blue;
            public byte Green;
            public byte Red;
            public byte Reserved;

            public Color ToColor() {
                return Color.FromArgb(this.Red,this.Green,this.Blue);
            }
        }
    }
}
