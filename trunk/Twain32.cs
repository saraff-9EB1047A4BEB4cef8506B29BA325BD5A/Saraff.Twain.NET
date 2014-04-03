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

namespace Saraff.Twain {

    /// <summary>
    /// Обеспечивает возможность работы с TWAIN-источниками.
    /// </summary>
    [ToolboxBitmap(typeof(Twain32),"Resources.scanner.bmp")]
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
                    MajorNum=(short)_version.Major,
                    MinorNum=(short)_version.Minor,
                    Language=TwLanguage.USA,
                    Country=TwCountry.USA,
                    Info=_asm_name.Version.ToString()
                },
                ProtocolMajor=(short)(this._isTwain2Enable?2:1),
                ProtocolMinor=(short)(this._isTwain2Enable?0:9),
                SupportedGroups=(int)(TwDG.Image|TwDG.Control)|(this._isTwain2Enable?(int)TwDF.APP2:0),
                Manufacturer=((AssemblyCompanyAttribute)_asm.GetCustomAttributes(typeof(AssemblyCompanyAttribute),false)[0]).Company,
                ProductFamily="TWAIN Class Library",
                ProductName=((AssemblyProductAttribute)_asm.GetCustomAttributes(typeof(AssemblyProductAttribute),false)[0]).Product
            };
            this._srcds=new TwIdentity();
            this._srcds.Id=0;
            this._filter=new _MessageFilter(this);
            this.ShowUI=true;
            this.DisableAfterAcquire=true;
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
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString() {
            if(!string.IsNullOrEmpty(this._srcds.ProductName)) {
                return string.Format("{0}, Version={1}",this._srcds.ProductName,this._srcds.Version);
            } else {
                return this._appid.ProductName;
            }
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
                        var _entry=new _TwEntryPoint();
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
                    ShowUI=(short)(this.ShowUI?1:0),
                    ModalUI=(short)(this.ModalUI?1:0),
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
                    ShowUI=0xff
                };
                _rc=this._dsmEntry.DSUI(this._appid,this._srcds,TwDG.Control,TwDAT.UserInterface,TwMSG.DisableDS,_guif);
                if(_rc==TwRC.Success) {
                    this._TwainState&=~TwainStateFlag.DSEnabled;
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
        [DefaultValue(true)]
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
                if(IntPtr.Size!=4) {
                    throw new InvalidOperationException("In x64 mode only TWAIN 2.0 enabled.");
                }
                if(this._isTwain2Enable=value) {
                    this._appid.SupportedGroups|=(int)TwDF.APP2;
                } else {
                    this._appid.SupportedGroups&=~(int)TwDF.APP2;
                }
                this._appid.ProtocolMajor=(short)(this._isTwain2Enable?2:1);
                this._appid.ProtocolMinor=(short)(this._isTwain2Enable?0:9);
            }
        }

        /// <summary>
        /// Возвращает истину, если DSM поддерживает TWAIN 2.0; иначе лож.
        /// </summary>
        public bool IsTwain2Supported {
            get {
                if((this._TwainState&TwainStateFlag.DSMOpen)==0) {
                    throw new InvalidOperationException("DSM is not open.");
                }
                return (this._appid.SupportedGroups&(int)TwDF.DSM2)!=0;
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
            return (this._sources[index].SupportedGroups&(int)TwDF.DS2)!=0;
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
                var _imageLayout=new TwImageLayout();
                var _rc=this._dsmEntry.DSImageLayuot(this._appid,this._srcds,TwDG.Image,TwDAT.ImageLayout,TwMSG.Get,_imageLayout);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus());
                }
                return new RectangleF(
                    _imageLayout.Frame.Left.ToFloat(),
                    _imageLayout.Frame.Top.ToFloat(),
                    _imageLayout.Frame.Right.ToFloat()-_imageLayout.Frame.Left.ToFloat(),
                    _imageLayout.Frame.Bottom.ToFloat()-_imageLayout.Frame.Top.ToFloat());
            }
            set {
                var _imageLayout=new TwImageLayout() {
                    Frame=new TwFrame() {
                        Left=TwFix32.FromFloat(value.Left),
                        Top=TwFix32.FromFloat(value.Top),
                        Right=TwFix32.FromFloat(value.Right),
                        Bottom=TwFix32.FromFloat(value.Bottom)
                    }
                };
                var _rc=this._dsmEntry.DSImageLayuot(this._appid,this._srcds,TwDG.Image,TwDAT.ImageLayout,TwMSG.Set,_imageLayout);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus());
                }
            }
        }

        /// <summary>
        /// Возвращает разрешения, поддерживаемые источником данных.
        /// </summary>
        /// <returns>Коллекция значений.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public Enumeration GetResolutions() {
            return Enumeration.FromObject(this.GetCap(TwCap.XResolution));
        }

        /// <summary>
        /// Устанавливает текущее разрешение.
        /// </summary>
        /// <param name="value">Разрешение.</param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public void SetResolutions(float value) {
            this.SetCap(TwCap.XResolution,value);
            this.SetCap(TwCap.YResolution,value);
        }

        /// <summary>
        /// Возвращает типы пикселей, поддерживаемые источником данных.
        /// </summary>
        /// <returns>Коллекция значений.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public Enumeration GetPixelTypes() {
            Enumeration _val=Enumeration.FromObject(this.GetCap(TwCap.IPixelType));
            for(int i=0;i<_val.Count;i++) {
                _val[i]=(TwPixelType)Convert.ToInt16(_val[i]);
            }
            return _val;
        }

        /// <summary>
        /// Устанавливает текущий тип пикселей.
        /// </summary>
        /// <param name="value">Тип пикселей.</param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public void SetPixelType(TwPixelType value) {
            this.SetCap(TwCap.IPixelType,(ushort)value);
        }

        /// <summary>
        /// Возвращает единицы измерения, используемые источником данных.
        /// </summary>
        /// <returns>Единицы измерения.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public Enumeration GetUnitOfMeasure() {
            Enumeration _val=Enumeration.FromObject(this.GetCap(TwCap.IUnits));
            for (int i=0; i<_val.Count; i++) {
                _val[i]=(TwUnits)Convert.ToInt16(_val[i]);
            }
            return _val;
        }

        /// <summary>
        /// Устанавливает текущую единицу измерения, используемую источником данных.
        /// </summary>
        /// <param name="value">Единица измерения.</param>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public void SetUnitOfMeasure(TwUnits value) {
            this.SetCap(TwCap.IUnits,(ushort)value);
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
                        return (TwQC)((_TwOneValue)_cap.GetValue()).Item;
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
        /// <returns>В зависимости от значение capability, могут быть возвращены: тип-значение, массив, <see cref="Twain32.Range">диапазон</see>, <see cref="Twain32.Enumeration">перечисление</see>.</returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public object GetCap(TwCap capability) {
            if((this._TwainState&TwainStateFlag.DSOpen)!=0) {
                using(TwCapability _cap=new TwCapability(capability)) {
                    TwRC _rc=this._dsmEntry.DSCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Get,_cap);
                    if(_rc==TwRC.Success) {
                        switch(_cap.ConType) {
                            case TwOn.One:
                                _TwOneValue _one_val=(_TwOneValue)_cap.GetValue();
                                return Twain32._Convert(_one_val.Item,_one_val.ItemType);
                            case TwOn.Range:
                                return Range.CreateRange((_TwRange)_cap.GetValue());
                            case TwOn.Array:
                            case TwOn.Enum:
                                object _val=_cap.GetValue();
                                TwType _item_type=(TwType)_val.GetType().GetProperty("ItemType").GetValue(_val,null);
                                object[] _result_array=new object[(int)_val.GetType().GetProperty("NumItems").GetValue(_val,null)];
                                object _items=_val.GetType().GetProperty("ItemList").GetValue(_val,null);
                                MethodInfo _items_getter=_items.GetType().GetMethod("GetValue",new Type[]{typeof(int)});
                                for(int i=0;i<_result_array.Length;i++) {
                                    _result_array[i]=Twain32._Convert(_items_getter.Invoke(_items,new object[] { i }),_item_type);
                                }
                                if(_cap.ConType==TwOn.Array) {
                                    return _result_array;
                                } else {
                                    return Enumeration.CreateEnumeration(
                                        _result_array,
                                        (int)_val.GetType().GetProperty("CurrentIndex").GetValue(_val,null),
                                        (int)_val.GetType().GetProperty("DefaultIndex").GetValue(_val,null));
                                }
                                
                        }
                        return _cap.GetValue();
                    } else {
                        throw new TwainException(this._GetTwainStatus());
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
                using(TwCapability _cap=new TwCapability(capability,Twain32._Convert(value),TwTypeHelper.TypeOf(value.GetType()))) {
                    TwRC _rc=this._dsmEntry.DSCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Set,_cap);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus());
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
                    new _TwArray() {
                        ItemType=TwTypeHelper.TypeOf(capabilityValue[0].GetType()), NumItems=capabilityValue.Length
                    },
                    capabilityValue)) {

                    TwRC _rc=this._dsmEntry.DSCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Set,_cap);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus());
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
                        throw new TwainException(this._GetTwainStatus());
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
                    new _TwEnumeration() {
                        ItemType=TwTypeHelper.TypeOf(capabilityValue.Items[0].GetType()),
                        NumItems=capabilityValue.Count,
                        CurrentIndex=capabilityValue.CurrentIndex,
                        DefaultIndex=capabilityValue.DefaultIndex
                    },
                    capabilityValue.Items)) {

                    TwRC _rc=this._dsmEntry.DSCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Set,_cap);
                    if(_rc!=TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus());
                    }
                }
            } else {
                throw new TwainException("Источник данных не открыт.");
            }
        }

        /// <summary>
        /// Конвертирует тип TWAIN в тип-значение
        /// </summary>
        /// <param name="item">Конвертируемое значение.</param>
        /// <param name="itemType">Тип конвертируемого значения.</param>
        /// <returns>Тип-значение.</returns>
        private static object _Convert(object item,TwType itemType) {
            switch(itemType) {
                case TwType.Int8:
                    return (sbyte)Convert.ToByte(item);
                case TwType.Int16:
                    return (short)Convert.ToUInt16(item);
                case TwType.Int32:
                    return (int)Convert.ToUInt32(item);
                case TwType.UInt8:
                    return Convert.ToByte(item);
                case TwType.UInt16:
                    return Convert.ToUInt16(item);
                case TwType.UInt32:
                    return Convert.ToUInt32(item);
                case TwType.Bool:
                    return Convert.ToUInt32(item)!=0;
                case TwType.Fix32:
                    return TwFix32.FromInt32(Convert.ToInt32(item)).ToFloat();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Конвертирует указанный экземпляр в экземпляр Int32.
        /// </summary>
        /// <param name="item">Конвертируемый экземпляр.</param>
        /// <returns>Экземпляр Int32.</returns>
        private static int _Convert(object item) {
            switch(TwTypeHelper.TypeOf(item.GetType())){
                case TwType.Bool:
                    return (bool)item?1:0;
                case TwType.Fix32:
                    return TwFix32.FromFloat((float)item).ToInt32();
                default:
                    return Convert.ToInt32(item);
            }
        }

        #endregion

        /// <summary>
        /// Выполняет передачу изображения.
        /// </summary>
        private void _TransferPictures() {
            if(this._srcds.Id==0) {
                return;
            }
            IntPtr _hBitmap=IntPtr.Zero;
            var _pxfr=new TwPendingXfers();
            this._images.Clear();

            do {
                _pxfr.Count=0;
                _hBitmap=IntPtr.Zero;

                var _isXferDone=this._dsmEntry.DSImageXfer(this._appid,this._srcds,TwDG.Image,TwDAT.ImageNativeXfer,TwMSG.Get,ref _hBitmap)==TwRC.XferDone;

                if(_isXferDone) {
                    // DG_IMAGE / DAT_IMAGEINFO / MSG_GET
                    // DG_IMAGE / DAT_EXTIMAGEINFO / MSG_GET
                    this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo,this._GetExtImageInfo));
                }
                
                if(this._dsmEntry.DSPendingXfer(this._appid,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.EndXfer,_pxfr)==TwRC.Success&&_isXferDone) {
                    IntPtr _pBitmap=_Memory.Lock(_hBitmap);
                    try {
                        Image _img=null;
                        this._images.Add(_img=DibToImage.WithScan0(_pBitmap));
                        this._OnEndXfer(new EndXferEventArgs(_img));
                    } finally {
                        _Memory.Unlock(_hBitmap);
                        _Memory.Free(_hBitmap);
                    }
                } else {
                    break;
                }
            } while(_pxfr.Count!=0);
            var _rc=this._dsmEntry.DSPendingXfer(this._appid,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.Reset,_pxfr);
        }

        private void _OnAcquireCompleted(EventArgs e) {
            if(this._context!=null) {
                this._context.ExitThread();
                this._context.Dispose();
                this._context=null;
            }
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
            var _imageInfo=new TwImageInfo();
            var _rc=this._dsmEntry.DSImageInfo(this._appid,this._srcds,TwDG.Image,TwDAT.ImageInfo,TwMSG.Get,_imageInfo);
            if(_rc!=TwRC.Success) {
                throw new TwainException(this._GetTwainStatus());
            }
            return ImageInfo.FromTwImageInfo(_imageInfo);
        }

        /// <summary>
        /// Возвращает расширенного описание полученного изображения.
        /// </summary>
        /// <param name="extInfo">Набор кодов расширенного описания изображения для которых требуется получить описание.</param>
        /// <returns>Расширенное описание изображения.</returns>
        private ExtImageInfo _GetExtImageInfo(TwEI[] extInfo) {
            var _info=new TwInfo[extInfo.Length];
            for(int i=0; i<extInfo.Length; i++) {
                _info[i]=new TwInfo {InfoId=extInfo[i]};
            }
            var _extImageInfo=TwExtImageInfo.ToPtr(_info);
            try {

                var _rc=this._dsmEntry.DSExtImageInfo(this._appid,this._srcds,TwDG.Image,TwDAT.ExtImageInfo,TwMSG.Get,_extImageInfo);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus());
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
        /// Возникает в момент окончания сканирования.
        /// </summary>
        [Category("Action")]
        [Description("Возникает в момент окончания сканирования.")]
        public event EventHandler AcquireCompleted;

        /// <summary>
        /// Возникает в момент окончания получения изображения приложением.
        /// </summary>
        [Category("Action")]
        [Description("Возникает в момент окончания получения изображения приложением.")]
        public event EventHandler<EndXferEventArgs> EndXfer;

        /// <summary>
        /// Возникает в момент окончания получения изображения источником.
        /// </summary>
        [Category("Action")]
        [Description("Возникает в момент окончания получения изображения источником.")]
        public event EventHandler<XferDoneEventArgs> XferDone;

        /// <summary>
        /// Возникает в момент изменения состояния twain-устройства.
        /// </summary>
        [Category("Action")]
        [Description("Возникает в момент изменения состояния twain-устройства.")]
        public event EventHandler<TwainStateEventArgs> TwainStateChanged;

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
        /// Аргументы события TwainStateChanged.
        /// </summary>
        public sealed class TwainStateEventArgs:EventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса.
            /// </summary>
            /// <param name="flags">Флаги состояния.</param>
            public TwainStateEventArgs(TwainStateFlag flags) {
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

        #endregion

        #region Nested classes

        private sealed class _DsmEntry {

            private _DsmEntry() {
            }

            public static _DsmEntry Create(IntPtr ptr) {
                return new _DsmEntry {
                    DsmParent=(_DSMparent)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSMparent)),
                    DsmIdent=(_DSMident)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSMident)),
                    DsmEntryPoint=(_DSMEntryPoint)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSMEntryPoint)),
                    DsmStatus=(_DSMstatus)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSMstatus)),
                    DSUI=(_DSuserif)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSuserif)),
                    DSEvent=(_DSevent)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSevent)),
                    DSStatus=(_DSstatus)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSstatus)),
                    DSCap=(_DScap)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DScap)),
                    DSImageInfo=(_DSiinf)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSiinf)),
                    DSExtImageInfo=(_DSextiinf)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSextiinf)),
                    DSImageXfer=(_DSixfer)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSixfer)),
                    DSPendingXfer=(_DSpxfer)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSpxfer)),
                    DSImageLayuot=(_DSimagelayuot)Marshal.GetDelegateForFunctionPointer(ptr,typeof(_DSimagelayuot))
                };
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

            public _DSextiinf DSExtImageInfo {
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

            public _DSimagelayuot DSImageLayuot {
                get;
                private set;
            }

            #endregion
        }

        /// <summary>
        /// Точки входа для функций управления памятью.
        /// </summary>
        internal sealed class _Memory {
            private static _TwEntryPoint _entryPoint;

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
            internal static void _SetEntryPoints(_TwEntryPoint entry) {
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

            #endregion
        }

        /// <summary>
        /// Фильтр win32-сообщений.
        /// </summary>
        private sealed class _MessageFilter:IMessageFilter,IDisposable {
            private Twain32 _twain;
            private bool _is_set_filter=false;
            private TwEvent _evtmsg;
            private WINMSG _winmsg;

            public _MessageFilter(Twain32 twain) {
                this._twain=twain;
                this._evtmsg.EventPtr=Marshal.AllocHGlobal(Marshal.SizeOf(this._winmsg));
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
                        break;
                    case TwainCommand.TransferReady:
                        this._twain._TransferPictures();
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
                if(this._twain._srcds.Id==0)
                    return TwainCommand.Not;

                int _pos=GetMessagePos();

                this._winmsg.hwnd=m.HWnd;
                this._winmsg.message=m.Msg;
                this._winmsg.wParam=m.WParam;
                this._winmsg.lParam=m.LParam;
                this._winmsg.time=GetMessageTime();
                this._winmsg.x=(short)_pos;
                this._winmsg.y=(short)(_pos>>16);

                Marshal.StructureToPtr(this._winmsg,this._evtmsg.EventPtr,false);
                this._evtmsg.Message=0;
                TwRC rc=this._twain._dsmEntry.DSEvent(this._twain._appid,this._twain._srcds,TwDG.Control,TwDAT.Event,TwMSG.ProcessEvent,ref this._evtmsg);
                if(rc==TwRC.NotDSEvent)
                    return TwainCommand.Not;
                if(this._evtmsg.Message==TwMSG.XFerReady)
                    return TwainCommand.TransferReady;
                if(this._evtmsg.Message==TwMSG.CloseDSReq)
                    return TwainCommand.CloseRequest;
                if(this._evtmsg.Message==TwMSG.CloseDSOK)
                    return TwainCommand.CloseOk;
                if(this._evtmsg.Message==TwMSG.DeviceEvent)
                    return TwainCommand.DeviceEvent;

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
            private Range(_TwRange range) {
                this.MinValue=Twain32._Convert(range.MinValue,range.ItemType);
                this.MaxValue=Twain32._Convert(range.MaxValue,range.ItemType);
                this.StepSize=Twain32._Convert(range.StepSize,range.ItemType);
                this.CurrentValue=Twain32._Convert(range.CurrentValue,range.ItemType);
                this.DefaultValue=Twain32._Convert(range.DefaultValue,range.ItemType);
            }

            /// <summary>
            /// Создает и возвращает экземпляр <see cref="Range"/>.
            /// </summary>
            /// <param name="range">Экземпляр <see cref="_TwRange"/>.</param>
            /// <returns>Экземпляр <see cref="Range"/>.</returns>
            internal static Range CreateRange(_TwRange range) {
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
            /// Конвертирует экземпляр класса в экземпляр <see cref="_TwRange"/>.
            /// </summary>
            /// <returns>Экземпляр <see cref="_TwRange"/>.</returns>
            internal _TwRange ToTwRange() {
                return new _TwRange() {
                    ItemType=TwTypeHelper.TypeOf(this.CurrentValue.GetType()),
                    MinValue=Twain32._Convert(this.MinValue),
                    MaxValue=Twain32._Convert(this.MaxValue),
                    StepSize=Twain32._Convert(this.StepSize),
                    DefaultValue=Twain32._Convert(this.DefaultValue),
                    CurrentValue=Twain32._Convert(this.CurrentValue)
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
                    Planar=info.Planar!=0,
                    SamplesPerPixel=info.SamplesPerPixel,
                    XResolution=info.XResolution.ToFloat(),
                    YResolution=info.YResolution.ToFloat()
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
                var _twExtImageInfoSize=Marshal.SizeOf(typeof(TwExtImageInfo));
                var _twInfoSize=Marshal.SizeOf(typeof(TwInfo));
                var _extImageInfo=Marshal.PtrToStructure(ptr,typeof(TwExtImageInfo)) as TwExtImageInfo;
                var _result=new ExtImageInfo();
                for(int i=0; i<_extImageInfo.NumInfos; i++) {
                    using(var _item=Marshal.PtrToStructure((IntPtr)(ptr.ToInt64()+_twExtImageInfoSize+(_twInfoSize*i)),typeof(TwInfo)) as TwInfo) {
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
                    foreach(var _item in this) {
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

        #endregion

        #region Delegates

        #region DSM delegates DAT_ variants

        private delegate TwRC _DSMparent([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr refptr);

        private delegate TwRC _DSMident([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwIdentity idds);

        private delegate TwRC _DSMEntryPoint([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] _TwEntryPoint entry);

        private delegate TwRC _DSMstatus([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwStatus dsmstat);

        #endregion

        #region DS delegates DAT_ variants to DS

        private delegate TwRC _DSuserif([In,Out] TwIdentity origin,[In,Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,TwUserInterface guif);

        private delegate TwRC _DSevent([In,Out] TwIdentity origin,[In,Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,ref TwEvent evt);

        private delegate TwRC _DSstatus([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwStatus dsmstat);

        private delegate TwRC _DScap([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwCapability capa);

        private delegate TwRC _DSiinf([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[Out] TwImageInfo imgInf);

        private delegate TwRC _DSextiinf([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,/*[In,Out] TwExtImageInfo*/ IntPtr extImgInf);

        private delegate TwRC _DSimagelayuot([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwImageLayout imageLayuot);

        private delegate TwRC _DSixfer([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr hbitmap);

        private delegate TwRC _DSpxfer([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwPendingXfers pxfr);

        #endregion

        internal delegate ImageInfo GetImageInfoCallback();

        internal delegate ExtImageInfo GetExtImageInfoCallback(TwEI[] extInfo);

        #endregion
    }
}
