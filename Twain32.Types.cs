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
        #region Nested classes

        /// <summary>
        /// Entry points for working with DSM.
        /// <para xml:lang="ru">Точки входа для работы с DSM.</para>
        /// </summary>
        private sealed class _DsmEntry {

            /// <summary>
            /// Initializes a new instance of the class <see cref="_DsmEntry"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="_DsmEntry"/>.</para>
            /// </summary>
            /// <param name="ptr">Pointer to DSM_Entry.<para xml:lang="ru">Указатель на DSM_Entry.</para></param>
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
            /// Creates and returns a new instance of the class <see cref="_DsmEntry"/>.
            /// <para xml:lang="ru">Создает и возвращает новый экземпляр класса <see cref="_DsmEntry"/>.</para>
            /// </summary>
            /// <param name="ptr">Pointer to DSM_Entry.<para xml:lang="ru">Указатель на DSM_Entry.</para></param>
            /// <returns>Class instance <see cref="_DsmEntry"/>.<para xml:lang="ru">Экземпляр класса <see cref="_DsmEntry"/>.</para></returns>
            public static _DsmEntry Create(IntPtr ptr) => new _DsmEntry(ptr);

            /// <summary>
            /// Casts a pointer to the requested delegate.
            /// <para xml:lang="ru">Приводит указатель к требуемомы делегату.</para>
            /// </summary>
            /// <typeparam name="T">Требуемый делегат.</typeparam>
            /// <param name="ptr">Pointer to DSM_Entry.<para xml:lang="ru">Указатель на DSM_Entry.</para></param>
            /// <returns>Delegate.<para xml:lang="ru">Делегат.</para></returns>
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


        /// <summary>
        /// Win32 message filter.
        /// <para xml:lang="ru">Фильтр win32-сообщений.</para>
        /// </summary>
        private sealed class _MessageFilter : IMessageFilter, IDisposable {
            private Twain32 _twain;
            private bool _is_set_filter = false;
            private TwEvent _evtmsg = new TwEvent();

            public _MessageFilter(Twain32 twain) {
                this._twain = twain;
                this._evtmsg.EventPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINMSG)));
            }

            #region IMessageFilter

            public bool PreFilterMessage(ref Message m) {
                try {
                    if(this._twain._srcds.Id == 0) {
                        return false;
                    }
                    Marshal.StructureToPtr(new WINMSG { hwnd = m.HWnd, message = m.Msg, wParam = m.WParam, lParam = m.LParam }, this._evtmsg.EventPtr, true);
                    this._evtmsg.Message = TwMSG.Null;

                    switch(this._twain._dsmEntry.DsInvoke(this._twain._AppId, this._twain._srcds, TwDG.Control, TwDAT.Event, TwMSG.ProcessEvent, ref this._evtmsg)) {
                        case TwRC.DSEvent:
                            this._twain._TwCallbackProcCore(this._evtmsg.Message, isCloseReq => {
                                if(isCloseReq || this._twain.DisableAfterAcquire) {
                                    this._RemoveFilter();
                                    this._twain._DisableDataSource();
                                }
                            });
                            break;
                        case TwRC.NotDSEvent:
                            return false;
                        case TwRC.Failure:
                            throw new TwainException(this._twain._GetTwainStatus(), TwRC.Failure);
                        default:
                            throw new InvalidOperationException("Получен неверный код результата операции. Invalid a Return Code value.");
                    }
                } catch(TwainException ex) {
                    this._twain._OnAcquireError(new AcquireErrorEventArgs(ex));
                } catch(Exception ex) {
                    this._twain._OnAcquireError(new AcquireErrorEventArgs(new TwainException(ex.Message, ex)));
                }
                return true;
            }

            #endregion

            #region IDisposable

            public void Dispose() {
                if(this._evtmsg != null && this._evtmsg.EventPtr != IntPtr.Zero) {
                    Marshal.FreeHGlobal(this._evtmsg.EventPtr);
                    this._evtmsg.EventPtr = IntPtr.Zero;
                }
            }

            #endregion

            public void SetFilter() {
                if(!this._is_set_filter) {
                    this._is_set_filter = true;
                    Application.AddMessageFilter(this);
                }
            }

            private void _RemoveFilter() {
                Application.RemoveMessageFilter(this);
                this._is_set_filter = false;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 2)]
            internal struct WINMSG {
                public IntPtr hwnd;
                public int message;
                public IntPtr wParam;
                public IntPtr lParam;
            }
        }

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

        /// <summary>
        /// Range of values.
        /// <para xml:lang="ru">Диапазон значений.</para>
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
            /// Creates and returns an instance <see cref="Range"/>.
            /// <para xml:lang="ru">Создает и возвращает экземпляр <see cref="Range"/>.</para>
            /// </summary>
            /// <param name="range">Instance <see cref="TwRange"/>.<para xml:lang="ru">Экземпляр <see cref="TwRange"/>.</para></param>
            /// <returns>Instance <see cref="Range"/>.<para xml:lang="ru">Экземпляр <see cref="Range"/>.</para></returns>
            internal static Range CreateRange(TwRange range) => new Range(range);

            /// <summary>
            /// Creates and returns an instance <see cref="Range"/>.
            /// <para xml:lang="ru">Создает и возвращает экземпляр <see cref="Range"/>.</para>
            /// </summary>
            /// <param name="minValue">Minimum value.<para xml:lang="ru">Минимальное значение.</para></param>
            /// <param name="maxValue">The maximum value.<para xml:lang="ru">Максимальное значение.</para></param>
            /// <param name="stepSize">Step.<para xml:lang="ru">Шаг.</para></param>
            /// <param name="defaultValue">The default value.<para xml:lang="ru">Значение по умолчанию.</para></param>
            /// <param name="currentValue">Present value.<para xml:lang="ru">Текущее значение.</para></param>
            /// <returns>Instance <see cref="Range"/>.<para xml:lang="ru">Экземпляр <see cref="Range"/>.</para></returns>
            public static Range CreateRange(object minValue, object maxValue, object stepSize, object defaultValue, object currentValue) => new Range() {
                MinValue = minValue, MaxValue = maxValue, StepSize = stepSize, DefaultValue = defaultValue, CurrentValue = currentValue
            };

            /// <summary>
            /// Gets or sets the minimum value.
            /// <para xml:lang="ru">Возвращает или устанавливает минимальное значение.</para>
            /// </summary>
            public object MinValue { get; set; }

            /// <summary>
            /// Gets or sets the maximum value.
            /// <para xml:lang="ru">Возвращает или устанавливает максимальное значение.</para>
            /// </summary>
            public object MaxValue { get; set; }

            /// <summary>
            /// Gets or sets the step.
            /// <para xml:lang="ru">Возвращает или устанавливает шаг.</para>
            /// </summary>
            public object StepSize { get; set; }

            /// <summary>
            /// Gets or sets the default value.
            /// <para xml:lang="ru">Возвращает или устанавливает значае по умолчанию.</para>
            /// </summary>
            public object DefaultValue { get; set; }

            /// <summary>
            /// Gets or sets the current value.
            /// <para xml:lang="ru">Возвращает или устанавливает текущее значение.</para>
            /// </summary>
            public object CurrentValue { get; set; }

            /// <summary>
            /// Converts an instance of a class to an instance <see cref="TwRange"/>.
            /// <para xml:lang="ru">Конвертирует экземпляр класса в экземпляр <see cref="TwRange"/>.</para>
            /// </summary>
            /// <returns>Instance <see cref="TwRange"/>.<para xml:lang="ru">Экземпляр <see cref="TwRange"/>.</para></returns>
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
        /// Enumeration.
        /// <para xml:lang="ru">Перечисление.</para>
        /// </summary>
        [Serializable]
        public sealed class Enumeration {
            private object[] _items;

            /// <summary>
            /// Prevents a default instance of the <see cref="Enumeration"/> class from being created.
            /// </summary>
            /// <param name="items">Listing items.<para xml:lang="ru">Элементы перечисления.</para></param>
            /// <param name="currentIndex">Current index.<para xml:lang="ru">Текущий индекс.</para></param>
            /// <param name="defaultIndex">The default index.<para xml:lang="ru">Индекс по умолчанию.</para></param>
            private Enumeration(object[] items, int currentIndex, int defaultIndex) {
                this._items = items;
                this.CurrentIndex = currentIndex;
                this.DefaultIndex = defaultIndex;
            }

            /// <summary>
            /// Creates and returns an instance <see cref="Enumeration"/>.
            /// <para xml:lang="ru">Создает и возвращает экземпляр <see cref="Enumeration"/>.</para>
            /// </summary>
            /// <param name="items">Listing items.<para xml:lang="ru">Элементы перечисления.</para></param>
            /// <param name="currentIndex">Current index.<para xml:lang="ru">Текущий индекс.</para></param>
            /// <param name="defaultIndex">The default index.<para xml:lang="ru">Индекс по умолчанию.</para></param>
            /// <returns>Instance <see cref="Enumeration"/>.<para xml:lang="ru">Экземпляр <see cref="Enumeration"/>.</para></returns>
            public static Enumeration CreateEnumeration(object[] items, int currentIndex, int defaultIndex) => new Enumeration(items, currentIndex, defaultIndex);

            /// <summary>
            /// Returns the number of items.
            /// <para xml:lang="ru">Возвращает количество элементов.</para>
            /// </summary>
            public int Count => this._items.Length;

            /// <summary>
            /// Returns the current index.
            /// <para xml:lang="ru">Возвращает текущий индекс.</para>
            /// </summary>
            public int CurrentIndex { get; private set; }

            /// <summary>
            /// Returns the default index.
            /// <para xml:lang="ru">Возвращает индекс по умолчанию.</para>
            /// </summary>
            public int DefaultIndex { get; private set; }

            /// <summary>
            /// Returns the element at the specified index.
            /// <para xml:lang="ru">Возвращает элемент по указанному индексу.</para>
            /// </summary>
            /// <param name="index">Index.<para xml:lang="ru">Индекс.</para></param>
            /// <returns>The item at the specified index.<para xml:lang="ru">Элемент по указанному индексу.</para></returns>
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
            /// Creates and returns an instance <see cref="Enumeration"/>.
            /// <para xml:lang="ru">Создает и возвращает экземпляр <see cref="Enumeration"/>.</para>
            /// </summary>
            /// <param name="value">Instance <see cref="Range"/>.<para xml:lang="ru">Экземпляр <see cref="Range"/>.</para></param>
            /// <returns>Instance <see cref="Enumeration"/>.<para xml:lang="ru">Экземпляр <see cref="Enumeration"/>.</para></returns>
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
            /// Creates and returns an instance <see cref="Enumeration"/>.
            /// <para xml:lang="ru">Создает и возвращает экземпляр <see cref="Enumeration"/>.</para>
            /// </summary>
            /// <param name="value">An array of values.<para xml:lang="ru">Массив значений.</para></param>
            /// <returns>Instance <see cref="Enumeration"/>.<para xml:lang="ru">Экземпляр <see cref="Enumeration"/>.</para></returns>
            public static Enumeration FromArray(object[] value) => Enumeration.CreateEnumeration(value, 0, 0);

            /// <summary>
            /// Creates and returns an instance <see cref="Enumeration"/>.
            /// <para xml:lang="ru">Создает и возвращает экземпляр <see cref="Enumeration"/>.</para>
            /// </summary>
            /// <param name="value">Value.<para xml:lang="ru">Значение.</para></param>
            /// <returns>Instance <see cref="Enumeration"/>.<para xml:lang="ru">Экземпляр <see cref="Enumeration"/>.</para></returns>
            public static Enumeration FromOneValue(ValueType value) {
                return Enumeration.CreateEnumeration(new object[] { value }, 0, 0);
            }

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

        /// <summary>
        /// A set of operations for working with a color palette.
        /// <para xml:lang="ru">Набор операций для работы с цветовой палитрой.</para>
        /// </summary>
        public sealed class TwainPalette : MarshalByRefObject {
            private Twain32 _twain;

            /// <summary>
            /// Initializes a new instance of the class <see cref="TwainPalette"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="TwainPalette"/>.</para>
            /// </summary>
            /// <param name="twain">Class instance <see cref="TwainPalette"/>.<para xml:lang="ru">Экземпляр класса <see cref="TwainPalette"/>.</para></param>
            internal TwainPalette(Twain32 twain) {
                this._twain = twain;
            }

            /// <summary>
            /// Returns the current color palette.
            /// <para xml:lang="ru">Возвращает текущую цветовую палитру.</para>
            /// </summary>
            /// <returns>Class instance <see cref="TwainPalette"/>.<para xml:lang="ru">Экземпляр класса <see cref="TwainPalette"/>.</para></returns>
            public ColorPalette Get() {
                TwPalette8 _palette = new TwPalette8();
                TwRC _rc = this._twain._dsmEntry.DsInvoke(this._twain._AppId, this._twain._srcds, TwDG.Image, TwDAT.Palette8, TwMSG.Get, ref _palette);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(), _rc);
                }
                return _palette;
            }

            /// <summary>
            /// Returns the current default color palette.
            /// <para xml:lang="ru">Возвращает текущую цветовую палитру, используемую по умолчанию.</para>
            /// </summary>
            /// <returns>Class instance <see cref="TwainPalette"/>.<para xml:lang="ru">Экземпляр класса <see cref="TwainPalette"/>.</para></returns>
            public ColorPalette GetDefault() {
                TwPalette8 _palette = new TwPalette8();
                TwRC _rc = this._twain._dsmEntry.DsInvoke(this._twain._AppId, this._twain._srcds, TwDG.Image, TwDAT.Palette8, TwMSG.GetDefault, ref _palette);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(), _rc);
                }
                return _palette;
            }

            /// <summary>
            /// Resets the current color palette and sets the specified one.
            /// <para xml:lang="ru">Сбрасывает текущую цветовую палитру и устанавливает указанную.</para>
            /// </summary>
            /// <param name="palette">Class instance <see cref="TwainPalette"/>.<para xml:lang="ru">Экземпляр класса <see cref="TwainPalette"/>.</para></param>
            public void Reset(ColorPalette palette) {
                TwRC _rc = this._twain._dsmEntry.DsInvoke(this._twain._AppId, this._twain._srcds, TwDG.Image, TwDAT.Palette8, TwMSG.Reset, ref palette);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(), _rc);
                }
            }

            /// <summary>
            /// Sets the specified color palette.
            /// <para xml:lang="ru">Устанавливает указанную цветовую палитру.</para>
            /// </summary>
            /// <param name="palette">Class instance <see cref="TwainPalette"/>.<para xml:lang="ru">Экземпляр класса <see cref="TwainPalette"/>.</para></param>
            public void Set(ColorPalette palette) {
                TwRC _rc = this._twain._dsmEntry.DsInvoke(this._twain._AppId, this._twain._srcds, TwDG.Image, TwDAT.Palette8, TwMSG.Set, ref palette);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(), _rc);
                }
            }
        }

        /// <summary>
        /// Color palette.
        /// <para xml:lang="ru">Цветовая палитра.</para>
        /// </summary>
        [Serializable]
        public sealed class ColorPalette {

            /// <summary>
            /// Initializes a new instance <see cref="ColorPalette"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр <see cref="ColorPalette"/>.</para>
            /// </summary>
            private ColorPalette() {
            }

            /// <summary>
            /// Creates and returns a new instance <see cref="ColorPalette"/>.
            /// <para xml:lang="ru">Создает и возвращает новый экземпляр <see cref="ColorPalette"/>.</para>
            /// </summary>
            /// <param name="palette">Color palette.<para xml:lang="ru">Цветовая палитра.</para></param>
            /// <returns>Instance <see cref="ColorPalette"/>.<para xml:lang="ru">Экземпляр <see cref="ColorPalette"/>.</para></returns>
            internal static ColorPalette Create(TwPalette8 palette) {
                Twain32.ColorPalette _result = new Twain32.ColorPalette {
                    PaletteType = palette.PaletteType,
                    Colors = new Color[palette.NumColors]
                };
                for(int i = 0; i < palette.NumColors; i++) {
                    _result.Colors[i] = palette.Colors[i];
                }
                return _result;
            }

            /// <summary>
            /// Returns the type of palette.
            /// <para xml:lang="ru">Возвращает тип палитры.</para>
            /// </summary>
            public TwPA PaletteType { get; private set; }

            /// <summary>
            /// Returns the colors that make up the palette.
            /// <para xml:lang="ru">Возвращает цвета, входящие в состав палитры.</para>
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
