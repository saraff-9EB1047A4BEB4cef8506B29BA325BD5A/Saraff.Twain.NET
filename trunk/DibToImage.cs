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


namespace Saraff.Twain {

    internal sealed class DibToImage {

        /// <summary>
        /// Get .NET 'Bitmap' object from memory DIB via stream constructor.
        /// This should work for most DIBs.
        /// </summary>
        /// <param name="dibPtr">Pointer to memory DIB, starting with BITMAPINFOHEADER.</param>
        [Obsolete("В методе часто возникают ошибки",true)]
        public static Bitmap WithStream(IntPtr dibPtr) {
            BITMAPINFOHEADER _bmi=(BITMAPINFOHEADER)Marshal.PtrToStructure(dibPtr,typeof(BITMAPINFOHEADER));
            if(_bmi.biSizeImage==0) {
                _bmi.biSizeImage=((((_bmi.biWidth*_bmi.biBitCount)+31)&~31)>>3)*Math.Abs(_bmi.biHeight);
            }
            if((_bmi.biClrUsed==0)&&(_bmi.biBitCount<16)) {
                _bmi.biClrUsed=1<<_bmi.biBitCount;
            }

            int _bmp_header_size=Marshal.SizeOf(typeof(BITMAPFILEHEADER));
            int _dib_size=_bmi.biSize+(_bmi.biClrUsed*4)+_bmi.biSizeImage;  // info + rgb + pixels

            BITMAPFILEHEADER _bmp_header=new BITMAPFILEHEADER() {
                Type=new Char[] { 'B','M' },						// "BM"
                Size=_bmp_header_size+_dib_size,								// final file size
                OffBits=_bmp_header_size+_bmi.biSize+(_bmi.biClrUsed*4)	    // offset to pixels
            };

            byte[] data=new byte[_bmp_header.Size];					// file-sized byte[] 
            RawSerializeInto(_bmp_header,data);						// serialize BITMAPFILEHEADER into byte[]
            Marshal.Copy(dibPtr,data,_bmp_header_size,_dib_size);		// mem-copy DIB into byte[]

            using(MemoryStream stream=new MemoryStream(data)) {		// file-sized stream
                using(Bitmap tmp=new Bitmap(stream,true)) {					// 'tmp' is wired to stream (unfortunately)
                    return new Bitmap(tmp);					// 'result' is a copy (stand-alone)
                }
            }
        }

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
            switch(_bmi.biBitCount) {
                case 32:
                    _fmt=PixelFormat.Format32bppRgb;
                    break;
                case 24:
                    _fmt=PixelFormat.Format24bppRgb;
                    break;
                case 16:
                    _fmt=PixelFormat.Format16bppRgb555;
                    break;
                case 8:
                    _fmt=PixelFormat.Format8bppIndexed;
                    break;
                case 4:
                    _fmt=PixelFormat.Format4bppIndexed;
                    break;
                case 1:
                    _fmt=PixelFormat.Format1bppIndexed;
                    break;
            }

            int _scan0=((int)dibPtr)+_bmi.biSize+(_bmi.biClrUsed*4);	// pointer to pixels
            int _stride=(((_bmi.biWidth*_bmi.biBitCount)+31)&~31)>>3;	// bytes/line
            if(_bmi.biHeight>0) {									// bottom-up
                _scan0+=_stride*(_bmi.biHeight-1);
                _stride=-_stride;
            }
            using(Bitmap _tmp_bitmap=new Bitmap(_bmi.biWidth,Math.Abs(_bmi.biHeight),_stride,_fmt,(IntPtr)_scan0)) {// '_tmp' is wired to scan0 (unfortunately)
                if(_tmp_bitmap.Palette.Entries.Length>0) {
                    ColorPalette _palette=_tmp_bitmap.Palette;
                    for(int i=0,_ptr=dibPtr.ToInt32()+_bmi.biSize;i<_palette.Entries.Length;i++,_ptr+=4) {
                        _palette.Entries[i]=((RGBQUAD)Marshal.PtrToStructure((IntPtr)_ptr,typeof(RGBQUAD))).ToColor();
                    }
                    _tmp_bitmap.Palette=_palette;
                }

                return new Bitmap(_tmp_bitmap); // 'result' is a copy (stand-alone)
            }
        }

        ///// <summary>
        ///// Get .NET 'Bitmap' object from memory DIB via HBITMAP.
        ///// Uses many temporary copies [huge memory usage]!
        ///// </summary>
        ///// <param name="dibPtr">Pointer to memory DIB, starting with BITMAPINFOHEADER.</param>
        //public static Bitmap WithHBitmap(IntPtr dibPtr) {
        //    Type bmiTyp=typeof(BITMAPINFOHEADER);
        //    BITMAPINFOHEADER bmi=(BITMAPINFOHEADER)Marshal.PtrToStructure(dibPtr,bmiTyp);
        //    if(bmi.biSizeImage==0) {
        //        bmi.biSizeImage=((((bmi.biWidth*bmi.biBitCount)+31)&~31)>>3)*Math.Abs(bmi.biHeight);
        //    }
        //    if((bmi.biClrUsed==0)&&(bmi.biBitCount<16)) {
        //        bmi.biClrUsed=1<<bmi.biBitCount;
        //    }

        //    IntPtr pixPtr=new IntPtr((int)dibPtr+bmi.biSize+(bmi.biClrUsed*4));		// pointer to pixels

        //    IntPtr img=IntPtr.Zero;
        //    int st=GdipCreateBitmapFromGdiDib(dibPtr,pixPtr,ref img);
        //    if((st!=0)||(img==IntPtr.Zero)) {
        //        throw new ArgumentException("Invalid bitmap for GDI+","IntPtr dibPtr");
        //    }

        //    IntPtr hbitmap;
        //    st=GdipCreateHBITMAPFromBitmap(img,out hbitmap,0);
        //    if((st!=0)||(hbitmap==IntPtr.Zero)) {
        //        GdipDisposeImage(img);
        //        throw new ArgumentException("can't get HBITMAP with GDI+","IntPtr dibPtr");
        //    }

        //    Bitmap tmp=Image.FromHbitmap(hbitmap);			// 'tmp' is wired to hbitmap (unfortunately)
        //    Bitmap result=new Bitmap(tmp);					// 'result' is a copy (stand-alone)
        //    tmp.Dispose();
        //    tmp=null;
        //    bool ok=DeleteObject(hbitmap);
        //    hbitmap=IntPtr.Zero;
        //    st=GdipDisposeImage(img);
        //    img=IntPtr.Zero;
        //    return result;
        //}

        /// <summary>
        /// Copy structure into Byte-Array.
        /// </summary>
        /// <param name="anything">Anything.</param>
        /// <param name="datas">The datas.</param>
        private static void RawSerializeInto(object anything,byte[] datas) {
            int rawsize=Marshal.SizeOf(anything);
            if(rawsize>datas.Length) {
                throw new ArgumentException(" buffer too small "," byte[] datas ");
            }
            GCHandle handle=GCHandle.Alloc(datas,GCHandleType.Pinned);
            IntPtr buffer=handle.AddrOfPinnedObject();
            Marshal.StructureToPtr(anything,buffer,false);
            handle.Free();
        }

        // GDI imports : read MSDN!

        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi,Pack=1)]
        private class BITMAPFILEHEADER {
            [MarshalAs(UnmanagedType.ByValArray,SizeConst=2)]
            public Char[] Type;
            public Int32 Size;
            public Int16 reserved1;
            public Int16 reserved2;
            public Int32 OffBits;
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

        #region GDI32

        //[DllImport("gdi32.dll",ExactSpelling=true)]
        //private static extern bool DeleteObject(IntPtr obj);

        #endregion

        #region GDI+ from GdiplusFlat.h :   http://msdn.microsoft.com/library/en-us/gdicpp/gdi+/gdi+reference/flatapi.asp

        //	GpStatus WINGDIPAPI    GdipCreateBitmapFromGdiDib( GDIPCONST BITMAPINFO* gdiBitmapInfo, VOID* gdiBitmapData, GpBitmap** bitmap);
        //[DllImport("gdiplus.dll",ExactSpelling=true)]
        //private static extern int GdipCreateBitmapFromGdiDib(IntPtr bminfo,IntPtr pixdat,ref IntPtr image);

        //	GpStatus WINGDIPAPI    GdipCreateHBITMAPFromBitmap( GpBitmap* bitmap, HBITMAP* hbmReturn, ARGB background);
        //[DllImport("gdiplus.dll",ExactSpelling=true)]
        //private static extern int GdipCreateHBITMAPFromBitmap(IntPtr image,out IntPtr hbitmap,int bkg);

        //[DllImport("gdiplus.dll",ExactSpelling=true)]
        //private static extern int GdipDisposeImage(IntPtr image);

        #endregion
    }
}
