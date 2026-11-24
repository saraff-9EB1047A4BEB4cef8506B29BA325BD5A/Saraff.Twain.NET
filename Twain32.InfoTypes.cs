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
        /// <summary>
        /// Description of the image.
        /// <para xml:lang="ru">Описание изображения.</para>
        /// </summary>
        [Serializable]
        public sealed class ImageInfo {

            private ImageInfo() {
            }

            /// <summary>
            /// Creates and returns a new instance of the ImageInfo class based on an instance of the TwImageInfo class.
            /// <para xml:lang="ru">Создает и возвращает новый экземпляр класса ImageInfo на основе экземпляра класса TwImageInfo.</para>
            /// </summary>
            /// <param name="info">Description of the image.<para xml:lang="ru">Описание изображения.</para></param>
            /// <returns>An instance of the ImageInfo class.<para xml:lang="ru">Экземпляр класса ImageInfo.</para></returns>
            internal static ImageInfo FromTwImageInfo(TwImageInfo info) => new ImageInfo {
                BitsPerPixel = info.BitsPerPixel,
                BitsPerSample = ImageInfo._Copy(info.BitsPerSample, info.SamplesPerPixel),
                Compression = info.Compression,
                ImageLength = info.ImageLength,
                ImageWidth = info.ImageWidth,
                PixelType = info.PixelType,
                Planar = info.Planar,
                XResolution = info.XResolution,
                YResolution = info.YResolution
            };

            private static short[] _Copy(short[] array, int len) {
                var _result = new short[len];
                for(var i = 0; i < len; i++) {
                    _result[i] = array[i];
                }
                return _result;
            }

            /// <summary>
            /// Resolution in the horizontal
            /// </summary>
            public float XResolution { get; private set; }

            /// <summary>
            /// Resolution in the vertical
            /// </summary>
            public float YResolution { get; private set; }

            /// <summary>
            /// Columns in the image, -1 if unknown by DS
            /// </summary>
            public int ImageWidth { get; private set; }

            /// <summary>
            /// Rows in the image, -1 if unknown by DS
            /// </summary>
            public int ImageLength { get; private set; }

            /// <summary>
            /// Number of bits for each sample
            /// </summary>
            public short[] BitsPerSample { get; private set; }

            /// <summary>
            /// Number of bits for each padded pixel
            /// </summary>
            public short BitsPerPixel { get; private set; }

            /// <summary>
            /// True if Planar, False if chunky
            /// </summary>
            public bool Planar { get; private set; }

            /// <summary>
            /// How to interp data; photo interp
            /// </summary>
            public TwPixelType PixelType { get; private set; }

            /// <summary>
            /// How the data is compressed
            /// </summary>
            public TwCompression Compression { get; private set; }
        }

        /// <summary>
        /// Extended image description.
        /// <para xml:lang="ru">Расширенное описание изображения.</para>
        /// </summary>
        [Serializable]
        public sealed class ExtImageInfo : Collection<ExtImageInfo.InfoItem> {

            private ExtImageInfo() {
            }

            /// <summary>
            /// Creates and returns an instance of the ExtImageInfo class from an unmanaged memory block.
            /// <para xml:lang="ru">Создает и возвращает экземпляр класса ExtImageInfo из блока неуправляемой памяти.</para>
            /// </summary>
            /// <param name="ptr">Pointer to an unmanaged memory block.<para xml:lang="ru">Указатель на блок неуправляемой памяти.</para></param>
            /// <returns>An instance of the ExtImageInfo class.<para xml:lang="ru">Экземпляр класса ExtImageInfo.</para></returns>
            internal static ExtImageInfo FromPtr(IntPtr ptr) {
                int _twExtImageInfoSize = Marshal.SizeOf(typeof(TwExtImageInfo));
                int _twInfoSize = Marshal.SizeOf(typeof(TwInfo));
                TwExtImageInfo _extImageInfo = Marshal.PtrToStructure(ptr, typeof(TwExtImageInfo)) as TwExtImageInfo;
                ExtImageInfo _result = new ExtImageInfo();
                for(int i = 0; i < _extImageInfo.NumInfos; i++) {
                    using(TwInfo _item = Marshal.PtrToStructure((IntPtr)(ptr.ToInt64() + _twExtImageInfoSize + (_twInfoSize * i)), typeof(TwInfo)) as TwInfo) {
                        _result.Add(InfoItem.FromTwInfo(_item));
                    }
                }
                return _result;
            }

            /// <summary>
            /// Returns a description element of the extended image information by its code.
            /// <para xml:lang="ru">Возвращает элемент описания расширенной информации о изображении по его коду.</para>
            /// </summary>
            /// <param name="infoId">Description element code for extended image information.<para xml:lang="ru">Код элемента описания расширенной информации о изображении.</para></param>
            /// <returns>Description element for extended image information.<para xml:lang="ru">Элемент описания расширенной информации о изображении.</para></returns>
            /// <exception cref="System.Collections.Generic.KeyNotFoundException">Для указанного кода отсутствует соответствующий элемент.</exception>
            public InfoItem this[TwEI infoId] {
                get {
                    foreach(InfoItem _item in this) {
                        if(_item.InfoId == infoId) {
                            return _item;
                        }
                    }
                    throw new KeyNotFoundException();
                }
            }

            /// <summary>
            /// Description element for extended image information.
            /// <para xml:lang="ru">Элемент описания расширенной информации о изображении.</para>
            /// </summary>
            [Serializable]
            [DebuggerDisplay("InfoId = {InfoId}, IsSuccess = {IsSuccess}, Value = {Value}")]
            public sealed class InfoItem {

                private InfoItem() {
                }

                /// <summary>
                /// Creates and returns an instance class of an extended image information description element from an internal instance of an extended image information description element class.
                /// <para xml:lang="ru">Создает и возвращает экземпляр класса элемента описания расширенной информации о изображении из внутреннего экземпляра класса элемента описания расширенной информации о изображении.</para>
                /// </summary>
                /// <param name="info">An internal instance of the extended image information description element class.<para xml:lang="ru">Внутрений экземпляр класса элемента описания расширенной информации о изображении.</para></param>
                /// <returns>An instance of the extended image information description item class.<para xml:lang="ru">Экземпляр класса элемента описания расширенной информации о изображении.</para></returns>
                internal static InfoItem FromTwInfo(TwInfo info) => new InfoItem {
                    InfoId = info.InfoId,
                    IsNotSupported = info.ReturnCode == TwRC.InfoNotSupported,
                    IsNotAvailable = info.ReturnCode == TwRC.DataNotAvailable,
                    IsSuccess = info.ReturnCode == TwRC.Success,
                    Value = info.GetValue()
                };

                /// <summary>
                /// Returns a code for extended image information.
                /// <para xml:lang="ru">Возвращает код расширенной информации о изображении.</para>
                /// </summary>
                public TwEI InfoId { get; private set; }

                /// <summary>
                /// Вreturns true if the requested information is not supported by the data source; otherwise false.
                /// <para xml:lang="ru">Возвращает true, если запрошенная информация не поддерживается источником данных; иначе, false.</para>
                /// </summary>
                public bool IsNotSupported { get; private set; }

                /// <summary>
                /// Returns true if the requested information is supported by the data source but is currently unavailable; otherwise false.
                /// <para xml:lang="ru">Возвращает true, если запрошенная информация поддерживается источником данных, но в данный момент недоступна; иначе, false.</para>
                /// </summary>
                public bool IsNotAvailable { get; private set; }

                /// <summary>
                /// Returns true if the requested information was successfully retrieved; otherwise false.
                /// <para xml:lang="ru">Возвращает true, если запрошенная информация была успешно извлечена; иначе, false.</para>
                /// </summary>
                public bool IsSuccess { get; private set; }

                /// <summary>
                /// Returns the value of an element.
                /// <para xml:lang="ru">Возвращает значение элемента.</para>
                /// </summary>
                public object Value { get; private set; }
            }
        }

        /// <summary>
        /// Used to pass image data (e.g. in strips) from DS to application.
        /// </summary>
        [Serializable]
        public sealed class ImageMemXfer {

            private ImageMemXfer() {
            }

            internal static ImageMemXfer Create(TwImageMemXfer data) {
                ImageMemXfer _res = new ImageMemXfer() {
                    BytesPerRow = data.BytesPerRow,
                    Columns = data.Columns,
                    Compression = data.Compression,
                    Rows = data.Rows,
                    XOffset = data.XOffset,
                    YOffset = data.YOffset
                };
                if((data.Memory.Flags & TwMF.Handle) != 0) {
                    IntPtr _data = _Memory.Lock(data.Memory.TheMem);
                    try {
                        _res.ImageData = new byte[data.BytesWritten];
                        Marshal.Copy(_data, _res.ImageData, 0, _res.ImageData.Length);
                    } finally {
                        _Memory.Unlock(data.Memory.TheMem);
                    }
                } else {
                    _res.ImageData = new byte[data.BytesWritten];
                    Marshal.Copy(data.Memory.TheMem, _res.ImageData, 0, _res.ImageData.Length);
                }
                return _res;
            }

            /// <summary>
            /// How the data is compressed.
            /// </summary>
            public TwCompression Compression { get; private set; }

            /// <summary>
            /// Number of bytes in a row of data.
            /// </summary>
            public uint BytesPerRow { get; private set; }

            /// <summary>
            /// How many columns.
            /// </summary>
            public uint Columns { get; private set; }

            /// <summary>
            /// How many rows.
            /// </summary>
            public uint Rows { get; private set; }

            /// <summary>
            /// How far from the side of the image.
            /// </summary>
            public uint XOffset { get; private set; }

            /// <summary>
            /// How far from the top of the image.
            /// </summary>
            public uint YOffset { get; private set; }

            /// <summary>
            /// Data.
            /// </summary>
            public byte[] ImageData { get; private set; }
        }

        /// <summary>
        /// Description of the image file.
        /// <para xml:lang="ru">Описание файла изображения.</para>
        /// </summary>
        [Serializable]
        public sealed class ImageFileXfer {

            /// <summary>
            /// Initializes a new instance <see cref="ImageFileXfer"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр <see cref="ImageFileXfer"/>.</para>
            /// </summary>
            private ImageFileXfer() {
            }

            /// <summary>
            /// Creates and returns a new instance <see cref="ImageFileXfer"/>.
            /// <para xml:lang="ru">Создает и возвращает новый экземпляр <see cref="ImageFileXfer"/>.</para>
            /// </summary>
            /// <param name="data">File description.<para xml:lang="ru">Описание файла.</para></param>
            /// <returns>Instance <see cref="ImageFileXfer"/>.<para xml:lang="ru">Экземпляр <see cref="ImageFileXfer"/>.</para></returns>
            internal static ImageFileXfer Create(TwSetupFileXfer data) => new ImageFileXfer {
                FileName = data.FileName,
                Format = data.Format
            };

            /// <summary>
            /// Returns the file name.
            /// <para xml:lang="ru">Возвращает имя файла.</para>
            /// </summary>
            public string FileName { get; private set; }

            /// <summary>
            /// Returns the file format.
            /// <para xml:lang="ru">Фозвращает формат файла.</para>
            /// </summary>
            public TwFF Format { get; private set; }
        }
    }
}
