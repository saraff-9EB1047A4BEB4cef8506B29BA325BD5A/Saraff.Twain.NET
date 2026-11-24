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

    /// <summary>
    /// Обеспечивает возможность работы с TWAIN-источниками.
    /// </summary>
    /// <remarks>
    /// Все операции сеанса TWAIN должны выполняться в одном потоке, как правило в STA-потоке пользовательского интерфейса.
    /// All TWAIN session operations must be performed on the same thread, normally an STA UI thread.
    /// </remarks>
    [DebuggerDisplay("ProductName = {_appid.ProductName.Value}, Version = {_appid.Version.Info}, DS = {_srcds.ProductName}")]
    [DefaultEvent("AcquireCompleted")]
    [DefaultProperty("AppProductName")]
    public sealed partial class TwainX : Component {
        /// <summary>
        /// Возвращает или устанавливает значение, указывающее на необходимость деактивации источника данных после получения изображения.
        /// </summary>
        [DefaultValue(true)]
        [Category("Behavior")]
        [Description("Возвращает или устанавливает значение, указывающее на необходимость деактивации источника данных после получения изображения.")]
        public bool DisableAfterAcquire { get; set; }

        /// <summary>
        /// Возвращает истину, если DSM поддерживает TWAIN 2.0; иначе лож.
        /// </summary>
        [Browsable(false)]
        private bool IsTwain2Supported {
            get {
                if(!this._session.IsDsmOpen) {
                    throw new InvalidOperationException("DSM is not open.");
                }
                return (this._AppId.SupportedGroups & TwDG.DSM2) != 0;
            }
        }



        #region DG_IMAGE / IMAGExxxxXFER / MSG_GET operation




        #endregion

        #region Raise events

        private void _OnAcquireCompleted(EventArgs e) => TwainEventDispatcher.Raise(this, this.AcquireCompleted, e, nameof(this.AcquireCompleted));

        private void _OnAcquireError(AcquireErrorEventArgs e) => TwainEventDispatcher.Raise(this, this.AcquireError, e, nameof(this.AcquireError));

        private bool _OnXferDone(XferDoneEventArgs e) {
            TwainEventDispatcher.Raise(this, this.XferDone, e, nameof(this.XferDone));
            return e.Cancel;
        }

        private bool _OnEndXfer(EndXferEventArgs e) {
            TwainEventDispatcher.Raise(this, this.EndXfer, e, nameof(this.EndXfer));
            return e.Cancel;
        }

        private bool _OnSetupMemXfer(SetupMemXferEventArgs e) {
            TwainEventDispatcher.Raise(this, this.SetupMemXferEvent, e, nameof(this.SetupMemXferEvent));
            return e.Cancel;
        }

        private bool _OnMemXfer(MemXferEventArgs e) {
            TwainEventDispatcher.Raise(this, this.MemXferEvent, e, nameof(this.MemXferEvent));
            return e.Cancel;
        }

        private bool _OnSetupFileXfer(SetupFileXferEventArgs e) {
            TwainEventDispatcher.Raise(this, this.SetupFileXferEvent, e, nameof(this.SetupFileXferEvent));
            return e.Cancel;
        }

        private bool _OnFileXfer(FileXferEventArgs e) {
            TwainEventDispatcher.Raise(this, this.FileXferEvent, e, nameof(this.FileXferEvent));
            return e.Cancel;
        }

        private void _OnDeviceEvent(DeviceEventEventArgs e) => TwainEventDispatcher.Raise(this, this.DeviceEvent, e, nameof(this.DeviceEvent));

        #endregion

        /// <summary>
        /// Флаги состояния.
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



        #region Delegates

        #region DSM delegates DAT_ variants

        private delegate TwRC _DSMparent([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr refptr);

        private delegate TwRC _DSMraw([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, IntPtr rawData);

        #endregion

        #region DS delegates DAT_ variants to DS

        private delegate TwRC _DSixfer([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr hbitmap);

        private delegate TwRC _DSraw([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, IntPtr arg);

        #endregion

        internal delegate ImageInfo GetImageInfoCallback();

        internal delegate ExtImageInfo GetExtImageInfoCallback(TwEI[] extInfo);

        private delegate void Action<T>(T arg);

        #endregion
    }
}
