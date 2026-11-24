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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace Saraff.Twain {

    public sealed partial class Twain32 {
        [Serializable]
        private sealed class _Image {
            private Stream _stream = null;

            [NonSerialized]
            private Image _image = null;
#if !NET2
            [NonSerialized]
            private System.Windows.Media.Imaging.BitmapImage _image2 = null;
#endif

            private _Image() {
            }

            public static implicit operator _Image(Stream stream) => new _Image { _stream = stream };

            public static implicit operator Stream(_Image image) {
                image._stream.Seek(0L, SeekOrigin.Begin);
                return image._stream;
            }

            public static implicit operator Image(_Image value) {
                if(value._image == null) {
                    value._stream.Seek(0L, SeekOrigin.Begin);
                    value._image = Image.FromStream(value._stream);
                }
                return value._image;
            }

#if !NET2
            public static implicit operator System.Windows.Media.ImageSource(_Image value) {
                if(value._image2==null) {
                    value._stream.Seek(0L,SeekOrigin.Begin);
                    value._image2=new System.Windows.Media.Imaging.BitmapImage();
                    value._image2.BeginInit();
                    value._image2.StreamSource=value._stream;
                    value._image2.CacheOption=System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    value._image2.EndInit();
                    value._image2.Freeze();
                }
                return value._image2;
            }
#endif
        }
    }
}
