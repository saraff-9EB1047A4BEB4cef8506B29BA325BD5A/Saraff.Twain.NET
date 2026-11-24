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
        #region Events

        /// <summary>
        /// Возникает в момент окончания сканирования. Occurs when the acquire is completed.
        /// </summary>
        [Category("Action")]
        [Description("Возникает в момент окончания сканирования. Occurs when the acquire is completed.")]
        public event EventHandler AcquireCompleted;

        /// <summary>
        /// Возникает в момент получения ошибки в процессе сканирования. Occurs when error received during acquire.
        /// </summary>
        [Category("Action")]
        [Description("Возникает в момент получения ошибки в процессе сканирования. Occurs when error received during acquire.")]
        public event EventHandler<AcquireErrorEventArgs> AcquireError;

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
        [Serializable]
        public sealed class EndXferEventArgs : SerializableCancelEventArgs {
            private readonly _Image _image;

            /// <summary>
            /// Инициализирует новый экземпляр класса.
            /// </summary>
            /// <param name="image">Изображение.</param>
            internal EndXferEventArgs(object image) {
                this._image = image as _Image;
            }

            private EndXferEventArgs(Stream stream) {
                this._image = stream;
            }

            internal static EndXferEventArgs FromStream(Stream stream) => new EndXferEventArgs(stream);

            public T CreateImage<T>(IImageFactory<T> factory) where T : class => factory.Create(this._image);

            /// <summary>
            /// Возвращает поток изображения. Поток принадлежит этому экземпляру и позиционируется в начало.
            /// Gets the image stream. The stream is owned by this event instance and is rewound to the beginning.
            /// </summary>
            public Stream Stream => (Stream)this._image;
        }

        /// <summary>
        /// Аргументы события XferDone.
        /// </summary>
        public sealed class XferDoneEventArgs : SerializableCancelEventArgs {
            private readonly GetImageInfoCallback _imageInfoMethod;
            private readonly GetExtImageInfoCallback _extImageInfoMethod;

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="XferDoneEventArgs"/>.
            /// </summary>
            /// <param name="method1">Метод обратного вызова для получения описания изображения.</param>
            /// <param name="method2">Метод обратного вызова для получения расширенного описания изображения.</param>
            internal XferDoneEventArgs(GetImageInfoCallback method1, GetExtImageInfoCallback method2) {
                this._imageInfoMethod = method1;
                this._extImageInfoMethod = method2;
            }

            /// <summary>
            /// Возвращает описание полученного изображения.
            /// </summary>
            /// <returns>Описание изображения.</returns>
            public ImageInfo GetImageInfo() => this._imageInfoMethod();

            /// <summary>
            /// Возвращает расширенного описание полученного изображения.
            /// </summary>
            /// <param name="extInfo">Набор кодов расширенного описания изображения для которых требуется получить описание.</param>
            /// <returns>Расширенное описание изображения.</returns>
            public ExtImageInfo GetExtImageInfo(params TwEI[] extInfo) => this._extImageInfoMethod(extInfo);
        }

        /// <summary>
        /// Аргументы события SetupMemXferEvent.
        /// </summary>
        [Serializable]
        public sealed class SetupMemXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="SetupMemXferEventArgs"/>.
            /// </summary>
            /// <param name="info">Описание изображения.</param>
            /// <param name="bufferSize">Размер буфера памяти для передачи данных.</param>
            internal SetupMemXferEventArgs(ImageInfo info, uint bufferSize) {
                this.ImageInfo = info;
                this.BufferSize = bufferSize;
            }

            /// <summary>
            /// Возвращает описание изображения.
            /// </summary>
            public ImageInfo ImageInfo { get; private set; }

            /// <summary>
            /// Возвращает размер буфера памяти для передачи данных.
            /// </summary>
            public uint BufferSize { get; private set; }
        }

        /// <summary>
        /// Аргументы события MemXferEvent.
        /// </summary>
        [Serializable]
        public sealed class MemXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="MemXferEventArgs"/>.
            /// </summary>
            /// <param name="info">Описание изображения.</param>
            /// <param name="image">Фрагмент данных изображения.</param>
            internal MemXferEventArgs(ImageInfo info, ImageMemXfer image) {
                this.ImageInfo = info;
                this.ImageMemXfer = image;
            }

            /// <summary>
            /// Возвращает описание изображения.
            /// </summary>
            public ImageInfo ImageInfo { get; private set; }

            /// <summary>
            /// Возвращает фрагмент данных изображения.
            /// </summary>
            public ImageMemXfer ImageMemXfer { get; private set; }
        }

        /// <summary>
        /// Аргументы события SetupFileXferEvent.
        /// </summary>
        [Serializable]
        public sealed class SetupFileXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="SetupFileXferEventArgs"/>.
            /// </summary>
            internal SetupFileXferEventArgs() {
            }

            /// <summary>
            /// Возвращает или устанавливает имя файла изображения.
            /// </summary>
            public string FileName { get; set; }
        }

        /// <summary>
        /// Аргументы события FileXferEvent.
        /// </summary>
        [Serializable]
        public sealed class FileXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="FileXferEventArgs"/>.
            /// </summary>
            /// <param name="image">Описание файла изображения.</param>
            internal FileXferEventArgs(ImageFileXfer image) {
                this.ImageFileXfer = image;
            }

            /// <summary>
            /// Возвращает описание файла изображения.
            /// </summary>
            public ImageFileXfer ImageFileXfer { get; private set; }
        }

        /// <summary>
        /// Аргументы события TwainStateChanged.
        /// </summary>
        [Serializable]
        public sealed class TwainStateEventArgs : EventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса.
            /// </summary>
            /// <param name="flags">Флаги состояния.</param>
            internal TwainStateEventArgs(TwainStateFlag flags) {
                this.TwainState = flags;
            }

            /// <summary>
            /// Возвращает флаги состояния twain-устройства.
            /// </summary>
            public TwainStateFlag TwainState { get; private set; }
        }

        /// <summary>
        /// Аргументы события DeviceEvent.
        /// </summary>
        public sealed class DeviceEventEventArgs : EventArgs {
            private readonly TwDeviceEvent _deviceEvent;

            internal DeviceEventEventArgs(TwDeviceEvent deviceEvent) {
                this._deviceEvent = deviceEvent;
            }

            /// <summary>
            /// One of the TWDE_xxxx values.
            /// </summary>
            public TwDE Event => this._deviceEvent.Event;

            /// <summary>
            /// The name of the device that generated the event.
            /// </summary>
            public string DeviceName => this._deviceEvent.DeviceName;

            /// <summary>
            /// Battery Minutes Remaining.
            /// </summary>
            public uint BatteryMinutes => this._deviceEvent.BatteryMinutes;

            /// <summary>
            /// Battery Percentage Remaining.
            /// </summary>
            public short BatteryPercentAge => this._deviceEvent.BatteryPercentAge;

            /// <summary>
            /// Power Supply.
            /// </summary>
            public int PowerSupply => this._deviceEvent.PowerSupply;

            /// <summary>
            /// Resolution.
            /// </summary>
            public float XResolution => this._deviceEvent.XResolution;

            /// <summary>
            /// Resolution.
            /// </summary>
            public float YResolution => this._deviceEvent.YResolution;

            /// <summary>
            /// Flash Used2.
            /// </summary>
            public uint FlashUsed2 => this._deviceEvent.FlashUsed2;

            /// <summary>
            /// Automatic Capture.
            /// </summary>
            public uint AutomaticCapture => this._deviceEvent.AutomaticCapture;

            /// <summary>
            /// Automatic Capture.
            /// </summary>
            public uint TimeBeforeFirstCapture => this._deviceEvent.TimeBeforeFirstCapture;

            /// <summary>
            /// Automatic Capture.
            /// </summary>
            public uint TimeBetweenCaptures => this._deviceEvent.TimeBetweenCaptures;
        }

        /// <summary>
        /// Аргументы события AcquireError.
        /// </summary>
        [Serializable]
        public sealed class AcquireErrorEventArgs : EventArgs {

            /// <summary>
            /// Инициализирует новый экземпляр класса.
            /// </summary>
            /// <param name="ex">Экземпляр класса исключения.</param>
            internal AcquireErrorEventArgs(TwainException ex) {
                this.Exception = ex;
            }

            /// <summary>
            /// Возвращает экземпляр класса исключения.
            /// </summary>
            public TwainException Exception { get; private set; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="System.EventArgs" />
        [Serializable]
        public class SerializableCancelEventArgs : EventArgs {

            /// <summary>
            /// Получает или задает значение, показывающее, следует ли отменить событие. Gets or sets a value indicating whether the event should be canceled.
            /// </summary>
            /// <value>
            /// Значение <c>true</c>, если событие следует отменить, в противном случае — значение <c>false</c>. <c>true</c> if cancel; otherwise, <c>false</c>.
            /// </value>
            public bool Cancel { get; set; }
        }

        #endregion
    }
}
