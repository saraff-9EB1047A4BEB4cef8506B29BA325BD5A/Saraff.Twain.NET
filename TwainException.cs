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
    public sealed class TwainException : Exception {
        private readonly static Dictionary<TwCC, string> _twcc = new Dictionary<TwCC, string> {
            {TwCC.Success, "It worked!"},
            {TwCC.Bummer, "Failure due to unknown causes."},
            {TwCC.LowMemory, "Not enough memory to perform operation."},
            {TwCC.NoDS, "No Data Source."},
            {TwCC.MaxConnections, "DS is connected to max possible applications."},
            {TwCC.OperationError, "DS or DSM reported error, application shouldn't."},
            {TwCC.BadCap, "Unknown capability."},
            {TwCC.BadProtocol, "Unrecognized MSG DG DAT combination."},
            {TwCC.BadValue, "Data parameter out of range."},
            {TwCC.SeqError, "DG DAT MSG out of expected sequence."},
            {TwCC.BadDest, "Unknown destination Application/Source in DSM_Entry."},
            {TwCC.CapUnsupported, "Capability not supported by source."},
            {TwCC.CapBadOperation, "Operation not supported by capability."},
            {TwCC.CapSeqError, "Capability has dependancy on other capability."},
            /* Added 1.8 */
            {TwCC.Denied, "File System operation is denied (file is protected)."},
            {TwCC.FileExists, "Operation failed because file already exists."},
            {TwCC.FileNotFound, "File not found."},
            {TwCC.NotEmpty, "Operation failed because directory is not empty."},
            {TwCC.PaperJam, "The feeder is jammed."},
            {TwCC.PaperDoubleFeed, "The feeder detected multiple pages."},
            {TwCC.FileWriteError, "Error writing the file (meant for things like disk full conditions)."},
            {TwCC.CheckDeviceOnline, "The device went offline prior to or during this operation."}
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TwainException"/> class.
        /// </summary>
        /// <param name="cc">The condition code.</param>
        /// <param name="rc">The return code.</param>
        internal TwainException(TwCC cc, TwRC rc) : this(TwainException._CC2Message(cc)) {
            this.ConditionCode = cc;
            this.ReturnCode = rc;
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
        internal TwainException(string message, Exception innerException) : base(message, innerException) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwainException"/> class.
        /// </summary>
        /// <param name="info">An object <see cref="T:System.Runtime.Serialization.SerializationInfo" />, containing serialized object data about the thrown exception.<para xml:lang="ru">Объект <see cref="T:System.Runtime.Serialization.SerializationInfo" />, содержащий сериализованные данные объекта о выбрасываемом исключении.</para></param>
        /// <param name="context">An object <see cref="T:System.Runtime.Serialization.StreamingContext" />, containing contextual information about the source or destination.<para xml:lang="ru">Объект <see cref="T:System.Runtime.Serialization.StreamingContext" />, содержащий контекстные сведения об источнике или назначении.</para></param>
        internal TwainException(SerializationInfo info, StreamingContext context) : base(info, context) {
            this.ConditionCode = (TwCC)info.GetValue("ConditionCode", typeof(TwCC));
            this.ReturnCode = (TwRC)info.GetValue("ReturnCode", typeof(TwRC));
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
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("ConditionCode", this.ConditionCode);
            info.AddValue("ReturnCode", this.ReturnCode);
        }

        /// <summary>
        /// Returns the operation status code.
        /// <para xml:lang="ru">Возвращает код состояния операции.</para>
        /// </summary>
        public TwCC ConditionCode { get; private set; }

        /// <summary>
        /// Returns the result code of the operation.
        /// <para xml:lang="ru">Возвращает код результата операции.</para>
        /// </summary>
        public TwRC ReturnCode { get; private set; }

        private static string _CC2Message(TwCC code) {
            if(TwainException._twcc.TryGetValue(code, out var _result)) {
                return _result;
            }
            return "Unknown error.";
        }
    }
}
