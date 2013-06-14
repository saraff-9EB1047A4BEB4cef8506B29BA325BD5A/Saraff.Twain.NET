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
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;
using System.Windows.Forms;
using System.ComponentModel;
using System.Reflection;
using System.Drawing;

namespace Saraff.Twain {

    /// <summary>
    /// Обеспечивает возможность работы с TWAIN-источниками.
    /// </summary>
    [ToolboxBitmap(typeof(Twain32),"Resources.scanner.bmp")]
    [DefaultEvent("AcquireCompleted")]
    [DefaultProperty("AppProductName")]
    public sealed class Twain32:Component {
        #region Twain Entry Points
        private _DSMparent _DsmParent;
        private _DSMident _DsmIdent;
        private _DSMstatus _DsmStatus;
        private _DSuserif _DsUI;
        private _DSevent _DsEvent;
        private _DSstatus _DsStatus;
        private _DScap _DsCap;
        private _DSiinf _DsImageInfo;
        private _DSixfer _DsImageXfer;
        private _DSpxfer _DsPendingXfer;
        private _DSimagelayuot _DsImageLayuot;
        #endregion
        private IntPtr _hTwainDll; //дескриптор модуля twain_32.dll
        private IContainer _components=new Container();
        private IntPtr _hwnd; //дескриптор родительского окна.
        private TwIdentity _appid; //идентификатор приложения.
        private TwIdentity _srcds; //идентификатор текущего источника данных.
        private TwEvent _evtmsg;
        private WINMSG _winmsg;
        private _MessageFilter _filter; //фильтр событий WIN32
        private TwIdentity[] _sorces; //массив доступных источников данных.
        private ApplicationContext _context=null; //контекст приложения. используется в случае отсутствия основного цикла обработки сообщений.

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
                Id=IntPtr.Zero,
                Version=new TwVersion() {
                    MajorNum=(short)_version.Major,
                    MinorNum=(short)_version.Minor,
                    Language=TwLanguage.USA,
                    Country=TwCountry.USA,
                    Info=_asm_name.Version.ToString()
                },
                ProtocolMajor=TwProtocol.Major,
                ProtocolMinor=TwProtocol.Minor,
                SupportedGroups=(int)(TwDG.Image|TwDG.Control),
                Manufacturer=((AssemblyCompanyAttribute)_asm.GetCustomAttributes(typeof(AssemblyCompanyAttribute),false)[0]).Company,
                ProductFamily="TWAIN Class Library",
                ProductName=((AssemblyProductAttribute)_asm.GetCustomAttributes(typeof(AssemblyProductAttribute),false)[0]).Product
            };

            this._srcds=new TwIdentity();
            this._srcds.Id=IntPtr.Zero;
            this._evtmsg.EventPtr=Marshal.AllocHGlobal(Marshal.SizeOf(this._winmsg));
            this._filter=new _MessageFilter(this);
            this.ShowUI=true;
            this.DisableAfterAcquire=true;

            #region Загружаем twain_32.dll, получаем адрес точки входа DSM_Entry и приводим ее к соответствующим делегатам

            this._hTwainDll=LoadLibrary(string.Format("{0}\\..\\twain_32.dll",Environment.SystemDirectory));
            if(this._hTwainDll!=IntPtr.Zero) {
                IntPtr _pDsmEntry=GetProcAddress(this._hTwainDll,1);
                if(_pDsmEntry!=IntPtr.Zero) {
                    this._DsmParent=(_DSMparent)Marshal.GetDelegateForFunctionPointer(_pDsmEntry,typeof(_DSMparent));
                    this._DsmIdent=(_DSMident)Marshal.GetDelegateForFunctionPointer(_pDsmEntry,typeof(_DSMident));
                    this._DsmStatus=(_DSMstatus)Marshal.GetDelegateForFunctionPointer(_pDsmEntry,typeof(_DSMstatus));
                    this._DsUI=(_DSuserif)Marshal.GetDelegateForFunctionPointer(_pDsmEntry,typeof(_DSuserif));
                    this._DsEvent=(_DSevent)Marshal.GetDelegateForFunctionPointer(_pDsmEntry,typeof(_DSevent));
                    this._DsStatus=(_DSstatus)Marshal.GetDelegateForFunctionPointer(_pDsmEntry,typeof(_DSstatus));
                    this._DsCap=(_DScap)Marshal.GetDelegateForFunctionPointer(_pDsmEntry,typeof(_DScap));
                    this._DsImageInfo=(_DSiinf)Marshal.GetDelegateForFunctionPointer(_pDsmEntry,typeof(_DSiinf));
                    this._DsImageXfer=(_DSixfer)Marshal.GetDelegateForFunctionPointer(_pDsmEntry,typeof(_DSixfer));
                    this._DsPendingXfer=(_DSpxfer)Marshal.GetDelegateForFunctionPointer(_pDsmEntry,typeof(_DSpxfer));
                    this._DsImageLayuot=(_DSimagelayuot)Marshal.GetDelegateForFunctionPointer(_pDsmEntry,typeof(_DSimagelayuot));
                } else {
                    throw new TwainException("Cann't find DSM_Entry entry point.");
                }
            } else {
                throw new TwainException("Cann't load library twain_32.dll");
            }

            #endregion
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
                if(this._evtmsg.EventPtr!=IntPtr.Zero) {
                    Marshal.FreeHGlobal(this._evtmsg.EventPtr);
                    this._evtmsg.EventPtr=IntPtr.Zero;
                }
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
                if(this.Parent!=null) {
                    this._hwnd=this.Parent.Handle;
                }
                TwRC _rc=_DsmParent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Parent,TwMSG.OpenDSM,ref this._hwnd);
                if(_rc==TwRC.Success) {
                    this._TwainState|=TwainStateFlag.DSMOpen;
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
                TwRC _rc=this._DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.UserSelect,_src);
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
                TwRC _rc=this._DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.OpenDS,this._srcds);
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
                TwRC _rc=this._DsUI(this._appid,this._srcds,TwDG.Control,TwDAT.UserInterface,TwMSG.EnableDS,_guif);
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
                _rc=this._DsUI(this._appid,this._srcds,TwDG.Control,TwDAT.UserInterface,TwMSG.DisableDS,_guif);
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
                _rc=this._DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.CloseDS,this._srcds);
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
                _rc=_DsmParent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Parent,TwMSG.CloseDSM,ref this._hwnd);
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
            return this._filter.GetImage(index);
        }

        /// <summary>
        /// Возвращает количество отсканированных изображений.
        /// </summary>
        public int ImageCount {
            get {
                return this._filter.ImageCount;
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
                    for(i=0;i<this._sorces.Length;i++) {
                        if(this._sorces[i].Equals(this._srcds)) {
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
                        this._srcds=this._sorces[value];
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
                return this._sorces.Length;
            }
        }

        /// <summary>
        /// Возвращает имя источника данных по указанному индексу.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <returns>Имя источника данных.</returns>
        public string GetSourceProductName(int index) {
            return this._sorces[index].ProductName;
        }

        #endregion

        #region Properties of sorce

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

        /// <summary>
        /// Возвращает или устанавливает кадр физического расположения изображения.
        /// </summary>
        public RectangleF ImageLayout {
            get {
                var _imageLayout=new TwImageLayout();
                var _rc=this._DsImageLayuot(this._appid,this._srcds,TwDG.Image,TwDAT.ImageLayout,TwMSG.Get,_imageLayout);
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
                var _rc=this._DsImageLayuot(this._appid,this._srcds,TwDG.Image,TwDAT.ImageLayout,TwMSG.Set,_imageLayout);
                if(_rc!=TwRC.Success) {
                    throw new TwainException(this._GetTwainStatus());
                }
            }
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
                    TwRC _rc=this._DsCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.QuerySupport,_cap);
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
                    TwRC _rc=this._DsCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Get,_cap);
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
                using(TwCapability _cap=new TwCapability(capability,Twain32._Convert(value),Twain32._Convert(value.GetType()))) {
                    TwRC _rc=this._DsCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Set,_cap);
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
                        ItemType=Twain32._Convert(capabilityValue[0].GetType()), NumItems=capabilityValue.Length
                    },
                    capabilityValue)) {

                    TwRC _rc=this._DsCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Set,_cap);
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
                    TwRC _rc=this._DsCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Set,_cap);
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
                        ItemType=Twain32._Convert(capabilityValue.Items[0].GetType()),
                        NumItems=capabilityValue.Count,
                        CurrentIndex=capabilityValue.CurrentIndex,
                        DefaultIndex=capabilityValue.DefaultIndex
                    },
                    capabilityValue.Items)) {

                    TwRC _rc=this._DsCap(this._appid,this._srcds,TwDG.Control,TwDAT.Capability,TwMSG.Set,_cap);
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
        /// Конвертирует экземпляр <see cref="System.Type"/> в значение перечисления <see cref="TwType"/>.
        /// </summary>
        /// <param name="type">Экземпляр <see cref="System.Type"/>.</param>
        /// <returns>Значение перечисления <see cref="TwType"/></returns>
        private static TwType _Convert(Type type) {
            if(type==typeof(byte)) {
                return TwType.UInt8;
            }
            if(type==typeof(ushort)) {
                return TwType.UInt16;
            }
            if(type==typeof(uint)) {
                return TwType.UInt32;
            }
            if(type==typeof(sbyte)) {
                return TwType.Int8;
            }
            if(type==typeof(short)) {
                return TwType.Int16;
            }
            if(type==typeof(int)) {
                return TwType.Int32;
            }
            if(type==typeof(float)) {
                return TwType.Fix32;
            }
            if(type==typeof(bool)) {
                return TwType.Bool;
            }
            return 0;
        }

        /// <summary>
        /// Конвертирует указанный экземпляр в экземпляр Int32.
        /// </summary>
        /// <param name="item">Конвертируемый экземпляр.</param>
        /// <returns>Экземпляр Int32.</returns>
        private static int _Convert(object item) {
            switch(Twain32._Convert(item.GetType())){
                case TwType.Bool:
                    return (bool)item?1:0;
                case TwType.Fix32:
                    return TwFix32.FromFloat((float)item).ToInt32();
                default:
                    return Convert.ToInt32(item);
            }
        }

        #endregion

        private ArrayList _TransferPictures() {
            ArrayList pics=new ArrayList();
            if(this._srcds.Id==IntPtr.Zero)
                return pics;

            TwRC rc;
            IntPtr _hBitmap=IntPtr.Zero;
            TwPendingXfers pxfr=new TwPendingXfers();

            do {
                pxfr.Count=0;
                _hBitmap=IntPtr.Zero;

                TwImageInfo iinf=new TwImageInfo();
                rc=this._DsImageInfo(this._appid,this._srcds,TwDG.Image,TwDAT.ImageInfo,TwMSG.Get,iinf);
                if(rc!=TwRC.Success) {
                    this.CloseDataSource();
                    return pics;
                }

                rc=_DsImageXfer(this._appid,this._srcds,TwDG.Image,TwDAT.ImageNativeXfer,TwMSG.Get,ref _hBitmap);
                if(rc!=TwRC.XferDone) {
                    this.CloseDataSource();
                    return pics;
                }

                rc=_DsPendingXfer(this._appid,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.EndXfer,pxfr);
                if(rc!=TwRC.Success) {
                    this.CloseDataSource();
                    return pics;
                }

                IntPtr _pBitmap=GlobalLock(_hBitmap);
                try {
                    pics.Add(_pBitmap);
                } finally {
                    GlobalUnlock(_hBitmap);
                }
            }
            while(pxfr.Count!=0);

            rc=_DsPendingXfer(this._appid,this._srcds,TwDG.Control,TwDAT.PendingXfers,TwMSG.Reset,pxfr);
            return pics;
        }

        private TwainCommand _PassMessage(ref Message m) {
            if(this._srcds.Id==IntPtr.Zero)
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
            TwRC rc=this._DsEvent(this._appid,this._srcds,TwDG.Control,TwDAT.Event,TwMSG.ProcessEvent,ref this._evtmsg);
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

        /// <summary>
        /// Получает описание всех доступных источников данных.
        /// </summary>
        private void _GetAllSorces() {
            List<TwIdentity> _src=new List<TwIdentity>();
            TwIdentity _item=new TwIdentity();
            TwRC _rc=this._DsmIdent(this._appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.GetFirst, _item);
            if(_rc==TwRC.Success) {
                _src.Add(_item);
                do {
                    _item=new TwIdentity();
                    _rc=this._DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.GetNext,_item);
                    if(_rc==TwRC.Success) {
                        _src.Add(_item);
                    }
                } while(_rc!=TwRC.EndOfList);
                _rc=this._DsmIdent(this._appid,IntPtr.Zero,TwDG.Control,TwDAT.Identity,TwMSG.GetDefault,this._srcds);
            } else {
                TwCC _state=this._GetTwainStatus();
            }
            this._sorces=_src.ToArray();
        }

        /// <summary>
        /// Возвращает или устанавливает значение флагов состояния.
        /// </summary>
        private TwainStateFlag _TwainState {
            get;
            set;
        }

        /// <summary>
        /// Возвращает код состояния TWAIN.
        /// </summary>
        /// <returns></returns>
        private TwCC _GetTwainStatus() {
            TwStatus _status=new TwStatus();
            TwRC _rc=this._DsStatus(this._appid,this._srcds,TwDG.Control,TwDAT.Status,TwMSG.Get,_status);
            return _status.ConditionCode;
        }

        #region import kernel32.dll

        [DllImport("kernel32.dll",ExactSpelling=true)]
        internal static extern IntPtr GlobalAlloc(int flags,int size);

        [DllImport("kernel32.dll",ExactSpelling=true)]
        internal static extern IntPtr GlobalLock(IntPtr handle);

        [DllImport("kernel32.dll",ExactSpelling=true)]
        internal static extern bool GlobalUnlock(IntPtr handle);

        [DllImport("kernel32.dll",ExactSpelling=true)]
        internal static extern IntPtr GlobalFree(IntPtr handle);

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
        /// Фильтр win32-сообщений.
        /// </summary>
        private sealed class _MessageFilter:IMessageFilter {
            private Twain32 _twain;
            private bool _is_set_filter=false;
            private List<Image> _images=new List<Image>();

            public _MessageFilter(Twain32 twain) {
                this._twain=twain;
            }

            public Image GetImage(int index) {
                return this._images[index];
            }

            public int ImageCount {
                get {
                    return this._images.Count;
                }
            }

            public bool PreFilterMessage(ref Message m) {
                TwainCommand _cmd=this._twain._PassMessage(ref m);
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
                        ArrayList _pics=this._twain._TransferPictures();
                        this._images.Clear();
                        for(int i=0;i<_pics.Count;i++) {
                            this._images.Add(DibToImage.WithScan0((IntPtr)_pics[i]));
                            IntPtr _res=GlobalFree((IntPtr)_pics[i]);
                        }
                        _end();
                        this._twain._OnAcquireCompleted(new EventArgs());
                        break;
                }
                return true;
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

        /// <summary>
        /// Флаги состояния.
        /// </summary>
        [Flags]
        private enum TwainStateFlag {
            DSMOpen=0x1,
            DSOpen=0x2,
            DSEnabled=0x4
        }

        /// <summary>
        /// Возникает в момент окончания сканирования.
        /// </summary>
        [Category("Action")]
        [Description("Возникает в момент окончания сканирования.")]
        public event EventHandler AcquireCompleted;

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
                    ItemType=Twain32._Convert(this.CurrentValue.GetType()),
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

        #region private delegates

        #region DSM delegates DAT_ variants

        private delegate TwRC _DSMparent([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr refptr);

        private delegate TwRC _DSMident([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwIdentity idds);

        private delegate TwRC _DSMstatus([In,Out] TwIdentity origin,IntPtr zeroptr,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwStatus dsmstat);

        #endregion

        #region DS delegates DAT_ variants to DS

        private delegate TwRC _DSuserif([In,Out] TwIdentity origin,[In,Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,TwUserInterface guif);

        private delegate TwRC _DSevent([In,Out] TwIdentity origin,[In,Out] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,ref TwEvent evt);

        private delegate TwRC _DSstatus([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwStatus dsmstat);

        private delegate TwRC _DScap([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwCapability capa);

        private delegate TwRC _DSiinf([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwImageInfo imginf);

        private delegate TwRC _DSimagelayuot([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwImageLayout imageLayuot);

        private delegate TwRC _DSixfer([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,ref IntPtr hbitmap);

        private delegate TwRC _DSpxfer([In,Out] TwIdentity origin,[In] TwIdentity dest,TwDG dg,TwDAT dat,TwMSG msg,[In,Out] TwPendingXfers pxfr);

        #endregion

        #endregion
    }
}
