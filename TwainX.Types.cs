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
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace Saraff.TwainX {

    public sealed partial class TwainX {
        #region Nested classes

        /// <summary>
        /// Точки входа для работы с DSM.
        /// </summary>
        private sealed class _DsmEntry {

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="_DsmEntry"/>.
            /// </summary>
            /// <param name="ptr">Указатель на DSM_Entry.</param>
            private _DsmEntry(IntPtr ptr) {
                if(ptr != IntPtr.Zero) {
                    this.DsmParent = _DsmEntry.CreateDelegate<_DSMparent>(ptr);
                    this.DsmRaw = _DsmEntry.CreateDelegate<_DSMraw>(ptr);
                    this.DSImageXfer = _DsmEntry.CreateDelegate<_DSixfer>(ptr);
                    this.DsRaw = _DsmEntry.CreateDelegate<_DSraw>(ptr);
                    return;
                }
                switch(_Platform.Platform) {
                    case PlatformID.Unix:
                        this.DsmParent = _DsmEntry._LinuxDsmParent;
                        this.DsmRaw = _DsmEntry._LinuxDsmRaw;
                        this.DSImageXfer = _DsmEntry._LinuxDsImageXfer;
                        this.DsRaw = _DsmEntry._LinuxDsRaw;
                        break;
                    case PlatformID.MacOSX:
                        this.DsmParent = _DsmEntry._MacosxDsmParent;
                        this.DsmRaw = _DsmEntry._MacosxDsmRaw;
                        this.DSImageXfer = _DsmEntry._MacosxDsImageXfer;
                        this.DsRaw = _DsmEntry._MacosxDsRaw;
                        break;
                    default:
                        throw new ArgumentException("DSM_Entry pointer is not specified.", nameof(ptr));
                }
            }

            /// <summary>
            /// Создает и возвращает новый экземпляр класса <see cref="_DsmEntry"/>.
            /// </summary>
            /// <param name="ptr">Указатель на DSM_Entry.</param>
            /// <returns>Экземпляр класса <see cref="_DsmEntry"/>.</returns>
            public static _DsmEntry Create(IntPtr ptr) => new _DsmEntry(ptr);

            /// <summary>
            /// Приводит указатель к требуемомы делегату.
            /// </summary>
            /// <typeparam name="T">Требуемый делегат.</typeparam>
            /// <param name="ptr">Указатель на DSM_Entry.</param>
            /// <returns>Делегат.</returns>
            private static T CreateDelegate<T>(IntPtr ptr) where T : class => Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)) as T;

            public TwRC DsmInvoke<T>(TwIdentity origin, TwDG dg, TwDAT dat, TwMSG msg, ref T data) where T : class {
                if(data == null) {
                    throw new ArgumentNullException();
                }
                IntPtr _data = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
                try {
                    Marshal.StructureToPtr(data, _data, true);

                    TwRC _rc = this.DsmRaw(origin, IntPtr.Zero, dg, dat, msg, _data);
                    if(_rc == TwRC.Success) {
                        data = (T)Marshal.PtrToStructure(_data, typeof(T));
                    }
                    return _rc;
                } finally {
                    Marshal.FreeHGlobal(_data);
                }
            }

            public TwRC DsInvoke<T>(TwIdentity origin, TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref T data) where T : class {
                if(data == null) {
                    throw new ArgumentNullException();
                }
                IntPtr _data = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
                try {
                    Marshal.StructureToPtr(data, _data, true);

                    TwRC _rc = this.DsRaw(origin, dest, dg, dat, msg, _data);
                    if(_rc == TwRC.Success || _rc == TwRC.DSEvent || _rc == TwRC.XferDone) {
                        data = (T)Marshal.PtrToStructure(_data, typeof(T));
                    }
                    return _rc;
                } finally {
                    Marshal.FreeHGlobal(_data);
                }
            }

            #region Properties

            public _DSMparent DsmParent { get; private set; }

            public _DSMraw DsmRaw { get; private set; }

            public _DSixfer DSImageXfer { get; private set; }

            public _DSraw DsRaw { get; private set; }

            #endregion

            #region import libtwaindsm.so (Unix)

            [DllImport("/usr/local/lib/libtwaindsm.so", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _LinuxDsmParent([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr refptr);

            [DllImport("/usr/local/lib/libtwaindsm.so", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _LinuxDsmRaw([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, IntPtr rawData);

            [DllImport("/usr/local/lib/libtwaindsm.so", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _LinuxDsImageXfer([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr hbitmap);

            [DllImport("/usr/local/lib/libtwaindsm.so", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _LinuxDsRaw([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, IntPtr arg);

            #endregion

            #region import TWAIN.framework/TWAIN (MacOSX)

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsmParent([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr refptr);

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsmRaw([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, IntPtr rawData);

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsImageXfer([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr hbitmap);

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsRaw([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, IntPtr arg);

            #endregion

        }


        [Serializable]
        private sealed class _Image {
            private Stream _stream = null;

            private _Image() {
            }

            public static implicit operator _Image(Stream stream) => new _Image { _stream = stream };

            public static implicit operator Stream(_Image image) {
                image._stream.Seek(0L, SeekOrigin.Begin);
                return image._stream;
            }
        }

        /// <summary>
        /// Диапазон значений.
        /// </summary>
        [Serializable]
        public sealed class Range {

            /// <summary>
            /// Prevents a default instance of the <see cref="Range"/> class from being created.
            /// </summary>
            private Range() {
            }

            /// <summary>
            /// Prevents a default instance of the <see cref="Range"/> class from being created.
            /// </summary>
            /// <param name="range">The range.</param>
            private Range(TwRange range) {
                this.MinValue = TwTypeHelper.CastToCommon(range.ItemType, TwTypeHelper.ValueToTw<uint>(range.ItemType, range.MinValue));
                this.MaxValue = TwTypeHelper.CastToCommon(range.ItemType, TwTypeHelper.ValueToTw<uint>(range.ItemType, range.MaxValue));
                this.StepSize = TwTypeHelper.CastToCommon(range.ItemType, TwTypeHelper.ValueToTw<uint>(range.ItemType, range.StepSize));
                this.CurrentValue = TwTypeHelper.CastToCommon(range.ItemType, TwTypeHelper.ValueToTw<uint>(range.ItemType, range.CurrentValue));
                this.DefaultValue = TwTypeHelper.CastToCommon(range.ItemType, TwTypeHelper.ValueToTw<uint>(range.ItemType, range.DefaultValue));
            }

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Range"/>.
            /// </summary>
            /// <param name="range">Экземпляр <see cref="TwRange"/>.</param>
            /// <returns>Экземпляр <see cref="Range"/>.</returns>
            internal static Range CreateRange(TwRange range) => new Range(range);

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Range"/>.
            /// </summary>
            /// <param name="minValue">Минимальное значение.</param>
            /// <param name="maxValue">Максимальное значение.</param>
            /// <param name="stepSize">Шаг.</param>
            /// <param name="defaultValue">Значение по умолчанию.</param>
            /// <param name="currentValue">Текущее значение.</param>
            /// <returns>Экземпляр <see cref="Range"/>.</returns>
            public static Range CreateRange(object minValue, object maxValue, object stepSize, object defaultValue, object currentValue) => new Range() {
                MinValue = minValue, MaxValue = maxValue, StepSize = stepSize, DefaultValue = defaultValue, CurrentValue = currentValue
            };

            /// <summary>
            /// Возвращает или устанавливает минимальное значение.
            /// </summary>
            public object MinValue { get; set; }

            /// <summary>
            /// Возвращает или устанавливает максимальное значение.
            /// </summary>
            public object MaxValue { get; set; }

            /// <summary>
            /// Возвращает или устанавливает шаг.
            /// </summary>
            public object StepSize { get; set; }

            /// <summary>
            /// Возвращает или устанавливает значае по умолчанию.
            /// </summary>
            public object DefaultValue { get; set; }

            /// <summary>
            /// Возвращает или устанавливает текущее значение.
            /// </summary>
            public object CurrentValue { get; set; }

            /// <summary>
            /// Конвертирует экземпляр класса в экземпляр <see cref="TwRange"/>.
            /// </summary>
            /// <returns>Экземпляр <see cref="TwRange"/>.</returns>
            internal TwRange ToTwRange() {
                TwType _type = TwTypeHelper.TypeOf(this.CurrentValue.GetType());
                return new TwRange() {
                    ItemType = _type,
                    MinValue = TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type, this.MinValue)),
                    MaxValue = TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type, this.MaxValue)),
                    StepSize = TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type, this.StepSize)),
                    DefaultValue = TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type, this.DefaultValue)),
                    CurrentValue = TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type, this.CurrentValue))
                };
            }
        }

        /// <summary>
        /// Перечисление.
        /// </summary>
        [Serializable]
        public sealed class Enumeration {
            private object[] _items;

            /// <summary>
            /// Prevents a default instance of the <see cref="Enumeration"/> class from being created.
            /// </summary>
            /// <param name="items">Элементы перечисления.</param>
            /// <param name="currentIndex">Текущий индекс.</param>
            /// <param name="defaultIndex">Индекс по умолчанию.</param>
            private Enumeration(object[] items, int currentIndex, int defaultIndex) {
                this._items = items;
                this.CurrentIndex = currentIndex;
                this.DefaultIndex = defaultIndex;
            }

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Enumeration"/>.
            /// </summary>
            /// <param name="items">Элементы перечисления.</param>
            /// <param name="currentIndex">Текущий индекс.</param>
            /// <param name="defaultIndex">Индекс по умолчанию.</param>
            /// <returns>Экземпляр <see cref="Enumeration"/>.</returns>
            public static Enumeration CreateEnumeration(object[] items, int currentIndex, int defaultIndex) => new Enumeration(items, currentIndex, defaultIndex);

            /// <summary>
            /// Возвращает количество элементов.
            /// </summary>
            public int Count => this._items.Length;

            /// <summary>
            /// Возвращает текущий индекс.
            /// </summary>
            public int CurrentIndex { get; private set; }

            /// <summary>
            /// Возвращает индекс по умолчанию.
            /// </summary>
            public int DefaultIndex { get; private set; }

            /// <summary>
            /// Возвращает элемент по указанному индексу.
            /// </summary>
            /// <param name="index">Индекс.</param>
            /// <returns>Элемент по указанному индексу.</returns>
            public object this[int index] {
                get {
                    return this._items[index];
                }
                internal set {
                    this._items[index] = value;
                }
            }

            internal object[] Items => this._items;

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Enumeration"/>.
            /// </summary>
            /// <param name="value">Экземпляр <see cref="Range"/>.</param>
            /// <returns>Экземпляр <see cref="Enumeration"/>.</returns>
            public static Enumeration FromRange(Range value) {
                int _currentIndex = 0, _defaultIndex = 0;
                object[] _items = new object[(int)((Convert.ToSingle(value.MaxValue) - Convert.ToSingle(value.MinValue)) / Convert.ToSingle(value.StepSize)) + 1];
                for(int i = 0; i < _items.Length; i++) {
                    _items[i] = Convert.ToSingle(value.MinValue) + (Convert.ToSingle(value.StepSize) * i);
                    if(Convert.ToSingle(_items[i]) == Convert.ToSingle(value.CurrentValue)) {
                        _currentIndex = i;
                    }
                    if(Convert.ToSingle(_items[i]) == Convert.ToSingle(value.DefaultValue)) {
                        _defaultIndex = i;
                    }
                }
                return Enumeration.CreateEnumeration(_items, _currentIndex, _defaultIndex);
            }

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Enumeration"/>.
            /// </summary>
            /// <param name="value">Массив значений.</param>
            /// <returns>Экземпляр <see cref="Enumeration"/>.</returns>
            public static Enumeration FromArray(object[] value) => Enumeration.CreateEnumeration(value, 0, 0);

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Enumeration"/>.
            /// </summary>
            /// <param name="value">Значение.</param>
            /// <returns>Экземпляр <see cref="Enumeration"/>.</returns>
            public static Enumeration FromOneValue(ValueType value) => Enumeration.CreateEnumeration(new object[] { value }, 0, 0);

            internal static Enumeration FromObject(object value) {
                if(value is Range _v1) {
                    return Enumeration.FromRange(_v1);
                }
                if(value is object[] _v2) {
                    return Enumeration.FromArray(_v2);
                }
                if(value is ValueType _v3) {
                    return Enumeration.FromOneValue(_v3);
                }
                if(value is string) {
                    return Enumeration.CreateEnumeration(new object[] { value }, 0, 0);
                }
                return value as Enumeration;
            }
        }

        /// <summary>
        /// Описание изображения.
        /// </summary>
        [Serializable]
        public sealed class ImageInfo {

            private ImageInfo() {
            }

            /// <summary>
            /// Создает и возвращает новый экземпляр класса ImageInfo на основе экземпляра класса TwImageInfo.
            /// </summary>
            /// <param name="info">Описание изображения.</param>
            /// <returns>Экземпляр класса ImageInfo.</returns>
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
        /// Расширенное описание изображения.
        /// </summary>
        [Serializable]
        public sealed class ExtImageInfo : Collection<ExtImageInfo.InfoItem> {

            private ExtImageInfo() {
            }

            /// <summary>
            /// Создает и возвращает экземпляр класса ExtImageInfo из блока неуправляемой памяти.
            /// </summary>
            /// <param name="ptr">Указатель на блок неуправляемой памяти.</param>
            /// <returns>Экземпляр класса ExtImageInfo.</returns>
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
            /// Возвращает элемент описания расширенной информации о изображении по его коду.
            /// </summary>
            /// <param name="infoId">Код элемента описания расширенной информации о изображении.</param>
            /// <returns>Элемент описания расширенной информации о изображении.</returns>
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
            /// Элемент описания расширенной информации о изображении.
            /// </summary>
            [Serializable]
            [DebuggerDisplay("InfoId = {InfoId}, IsSuccess = {IsSuccess}, Value = {Value}")]
            public sealed class InfoItem {

                private InfoItem() {
                }

                /// <summary>
                /// Создает и возвращает экземпляр класса элемента описания расширенной информации о изображении из внутреннего экземпляра класса элемента описания расширенной информации о изображении.
                /// </summary>
                /// <param name="info">Внутрений экземпляр класса элемента описания расширенной информации о изображении.</param>
                /// <returns>Экземпляр класса элемента описания расширенной информации о изображении.</returns>
                internal static InfoItem FromTwInfo(TwInfo info) => new InfoItem {
                    InfoId = info.InfoId,
                    IsNotSupported = info.ReturnCode == TwRC.InfoNotSupported,
                    IsNotAvailable = info.ReturnCode == TwRC.DataNotAvailable,
                    IsSuccess = info.ReturnCode == TwRC.Success,
                    Value = info.GetValue()
                };

                /// <summary>
                /// Возвращает код расширенной информации о изображении.
                /// </summary>
                public TwEI InfoId { get; private set; }

                /// <summary>
                /// Возвращает true, если запрошенная информация не поддерживается источником данных; иначе, false.
                /// </summary>
                public bool IsNotSupported { get; private set; }

                /// <summary>
                /// Возвращает true, если запрошенная информация поддерживается источником данных, но в данный момент недоступна; иначе, false.
                /// </summary>
                public bool IsNotAvailable { get; private set; }

                /// <summary>
                /// Возвращает true, если запрошенная информация была успешно извлечена; иначе, false.
                /// </summary>
                public bool IsSuccess { get; private set; }

                /// <summary>
                /// Возвращает значение элемента.
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
        /// Описание файла изображения.
        /// </summary>
        [Serializable]
        public sealed class ImageFileXfer {

            /// <summary>
            /// Инициализирует новый экземпляр <see cref="ImageFileXfer"/>.
            /// </summary>
            private ImageFileXfer() {
            }

            /// <summary>
            /// Создает и возвращает новый экземпляр <see cref="ImageFileXfer"/>.
            /// </summary>
            /// <param name="data">Описание файла.</param>
            /// <returns>Экземпляр <see cref="ImageFileXfer"/>.</returns>
            internal static ImageFileXfer Create(TwSetupFileXfer data) => new ImageFileXfer {
                FileName = data.FileName,
                Format = data.Format
            };

            /// <summary>
            /// Возвращает имя файла.
            /// </summary>
            public string FileName { get; private set; }

            /// <summary>
            /// Фозвращает формат файла.
            /// </summary>
            public TwFF Format { get; private set; }
        }

        /// <summary>
        /// Набор операций для работы с цветовой палитрой.
        /// </summary>
        public sealed class TwainPalette : MarshalByRefObject {
            private TwainX _twain;

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="TwainPalette"/>.
            /// </summary>
            /// <param name="twain">Экземпляр класса <see cref="TwainPalette"/>.</param>
            internal TwainPalette(TwainX twain) {
                this._twain = twain;
            }

            /// <summary>
            /// Возвращает текущую цветовую палитру.
            /// </summary>
            /// <returns>Экземпляр класса <see cref="TwainPalette"/>.</returns>
            public ColorPalette Get() {
                TwPalette8 _palette = new TwPalette8();
                TwRC _rc = this._twain._dsmEntry.DsInvoke(this._twain._AppId, this._twain._srcds, TwDG.Image, TwDAT.Palette8, TwMSG.Get, ref _palette);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(), _rc);
                }
                return _palette;
            }

            /// <summary>
            /// Возвращает текущую цветовую палитру, используемую по умолчанию.
            /// </summary>
            /// <returns>Экземпляр класса <see cref="TwainPalette"/>.</returns>
            public ColorPalette GetDefault() {
                TwPalette8 _palette = new TwPalette8();
                TwRC _rc = this._twain._dsmEntry.DsInvoke(this._twain._AppId, this._twain._srcds, TwDG.Image, TwDAT.Palette8, TwMSG.GetDefault, ref _palette);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(), _rc);
                }
                return _palette;
            }

            /// <summary>
            /// Сбрасывает текущую цветовую палитру и устанавливает указанную.
            /// </summary>
            /// <param name="palette">Экземпляр класса <see cref="TwainPalette"/>.</param>
            public void Reset(ColorPalette palette) {
                TwRC _rc = this._twain._dsmEntry.DsInvoke(this._twain._AppId, this._twain._srcds, TwDG.Image, TwDAT.Palette8, TwMSG.Reset, ref palette);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(), _rc);
                }
            }

            /// <summary>
            /// Устанавливает указанную цветовую палитру.
            /// </summary>
            /// <param name="palette">Экземпляр класса <see cref="TwainPalette"/>.</param>
            public void Set(ColorPalette palette) {
                TwRC _rc = this._twain._dsmEntry.DsInvoke(this._twain._AppId, this._twain._srcds, TwDG.Image, TwDAT.Palette8, TwMSG.Set, ref palette);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(), _rc);
                }
            }
        }

        /// <summary>
        /// Цветовая палитра.
        /// </summary>
        [Serializable]
        public sealed class ColorPalette {

            /// <summary>
            /// Инициализирует новый экземпляр <see cref="ColorPalette"/>.
            /// </summary>
            private ColorPalette() {
            }

            /// <summary>
            /// Создает и возвращает новый экземпляр <see cref="ColorPalette"/>.
            /// </summary>
            /// <param name="palette">Цветовая палитра.</param>
            /// <returns>Экземпляр <see cref="ColorPalette"/>.</returns>
            internal static ColorPalette Create(TwPalette8 palette) {
                TwainX.ColorPalette _result = new TwainX.ColorPalette {
                    PaletteType = palette.PaletteType,
                    Colors = new Color[palette.NumColors]
                };
                for(int i = 0; i < palette.NumColors; i++) {
                    _result.Colors[i] = palette.Colors[i];
                }
                return _result;
            }

            /// <summary>
            /// Возвращает тип палитры.
            /// </summary>
            public TwPA PaletteType { get; private set; }

            /// <summary>
            /// Возвращает цвета, входящие в состав палитры.
            /// </summary>
            public Color[] Colors { get; private set; }
        }

        /// <summary>
        /// Identifies the resource.
        /// </summary>
        [Serializable]
        [DebuggerDisplay("{Name}, Version = {Version}")]
        public sealed class Identity {

            /// <summary>
            /// Initializes a new instance of the <see cref="Identity"/> class.
            /// </summary>
            /// <param name="identity">The identity.</param>
            internal Identity(TwIdentity identity) {
                this.Family = identity.ProductFamily;
                this.Manufacturer = identity.Manufacturer;
                this.Name = identity.ProductName;
                this.ProtocolVersion = new Version(identity.ProtocolMajor, identity.ProtocolMinor);
                this.Version = new Version(identity.Version.MajorNum, identity.Version.MinorNum);
            }

            /// <summary>
            /// Get the version of the software.
            /// </summary>
            /// <value>
            /// The version.
            /// </value>
            public Version Version { get; private set; }

            /// <summary>
            /// Get the protocol version.
            /// </summary>
            /// <value>
            /// The protocol version.
            /// </value>
            public Version ProtocolVersion { get; private set; }

            /// <summary>
            /// Get manufacturer name, e.g. "Hewlett-Packard".
            /// </summary>
            public string Manufacturer { get; private set; }

            /// <summary>
            /// Get product family name, e.g. "ScanJet".
            /// </summary>
            public string Family { get; private set; }

            /// <summary>
            /// Get product name, e.g. "ScanJet Plus".
            /// </summary>
            public string Name { get; private set; }
        }

        #endregion
    }
}
