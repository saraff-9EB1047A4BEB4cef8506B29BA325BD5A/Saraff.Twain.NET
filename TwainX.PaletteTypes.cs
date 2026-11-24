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
    }
}
