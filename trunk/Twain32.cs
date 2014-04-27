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
    /// Обеспечивает возможность работы с TWAIN-источниками.
    /// </summary>
    [ToolboxBitmap(typeof(Twain32),"Resources.scanner.bmp")]
    [DebuggerDisplay("ProductName = {_appid.ProductName.Value}, Version = {_appid.Version.Info}, DS = {_srcds.ProductName}")]
    [DefaultEvent("AcquireCompleted")]
    [DefaultProperty("AppProductName")]
    public sealed class Twain32:Component {
        private _DsmEntry _dsmEntry;
        private IntPtr _hTwainDll; //дескриптор модуля twain_32.dll
        private IContainer _components=new Container();
        private IntPtr _hwnd; //дескриптор родительского окна.
        private TwIdentity _appid; //идентификатор приложения.
        private TwIdentity _srcds; //идентификатор текущего источника данных.
        private _MessageFilter _filter; //фильтр событий WIN32
        private TwIdentity[] _sources; //массив доступных источников данных.
        private ApplicationContext _context=null; //контекст приложения. используется в случае отсутствия основного цикла обработки сообщений.
        private Collection<Image> _images=new Collection<Image>();
        private TwainStateFlag _twainState;
        private bool _isTwain2Enable=IntPtr.Size!=4;

        /// <summary>
        /// Initializes a new instance of the <see cref="Twain32"/> class.
        /// </summary>
        public Twain32() {
            Form _window=new Form();
            this._components.Add(_window);
            this._hwnd=_window.Handle;

            Assembly _asm=Assembly.GetExecutingAssembly();
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
            this._srcds=new TwIdentity();
            this._srcds.Id=0;
            this._filter=new _MessageFilter(this);
            this.ShowUI=true;
            this.DisableAfterAcquire=true;
            this.Capabilities=new TwainCapabilities(this);
            this.Palette=new TwainPalette(this);
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
                this._filter.Dispose();
                if(this._hTwainDll!=IntPtr.Zero) {
                    FreeLibrary(this._hTwainDll);
                    this._hTwainDll=IntPtr.Zero;
                }
                if(this._components!=null) {
                    this._components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Открывает менеджер источников данных.
        /// </summary>
        /// <returns>Истина, если операция прошла удачно; иначе, лож.</returns>
        public bool OpenDSM() {
            if((this._TwainState&TwainStateFlag.DSMOpen)==0) {

                #region Загружаем DSM, получаем адрес точки входа DSM_Entry и приводим ее к соответствующим делегатам

                this._hTwainDll=LoadLibrary(string.Format("{0}\\{1}.dll",Environment.SystemDirectory,IntPtr.Size==4&&!this.IsTwain2Enable?"..\\twain_32":"TWAINDSM"));
                if(this._hTwainDll!=IntPtr.Zero) {
                    IntPtr _pDsmEntry=GetProcAddress(this._hTwainDll,1);
                    if(_pDsmEntry!=IntPtr.Zero) {
                        this._dsmEntry=_DsmEntry.Create(_pDsmEntry);
                        _Memory._SetEntryPoints(null);
                    } else {
                        throw new TwainException("Cann't find DSM_Entry entry point.");
                    }
                } else {
                    throw new TwainException("Cann't load DSM.");
                }

                #endregion

                if(this.Parent!=null) {
                    this._hwnd=this.Parent.Handle;
                }
                TwRC _rc=this._dsmEntry.DsmParent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Parent,TwMSG.OpenDSM,ref this._hwnd);
                if(_rc==TwRC.Success) {
                    this._TwainState|=TwainStateFlag.DSMOpen;

                    if(this.IsTwain2Supported) {
                        TwEntryPoint _entry=new TwEntryPoint();
                        if(this._dsmEntry.DsmEntryPoint(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.EntryPoint,TwMSG.Get,_entry)==TwRC.Success) {
                            _Memory._SetEntryPoints(_entry);
                        }
                    }

                    this._GetAllSorces();
                }
            }
            return (this._TwainState&TwainStateFlag.DSMOpen)!=0;
        }

        /// <summary>
        /// Отображает диалоговое окно для выбора источника данных.
        /// </summary>
        /// <returns>Истина, если операция прошла удачно; иначе, лож.</returns>
        public bool SelectSource() {
            if((this._TwainState&TwainStateFlag.DSOpen)==0) {
                if((this._TwainState&TwainStateFlag.DSMOpen)==0) {
                    this.OpenDSM();
                    if((this._TwainState&TwainStateFlag.DSMOpen)==0) {
                        return false;
                    }
                }
                TwIdentity _src=new TwIdentity();
                TwRC _rc=this._dsmEntry.DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.UserSelect,_src);
                if(_rc==TwRC.Success) {
                    this._srcds=_src;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Открывает источник данных.
        /// </summary>
        /// <returns>Истина, если операция прошла удачно; иначе, лож.</returns>
        public bool OpenDataSource() {
            if((this._TwainState&TwainStateFlag.DSMOpen)!=0 && (this._TwainState&TwainStateFlag.DSOpen)==0) {
                TwRC _rc=this._dsmEntry.DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.OpenDS,this._srcds);
                if(_rc==TwRC.Success) {
                    this._TwainState|=TwainStateFlag.DSOpen;
                }
            }
            return (this._TwainState&TwainStateFlag.DSOpen)!=0;
        }

        /// <summary>
        /// Активирует источник данных.
        /// </summary>
        /// <returns>Истина, если операция прошла удачно; иначе, лож.</returns>
        private bool _EnableDataSource() {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0 && (this._TwainState&TwainStateFlag.DSEnabled)==0) {
                TwUserInterface _guif=new TwUserInterface() {
                    ShowUI=this.ShowUI,
                    ModalUI=this.ModalUI,
                    ParentHand=this._hwnd
                };
                TwRC _rc=this._dsmEntry.DSUI(this._appid,this._srcds,TwDG.Control,TwDAT.UserInterface,TwMSG.EnableDS,_guif);
                if(_rc==TwRC.Success) {
                    this._TwainState|=TwainStateFlag.DSEnabled;
                }
            }
            return (this._TwainState&TwainStateFlag.DSEnabled)!=0;
        }

        /// <summary>
        /// Получает изображение с источника данных.
        /// </summary>
        public void Acquire() {
            this._filter.SetFilter();

            if(this.OpenDSM()) {
                if(this.OpenDataSource()) {
                    this._EnableDataSource();
                    if(!Application.MessageLoop) {
                        Application.Run(this._context=new ApplicationContext());
                    }
                }
            }
        }

        /// <summary>
        /// Деактивирует источник данных.
        /// </summary>
        /// <returns>Истина, если операция прошла удачно; иначе, лож.</returns>
        private bool _DisableDataSource() {
            TwRC _rc=TwRC.Failure;
            if((this._TwainState&TwainStateFlag.DSEnabled)!=0) {
                TwUserInterface _guif=new TwUserInterface() {
                    ParentHand=this._hwnd,
                    ShowUI=false
                };
                _rc=this._dsmEntry.DSUI(this._appid,this._srcds,TwDG.Control,TwDAT.UserInterface,TwMSG.DisableDS,_guif);
                if(_rc==TwRC.Success) {
                    this._TwainState&=~TwainStateFlag.DSEnabled;
                    if(this._context!=null) {
                        this._context.ExitThread();
                        this._context.Dispose();
                        this._context=null;
                    }
                }
            }
            return _rc==TwRC.Success;
        }

        /// <summary>
        /// Закрывает источник данных.
        /// </summary>
        /// <returns>Истина, если операция прошла удачно; иначе, лож.</returns>
        public bool CloseDataSource() {
            TwRC _rc=TwRC.Failure;
            if((this._TwainState&TwainStateFlag.DSOpen)!=0 && (this._TwainState&TwainStateFlag.DSEnabled)==0) {
                _rc=this._dsmEntry.DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.CloseDS,this._srcds);
                if(_rc==TwRC.Success) {
                    this._TwainState&=~TwainStateFlag.DSOpen;
                }
            }
            return _rc==TwRC.Success;
        }

        /// <summary>
        /// Закрывает менежер источников данных.
        /// </summary>
        /// <returns>Истина, если операция прошла удачно; иначе, лож.</returns>
        public bool CloseDSM() {
            TwRC _rc=TwRC.Failure;
            if((this._TwainState&TwainStateFlag.DSEnabled)!=0) {
                this._DisableDataSource();
            }
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                this.CloseDataSource();
            }
            if((this._TwainState&TwainStateFlag.DSMOpen)!=0&&(this._TwainState&TwainStateFlag.DSOpen)==0) {
                _rc=this._dsmEntry.DsmParent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Parent,TwMSG.CloseDSM,ref this._hwnd);
                if(_rc==TwRC.Success) {
                    this._TwainState&=~TwainStateFlag.DSMOpen;
                }
            }
            return _rc==TwRC.Success;
        }

        /// <summary>
        /// Возвращает отсканированое изображение.
        /// </summary>
        /// <param name="index">Индекс изображения.</param>
        /// <returns>Экземпляр изображения.</returns>
        public Image GetImage(int index) {
            return this._images[index];
        }

        /// <summary>
        /// Возвращает количество отсканированных изображений.
        /// </summary>
        [Browsable(false)]
        public int ImageCount {
            get {
                return this._images.Count;
            }
        }

        /// <summary>
        /// Возвращает или устанавливает значение, указывающее на необходимость деактивации источника данных после получения изображения.
        /// </summary>
        [DefaultValue(true)]
        [Category("Behavior")]
        [Description("Возвращает или устанавливает значение, указывающее на необходимость деактивации источника данных после получения изображения.")]
        public bool DisableAfterAcquire {
            get;
            set;
        }

        /// <summary>
        /// Возвращает или устанавливает значение, указывающее на необходимость использования TWAIN 2.0.
        /// </summary>
        [DefaultValue(false)]
        [Category("Behavior")]
        [Description("Возвращает или устанавливает значение, указывающее на необходимость использования TWAIN 2.0.")]
        public bool IsTwain2Enable {
            get {
                return this._isTwain2Enable;
            }
            set {
                if((this._TwainState&TwainStateFlag.DSMOpen)!=0) {
                    throw new InvalidOperationException("DSM already opened.");
                }
                if(IntPtr.Size!=4&&!value) {
                    throw new InvalidOperationException("In x64 mode only TWAIN 2.0 enabled.");
                }
                if(this._isTwain2Enable=value) {
                    this._appid.SupportedGroups|=TwDG.APP2;
                } else {
                    this._appid.SupportedGroups&=~TwDG.APP2;
                }
                this._appid.ProtocolMajor=(ushort)(this._isTwain2Enable?2:1);
                this._appid.ProtocolMinor=(ushort)(this._isTwain2Enable?3:9);
            }
        }

        /// <summary>
        /// Возвращает истину, если DSM поддерживает TWAIN 2.0; иначе лож.
        /// </summary>
        [Browsable(false)]
        public bool IsTwain2Supported {
            get {
                if((this._TwainState&TwainStateFlag.DSMOpen)==0) {
                    throw new InvalidOperationException("DSM is not open.");
                }
                return (this._appid.SupportedGroups&TwDG.DSM2)!=0;
            }
        }

        #region Information of sorces

        /// <summary>
        /// Возвращает или устанавливает индекс текущего источника данных.
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
                        throw new TwainException("Источник данных уже открыт.");
                    }
                } else {
                    throw new TwainException("Менеджер источников данных не открыт.");
                }
            }
        }

        /// <summary>
        /// Возвращает количество источников данных.
        /// </summary>
        [Browsable(false)]
        public int SourcesCount {
            get {
                return this._sources.Length;
            }
        }

        /// <summary>
        /// Возвращает имя источника данных по указанному индексу.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <returns>Имя источника данных.</returns>
        public string GetSourceProductName(int index) {
            return this._sources[index].ProductName;
        }

        /// <summary>
        /// Возвращает истину, если указанный источник поддерживает TWAIN 2.0; иначе лож.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <returns>Истина, если указанный источник поддерживает TWAIN 2.0; иначе лож.</returns>
        public bool GetIsSourceTwain2Compatible(int index) {
            return (this._sources[index].SupportedGroups&TwDG.DS2)!=0;
        }

        /// <summary>
        /// Устанавливает указанный источник данных в качестве источника данных по умолчанию.
        /// </summary>
        /// <param name="index">Индекс.</param>
        public void SetDefaultSource(int index) {
            if((this._TwainState&TwainStateFlag.DSMOpen)!=0) {
                if((this._TwainState&TwainStateFlag.DSOpen)==0) {
                    TwIdentity _src=this._sources[index];
                    TwRC _rc=this._dsmEntry.DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.Set,_src);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                } else {
                    throw new TwainException("Источник данных уже открыт. Необходимо сперва закрыть источник данных.");
                }
            } else {
                throw new TwainException("DSM не открыт.");
            }
        }

        #endregion

        #region Properties of source

        /// <summary>
        /// Возвращает или устанавливает имя приложения.
        /// </summary>
        [Category("Behavior")]
        [Description("Возвращает или устанавливает имя приложения.")]
        public string AppProductName {
            get {
                return this._appid.ProductName;
            }
            set {
                this._appid.ProductName=value;
            }
        }

        /// <summary>
        /// Возвращает или устанавливает значение указывающие на необходимость отображения UI TWAIN-источника.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(true)]
        [Description("Возвращает или устанавливает значение указывающие на необходимость отображения UI TWAIN-источника.")]
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
        /// Возвращает или устанавливает родительское окно для TWAIN-источника.
        /// </summary>
        /// <value>
        /// Окно.
        /// </value>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Возвращает или устанавливает родительское окно для TWAIN-источника.")]
        public IWin32Window Parent {
            get;
            set;
        }

        /// <summary>
        /// Возвращает или устанавливает кадр физического расположения изображения.
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public RectangleF ImageLayout {
            get {
                TwImageLayout _imageLayout=new TwImageLayout();
                TwRC _rc=this._dsmEntry.DSImageLayout(this._appid,this._srcds,TwDG.Image,TwDAT.ImageLayout,TwMSG.Get,_imageLayout);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                return _imageLayout.Frame;
            }
            set {
                TwImageLayout _imageLayout=new TwImageLayout{Frame=value};
                TwRC _rc=this._dsmEntry.DSImageLayout(this._appid,this._srcds,TwDG.Image,TwDAT.ImageLayout,TwMSG.Set,_imageLayout);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
            }
        }

        /// <summary>
        /// Возвращает набор возможностей (Capabilities).
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public TwainCapabilities Capabilities {
            get;
            private set;
        }

        /// <summary>
        /// Возвращает набор операций для работы с цветовой палитрой.
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public TwainPalette Palette {
            get;
            private set;
        }

        /// <summary>
        /// Возвращает разрешения, поддерживаемые источником данных.
        /// </summary>
        /// <returns>Коллекция значений.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        [Obsolete("Use Twain32.Capabilities.XResolution.Get() instead.")]
        public Enumeration GetResolutions() {
            return Enumeration.FromObject(this.GetCap(TwCap.XResolution));
        }

        /// <summary>
        /// Устанавливает текущее разрешение.
        /// </summary>
        /// <param name="value">Разрешение.</param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        [Obsolete("Use Twain32.Capabilities.XResolution.Set(value) and Twain32.Capabilities.YResolution.Set(value) instead.")]
        public void SetResolutions(float value) {
            this.SetCap(TwCap.XResolution,value);
            this.SetCap(TwCap.YResolution,value);
        }

        /// <summary>
        /// Возвращает типы пикселей, поддерживаемые источником данных.
        /// </summary>
        /// <returns>Коллекция значений.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        [Obsolete("Use Twain32.Capabilities.PixelType.Get() instead.")]
        public Enumeration GetPixelTypes() {
            Enumeration _val=Enumeration.FromObject(this.GetCap(TwCap.IPixelType));
            for(int i=0;i<_val.Count;i++) {
                _val[i]=(TwPixelType)_val[i];
            }
            return _val;
        }

        /// <summary>
        /// Устанавливает текущий тип пикселей.
        /// </summary>
        /// <param name="value">Тип пикселей.</param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        [Obsolete("Use Twain32.Capabilities.PixelType.Set(value) instead.")]
        public void SetPixelType(TwPixelType value) {
            this.SetCap(TwCap.IPixelType,value);
        }

        /// <summary>
        /// Возвращает единицы измерения, используемые источником данных.
        /// </summary>
        /// <returns>Единицы измерения.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        [Obsolete("Use Twain32.Capabilities.Units.Get() instead.")]
        public Enumeration GetUnitOfMeasure() {
            Enumeration _val=Enumeration.FromObject(this.GetCap(TwCap.IUnits));
            for (int i=0; i<_val.Count; i++) {
                _val[i]=(TwUnits)_val[i];
            }
            return _val;
        }

        /// <summary>
        /// Устанавливает текущую единицу измерения, используемую источником данных.
        /// </summary>
        /// <param name="value">Единица измерения.</param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        [Obsolete("Use Twain32.Capabilities.Units.Set(value) instead.")]
        public void SetUnitOfMeasure(TwUnits value) {
            this.SetCap(TwCap.IUnits,value);
        }

        #endregion

        #region All capabilities

        /// <summary>
        /// Возвращает флаги, указывающие на поддерживаемые источником данных операции, для указанного значения capability.
        /// </summary>
        /// <param name="capability">Значение перечисдения TwCap.</param>
        /// <returns>Набор флагов.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public TwQC IsCapSupported(TwCap capability) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                using(TwCapability _cap=new TwCapability(capability)) {
                    TwRC _rc=this._dsmEntry.DSCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.QuerySupport,_cap);
                    if(_rc==TwRC.Success) {
                        return (TwQC)((TwOneValue)_cap.GetValue()).Item;
                    } else {
                        return 0;
                    }
                }
            } else {
                throw new TwainException("Источник данных не открыт.");
            }
        }

        /// <summary>
        /// Возвращает значение для указанного capability (возможность).
        /// </summary>
        /// <param name="capability">Значение перечисления TwCap.</param>
        /// <param name="msg">Значение перечисления TwMSG.</param>
        /// <returns>В зависимости от значение capability, могут быть возвращены: тип-значение, массив, <see cref="Twain32.Range">диапазон</see>, <see cref="Twain32.Enumeration">перечисление</see>.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        private object _GetCapCore(TwCap capability,TwMSG msg) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                using(TwCapability _cap=new TwCapability(capability)) {
                    TwRC _rc=this._dsmEntry.DSCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,msg,_cap);
                    if(_rc==TwRC.Success) {
                        switch(_cap.ConType) {
                            case TwOn.One:
                                TwOneValue _value=(TwOneValue)_cap.GetValue();
                                return TwTypeHelper.CastToCommon(_value.ItemType,TwTypeHelper.ValueToTw<uint>(_value.ItemType,_value.Item));
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
                }
            } else {
                throw new TwainException("Источник данных не открыт.");
            }
        }

        /// <summary>
        /// Возвращает значения указанной возможности (capability).
        /// </summary>
        /// <param name="capability">Значение перечисления TwCap.</param>
        /// <returns>В зависимости от значение capability, могут быть возвращены: тип-значение, массив, <see cref="Twain32.Range">диапазон</see>, <see cref="Twain32.Enumeration">перечисление</see>.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public object GetCap(TwCap capability) {
            return this._GetCapCore(capability,TwMSG.Get);
        }

        /// <summary>
        /// Возвращает текущее значение для указанной возможности (capability).
        /// </summary>
        /// <param name="capability">Значение перечисления TwCap.</param>
        /// <returns>В зависимости от значение capability, могут быть возвращены: тип-значение, массив, <see cref="Twain32.Range">диапазон</see>, <see cref="Twain32.Enumeration">перечисление</see>.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public object GetCurrentCap(TwCap capability) {
            return this._GetCapCore(capability,TwMSG.GetCurrent);
        }

        /// <summary>
        /// Возвращает значение по умолчанию для указанной возможности (capability).
        /// </summary>
        /// <param name="capability">Значение перечисления TwCap.</param>
        /// <returns>В зависимости от значение capability, могут быть возвращены: тип-значение, массив, <see cref="Twain32.Range">диапазон</see>, <see cref="Twain32.Enumeration">перечисление</see>.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public object GetDefaultCap(TwCap capability) {
            return this._GetCapCore(capability,TwMSG.GetDefault);
        }

        /// <summary>
        /// Сбрасывает текущее значение для указанного <see cref="TwCap">capability</see> в значение по умолчанию.
        /// </summary>
        /// <param name="capability">Значение перечисления <see cref="TwCap"/>.</param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public void ResetCap(TwCap capability) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                using(TwCapability _cap=new TwCapability(capability)) {
                    TwRC _rc=this._dsmEntry.DSCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Reset,_cap);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                }
            } else {
                throw new TwainException("Источник данных не открыт.");
            }
        }

        /// <summary>
        /// Устанавливает значение для указанного <see cref="TwCap">capability</see>
        /// </summary>
        /// <param name="capability">Значение перечисления <see cref="TwCap"/>.</param>
        /// <param name="value">Устанавливаемое значение.</param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public void SetCap(TwCap capability,object value) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                TwType _type=TwTypeHelper.TypeOf(value.GetType());
                using(TwCapability _cap=new TwCapability(capability,TwTypeHelper.ValueFromTw<uint>(TwTypeHelper.CastToTw(_type,value)),_type)) {
                    TwRC _rc=this._dsmEntry.DSCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Set,_cap);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                }
            } else {
                throw new TwainException("Источник данных не открыт.");
            }
        }

        /// <summary>
        /// Устанавливает значение для указанного <see cref="TwCap">capability</see>
        /// </summary>
        /// <param name="capability">Значение перечисления <see cref="TwCap"/>.</param>
        /// <param name="capabilityValue">Устанавливаемое значение.</param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public void SetCap(TwCap capability,object[] capabilityValue) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                using(TwCapability _cap=new TwCapability(
                    capability,
                    new TwArray() {
                        ItemType=TwTypeHelper.TypeOf(capabilityValue[0].GetType()), NumItems=(uint)capabilityValue.Length
                    },
                    capabilityValue)) {

                    TwRC _rc=this._dsmEntry.DSCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Set,_cap);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                }
            } else {
                throw new TwainException("Источник данных не открыт.");
            }
        }

        /// <summary>
        /// Устанавливает значение для указанного <see cref="TwCap">capability</see>
        /// </summary>
        /// <param name="capability">Значение перечисления <see cref="TwCap"/>.</param>
        /// <param name="capabilityValue">Устанавливаемое значение.</param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public void SetCap(TwCap capability,Range capabilityValue) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                using(TwCapability _cap=new TwCapability(capability,capabilityValue.ToTwRange())) {
                    TwRC _rc=this._dsmEntry.DSCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Set,_cap);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                }
            } else {
                throw new TwainException("Источник данных не открыт.");
            }
        }

        /// <summary>
        /// Устанавливает значение для указанного <see cref="TwCap">capability</see>
        /// </summary>
        /// <param name="capability">Значение перечисления <see cref="TwCap"/>.</param>
        /// <param name="capabilityValue">Устанавливаемое значение.</param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public void SetCap(TwCap capability,Enumeration capabilityValue) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                using(TwCapability _cap=new TwCapability(
                    capability,
                    new TwEnumeration() {
                        ItemType=TwTypeHelper.TypeOf(capabilityValue.Items[0].GetType()),
                        NumItems=(uint)capabilityValue.Count,
                        CurrentIndex=(uint)capabilityValue.CurrentIndex,
                        DefaultIndex=(uint)capabilityValue.DefaultIndex
                    },
                    capabilityValue.Items)) {

                    TwRC _rc=this._dsmEntry.DSCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Set,_cap);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(),_rc);
                    }
                }
            } else {
                throw new TwainException("Источник данных не открыт.");
            }
        }

        #endregion

        #region DG_IMAGE / IMAGExxxxXFER / MSG_GET operation

        /// <summary>
        /// Выполняет передачу изображения (Native Mode Transfer).
        /// </summary>
        private void _NativeTransferPictures() {
            if(this._srcds.Id==0) {
                return;
            }
            IntPtr _hBitmap=IntPtr.Zero;
            TwPendingXfers _pxfr=new TwPendingXfers();
            this._images.Clear();

            do {
                _pxfr.Count=0;
                _hBitmap=IntPtr.Zero;

                if(this._dsmEntry.DSImageXfer(this._appid,this._srcds,TwDG.Image,TwDAT.ImageNativeXfer,TwMSG.Get,ref _hBitmap)==TwRC.XferDone) {
                    // DG_IMAGE / DAT_IMAGEINFO / MSG_GET
                    // DG_IMAGE / DAT_EXTIMAGEINFO / MSG_GET
                    this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo,this._GetExtImageInfo));

                    IntPtr _pBitmap=_Memory.Lock(_hBitmap);
                    try {
                        Image _img=null;
                        this._images.Add(_img=DibToImage.WithStream(_pBitmap));
                        this._OnEndXfer(new EndXferEventArgs(_img));
                    } finally {
                        _Memory.Unlock(_hBitmap);
                        _Memory.Free(_hBitmap);
                    }
                    if(this._dsmEntry.DSPendingXfer(this._appid,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.EndXfer,_pxfr)!=TwRC.Success) {
                        break;
                    }
                } else {
                    break;
                }
            } while(_pxfr.Count!=0);
            TwRC _rc=this._dsmEntry.DSPendingXfer(this._appid,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.Reset,_pxfr);
        }

        /// <summary>
        /// Выполняет передачу изображения (Disk File Mode Transfer).
        /// </summary>
        private void _FileTransferPictures() {
            if(this._srcds.Id==0) {
                return;
            }

            TwPendingXfers _pxfr=new TwPendingXfers();
            this._images.Clear();
            do {
                _pxfr.Count=0;
                ImageInfo _info=this._GetImageInfo();

                TwSetupFileXfer _fileXfer=new TwSetupFileXfer {Format=TwFF.Bmp,FileName=Path.GetTempFileName()};
                SetupFileXferEventArgs _args=new SetupFileXferEventArgs();
                this._OnSetupFileXfer(_args);
                if(!string.IsNullOrEmpty(_args.FileName)) {
                    _fileXfer.FileName=_args.FileName;
                }
                if((this.Capabilities.ImageFileFormat.IsSupported()&TwQC.GetCurrent)!=0) {
                    _fileXfer.Format=this.Capabilities.ImageFileFormat.GetCurrent();
                }
                TwRC _rc=this._dsmEntry.DSsfxfer(this._appid,this._srcds,TwDG.Control,TwDAT.SetupFileXfer,TwMSG.Set,_fileXfer);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }

                if(this._dsmEntry.DSifxfer(this._appid,this._srcds,TwDG.Image,TwDAT.ImageFileXfer,TwMSG.Get,IntPtr.Zero)==TwRC.XferDone) {
                    // DG_IMAGE / DAT_IMAGEINFO / MSG_GET
                    // DG_IMAGE / DAT_EXTIMAGEINFO / MSG_GET
                    this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo,this._GetExtImageInfo));
                }
                if(this._dsmEntry.DSPendingXfer(this._appid,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.EndXfer,_pxfr)!=TwRC.Success) {
                    break;
                }
                if(this._dsmEntry.DSsfxfer(this._appid,this._srcds,TwDG.Control,TwDAT.SetupFileXfer,TwMSG.Get,_fileXfer)==TwRC.Success) {
                    this._OnFileXfer(new FileXferEventArgs(ImageFileXfer.Create(_fileXfer)));
                }
            } while(_pxfr.Count!=0);
            TwRC _rc2=this._dsmEntry.DSPendingXfer(this._appid,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.Reset,_pxfr);
        }

        /// <summary>
        /// Выполняет передачу изображения (Buffered Memory Mode Transfer and Memory File Mode Transfer).
        /// </summary>
        private void _MemoryTransferPictures(bool isMemFile) {
            if(this._srcds.Id==0) {
                return;
            }

            TwPendingXfers _pxfr=new TwPendingXfers();
            this._images.Clear();
            do {
                _pxfr.Count=0;
                ImageInfo _info=this._GetImageInfo();

                if(isMemFile) {
                    if((this.Capabilities.ImageFileFormat.IsSupported()&TwQC.GetCurrent)!=0) {
                        TwSetupFileXfer _fileXfer=new TwSetupFileXfer {
                            Format=this.Capabilities.ImageFileFormat.GetCurrent()
                        };
                        TwRC _rc=this._dsmEntry.DSsfxfer(this._appid,this._srcds,TwDG.Control,TwDAT.SetupFileXfer,TwMSG.Set,_fileXfer);
                        if(_rc!=TwRC.Success) {
                            throw new TwainException(this._GetTwainStatus(),_rc);
                        }
                    }
                }

                TwSetupMemXfer _memBufSize=new TwSetupMemXfer();
                TwRC _rc1=this._dsmEntry.DSsmxfer(this._appid,this._srcds,TwDG.Control,TwDAT.SetupMemXfer,TwMSG.Get,_memBufSize);
                if(_rc1!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(),_rc1);
                }
                this._OnSetupMemXfer(new SetupMemXferEventArgs(_info,_memBufSize.Preferred));

                IntPtr _hMem=_Memory.Alloc((int)_memBufSize.Preferred);
                if(_hMem==IntPtr.Zero) {
                    throw new TwainException("Ошибка выделениия памяти.");
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
                        
                        TwRC _rc=this._dsmEntry.DSimxfer(this._appid,this._srcds,TwDG.Image,isMemFile?TwDAT.ImageMemFileXfer:TwDAT.ImageMemXfer,TwMSG.Get,_memXferBuf);
                        if(_rc==TwRC.Success||_rc==TwRC.XferDone) {
                            this._OnMemXfer(new MemXferEventArgs(_info,ImageMemXfer.Create(_memXferBuf)));
                            if(_rc==TwRC.XferDone) {
                                // DG_IMAGE / DAT_IMAGEINFO / MSG_GET
                                // DG_IMAGE / DAT_EXTIMAGEINFO / MSG_GET
                                this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo,this._GetExtImageInfo));
                                break;
                            }
                        } else {
                            break;
                        }
                    } while(true);
                } finally {
                    _Memory.Unlock(_hMem);
                    _Memory.Free(_hMem);
                }
                if(this._dsmEntry.DSPendingXfer(this._appid,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.EndXfer,_pxfr)!=TwRC.Success) {
                    break;
                }
            } while(_pxfr.Count!=0);
            TwRC _rc2=this._dsmEntry.DSPendingXfer(this._appid,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.Reset,_pxfr);
        }

        #endregion

        #region Device Event

        private void _DeviceEventObtain() {
            TwDeviceEvent _deviceEvent=new TwDeviceEvent();
            if(this._dsmEntry.DSdeviceevent(this._appid,this._srcds,TwDG.Control,TwDAT.DeviceEvent,TwMSG.Get,_deviceEvent)==TwRC.Success) {
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

        private void _OnXferDone(XferDoneEventArgs e) {
            if(this.XferDone!=null) {
                this.XferDone(this,e);
            }
        }

        private void _OnEndXfer(EndXferEventArgs e) {
            if(this.EndXfer!=null) {
                this.EndXfer(this,e);
            }
        }

        private void _OnSetupMemXfer(SetupMemXferEventArgs e) {
            if(this.SetupMemXferEvent!=null) {
                this.SetupMemXferEvent(this,e);
            }
        }

        private void _OnMemXfer(MemXferEventArgs e) {
            if(this.MemXferEvent!=null) {
                this.MemXferEvent(this,e);
            }
        }

        private void _OnSetupFileXfer(SetupFileXferEventArgs e) {
            if(this.SetupFileXferEvent!=null) {
                this.SetupFileXferEvent(this,e);
            }
        }

        private void _OnFileXfer(FileXferEventArgs e) {
            if(this.FileXferEvent!=null) {
                this.FileXferEvent(this,e);
            }
        }

        private void _OnDeviceEvent(DeviceEventEventArgs e) {
            if(this.DeviceEvent!=null) {
                this.DeviceEvent(this,e);
            }
        }

        #endregion

        /// <summary>
        /// Получает описание всех доступных источников данных.
        /// </summary>
        private void _GetAllSorces() {
            List<TwIdentity> _src=new List<TwIdentity>();
            TwIdentity _item=new TwIdentity();
            TwRC _rc=this._dsmEntry.DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.GetFirst,_item);
            if(_rc==TwRC.Success) {
                _src.Add(_item);
                do {
                    _item=new TwIdentity();
                    _rc=this._dsmEntry.DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.GetNext,_item);
                    if(_rc==TwRC.Success) {
                        _src.Add(_item);
                    }
                } while(_rc!=TwRC.EndOfList);
                _rc=this._dsmEntry.DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.GetDefault,this._srcds);
            } else {
                TwCC _state=this._GetTwainStatus();
            }
            this._sources=_src.ToArray();
        }

        /// <summary>
        /// Возвращает или устанавливает значение флагов состояния.
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
        /// Возвращает код состояния TWAIN.
        /// </summary>
        /// <returns></returns>
        private TwCC _GetTwainStatus() {
            TwStatus _status=new TwStatus();
            TwRC _rc=this._dsmEntry.DSStatus(this._appid,this._srcds,TwDG.Control,TwDAT.Status,TwMSG.Get,_status);
            return _status.ConditionCode;
        }

        /// <summary>
        /// Возвращает описание полученного изображения.
        /// </summary>
        /// <returns>Описание изображения.</returns>
        private ImageInfo _GetImageInfo() {
            TwImageInfo _imageInfo=new TwImageInfo();
            TwRC _rc=this._dsmEntry.DSImageInfo(this._appid,this._srcds,TwDG.Image,TwDAT.ImageInfo,TwMSG.Get,_imageInfo);
            if(_rc!=TwRC.Success) {
                throw new TwainException(this._GetTwainStatus(),_rc);
            }
            return ImageInfo.FromTwImageInfo(_imageInfo);
        }

        /// <summary>
        /// Возвращает расширенного описание полученного изображения.
        /// </summary>
        /// <param name="extInfo">Набор кодов расширенного описания изображения для которых требуется получить описание.</param>
        /// <returns>Расширенное описание изображения.</returns>
        private ExtImageInfo _GetExtImageInfo(TwEI[] extInfo) {
            TwInfo[] _info=new TwInfo[extInfo.Length];
            for(int i=0; i<extInfo.Length; i++) {
                _info[i]=new TwInfo {InfoId=extInfo[i]};
            }
            IntPtr _extImageInfo=TwExtImageInfo.ToPtr(_info);
            try {

                TwRC _rc=this._dsmEntry.DSExtImageInfo(this._appid,this._srcds,TwDG.Image,TwDAT.ExtImageInfo,TwMSG.Get,_extImageInfo);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus(),_rc);
                }
                return ExtImageInfo.FromPtr(_extImageInfo);
            } finally {
                Marshal.FreeHGlobal(_extImageInfo);
            }
        }

        #region import kernel32.dll

        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string fileName);

        [DllImport("kernel32.dll",CharSet=CharSet.Ansi,ExactSpelling=true)]
        static extern IntPtr GetProcAddress(IntPtr hModule,int procId);

        [DllImport("kernel32.dll",ExactSpelling=true)]
        static extern bool FreeLibrary(IntPtr hModule);

        #endregion

        #region import user32.dll

        [DllImport("user32.dll",ExactSpelling=true)]
        private static extern int GetMessagePos();

        [DllImport("user32.dll",ExactSpelling=true)]
        private static extern int GetMessageTime();

        #endregion

        /// <summary>
        /// Флаги состояния.
        /// </summary>
        [Flags]
        public enum TwainStateFlag {
            DSMOpen=0x1,
            DSOpen=0x2,
            DSEnabled=0x4
        }

        #region Events

        /// <summary>
        /// Возникает в момент окончания сканирования. Occurs when the acquire is completed.
        /// </summary>
        [Category("Action")]
        [Description("Возникает в момент окончания сканирования. Occurs when the acquire is completed.")]
        public event EventHandler AcquireCompleted;

        /// <summary>
        /// Возникает в момент окончания получения изображения приложением. Occurs when the transfer into application was completed (Native Mode Transfer).
        /// </summary>
        [Category("Native Mode Action")]
        [Description("Возникает в момент окончания получения изображения приложением. Occurs when the transfer into application was completed (Native Mode Transfer).")]
        public event EventHandler<EndXferEventArgs> EndXfer;

        /// <summary>
        /// Возникает в момент окончания получения изображения источником.
        /// </summary>
        [Category("Action")]
        [Description("Возникает в момент окончания получения изображения источником. Occurs when the transfer was completed.")]
        public event EventHandler<XferDoneEventArgs> XferDone;

        /// <summary>
        /// Возникает в момент установки размера буфера памяти. Occurs when determined size of buffer to use during the transfer (Memory Mode Transfer and MemFile Mode Transfer).
        /// </summary>
        [Category("Memory Mode Action")]
        [Description("Возникает в момент установки размера буфера памяти. Occurs when determined size of buffer to use during the transfer (Memory Mode Transfer and MemFile Mode Transfer).")]
        public event EventHandler<SetupMemXferEventArgs> SetupMemXferEvent;

        /// <summary>
        /// Возникает в момент получения очередного блока данных. Occurs when the memory block for the data was recived (Memory Mode Transfer and MemFile Mode Transfer).
        /// </summary>
        [Category("Memory Mode Action")]
        [Description("Возникает в момент получения очередного блока данных. Occurs when the memory block for the data was recived (Memory Mode Transfer and MemFile Mode Transfer).")]
        public event EventHandler<MemXferEventArgs> MemXferEvent;

        /// <summary>
        /// Возникает в момент, когда необходимо задать имя файла изображения. Occurs when you need to specify the filename (File Mode Transfer).
        /// </summary>
        [Category("File Mode Action")]
        [Description("Возникает в момент, когда необходимо задать имя файла изображения. Occurs when you need to specify the filename. (File Mode Transfer)")]
        public event EventHandler<SetupFileXferEventArgs> SetupFileXferEvent;

        /// <summary>
        /// Возникает в момент окончания получения файла изображения приложением. Occurs when the transfer into application was completed (File Mode Transfer).
        /// </summary>
        [Category("File Mode Action")]
        [Description("Возникает в момент окончания получения файла изображения приложением. Occurs when the transfer into application was completed (File Mode Transfer).")]
        public event EventHandler<FileXferEventArgs> FileXferEvent;

        /// <summary>
        /// Возникает в момент изменения состояния twain-устройства. Occurs when TWAIN state was changed.
        /// </summary>
        [Category("Behavior")]
        [Description("Возникает в момент изменения состояния twain-устройства. Occurs when TWAIN state was changed.")]
        public event EventHandler<TwainStateEventArgs> TwainStateChanged;

        /// <summary>
        /// Возникает в момент, когда источник уведомляет приложение о произошедшем событии. Occurs when enabled the source sends this message to the Application to alert it that some event has taken place.
        /// </summary>
        [Category("Behavior")]
        [Description("Возникает в момент, когда источник уведомляет приложение о произошедшем событии. Occurs when enabled the source sends this message to the Application to alert it that some event has taken place.")]
        public event EventHandler<DeviceEventEventArgs> DeviceEvent;

        #endregion

        #region Events Args

        /// <summary>
        /// Аргументы события EndXfer.
        /// </summary>
        public sealed class EndXferEventArgs:EventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса.
            /// </summary>
            /// <param name="image">Изображение.</param>
            internal EndXferEventArgs(Image image) {
                this.Image=image;
            }

            /// <summary>
            /// Возвращает изображение.
            /// </summary>
            public Image Image {
                get;
                private set;
            }
        }

        /// <summary>
        /// Аргументы события XferDone.
        /// </summary>
        public sealed class XferDoneEventArgs:EventArgs {
            private GetImageInfoCallback _imageInfoMethod;
            private GetExtImageInfoCallback _extImageInfoMethod;

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="XferDoneEventArgs"/>.
            /// </summary>
            /// <param name="method1">Метод обратного вызова для получения описания изображения.</param>
            /// <param name="method2">Метод обратного вызова для получения расширенного описания изображения.</param>
            internal XferDoneEventArgs(GetImageInfoCallback method1,GetExtImageInfoCallback method2) {
                this._imageInfoMethod=method1;
                this._extImageInfoMethod=method2;
            }

            /// <summary>
            /// Возвращает описание полученного изображения.
            /// </summary>
            /// <returns>Описание изображения.</returns>
            public ImageInfo GetImageInfo() {
                return this._imageInfoMethod();
            }

            /// <summary>
            /// Возвращает расширенного описание полученного изображения.
            /// </summary>
            /// <param name="extInfo">Набор кодов расширенного описания изображения для которых требуется получить описание.</param>
            /// <returns>Расширенное описание изображения.</returns>
            public ExtImageInfo GetExtImageInfo(TwEI[] extInfo) {
                return this._extImageInfoMethod(extInfo);
            }
        }

        /// <summary>
        /// Аргументы события SetupMemXferEvent.
        /// </summary>
        public sealed class SetupMemXferEventArgs:EventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="SetupMemXferEventArgs"/>.
            /// </summary>
            /// <param name="info">Описание изображения.</param>
            /// <param name="bufferSize">Размер буфера памяти для передачи данных.</param>
            internal SetupMemXferEventArgs(ImageInfo info,uint bufferSize) {
                this.ImageInfo=info;
                this.BufferSize=bufferSize;
            }

            /// <summary>
            /// Возвращает описание изображения.
            /// </summary>
            public ImageInfo ImageInfo {
                get;
                private set;
            }

            /// <summary>
            /// Возвращает размер буфера памяти для передачи данных.
            /// </summary>
            public uint BufferSize {
                get;
                private set;
            }
        }

        /// <summary>
        /// Аргументы события MemXferEvent.
        /// </summary>
        public sealed class MemXferEventArgs:EventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="MemXferEventArgs"/>.
            /// </summary>
            /// <param name="info">Описание изображения.</param>
            /// <param name="image">Фрагмент данных изображения.</param>
            internal MemXferEventArgs(ImageInfo info,ImageMemXfer image) {
                this.ImageInfo=info;
                this.ImageMemXfer=image;
            }

            /// <summary>
            /// Возвращает описание изображения.
            /// </summary>
            public ImageInfo ImageInfo {
                get;
                private set;
            }

            /// <summary>
            /// Возвращает фрагмент данных изображения.
            /// </summary>
            public ImageMemXfer ImageMemXfer {
                get;
                private set;
            }
        }

        /// <summary>
        /// Аргументы события SetupFileXferEvent.
        /// </summary>
        public sealed class SetupFileXferEventArgs:EventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="SetupFileXferEventArgs"/>.
            /// </summary>
            internal SetupFileXferEventArgs() {
            }

            /// <summary>
            /// Возвращает или устанавливает имя файла изображения.
            /// </summary>
            public string FileName {
                get;
                set;
            }
        }

        /// <summary>
        /// Аргументы события FileXferEvent.
        /// </summary>
        public sealed class FileXferEventArgs:EventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="FileXferEventArgs"/>.
            /// </summary>
            /// <param name="image">Описание файла изображения.</param>
            internal FileXferEventArgs(ImageFileXfer image) {
                this.ImageFileXfer=image;
            }

            /// <summary>
            /// Возвращает описание файла изображения.
            /// </summary>
            public ImageFileXfer ImageFileXfer {
                get;
                private set;
            }
        }

        /// <summary>
        /// Аргументы события TwainStateChanged.
        /// </summary>
        public sealed class TwainStateEventArgs:EventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса.
            /// </summary>
            /// <param name="flags">Флаги состояния.</param>
            internal TwainStateEventArgs(TwainStateFlag flags) {
                this.TwainState=flags;
            }

            /// <summary>
            /// Возвращает флаги состояния twain-устройства.
            /// </summary>
            public TwainStateFlag TwainState {
                get;
                private set;
            }
        }

        /// <summary>
        /// Аргументы события DeviceEvent.
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

        #endregion

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
                MethodInfo _createDelegate=typeof(_DsmEntry).GetMethod("CreateDelegate",BindingFlags.Static|BindingFlags.NonPublic);
                foreach(PropertyInfo _prop in typeof(_DsmEntry).GetProperties()) {
                    _prop.SetValue(this,_createDelegate.MakeGenericMethod(_prop.PropertyType).Invoke(this,new object[] { ptr }),null);
                }
            }

            /// <summary>
            /// Создает и возвращает новый экземпляр класса <see cref="_DsmEntry"/>.
            /// </summary>
            /// <param name="ptr">Указатель на DSM_Entry.</param>
            /// <returns>Экземпляр класса <see cref="_DsmEntry"/>.</returns>
            public static _DsmEntry Create(IntPtr ptr) {
                return new _DsmEntry(ptr);
            }

            /// <summary>
            /// Приводит указатель к требуемомы делегату.
            /// </summary>
            /// <typeparam name="T">Требуемый делегат.</typeparam>
            /// <param name="ptr">Указатель на DSM_Entry.</param>
            /// <returns>Делегат.</returns>
            private static T CreateDelegate<T>(IntPtr ptr) where T:class {
                return Marshal.GetDelegateForFunctionPointer(ptr,typeof(T)) as T;
            }

            #region Properties

            public _DSMparent DsmParent {
                get;
                private set;
            }

            public _DSMident DsmIdent {
                get;
                private set;
            }

            public _DSMEntryPoint DsmEntryPoint {
                get;
                private set;
            }

            public _DSMstatus DsmStatus {
                get;
                private set;
            }

            public _DSuserif DSUI {
                get;
                private set;
            }

            public _DSevent DSEvent {
                get;
                private set;
            }

            public _DSstatus DSStatus {
                get;
                private set;
            }

            public _DScap DSCap {
                get;
                private set;
            }

            public _DSiinf DSImageInfo {
                get;
                private set;
            }

            public TwDSFuncPtr DSExtImageInfo {
                get;
                private set;
            }

            public _DSixfer DSImageXfer {
                get;
                private set;
            }

            public _DSpxfer DSPendingXfer {
                get;
                private set;
            }

            public _DSimagelayout DSImageLayout {
                get;
                private set;
            }

            public _DSsmxfer DSsmxfer {
                get;
                private set;
            }

            public _DSimxfer DSimxfer {
                get;
                private set;
            }

            public _DSsfxfer DSsfxfer {
                get;
                private set;
            }

            public TwDSFuncPtr DSifxfer {
                get;
                private set;
            }

            public _DSpalette DSpalette {
                get;
                private set;
            }

            public _DSdeviceevent DSdeviceevent {
                get;
                private set;
            }

            #endregion
        }

        /// <summary>
        /// Точки входа для функций управления памятью.
        /// </summary>
        internal sealed class _Memory {
            private static TwEntryPoint _entryPoint;

            /// <summary>
            /// Выделяет блок памяти указанного размера.
            /// </summary>
            /// <param name="size">Размер блока памяти.</param>
            /// <returns>Дескриптор памяти.</returns>
            public static IntPtr Alloc(int size) {
                if(_Memory._entryPoint!=null&&_Memory._entryPoint.MemoryAllocate!=null) {
                    return _Memory._entryPoint.MemoryAllocate(size);
                }
                return _Memory.GlobalAlloc(0x42,size);
            }

            /// <summary>
            /// Освобождает память.
            /// </summary>
            /// <param name="handle">Дескриптор памяти.</param>
            public static void Free(IntPtr handle) {
                if(_Memory._entryPoint!=null&&_Memory._entryPoint.MemoryFree!=null) {
                    _Memory._entryPoint.MemoryFree(handle);
                    return;
                }
                _Memory.GlobalFree(handle);
            }

            /// <summary>
            /// Выполняет блокировку памяти.
            /// </summary>
            /// <param name="handle">Дескриптор памяти.</param>
            /// <returns>Указатель на блок памяти.</returns>
            public static IntPtr Lock(IntPtr handle) {
                if(_Memory._entryPoint!=null&&_Memory._entryPoint.MemoryLock!=null) {
                    return _Memory._entryPoint.MemoryLock(handle);
                }
                return _Memory.GlobalLock(handle);
            }

            /// <summary>
            /// Выполняет разблокировку памяти.
            /// </summary>
            /// <param name="handle">Дескриптор памяти.</param>
            public static void Unlock(IntPtr handle) {
                if(_Memory._entryPoint!=null&&_Memory._entryPoint.MemoryUnlock!=null) {
                    _Memory._entryPoint.MemoryUnlock(handle);
                    return;
                }
                _Memory.GlobalUnlock(handle);
            }

            /// <summary>
            /// Устаначливает точки входа.
            /// </summary>
            /// <param name="entry">Точки входа.</param>
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
            public static extern void ZeroMemory(IntPtr dest,IntPtr size);


            #endregion
        }

        /// <summary>
        /// Фильтр win32-сообщений.
        /// </summary>
        private sealed class _MessageFilter:IMessageFilter,IDisposable {
            private Twain32 _twain;
            private bool _is_set_filter=false;
            private TwEvent _evtmsg;

            public _MessageFilter(Twain32 twain) {
                this._twain=twain;
                this._evtmsg.EventPtr=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINMSG)));
            }

            #region IMessageFilter

            public bool PreFilterMessage(ref Message m) {
                TwainCommand _cmd=this._PassMessage(ref m);
                if(_cmd==TwainCommand.Not) {
                    return false;
                }
                MethodInvoker _end=()=>{
                    this.RemoveFilter();
                    if(this._twain.DisableAfterAcquire) {
                        this._twain._DisableDataSource();
                    }
                };
                switch(_cmd) {
                    case TwainCommand.CloseRequest:
                    case TwainCommand.CloseOk:
                        _end();
                        break;
                    case TwainCommand.DeviceEvent:
                        this._twain._DeviceEventObtain();
                        break;
                    case TwainCommand.TransferReady:
                        switch(this._twain.Capabilities.XferMech.GetCurrent()) {
                            case TwSX.File:
                                this._twain._FileTransferPictures();
                                break;
                            case TwSX.Memory:
                                this._twain._MemoryTransferPictures(false);
                                break;
                            case TwSX.MemFile:
                                this._twain._MemoryTransferPictures(true);
                                break;
                            default:
                                this._twain._NativeTransferPictures();
                                break;
                        }
                        _end();
                        this._twain._OnAcquireCompleted(new EventArgs());
                        break;
                }
                return true;
            }

            #endregion

            #region IDisposable

            public void Dispose() {
                if(this._evtmsg.EventPtr!=IntPtr.Zero) {
                    Marshal.FreeHGlobal(this._evtmsg.EventPtr);
                    this._evtmsg.EventPtr=IntPtr.Zero;
                }
            }

            #endregion

            private TwainCommand _PassMessage(ref Message m) {
                if(this._twain._srcds.Id==0) {
                    return TwainCommand.Not;
                }

                int _pos=GetMessagePos();
                WINMSG _winmsg=new WINMSG {
                    hwnd=m.HWnd,
                    message=m.Msg,
                    wParam=m.WParam,
                    lParam=m.LParam,
                    time=GetMessageTime(),
                    x=(short)_pos,
                    y=(short)(_pos>>16)
                };
                Marshal.StructureToPtr(_winmsg,this._evtmsg.EventPtr,true);
                this._evtmsg.Message=0;

                TwRC rc=this._twain._dsmEntry.DSEvent(this._twain._appid,this._twain._srcds,TwDG.Control,TwDAT.Event,TwMSG.ProcessEvent,ref this._evtmsg);
                if(rc==TwRC.NotDSEvent) {
                    return TwainCommand.Not;
                }
                switch(this._evtmsg.Message) {
                    case TwMSG.XFerReady:
                        return TwainCommand.TransferReady;
                    case TwMSG.CloseDSReq:
                        return TwainCommand.CloseRequest;
                    case TwMSG.CloseDSOK:
                        return TwainCommand.CloseOk;
                    case TwMSG.DeviceEvent:
                        return TwainCommand.DeviceEvent;
                }
                return TwainCommand.Null;
            }

            public void SetFilter() {
                if(!this._is_set_filter) {
                    this._is_set_filter=true;
                    Application.AddMessageFilter(this);
                }
            }

            private void RemoveFilter() {
                Application.RemoveMessageFilter(this);
                this._is_set_filter=false;
            }

            [StructLayout(LayoutKind.Sequential,Pack=4)]
            internal struct WINMSG {
                public IntPtr hwnd;
                public int message;
                public IntPtr wParam;
                public IntPtr lParam;
                public int time;
                public int x;
                public int y;
            }

            private enum TwainCommand {
                Not=-1,
                Null=0,
                TransferReady=1,
                CloseRequest=2,
                CloseOk=3,
                DeviceEvent=4
            }
        }

        /// <summary>
        /// Диапазон значений.
        /// </summary>
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
            /// Создает и возвращает экземпляр <see cref="Range"/>.
            /// </summary>
            /// <param name="range">Экземпляр <see cref="TwRange"/>.</param>
            /// <returns>Экземпляр <see cref="Range"/>.</returns>
            internal static Range CreateRange(TwRange range) {
                return new Range(range);
            }

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Range"/>.
            /// </summary>
            /// <param name="minValue">Минимальное значение.</param>
            /// <param name="maxValue">Максимальное значение.</param>
            /// <param name="stepSize">Шаг.</param>
            /// <param name="defaultValue">Значение по умолчанию.</param>
            /// <param name="currentValue">Текущее значение.</param>
            /// <returns>Экземпляр <see cref="Range"/>.</returns>
            public static Range CreateRange(object minValue,object maxValue,object stepSize,object defaultValue,object currentValue) {
                return new Range() {
                    MinValue=minValue, MaxValue=maxValue, StepSize=stepSize, DefaultValue=defaultValue, CurrentValue=currentValue
                };
            }

            /// <summary>
            /// Возвращает или устанавливает минимальное значение.
            /// </summary>
            public object MinValue {
                get;
                set;
            }

            /// <summary>
            /// Возвращает или устанавливает максимальное значение.
            /// </summary>
            public object MaxValue {
                get;
                set;
            }

            /// <summary>
            /// Возвращает или устанавливает шаг.
            /// </summary>
            public object StepSize {
                get;
                set;
            }

            /// <summary>
            /// Возвращает или устанавливает значае по умолчанию.
            /// </summary>
            public object DefaultValue {
                get;
                set;
            }

            /// <summary>
            /// Возвращает или устанавливает текущее значение.
            /// </summary>
            public object CurrentValue {
                get;
                set;
            }

            /// <summary>
            /// Конвертирует экземпляр класса в экземпляр <see cref="TwRange"/>.
            /// </summary>
            /// <returns>Экземпляр <see cref="TwRange"/>.</returns>
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
        /// Перечисление.
        /// </summary>
        public sealed class Enumeration {
            private object[] _items;

            /// <summary>
            /// Prevents a default instance of the <see cref="Enumeration"/> class from being created.
            /// </summary>
            /// <param name="items">Элементы перечисления.</param>
            /// <param name="currentIndex">Текущий индекс.</param>
            /// <param name="defaultIndex">Индекс по умолчанию.</param>
            private Enumeration(object[] items,int currentIndex,int defaultIndex) {
                this._items=items;
                this.CurrentIndex=currentIndex;
                this.DefaultIndex=defaultIndex;
            }

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Enumeration"/>.
            /// </summary>
            /// <param name="items">Элементы перечисления.</param>
            /// <param name="currentIndex">Текущий индекс.</param>
            /// <param name="defaultIndex">Индекс по умолчанию.</param>
            /// <returns>Экземпляр <see cref="Enumeration"/>.</returns>
            public static Enumeration CreateEnumeration(object[] items,int currentIndex,int defaultIndex) {
                return new Enumeration(items,currentIndex,defaultIndex);
            }

            /// <summary>
            /// Возвращает количество элементов.
            /// </summary>
            public int Count {
                get {
                    return this._items.Length;
                }
            }

            /// <summary>
            /// Возвращает текущий индекс.
            /// </summary>
            public int CurrentIndex {
                get;
                private set;
            }

            /// <summary>
            /// Возвращает индекс по умолчанию.
            /// </summary>
            public int DefaultIndex {
                get;
                private set;
            }

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
                    this._items[index]=value;
                }
            }

            internal object[] Items {
                get {
                    return this._items;
                }
            }

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Enumeration"/>.
            /// </summary>
            /// <param name="value">Экземпляр <see cref="Range"/>.</param>
            /// <returns>Экземпляр <see cref="Enumeration"/>.</returns>
            public static Enumeration FromRange(Range value) {
                int _current_index=0, _default_index=0;
                object[] _items=new object[(int)(((float)value.MaxValue-(float)value.MinValue)/(float)value.StepSize)];
                for (int i=0; i<_items.Length; i++) {
                    _items[i]=(float)value.MinValue+((float)value.StepSize*(float)i);
                    if ((float)_items[i]==(float)value.CurrentValue) {
                        _current_index=i;
                    }
                    if ((float)_items[i]==(float)value.DefaultValue) {
                        _default_index=i;
                    }
                }
                return Enumeration.CreateEnumeration(_items, _current_index, _default_index);
            }

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Enumeration"/>.
            /// </summary>
            /// <param name="value">Массив значений.</param>
            /// <returns>Экземпляр <see cref="Enumeration"/>.</returns>
            public static Enumeration FromArray(object[] value) {
                return Enumeration.CreateEnumeration(value, 0, 0);
            }

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Enumeration"/>.
            /// </summary>
            /// <param name="value">Значение.</param>
            /// <returns>Экземпляр <see cref="Enumeration"/>.</returns>
            public static Enumeration FromOneValue(ValueType value) {
                return Enumeration.CreateEnumeration(new object[] { value }, 0, 0);
            }

            internal static Enumeration FromObject(object value) {
                if (value is Range) {
                    return Enumeration.FromRange((Range)value);
                }
                if (value is object[]) {
                    return Enumeration.FromArray((object[])value);
                }
                if (value is ValueType) {
                    return Enumeration.FromOneValue((ValueType)value);
                }
                return value as Enumeration;
            }
        }

        /// <summary>
        /// Описание изображения.
        /// </summary>
        public sealed class ImageInfo {

            private ImageInfo() {
            }

            /// <summary>
            /// Создает и возвращает новый экземпляр класса ImageInfo на основе экземпляра класса TwImageInfo.
            /// </summary>
            /// <param name="info">Описание изображения.</param>
            /// <returns>Экземпляр класса ImageInfo.</returns>
            internal static ImageInfo FromTwImageInfo(TwImageInfo info) {
                return new ImageInfo {
                    BitsPerPixel=info.BitsPerPixel,
                    BitsPerSample=info.BitsPerSample,
                    Compression=info.Compression,
                    ImageLength=info.ImageLength,
                    ImageWidth=info.ImageWidth,
                    PixelType=info.PixelType,
                    Planar=info.Planar,
                    SamplesPerPixel=info.SamplesPerPixel,
                    XResolution=info.XResolution,
                    YResolution=info.YResolution
                };
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
            /// Number of samples per pixel, 3 for RGB
            /// </summary>
            public short SamplesPerPixel {
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
        /// Расширенное описание изображения.
        /// </summary>
        public sealed class ExtImageInfo:Collection<ExtImageInfo.InfoItem> {

            private ExtImageInfo() {
            }

            /// <summary>
            /// Создает и возвращает экземпляр класса ExtImageInfo из блока неуправляемой памяти.
            /// </summary>
            /// <param name="ptr">Указатель на блок неуправляемой памяти.</param>
            /// <returns>Экземпляр класса ExtImageInfo.</returns>
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
            /// Возвращает элемент описания расширенной информации о изображении по его коду.
            /// </summary>
            /// <param name="infoId">Код элемента описания расширенной информации о изображении.</param>
            /// <returns>Элемент описания расширенной информации о изображении.</returns>
            /// <exception cref="System.Collections.Generic.KeyNotFoundException">Для указанного кода отсутствует соответствующий элемент.</exception>
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
            /// Элемент описания расширенной информации о изображении.
            /// </summary>
            [DebuggerDisplay("InfoId = {InfoId}, IsSuccess = {IsSuccess}, Value = {Value}")]
            public sealed class InfoItem {

                private InfoItem() {
                }

                /// <summary>
                /// Создает и возвращает экземпляр класса элемента описания расширенной информации о изображении из внутреннего экземпляра класса элемента описания расширенной информации о изображении.
                /// </summary>
                /// <param name="info">Внутрений экземпляр класса элемента описания расширенной информации о изображении.</param>
                /// <returns>Экземпляр класса элемента описания расширенной информации о изображении.</returns>
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
                /// Возвращает код расширенной информации о изображении.
                /// </summary>
                public TwEI InfoId {
                    get;
                    private set;
                }

                /// <summary>
                /// Возвращает true, если запрошенная информация не поддерживается источником данных; иначе, false.
                /// </summary>
                public bool IsNotSupported {
                    get;
                    private set;
                }

                /// <summary>
                /// Возвращает true, если запрошенная информация поддерживается источником данных, но в данный момент недоступна; иначе, false.
                /// </summary>
                public bool IsNotAvailable {
                    get;
                    private set;
                }

                /// <summary>
                /// Возвращает true, если запрошенная информация была успешно извлечена; иначе, false.
                /// </summary>
                public bool IsSuccess {
                    get;
                    private set;
                }

                /// <summary>
                /// Возвращает значение элемента.
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
        public sealed class ImageMemXfer {

            private ImageMemXfer() {
            }

            internal static ImageMemXfer Create(TwImageMemXfer data) {
                ImageMemXfer _res=new ImageMemXfer() {
                    BytesPerRow=data.BytesPerRow,
                    BytesWritten=data.BytesWritten,
                    Columns=data.Columns,
                    Compression=data.Compression,
                    Rows=data.Rows,
                    XOffset=data.XOffset,
                    YOffset=data.YOffset
                };
                if((data.Memory.Flags&TwMF.Handle)!=0) {
                    IntPtr _data=Twain32._Memory.Lock(data.Memory.TheMem);
                    try {
                        _res.ImageData=new byte[_res.BytesWritten];
                        Marshal.Copy(_data,_res.ImageData,0,_res.ImageData.Length);
                    } finally {
                        Twain32._Memory.Unlock(data.Memory.TheMem);
                    }
                } else {
                    _res.ImageData=new byte[_res.BytesWritten];
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
            /// How many bytes written in Memory.
            /// </summary>
            public uint BytesWritten {
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
        /// Описание файла изображения.
        /// </summary>
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
            internal static ImageFileXfer Create(TwSetupFileXfer data) {
                return new ImageFileXfer {
                    FileName=data.FileName,
                    Format=data.Format
                };
            }

            /// <summary>
            /// Возвращает имя файла.
            /// </summary>
            public string FileName {
                get;
                private set;
            }

            /// <summary>
            /// Фозвращает формат файла.
            /// </summary>
            public TwFF Format {
                get;
                private set;
            }
        }

        /// <summary>
        /// Набор операций для работы с цветовой палитрой.
        /// </summary>
        public sealed class TwainPalette {
            private Twain32 _twain;

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="TwainPalette"/>.
            /// </summary>
            /// <param name="twain">Экземпляр класса <see cref="TwainPalette"/>.</param>
            internal TwainPalette(Twain32 twain) {
                this._twain=twain;
            }

            /// <summary>
            /// Возвращает текущую цветовую палитру.
            /// </summary>
            /// <returns>Экземпляр класса <see cref="TwainPalette"/>.</returns>
            public ColorPalette Get() {
                TwPalette8 _palette=new TwPalette8();
                TwRC _rc=this._twain._dsmEntry.DSpalette(this._twain._appid,this._twain._srcds,TwDG.Image,TwDAT.Palette8,TwMSG.Get,_palette);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(),_rc);
                }
                return _palette;
            }

            /// <summary>
            /// Возвращает текущую цветовую палитру, используемую по умолчанию.
            /// </summary>
            /// <returns>Экземпляр класса <see cref="TwainPalette"/>.</returns>
            public ColorPalette GetDefault() {
                TwPalette8 _palette=new TwPalette8();
                TwRC _rc=this._twain._dsmEntry.DSpalette(this._twain._appid,this._twain._srcds,TwDG.Image,TwDAT.Palette8,TwMSG.GetDefault,_palette);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(),_rc);
                }
                return _palette;
            }

            /// <summary>
            /// Сбрасывает текущую цветовую палитру и устанавливает указанную.
            /// </summary>
            /// <param name="palette">Экземпляр класса <see cref="TwainPalette"/>.</param>
            public void Reset(ColorPalette palette) {
                TwRC _rc=this._twain._dsmEntry.DSpalette(this._twain._appid,this._twain._srcds,TwDG.Image,TwDAT.Palette8,TwMSG.Reset,palette);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(),_rc);
                }
            }

            /// <summary>
            /// Устанавливает указанную цветовую палитру.
            /// </summary>
            /// <param name="palette">Экземпляр класса <see cref="TwainPalette"/>.</param>
            public void Set(ColorPalette palette) {
                TwRC _rc=this._twain._dsmEntry.DSpalette(this._twain._appid,this._twain._srcds,TwDG.Image,TwDAT.Palette8,TwMSG.Set,palette);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._twain._GetTwainStatus(),_rc);
                }
            }
        }

        /// <summary>
        /// Цветовая палитра.
        /// </summary>
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
            /// Возвращает тип палитры.
            /// </summary>
            public TwPA PaletteType {
                get;
                private set;
            }

            /// <summary>
            /// Возвращает цвета, входящие в состав палитры.
            /// </summary>
            public Color[] Colors {
                get;
                private set;
            }
        }

        #endregion

        #region Delegates

        #region DSM delegates DAT_ variants

        private delegate TwRC _DSMparent([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr refptr);

        private delegate TwRC _DSMident([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwIdentity idds);

        private delegate TwRC _DSMEntryPoint([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwEntryPoint entry);

        private delegate TwRC _DSMstatus([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwStatus dsmstat);

        #endregion

        #region DS delegates DAT_ variants to DS

        private delegate TwRC _DSuserif([In,Out] TwIdentity origin,[In,Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,TwUserInterface guif);

        private delegate TwRC _DSevent([In,Out] TwIdentity origin,[In,Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,ref TwEvent evt);

        private delegate TwRC _DSstatus([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwStatus dsmstat);

        private delegate TwRC _DScap([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwCapability capa);

        private delegate TwRC _DSiinf([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[Out] TwImageInfo imgInf);

        //private delegate TwRC _DSextiinf([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,/*[In,Out] TwExtImageInfo*/ IntPtr extImgInf);

        private delegate TwRC _DSimagelayout([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwImageLayout imageLayuot);

        private delegate TwRC _DSixfer([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr hbitmap);

        private delegate TwRC _DSpxfer([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwPendingXfers pxfr);

        private delegate TwRC _DSsmxfer([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwSetupMemXfer smxfr);

        private delegate TwRC _DSimxfer([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwImageMemXfer imxfr);

        private delegate TwRC _DSsfxfer([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwSetupFileXfer sfxfr);

        private delegate TwRC TwDSFuncPtr([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,IntPtr arg);

        private delegate TwRC _DSpalette([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwPalette8 palette);
        
        private delegate TwRC _DSdeviceevent([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[Out] TwDeviceEvent deveceEvent);

        #endregion

        internal delegate ImageInfo GetImageInfoCallback();

        internal delegate ExtImageInfo GetExtImageInfoCallback(TwEI[] extInfo);

        #endregion
    }
}
