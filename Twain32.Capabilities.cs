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
        #region All capabilities

        /// <summary>
        /// Returns flags indicating operations supported by the data source for the specified capability value.
        /// <para xml:lang="ru">Возвращает флаги, указывающие на поддерживаемые источником данных операции, для указанного значения capability.</para>
        /// </summary>
        /// <param name="capability">The value of the TwCap enumeration.<para xml:lang="ru">Значение перечисдения TwCap.</para></param>
        /// <returns>Set of flags.<para xml:lang="ru">Набор флагов.</para></returns>
        /// <exception cref="TwainException">Возбуждается в случае возникновения ошибки во время операции.</exception>
        public TwQC IsCapSupported(TwCap capability) {
            if(this._session.IsDsOpen) {
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
            this._CheckThread();
            if(this._session.IsDsOpen) {
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
            if(this._session.IsDsOpen) {
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
            if(this._session.IsDsOpen) {
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
            this._CheckThread();
            if(this._session.IsDsOpen) {
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
    }
}
