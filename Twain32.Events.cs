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
        #region Events

        /// <summary>
        /// Occurs when the acquire is completed.
        /// <para xml:lang="ru">Возникает в момент окончания сканирования.</para>
        /// </summary>
        [Category("Action")]
        [Description("Occurs when the acquire is completed. Возникает в момент окончания сканирования.")]
        public event EventHandler AcquireCompleted;

        /// <summary>
        /// Occurs when error received during acquire.
        /// <para xml:lang="ru">Возникает в момент получения ошибки в процессе сканирования.</para>
        /// </summary>
        [Category("Action")]
        [Description("Occurs when error received during acquire. Возникает в момент получения ошибки в процессе сканирования.")]
        public event EventHandler<AcquireErrorEventArgs> AcquireError;

        /// <summary>
        /// Occurs when the transfer into application was completed (Native Mode Transfer).
        /// <para xml:lang="ru">Возникает в момент окончания получения изображения приложением.</para>
        /// </summary>
        [Category("Native Mode Action")]
        [Description("Occurs when the transfer into application was completed (Native Mode Transfer). Возникает в момент окончания получения изображения приложением.")]
        public event EventHandler<EndXferEventArgs> EndXfer;

        /// <summary>
        /// Occurs when the transfer was completed.
        /// <para xml:lang="ru">Возникает в момент окончания получения изображения источником.</para>
        /// </summary>
        [Category("Action")]
        [Description("Occurs when the transfer was completed. Возникает в момент окончания получения изображения источником.")]
        public event EventHandler<XferDoneEventArgs> XferDone;

        /// <summary>
        /// Occurs when determined size of buffer to use during the transfer (Memory Mode Transfer and MemFile Mode Transfer).
        /// <para xml:lang="ru">Возникает в момент установки размера буфера памяти.</para>
        /// </summary>
        [Category("Memory Mode Action")]
        [Description("Occurs when determined size of buffer to use during the transfer (Memory Mode Transfer and MemFile Mode Transfer). Возникает в момент установки размера буфера памяти.")]
        public event EventHandler<SetupMemXferEventArgs> SetupMemXferEvent;

        /// <summary>
        /// Occurs when the memory block for the data was recived (Memory Mode Transfer and MemFile Mode Transfer).
        /// <para xml:lang="ru">Возникает в момент получения очередного блока данных.</para>
        /// </summary>
        [Category("Memory Mode Action")]
        [Description("Occurs when the memory block for the data was recived (Memory Mode Transfer and MemFile Mode Transfer). Возникает в момент получения очередного блока данных.")]
        public event EventHandler<MemXferEventArgs> MemXferEvent;

        /// <summary>
        /// Occurs when you need to specify the filename (File Mode Transfer).
        /// <para xml:lang="ru">Возникает в момент, когда необходимо задать имя файла изображения.</para>
        /// </summary>
        [Category("File Mode Action")]
        [Description("Occurs when you need to specify the filename. (File Mode Transfer) Возникает в момент, когда необходимо задать имя файла изображения.")]
        public event EventHandler<SetupFileXferEventArgs> SetupFileXferEvent;

        /// <summary>
        /// Occurs when the transfer into application was completed (File Mode Transfer).
        /// <para xml:lang="ru">Возникает в момент окончания получения файла изображения приложением.</para>
        /// </summary>
        [Category("File Mode Action")]
        [Description("Occurs when the transfer into application was completed (File Mode Transfer). Возникает в момент окончания получения файла изображения приложением.")]
        public event EventHandler<FileXferEventArgs> FileXferEvent;

        /// <summary>
        /// Occurs when TWAIN state was changed.
        /// <para xml:lang="ru">Возникает в момент изменения состояния twain-устройства.</para>
        /// </summary>
        [Category("Behavior")]
        [Description("Occurs when TWAIN state was changed. Возникает в момент изменения состояния twain-устройства.")]
        public event EventHandler<TwainStateEventArgs> TwainStateChanged;

        /// <summary>
        /// Occurs when enabled the source sends this message to the Application to alert it that some event has taken place.
        /// <para xml:lang="ru">Возникает в момент, когда источник уведомляет приложение о произошедшем событии.</para>
        /// </summary>
        [Category("Behavior")]
        [Description("Occurs when enabled the source sends this message to the Application to alert it that some event has taken place. Возникает в момент, когда источник уведомляет приложение о произошедшем событии.")]
        public event EventHandler<DeviceEventEventArgs> DeviceEvent;

        #endregion

        #region Events Args

        /// <summary>
        /// Arguments for the EndXfer event.
        /// <para xml:lang="ru">Аргументы события EndXfer.</para>
        /// </summary>
        [Serializable]
        public sealed class EndXferEventArgs : SerializableCancelEventArgs {
            private readonly _Image _image;

            /// <summary>
            /// Initializes a new instance of the class.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса.</para>
            /// </summary>
            /// <param name="image">Image.<para xml:lang="ru">Изображение.</para></param>
            internal EndXferEventArgs(object image) {
                this._image = image as _Image;
            }

            public T CreateImage<T>(IImageFactory<T> factory) where T : class => factory.Create(this._image);

            /// <summary>
            /// Returns the image.
            /// <para xml:lang="ru">Возвращает изображение.</para>
            /// </summary>
            public Image Image => this._image;

#if !NET2            
            /// <summary>
            /// Returns the image.
            /// <para xml:lang="ru">Возвращает изображение.</para>
            /// </summary>
            public System.Windows.Media.ImageSource ImageSource => this._image;
#endif
        }

        /// <summary>
        /// Arguments for the XferDone event.
        /// <para xml:lang="ru">Аргументы события XferDone.</para>
        /// </summary>
        public sealed class XferDoneEventArgs : SerializableCancelEventArgs {
            private readonly GetImageInfoCallback _imageInfoMethod;
            private readonly GetExtImageInfoCallback _extImageInfoMethod;

            /// <summary>
            /// Initializes a new instance of the class <see cref="XferDoneEventArgs"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="XferDoneEventArgs"/>.</para>
            /// </summary>
            /// <param name="method1">Callback method to get image description.<para xml:lang="ru">Метод обратного вызова для получения описания изображения.</para></param>
            /// <param name="method2">Callback method to get an extended image description.<para xml:lang="ru">Метод обратного вызова для получения расширенного описания изображения.</para></param>
            internal XferDoneEventArgs(GetImageInfoCallback method1, GetExtImageInfoCallback method2) {
                this._imageInfoMethod = method1;
                this._extImageInfoMethod = method2;
            }

            /// <summary>
            /// Returns a description of the received image.
            /// <para xml:lang="ru">Возвращает описание полученного изображения.</para>
            /// </summary>
            /// <returns>Description of the image.<para xml:lang="ru">Описание изображения.</para></returns>
            public ImageInfo GetImageInfo() => this._imageInfoMethod();

            /// <summary>
            /// Returns an extended description of the resulting image.
            /// <para xml:lang="ru">Возвращает расширенного описание полученного изображения.</para>
            /// </summary>
            /// <param name="extInfo">A set of codes for the extended image description for which you want to get a description.<para xml:lang="ru">Набор кодов расширенного описания изображения для которых требуется получить описание.</para></param>
            /// <returns>Extended image description.<para xml:lang="ru">Расширенное описание изображения.</para></returns>
            public ExtImageInfo GetExtImageInfo(params TwEI[] extInfo) => this._extImageInfoMethod(extInfo);
        }

        /// <summary>
        /// Arguments for the SetupMemXferEvent event.
        /// <para xml:lang="ru">Аргументы события SetupMemXferEvent.</para>
        /// </summary>
        [Serializable]
        public sealed class SetupMemXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="SetupMemXferEventArgs"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="SetupMemXferEventArgs"/>.</para>
            /// </summary>
            /// <param name="info">Description of the image.<para xml:lang="ru">Описание изображения.</para></param>
            /// <param name="bufferSize">The size of the memory buffer for data transfer.<para xml:lang="ru">Размер буфера памяти для передачи данных.</para></param>
            internal SetupMemXferEventArgs(ImageInfo info, uint bufferSize) {
                this.ImageInfo = info;
                this.BufferSize = bufferSize;
            }

            /// <summary>
            /// Returns a description of the image.
            /// <para xml:lang="ru">Возвращает описание изображения.</para>
            /// </summary>
            public ImageInfo ImageInfo { get; private set; }

            /// <summary>
            /// Gets the size of the memory buffer for data transfer.
            /// <para xml:lang="ru">Возвращает размер буфера памяти для передачи данных.</para>
            /// </summary>
            public uint BufferSize { get; private set; }
        }

        /// <summary>
        /// Arguments for the MemXferEvent event.
        /// <para xml:lang="ru">Аргументы события MemXferEvent.</para>
        /// </summary>
        [Serializable]
        public sealed class MemXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="MemXferEventArgs"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="MemXferEventArgs"/>.</para>
            /// </summary>
            /// <param name="info">Description of the image.<para xml:lang="ru">Описание изображения.</para></param>
            /// <param name="image">A fragment of image data.<para xml:lang="ru">Фрагмент данных изображения.</para></param>
            internal MemXferEventArgs(ImageInfo info, ImageMemXfer image) {
                this.ImageInfo = info;
                this.ImageMemXfer = image;
            }

            /// <summary>
            /// Returns a description of the image.
            /// <para xml:lang="ru">Возвращает описание изображения.</para>
            /// </summary>
            public ImageInfo ImageInfo { get; private set; }

            /// <summary>
            /// Returns a piece of image data.
            /// <para xml:lang="ru">Возвращает фрагмент данных изображения.</para>
            /// </summary>
            public ImageMemXfer ImageMemXfer { get; private set; }
        }

        /// <summary>
        /// Arguments for the SetupFileXferEvent event.
        /// <para xml:lang="ru">Аргументы события SetupFileXferEvent.</para>
        /// </summary>
        [Serializable]
        public sealed class SetupFileXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="SetupFileXferEventArgs"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="SetupFileXferEventArgs"/>.</para>
            /// </summary>
            internal SetupFileXferEventArgs() {
            }

            /// <summary>
            /// Gets or sets the name of the image file.
            /// <para xml:lang="ru">Возвращает или устанавливает имя файла изображения.</para>
            /// </summary>
            public string FileName { get; set; }
        }

        /// <summary>
        /// Arguments for the FileXferEvent event.
        /// <para xml:lang="ru">Аргументы события FileXferEvent.</para>
        /// </summary>
        [Serializable]
        public sealed class FileXferEventArgs : SerializableCancelEventArgs {

            /// <summary>
            /// Initializes a new instance of the class <see cref="FileXferEventArgs"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="FileXferEventArgs"/>.</para>
            /// </summary>
            /// <param name="image">Description of the image file.<para xml:lang="ru">Описание файла изображения.</para></param>
            internal FileXferEventArgs(ImageFileXfer image) {
                this.ImageFileXfer = image;
            }

            /// <summary>
            /// Returns a description of the image file.
            /// <para xml:lang="ru">Возвращает описание файла изображения.</para>
            /// </summary>
            public ImageFileXfer ImageFileXfer { get; private set; }
        }

        /// <summary>
        /// Arguments for the TwainStateChanged event.
        /// <para xml:lang="ru">Аргументы события TwainStateChanged.</para>
        /// </summary>
        [Serializable]
        public sealed class TwainStateEventArgs : EventArgs {

            /// <summary>
            /// Initializes a new instance of the class.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса.</para>
            /// </summary>
            /// <param name="flags">State flags.<para xml:lang="ru">Флаги состояния.</para></param>
            internal TwainStateEventArgs(TwainStateFlag flags) {
                this.TwainState = flags;
            }

            /// <summary>
            /// Returns the status flags of a twain device.
            /// <para xml:lang="ru">Возвращает флаги состояния twain-устройства.</para>
            /// </summary>
            public TwainStateFlag TwainState { get; private set; }
        }

        /// <summary>
        /// Arguments for the DeviceEvent event.
        /// <para xml:lang="ru">Аргументы события DeviceEvent.</para>
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
        /// Arguments for the AcquireError event.
        /// <para xml:lang="ru">Аргументы события AcquireError.</para>
        /// </summary>
        [Serializable]
        public sealed class AcquireErrorEventArgs : EventArgs {

            /// <summary>
            /// Initializes a new instance of the class.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса.</para>
            /// </summary>
            /// <param name="ex">An instance of the exception class.<para xml:lang="ru">Экземпляр класса исключения.</para></param>
            internal AcquireErrorEventArgs(TwainException ex) {
                this.Exception = ex;
            }

            /// <summary>
            /// Gets an instance of the exception class.
            /// <para xml:lang="ru">Возвращает экземпляр класса исключения.</para>
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
            /// Gets or sets a value indicating whether the event should be canceled.
            /// <para xml:lang="ru">Получает или задает значение, показывающее, следует ли отменить событие.</para>
            /// </summary>
            /// <value>
            /// Значение <c>true</c>, если событие следует отменить, в противном случае — значение <c>false</c>.
            /// </value>
            public bool Cancel { get; set; }
        }

        #endregion
    }
}
