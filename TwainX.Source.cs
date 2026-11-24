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
        #region Information of sorces

        /// <summary>
        /// Возвращает или устанавливает индекс текущего источника данных.
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
        public int SourcesCount => this._sources.Length;

        /// <summary>
        /// Возвращает имя источника данных по указанному индексу.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <returns>Имя источника данных.</returns>
        public string GetSourceProductName(int index) => this._sources[index].ProductName;

        /// <summary>
        /// Возвращает описание указанного источника. Gets the source identity.
        /// </summary>
        /// <param name="index">Индекс. The index.</param>
        /// <returns>Описание источника данных.</returns>
        public Identity GetSourceIdentity(int index) => new Identity(this._sources[index]);

        /// <summary>
        /// Возвращает истину, если указанный источник поддерживает TWAIN 2.0; иначе лож.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <returns>Истина, если указанный источник поддерживает TWAIN 2.0; иначе лож.</returns>
        public bool GetIsSourceTwain2Compatible(int index) => (this._sources[index].SupportedGroups & TwDG.DS2) != 0;

        /// <summary>
        /// Устанавливает указанный источник данных в качестве источника данных по умолчанию.
        /// </summary>
        /// <param name="index">Индекс.</param>
        public void SetDefaultSource(int index) {
            if(this._session.IsDsmOpen) {
                if(!this._session.IsDsOpen) {
                    TwIdentity _src = this._sources[index];
                    TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.Set, ref _src);
                    if(_rc != TwRC.Success) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                } else {
                    throw new TwainException("Источник данных уже открыт. Необходимо сперва закрыть источник данных.");
                }
            } else {
                throw new TwainException("DSM не открыт.");
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
                throw new TwainException("Не удалось найти источник данных по умолчанию.");
            } else {
                throw new TwainException("DSM не открыт.");
            }
        }

        #endregion

        #region Properties of source

        /// <summary>
        /// Возвращает идентификатор приложения.
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        private TwIdentity _AppId {
            get {
                if(this._appid == null) {
                    Assembly _asm = typeof(TwainX).Assembly;
                    Version _version = new Version((Attribute.GetCustomAttribute(_asm, typeof(AssemblyFileVersionAttribute), false) as AssemblyFileVersionAttribute)?.Version ?? "1.0.0.0");

                    this._appid = new TwIdentity() {
                        Id = 0,
                        Version = new TwVersion() {
                            MajorNum = (ushort)_version.Major,
                            MinorNum = (ushort)_version.Minor,
                            Language = TwLanguage.RUSSIAN,
                            Country = TwCountry.BELARUS,
                            Info = new AssemblyName(_asm.FullName).Version.ToString()
                        },
                        ProtocolMajor = 2,
                        ProtocolMinor = 4,
                        SupportedGroups = TwDG.Image | TwDG.Control | TwDG.APP2,
                        Manufacturer = (Attribute.GetCustomAttribute(_asm, typeof(AssemblyCompanyAttribute), false) as AssemblyCompanyAttribute)?.Company ?? "SARAFF SOFTWARE",
                        ProductFamily = "TWAIN Class Library",
                        ProductName = (Attribute.GetCustomAttribute(_asm, typeof(AssemblyProductAttribute), false) as AssemblyProductAttribute)?.Product ?? "Saraff.TwainX"
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
        /// Возвращает или устанавливает имя приложения.
        /// </summary>
        [Category("Behavior")]
        [Description("Возвращает или устанавливает имя приложения.")]
        public string AppProductName {
            get {
                return this._AppId.ProductName;
            }
            set {
                this._AppId.ProductName = value;
            }
        }

        /// <summary>
        /// Возвращает или устанавливает значение указывающие на необходимость отображения UI TWAIN-источника.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Возвращает или устанавливает значение указывающие на необходимость отображения UI TWAIN-источника.")]
        public bool ShowUI { get; set; }

        /// <summary>
        /// Возвращает или устанавливает явный путь к библиотеке менеджера источников данных TWAIN.
        /// Gets or sets an explicit path to the TWAIN data source manager library.
        /// </summary>
        /// <remarks>
        /// Если значение не задано, используется расположение по умолчанию для текущей платформы.
        /// If not set, the default platform-specific location is used.
        /// </remarks>
        [Category("Behavior")]
        [DefaultValue(null)]
        [Description("Возвращает или устанавливает явный путь к библиотеке менеджера источников данных TWAIN. Gets or sets an explicit path to the TWAIN data source manager library.")]
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
        public IntPtr Parent { get; set; }

        /// <summary>
        /// Возвращает или устанавливает используемый приложением язык. Get or set the primary language for your application.
        /// </summary>
        [Category("Culture")]
        [DefaultValue(TwLanguage.RUSSIAN)]
        [Description("Возвращает или устанавливает используемый приложением язык. Get or set the primary language for your application.")]
        public TwLanguage Language {
            get {
                return this._AppId.Version.Language;
            }
            set {
                this._AppId.Version.Language = value;
            }
        }

        /// <summary>
        /// Возвращает или устанавливает страну происхождения приложения. Get or set the primary country where your application is intended to be distributed.
        /// </summary>
        [Category("Culture")]
        [DefaultValue(TwCountry.BELARUS)]
        [Description("Возвращает или устанавливает страну происхождения приложения. Get or set the primary country where your application is intended to be distributed.")]
        public TwCountry Country {
            get {
                return this._AppId.Version.Country;
            }
            set {
                this._AppId.Version.Country = value;
            }
        }

        /// <summary>
        /// Возвращает или устанавливает кадр физического расположения изображения.
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
        /// Возвращает набор возможностей (Capabilities).
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
        /// Возвращает набор операций для работы с цветовой палитрой.
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public TwainPalette Palette { get; private set; }

        #endregion
    }
}
