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
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Saraff.Twain {

    /// <summary>
    /// Набор возможностей (Capabilities).
    /// </summary>
    public sealed class TwainCapabilities {

        internal TwainCapabilities(Twain32 twain) {
            MethodInfo _сreateCapability=typeof(TwainCapabilities).GetMethod("CreateCapability",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach(PropertyInfo _prop in typeof(TwainCapabilities).GetProperties()) {
                object[] _attrs=_prop.GetCustomAttributes(typeof(CapabilityAttribute),false);
                if(_attrs.Length>0) {
                    CapabilityAttribute _attr=_attrs[0] as CapabilityAttribute;
                    _prop.SetValue(this,_сreateCapability.MakeGenericMethod(_prop.PropertyType.GetGenericArguments()[0]).Invoke(this,new object[] { twain,_attr.Cap }),null);
                }
            }
        }

        private Capability<T> CreateCapability<T>(Twain32 twain,TwCap cap) where T:struct {
            return Activator.CreateInstance(typeof(Capability<T>),new object[] { twain,cap }) as Capability<T>;
        }

        #region Properties

        /// <summary>
        /// ICAP_XRESOLUTION.
        /// </summary>
        [Capability(TwCap.XResolution)]
        public ICapability<float> XResolution {
            get;
            private set;
        }

        /// <summary>
        /// ICAP_YRESOLUTION.
        /// </summary>
        [Capability(TwCap.YResolution)]
        public ICapability<float> YResolution {
            get;
            private set;
        }

        /// <summary>
        /// ICAP_PIXELTYPE.
        /// </summary>
        [Capability(TwCap.IPixelType)]
        public ICapability<TwPixelType> PixelType {
            get;
            private set;
        }

        /// <summary>
        /// ICAP_UNITS.
        /// </summary>
        [Capability(TwCap.IUnits)]
        public ICapability<TwUnits> Units {
            get;
            private set;
        }

        /// <summary>
        /// ICAP_XFERMECH.
        /// </summary>
        [Capability(TwCap.IXferMech)]
        public ICapability<TwSX> XferMech {
            get;
            private set;
        }

        /// <summary>
        /// ICAP_SUPPORTEDSIZES.
        /// </summary>
        [Capability(TwCap.SupportedSizes)]
        public ICapability<TwSS> SupportedSizes {
            get;
            private set;
        }

        /// <summary>
        /// ICAP_IMAGEFILEFORMAT.
        /// </summary>
        [Capability(TwCap.ImageFileFormat)]
        public ICapability<TwFF> ImageFileFormat {
            get;
            private set;
        }

        #endregion

        private class Capability<T>:ICapability<T> where T:struct {

            public Capability(Twain32 twain,TwCap cap) {
                this._Twain32=twain;
                this._Cap=cap;
            }

            public Twain32.Enumeration Get() {
                Twain32.Enumeration _val=Twain32.Enumeration.FromObject(this._Twain32.GetCap(this._Cap));
                for(int i=0; i<_val.Count; i++) {
                    _val[i]=(T)_val[i];
                }
                return _val;
            }

            public T GetCurrent() {
                return (T)this._Twain32.GetCurrentCap(this._Cap);
            }

            public T GetDefault() {
                return (T)this._Twain32.GetDefaultCap(this._Cap);
            }

            public void Set(T value) {
                if(!this.GetCurrent().Equals(value)) {
                    this._Twain32.SetCap(this._Cap,value);
                }
            }

            public void Reset() {
                this._Twain32.ResetCap(this._Cap);
            }

            public TwQC IsSupported() {
                return this._Twain32.IsCapSupported(this._Cap);
            }

            protected Twain32 _Twain32 {
                get;
                private set;
            }

            protected TwCap _Cap {
                get;
                private set;
            }
        }

        [AttributeUsage(AttributeTargets.Property,AllowMultiple=false,Inherited=false)]
        private sealed class CapabilityAttribute:Attribute {

            public CapabilityAttribute(TwCap cap) {
                this.Cap=cap;
            }

            public TwCap Cap {
                get;
                private set;
            }
        }
    }

    /// <summary>
    /// Представляет возможность (Capability).
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    public interface ICapability<T> where T:struct {

        /// <summary>
        /// Возвращает значения указанной возможности (capability).
        /// </summary>
        /// <returns>Значения указанной возможности (capability).</returns>
        Twain32.Enumeration Get();

        /// <summary>
        /// Возвращает текущее значение указанной возможности (capability).
        /// </summary>
        /// <returns>Текущее значение указанной возможности (capability).</returns>
        T GetCurrent();

        /// <summary>
        /// Возвращает значение по умолчанию указанной возможности (capability).
        /// </summary>
        /// <returns>Значение по умолчанию указанной возможности (capability).</returns>
        T GetDefault();

        /// <summary>
        /// Устанавливает текущее значение указанной возможности (capability).
        /// </summary>
        /// <param name="value">Значение.</param>
        void Set(T value);

        /// <summary>
        /// Сбрасывает текущее значение указанной возможности (capability) в значение по умолчанию.
        /// </summary>
        void Reset();

        /// <summary>
        /// Возвращает набор флагов поддерживаемых операций.
        /// </summary>
        /// <returns>Набор флагов поддерживаемых операций.</returns>
        TwQC IsSupported();
    }
}
