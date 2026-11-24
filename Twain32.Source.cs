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
        #region Information of sorces

        /// <summary>
        /// Gets or sets the index of the current data source.
        /// <para xml:lang="ru">Возвращает или устанавливает индекс текущего источника данных.</para>
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public int SourceIndex {
            get {
                if(this._session.IsDsmOpen) {
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
                if(this._session.IsDsmOpen) {
                    if(!this._session.IsDsOpen) {
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
            if(this._session.IsDsmOpen) {
                if(!this._session.IsDsOpen) {
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
            if(this._session.IsDsmOpen) {
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

        /// <summary>
        /// Gets or sets an explicit path to the TWAIN data source manager library.
        /// <para xml:lang="ru">Возвращает или устанавливает явный путь к библиотеке менеджера источников данных TWAIN.</para>
        /// </summary>
        /// <remarks>
        /// If not set, the default platform-specific location is used.
        /// <para xml:lang="ru">Если значение не задано, используется расположение по умолчанию для текущей платформы.</para>
        /// </remarks>
        [Category("Behavior")]
        [DefaultValue(null)]
        [Description("Gets or sets an explicit path to the TWAIN data source manager library. Возвращает или устанавливает явный путь к библиотеке менеджера источников данных TWAIN.")]
        public string DsmPath { get; set; }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ModalUI { get; set; }

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
    }
}
