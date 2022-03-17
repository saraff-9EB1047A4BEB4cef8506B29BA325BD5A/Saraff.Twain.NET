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
using System.Reflection;
using System.Drawing;
using System.Diagnostics;
using System.IO;

namespace Saraff.Twain {

    /// <summary>
    /// Provides the ability to work with TWAIN sources.
    /// <para xml:lang="ru">Обеспечивает возможность работы с TWAIN-источниками.</para>
    /// </summary>
    [ToolboxBitmap(typeof(Twain32), "Resources.scanner.bmp")]
    [DebuggerDisplay("ProductName = {_appid.ProductName.Value}, Version = {_appid.Version.Info}, DS = {_srcds.ProductName}")]
    [DefaultEvent("AcquireCompleted")]
    [DefaultProperty("AppProductName")]
    public sealed class Twain32 : Component {
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
        private TwainStateFlag _twainState;
        private bool _isTwain2Enable = IntPtr.Size != 4 || Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;
        private CallBackProc _callbackProc;
        private TwainCapabilities _capabilities;

        /// <summary>
        /// Initializes a new instance of the <see cref="Twain32"/> class.
        /// </summary>
        public Twain32() {
            this._srcds = new TwIdentity();
            this._srcds.Id = 0;
            this._filter = new _MessageFilter(this);
            this.ShowUI = true;
            this.DisableAfterAcquire = true;
            this.Palette = new TwainPalette(this);
            this._callbackProc = this._TwCallbackProc;
            switch(Environment.OSVersion.Platform) {
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
            if(disposing) {
                this.CloseDSM();
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        break;
                    default:
                        this._filter.Dispose();
                        break;
                }
                this._UnloadDSM();
                if(this._components != null) {
                    this._components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Opens the data source manager
        /// <para xml:lang="ru">Открывает менеджер источников данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.<para xml:lang="ru">Истина, если операция прошла удачно; иначе, лож.</para></returns>
        public bool OpenDSM() {
            if((this._TwainState & TwainStateFlag.DSMOpen) == 0) {

                #region We load DSM, we receive the address of the entry point DSM_Entry and we bring it to the appropriate delegates / Загружаем DSM, получаем адрес точки входа DSM_Entry и приводим ее к соответствующим делегатам

                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        this._dsmEntry = _DsmEntry.Create(IntPtr.Zero);
                        try {
                            if(this._dsmEntry.DsmRaw == null) {
                                throw new InvalidOperationException("Can't load DSM.");
                            }
                        } catch(Exception ex) {
                            throw new TwainException("Can't load DSM.", ex);
                        }
                        break;
                    default:
                        string _twainDsm = Path.ChangeExtension(Path.Combine(Environment.SystemDirectory, "TWAINDSM"), ".dll");
                        this._hTwainDll = _Platform.Load(File.Exists(_twainDsm) && this.IsTwain2Enable ? _twainDsm : Path.ChangeExtension(Path.Combine(Environment.SystemDirectory, "..\\twain_32"), ".dll"));
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


                #endregion

                for(TwRC _rc = this._dsmEntry.DsmParent(this._AppId, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.OpenDSM, ref this._hwnd); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                this._TwainState |= TwainStateFlag.DSMOpen;

                if(this.IsTwain2Supported) {
                    TwEntryPoint _entry = new TwEntryPoint();
                    for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.EntryPoint, TwMSG.Get, ref _entry); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    _Memory._SetEntryPoints(_entry);
                }

                this._GetAllSorces();
            }
            return (this._TwainState & TwainStateFlag.DSMOpen) != 0;
        }

        /// <summary>
        /// Displays a dialog box for selecting a data source.
        /// <para xml:lang="ru">Отображает диалоговое окно для выбора источника данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.<para xml:lang="ru">Истина, если операция прошла удачно; иначе, лож.</para></returns>
        public bool SelectSource() {
            if(Environment.OSVersion.Platform == PlatformID.Unix) {
                throw new NotSupportedException("DG_CONTROL / DAT_IDENTITY / MSG_USERSELECT is not available on Linux.");
            }
            if((this._TwainState & TwainStateFlag.DSOpen) == 0) {
                if((this._TwainState & TwainStateFlag.DSMOpen) == 0) {
                    this.OpenDSM();
                    if((this._TwainState & TwainStateFlag.DSMOpen) == 0) {
                        return false;
                    }
                }
                TwIdentity _src = new TwIdentity();
                for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.UserSelect, ref _src); _rc != TwRC.Success;) {
                    if(_rc == TwRC.Cancel) {
                        return false;
                    }
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                this._srcds = _src;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Opens a data source.
        /// <para xml:lang="ru">Открывает источник данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.<para xml:lang="ru">Истина, если операция прошла удачно; иначе, лож.</para></returns>
        public bool OpenDataSource() {
            if((this._TwainState & TwainStateFlag.DSMOpen) != 0 && (this._TwainState & TwainStateFlag.DSOpen) == 0) {
                for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.OpenDS, ref this._srcds); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                this._TwainState |= TwainStateFlag.DSOpen;

                switch(Environment.OSVersion.Platform) {
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
            return (this._TwainState & TwainStateFlag.DSOpen) != 0;
        }

        /// <summary>
        /// Registers a data source event handler.
        /// <para xml:lang="ru">Регестрирует обработчик событий источника данных.</para>
        /// </summary>
        private void _RegisterCallback() {
            TwCallback2 _callback = new TwCallback2 {
                CallBackProc = this._callbackProc
            };
            TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.Callback2, TwMSG.RegisterCallback, ref _callback);
            if(_rc != TwRC.Success) {
                throw new TwainException(this._GetTwainStatus(), _rc);
            }
        }

        /// <summary>
        /// Activates a data source.
        /// <para xml:lang="ru">Активирует источник данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.<para xml:lang="ru">Истина, если операция прошла удачно; иначе, лож.</para></returns>
        private bool _EnableDataSource() {
            if((this._TwainState & TwainStateFlag.DSOpen) != 0 && (this._TwainState & TwainStateFlag.DSEnabled) == 0) {
                TwUserInterface _guif = new TwUserInterface() {
                    ShowUI = this.ShowUI,
                    ModalUI = this.ModalUI,
                    ParentHand = this._hwnd
                };
                for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.UserInterface, TwMSG.EnableDS, ref _guif); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                if((this._TwainState & TwainStateFlag.DSReady) != 0) {
                    this._TwainState &= ~TwainStateFlag.DSReady;
                } else {
                    this._TwainState |= TwainStateFlag.DSEnabled;
                }
            }
            return (this._TwainState & TwainStateFlag.DSEnabled) != 0;
        }

        /// <summary>
        /// Gets an image from a data source.
        /// <para xml:lang="ru">Получает изображение с источника данных.</para>
        /// </summary>
        public void Acquire() {
            if(this.OpenDSM()) {
                if(this.OpenDataSource()) {
                    if(this._EnableDataSource()) {
                        switch(Environment.OSVersion.Platform) {
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

        /// <summary>
        /// Deactivates the data source.
        /// <para xml:lang="ru">Деактивирует источник данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.<para xml:lang="ru">Истина, если операция прошла удачно; иначе, лож.</para></returns>
        private bool _DisableDataSource() {
            if((this._TwainState & TwainStateFlag.DSEnabled) != 0) {
                try {
                    TwUserInterface _guif = new TwUserInterface() {
                        ParentHand = this._hwnd,
                        ShowUI = false
                    };
                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.UserInterface, TwMSG.DisableDS, ref _guif); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                } finally {
                    this._TwainState &= ~TwainStateFlag.DSEnabled;
                    if(this._context != null) {
                        this._context.ExitThread();
                        this._context.Dispose();
                        this._context = null;
                    }
                }
                return (this._TwainState & TwainStateFlag.DSEnabled) == 0;
            }
            return false;
        }

        /// <summary>
        /// Closes the data source.
        /// <para xml:lang="ru">Закрывает источник данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.<para xml:lang="ru">Истина, если операция прошла удачно; иначе, лож.</para></returns>
        public bool CloseDataSource() {
            if((this._TwainState & TwainStateFlag.DSOpen) != 0 && (this._TwainState & TwainStateFlag.DSEnabled) == 0) {
                this._images.Clear();
                for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.CloseDS, ref this._srcds); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                this._TwainState &= ~TwainStateFlag.DSOpen;
                return (this._TwainState & TwainStateFlag.DSOpen) == 0;
            }
            return false;
        }

        /// <summary>
        /// Closes the data source manager.
        /// <para xml:lang="ru">Закрывает менежер источников данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.<para xml:lang="ru">Истина, если операция прошла удачно; иначе, лож.</para></returns>
        public bool CloseDSM() {
            if((this._TwainState & TwainStateFlag.DSEnabled) != 0) {
                this._DisableDataSource();
            }
            if((this._TwainState & TwainStateFlag.DSOpen) != 0) {
                this.CloseDataSource();
            }
            if((this._TwainState & TwainStateFlag.DSMOpen) != 0 && (this._TwainState & TwainStateFlag.DSOpen) == 0) {
                for(TwRC _rc = this._dsmEntry.DsmParent(this._AppId, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.CloseDSM, ref this._hwnd); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                this._TwainState &= ~TwainStateFlag.DSMOpen;
                this._UnloadDSM();
                return (this._TwainState & TwainStateFlag.DSMOpen) == 0;
            }
            return false;
        }

        private void _UnloadDSM() {
            this._AppId = null;
            if(this._hTwainDll != IntPtr.Zero) {
                _Platform.Unload(this._hTwainDll);
                this._hTwainDll = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Returns the scanned image.
        /// <para xml:lang="ru">Возвращает отсканированое изображение.</para>
        /// </summary>
        /// <param name="index">Image index.<para xml:lang="ru">Индекс изображения.</para></param>
        /// <returns>Instance of the image.<para xml:lang="ru">Экземпляр изображения.</para></returns>
        public Image GetImage(int index) => this._images[index];

        /// <summary>
        /// Returns the number of scanned images.
        /// <para xml:lang="ru">Возвращает количество отсканированных изображений.</para>
        /// </summary>
        [Browsable(false)]
        public int ImageCount => this._images.Count;

        /// <summary>
        /// Gets or sets a value indicating the need to deactivate the data source after receiving the image.
        /// <para xml:lang="ru">Возвращает или устанавливает значение, указывающее на необходимость деактивации источника данных после получения изображения.</para>
        /// </summary>
        [DefaultValue(true)]
        [Category("Behavior")]
        [Description("Gets or sets a value indicating the need to deactivate the data source after receiving the image. Возвращает или устанавливает значение, указывающее на необходимость деактивации источника данных после получения изображения.")]
        public bool DisableAfterAcquire { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use TWAIN 2.0.
        /// <para xml:lang="ru">Возвращает или устанавливает значение, указывающее на необходимость использования TWAIN 2.0.</para>
        /// </summary>
        [DefaultValue(false)]
        [Category("Behavior")]
        [Description("Gets or sets a value indicating whether to use TWAIN 2.0. Возвращает или устанавливает значение, указывающее на необходимость использования TWAIN 2.0.")]
        public bool IsTwain2Enable {
            get {
                return this._isTwain2Enable;
            }
            set {
                if((this._TwainState & TwainStateFlag.DSMOpen) != 0) {
                    throw new InvalidOperationException("DSM already opened.");
                }
                if(IntPtr.Size != 4 && !value) {
                    throw new InvalidOperationException("In x64 mode only TWAIN 2.x enabled.");
                }
                if(Environment.OSVersion.Platform == PlatformID.Unix && !value) {
                    throw new InvalidOperationException("On UNIX platform only TWAIN 2.x enabled.");
                }
                if(Environment.OSVersion.Platform == PlatformID.MacOSX && !value) {
                    throw new InvalidOperationException("On MacOSX platform only TWAIN 2.x enabled.");
                }
                if(this._isTwain2Enable = value) {
                    this._AppId.SupportedGroups |= TwDG.APP2;
                } else {
                    this._AppId.SupportedGroups &= ~TwDG.APP2;
                }
                this._AppId.ProtocolMajor = (ushort)(this._isTwain2Enable ? 2 : 1);
                this._AppId.ProtocolMinor = (ushort)(this._isTwain2Enable ? 3 : 9);
            }
        }

        /// <summary>
        /// Returns true if DSM supports TWAIN 2.0; otherwise false.
        /// <para xml:lang="ru">Возвращает истину, если DSM поддерживает TWAIN 2.0; иначе лож.</para>
        /// </summary>
        [Browsable(false)]
        public bool IsTwain2Supported {
            get {
                if((this._TwainState & TwainStateFlag.DSMOpen) == 0) {
                    throw new InvalidOperationException("DSM is not open.");
                }
                return (this._AppId.SupportedGroups & TwDG.DSM2) != 0;
            }
        }

        #region Information of sorces

        /// <summary>
        /// Gets or sets the index of the current data source.
        /// <para xml:lang="ru">Возвращает или устанавливает индекс текущего источника данных.</para>
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public int SourceIndex {
            get {
                if((this._TwainState & TwainStateFlag.DSMOpen) != 0) {
                    int i;
                    for(i = 0; i < this._sources.Length; i++) {
                        if(this._sources[i].Equals(this._srcds)) {
                            break;
                        }
                    }
                    return i;
                } else {
                    return -1;
                }
            }
            set {
                if((this._TwainState & TwainStateFlag.DSMOpen) != 0) {
                    if((this._TwainState & TwainStateFlag.DSOpen) == 0) {
                        this._srcds = this._sources[value];
                    } else {
                        throw new TwainException("The data source is already open. Источник данных уже открыт.");
                    }
                } else {
                    throw new TwainException("Data Source Manager is not open. Менеджер источников данных не открыт.");
                }
            }
        }

        /// <summary>
        /// Returns the number of data sources.
        /// <para xml:lang="ru">Возвращает количество источников данных.</para>
        /// </summary>
        [Browsable(false)]
        public int SourcesCount => this._sources.Length;

        /// <summary>
        /// Returns the name of the data source at the specified index.
        /// <para xml:lang="ru">Возвращает имя источника данных по указанному индексу.</para>
        /// </summary>
        /// <param name="index">Index.<para xml:lang="ru">Индекс.</para></param>
        /// <returns>The name of the data source.<para xml:lang="ru">Имя источника данных.</para></returns>
        public string GetSourceProductName(int index) => this._sources[index].ProductName;

        /// <summary>
        /// Gets a description of the specified source.
        /// <para xml:lang="ru">Возвращает описание указанного источника. Gets the source identity.</para>
        /// </summary>
        /// <param name="index">Index.<para xml:lang="ru">Индекс. The index.</para></param>
        /// <returns>Description of the data source.<para xml:lang="ru">Описание источника данных.</para></returns>
        public Identity GetSourceIdentity(int index) => new Identity(this._sources[index]);

        /// <summary>
        /// Returns true if the specified source supports TWAIN 2.0; otherwise false.
        /// <para xml:lang="ru">Возвращает истину, если указанный источник поддерживает TWAIN 2.0; иначе лож.</para>
        /// </summary>
        /// <param name="index">Index<para xml:lang="ru">Индекс.</para></param>
        /// <returns>True if the specified source supports TWAIN 2.0; otherwise false.<para xml:lang="ru">Истина, если указанный источник поддерживает TWAIN 2.0; иначе лож.</para></returns>
        public bool GetIsSourceTwain2Compatible(int index) => (this._sources[index].SupportedGroups & TwDG.DS2) != 0;

        /// <summary>
        /// Sets the specified data source as the default data source.
        /// <para xml:lang="ru">Устанавливает указанный источник данных в качестве источника данных по умолчанию.</para>
        /// </summary>
        /// <param name="index">Index.<para xml:lang="ru">Индекс.</para></param>
        public void SetDefaultSource(int index) {
            if((this._TwainState & TwainStateFlag.DSMOpen) != 0) {
                if((this._TwainState & TwainStateFlag.DSOpen) == 0) {
                    TwIdentity _src = this._sources[index];
                    TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.Set, ref _src);
                    if(_rc != TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                } else {
                    throw new TwainException("The data source is already open. You must first close the data source. Источник данных уже открыт. Необходимо сперва закрыть источник данных.");
                }
            } else {
                throw new TwainException("DSM is not open. DSM не открыт.");
            }
        }

        /// <summary>
        /// Gets the default Data Source.
        /// </summary>
        /// <returns>Index of default Data Source.</returns>
        /// <exception cref="TwainException">
        /// Не удалось найти источник данных по умолчанию.
        /// or
        /// DSM не открыт.
        /// </exception>
        public int GetDefaultSource() {
            if((this._TwainState & TwainStateFlag.DSMOpen) != 0) {
                TwIdentity _identity = new TwIdentity();
                for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.GetDefault, ref _identity); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                for(var i = 0; i < this._sources.Length; i++) {
                    if(_identity.Id == this._sources[i].Id) {
                        return i;
                    }
                }
                throw new TwainException("Could not find default data source. Не удалось найти источник данных по умолчанию.");
            } else {
                throw new TwainException("DSM is not open. DSM не открыт.");
            }
        }

        #endregion

        #region Properties of source

        /// <summary>
        /// Gets the application identifier.
        /// <para xml:lang="ru">Возвращает идентификатор приложения.</para>
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        private TwIdentity _AppId {
            get {
                if(this._appid == null) {
                    Assembly _asm = typeof(Twain32).Assembly;
                    AssemblyName _asm_name = new AssemblyName(_asm.FullName);
                    Version _version = new Version(((AssemblyFileVersionAttribute)_asm.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false)[0]).Version);

                    this._appid = new TwIdentity() {
                        Id = 0,
                        Version = new TwVersion() {
                            MajorNum = (ushort)_version.Major,
                            MinorNum = (ushort)_version.Minor,
                            Language = TwLanguage.RUSSIAN,
                            Country = TwCountry.BELARUS,
                            Info = _asm_name.Version.ToString()
                        },
                        ProtocolMajor = (ushort)(this._isTwain2Enable ? 2 : 1),
                        ProtocolMinor = (ushort)(this._isTwain2Enable ? 3 : 9),
                        SupportedGroups = TwDG.Image | TwDG.Control | (this._isTwain2Enable ? TwDG.APP2 : 0),
                        Manufacturer = ((AssemblyCompanyAttribute)_asm.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0]).Company,
                        ProductFamily = "TWAIN Class Library",
                        ProductName = ((AssemblyProductAttribute)_asm.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0]).Product
                    };
                }
                return this._appid;
            }
            set {
                if(value != null) {
                    throw new ArgumentException("Is read only property.");
                }
                this._appid = null;
            }
        }

        /// <summary>
        /// Gets or sets the name of the application.
        /// <para xml:lang="ru">Возвращает или устанавливает имя приложения.</para>
        /// </summary>
        [Category("Behavior")]
        [Description("Gets or sets the name of the application. Возвращает или устанавливает имя приложения.")]
        public string AppProductName {
            get {
                return this._AppId.ProductName;
            }
            set {
                this._AppId.ProductName = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to display the UI of the TWAIN source.
        /// <para xml:lang="ru">Возвращает или устанавливает значение указывающие на необходимость отображения UI TWAIN-источника.</para>
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(true)]
        [Description("Gets or sets a value indicating whether to display the UI of the TWAIN source. Возвращает или устанавливает значение указывающие на необходимость отображения UI TWAIN-источника.")]
        public bool ShowUI { get; set; }

        [Category("Behavior")]
        [DefaultValue(false)]
        private bool ModalUI { get; set; }

        /// <summary>
        /// Gets or sets the parent window for the TWAIN source.
        /// <para xml:lang="ru">Возвращает или устанавливает родительское окно для TWAIN-источника.</para>
        /// </summary>
        /// <value>
        /// Окно.
        /// </value>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Gets or sets the parent window for the TWAIN source. Возвращает или устанавливает родительское окно для TWAIN-источника.")]
        public IWin32Window Parent { get; set; }

        /// <summary>
        /// Get or set the primary language for your application.
        /// <para xml:lang="ru">Возвращает или устанавливает используемый приложением язык.</para>
        /// </summary>
        [Category("Culture")]
        [DefaultValue(TwLanguage.RUSSIAN)]
        [Description("Get or set the primary language for your application. Возвращает или устанавливает используемый приложением язык.")]
        public TwLanguage Language {
            get {
                return this._AppId.Version.Language;
            }
            set {
                this._AppId.Version.Language = value;
            }
        }

        /// <summary>
        /// Get or set the primary country where your application is intended to be distributed.
        /// <para xml:lang="ru">Возвращает или устанавливает страну происхождения приложения.</para>
        /// </summary>
        [Category("Culture")]
        [DefaultValue(TwCountry.BELARUS)]
        [Description("Get or set the primary country where your application is intended to be distributed. Возвращает или устанавливает страну происхождения приложения.")]
        public TwCountry Country {
            get {
                return this._AppId.Version.Country;
            }
            set {
                this._AppId.Version.Country = value;
            }
        }

        /// <summary>
        /// Gets or sets the frame of the physical location of the image.
        /// <para xml:lang="ru">Возвращает или устанавливает кадр физического расположения изображения.</para>
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public RectangleF ImageLayout {
            get {
                TwImageLayout _imageLayout = new TwImageLayout();
                TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Image, TwDAT.ImageLayout, TwMSG.Get, ref _imageLayout);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                return _imageLayout.Frame;
            }
            set {
                TwImageLayout _imageLayout = new TwImageLayout { Frame = value };
                TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Image, TwDAT.ImageLayout, TwMSG.Set, ref _imageLayout);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
            }
        }

        /// <summary>
        /// Returns a set of capabilities (Capabilities).
        /// <para xml:lang="ru">Возвращает набор возможностей (Capabilities).</para>
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public TwainCapabilities Capabilities {
            get {
                if(this._capabilities == null) {
                    this._capabilities = new TwainCapabilities(this);
                }
                return this._capabilities;
            }
        }

        /// <summary>
        /// Returns a set of operations for working with a color palette.
        /// <para xml:lang="ru">Возвращает набор операций для работы с цветовой палитрой.</para>
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public TwainPalette Palette { get; private set; }

        #endregion

        #region All capabilities

        /// <summary>
        /// Returns flags indicating operations supported by the data source for the specified capability value.
        /// <para xml:lang="ru">Возвращает флаги, указывающие на поддерживаемые источником данных операции, для указанного значения capability.</para>
        /// </summary>
        /// <param name="capability">The value of the TwCap enumeration.<para xml:lang="ru">Значение перечисдения TwCap.</para></param>
        /// <returns>Set of flags.<para xml:lang="ru">Набор флагов.</para></returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public TwQC IsCapSupported(TwCap capability) {
            if((this._TwainState & TwainStateFlag.DSOpen) != 0) {
                TwCapability _cap = new TwCapability(capability);
                try {
                    TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.Capability, TwMSG.QuerySupport, ref _cap);
                    if(_rc == TwRC.Success) {
                        return (TwQC)((TwOneValue)_cap.GetValue()).Item;
                    }
                    return 0;
                } finally {
                    _cap.Dispose();
                }
            } else {
                throw new TwainException("The data source is not open. Источник данных не открыт.");
            }
        }

        /// <summary>
        /// Returns the value for the specified capability.
        /// <para xml:lang="ru">Возвращает значение для указанного capability (возможность).</para>
        /// </summary>
        /// <param name="capability">The value of the TwCap enumeration.<para xml:lang="ru">Значение перечисления TwCap.</para></param>
        /// <param name="msg">The value of the TwMSG enumeration.<para xml:lang="ru">Значение перечисления TwMSG.</para></param>
        /// <returns>Depending on the value of capability, the following can be returned: type-value, array, <see cref="Twain32.Range">range</see>, <see cref="Twain32.Enumeration">transfer</see>.<para xml:lang="ru">В зависимости от значение capability, могут быть возвращены: тип-значение, массив, <see cref="Twain32.Range">диапазон</see>, <see cref="Twain32.Enumeration">перечисление</see>.</para></returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        private object _GetCapCore(TwCap capability, TwMSG msg) {
            if((this._TwainState & TwainStateFlag.DSOpen) != 0) {
                TwCapability _cap = new TwCapability(capability);
                try {
                    TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.Capability, msg, ref _cap);
                    if(_rc == TwRC.Success) {
                        switch(_cap.ConType) {
                            case TwOn.One:
                                object _valueRaw = _cap.GetValue();
                                TwOneValue _value = _valueRaw as TwOneValue;
                                if(_value != null) {
                                    return TwTypeHelper.CastToCommon(_value.ItemType, TwTypeHelper.ValueToTw<uint>(_value.ItemType, _value.Item));
                                } else {
                                    return _valueRaw;
                                }
                            case TwOn.Range:
                                return Range.CreateRange((TwRange)_cap.GetValue());
                            case TwOn.Array:
                                return ((__ITwArray)_cap.GetValue()).Items;
                            case TwOn.Enum:
                                __ITwEnumeration _enum = _cap.GetValue() as __ITwEnumeration;
                                return Enumeration.CreateEnumeration(_enum.Items, _enum.CurrentIndex, _enum.DefaultIndex);
                        }
                        return _cap.GetValue();
                    } else {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                } finally {
                    _cap.Dispose();
                }
            } else {
                throw new TwainException("The data source is not open. Источник данных не открыт.");
            }
        }

        /// <summary>
        /// Gets the values of the specified feature (capability).
        /// <para xml:lang="ru">Возвращает значения указанной возможности (capability).</para>
        /// </summary>
        /// <param name="capability">The value of the TwCap enumeration.<para xml:lang="ru">Значение перечисления TwCap.</para></param>
        /// <returns>Depending on the value of capability, the following can be returned: type-value, array, <see cref="Twain32.Range">range</see>, <see cref="Twain32.Enumeration">transfer</see>.<para xml:lang="ru">В зависимости от значение capability, могут быть возвращены: тип-значение, массив, <see cref="Twain32.Range">диапазон</see>, <see cref="Twain32.Enumeration">перечисление</see>.</para></returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public object GetCap(TwCap capability) => this._GetCapCore(capability, TwMSG.Get);

        /// <summary>
        /// Returns the current value for the specified feature. (capability).
        /// <para xml:lang="ru">Возвращает текущее значение для указанной возможности (capability).</para>
        /// </summary>
        /// <param name="capability">The value of the TwCap enumeration.<para xml:lang="ru">Значение перечисления TwCap.</para></param>
        /// <returns>Depending on the value of capability, the following can be returned: type-value, array, <see cref="Twain32.Range">range</see>, <see cref="Twain32.Enumeration">transfer</see>.<para xml:lang="ru">В зависимости от значение capability, могут быть возвращены: тип-значение, массив, <see cref="Twain32.Range">диапазон</see>, <see cref="Twain32.Enumeration">перечисление</see>.</para></returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public object GetCurrentCap(TwCap capability) => this._GetCapCore(capability, TwMSG.GetCurrent);

        /// <summary>
        /// Returns the default value for the specified feature. (capability).
        /// <para xml:lang="ru">Возвращает значение по умолчанию для указанной возможности (capability).</para>
        /// </summary>
        /// <param name="capability">The value of the TwCap enumeration.<para xml:lang="ru">Значение перечисления TwCap.</para></param>
        /// <returns>Depending on the value of capability, the following can be returned: type-value, array, <see cref="Twain32.Range">range</see>, <see cref="Twain32.Enumeration">transfer</see>.<para xml:lang="ru">В зависимости от значение capability, могут быть возвращены: тип-значение, массив, <see cref="Twain32.Range">диапазон</see>, <see cref="Twain32.Enumeration">перечисление</see>.</para></returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public object GetDefaultCap(TwCap capability) => this._GetCapCore(capability, TwMSG.GetDefault);

        /// <summary>
        /// Resets the current value for the specified <see cref="TwCap">capability</see> to default value.
        /// <para xml:lang="ru">Сбрасывает текущее значение для указанного <see cref="TwCap">capability</see> в значение по умолчанию.</para>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.<para xml:lang="ru">Значение перечисления <see cref="TwCap"/>.</para></param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public void ResetCap(TwCap capability) {
            if((this._TwainState & TwainStateFlag.DSOpen) != 0) {
                TwCapability _cap = new TwCapability(capability);
                try {
                    TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.Capability, TwMSG.Reset, ref _cap);
                    if(_rc != TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                } finally {
                    _cap.Dispose();
                }
            } else {
                throw new TwainException("The data source is not open. Источник данных не открыт.");
            }
        }

        /// <summary>
        /// Resets the current value of all current values to the default values.
        /// <para xml:lang="ru">Сбрасывает текущее значение всех текущих значений в значения по умолчанию.</para>
        /// </summary>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public void ResetAllCap() {
            if((this._TwainState & TwainStateFlag.DSOpen) != 0) {
                TwCapability _cap = new TwCapability(TwCap.SupportedCaps);
                try {
                    TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.Capability, TwMSG.ResetAll, ref _cap);
                    if(_rc != TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                } finally {
                    _cap.Dispose();
                }
            } else {
                throw new TwainException("The data source is not open. Источник данных не открыт.");
            }
        }

        private void _SetCapCore(TwCapability cap, TwMSG msg) {
            if((this._TwainState & TwainStateFlag.DSOpen) != 0) {
                try {
                    TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.Capability, msg, ref cap);
                    if(_rc != TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                } finally {
                    cap.Dispose();
                }
            } else {
                throw new TwainException("The data source is not open. Источник данных не открыт.");
            }
        }

        private void _SetCapCore(TwCap capability, TwMSG msg, object value) {
            TwCapability _cap = null;
            if(value is string) {
                object[] _attrs = typeof(TwCap).GetField(capability.ToString())?.GetCustomAttributes(typeof(TwTypeAttribute), false);
                if(_attrs?.Length > 0) {
                    _cap = new TwCapability(capability, (string)value, ((TwTypeAttribute)_attrs[0]).TwType);
                } else {
                    _cap = new TwCapability(capability, (string)value, TwTypeHelper.TypeOf(value));
                }
            } else {
                TwType _type = TwTypeHelper.TypeOf(value.GetType());
                _cap = new TwCapability(capability, TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type, value)), _type);
            }
            this._SetCapCore(_cap, msg);
        }

        private void _SetCapCore(TwCap capability, TwMSG msg, object[] value) {
            var _attrs = typeof(TwCap).GetField(capability.ToString())?.GetCustomAttributes(typeof(TwTypeAttribute), false);
            this._SetCapCore(
                new TwCapability(
                    capability,
                    new TwArray() {
                        ItemType = _attrs?.Length > 0 ? ((TwTypeAttribute)(_attrs[0])).TwType : TwTypeHelper.TypeOf(value[0]),
                        NumItems = (uint)value.Length
                    },
                    value),
                msg);
        }

        private void _SetCapCore(TwCap capability, TwMSG msg, Range value) => this._SetCapCore(new TwCapability(capability, value.ToTwRange()), msg);

        private void _SetCapCore(TwCap capability, TwMSG msg, Enumeration value) {
            var _attrs = typeof(TwCap).GetField(capability.ToString())?.GetCustomAttributes(typeof(TwTypeAttribute), false);
            this._SetCapCore(
                new TwCapability(
                    capability,
                    new TwEnumeration {
                        ItemType = _attrs?.Length > 0 ? ((TwTypeAttribute)(_attrs[0])).TwType : TwTypeHelper.TypeOf(value[0]),
                        NumItems = (uint)value.Count,
                        CurrentIndex = (uint)value.CurrentIndex,
                        DefaultIndex = (uint)value.DefaultIndex
                    },
                    value.Items),
                msg);
        }

        /// <summary>
        /// Sets the value for the specified <see cref="TwCap">capability</see>
        /// <para xml:lang="ru">Устанавливает значение для указанного <see cref="TwCap">capability</see></para>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.<para xml:lang="ru">Значение перечисления <see cref="TwCap"/>.</para></param>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        /// <exception cref="TwainException">Возникает в случае, если источник данных не открыт.</exception>
        public void SetCap(TwCap capability, object value) => this._SetCapCore(capability, TwMSG.Set, value);

        /// <summary>
        /// Sets the value for the specified <see cref="TwCap">capability</see>
        /// <para xml:lang="ru">Устанавливает значение для указанного <see cref="TwCap">capability</see></para>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.<para xml:lang="ru">Значение перечисления <see cref="TwCap"/>.</para></param>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        /// <exception cref="TwainException">Возникает в случае, если источник данных не открыт.</exception>
        public void SetCap(TwCap capability, object[] value) => this._SetCapCore(capability, TwMSG.Set, value);

        /// <summary>
        /// Sets the value for the specified <see cref="TwCap">capability</see>
        /// <para xml:lang="ru">Устанавливает значение для указанного <see cref="TwCap">capability</see></para>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.<para xml:lang="ru">Значение перечисления <see cref="TwCap"/>.</para></param>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        /// <exception cref="TwainException">Возникает в случае, если источник данных не открыт.</exception>
        public void SetCap(TwCap capability, Range value) => this._SetCapCore(capability, TwMSG.Set, value);

        /// <summary>
        /// Sets the value for the specified <see cref="TwCap">capability</see>
        /// <para xml:lang="ru">Устанавливает значение для указанного <see cref="TwCap">capability</see></para>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.<para xml:lang="ru">Значение перечисления <see cref="TwCap"/>.</para></param>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        /// <exception cref="TwainException">Возникает в случае, если источник данных не открыт.</exception>
        public void SetCap(TwCap capability, Enumeration value) => this._SetCapCore(capability, TwMSG.Set, value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.<para xml:lang="ru">Значение перечисления <see cref="TwCap"/>.</para></param>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        /// <exception cref="TwainException">Возникает в случае, если источник данных не открыт.</exception>
        public void SetConstraintCap(TwCap capability, object value) => this._SetCapCore(capability, TwMSG.SetConstraint, value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.<para xml:lang="ru">Значение перечисления <see cref="TwCap"/>.</para></param>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        /// <exception cref="TwainException">Возникает в случае, если источник данных не открыт.</exception>
        public void SetConstraintCap(TwCap capability, object[] value) => this._SetCapCore(capability, TwMSG.SetConstraint, value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.<para xml:lang="ru">Значение перечисления <see cref="TwCap"/>.</para></param>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        /// <exception cref="TwainException">Возникает в случае, если источник данных не открыт.</exception>
        public void SetConstraintCap(TwCap capability, Range value) => this._SetCapCore(capability, TwMSG.SetConstraint, value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.<para xml:lang="ru">Значение перечисления <see cref="TwCap"/>.</para></param>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        /// <exception cref="TwainException">Возникает в случае, если источник данных не открыт.</exception>
        public void SetConstraintCap(TwCap capability, Enumeration value) => this._SetCapCore(capability, TwMSG.SetConstraint, value);

        #endregion

        #region DG_IMAGE / IMAGExxxxXFER / MSG_GET operation

        /// <summary>
        /// Performs image transfer (Native Mode Transfer).
        /// <para xml:lang="ru">Выполняет передачу изображения (Native Mode Transfer).</para>
        /// </summary>
        private void _NativeTransferPictures() {
            if(this._srcds.Id == 0) {
                return;
            }
            IntPtr _hBitmap = IntPtr.Zero;
            TwPendingXfers _pxfr = new TwPendingXfers();
            try {
                this._images.Clear();

                do {
                    _pxfr.Count = 0;
                    _hBitmap = IntPtr.Zero;

                    for(TwRC _rc = this._dsmEntry.DSImageXfer(this._AppId, this._srcds, TwDG.Image, TwDAT.ImageNativeXfer, TwMSG.Get, ref _hBitmap); _rc != TwRC.XferDone;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    // DG_IMAGE / DAT_IMAGEINFO / MSG_GET
                    // DG_IMAGE / DAT_EXTIMAGEINFO / MSG_GET
                    if(this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo, this._GetExtImageInfo))) {
                        return;
                    }

                    IntPtr _pBitmap = _Memory.Lock(_hBitmap);
                    try {
                        _Image _img = null;

                        IImageHandler _handler = this.GetService(typeof(IImageHandler)) as IImageHandler;
                        if(_handler == null) {
                            switch(Environment.OSVersion.Platform) {
                                case PlatformID.Unix:
                                    _handler = new Tiff();
                                    break;
                                case PlatformID.MacOSX:
                                    _handler = new Pict();
                                    break;
                                default:
                                    _handler = new DibToImage();
                                    break;
                            }
                        }
                        _img = _handler.PtrToStream(_pBitmap, this.GetService(typeof(IStreamProvider)) as IStreamProvider);

                        this._images.Add(_img);
                        if(this._OnEndXfer(new EndXferEventArgs(_img))) {
                            return;
                        }
                    } finally {
                        _Memory.Unlock(_hBitmap);
                        _Memory.Free(_hBitmap);
                    }
                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.EndXfer, ref _pxfr); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                } while(_pxfr.Count != 0);
            } finally {
                TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.Reset, ref _pxfr);
            }
        }

        /// <summary>
        /// Performs image transfer (Disk File Mode Transfer).
        /// <para xml:lang="ru">Выполняет передачу изображения (Disk File Mode Transfer).</para>
        /// </summary>
        private void _FileTransferPictures() {
            if(this._srcds.Id == 0) {
                return;
            }

            TwPendingXfers _pxfr = new TwPendingXfers();
            try {
                this._images.Clear();
                do {
                    _pxfr.Count = 0;

                    SetupFileXferEventArgs _args = new SetupFileXferEventArgs();
                    if(this._OnSetupFileXfer(_args)) {
                        return;
                    }

                    TwSetupFileXfer _fileXfer = new TwSetupFileXfer {
                        Format = this.Capabilities.ImageFileFormat.IsSupported(TwQC.GetCurrent) ? this.Capabilities.ImageFileFormat.GetCurrent() : TwFF.Bmp,
                        FileName = string.IsNullOrEmpty(_args.FileName) ? Path.GetTempFileName() : _args.FileName
                    };

                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.SetupFileXfer, TwMSG.Set, ref _fileXfer); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }

                    for(TwRC _rc = this._dsmEntry.DsRaw(this._AppId, this._srcds, TwDG.Image, TwDAT.ImageFileXfer, TwMSG.Get, IntPtr.Zero); _rc != TwRC.XferDone;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    // DG_IMAGE / DAT_IMAGEINFO / MSG_GET
                    // DG_IMAGE / DAT_EXTIMAGEINFO / MSG_GET
                    if(this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo, this._GetExtImageInfo))) {
                        return;
                    }

                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.EndXfer, ref _pxfr); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.SetupFileXfer, TwMSG.Get, ref _fileXfer); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    if(this._OnFileXfer(new FileXferEventArgs(ImageFileXfer.Create(_fileXfer)))) {
                        return;
                    }
                } while(_pxfr.Count != 0);
            } finally {
                TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.Reset, ref _pxfr);
            }
        }

        /// <summary>
        /// Performs image transfer (Buffered Memory Mode Transfer and Memory File Mode Transfer).
        /// <para xml:lang="ru">Выполняет передачу изображения (Buffered Memory Mode Transfer and Memory File Mode Transfer).</para>
        /// </summary>
        private void _MemoryTransferPictures(bool isMemFile) {
            if(this._srcds.Id == 0) {
                return;
            }

            TwPendingXfers _pxfr = new TwPendingXfers();
            try {
                this._images.Clear();
                do {
                    _pxfr.Count = 0;
                    ImageInfo _info = this._GetImageInfo();

                    if(isMemFile) {
                        if((this.Capabilities.ImageFileFormat.IsSupported() & TwQC.GetCurrent) != 0) {
                            TwSetupFileXfer _fileXfer = new TwSetupFileXfer {
                                Format = this.Capabilities.ImageFileFormat.GetCurrent()
                            };
                            for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.SetupFileXfer, TwMSG.Set, ref _fileXfer); _rc != TwRC.Success;) {
                                throw new TwainException(this._GetTwainStatus(), _rc);
                            }
                        }
                    }

                    TwSetupMemXfer _memBufSize = new TwSetupMemXfer();

                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.SetupMemXfer, TwMSG.Get, ref _memBufSize); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    if(this._OnSetupMemXfer(new SetupMemXferEventArgs(_info, _memBufSize.Preferred))) {
                        return;
                    }

                    IntPtr _hMem = _Memory.Alloc((int)_memBufSize.Preferred);
                    if(_hMem == IntPtr.Zero) {
                        throw new TwainException("Error allocating memory. Ошибка выделениия памяти.");
                    }
                    try {
                        TwMemory _mem = new TwMemory {
                            Flags = TwMF.AppOwns | TwMF.Pointer,
                            Length = _memBufSize.Preferred,
                            TheMem = _Memory.Lock(_hMem)
                        };

                        do {
                            TwImageMemXfer _memXferBuf = new TwImageMemXfer { Memory = _mem };
                            _Memory.ZeroMemory(_memXferBuf.Memory.TheMem, (IntPtr)_memXferBuf.Memory.Length);

                            TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Image, isMemFile ? TwDAT.ImageMemFileXfer : TwDAT.ImageMemXfer, TwMSG.Get, ref _memXferBuf);
                            if(_rc != TwRC.Success && _rc != TwRC.XferDone) {
                                TwCC _cc = this._GetTwainStatus();
                                TwRC _rc2 = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.EndXfer, ref _pxfr);
                                throw new TwainException(_cc, _rc);
                            }
                            if(this._OnMemXfer(new MemXferEventArgs(_info, ImageMemXfer.Create(_memXferBuf)))) {
                                return;
                            }
                            if(_rc == TwRC.XferDone) {
                                // DG_IMAGE / DAT_IMAGEINFO / MSG_GET
                                // DG_IMAGE / DAT_EXTIMAGEINFO / MSG_GET
                                if(this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo, this._GetExtImageInfo))) {
                                    return;
                                }
                                break;
                            }
                        } while(true);
                    } finally {
                        _Memory.Unlock(_hMem);
                        _Memory.Free(_hMem);
                    }
                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.EndXfer, ref _pxfr); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                } while(_pxfr.Count != 0);
            } finally {
                TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.Reset, ref _pxfr);
            }
        }

        #endregion

        #region DG_CONTROL / DAT_CUSTOMDSDATA / MSG_xxx

        /// <summary>
        /// Get or set the Custom Data (DG_CONTROL / DAT_CUSTOMDSDATA / MSG_xxx).
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public byte[] CustomDSData {
            get {
                if((this._TwainState & TwainStateFlag.DSOpen) != 0) {
                    TwCustomDSData _data = new TwCustomDSData { };
                    TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.CustomDSData, TwMSG.Get, ref _data);
                    if(_rc != TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    byte[] _value = new byte[_data.InfoLength];
                    Marshal.Copy(_Memory.Lock(_data.hData), _value, 0, _value.Length);
                    _Memory.Unlock(_data.hData);
                    _Memory.Free(_data.hData);
                    return _value;
                } else {
                    throw new TwainException("Источник данных не открыт.");
                }
            }
            set {
                if((this._TwainState & TwainStateFlag.DSOpen) != 0) {
                    TwCustomDSData _data = new TwCustomDSData {
                        InfoLength = (uint)value.Length,
                        hData = _Memory.Alloc(value.Length)
                    };
                    Marshal.Copy(value, 0, _Memory.Lock(_data.hData), value.Length);
                    _Memory.Unlock(_data.hData);
                    TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.CustomDSData, TwMSG.Set, ref _data);
                    if(_rc != TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    _Memory.Free(_data.hData);
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

                if((this._TwainState & TwainStateFlag.DSEnabled) == 0) {
                    this._TwainState |= TwainStateFlag.DSEnabled | TwainStateFlag.DSReady;
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

        #region Raise events

        private void _OnAcquireCompleted(EventArgs e) => this.AcquireCompleted?.Invoke(this, e);

        private void _OnAcquireError(AcquireErrorEventArgs e) => this.AcquireError?.Invoke(this, e);

        private bool _OnXferDone(XferDoneEventArgs e) {
            this.XferDone?.Invoke(this, e);
            return e.Cancel;
        }

        private bool _OnEndXfer(EndXferEventArgs e) {
            this.EndXfer?.Invoke(this, e);
            return e.Cancel;
        }

        private bool _OnSetupMemXfer(SetupMemXferEventArgs e) {
            this.SetupMemXferEvent?.Invoke(this, e);
            return e.Cancel;
        }

        private bool _OnMemXfer(MemXferEventArgs e) {
            this.MemXferEvent?.Invoke(this, e);
            return e.Cancel;
        }

        private bool _OnSetupFileXfer(SetupFileXferEventArgs e) {
            this.SetupFileXferEvent?.Invoke(this, e);
            return e.Cancel;
        }

        private bool _OnFileXfer(FileXferEventArgs e) {
            this.FileXferEvent?.Invoke(this, e);
            return e.Cancel;
        }

        private void _OnDeviceEvent(DeviceEventEventArgs e) => this.DeviceEvent?.Invoke(this, e);

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
        private TwainStateFlag _TwainState {
            get {
                return this._twainState;
            }
            set {
                if(this._twainState != value) {
                    this._twainState = value;
                    if(this.TwainStateChanged != null) {
                        this.TwainStateChanged(this, new TwainStateEventArgs(this._twainState));
                    }
                }
            }
        }

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
            IntPtr _extImageInfo = TwExtImageInfo.ToPtr(_info);
            try {

                TwRC _rc = this._dsmEntry.DsRaw(this._AppId, this._srcds, TwDG.Image, TwDAT.ExtImageInfo, TwMSG.Get, _extImageInfo);
                if(_rc != TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                return ExtImageInfo.FromPtr(_extImageInfo);
            } finally {
                Marshal.FreeHGlobal(_extImageInfo);
            }
        }

        /// <summary>
        /// State flags.
        /// <para xml:lang="ru">Флаги состояния.</para>
        /// </summary>
        [Flags]
        public enum TwainStateFlag {

            /// <summary>
            /// The DSM open.
            /// </summary>
            DSMOpen = 0x1,

            /// <summary>
            /// The ds open.
            /// </summary>
            DSOpen = 0x2,

            /// <summary>
            /// The ds enabled.
            /// </summary>
            DSEnabled = 0x4,

            /// <summary>
            /// The ds ready.
            /// </summary>
            DSReady = 0x08
        }

        #region Events

        /// <summary>
        /// Occurs when the acquire is completed.
        /// <para xml:lang="ru">Возникает в момент окончания сканирования.</para>
        /// </summary>
        [Category("Action")]
        [Description("Occurs when the acquire is completed. Возникает в момент окончания сканирования.")]
        public event EventHandler AcquireCompleted;

        /// <summary>
        /// Occurs when error received during acquire.
        /// <para xml:lang="ru">Возникает в момент получения ошибки в процессе сканирования.</para>
        /// </summary>
        [Category("Action")]
        [Description("Occurs when error received during acquire. Возникает в момент получения ошибки в процессе сканирования.")]
        public event EventHandler<AcquireErrorEventArgs> AcquireError;

        /// <summary>
        /// Occurs when the transfer into application was completed (Native Mode Transfer).
        /// <para xml:lang="ru">Возникает в момент окончания получения изображения приложением.</para>
        /// </summary>
        [Category("Native Mode Action")]
        [Description("Occurs when the transfer into application was completed (Native Mode Transfer). Возникает в момент окончания получения изображения приложением.")]
        public event EventHandler<EndXferEventArgs> EndXfer;

        /// <summary>
        /// Occurs when the transfer was completed.
        /// <para xml:lang="ru">Возникает в момент окончания получения изображения источником.</para>
        /// </summary>
        [Category("Action")]
        [Description("Occurs when the transfer was completed. Возникает в момент окончания получения изображения источником.")]
        public event EventHandler<XferDoneEventArgs> XferDone;

        /// <summary>
        /// Occurs when determined size of buffer to use during the transfer (Memory Mode Transfer and MemFile Mode Transfer).
        /// <para xml:lang="ru">Возникает в момент установки размера буфера памяти.</para>
        /// </summary>
        [Category("Memory Mode Action")]
        [Description("Occurs when determined size of buffer to use during the transfer (Memory Mode Transfer and MemFile Mode Transfer). Возникает в момент установки размера буфера памяти.")]
        public event EventHandler<SetupMemXferEventArgs> SetupMemXferEvent;

        /// <summary>
        /// Occurs when the memory block for the data was recived (Memory Mode Transfer and MemFile Mode Transfer).
        /// <para xml:lang="ru">Возникает в момент получения очередного блока данных.</para>
        /// </summary>
        [Category("Memory Mode Action")]
        [Description("Occurs when the memory block for the data was recived (Memory Mode Transfer and MemFile Mode Transfer). Возникает в момент получения очередного блока данных.")]
        public event EventHandler<MemXferEventArgs> MemXferEvent;

        /// <summary>
        /// Occurs when you need to specify the filename (File Mode Transfer).
        /// <para xml:lang="ru">Возникает в момент, когда необходимо задать имя файла изображения.</para>
        /// </summary>
        [Category("File Mode Action")]
        [Description("Occurs when you need to specify the filename. (File Mode Transfer) Возникает в момент, когда необходимо задать имя файла изображения.")]
        public event EventHandler<SetupFileXferEventArgs> SetupFileXferEvent;

        /// <summary>
        /// Occurs when the transfer into application was completed (File Mode Transfer).
        /// <para xml:lang="ru">Возникает в момент окончания получения файла изображения приложением.</para>
        /// </summary>
        [Category("File Mode Action")]
        [Description("Occurs when the transfer into application was completed (File Mode Transfer). Возникает в момент окончания получения файла изображения приложением.")]
        public event EventHandler<FileXferEventArgs> FileXferEvent;

        /// <summary>
        /// Occurs when TWAIN state was changed.
        /// <para xml:lang="ru">Возникает в момент изменения состояния twain-устройства.</para>
        /// </summary>
        [Category("Behavior")]
        [Description("Occurs when TWAIN state was changed. Возникает в момент изменения состояния twain-устройства.")]
        public event EventHandler<TwainStateEventArgs> TwainStateChanged;

        /// <summary>
        /// Occurs when enabled the source sends this message to the Application to alert it that some event has taken place.
        /// <para xml:lang="ru">Возникает в момент, когда источник уведомляет приложение о произошедшем событии.</para>
        /// </summary>
        [Category("Behavior")]
        [Description("Occurs when enabled the source sends this message to the Application to alert it that some event has taken place. Возникает в момент, когда источник уведомляет приложение о произошедшем событии.")]
        public event EventHandler<DeviceEventEventArgs> DeviceEvent;

        #endregion

        #region Events Args

        /// <summary>
        /// Arguments for the EndXfer event.
        /// <para xml:lang="ru">Аргументы события EndXfer.</para>
        /// </summary>
        [Serializable]
        public sealed class EndXferEventArgs : SerializableCancelEventArgs {
            private readonly _Image _image;

            /// <summary>
            /// Initializes a new instance of the class.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса.</para>
            /// </summary>
            /// <param name="image">Image.<para xml:lang="ru">Изображение.</para></param>
            internal EndXferEventArgs(object image) {
                this._image = image as _Image;
            }

            public T CreateImage<T>(IImageFactory<T> factory) where T : class => factory.Create(this._image);

            /// <summary>
            /// Returns the image.
            /// <para xml:lang="ru">Возвращает изображение.</para>
            /// </summary>
            public Image Image => this._image;

#if !NET2            
            /// <summary>
            /// Returns the image.
            /// <para xml:lang="ru">Возвращает изображение.</para>
            /// </summary>
            public System.Windows.Media.ImageSource ImageSource => this._image;
#endif
        }

        /// <summary>
        /// Arguments for the XferDone event.
        /// <para xml:lang="ru">Аргументы события XferDone.</para>
        /// </summary>
        public sealed class XferDoneEventArgs : SerializableCancelEventArgs {
            private readonly GetImageInfoCallback _imageInfoMethod;
            private readonly GetExtImageInfoCallback _extImageInfoMethod;

            /// <summary>
            /// Initializes a new instance of the class <see cref="XferDoneEventArgs"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="XferDoneEventArgs"/>.</para>
            /// </summary>
            /// <param name="method1">Callback method to get image description.<para xml:lang="ru">Метод обратного вызова для получения описания изображения.</para></param>
            /// <param name="method2">Callback method to get an extended image description.<para xml:lang="ru">Метод обратного вызова для получения расширенного описания изображения.</para></param>
            internal XferDoneEventArgs(GetImageInfoCallback method1, GetExtImageInfoCallback method2) {
                this._imageInfoMethod = method1;
                this._extImageInfoMethod = method2;
            }

            /// <summary>
            /// Returns a description of the received image.
            /// <para xml:lang="ru">Возвращает описание полученного изображения.</para>
            /// </summary>
            /// <returns>Description of the image.<para xml:lang="ru">Описание изображения.</para></returns>
            public ImageInfo GetImageInfo() => this._imageInfoMethod();

            /// <summary>
            /// Returns an extended description of the resulting image.
            /// <para xml:lang="ru">Возвращает расширенного описание полученного изображения.</para>
            /// </summary>
            /// <param name="extInfo">A set of codes for the extended image description for which you want to get a description.<para xml:lang="ru">Набор кодов расширенного описания изображения для которых требуется получить описание.</para></param>
            /// <returns>Extended image description.<para xml:lang="ru">Расширенное описание изображения.</para></returns>
            public ExtImageInfo GetExtImageInfo(params TwEI[] extInfo) => this._extImageInfoMethod(extInfo);
        }

        /// <summary>
        /// Arguments for the SetupMemXferEvent event.
        /// <para xml:lang="ru">Аргументы события SetupMemXferEvent.</para>
        /// </summary>
        [Serializable]
        public sealed class SetupMemXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="SetupMemXferEventArgs"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="SetupMemXferEventArgs"/>.</para>
            /// </summary>
            /// <param name="info">Description of the image.<para xml:lang="ru">Описание изображения.</para></param>
            /// <param name="bufferSize">The size of the memory buffer for data transfer.<para xml:lang="ru">Размер буфера памяти для передачи данных.</para></param>
            internal SetupMemXferEventArgs(ImageInfo info, uint bufferSize) {
                this.ImageInfo = info;
                this.BufferSize = bufferSize;
            }

            /// <summary>
            /// Returns a description of the image.
            /// <para xml:lang="ru">Возвращает описание изображения.</para>
            /// </summary>
            public ImageInfo ImageInfo { get; private set; }

            /// <summary>
            /// Gets the size of the memory buffer for data transfer.
            /// <para xml:lang="ru">Возвращает размер буфера памяти для передачи данных.</para>
            /// </summary>
            public uint BufferSize { get; private set; }
        }

        /// <summary>
        /// Arguments for the MemXferEvent event.
        /// <para xml:lang="ru">Аргументы события MemXferEvent.</para>
        /// </summary>
        [Serializable]
        public sealed class MemXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="MemXferEventArgs"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="MemXferEventArgs"/>.</para>
            /// </summary>
            /// <param name="info">Description of the image.<para xml:lang="ru">Описание изображения.</para></param>
            /// <param name="image">A fragment of image data.<para xml:lang="ru">Фрагмент данных изображения.</para></param>
            internal MemXferEventArgs(ImageInfo info, ImageMemXfer image) {
                this.ImageInfo = info;
                this.ImageMemXfer = image;
            }

            /// <summary>
            /// Returns a description of the image.
            /// <para xml:lang="ru">Возвращает описание изображения.</para>
            /// </summary>
            public ImageInfo ImageInfo { get; private set; }

            /// <summary>
            /// Returns a piece of image data.
            /// <para xml:lang="ru">Возвращает фрагмент данных изображения.</para>
            /// </summary>
            public ImageMemXfer ImageMemXfer { get; private set; }
        }

        /// <summary>
        /// Arguments for the SetupFileXferEvent event.
        /// <para xml:lang="ru">Аргументы события SetupFileXferEvent.</para>
        /// </summary>
        [Serializable]
        public sealed class SetupFileXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="SetupFileXferEventArgs"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="SetupFileXferEventArgs"/>.</para>
            /// </summary>
            internal SetupFileXferEventArgs() {
            }

            /// <summary>
            /// Gets or sets the name of the image file.
            /// <para xml:lang="ru">Возвращает или устанавливает имя файла изображения.</para>
            /// </summary>
            public string FileName { get; set; }
        }

        /// <summary>
        /// Arguments for the FileXferEvent event.
        /// <para xml:lang="ru">Аргументы события FileXferEvent.</para>
        /// </summary>
        [Serializable]
        public sealed class FileXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="FileXferEventArgs"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="FileXferEventArgs"/>.</para>
            /// </summary>
            /// <param name="image">Description of the image file.<para xml:lang="ru">Описание файла изображения.</para></param>
            internal FileXferEventArgs(ImageFileXfer image) {
                this.ImageFileXfer = image;
            }

            /// <summary>
            /// Returns a description of the image file.
            /// <para xml:lang="ru">Возвращает описание файла изображения.</para>
            /// </summary>
            public ImageFileXfer ImageFileXfer { get; private set; }
        }

        /// <summary>
        /// Arguments for the TwainStateChanged event.
        /// <para xml:lang="ru">Аргументы события TwainStateChanged.</para>
        /// </summary>
        [Serializable]
        public sealed class TwainStateEventArgs : EventArgs {

            /// <summary>
            /// Initializes a new instance of the class.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса.</para>
            /// </summary>
            /// <param name="flags">State flags.<para xml:lang="ru">Флаги состояния.</para></param>
            internal TwainStateEventArgs(TwainStateFlag flags) {
                this.TwainState = flags;
            }

            /// <summary>
            /// Returns the status flags of a twain device.
            /// <para xml:lang="ru">Возвращает флаги состояния twain-устройства.</para>
            /// </summary>
            public TwainStateFlag TwainState { get; private set; }
        }

        /// <summary>
        /// Arguments for the DeviceEvent event.
        /// <para xml:lang="ru">Аргументы события DeviceEvent.</para>
        /// </summary>
        public sealed class DeviceEventEventArgs : EventArgs {
            private readonly TwDeviceEvent _deviceEvent;

            internal DeviceEventEventArgs(TwDeviceEvent deviceEvent) {
                this._deviceEvent = deviceEvent;
            }

            /// <summary>
            /// One of the TWDE_xxxx values.
            /// </summary>
            public TwDE Event => this._deviceEvent.Event;

            /// <summary>
            /// The name of the device that generated the event.
            /// </summary>
            public string DeviceName => this._deviceEvent.DeviceName;

            /// <summary>
            /// Battery Minutes Remaining.
            /// </summary>
            public uint BatteryMinutes => this._deviceEvent.BatteryMinutes;

            /// <summary>
            /// Battery Percentage Remaining.
            /// </summary>
            public short BatteryPercentAge => this._deviceEvent.BatteryPercentAge;

            /// <summary>
            /// Power Supply.
            /// </summary>
            public int PowerSupply => this._deviceEvent.PowerSupply;

            /// <summary>
            /// Resolution.
            /// </summary>
            public float XResolution => this._deviceEvent.XResolution;

            /// <summary>
            /// Resolution.
            /// </summary>
            public float YResolution => this._deviceEvent.YResolution;

            /// <summary>
            /// Flash Used2.
            /// </summary>
            public uint FlashUsed2 => this._deviceEvent.FlashUsed2;

            /// <summary>
            /// Automatic Capture.
            /// </summary>
            public uint AutomaticCapture => this._deviceEvent.AutomaticCapture;

            /// <summary>
            /// Automatic Capture.
            /// </summary>
            public uint TimeBeforeFirstCapture => this._deviceEvent.TimeBeforeFirstCapture;

            /// <summary>
            /// Automatic Capture.
            /// </summary>
            public uint TimeBetweenCaptures => this._deviceEvent.TimeBetweenCaptures;
        }

        /// <summary>
        /// Arguments for the AcquireError event.
        /// <para xml:lang="ru">Аргументы события AcquireError.</para>
        /// </summary>
        [Serializable]
        public sealed class AcquireErrorEventArgs : EventArgs {

            /// <summary>
            /// Initializes a new instance of the class.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса.</para>
            /// </summary>
            /// <param name="ex">An instance of the exception class.<para xml:lang="ru">Экземпляр класса исключения.</para></param>
            internal AcquireErrorEventArgs(TwainException ex) {
                this.Exception = ex;
            }

            /// <summary>
            /// Gets an instance of the exception class.
            /// <para xml:lang="ru">Возвращает экземпляр класса исключения.</para>
            /// </summary>
            public TwainException Exception { get; private set; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="System.EventArgs" />
        [Serializable]
        public class SerializableCancelEventArgs : EventArgs {

            /// <summary>
            /// Gets or sets a value indicating whether the event should be canceled.
            /// <para xml:lang="ru">Получает или задает значение, показывающее, следует ли отменить событие.</para>
            /// </summary>
            /// <value>
            /// Значение <c>true</c>, если событие следует отменить, в противном случае — значение <c>false</c>.
            /// </value>
            public bool Cancel { get; set; }
        }

        #endregion

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
                switch(Environment.OSVersion.Platform) {
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
                        MethodInfo _createDelegate = typeof(_DsmEntry).GetMethod("CreateDelegate", BindingFlags.Static | BindingFlags.NonPublic);
                        foreach(PropertyInfo _prop in typeof(_DsmEntry).GetProperties()) {
                            _prop.SetValue(this, _createDelegate.MakeGenericMethod(_prop.PropertyType).Invoke(this, new object[] { ptr }), null);
                        }
                        break;
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
        /// Entry points for memory management functions.
        /// <para xml:lang="ru">Точки входа для функций управления памятью.</para>
        /// </summary>
        internal sealed class _Memory {
            private static TwEntryPoint _entryPoint;

            /// <summary>
            /// Allocates a memory block of the specified size.
            /// <para xml:lang="ru">Выделяет блок памяти указанного размера.</para>
            /// </summary>
            /// <param name="size">The size of the memory block.<para xml:lang="ru">Размер блока памяти.</para></param>
            /// <returns>Memory descriptor.<para xml:lang="ru">Дескриптор памяти.</para></returns>
            public static IntPtr Alloc(int size) {
                if(_Memory._entryPoint != null && _Memory._entryPoint.MemoryAllocate != null) {
                    return _Memory._entryPoint.MemoryAllocate(size);
                }
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        return _Memory.GlobalAlloc(0x42, size);
                }
            }

            /// <summary>
            /// Frees up memory.
            /// <para xml:lang="ru">Освобождает память.</para>
            /// </summary>
            /// <param name="handle">Memory descriptor.<para xml:lang="ru">Дескриптор памяти.</para></param>
            public static void Free(IntPtr handle) {
                if(_Memory._entryPoint != null && _Memory._entryPoint.MemoryFree != null) {
                    _Memory._entryPoint.MemoryFree(handle);
                    return;
                }
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        _Memory.GlobalFree(handle);
                        break;
                }
            }

            /// <summary>
            /// Performs a memory lock.
            /// <para xml:lang="ru">Выполняет блокировку памяти.</para>
            /// </summary>
            /// <param name="handle">Memory descriptor.<para xml:lang="ru">Дескриптор памяти.</para></param>
            /// <returns>Pointer to a block of memory.<para xml:lang="ru">Указатель на блок памяти.</para></returns>
            public static IntPtr Lock(IntPtr handle) {
                if(_Memory._entryPoint != null && _Memory._entryPoint.MemoryLock != null) {
                    return _Memory._entryPoint.MemoryLock(handle);
                }
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        return _Memory.GlobalLock(handle);
                }
            }

            /// <summary>
            /// Unlocks memory.
            /// <para xml:lang="ru">Выполняет разблокировку памяти.</para>
            /// </summary>
            /// <param name="handle">Memory descriptor.<para xml:lang="ru">Дескриптор памяти.</para></param>
            public static void Unlock(IntPtr handle) {
                if(_Memory._entryPoint != null && _Memory._entryPoint.MemoryUnlock != null) {
                    _Memory._entryPoint.MemoryUnlock(handle);
                    return;
                }
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        _Memory.GlobalUnlock(handle);
                        break;
                }
            }

            public static void ZeroMemory(IntPtr dest, IntPtr size) {
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        byte[] _data = new byte[size.ToInt32()];
                        Marshal.Copy(_data, 0, dest, _data.Length);
                        break;
                    default:
                        _Memory._ZeroMemory(dest, size);
                        break;
                }
            }

            /// <summary>
            /// Sets entry points.
            /// <para xml:lang="ru">Устаначливает точки входа.</para>
            /// </summary>
            /// <param name="entry">Entry points.<para xml:lang="ru">Точки входа.</para></param>
            internal static void _SetEntryPoints(TwEntryPoint entry) => _Memory._entryPoint = entry;

            #region import kernel32.dll

            [DllImport("kernel32.dll", ExactSpelling = true)]
            private static extern IntPtr GlobalAlloc(int flags, int size);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            private static extern IntPtr GlobalLock(IntPtr handle);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            private static extern bool GlobalUnlock(IntPtr handle);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            private static extern IntPtr GlobalFree(IntPtr handle);

            [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
            private static extern void _ZeroMemory(IntPtr dest, IntPtr size);


            #endregion
        }

        /// <summary>
        /// Entry points for platform features.
        /// <para xml:lang="ru">Точки входа для функций платформы.</para>
        /// </summary>
        internal sealed class _Platform {

            /// <summary>
            /// Loads the specified library into the process memory.
            /// <para xml:lang="ru">Загружает указаную библиотеку в память процесса.</para>
            /// </summary>
            /// <param name="fileName">The name of the library.<para xml:lang="ru">Имя библиотеки.</para></param>
            /// <returns>Module descriptor.<para xml:lang="ru">Дескриптор модуля.</para></returns>
            internal static IntPtr Load(string fileName) {
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        return _Platform.LoadLibrary(fileName);
                }
            }

            /// <summary>
            /// Unloads the specified library from the process memory.
            /// <para xml:lang="ru">Выгружает указаную библиотеку из памяти процесса.</para>
            /// </summary>
            /// <param name="hModule">Module descriptor<para xml:lang="ru">Дескриптор модуля</para></param>
            internal static void Unload(IntPtr hModule) {
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        break;
                    default:
                        _Platform.FreeLibrary(hModule);
                        break;
                }
            }

            /// <summary>
            /// Returns the address of the specified procedure.
            /// <para xml:lang="ru">Возвращает адрес указанной процедуры.</para>
            /// </summary>
            /// <param name="hModule">Module descriptor.<para xml:lang="ru">Дескриптор модуля.</para></param>
            /// <param name="procName">The name of the procedure.<para xml:lang="ru">Имя процедуры.</para></param>
            /// <returns>Pointer to a procedure.<para xml:lang="ru">Указатель на процедуру.</para></returns>
            internal static IntPtr GetProcAddr(IntPtr hModule, string procName) {
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        return _Platform.GetProcAddress(hModule, procName);
                }
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            private static extern IntPtr LoadLibrary(string fileName);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            private static extern bool FreeLibrary(IntPtr hModule);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
            private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
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
                    IntPtr _data = Twain32._Memory.Lock(data.Memory.TheMem);
                    try {
                        _res.ImageData = new byte[data.BytesWritten];
                        Marshal.Copy(_data, _res.ImageData, 0, _res.ImageData.Length);
                    } finally {
                        Twain32._Memory.Unlock(data.Memory.TheMem);
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

        #region Delegates

        #region DSM delegates DAT_ variants

        private delegate TwRC _DSMparent([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr refptr);

        private delegate TwRC _DSMraw([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, IntPtr rawData);

        #endregion

        #region DS delegates DAT_ variants to DS

        private delegate TwRC _DSixfer([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr hbitmap);

        private delegate TwRC _DSraw([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, IntPtr arg);

        #endregion

        internal delegate ImageInfo GetImageInfoCallback();

        internal delegate ExtImageInfo GetExtImageInfoCallback(TwEI[] extInfo);

        private delegate void Action<T>(T arg);

        #endregion
    }
}
