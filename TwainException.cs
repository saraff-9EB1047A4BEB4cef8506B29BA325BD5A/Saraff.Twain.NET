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
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Security.Permissions;

namespace Saraff.Twain {

    /// <summary>
    /// Exception class <see cref="TwainException"/>
    /// <para xml:lang="ru">Класс исключения <see cref="TwainException"/></para>
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{Message}; ReturnCode = {ReturnCode}; ConditionCode = {ConditionCode}")]
    public sealed class TwainException:Exception {

        /// <summary>
        /// Initializes a new instance of the <see cref="TwainException"/> class.
        /// </summary>
        /// <param name="cc">The condition code.</param>
        /// <param name="rc">The return code.</param>
        internal TwainException(TwCC cc,TwRC rc):this(TwainException._CodeToMessage(cc)) {
            this.ConditionCode=cc;
            this.ReturnCode=rc;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwainException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        internal TwainException(string message) : base(message) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwainException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">Inner exception.</param>
        internal TwainException(string message,Exception innerException):base(message,innerException) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwainException"/> class.
        /// </summary>
        /// <param name="info">An object <see cref="T:System.Runtime.Serialization.SerializationInfo" />, containing serialized object data about the thrown exception.<para xml:lang="ru">Объект <see cref="T:System.Runtime.Serialization.SerializationInfo" />, содержащий сериализованные данные объекта о выбрасываемом исключении.</para></param>
        /// <param name="context">An object <see cref="T:System.Runtime.Serialization.StreamingContext" />, containing contextual information about the source or destination.<para xml:lang="ru">Объект <see cref="T:System.Runtime.Serialization.StreamingContext" />, содержащий контекстные сведения об источнике или назначении.</para></param>
        internal TwainException(SerializationInfo info,StreamingContext context) : base(info,context) {
            this.ConditionCode=(TwCC)info.GetValue("ConditionCode",typeof(TwCC));
            this.ReturnCode=(TwRC)info.GetValue("ReturnCode",typeof(TwRC));
        }

        /// <summary>
        /// When overridden in a derived class, sets the exception information for <see cref="T:System.Runtime.Serialization.SerializationInfo" />.
        /// <para xml:lang="ru">При переопределении в производном классе задает сведения об исключении для <see cref="T:System.Runtime.Serialization.SerializationInfo" />.</para>
        /// </summary>
        /// <param name="info">An object <see cref="T:System.Runtime.Serialization.SerializationInfo" />, containing serialized object data about the thrown exception.<para xml:lang="ru">Объект <see cref="T:System.Runtime.Serialization.SerializationInfo" />, содержащий сериализованные данные объекта о выбрасываемом исключении.</para></param>
        /// <param name="context">An object <see cref="T:System.Runtime.Serialization.StreamingContext" />, containing contextual information about the source or destination.<para xml:lang="ru">Объект <see cref="T:System.Runtime.Serialization.StreamingContext" />, содержащий контекстные сведения об источнике или назначении.</para></param>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter" />
        /// </PermissionSet>
        [SecurityPermission(SecurityAction.Demand,SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info,StreamingContext context) {
            base.GetObjectData(info,context);
            info.AddValue("ConditionCode",this.ConditionCode);
            info.AddValue("ReturnCode",this.ReturnCode);
        }

        /// <summary>
        /// Returns the operation status code. Get condition code.
        /// <para xml:lang="ru">Возвращает код состояния операции. Get condition code.</para>
        /// </summary>
        public TwCC ConditionCode {
            get;
            private set;
        }

        /// <summary>
        /// Returns the result code of the operation. Get return code.
        /// <para xml:lang="ru">Возвращает код результата операции. Get return code.</para>
        /// </summary>
        public TwRC ReturnCode {
            get;
            private set;
        }

        private static string _CodeToMessage(TwCC code) {
            switch(code) {
                case TwCC.Success:
                    return "It worked!";
                case TwCC.Bummer:
                    return "Failure due to unknown causes.";
                case TwCC.LowMemory:
                    return "Not enough memory to perform operation.";
                case TwCC.NoDS:
                    return "No Data Source.";
                case TwCC.MaxConnections:
                    return "DS is connected to max possible applications.";
                case TwCC.OperationError:
                    return "DS or DSM reported error, application shouldn't.";
                case TwCC.BadCap:
                    return "Unknown capability.";
                case TwCC.BadProtocol:
                    return "Unrecognized MSG DG DAT combination.";
                case TwCC.BadValue:
                    return "Data parameter out of range.";
                case TwCC.SeqError:
                    return "DG DAT MSG out of expected sequence.";
                case TwCC.BadDest:
                    return "Unknown destination Application/Source in DSM_Entry.";
                case TwCC.CapUnsupported:
                    return "Capability not supported by source.";
                case TwCC.CapBadOperation:
                    return "Operation not supported by capability.";
                case TwCC.CapSeqError:
                    return "Capability has dependancy on other capability.";
                /* Added 1.8 */
                case TwCC.Denied:
                    return "File System operation is denied (file is protected).";
                case TwCC.FileExists:
                    return "Operation failed because file already exists.";
                case TwCC.FileNotFound:
                    return "File not found.";
                case TwCC.NotEmpty:
                    return "Operation failed because directory is not empty.";
                case TwCC.PaperJam:
                    return "The feeder is jammed.";
                case TwCC.PaperDoubleFeed:
                    return "The feeder detected multiple pages.";
                case TwCC.FileWriteError:
                    return "Error writing the file (meant for things like disk full conditions).";
                case TwCC.CheckDeviceOnline:
                    return "The device went offline prior to or during this operation.";
                default:
                    return "Unknown error.";
            }
        }
    }
}
