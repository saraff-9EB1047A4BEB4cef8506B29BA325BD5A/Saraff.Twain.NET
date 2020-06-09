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
    /// </summary>
    [ToolboxBitmap(typeof(Twain32),"Resources.scanner.bmp")]
    [DebuggerDisplay("ProductName = {_appid.ProductName.Value}, Version = {_appid.Version.Info}, DS = {_srcds.ProductName}")]
    [DefaultEvent("AcquireCompleted")]
    [DefaultProperty("AppProductName")]
    public sealed class Twain32:Component {
        private _DsmEntry _dsmEntry;
        private IntPtr _hTwainDll; //module descriptor twain_32.dll
        private IContainer _components=new Container();
        private IntPtr _hwnd; //handle to the parent window
        private TwIdentity _appid; //application identifier
        private TwIdentity _srcds; //identifier of the current data source
        private _MessageFilter _filter; //WIN32 event filter
        private TwIdentity[] _sources=new TwIdentity[0]; //an array of available data sources
        private ApplicationContext _context=null; //application context. used if there is no main message processing cycle
        private Collection<_Image> _images=new Collection<_Image>();
        private TwainStateFlag _twainState;
        private bool _isTwain2Enable=IntPtr.Size!=4||Environment.OSVersion.Platform==PlatformID.Unix||Environment.OSVersion.Platform==PlatformID.MacOSX;
        private CallBackProc _callbackProc;
        private TwainCapabilities _capabilities;

        /// <summary>
        /// Initializes a new instance of the <see cref="Twain32"/> class.
        /// </summary>
        public Twain32() {
            this._srcds=new TwIdentity();
            this._srcds.Id=0;
            this._filter=new _MessageFilter(this);
            this.ShowUI=true;
            this.DisableAfterAcquire=true;
            this.Palette=new TwainPalette(this);
            this._callbackProc=this._TwCallbackProc;
            switch(Environment.OSVersion.Platform) {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    break;
                default:
                    Form _window=new Form();
                    this._components.Add(_window);
                    this._hwnd=_window.Handle;
                    break;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Twain32"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public Twain32(IContainer container):this() {
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
                if(this._components!=null) {
                    this._components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Opens the data source manager
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool OpenDSM() {
            if((this._TwainState&TwainStateFlag.DSMOpen)==0) {

                #region We load DSM, we receive the address of the entry point DSM_Entry and we bring it to the appropriate delegates

                switch (Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        this._dsmEntry = _DsmEntry.Create(IntPtr.Zero);
                        try {
                            if(this._dsmEntry.DsmRaw == null) {
                                throw new InvalidOperationException("Cann't load DSM.");
                            }
                        } catch(Exception ex) {
                            throw new TwainException("Cann't load DSM.",ex);
                        }
                        break;
                    default:
                        string _twainDsm=Path.ChangeExtension(Path.Combine(Environment.SystemDirectory,"TWAINDSM"),".dll");
                        this._hTwainDll=_Platform.Load(File.Exists(_twainDsm)&&this.IsTwain2Enable?_twainDsm:Path.ChangeExtension(Path.Combine(Environment.SystemDirectory,"..\\twain_32"),".dll"));
                        if(this.Parent!=null) {
                            this._hwnd=this.Parent.Handle;
                        }
                        if(this._hTwainDll!=IntPtr.Zero) {
                            IntPtr _pDsmEntry=_Platform.GetProcAddr(this._hTwainDll,"DSM_Entry");
                            if(_pDsmEntry!=IntPtr.Zero) {
                                this._dsmEntry=_DsmEntry.Create(_pDsmEntry);
                                _Memory._SetEntryPoints(null);
                            } else {
                                throw new TwainException("Cann't find DSM_Entry entry point.");
                            }
                        } else {
                            throw new TwainException("Cann't load DSM.");
                        }
                        break;
                }


                #endregion

                for(TwRC _rc=this._dsmEntry.DsmParent(this._AppId,IntPtr.Zero,TwDG.Control,TwDAT.Parent,TwMSG.OpenDSM,ref this._hwnd); _rc!=TwRC.Success; ) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                this._TwainState|=TwainStateFlag.DSMOpen;

                if(this.IsTwain2Supported) {
                    TwEntryPoint _entry=new TwEntryPoint();
                    for(TwRC _rc=this._dsmEntry.DsmInvoke(this._AppId,TwDG.Control,TwDAT.EntryPoint,TwMSG.Get,ref _entry); _rc!=TwRC.Success; ) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                    _Memory._SetEntryPoints(_entry);
                }

                this._GetAllSorces();
            }
            return (this._TwainState&TwainStateFlag.DSMOpen)!=0;
        }

        /// <summary>
        /// Displays a dialog box for selecting a data source.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool SelectSource() {
            if(Environment.OSVersion.Platform==PlatformID.Unix) {
                throw new NotSupportedException("DG_CONTROL / DAT_IDENTITY / MSG_USERSELECT is not available on Linux.");
            }
            if((this._TwainState&TwainStateFlag.DSOpen)==0) {
                if((this._TwainState&TwainStateFlag.DSMOpen)==0) {
                    this.OpenDSM();
                    if((this._TwainState&TwainStateFlag.DSMOpen)==0) {
                        return false;
                    }
                }
                TwIdentity _src=new TwIdentity();
                for(TwRC _rc=this._dsmEntry.DsmInvoke(this._AppId,TwDG.Control,TwDAT.Identity,TwMSG.UserSelect,ref _src); _rc!=TwRC.Success; ) {
                    if(_rc==TwRC.Cancel) {
                        return false;
                    }
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                this._srcds=_src;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Opens a data source.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool OpenDataSource() {
            if((this._TwainState&TwainStateFlag.DSMOpen)!=0 && (this._TwainState&TwainStateFlag.DSOpen)==0) {
                for(TwRC _rc=this._dsmEntry.DsmInvoke(this._AppId,TwDG.Control,TwDAT.Identity,TwMSG.OpenDS,ref this._srcds); _rc!=TwRC.Success; ) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                this._TwainState|=TwainStateFlag.DSOpen;

                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        this._RegisterCallback();
                        break;
                    default:
                        if(this.IsTwain2Supported&&(this._srcds.SupportedGroups&TwDG.DS2)!=0) {
                            this._RegisterCallback();
                        }
                        break;
                }

            }
            return (this._TwainState&TwainStateFlag.DSOpen)!=0;
        }

        /// <summary>
        /// Registers a data source event handler.
        /// </summary>
        private void _RegisterCallback() {
            TwCallback2 _callback=new TwCallback2 {
                CallBackProc=this._callbackProc
            };
            TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.Callback2,TwMSG.RegisterCallback,ref _callback);
            if(_rc!=TwRC.Success) {
                throw new TwainException(this._GetTwainStatus(),_rc);
            }
        }

        /// <summary>
        /// Activates a data source.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        private bool _EnableDataSource() {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0 && (this._TwainState&TwainStateFlag.DSEnabled)==0) {
                TwUserInterface _guif=new TwUserInterface() {
                    ShowUI=this.ShowUI,
                    ModalUI=this.ModalUI,
                    ParentHand=this._hwnd
                };
                for(TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.UserInterface,TwMSG.EnableDS,ref _guif); _rc!=TwRC.Success; ) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                if((this._TwainState&TwainStateFlag.DSReady)!=0) {
                    this._TwainState&=~TwainStateFlag.DSReady;
                } else {
                    this._TwainState|=TwainStateFlag.DSEnabled;
                }
            }
            return (this._TwainState&TwainStateFlag.DSEnabled)!=0;
        }

        /// <summary>
        /// Gets an image from a data source.
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
                                if(!this.IsTwain2Supported||(this._srcds.SupportedGroups&TwDG.DS2)==0) {
                                    this._filter.SetFilter();
                                }
                                if(!Application.MessageLoop) {
                                    Application.Run(this._context=new ApplicationContext());
                                }
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deactivates the data source.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        private bool _DisableDataSource() {
            if((this._TwainState&TwainStateFlag.DSEnabled)!=0) {
                try {
                    TwUserInterface _guif=new TwUserInterface() {
                        ParentHand=this._hwnd,
                        ShowUI=false
                    };
                    for(TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.UserInterface,TwMSG.DisableDS,ref _guif); _rc!=TwRC.Success; ) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                } finally {
                    this._TwainState&=~TwainStateFlag.DSEnabled;
                    if(this._context!=null) {
                        this._context.ExitThread();
                        this._context.Dispose();
                        this._context=null;
                    }
                }
                return (this._TwainState&TwainStateFlag.DSEnabled)==0;
            }
            return false;
        }

        /// <summary>
        /// Closes the data source.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool CloseDataSource() {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0 && (this._TwainState&TwainStateFlag.DSEnabled)==0) {
                this._images.Clear();
                for(TwRC _rc=this._dsmEntry.DsmInvoke(this._AppId,TwDG.Control,TwDAT.Identity,TwMSG.CloseDS,ref this._srcds); _rc!=TwRC.Success; ) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                this._TwainState&=~TwainStateFlag.DSOpen;
                return (this._TwainState&TwainStateFlag.DSOpen)==0;
            }
            return false;
        }

        /// <summary>
        /// Closes the data source manager.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool CloseDSM() {
            if((this._TwainState&TwainStateFlag.DSEnabled)!=0) {
                this._DisableDataSource();
            }
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                this.CloseDataSource();
            }
            if((this._TwainState&TwainStateFlag.DSMOpen)!=0&&(this._TwainState&TwainStateFlag.DSOpen)==0) {
                for(TwRC _rc=this._dsmEntry.DsmParent(this._AppId,IntPtr.Zero,TwDG.Control,TwDAT.Parent,TwMSG.CloseDSM,ref this._hwnd); _rc!=TwRC.Success; ) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                this._TwainState&=~TwainStateFlag.DSMOpen;
                this._UnloadDSM();
                return (this._TwainState&TwainStateFlag.DSMOpen)==0;
            }
            return false;
        }

        private void _UnloadDSM() {
            this._AppId=null;
            if(this._hTwainDll!=IntPtr.Zero) {
                _Platform.Unload(this._hTwainDll);
                this._hTwainDll=IntPtr.Zero;
            }
        }

        /// <summary>
        /// Returns the scanned image.
        /// </summary>
        /// <param name="index">Image index.</param>
        /// <returns>Instance of the image.</returns>
        public Image GetImage(int index) {
            return this._images[index];
        }

        /// <summary>
        /// Returns the number of scanned images.
        /// </summary>
        [Browsable(false)]
        public int ImageCount {
            get {
                return this._images.Count;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the need to deactivate the data source after receiving the image.
        /// </summary>
        [DefaultValue(true)]
        [Category("Behavior")]
        [Description("Gets or sets a value indicating the need to deactivate the data source after receiving the image.")]
        public bool DisableAfterAcquire {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use TWAIN 2.0.
        /// </summary>
        [DefaultValue(false)]
        [Category("Behavior")]
        [Description("Gets or sets a value indicating whether to use TWAIN 2.0.")]
        public bool IsTwain2Enable {
            get {
                return this._isTwain2Enable;
            }
            set {
                if((this._TwainState&TwainStateFlag.DSMOpen)!=0) {
                    throw new InvalidOperationException("DSM already opened.");
                }
                if(IntPtr.Size!=4&&!value) {
                    throw new InvalidOperationException("In x64 mode only TWAIN 2.x enabled.");
                }
                if(Environment.OSVersion.Platform==PlatformID.Unix&&!value) {
                    throw new InvalidOperationException("On UNIX platform only TWAIN 2.x enabled.");
                }
                if(Environment.OSVersion.Platform==PlatformID.MacOSX&&!value) {
                    throw new InvalidOperationException("On MacOSX platform only TWAIN 2.x enabled.");
                }
                if(this._isTwain2Enable=value) {
                    this._AppId.SupportedGroups|=TwDG.APP2;
                } else {
                    this._AppId.SupportedGroups&=~TwDG.APP2;
                }
                this._AppId.ProtocolMajor=(ushort)(this._isTwain2Enable?2:1);
                this._AppId.ProtocolMinor=(ushort)(this._isTwain2Enable?3:9);
            }
        }

        /// <summary>
        /// Returns true if DSM supports TWAIN 2.0; otherwise false.
        /// </summary>
        [Browsable(false)]
        public bool IsTwain2Supported {
            get {
                if((this._TwainState&TwainStateFlag.DSMOpen)==0) {
                    throw new InvalidOperationException("DSM is not open.");
                }
                return (this._AppId.SupportedGroups&TwDG.DSM2)!=0;
            }
        }

        #region Information of sorces

        /// <summary>
        /// Gets or sets the index of the current data source.
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public int SourceIndex {
            get {
                if((this._TwainState&TwainStateFlag.DSMOpen)!=0) {
                    int i;
                    for(i=0;i<this._sources.Length;i++) {
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
                if((this._TwainState&TwainStateFlag.DSMOpen)!=0) {
                    if((this._TwainState&TwainStateFlag.DSOpen)==0) {
                        this._srcds=this._sources[value];
                    } else {
                        throw new TwainException("The data source is already open.");
                    }
                } else {
                    throw new TwainException("Data Source Manager is not open.");
                }
            }
        }

        /// <summary>
        /// Returns the number of data sources.
        /// </summary>
        [Browsable(false)]
        public int SourcesCount {
            get {
                return this._sources.Length;
            }
        }

        /// <summary>
        /// Returns the name of the data source at the specified index.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <returns>The name of the data source.</returns>
        public string GetSourceProductName(int index) {
            return this._sources[index].ProductName;
        }

        /// <summary>
        /// Gets a description of the specified source.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <returns>Description of the data source.</returns>
        public Identity GetSourceIdentity(int index) {
            return new Identity(this._sources[index]);
        }

        /// <summary>
        /// Returns true if the specified source supports TWAIN 2.0; otherwise false.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>True if the specified source supports TWAIN 2.0; otherwise false.</returns>
        public bool GetIsSourceTwain2Compatible(int index) {
            return (this._sources[index].SupportedGroups&TwDG.DS2)!=0;
        }

        /// <summary>
        /// Sets the specified data source as the default data source.
        /// </summary>
        /// <param name="index">Index.</param>
        public void SetDefaultSource(int index) {
            if((this._TwainState&TwainStateFlag.DSMOpen)!=0) {
                if((this._TwainState&TwainStateFlag.DSOpen)==0) {
                    TwIdentity _src=this._sources[index];
                    TwRC _rc=this._dsmEntry.DsmInvoke(this._AppId,TwDG.Control,TwDAT.Identity,TwMSG.Set,ref _src);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                } else {
                    throw new TwainException("The data source is already open. You must first close the data source.");
                }
            } else {
                throw new TwainException("DSM is not open.");
            }
        }

        /// <summary>
        /// Gets the default Data Source.
        /// </summary>
        /// <returns>Index of default Data Source.</returns>
        /// <exception cref="TwainException">
        /// Could not find default data source.
        /// or
        /// DSM is not open.
        /// </exception>
        public int GetDefaultSource() {
            if((this._TwainState&TwainStateFlag.DSMOpen)!=0) {
                TwIdentity _identity = new TwIdentity();
                for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId,TwDG.Control,TwDAT.Identity,TwMSG.GetDefault,ref _identity); _rc!=TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                for(var i = 0; i<this._sources.Length; i++) {
                    if(_identity.Id==this._sources[i].Id) {
                        return i;
                    }
                }
                throw new TwainException("Could not find default data source.");
            } else {
                throw new TwainException("DSM is not open.");
            }
        }

        #endregion

        #region Properties of source

        /// <summary>
        /// Gets the application identifier.
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        private TwIdentity _AppId {
            get{
                if(this._appid==null) {
                    Assembly _asm=typeof(Twain32).Assembly;
                    AssemblyName _asm_name=new AssemblyName(_asm.FullName);
                    Version _version=new Version(((AssemblyFileVersionAttribute)_asm.GetCustomAttributes(typeof(AssemblyFileVersionAttribute),false)[0]).Version);
                    
                    this._appid=new TwIdentity() {
                        Id=0,
                        Version=new TwVersion() {
                            MajorNum=(ushort)_version.Major,
                            MinorNum=(ushort)_version.Minor,
                            Language=TwLanguage.RUSSIAN,
                            Country=TwCountry.BELARUS,
                            Info=_asm_name.Version.ToString()
                        },
                        ProtocolMajor=(ushort)(this._isTwain2Enable?2:1),
                        ProtocolMinor=(ushort)(this._isTwain2Enable?3:9),
                        SupportedGroups=TwDG.Image|TwDG.Control|(this._isTwain2Enable?TwDG.APP2:0),
                        Manufacturer=((AssemblyCompanyAttribute)_asm.GetCustomAttributes(typeof(AssemblyCompanyAttribute),false)[0]).Company,
                        ProductFamily="TWAIN Class Library",
                        ProductName=((AssemblyProductAttribute)_asm.GetCustomAttributes(typeof(AssemblyProductAttribute),false)[0]).Product
                    };
                }
                return this._appid;
            }
            set {
                if(value!=null) {
                    throw new ArgumentException("Is read only property.");
                }
                this._appid=null;
            }
        }

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        [Category("Behavior")]
        [Description("Gets or sets the name of the application.")]
        public string AppProductName {
            get {
                return this._AppId.ProductName;
            }
            set {
                this._AppId.ProductName=value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to display the UI of the TWAIN source.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(true)]
        [Description("Gets or sets a value indicating whether to display the UI of the TWAIN source.")]
        public bool ShowUI {
            get;
            set;
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        private bool ModalUI {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the parent window for the TWAIN source.
        /// </summary>
        /// <value>
        /// Окно.
        /// </value>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Gets or sets the parent window for the TWAIN source.")]
        public IWin32Window Parent {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the language used by the application. Get or set the primary language for your application.
        /// </summary>
        [Category("Culture")]
        [DefaultValue(TwLanguage.RUSSIAN)]
        [Description("Gets or sets the language used by the application. Get or set the primary language for your application.")]
        public TwLanguage Language {
            get {
                return this._AppId.Version.Language;
            }
            set {
                this._AppId.Version.Language=value;
            }
        }

        /// <summary>
        /// Get or set the primary country where your application is intended to be distributed.
        /// </summary>
        [Category("Culture")]
        [DefaultValue(TwCountry.BELARUS)]
        [Description("Get or set the primary country where your application is intended to be distributed.")]
        public TwCountry Country {
            get {
                return this._AppId.Version.Country;
            }
            set {
                this._AppId.Version.Country=value;
            }
        }

        /// <summary>
        /// Gets or sets the frame of the physical location of the image.
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public RectangleF ImageLayout {
            get {
                TwImageLayout _imageLayout=new TwImageLayout();
                TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Image,TwDAT.ImageLayout,TwMSG.Get,ref _imageLayout);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                return _imageLayout.Frame;
            }
            set {
                TwImageLayout _imageLayout=new TwImageLayout{Frame=value};
                TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Image,TwDAT.ImageLayout,TwMSG.Set,ref _imageLayout);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
            }
        }

        /// <summary>
        /// Returns a set of capabilities (Capabilities).
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public TwainCapabilities Capabilities {
            get {
                if(this._capabilities==null) {
                    this._capabilities=new TwainCapabilities(this);
                }
                return this._capabilities;
            }
        }

        /// <summary>
        /// Returns a set of operations for working with a color palette.
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public TwainPalette Palette {
            get;
            private set;
        }

        /// <summary>
        /// Gets the permissions supported by the data source.
        /// </summary>
        /// <returns>Collection of values.</returns>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        [Obsolete("Use Twain32.Capabilities.XResolution.Get() instead.",true)]
        public Enumeration GetResolutions() {
            return Enumeration.FromObject(this.GetCap(TwCap.XResolution));
        }

        /// <summary>
        /// Sets the current resolution.
        /// </summary>
        /// <param name="value">Resolution.</param>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        [Obsolete("Use Twain32.Capabilities.XResolution.Set(value) and Twain32.Capabilities.YResolution.Set(value) instead.",true)]
        public void SetResolutions(float value) {
            this.SetCap(TwCap.XResolution,value);
            this.SetCap(TwCap.YResolution,value);
        }

        /// <summary>
        /// Returns the pixel types supported by the data source.
        /// </summary>
        /// <returns>Collection of values.</returns>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        [Obsolete("Use Twain32.Capabilities.PixelType.Get() instead.",true)]
        public Enumeration GetPixelTypes() {
            Enumeration _val=Enumeration.FromObject(this.GetCap(TwCap.IPixelType));
            for(int i=0;i<_val.Count;i++) {
                _val[i]=(TwPixelType)_val[i];
            }
            return _val;
        }

        /// <summary>
        /// Sets the current type of pixels.
        /// </summary>
        /// <param name="value">Type of pixels.</param>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        [Obsolete("Use Twain32.Capabilities.PixelType.Set(value) instead.",true)]
        public void SetPixelType(TwPixelType value) {
            this.SetCap(TwCap.IPixelType,value);
        }

        /// <summary>
        /// Returns the units used by the data source.
        /// </summary>
        /// <returns>Units.</returns>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        [Obsolete("Use Twain32.Capabilities.Units.Get() instead.",true)]
        public Enumeration GetUnitOfMeasure() {
            Enumeration _val=Enumeration.FromObject(this.GetCap(TwCap.IUnits));
            for (int i=0; i<_val.Count; i++) {
                _val[i]=(TwUnits)_val[i];
            }
            return _val;
        }

        /// <summary>
        /// Sets the current unit of measure used by the data source.
        /// </summary>
        /// <param name="value">Unit of measurement.</param>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        [Obsolete("Use Twain32.Capabilities.Units.Set(value) instead.",true)]
        public void SetUnitOfMeasure(TwUnits value) {
            this.SetCap(TwCap.IUnits,value);
        }

        #endregion

        #region All capabilities

        /// <summary>
        /// Returns flags indicating operations supported by the data source for the specified capability value.
        /// </summary>
        /// <param name="capability">The value of the TwCap enumeration.</param>
        /// <returns>Set of flags.</returns>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        public TwQC IsCapSupported(TwCap capability) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                TwCapability _cap=new TwCapability(capability);
                try {
                    TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.QuerySupport,ref _cap);
                    if(_rc==TwRC.Success) {
                        return (TwQC)((TwOneValue)_cap.GetValue()).Item;
                    }
                    return 0;
                } finally {
                    _cap.Dispose();
                }
            } else {
                throw new TwainException("The data source is not open.");
            }
        }

        /// <summary>
        /// Returns the value for the specified capability.
        /// </summary>
        /// <param name="capability">The value of the TwCap enumeration.</param>
        /// <param name="msg">The value of the TwMSG enumeration.</param>
        /// <returns>Depending on the value of capability, the following can be returned: type-value, array, <see cref="Twain32.Range">range</see>, <see cref="Twain32.Enumeration">transfer</see>.</returns>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        private object _GetCapCore(TwCap capability,TwMSG msg) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                TwCapability _cap=new TwCapability(capability);
                try {
                    TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.Capability,msg,ref _cap);
                    if(_rc==TwRC.Success) {
                        switch(_cap.ConType) {
                            case TwOn.One:
                                object _valueRaw=_cap.GetValue();
                                TwOneValue _value=_valueRaw as TwOneValue;
                                if(_value!=null) {
                                    return TwTypeHelper.CastToCommon(_value.ItemType,TwTypeHelper.ValueToTw<uint>(_value.ItemType,_value.Item));
                                } else {
                                    return _valueRaw;
                                }
                            case TwOn.Range:
                                return Range.CreateRange((TwRange)_cap.GetValue());
                            case TwOn.Array:
                                return ((__ITwArray)_cap.GetValue()).Items;
                            case TwOn.Enum:
                                __ITwEnumeration _enum=_cap.GetValue() as __ITwEnumeration;
                                return Enumeration.CreateEnumeration(_enum.Items,_enum.CurrentIndex,_enum.DefaultIndex);
                        }
                        return _cap.GetValue();
                    } else {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                } finally {
                    _cap.Dispose();
                }
            } else {
                throw new TwainException("The data source is not open.");
            }
        }

        /// <summary>
        /// Gets the values of the specified feature (capability).
        /// </summary>
        /// <param name="capability">The value of the TwCap enumeration.</param>
        /// <returns>Depending on the value of capability, the following can be returned: type-value, array, <see cref="Twain32.Range">range</see>, <see cref="Twain32.Enumeration">transfer</see>.</returns>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        public object GetCap(TwCap capability) {
            return this._GetCapCore(capability,TwMSG.Get);
        }

        /// <summary>
        /// Returns the current value for the specified feature. (capability).
        /// </summary>
        /// <param name="capability">The value of the TwCap enumeration.</param>
        /// <returns>Depending on the value of capability, the following can be returned: type-value, array, <see cref="Twain32.Range">range</see>, <see cref="Twain32.Enumeration">transfer</see>.</returns>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        public object GetCurrentCap(TwCap capability) {
            return this._GetCapCore(capability,TwMSG.GetCurrent);
        }

        /// <summary>
        /// Returns the default value for the specified feature. (capability).
        /// </summary>
        /// <param name="capability">The value of the TwCap enumeration.</param>
        /// <returns>Depending on the value of capability, the following can be returned: type-value, array, <see cref="Twain32.Range">range</see>, <see cref="Twain32.Enumeration">transfer</see>.</returns>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        public object GetDefaultCap(TwCap capability) {
            return this._GetCapCore(capability,TwMSG.GetDefault);
        }

        /// <summary>
        /// Resets the current value for the specified <see cref="TwCap">capability</see> to default value.
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.</param>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        public void ResetCap(TwCap capability) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                TwCapability _cap=new TwCapability(capability);
                try {
                    TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Reset,ref _cap);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                } finally {
                    _cap.Dispose();
                }
            } else {
                throw new TwainException("The data source is not open.");
            }
        }

        /// <summary>
        /// Resets the current value of all current values to the default values.
        /// </summary>
        /// <exception cref="TwainException">It is activated in case of an error during the operation.</exception>
        public void ResetAllCap() {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                TwCapability _cap=new TwCapability(TwCap.SupportedCaps);
                try {
                    TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.ResetAll,ref _cap);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                } finally {
                    _cap.Dispose();
                }
            } else {
                throw new TwainException("The data source is not open.");
            }
        }

        private void _SetCapCore(TwCapability cap,TwMSG msg) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                try {
                    TwRC _rc = this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.Capability,msg,ref cap);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                } finally {
                    cap.Dispose();
                }
            } else {
                throw new TwainException("The data source is not open.");
            }
        }

        private void _SetCapCore(TwCap capability,TwMSG msg,object value) {
            TwCapability _cap = null;
            if(value is string) {
                object[] _attrs = typeof(TwCap).GetField(capability.ToString())?.GetCustomAttributes(typeof(TwTypeAttribute),false);
                if(_attrs?.Length>0) {
                    _cap=new TwCapability(capability,(string)value,((TwTypeAttribute)_attrs[0]).TwType);
                } else {
                    _cap=new TwCapability(capability,(string)value,TwTypeHelper.TypeOf(value));
                }
            } else {
                TwType _type = TwTypeHelper.TypeOf(value.GetType());
                _cap=new TwCapability(capability,TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type,value)),_type);
            }
            this._SetCapCore(_cap,msg);
        }

        private void _SetCapCore(TwCap capability,TwMSG msg,object[] value) {
            var _attrs = typeof(TwCap).GetField(capability.ToString())?.GetCustomAttributes(typeof(TwTypeAttribute),false);
            this._SetCapCore(
                new TwCapability(
                    capability,
                    new TwArray() {
                        ItemType=_attrs?.Length>0 ? ((TwTypeAttribute)(_attrs[0])).TwType : TwTypeHelper.TypeOf(value[0]),
                        NumItems=(uint)value.Length
                    },
                    value),
                msg);
        }

        private void _SetCapCore(TwCap capability,TwMSG msg,Range value) {
            this._SetCapCore(new TwCapability(capability,value.ToTwRange()),msg);
        }

        private void _SetCapCore(TwCap capability,TwMSG msg,Enumeration value) {
            var _attrs = typeof(TwCap).GetField(capability.ToString())?.GetCustomAttributes(typeof(TwTypeAttribute),false);
            this._SetCapCore(
                new TwCapability(
                    capability,
                    new TwEnumeration {
                        ItemType=_attrs?.Length>0 ? ((TwTypeAttribute)(_attrs[0])).TwType : TwTypeHelper.TypeOf(value[0]),
                        NumItems=(uint)value.Count,
                        CurrentIndex=(uint)value.CurrentIndex,
                        DefaultIndex=(uint)value.DefaultIndex
                    },
                    value.Items),
                msg);
        }

        /// <summary>
        /// Sets the value for the specified <see cref="TwCap">capability</see>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="TwainException">Occurs when the data source is not open.</exception>
        public void SetCap(TwCap capability,object value) {
            this._SetCapCore(capability,TwMSG.Set,value);
        }

        /// <summary>
        /// Sets the value for the specified <see cref="TwCap">capability</see>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="TwainException">Occurs when the data source is not open.</exception>
        public void SetCap(TwCap capability,object[] value) {
            this._SetCapCore(capability,TwMSG.Set,value);
        }

        /// <summary>
        /// Sets the value for the specified <see cref="TwCap">capability</see>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="TwainException">Occurs when the data source is not open.</exception>
        public void SetCap(TwCap capability,Range value) {
            this._SetCapCore(capability,TwMSG.Set,value);
        }

        /// <summary>
        /// Sets the value for the specified <see cref="TwCap">capability</see>
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="TwainException">Occurs when the data source is not open.</exception>
        public void SetCap(TwCap capability,Enumeration value) {
            this._SetCapCore(capability,TwMSG.Set,value);
        }

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="TwainException">Occurs when the data source is not open.</exception>
        public void SetConstraintCap(TwCap capability,object value) {
            this._SetCapCore(capability,TwMSG.SetConstraint,value);
        }

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="TwainException">Occurs when the data source is not open.</exception>
        public void SetConstraintCap(TwCap capability,object[] value) {
            this._SetCapCore(capability,TwMSG.SetConstraint,value);
        }

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="TwainException">Occurs when the data source is not open.</exception>
        public void SetConstraintCap(TwCap capability,Range value) {
            this._SetCapCore(capability,TwMSG.SetConstraint,value);
        }

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// </summary>
        /// <param name="capability">Listing Value <see cref="TwCap"/>.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="TwainException">Occurs when the data source is not open.</exception>
        public void SetConstraintCap(TwCap capability,Enumeration value) {
            this._SetCapCore(capability,TwMSG.SetConstraint,value);
        }

        #endregion

        #region DG_IMAGE / IMAGExxxxXFER / MSG_GET operation

        /// <summary>
        /// Performs image transfer (Native Mode Transfer).
        /// </summary>
        private void _NativeTransferPictures() {
            if(this._srcds.Id==0) {
                return;
            }
            IntPtr _hBitmap=IntPtr.Zero;
            TwPendingXfers _pxfr=new TwPendingXfers();
            try {
                this._images.Clear();

                do {
                    _pxfr.Count=0;
                    _hBitmap=IntPtr.Zero;

                    for(TwRC _rc=this._dsmEntry.DSImageXfer(this._AppId,this._srcds,TwDG.Image,TwDAT.ImageNativeXfer,TwMSG.Get,ref _hBitmap); _rc!=TwRC.XferDone; ) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                    // DG_IMAGE / DAT_IMAGEINFO / MSG_GET
                    // DG_IMAGE / DAT_EXTIMAGEINFO / MSG_GET
                    if(this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo,this._GetExtImageInfo))) {
                        return;
                    }

                    IntPtr _pBitmap=_Memory.Lock(_hBitmap);
                    try {
                        _Image _img=null;

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
                        _img = _handler.PtrToStream(_pBitmap,this.GetService(typeof(IStreamProvider)) as IStreamProvider);

                        this._images.Add(_img);
                        if(this._OnEndXfer(new EndXferEventArgs(_img))) {
                            return;
                        }
                    } finally {
                        _Memory.Unlock(_hBitmap);
                        _Memory.Free(_hBitmap);
                    }
                    for(TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.EndXfer,ref _pxfr); _rc!=TwRC.Success; ) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                } while(_pxfr.Count!=0);
            } finally {
                TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.Reset,ref _pxfr);
            }
        }

        /// <summary>
        /// Performs image transfer (Disk File Mode Transfer).
        /// </summary>
        private void _FileTransferPictures() {
            if(this._srcds.Id==0) {
                return;
            }

            TwPendingXfers _pxfr=new TwPendingXfers();
            try {
                this._images.Clear();
                do {
                    _pxfr.Count=0;

                    SetupFileXferEventArgs _args=new SetupFileXferEventArgs();
                    if(this._OnSetupFileXfer(_args)) {
                        return;
                    }

                    TwSetupFileXfer _fileXfer=new TwSetupFileXfer {
                        Format=this.Capabilities.ImageFileFormat.IsSupported(TwQC.GetCurrent)?this.Capabilities.ImageFileFormat.GetCurrent():TwFF.Bmp,
                        FileName=string.IsNullOrEmpty(_args.FileName)?Path.GetTempFileName():_args.FileName
                    };

                    for(TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.SetupFileXfer,TwMSG.Set,ref _fileXfer); _rc!=TwRC.Success; ) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }

                    for(TwRC _rc=this._dsmEntry.DsRaw(this._AppId,this._srcds,TwDG.Image,TwDAT.ImageFileXfer,TwMSG.Get,IntPtr.Zero); _rc!=TwRC.XferDone; ) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                    // DG_IMAGE / DAT_IMAGEINFO / MSG_GET
                    // DG_IMAGE / DAT_EXTIMAGEINFO / MSG_GET
                    if(this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo,this._GetExtImageInfo))) {
                        return;
                    }

                    for(TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.EndXfer,ref _pxfr); _rc!=TwRC.Success; ) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                    for(TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.SetupFileXfer,TwMSG.Get,ref _fileXfer); _rc!=TwRC.Success; ) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                    if(this._OnFileXfer(new FileXferEventArgs(ImageFileXfer.Create(_fileXfer)))) {
                        return;
                    }
                } while(_pxfr.Count!=0);
            } finally {
                TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.Reset,ref _pxfr);
            }
        }

        /// <summary>
        /// Performs image transfer (Buffered Memory Mode Transfer and Memory File Mode Transfer).
        /// </summary>
        private void _MemoryTransferPictures(bool isMemFile) {
            if(this._srcds.Id==0) {
                return;
            }

            TwPendingXfers _pxfr=new TwPendingXfers();
            try {
                this._images.Clear();
                do {
                    _pxfr.Count=0;
                    ImageInfo _info=this._GetImageInfo();

                    if(isMemFile) {
                        if((this.Capabilities.ImageFileFormat.IsSupported()&TwQC.GetCurrent)!=0) {
                            TwSetupFileXfer _fileXfer=new TwSetupFileXfer {
                                Format=this.Capabilities.ImageFileFormat.GetCurrent()
                            };
                            for(TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.SetupFileXfer,TwMSG.Set,ref _fileXfer); _rc!=TwRC.Success; ) {
                                throw new TwainException(this._GetTwainStatus(),_rc);
                            }
                        }
                    }

                    TwSetupMemXfer _memBufSize=new TwSetupMemXfer();

                    for(TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.SetupMemXfer,TwMSG.Get,ref _memBufSize); _rc!=TwRC.Success; ) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                    if(this._OnSetupMemXfer(new SetupMemXferEventArgs(_info,_memBufSize.Preferred))) {
                        return;
                    }

                    IntPtr _hMem=_Memory.Alloc((int)_memBufSize.Preferred);
                    if(_hMem==IntPtr.Zero) {
                        throw new TwainException("Error allocating memory.");
                    }
                    try {
                        TwMemory _mem=new TwMemory {
                            Flags=TwMF.AppOwns|TwMF.Pointer,
                            Length=_memBufSize.Preferred,
                            TheMem=_Memory.Lock(_hMem)
                        };

                        do {
                            TwImageMemXfer _memXferBuf=new TwImageMemXfer {Memory=_mem};
                            _Memory.ZeroMemory(_memXferBuf.Memory.TheMem,(IntPtr)_memXferBuf.Memory.Length);

                            TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Image,isMemFile?TwDAT.ImageMemFileXfer:TwDAT.ImageMemXfer,TwMSG.Get,ref _memXferBuf);
                            if(_rc!=TwRC.Success&&_rc!=TwRC.XferDone) {
                                TwCC _cc=this._GetTwainStatus();
                                TwRC _rc2=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.EndXfer,ref _pxfr);
                                throw new TwainException(_cc,_rc);
                            }
                            if(this._OnMemXfer(new MemXferEventArgs(_info,ImageMemXfer.Create(_memXferBuf)))) {
                                return;
                            }
                            if(_rc==TwRC.XferDone) {
                                // DG_IMAGE / DAT_IMAGEINFO / MSG_GET
                                // DG_IMAGE / DAT_EXTIMAGEINFO / MSG_GET
                                if(this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo,this._GetExtImageInfo))) {
                                    return;
                                }
                                break;
                            }
                        } while(true);
                    } finally {
                        _Memory.Unlock(_hMem);
                        _Memory.Free(_hMem);
                    }
                    for(TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.EndXfer,ref _pxfr); _rc!=TwRC.Success; ) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                } while(_pxfr.Count!=0);
            } finally {
                TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.Reset,ref _pxfr);
            }
        }

        #endregion

        #region DS events handler

        /// <summary>
        /// A data source event handler.
        /// </summary>
        /// <param name="appId">Description of the application.</param>
        /// <param name="srcId">Description of the data source.</param>
        /// <param name="dg">Description of the data group.</param>
        /// <param name="dat">Description of the data.</param>
        /// <param name="msg">Message.</param>
        /// <param name="data">Data.</param>
        /// <returns>Result event handlers.</returns>
        private TwRC _TwCallbackProc(TwIdentity srcId,TwIdentity appId,TwDG dg,TwDAT dat,TwMSG msg,IntPtr data) {
            try {
                if(appId==null||appId.Id!=this._AppId.Id) {
                    return TwRC.Failure;
                }

                if((this._TwainState&TwainStateFlag.DSEnabled)==0) {
                    this._TwainState|=TwainStateFlag.DSEnabled|TwainStateFlag.DSReady;
                }

                this._TwCallbackProcCore(msg,isCloseReq => {
                    if(isCloseReq||this.DisableAfterAcquire) {
                        this._DisableDataSource();
                    }
                });
            } catch(Exception ex) {
                this._OnAcquireError(new AcquireErrorEventArgs(new TwainException(ex.Message,ex)));
            }
            return TwRC.Success;
        }

        /// <summary>
        /// An internal data source event handler.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <param name="endAction">The action that completes the processing of the event.</param>
        private void _TwCallbackProcCore(TwMSG msg,Action<bool> endAction) {
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
            TwDeviceEvent _deviceEvent=new TwDeviceEvent();
            if(this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.DeviceEvent,TwMSG.Get,ref _deviceEvent)==TwRC.Success) {
                this._OnDeviceEvent(new DeviceEventEventArgs(_deviceEvent));
            }
        }

        #endregion

        #region Raise events

        private void _OnAcquireCompleted(EventArgs e) {
            if(this.AcquireCompleted!=null) {
                this.AcquireCompleted(this,e);
            }
        }

        private void _OnAcquireError(AcquireErrorEventArgs e) {
            if(this.AcquireError!=null) {
                this.AcquireError(this,e);
            }
        }

        private bool _OnXferDone(XferDoneEventArgs e) {
            if(this.XferDone!=null) {
                this.XferDone(this,e);
            }
            return e.Cancel;
        }

        private bool _OnEndXfer(EndXferEventArgs e) {
            if(this.EndXfer!=null) {
                this.EndXfer(this,e);
            }
            return e.Cancel;
        }

        private bool _OnSetupMemXfer(SetupMemXferEventArgs e) {
            if(this.SetupMemXferEvent!=null) {
                this.SetupMemXferEvent(this,e);
            }
            return e.Cancel;
        }

        private bool _OnMemXfer(MemXferEventArgs e) {
            if(this.MemXferEvent!=null) {
                this.MemXferEvent(this,e);
            }
            return e.Cancel;
        }

        private bool _OnSetupFileXfer(SetupFileXferEventArgs e) {
            if(this.SetupFileXferEvent!=null) {
                this.SetupFileXferEvent(this,e);
            }
            return e.Cancel;
        }

        private bool _OnFileXfer(FileXferEventArgs e) {
            if(this.FileXferEvent!=null) {
                this.FileXferEvent(this,e);
            }
            return e.Cancel;
        }

        private void _OnDeviceEvent(DeviceEventEventArgs e) {
            if(this.DeviceEvent!=null) {
                this.DeviceEvent(this,e);
            }
        }

        #endregion

        /// <summary>
        /// Gets a description of all available data sources.
        /// </summary>
        private void _GetAllSorces() {
            List<TwIdentity> _src=new List<TwIdentity>();
            TwIdentity _item=new TwIdentity();
            try {
                for(TwRC _rc=this._dsmEntry.DsmInvoke(this._AppId,TwDG.Control,TwDAT.Identity,TwMSG.GetFirst,ref _item); _rc!=TwRC.Success; ) {
                    if(_rc==TwRC.EndOfList) {
                        return;
                    }
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                _src.Add(_item);
                while(true) {
                    _item=new TwIdentity();
                    TwRC _rc=this._dsmEntry.DsmInvoke(this._AppId,TwDG.Control,TwDAT.Identity,TwMSG.GetNext,ref _item);
                    if(_rc==TwRC.Success) {
                        _src.Add(_item);
                        continue;
                    }
                    if(_rc==TwRC.EndOfList) {
                        break;
                    }
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                for(TwRC _rc=this._dsmEntry.DsmInvoke(this._AppId,TwDG.Control,TwDAT.Identity,TwMSG.GetDefault,ref _srcds); _rc!=TwRC.Success; ) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
            } finally {
                this._sources=_src.ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the value of the status flags.
        /// </summary>
        private TwainStateFlag _TwainState {
            get {
                return this._twainState;
            }
            set {
                if(this._twainState!=value) {
                    this._twainState=value;
                    if(this.TwainStateChanged!=null) {
                        this.TwainStateChanged(this,new TwainStateEventArgs(this._twainState));
                    }
                }
            }
        }

        /// <summary>
        /// Returns the TWAIN status code.
        /// </summary>
        /// <returns></returns>
        private TwCC _GetTwainStatus() {
            TwStatus _status=new TwStatus();
            TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Control,TwDAT.Status,TwMSG.Get,ref _status);
            return _status.ConditionCode;
        }

        /// <summary>
        /// Returns a description of the received image.
        /// </summary>
        /// <returns>Description of the image.</returns>
        private ImageInfo _GetImageInfo() {
            TwImageInfo _imageInfo=new TwImageInfo();
            TwRC _rc=this._dsmEntry.DsInvoke(this._AppId,this._srcds,TwDG.Image,TwDAT.ImageInfo,TwMSG.Get,ref _imageInfo);
            if(_rc!=TwRC.Success) {
                throw new TwainException(this._GetTwainStatus(),_rc);
            }
            return ImageInfo.FromTwImageInfo(_imageInfo);
        }

        /// <summary>
        /// Returns an extended description of the resulting image.
        /// </summary>
        /// <param name="extInfo">A set of codes for the extended image description for which you want to get a description.</param>
        /// <returns>Extended image description.</returns>
        private ExtImageInfo _GetExtImageInfo(TwEI[] extInfo) {
            TwInfo[] _info=new TwInfo[extInfo.Length];
            for(int i=0; i<extInfo.Length; i++) {
                _info[i]=new TwInfo {InfoId=extInfo[i]};
            }
            IntPtr _extImageInfo=TwExtImageInfo.ToPtr(_info);
            try {

                TwRC _rc=this._dsmEntry.DsRaw(this._AppId,this._srcds,TwDG.Image,TwDAT.ExtImageInfo,TwMSG.Get,_extImageInfo);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                return ExtImageInfo.FromPtr(_extImageInfo);
            } finally {
                Marshal.FreeHGlobal(_extImageInfo);
            }
        }

        /// <summary>
        /// State flags.
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
        /// </summary>
        [Category("Action")]
        [Description("Occurs when the acquire is completed.")]
        public event EventHandler AcquireCompleted;

        /// <summary>
        /// Occurs when error received during acquire.
        /// </summary>
        [Category("Action")]
        [Description("Occurs when error received during acquire.")]
        public event EventHandler<AcquireErrorEventArgs> AcquireError;

        /// <summary>
        /// Occurs when the transfer into application was completed (Native Mode Transfer).
        /// </summary>
        [Category("Native Mode Action")]
        [Description("Occurs when the transfer into application was completed (Native Mode Transfer).")]
        public event EventHandler<EndXferEventArgs> EndXfer;

        /// <summary>
        /// Occurs when the transfer was completed.
        /// </summary>
        [Category("Action")]
        [Description("Occurs when the transfer was completed.")]
        public event EventHandler<XferDoneEventArgs> XferDone;

        /// <summary>
        /// Occurs when determined size of buffer to use during the transfer (Memory Mode Transfer and MemFile Mode Transfer).
        /// </summary>
        [Category("Memory Mode Action")]
        [Description("Occurs when determined size of buffer to use during the transfer (Memory Mode Transfer and MemFile Mode Transfer).")]
        public event EventHandler<SetupMemXferEventArgs> SetupMemXferEvent;

        /// <summary>
        /// Occurs when the memory block for the data was recived (Memory Mode Transfer and MemFile Mode Transfer).
        /// </summary>
        [Category("Memory Mode Action")]
        [Description("Occurs when the memory block for the data was recived (Memory Mode Transfer and MemFile Mode Transfer).")]
        public event EventHandler<MemXferEventArgs> MemXferEvent;

        /// <summary>
        /// Occurs when you need to specify the filename (File Mode Transfer).
        /// </summary>
        [Category("File Mode Action")]
        [Description("Occurs when you need to specify the filename. (File Mode Transfer)")]
        public event EventHandler<SetupFileXferEventArgs> SetupFileXferEvent;

        /// <summary>
        /// Occurs when the transfer into application was completed (File Mode Transfer).
        /// </summary>
        [Category("File Mode Action")]
        [Description("Occurs when the transfer into application was completed (File Mode Transfer).")]
        public event EventHandler<FileXferEventArgs> FileXferEvent;

        /// <summary>
        /// Occurs when TWAIN state was changed.
        /// </summary>
        [Category("Behavior")]
        [Description("Occurs when TWAIN state was changed.")]
        public event EventHandler<TwainStateEventArgs> TwainStateChanged;

        /// <summary>
        /// Occurs when enabled the source sends this message to the Application to alert it that some event has taken place.
        /// </summary>
        [Category("Behavior")]
        [Description("Occurs when enabled the source sends this message to the Application to alert it that some event has taken place.")]
        public event EventHandler<DeviceEventEventArgs> DeviceEvent;

        #endregion

        #region Events Args

        /// <summary>
        /// Arguments for the EndXfer event.
        /// </summary>
        [Serializable]
        public sealed class EndXferEventArgs:SerializableCancelEventArgs {
            private _Image _image;

            /// <summary>
            /// Initializes a new instance of the class.
            /// </summary>
            /// <param name="image">Image.</param>
            internal EndXferEventArgs(object image) {
                this._image=image as _Image;
            }

            public T CreateImage<T>(IImageFactory<T> factory) where T : class {
                return factory.Create(this._image);
            }

            /// <summary>
            /// Returns the image.
            /// </summary>
            public Image Image {
                get {
                    return this._image;
                }
            }

#if !NET2            
            /// <summary>
            /// Returns the image.
            /// </summary>
            public System.Windows.Media.ImageSource ImageSource {
                get {
                    return this._image;
                }
            }
#endif
        }

        /// <summary>
        /// Arguments for the XferDone event.
        /// </summary>
        public sealed class XferDoneEventArgs:SerializableCancelEventArgs {
            private GetImageInfoCallback _imageInfoMethod;
            private GetExtImageInfoCallback _extImageInfoMethod;

            /// <summary>
            /// Initializes a new instance of the class <see cref="XferDoneEventArgs"/>.
            /// </summary>
            /// <param name="method1">Callback method to get image description.</param>
            /// <param name="method2">Callback method to get an extended image description.</param>
            internal XferDoneEventArgs(GetImageInfoCallback method1,GetExtImageInfoCallback method2) {
                this._imageInfoMethod=method1;
                this._extImageInfoMethod=method2;
            }

            /// <summary>
            /// Returns a description of the received image.
            /// </summary>
            /// <returns>Description of the image.</returns>
            public ImageInfo GetImageInfo() {
                return this._imageInfoMethod();
            }

            /// <summary>
            /// Returns an extended description of the resulting image.
            /// </summary>
            /// <param name="extInfo">A set of codes for the extended image description for which you want to get a description.</param>
            /// <returns>Extended image description.</returns>
            public ExtImageInfo GetExtImageInfo(params TwEI[] extInfo) {
                return this._extImageInfoMethod(extInfo);
            }
        }

        /// <summary>
        /// Arguments for the SetupMemXferEvent event.
        /// </summary>
        [Serializable]
        public sealed class SetupMemXferEventArgs:SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="SetupMemXferEventArgs"/>.
            /// </summary>
            /// <param name="info">Description of the image.</param>
            /// <param name="bufferSize">The size of the memory buffer for data transfer.</param>
            internal SetupMemXferEventArgs(ImageInfo info,uint bufferSize) {
                this.ImageInfo=info;
                this.BufferSize=bufferSize;
            }

            /// <summary>
            /// Returns a description of the image.
            /// </summary>
            public ImageInfo ImageInfo {
                get;
                private set;
            }

            /// <summary>
            /// Gets the size of the memory buffer for data transfer.
            /// </summary>
            public uint BufferSize {
                get;
                private set;
            }
        }

        /// <summary>
        /// Arguments for the MemXferEvent event.
        /// </summary>
        [Serializable]
        public sealed class MemXferEventArgs:SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="MemXferEventArgs"/>.
            /// </summary>
            /// <param name="info">Description of the image.</param>
            /// <param name="image">A fragment of image data.</param>
            internal MemXferEventArgs(ImageInfo info,ImageMemXfer image) {
                this.ImageInfo=info;
                this.ImageMemXfer=image;
            }

            /// <summary>
            /// Returns a description of the image.
            /// </summary>
            public ImageInfo ImageInfo {
                get;
                private set;
            }

            /// <summary>
            /// Returns a piece of image data.
            /// </summary>
            public ImageMemXfer ImageMemXfer {
                get;
                private set;
            }
        }

        /// <summary>
        /// Arguments for the SetupFileXferEvent event.
        /// </summary>
        [Serializable]
        public sealed class SetupFileXferEventArgs:SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="SetupFileXferEventArgs"/>.
            /// </summary>
            internal SetupFileXferEventArgs() {
            }

            /// <summary>
            /// Gets or sets the name of the image file.
            /// </summary>
            public string FileName {
                get;
                set;
            }
        }

        /// <summary>
        /// Arguments for the FileXferEvent event.
        /// </summary>
        [Serializable]
        public sealed class FileXferEventArgs:SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="FileXferEventArgs"/>.
            /// </summary>
            /// <param name="image">Description of the image file.</param>
            internal FileXferEventArgs(ImageFileXfer image) {
                this.ImageFileXfer=image;
            }

            /// <summary>
            /// Returns a description of the image file.
            /// </summary>
            public ImageFileXfer ImageFileXfer {
                get;
                private set;
            }
        }

        /// <summary>
        /// Arguments for the TwainStateChanged event.
        /// </summary>
        [Serializable]
        public sealed class TwainStateEventArgs:EventArgs {

            /// <summary>
            /// Initializes a new instance of the class.
            /// </summary>
            /// <param name="flags">State flags.</param>
            internal TwainStateEventArgs(TwainStateFlag flags) {
                this.TwainState=flags;
            }

            /// <summary>
            /// Returns the status flags of a twain device.
            /// </summary>
            public TwainStateFlag TwainState {
                get;
                private set;
            }
        }

        /// <summary>
        /// Arguments for the DeviceEvent event.
        /// </summary>
        public sealed class DeviceEventEventArgs:EventArgs {
            private TwDeviceEvent _deviceEvent;

            internal DeviceEventEventArgs(TwDeviceEvent deviceEvent) {
                this._deviceEvent=deviceEvent;
            }

            /// <summary>
            /// One of the TWDE_xxxx values.
            /// </summary>
            public TwDE Event {
                get {
                    return this._deviceEvent.Event;
                }
            }

            /// <summary>
            /// The name of the device that generated the event.
            /// </summary>
            public string DeviceName {
                get {
                    return this._deviceEvent.DeviceName;
                }
            }

            /// <summary>
            /// Battery Minutes Remaining.
            /// </summary>
            public uint BatteryMinutes {
                get {
                    return this._deviceEvent.BatteryMinutes;
                }
            }

            /// <summary>
            /// Battery Percentage Remaining.
            /// </summary>
            public short BatteryPercentAge {
                get {
                    return this._deviceEvent.BatteryPercentAge;
                }
            }

            /// <summary>
            /// Power Supply.
            /// </summary>
            public int PowerSupply {
                get {
                    return this._deviceEvent.PowerSupply;
                }
            }

            /// <summary>
            /// Resolution.
            /// </summary>
            public float XResolution {
                get {
                    return this._deviceEvent.XResolution;
                }
            }

            /// <summary>
            /// Resolution.
            /// </summary>
            public float YResolution {
                get {
                    return this._deviceEvent.YResolution;
                }
            }

            /// <summary>
            /// Flash Used2.
            /// </summary>
            public uint FlashUsed2 {
                get {
                    return this._deviceEvent.FlashUsed2;
                }
            }

            /// <summary>
            /// Automatic Capture.
            /// </summary>
            public uint AutomaticCapture {
                get {
                    return this._deviceEvent.AutomaticCapture;
                }
            }

            /// <summary>
            /// Automatic Capture.
            /// </summary>
            public uint TimeBeforeFirstCapture {
                get {
                    return this._deviceEvent.TimeBeforeFirstCapture;
                }
            }

            /// <summary>
            /// Automatic Capture.
            /// </summary>
            public uint TimeBetweenCaptures {
                get {
                    return this._deviceEvent.TimeBetweenCaptures;
                }
            }
        }

        /// <summary>
        /// Arguments for the AcquireError event.
        /// </summary>
        [Serializable]
        public sealed class AcquireErrorEventArgs:EventArgs {

            /// <summary>
            /// Initializes a new instance of the class.
            /// </summary>
            /// <param name="ex">An instance of the exception class.</param>
            internal AcquireErrorEventArgs(TwainException ex) {
                this.Exception=ex;
            }

            /// <summary>
            /// Gets an instance of the exception class.
            /// </summary>
            public TwainException Exception {
                get;
                private set;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="System.EventArgs" />
        [Serializable]
        public class SerializableCancelEventArgs:EventArgs {

            /// <summary>
            /// Gets or sets a value indicating whether the event should be canceled.
            /// </summary>
            /// <value>
            /// <c>true</c> if cancel; otherwise, <c>false</c>.
            /// </value>
            public bool Cancel {
                get;
                set;
            }
        }

        #endregion

        #region Nested classes

        /// <summary>
        /// Entry points for working with DSM.
        /// </summary>
        private sealed class _DsmEntry {

            /// <summary>
            /// Initializes a new instance of the class <see cref="_DsmEntry"/>.
            /// </summary>
            /// <param name="ptr">Pointer to DSM_Entry.</param>
            private _DsmEntry(IntPtr ptr) {
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                        this.DsmParent=_DsmEntry._LinuxDsmParent;
                        this.DsmRaw=_DsmEntry._LinuxDsmRaw;
                        this.DSImageXfer=_DsmEntry._LinuxDsImageXfer;
                        this.DsRaw=_DsmEntry._LinuxDsRaw;
                        break;
                    case PlatformID.MacOSX:
                        this.DsmParent = _DsmEntry._MacosxDsmParent;
                        this.DsmRaw = _DsmEntry._MacosxDsmRaw;
                        this.DSImageXfer = _DsmEntry._MacosxDsImageXfer;
                        this.DsRaw = _DsmEntry._MacosxDsRaw;
                        break;
                    default:
                        MethodInfo _createDelegate=typeof(_DsmEntry).GetMethod("CreateDelegate",BindingFlags.Static|BindingFlags.NonPublic);
                        foreach(PropertyInfo _prop in typeof(_DsmEntry).GetProperties()) {
                            _prop.SetValue(this,_createDelegate.MakeGenericMethod(_prop.PropertyType).Invoke(this,new object[] { ptr }),null);
                        }
                        break;
                }
            }

            /// <summary>
            /// Creates and returns a new instance of the class <see cref="_DsmEntry"/>.
            /// </summary>
            /// <param name="ptr">Pointer to DSM_Entry.</param>
            /// <returns>Class instance <see cref="_DsmEntry"/>.</returns>
            public static _DsmEntry Create(IntPtr ptr) {
                return new _DsmEntry(ptr);
            }

            /// <summary>
            /// Casts a pointer to the requested delegate.
            /// </summary>
            /// <typeparam name="T">Delegate Required.</typeparam>
            /// <param name="ptr">Pointer to DSM_Entry.</param>
            /// <returns>Delegate.</returns>
            private static T CreateDelegate<T>(IntPtr ptr) where T:class {
                return Marshal.GetDelegateForFunctionPointer(ptr,typeof(T)) as T;
            }

            public TwRC DsmInvoke<T>(TwIdentity origin,TwDG dg,TwDAT dat,TwMSG msg,ref T data) where T:class {
                if(data==null) {
                    throw new ArgumentNullException();
                }
                IntPtr _data=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
                try {
                    Marshal.StructureToPtr(data,_data,true);

                    TwRC _rc=this.DsmRaw(origin,IntPtr.Zero,dg,dat,msg,_data);
                    if(_rc==TwRC.Success) {
                        data=(T)Marshal.PtrToStructure(_data,typeof(T));
                    }
                    return _rc;
                } finally {
                    Marshal.FreeHGlobal(_data);
                }
            }

            public TwRC DsInvoke<T>(TwIdentity origin,TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,ref T data) where T:class {
                if(data==null) {
                    throw new ArgumentNullException();
                }
                IntPtr _data=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
                try {
                    Marshal.StructureToPtr(data,_data,true);

                    TwRC _rc=this.DsRaw(origin,dest,dg,dat,msg,_data);
                    if(_rc==TwRC.Success||_rc==TwRC.DSEvent||_rc==TwRC.XferDone) {
                        data=(T)Marshal.PtrToStructure(_data,typeof(T));
                    }
                    return _rc;
                } finally {
                    Marshal.FreeHGlobal(_data);
                }
            }

            #region Properties

            public _DSMparent DsmParent {
                get;
                private set;
            }

            public _DSMraw DsmRaw {
                get;
                private set;
            }

            public _DSixfer DSImageXfer {
                get;
                private set;
            }

            public _DSraw DsRaw {
                get;
                private set;
            }

            #endregion

            #region import libtwaindsm.so (Unix)

            [DllImport("/usr/local/lib/libtwaindsm.so",EntryPoint="DSM_Entry",CharSet=CharSet.Ansi)]
            private static extern TwRC _LinuxDsmParent([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr refptr);

            [DllImport("/usr/local/lib/libtwaindsm.so",EntryPoint="DSM_Entry",CharSet=CharSet.Ansi)]
            private static extern TwRC _LinuxDsmRaw([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,IntPtr rawData);

            [DllImport("/usr/local/lib/libtwaindsm.so",EntryPoint="DSM_Entry",CharSet=CharSet.Ansi)]
            private static extern TwRC _LinuxDsImageXfer([In,Out] TwIdentity origin,[In,Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr hbitmap);

            [DllImport("/usr/local/lib/libtwaindsm.so",EntryPoint="DSM_Entry",CharSet=CharSet.Ansi)]
            private static extern TwRC _LinuxDsRaw([In,Out] TwIdentity origin,[In,Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,IntPtr arg);

            #endregion

            #region import TWAIN.framework/TWAIN (MacOSX)

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN",EntryPoint = "DSM_Entry",CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsmParent([In, Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr refptr);

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN",EntryPoint = "DSM_Entry",CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsmRaw([In, Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,IntPtr rawData);

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN",EntryPoint = "DSM_Entry",CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsImageXfer([In, Out] TwIdentity origin,[In, Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr hbitmap);

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN",EntryPoint = "DSM_Entry",CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsRaw([In, Out] TwIdentity origin,[In, Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,IntPtr arg);

            #endregion

        }

        /// <summary>
        /// Entry points for memory management functions.
        /// </summary>
        internal sealed class _Memory {
            private static TwEntryPoint _entryPoint;

            /// <summary>
            /// Allocates a memory block of the specified size.
            /// </summary>
            /// <param name="size">The size of the memory block.</param>
            /// <returns>Memory descriptor.</returns>
            public static IntPtr Alloc(int size) {
                if(_Memory._entryPoint!=null&&_Memory._entryPoint.MemoryAllocate!=null) {
                    return _Memory._entryPoint.MemoryAllocate(size);
                }
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        return _Memory.GlobalAlloc(0x42,size);
                }
            }

            /// <summary>
            /// Frees up memory.
            /// </summary>
            /// <param name="handle">Memory descriptor.</param>
            public static void Free(IntPtr handle) {
                if(_Memory._entryPoint!=null&&_Memory._entryPoint.MemoryFree!=null) {
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
            /// </summary>
            /// <param name="handle">Memory descriptor.</param>
            /// <returns>Pointer to a block of memory.</returns>
            public static IntPtr Lock(IntPtr handle) {
                if(_Memory._entryPoint!=null&&_Memory._entryPoint.MemoryLock!=null) {
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
            /// </summary>
            /// <param name="handle">Memory descriptor.</param>
            public static void Unlock(IntPtr handle) {
                if(_Memory._entryPoint!=null&&_Memory._entryPoint.MemoryUnlock!=null) {
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

            public static void ZeroMemory(IntPtr dest,IntPtr size) {
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        byte[] _data= new byte[size.ToInt32()];
                        Marshal.Copy(_data,0,dest,_data.Length);
                        break;
                    default:
                        _Memory._ZeroMemory(dest,size);
                        break;
                }
            }

            /// <summary>
            /// Sets entry points.
            /// </summary>
            /// <param name="entry">Entry points.</param>
            internal static void _SetEntryPoints(TwEntryPoint entry) {
                _Memory._entryPoint=entry;
            }

            #region import kernel32.dll

            [DllImport("kernel32.dll",ExactSpelling=true)]
            private static extern IntPtr GlobalAlloc(int flags,int size);

            [DllImport("kernel32.dll",ExactSpelling=true)]
            private static extern IntPtr GlobalLock(IntPtr handle);

            [DllImport("kernel32.dll",ExactSpelling=true)]
            private static extern bool GlobalUnlock(IntPtr handle);

            [DllImport("kernel32.dll",ExactSpelling=true)]
            private static extern IntPtr GlobalFree(IntPtr handle);

            [DllImport("kernel32.dll",EntryPoint="RtlZeroMemory",SetLastError=false)]
            private static extern void _ZeroMemory(IntPtr dest,IntPtr size);


            #endregion
        }

        /// <summary>
        /// Entry points for platform features.
        /// </summary>
        internal sealed class _Platform {

            /// <summary>
            /// Loads the specified library into the process memory.
            /// </summary>
            /// <param name="fileName">The name of the library.</param>
            /// <returns>Module descriptor.</returns>
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
            /// </summary>
            /// <param name="hModule">Module descriptor</param>
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
            /// </summary>
            /// <param name="hModule">Module descriptor.</param>
            /// <param name="procName">The name of the procedure.</param>
            /// <returns>Pointer to a procedure.</returns>
            internal static IntPtr GetProcAddr(IntPtr hModule,string procName) {
                switch(Environment.OSVersion.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        return _Platform.GetProcAddress(hModule,procName);
                }
            }

            [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]
            private static extern IntPtr LoadLibrary(string fileName);

            [DllImport("kernel32.dll",ExactSpelling=true)]
            private static extern bool FreeLibrary(IntPtr hModule);

            [DllImport("kernel32.dll",CharSet=CharSet.Ansi,ExactSpelling=true)]
            private static extern IntPtr GetProcAddress(IntPtr hModule,string procName);
        }

        /// <summary>
        /// Win32 message filter.
        /// </summary>
        private sealed class _MessageFilter:IMessageFilter,IDisposable {
            private Twain32 _twain;
            private bool _is_set_filter=false;
            private TwEvent _evtmsg=new TwEvent();

            public _MessageFilter(Twain32 twain) {
                this._twain=twain;
                this._evtmsg.EventPtr=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINMSG)));
            }

            #region IMessageFilter

            public bool PreFilterMessage(ref Message m) {
                try {
                    if(this._twain._srcds.Id==0) {
                        return false;
                    }
                    Marshal.StructureToPtr(new WINMSG {hwnd=m.HWnd,message=m.Msg,wParam=m.WParam,lParam=m.LParam},this._evtmsg.EventPtr,true);
                    this._evtmsg.Message=TwMSG.Null;

                    switch(this._twain._dsmEntry.DsInvoke(this._twain._AppId,this._twain._srcds,TwDG.Control,TwDAT.Event,TwMSG.ProcessEvent,ref this._evtmsg)) {
                        case TwRC.DSEvent:
                            this._twain._TwCallbackProcCore(this._evtmsg.Message,isCloseReq => {
                                if(isCloseReq||this._twain.DisableAfterAcquire) {
                                    this._RemoveFilter();
                                    this._twain._DisableDataSource();
                                }
                            });
                            break;
                        case TwRC.NotDSEvent:
                            return false;
                        case TwRC.Failure:
                            throw new TwainException(this._twain._GetTwainStatus(),TwRC.Failure);
                        default:
                            throw new InvalidOperationException("Получен неверный код результата операции. Invalid a Return Code value.");
                    }
                } catch(TwainException ex) {
                    this._twain._OnAcquireError(new AcquireErrorEventArgs(ex));
                } catch(Exception ex) {
                    this._twain._OnAcquireError(new AcquireErrorEventArgs(new TwainException(ex.Message,ex)));
                }
                return true;
            }

            #endregion

            #region IDisposable

            public void Dispose() {
                if(this._evtmsg!=null&&this._evtmsg.EventPtr!=IntPtr.Zero) {
                    Marshal.FreeHGlobal(this._evtmsg.EventPtr);
                    this._evtmsg.EventPtr=IntPtr.Zero;
                }
            }

            #endregion

            public void SetFilter() {
                if(!this._is_set_filter) {
                    this._is_set_filter=true;
                    Application.AddMessageFilter(this);
                }
            }

            private void _RemoveFilter() {
                Application.RemoveMessageFilter(this);
                this._is_set_filter=false;
            }

            [StructLayout(LayoutKind.Sequential,Pack=2)]
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

            public static implicit operator _Image(Stream stream) {
                return new _Image { _stream=stream };
            }

            public static implicit operator Stream(_Image image) {
                image._stream.Seek(0L,SeekOrigin.Begin);
                return image._stream;
            }

            public static implicit operator Image(_Image value) {
                if(value._image==null) {
                    value._stream.Seek(0L,SeekOrigin.Begin);
                    value._image=Image.FromStream(value._stream);
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
                this.MinValue=TwTypeHelper.CastToCommon(range.ItemType,TwTypeHelper.ValueToTw<uint>(range.ItemType,range.MinValue));
                this.MaxValue=TwTypeHelper.CastToCommon(range.ItemType,TwTypeHelper.ValueToTw<uint>(range.ItemType,range.MaxValue));
                this.StepSize=TwTypeHelper.CastToCommon(range.ItemType,TwTypeHelper.ValueToTw<uint>(range.ItemType,range.StepSize));
                this.CurrentValue=TwTypeHelper.CastToCommon(range.ItemType,TwTypeHelper.ValueToTw<uint>(range.ItemType,range.CurrentValue));
                this.DefaultValue=TwTypeHelper.CastToCommon(range.ItemType,TwTypeHelper.ValueToTw<uint>(range.ItemType,range.DefaultValue));
            }

            /// <summary>
            /// Creates and returns an instance <see cref="Range"/>.
            /// </summary>
            /// <param name="range">Instance <see cref="TwRange"/>.</param>
            /// <returns>Instance <see cref="Range"/>.</returns>
            internal static Range CreateRange(TwRange range) {
                return new Range(range);
            }

            /// <summary>
            /// Creates and returns an instance <see cref="Range"/>.
            /// </summary>
            /// <param name="minValue">Minimum value.</param>
            /// <param name="maxValue">The maximum value.</param>
            /// <param name="stepSize">Step.</param>
            /// <param name="defaultValue">The default value.</param>
            /// <param name="currentValue">Present value.</param>
            /// <returns>Instance <see cref="Range"/>.</returns>
            public static Range CreateRange(object minValue,object maxValue,object stepSize,object defaultValue,object currentValue) {
                return new Range() {
                    MinValue=minValue, MaxValue=maxValue, StepSize=stepSize, DefaultValue=defaultValue, CurrentValue=currentValue
                };
            }

            /// <summary>
            /// Gets or sets the minimum value.
            /// </summary>
            public object MinValue {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the maximum value.
            /// </summary>
            public object MaxValue {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the step.
            /// </summary>
            public object StepSize {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the default value.
            /// </summary>
            public object DefaultValue {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the current value.
            /// </summary>
            public object CurrentValue {
                get;
                set;
            }

            /// <summary>
            /// Converts an instance of a class to an instance <see cref="TwRange"/>.
            /// </summary>
            /// <returns>Instance <see cref="TwRange"/>.</returns>
            internal TwRange ToTwRange() {
                TwType _type=TwTypeHelper.TypeOf(this.CurrentValue.GetType());
                return new TwRange() {
                    ItemType=_type,
                    MinValue=TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type,this.MinValue)),
                    MaxValue=TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type,this.MaxValue)),
                    StepSize=TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type,this.StepSize)),
                    DefaultValue=TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type,this.DefaultValue)),
                    CurrentValue=TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type,this.CurrentValue))
                };
            }
        }

        /// <summary>
        /// Enumeration.
        /// </summary>
        [Serializable]
        public sealed class Enumeration {
            private object[] _items;

            /// <summary>
            /// Prevents a default instance of the <see cref="Enumeration"/> class from being created.
            /// </summary>
            /// <param name="items">Listing items.</param>
            /// <param name="currentIndex">Current index.</param>
            /// <param name="defaultIndex">The default index.</param>
            private Enumeration(object[] items,int currentIndex,int defaultIndex) {
                this._items=items;
                this.CurrentIndex=currentIndex;
                this.DefaultIndex=defaultIndex;
            }

            /// <summary>
            /// Creates and returns an instance <see cref="Enumeration"/>.
            /// </summary>
            /// <param name="items">Listing items.</param>
            /// <param name="currentIndex">Current index.</param>
            /// <param name="defaultIndex">The default index.</param>
            /// <returns>Instance <see cref="Enumeration"/>.</returns>
            public static Enumeration CreateEnumeration(object[] items,int currentIndex,int defaultIndex) {
                return new Enumeration(items,currentIndex,defaultIndex);
            }

            /// <summary>
            /// Returns the number of items.
            /// </summary>
            public int Count {
                get {
                    return this._items.Length;
                }
            }

            /// <summary>
            /// Returns the current index.
            /// </summary>
            public int CurrentIndex {
                get;
                private set;
            }

            /// <summary>
            /// Returns the default index.
            /// </summary>
            public int DefaultIndex {
                get;
                private set;
            }

            /// <summary>
            /// Returns the element at the specified index.
            /// </summary>
            /// <param name="index">Index.</param>
            /// <returns>The item at the specified index.</returns>
            public object this[int index] {
                get {
                    return this._items[index];
                }
                internal set {
                    this._items[index]=value;
                }
            }

            internal object[] Items {
                get {
                    return this._items;
                }
            }

            /// <summary>
            /// Creates and returns an instance <see cref="Enumeration"/>.
            /// </summary>
            /// <param name="value">Instance <see cref="Range"/>.</param>
            /// <returns>Instance <see cref="Enumeration"/>.</returns>
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
            /// </summary>
            /// <param name="value">An array of values.</param>
            /// <returns>Instance <see cref="Enumeration"/>.</returns>
            public static Enumeration FromArray(object[] value) {
                return Enumeration.CreateEnumeration(value, 0, 0);
            }

            /// <summary>
            /// Creates and returns an instance <see cref="Enumeration"/>.
            /// </summary>
            /// <param name="value">Value.</param>
            /// <returns>Instance <see cref="Enumeration"/>.</returns>
            public static Enumeration FromOneValue(ValueType value) {
                return Enumeration.CreateEnumeration(new object[] { value }, 0, 0);
            }

            internal static Enumeration FromObject(object value) {
                if(value is Range) {
                    return Enumeration.FromRange((Range)value);
                }
                if(value is object[]) {
                    return Enumeration.FromArray((object[])value);
                }
                if(value is ValueType) {
                    return Enumeration.FromOneValue((ValueType)value);
                }
                if(value is string) {
                    return Enumeration.CreateEnumeration(new object[] { value },0,0);
                }
                return value as Enumeration;
            }
        }

        /// <summary>
        /// Description of the image.
        /// </summary>
        [Serializable]
        public sealed class ImageInfo {

            private ImageInfo() {
            }

            /// <summary>
            /// Creates and returns a new instance of the ImageInfo class based on an instance of the TwImageInfo class.
            /// </summary>
            /// <param name="info">Description of the image.</param>
            /// <returns>An instance of the ImageInfo class.</returns>
            internal static ImageInfo FromTwImageInfo(TwImageInfo info) {

                return new ImageInfo {
                    BitsPerPixel=info.BitsPerPixel,
                    BitsPerSample=ImageInfo._Copy(info.BitsPerSample,info.SamplesPerPixel),
                    Compression=info.Compression,
                    ImageLength=info.ImageLength,
                    ImageWidth=info.ImageWidth,
                    PixelType=info.PixelType,
                    Planar=info.Planar,
                    XResolution=info.XResolution,
                    YResolution=info.YResolution
                };
            }

            private static short[] _Copy(short[] array,int len) {
                var _result = new short[len];
                for(var i = 0; i<len; i++) {
                    _result[i]=array[i];
                }
                return _result;
            }

            /// <summary>
            /// Resolution in the horizontal
            /// </summary>
            public float XResolution {
                get;
                private set;
            }

            /// <summary>
            /// Resolution in the vertical
            /// </summary>
            public float YResolution {
                get;
                private set;
            }

            /// <summary>
            /// Columns in the image, -1 if unknown by DS
            /// </summary>
            public int ImageWidth {
                get;
                private set;
            }

            /// <summary>
            /// Rows in the image, -1 if unknown by DS
            /// </summary>
            public int ImageLength {
                get;
                private set;
            }

            /// <summary>
            /// Number of bits for each sample
            /// </summary>
            public short[] BitsPerSample {
                get;
                private set;
            }

            /// <summary>
            /// Number of bits for each padded pixel
            /// </summary>
            public short BitsPerPixel {
                get;
                private set;
            }

            /// <summary>
            /// True if Planar, False if chunky
            /// </summary>
            public bool Planar {
                get;
                private set;
            }

            /// <summary>
            /// How to interp data; photo interp
            /// </summary>
            public TwPixelType PixelType {
                get;
                private set;
            }

            /// <summary>
            /// How the data is compressed
            /// </summary>
            public TwCompression Compression {
                get;
                private set;
            }
        }

        /// <summary>
        /// Extended image description.
        /// </summary>
        [Serializable]
        public sealed class ExtImageInfo:Collection<ExtImageInfo.InfoItem> {

            private ExtImageInfo() {
            }

            /// <summary>
            /// Creates and returns an instance of the ExtImageInfo class from an unmanaged memory block.
            /// </summary>
            /// <param name="ptr">Pointer to an unmanaged memory block.</param>
            /// <returns>An instance of the ExtImageInfo class.</returns>
            internal static ExtImageInfo FromPtr(IntPtr ptr) {
                int _twExtImageInfoSize=Marshal.SizeOf(typeof(TwExtImageInfo));
                int _twInfoSize=Marshal.SizeOf(typeof(TwInfo));
                TwExtImageInfo _extImageInfo=Marshal.PtrToStructure(ptr,typeof(TwExtImageInfo)) as TwExtImageInfo;
                ExtImageInfo _result=new ExtImageInfo();
                for(int i=0; i<_extImageInfo.NumInfos; i++) {
                    using(TwInfo _item=Marshal.PtrToStructure((IntPtr)(ptr.ToInt64()+_twExtImageInfoSize+(_twInfoSize*i)),typeof(TwInfo)) as TwInfo) {
                        _result.Add(InfoItem.FromTwInfo(_item));
                    }
                }
                return _result;
            }

            /// <summary>
            /// Returns a description element of the extended image information by its code.
            /// </summary>
            /// <param name="infoId">Description element code for extended image information.</param>
            /// <returns>Description element for extended image information.</returns>
            /// <exception cref="System.Collections.Generic.KeyNotFoundException">There is no corresponding element for the specified code.</exception>
            public InfoItem this[TwEI infoId] {
                get {
                    foreach(InfoItem _item in this) {
                        if(_item.InfoId==infoId) {
                            return _item;
                        }
                    }
                    throw new KeyNotFoundException();
                }
            }

            /// <summary>
            /// Description element for extended image information.
            /// </summary>
            [Serializable]
            [DebuggerDisplay("InfoId = {InfoId}, IsSuccess = {IsSuccess}, Value = {Value}")]
            public sealed class InfoItem {

                private InfoItem() {
                }

                /// <summary>
                /// Creates and returns an instance class of an extended image information description element from an internal instance of an extended image information description element class.
                /// </summary>
                /// <param name="info">An internal instance of the extended image information description element class.</param>
                /// <returns>An instance of the extended image information description item class.</returns>
                internal static InfoItem FromTwInfo(TwInfo info) {
                    return new InfoItem {
                        InfoId=info.InfoId,
                        IsNotSupported=info.ReturnCode==TwRC.InfoNotSupported,
                        IsNotAvailable=info.ReturnCode==TwRC.DataNotAvailable,
                        IsSuccess=info.ReturnCode==TwRC.Success,
                        Value=info.GetValue()
                    };
                }

                /// <summary>
                /// Returns a code for extended image information.
                /// </summary>
                public TwEI InfoId {
                    get;
                    private set;
                }

                /// <summary>
                /// Вreturns true if the requested information is not supported by the data source; otherwise false.
                /// </summary>
                public bool IsNotSupported {
                    get;
                    private set;
                }

                /// <summary>
                /// Returns true if the requested information is supported by the data source but is currently unavailable; otherwise false.
                /// </summary>
                public bool IsNotAvailable {
                    get;
                    private set;
                }

                /// <summary>
                /// Returns true if the requested information was successfully retrieved; otherwise false.
                /// </summary>
                public bool IsSuccess {
                    get;
                    private set;
                }

                /// <summary>
                /// Returns the value of an element.
                /// </summary>
                public object Value {
                    get;
                    private set;
                }
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
                ImageMemXfer _res=new ImageMemXfer() {
                    BytesPerRow=data.BytesPerRow,
                    Columns=data.Columns,
                    Compression=data.Compression,
                    Rows=data.Rows,
                    XOffset=data.XOffset,
                    YOffset=data.YOffset
                };
                if((data.Memory.Flags&TwMF.Handle)!=0) {
                    IntPtr _data=Twain32._Memory.Lock(data.Memory.TheMem);
                    try {
                        _res.ImageData=new byte[data.BytesWritten];
                        Marshal.Copy(_data,_res.ImageData,0,_res.ImageData.Length);
                    } finally {
                        Twain32._Memory.Unlock(data.Memory.TheMem);
                    }
                } else {
                    _res.ImageData=new byte[data.BytesWritten];
                    Marshal.Copy(data.Memory.TheMem,_res.ImageData,0,_res.ImageData.Length);
                }
                return _res;
            }

            /// <summary>
            /// How the data is compressed.
            /// </summary>
            public TwCompression Compression {
                get;
                private set;
            }

            /// <summary>
            /// Number of bytes in a row of data.
            /// </summary>
            public uint BytesPerRow {
                get;
                private set;
            }

            /// <summary>
            /// How many columns.
            /// </summary>
            public uint Columns {
                get;
                private set;
            }

            /// <summary>
            /// How many rows.
            /// </summary>
            public uint Rows {
                get;
                private set;
            }

            /// <summary>
            /// How far from the side of the image.
            /// </summary>
            public uint XOffset {
                get;
                private set;
            }

            /// <summary>
            /// How far from the top of the image.
            /// </summary>
            public uint YOffset {
                get;
                private set;
            }

            /// <summary>
            /// Data.
            /// </summary>
            public byte[] ImageData {
                get;
                private set;
            }
        }

        /// <summary>
        /// Description of the image file.
        /// </summary>
        [Serializable]
        public sealed class ImageFileXfer {

            /// <summary>
            /// Initializes a new instance <see cref="ImageFileXfer"/>.
            /// </summary>
            private ImageFileXfer() {
            }

            /// <summary>
            /// Creates and returns a new instance <see cref="ImageFileXfer"/>.
            /// </summary>
            /// <param name="data">File description.</param>
            /// <returns>Instance <see cref="ImageFileXfer"/>.</returns>
            internal static ImageFileXfer Create(TwSetupFileXfer data) {
                return new ImageFileXfer {
                    FileName=data.FileName,
                    Format=data.Format
                };
            }

            /// <summary>
            /// Returns the file name.
            /// </summary>
            public string FileName {
                get;
                private set;
            }

            /// <summary>
            /// Returns the file format.
            /// </summary>
            public TwFF Format {
                get;
                private set;
            }
        }

        /// <summary>
        /// A set of operations for working with a color palette.
        /// </summary>
        public sealed class TwainPalette:MarshalByRefObject {
            private Twain32 _twain;

            /// <summary>
            /// Initializes a new instance of the class <see cref="TwainPalette"/>.
            /// </summary>
            /// <param name="twain">Class instance <see cref="TwainPalette"/>.</param>
            internal TwainPalette(Twain32 twain) {
                this._twain=twain;
            }

            /// <summary>
            /// Returns the current color palette.
            /// </summary>
            /// <returns>Class instance <see cref="TwainPalette"/>.</returns>
            public ColorPalette Get() {
                TwPalette8 _palette=new TwPalette8();
                TwRC _rc=this._twain._dsmEntry.DsInvoke(this._twain._AppId,this._twain._srcds,TwDG.Image,TwDAT.Palette8,TwMSG.Get,ref _palette);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(),_rc);
                }
                return _palette;
            }

            /// <summary>
            /// Returns the current default color palette.
            /// </summary>
            /// <returns>Class instance <see cref="TwainPalette"/>.</returns>
            public ColorPalette GetDefault() {
                TwPalette8 _palette=new TwPalette8();
                TwRC _rc=this._twain._dsmEntry.DsInvoke(this._twain._AppId,this._twain._srcds,TwDG.Image,TwDAT.Palette8,TwMSG.GetDefault,ref _palette);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(),_rc);
                }
                return _palette;
            }

            /// <summary>
            /// Resets the current color palette and sets the specified one.
            /// </summary>
            /// <param name="palette">Class instance <see cref="TwainPalette"/>.</param>
            public void Reset(ColorPalette palette) {
                TwRC _rc=this._twain._dsmEntry.DsInvoke(this._twain._AppId,this._twain._srcds,TwDG.Image,TwDAT.Palette8,TwMSG.Reset,ref palette);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(),_rc);
                }
            }

            /// <summary>
            /// Sets the specified color palette.
            /// </summary>
            /// <param name="palette">Class instance <see cref="TwainPalette"/>.</param>
            public void Set(ColorPalette palette) {
                TwRC _rc=this._twain._dsmEntry.DsInvoke(this._twain._AppId,this._twain._srcds,TwDG.Image,TwDAT.Palette8,TwMSG.Set,ref palette);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(),_rc);
                }
            }
        }

        /// <summary>
        /// Color palette.
        /// </summary>
        [Serializable]
        public sealed class ColorPalette {

            /// <summary>
            /// Initializes a new instance <see cref="ColorPalette"/>.
            /// </summary>
            private ColorPalette() {
            }

            /// <summary>
            /// Creates and returns a new instance <see cref="ColorPalette"/>.
            /// </summary>
            /// <param name="palette">Color palette.</param>
            /// <returns>Instance <see cref="ColorPalette"/>.</returns>
            internal static ColorPalette Create(TwPalette8 palette) {
                Twain32.ColorPalette _result=new Twain32.ColorPalette {
                    PaletteType=palette.PaletteType,
                    Colors=new Color[palette.NumColors]
                };
                for(int i=0; i<palette.NumColors; i++) {
                    _result.Colors[i]=palette.Colors[i];
                }
                return _result;
            }

            /// <summary>
            /// Returns the type of palette.
            /// </summary>
            public TwPA PaletteType {
                get;
                private set;
            }

            /// <summary>
            /// Returns the colors that make up the palette.
            /// </summary>
            public Color[] Colors {
                get;
                private set;
            }
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
                this.Family=identity.ProductFamily;
                this.Manufacturer=identity.Manufacturer;
                this.Name=identity.ProductName;
                this.ProtocolVersion=new Version(identity.ProtocolMajor,identity.ProtocolMinor);
                this.Version=new Version(identity.Version.MajorNum,identity.Version.MinorNum);
            }

            /// <summary>
            /// Get the version of the software.
            /// </summary>
            /// <value>
            /// The version.
            /// </value>
            public Version Version {
                get;
                private set;
            }

            /// <summary>
            /// Get the protocol version.
            /// </summary>
            /// <value>
            /// The protocol version.
            /// </value>
            public Version ProtocolVersion {
                get;
                private set;
            }

            /// <summary>
            /// Get manufacturer name, e.g. "Hewlett-Packard".
            /// </summary>
            public string Manufacturer {
                get;
                private set;
            }

            /// <summary>
            /// Get product family name, e.g. "ScanJet".
            /// </summary>
            public string Family {
                get;
                private set;
            }

            /// <summary>
            /// Get product name, e.g. "ScanJet Plus".
            /// </summary>
            public string Name {
                get;
                private set;
            }
        }

        #endregion

        #region Delegates

        #region DSM delegates DAT_ variants

        private delegate TwRC _DSMparent([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr refptr);

        private delegate TwRC _DSMraw([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,IntPtr rawData);

        #endregion

        #region DS delegates DAT_ variants to DS

        private delegate TwRC _DSixfer([In,Out] TwIdentity origin,[In,Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr hbitmap);

        private delegate TwRC _DSraw([In,Out] TwIdentity origin,[In,Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,IntPtr arg);

        #endregion

        internal delegate ImageInfo GetImageInfoCallback();

        internal delegate ExtImageInfo GetExtImageInfoCallback(TwEI[] extInfo);

        private delegate void Action<T>(T arg);

        #endregion
    }
}
