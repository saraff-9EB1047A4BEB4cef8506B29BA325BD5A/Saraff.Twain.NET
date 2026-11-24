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
        private _DsmEntry _dsmEntry;
        private IntPtr _hTwainDll; //module descriptor twain_32.dll / дескриптор модуля twain_32.dll
        private IContainer _components = new Container();
        private IntPtr _hwnd; //handle to the parent window / дескриптор родительского окна.
        private TwIdentity _appid; //application identifier / идентификатор приложения.
        private TwIdentity _srcds; //identifier of the current data source / идентификатор текущего источника данных.
        private _MessageFilter _filter; //WIN32 event filter / фильтр событий WIN32
        private TwIdentity[] _sources = new TwIdentity[0]; //an array of available data sources / массив доступных источников данных.
        private ApplicationContext _context = null; //application context. used if there is no main message processing cycle / контекст приложения. используется в случае отсутствия основного цикла обработки сообщений.
        private Collection<_Image> _images = new Collection<_Image>();
        private readonly IImageSink<_Image> _imageSink;

        private bool _isTwain2Enable = IntPtr.Size != 4 || _Platform.Platform == PlatformID.Unix || _Platform.Platform == PlatformID.MacOSX;
        private CallBackProc _callbackProc;
        private TwainCapabilities _capabilities;
        private int _threadId;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Twain32"/> class.
        /// </summary>
        public Twain32() {
            this._srcds = new TwIdentity();
            this._srcds.Id = 0;
            this._imageSink = new CollectionImageSink<_Image>(this._images);
            this._filter = new _MessageFilter(this);
            this.ShowUI = true;
            this.DisableAfterAcquire = true;
            this.Palette = new TwainPalette(this);
            this._callbackProc = this._TwCallbackProc;
            switch(_Platform.Platform) {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    break;
                default:
                    Form _window = new Form();
                    this._components.Add(_window);
                    this._hwnd = _window.Handle;
                    break;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Twain32"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public Twain32(IContainer container) : this() {
            container.Add(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.ComponentModel.Component"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing) {
            if(disposing && !this._disposed) {
                this._disposed = true;
                try {
                    if(this._threadId == 0 || this._threadId == Environment.CurrentManagedThreadId) {
                        try {
                            this.CloseDSM();
                        } finally {
                            switch(_Platform.Platform) {
                                case PlatformID.Unix:
                                case PlatformID.MacOSX:
                                    break;
                                default:
                                    this._filter.Dispose();
                                    break;
                            }
                            this._UnloadDSM();
                        }
                    } else {
                        Trace.TraceWarning("Saraff.Twain: Dispose was called from a thread other than the TWAIN thread. Native DSM resources are not released.");
                    }
                } finally {
                    if(this._components != null) {
                        this._components.Dispose();
                    }
                    base.Dispose(disposing);
                }
            } else {
                base.Dispose(disposing);
            }
        }






        private void _LoadDsm() {
            switch(_Platform.Platform) {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    if(!string.IsNullOrEmpty(this.DsmPath)) {
                        this._hTwainDll = _Platform.Load(this.DsmPath);
                        if(this._hTwainDll == IntPtr.Zero) {
                            throw new TwainException("Can't load DSM: " + this.DsmPath);
                        }
                        IntPtr _pDsmEntry = _Platform.GetProcAddr(this._hTwainDll, "DSM_Entry");
                        if(_pDsmEntry == IntPtr.Zero) {
                            _Platform.Unload(this._hTwainDll);
                            this._hTwainDll = IntPtr.Zero;
                            throw new TwainException("Can't find DSM_Entry entry point.");
                        }
                        this._dsmEntry = _DsmEntry.Create(_pDsmEntry);
                    } else {
                        this._dsmEntry = _DsmEntry.Create(IntPtr.Zero);
                    }
                    try {
                        if(this._dsmEntry.DsmRaw == null) {
                            throw new InvalidOperationException("Can't load DSM.");
                        }
                    } catch(Exception ex) {
                        throw new TwainException("Can't load DSM.", ex);
                    }
                    break;
                default:
                    string _twainDsm = this.DsmPath;
                    if(string.IsNullOrEmpty(_twainDsm)) {
                        _twainDsm = Path.ChangeExtension(Path.Combine(Environment.SystemDirectory, "TWAINDSM"), ".dll");
                        _twainDsm = File.Exists(_twainDsm) && this.IsTwain2Enable ? _twainDsm : Path.ChangeExtension(Path.Combine(Environment.SystemDirectory, "..\\twain_32"), ".dll");
                    }
                    this._hTwainDll = _Platform.Load(_twainDsm);
                    if(this.Parent != null) {
                        this._hwnd = this.Parent.Handle;
                    }
                    if(this._hTwainDll != IntPtr.Zero) {
                        IntPtr _pDsmEntry = _Platform.GetProcAddr(this._hTwainDll, "DSM_Entry");
                        if(_pDsmEntry != IntPtr.Zero) {
                            this._dsmEntry = _DsmEntry.Create(_pDsmEntry);
                            _Memory._SetEntryPoints(null);
                        } else {
                            throw new TwainException("Can't find DSM_Entry entry point.");
                        }
                    } else {
                        throw new TwainException("Can't load DSM.");
                    }
                    break;
            }
        }

        private void _OnDataSourceOpened() {
            switch(_Platform.Platform) {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    this._RegisterCallback();
                    break;
                default:
                    if(this.IsTwain2Supported && (this._srcds.SupportedGroups & TwDG.DS2) != 0) {
                        this._RegisterCallback();
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets an image from a data source.
        /// <para xml:lang="ru">Получает изображение с источника данных.</para>
        /// </summary>
        public void Acquire() {
            if(this.OpenDSM()) {
                if(this.OpenDataSource()) {
                    if(this._EnableDataSource()) {
                        switch(_Platform.Platform) {
                            case PlatformID.Unix:
                            case PlatformID.MacOSX:
                                break;
                            default:
                                if(!this.IsTwain2Supported || (this._srcds.SupportedGroups & TwDG.DS2) == 0) {
                                    this._filter.SetFilter();
                                }
                                if(!Application.MessageLoop) {
                                    Application.Run(this._context = new ApplicationContext());
                                }
                                break;
                        }
                    }
                }
            }
        }






        #region DG_CONTROL / DAT_CUSTOMDSDATA / MSG_xxx

        /// <summary>
        /// Get or set the Custom Data (DG_CONTROL / DAT_CUSTOMDSDATA / MSG_xxx).
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public byte[] CustomDSData {
            get {
                if(this._session.IsDsOpen) {
                    TwCustomDSData _data = new TwCustomDSData { };
                    TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.CustomDSData, TwMSG.Get, ref _data);
                    if(_rc != TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    using(TwainDsmBuffer _buffer = TwainDsmBuffer.FromPointer(_data.hData)) {
                        byte[] _value = new byte[_data.InfoLength];
                        IntPtr _ptr = _Memory.Lock(_buffer.Handle);
                        try {
                            Marshal.Copy(_ptr, _value, 0, _value.Length);
                        } finally {
                            _Memory.Unlock(_buffer.Handle);
                        }
                        return _value;
                    }
                } else {
                    throw new TwainException("Источник данных не открыт.");
                }
            }
            set {
                if(this._session.IsDsOpen) {
                    using(TwainDsmBuffer _buffer = TwainDsmBuffer.Alloc(value.Length)) {
                        if(_buffer.IsInvalid) {
                            throw new TwainException("Error allocating memory.");
                        }
                        TwCustomDSData _data = new TwCustomDSData {
                            InfoLength = (uint)value.Length,
                            hData = _buffer.Handle
                        };
                        IntPtr _ptr = _Memory.Lock(_buffer.Handle);
                        try {
                            Marshal.Copy(value, 0, _ptr, value.Length);
                        } finally {
                            _Memory.Unlock(_buffer.Handle);
                        }
                        TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.CustomDSData, TwMSG.Set, ref _data);
                        if(_rc != TwRC.Success) {
                            throw new TwainException(this._GetTwainStatus(), _rc);
                        }
                    }
                } else {
                    throw new TwainException("Источник данных не открыт.");
                }
            }
        }

        #endregion

        #region DS events handler

        /// <summary>
        /// A data source event handler.
        /// <para xml:lang="ru">Обработчик событий источника данных.</para>
        /// </summary>
        /// <param name="appId">Description of the application.<para xml:lang="ru">Описание приложения.</para></param>
        /// <param name="srcId">Description of the data source.<para xml:lang="ru">Описание источника данных.</para></param>
        /// <param name="dg">Description of the data group.<para xml:lang="ru">Описание группы данных.</para></param>
        /// <param name="dat">Description of the data.<para xml:lang="ru">Описание данных.</para></param>
        /// <param name="msg">Message.<para xml:lang="ru">Сообщение.</para></param>
        /// <param name="data">Data.<para xml:lang="ru">Данные.</para></param>
        /// <returns>Result event handlers.<para xml:lang="ru">Результат обработники события.</para></returns>
        private TwRC _TwCallbackProc(TwIdentity srcId, TwIdentity appId, TwDG dg, TwDAT dat, TwMSG msg, IntPtr data) {
            try {
                if(appId == null || appId.Id != this._AppId.Id) {
                    return TwRC.Failure;
                }

                if(!this._session.IsDsEnabled) {
                    this._session.SetEnabledAndReady(); this._SyncTwainState();
                }

                this._TwCallbackProcCore(msg, isCloseReq => {
                    if(isCloseReq || this.DisableAfterAcquire) {
                        this._DisableDataSource();
                    }
                });
            } catch(Exception ex) {
                this._OnAcquireError(new AcquireErrorEventArgs(new TwainException(ex.Message, ex)));
            }
            return TwRC.Success;
        }

        /// <summary>
        /// An internal data source event handler.
        /// <para xml:lang="ru">Внутренний обработчик событий источника данных.</para>
        /// </summary>
        /// <param name="msg">Message.<para xml:lang="ru">Сообщение.</para></param>
        /// <param name="endAction">The action that completes the processing of the event.<para xml:lang="ru">Действие, завершающее обработку события.</para></param>
        private void _TwCallbackProcCore(TwMSG msg, Action<bool> endAction) {
            try {
                switch(msg) {
                    case TwMSG.XFerReady:
                        switch(this.Capabilities.XferMech.GetCurrent()) {
                            case TwSX.File:
                                this._FileTransferPictures();
                                break;
                            case TwSX.Memory:
                                this._MemoryTransferPictures(false);
                                break;
                            case TwSX.MemFile:
                                this._MemoryTransferPictures(true);
                                break;
                            default:
                                this._NativeTransferPictures();
                                break;
                        }
                        endAction(false);
                        this._OnAcquireCompleted(new EventArgs());
                        break;
                    case TwMSG.CloseDSReq:
                        endAction(true);
                        break;
                    case TwMSG.CloseDSOK:
                        endAction(false);
                        break;
                    case TwMSG.DeviceEvent:
                        this._DeviceEventObtain();
                        break;
                }
            } catch(TwainException ex) {
                try {
                    endAction(false);
                } catch {
                }
                this._OnAcquireError(new AcquireErrorEventArgs(ex));
            } catch {
                try {
                    endAction(false);
                } catch {
                }
                throw;
            }
        }

        private void _DeviceEventObtain() {
            TwDeviceEvent _deviceEvent = new TwDeviceEvent();
            if(this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.DeviceEvent, TwMSG.Get, ref _deviceEvent) == TwRC.Success) {
                this._OnDeviceEvent(new DeviceEventEventArgs(_deviceEvent));
            }
        }

        #endregion

        /// <summary>
        /// Gets a description of all available data sources.
        /// <para xml:lang="ru">Получает описание всех доступных источников данных.</para>
        /// </summary>
        private void _GetAllSorces() {
            List<TwIdentity> _src = new List<TwIdentity>();
            TwIdentity _item = new TwIdentity();
            try {
                for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.GetFirst, ref _item); _rc != TwRC.Success;) {
                    if(_rc == TwRC.EndOfList) {
                        return;
                    }
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                _src.Add(_item);
                while(true) {
                    _item = new TwIdentity();
                    TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.GetNext, ref _item);
                    if(_rc == TwRC.Success) {
                        _src.Add(_item);
                        continue;
                    }
                    if(_rc == TwRC.EndOfList) {
                        break;
                    }
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.GetDefault, ref _srcds); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
            } finally {
                this._sources = _src.ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the value of the status flags.
        /// <para xml:lang="ru">Возвращает или устанавливает значение флагов состояния.</para>
        /// </summary>
        /// <summary>
        /// Returns the TWAIN status code.
        /// <para xml:lang="ru">Возвращает код состояния TWAIN.</para>
        /// </summary>
        /// <returns></returns>
        private TwCC _GetTwainStatus() {
            TwStatus _status = new TwStatus();
            TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.Status, TwMSG.Get, ref _status);
            return _status.ConditionCode;
        }

        /// <summary>
        /// Returns a description of the received image.
        /// <para xml:lang="ru">Возвращает описание полученного изображения.</para>
        /// </summary>
        /// <returns>Description of the image.<para xml:lang="ru">Описание изображения.</para></returns>
        private ImageInfo _GetImageInfo() {
            TwImageInfo _imageInfo = new TwImageInfo();
            TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Image, TwDAT.ImageInfo, TwMSG.Get, ref _imageInfo);
            if(_rc != TwRC.Success) {
                throw new TwainException(this._GetTwainStatus(), _rc);
            }
            return ImageInfo.FromTwImageInfo(_imageInfo);
        }

        /// <summary>
        /// Returns an extended description of the resulting image.
        /// <para xml:lang="ru">Возвращает расширенного описание полученного изображения.</para>
        /// </summary>
        /// <param name="extInfo">A set of codes for the extended image description for which you want to get a description.<para xml:lang="ru">Набор кодов расширенного описания изображения для которых требуется получить описание.</para></param>
        /// <returns>Extended image description.<para xml:lang="ru">Расширенное описание изображения.</para></returns>
        private ExtImageInfo _GetExtImageInfo(TwEI[] extInfo) {
            TwInfo[] _info = new TwInfo[extInfo.Length];
            for(int i = 0; i < extInfo.Length; i++) {
                _info[i] = new TwInfo { InfoId = extInfo[i] };
            }
            using(TwainHGlobalBuffer _extImageInfo = TwainHGlobalBuffer.FromPointer(TwExtImageInfo.ToPtr(_info))) {
                IntPtr _extImageInfoPtr = _extImageInfo.DangerousGetHandle();

                TwRC _rc = this._dsmEntry.DsRaw(this._AppId, this._srcds, TwDG.Image, TwDAT.ExtImageInfo, TwMSG.Get, _extImageInfoPtr);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                return ExtImageInfo.FromPtr(_extImageInfoPtr);
            }
        }

    }
}
