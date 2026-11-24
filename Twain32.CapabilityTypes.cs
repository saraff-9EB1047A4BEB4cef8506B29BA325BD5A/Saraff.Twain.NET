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
    }
}
