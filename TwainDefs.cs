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
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;

namespace Saraff.Twain {

    #region Generic Constants

    /// <summary>
    /// Data Groups.
    /// </summary>
    [Flags]
    internal enum TwDG : uint {									// DG_.....

        /// <summary>
        /// Data pertaining to control.
        /// </summary>
        Control = 0x0001,

        /// <summary>
        /// Data pertaining to raster images.
        /// </summary>
        Image = 0x0002,

        /// <summary>
        /// Data pertaining to audio.
        /// </summary>
        Audio = 0x0004,

        /// <summary>
        /// added to the identity by the DSM.
        /// </summary>
        DSM2 = 0x10000000,

        /// <summary>
        /// Set by the App to indicate it would prefer to use DSM2.
        /// </summary>
        APP2 = 0x20000000,

        /// <summary>
        /// Set by the DS to indicate it would prefer to use DSM2.
        /// </summary>
        DS2 = 0x40000000
    }

    /// <summary>
    /// Data codes.
    /// </summary>
    internal enum TwDAT : ushort {									// DAT_....

        #region Data Argument Types for the DG_CONTROL Data Group.

        Null = 0x0000,
        Capability = 0x0001,
        Event = 0x0002,
        Identity = 0x0003,
        Parent = 0x0004,
        PendingXfers = 0x0005,
        SetupMemXfer = 0x0006,
        SetupFileXfer = 0x0007,
        Status = 0x0008,
        UserInterface = 0x0009,
        XferGroup = 0x000a,
        TwunkIdentity = 0x000b,
        CustomDSData = 0x000c,
        DeviceEvent = 0x000d,
        FileSystem = 0x000e,
        PassThru = 0x000f,
        Callback = 0x0010,   /* TW_CALLBACK        Added 2.0         */
        StatusUtf8 = 0x0011, /* TW_STATUSUTF8      Added 2.1         */
        Callback2 = 0x0012,

        #endregion

        #region Data Argument Types for the DG_IMAGE Data Group.

        ImageInfo = 0x0101,
        ImageLayout = 0x0102,
        ImageMemXfer = 0x0103,
        ImageNativeXfer = 0x0104,
        ImageFileXfer = 0x0105,
        CieColor = 0x0106,
        GrayResponse = 0x0107,
        RGBResponse = 0x0108,
        JpegCompression = 0x0109,
        Palette8 = 0x010a,
        ExtImageInfo = 0x010b,

        #endregion

        #region misplaced

        IccProfile = 0x0401,       /* TW_MEMORY        Added 1.91  This Data Argument is misplaced but belongs to the DG_IMAGE Data Group */
        ImageMemFileXfer = 0x0402, /* TW_IMAGEMEMXFER  Added 1.91  This Data Argument is misplaced but belongs to the DG_IMAGE Data Group */
        EntryPoint = 0x0403,       /* TW_ENTRYPOINT    Added 2.0   This Data Argument is misplaced but belongs to the DG_CONTROL Data Group */

        #endregion
    }

    /// <summary>
    /// Messages.
    /// </summary>
    internal enum TwMSG : ushort {									// MSG_.....

        #region Generic messages may be used with any of several DATs.

        /// <summary>
        /// Used in TW_EVENT structure.
        /// </summary>
        Null = 0x0000,

        /// <summary>
        /// Get one or more values.
        /// </summary>
        Get = 0x0001,

        /// <summary>
        /// Get current value.
        /// </summary>
        GetCurrent = 0x0002,

        /// <summary>
        /// Get default (e.g. power up) value.
        /// </summary>
        GetDefault = 0x0003,

        /// <summary>
        /// Get first of a series of items, e.g. DSs.
        /// </summary>
        GetFirst = 0x0004,

        /// <summary>
        /// Iterate through a series of items.
        /// </summary>
        GetNext = 0x0005,

        /// <summary>
        /// Set one or more values.
        /// </summary>
        Set = 0x0006,

        /// <summary>
        /// Set current value to default value.
        /// </summary>
        Reset = 0x0007,

        /// <summary>
        /// Get supported operations on the cap.
        /// </summary>
        QuerySupport = 0x0008,

        GetHelp = 0x0009,
        GetLabel = 0x000a,
        GetLabelEnum = 0x000b,
        SetConstraint = 0x000c,

        #endregion

        #region Messages used with DAT_NULL

        XFerReady = 0x0101,
        CloseDSReq = 0x0102,
        CloseDSOK = 0x0103,
        DeviceEvent = 0x0104,

        #endregion

        #region Messages used with a pointer to a DAT_STATUS structure

        /// <summary>
        /// Get status information
        /// </summary>
        CheckStatus = 0x0201,

        #endregion

        #region Messages used with a pointer to DAT_PARENT data

        /// <summary>
        /// Open the DSM
        /// </summary>
        OpenDSM = 0x0301,

        /// <summary>
        /// Close the DSM
        /// </summary>
        CloseDSM = 0x0302,

        #endregion

        #region Messages used with a pointer to a DAT_IDENTITY structure

        /// <summary>
        /// Open a data source
        /// </summary>
        OpenDS = 0x0401,

        /// <summary>
        /// Close a data source
        /// </summary>
        CloseDS = 0x0402,

        /// <summary>
        /// Put up a dialog of all DS
        /// </summary>
        UserSelect = 0x0403,

        #endregion

        #region Messages used with a pointer to a DAT_USERINTERFACE structure

        /// <summary>
        /// Disable data transfer in the DS
        /// </summary>
        DisableDS = 0x0501,

        /// <summary>
        /// Enable data transfer in the DS
        /// </summary>
        EnableDS = 0x0502,

        /// <summary>
        /// Enable for saving DS state only.
        /// </summary>
        EnableDSUIOnly = 0x0503,

        #endregion

        #region Messages used with a pointer to a DAT_EVENT structure

        ProcessEvent = 0x0601,

        #endregion

        #region Messages used with a pointer to a DAT_PENDINGXFERS structure

        EndXfer = 0x0701,
        StopFeeder = 0x0702,

        #endregion

        #region Messages used with a pointer to a DAT_FILESYSTEM structure

        ChangeDirectory = 0x0801,
        CreateDirectory = 0x0802,
        Delete = 0x0803,
        FormatMedia = 0x0804,
        GetClose = 0x0805,
        GetFirstFile = 0x0806,
        GetInfo = 0x0807,
        GetNextFile = 0x0808,
        Rename = 0x0809,
        Copy = 0x080A,
        AutoCaptureDir = 0x080B,

        #endregion

        #region Messages used with a pointer to a DAT_PASSTHRU structure

        PassThru = 0x0901,

        #endregion

        #region used with DAT_CALLBACK

        RegisterCallback = 0x0902,

        #endregion

        #region used with DAT_CAPABILITY

        ResetAll = 0x0A01

        #endregion
    }

    /// <summary>
    /// Return Codes
    /// </summary>
    public enum TwRC : ushort {                                   // TWRC_....

        /// <summary>
        /// Operation was successful.
        /// </summary>
        Success = 0x0000,

        /// <summary>
        /// May be returned by any operation. An error has occurred.
        /// </summary>
        Failure = 0x0001,

        /// <summary>
        /// Intended for use with DAT_CAPABILITY and DAT_IMAGELAYOUT. 
        /// Operation failed to completely perform
        /// the desired operation. For example, setting ICAP_BRIGHTNESS to
        /// 3 when its range is -1000 to 1000 with a step of 200. The data source
        /// may opt to set the value to 0 and return this status.
        /// </summary>
        CheckStatus = 0x0002,

        /// <summary>
        /// Intended for use with the DAT_IMAGE*XFER operations. Operation has been canceled.
        /// </summary>
        Cancel = 0x0003,

        /// <summary>
        /// Intended for use with DAT_EVENT. The data source processed the event.
        /// </summary>
        DSEvent = 0x0004,

        /// <summary>
        /// Intended for use with DAT_EVENT. The data source did not process the event.
        /// </summary>
        NotDSEvent = 0x0005,

        /// <summary>
        /// Intended for use with the DAT_IMAGE*XFER operations. The image has been fully transferred.
        /// </summary>
        XferDone = 0x0006,

        /// <summary>
        /// Intended for use with DAT_IDENTITY and DAT_FILESYSTEM.
        /// </summary>
        EndOfList = 0x0007,

        /// <summary>
        /// Intended for use with DAT_EXTIMAGEINFO. 
        /// The requested TWEI_ data is either not supported by this data source, or is not supported for this particular image.
        /// </summary>
        InfoNotSupported = 0x0008,

        /// <summary>
        /// Intended for use with DAT_EXTIMAGEINFO. There is no data available for the requested TWEI_ item.
        /// </summary>
        DataNotAvailable = 0x0009,

        /// <summary>
        /// The busy.
        /// </summary>
        Busy = 10,

        /// <summary>
        /// The scanner locked.
        /// </summary>
        ScannerLocked = 11
    }

    /// <summary>
    /// Condition Codes
    /// </summary>
    public enum TwCC : ushort {                                   // TWCC_....

        /// <summary>
        /// Operation was successful. This value should only be paired with TWRC_SUCCESS.
        /// </summary>
        Success = 0x0000,

        /// <summary>
        /// May be returned by any operation. The data source is in a critical state.
        /// </summary>
        Bummer = 0x0001,

        /// <summary>
        /// May be returned for any operation except ones that reduce state
        /// (DAT_PENDINGXFERS / MSG_ENDXER, DAT_PENDINGXFERS / MSG_RESET, 
        /// DAT_USERINTERFACE / MSG_DISABLEDS, DAT_IDENTITY / MSG_CLOSEDS, 
        /// DAT_PARENT / MSG_CLOSEDSM).
        /// </summary>
        LowMemory = 0x0002,

        /// <summary>
        /// Intended for use with DAT_IDENTITY / MSG_OPENDS. The device is not online.
        /// </summary>
        NoDS = 0x0003,

        /// <summary>
        /// Intended for use with DAT_IDENTITY / MSG_OPENDS. The data
        /// source cannot support any more connections to this device.
        /// </summary>
        MaxConnections = 0x0004,

        /// <summary>
        /// The operation failed, but the user has already been informed by the data source.
        /// </summary>
        OperationError = 0x0005,

        /// <summary>
        /// Intended for use with DAT_CAPABILITY. Returned by pre-1.7
        /// data sources to indicate that the capability is not supported, that the
        /// value was bad, or that the desired value could not be set at this time.
        /// </summary>
        BadCap = 0x0006,

        /// <summary>
        /// May be returned by any operation. The requested 
        /// DG_* / DAT_* / MSG_* is not supported by the data source.
        /// </summary>
        BadProtocol = 0x0009,

        /// <summary>
        /// May be returned by any operation. The capability or operation has
        /// rejected the requested setting.
        /// </summary>
        BadValue = 0x000a,

        /// <summary>
        /// The seq error.
        /// </summary>
        SeqError = 0x000b,

        /// <summary>
        /// May be returned by any operation (save for the DAT_PARENT
        /// operations). The TW_IDENTITY for the destination (the data
        /// source) does not match any items opened by MSG_OPENDS.
        /// </summary>
        BadDest = 0x000c,

        /// <summary>
        /// Intended for use with DAT_CAPABILITY. The capability is not supported.
        /// </summary>
        CapUnsupported = 0x000d,

        /// <summary>
        /// Intended for use with DAT_CAPABILITY. The capability does not support the requested operation.
        /// </summary>
        CapBadOperation = 0x000e,

        /// <summary>
        /// Intended for use with DAT_CAPABILITY. The capability being
        /// MSG_SET or MSG_RESET cannot be modified due to a setting for a
        /// related capability. For instance, this may be returned by
        /// ICAP_CITTKFACTOR if ICAP_COMPRESSION is set to any value
        /// other than TWCP_GROUP32D.
        /// </summary>
        CapSeqError = 0x000f,

        /// <summary>
        /// Intended for DAT_IMAGEFILEXFER and DAT_FILESYSTEM, the
        /// specified file or directory cannot be modified or deleted.
        /// </summary>
        Denied = 0x0010,

        /// <summary>
        /// Intended for DAT_FILESYSTEM. The specified file or directory already exists.
        /// </summary>
        FileExists = 0x0011,

        /// <summary>
        /// Intended for DAT_IMAGEFILEXFER and DAT_FILESYSTEM. The
        /// specified file or directory cannot be found.
        /// </summary>
        FileNotFound = 0x0012,

        /// <summary>
        /// Intended for use with DAT_FILESYSTEM. Directory is in use, and cannot be deleted.
        /// </summary>
        NotEmpty = 0x0013,

        /// <summary>
        /// Intended for use with the DAT_IMAGE*XFER operations.
        /// </summary>
        PaperJam = 0x0014,

        /// <summary>
        /// Intended for use with the DAT_IMAGE*XFER operations.
        /// </summary>
        PaperDoubleFeed = 0x0015,

        /// <summary>
        /// Intended for DAT_IMAGEFILEXFER and DAT_FILESYSTEM, the
        /// specified file or directory could not be written, usually indicating a
        /// disk full condition, though it may also indicate a file or directory
        /// that the user has no permission to write.
        /// </summary>
        FileWriteError = 0x0016,

        /// <summary>
        /// May be returned for any operation in state 4 or higher, except ones
        /// that reduce state (DAT_PENDINGXFERS / MSG_ENDXER,
        /// DAT_PENDINGXFERS / MSG_RESET, DAT_USERINTERFACE / MSG_DISABLEDS, 
        /// DAT_IDENTITY / MSG_CLOSEDS, DAT_PARENT / MSG_CLOSEDSM).
        /// </summary>
        CheckDeviceOnline = 0x0017,

        /// <summary>
        /// Intended for use with the DAT_IMAGE*XFER operations.
        /// </summary>
        InterLock = 24,

        /// <summary>
        /// Intended for use with the DAT_IMAGE*XFER operations.
        /// </summary>
        DamagedCorner = 25,

        /// <summary>
        /// Intended for use with the DAT_IMAGE*XFER operations.
        /// </summary>
        FocusError = 26,

        /// <summary>
        /// Intended for use with the DAT_IMAGE*XFER operations.
        /// </summary>
        DocTooLight = 27,

        /// <summary>
        /// Intended for use with the DAT_IMAGE*XFER operations.
        /// </summary>
        DocTooDark = 28,

        /// <summary>
        /// Intended for use with the DAT_IMAGE*XFER operations.
        /// </summary>
        NoMedia = 29,
    }

    /// <summary>
    /// Generic Constants
    /// </summary>
    internal enum TwOn : ushort {									// TWON_....

        /// <summary>
        /// Indicates TW_ARRAY container
        /// </summary>
        Array = 0x0003,

        /// <summary>
        /// Indicates TW_ENUMERATION container
        /// </summary>
        Enum = 0x0004,

        /// <summary>
        /// Indicates TW_ONEVALUE container
        /// </summary>
        One = 0x0005,

        /// <summary>
        /// Indicates TW_RANGE container
        /// </summary>
        Range = 0x0006,
        DontCare = 0xffff
    }

    /// <summary>
    /// Data Types
    /// </summary>
    internal enum TwType : ushort {									// TWTY_....
        Int8 = 0x0000,
        Int16 = 0x0001,
        Int32 = 0x0002,
        UInt8 = 0x0003,
        UInt16 = 0x0004,
        UInt32 = 0x0005,
        Bool = 0x0006,
        Fix32 = 0x0007,
        Frame = 0x0008,
        Str32 = 0x0009,
        Str64 = 0x000a,
        Str128 = 0x000b,
        Str255 = 0x000c,
        Str1024 = 0x000d,
        Uni512 = 0x000e,
        Handle = 0x000f
    }

    /// <summary>
    /// Helper class for twain types.
    /// <para xml:lang="ru">Вспомогательный класс для типов twain.</para>
    /// </summary>
    internal sealed class TwTypeHelper {
        private static Dictionary<TwType, Type> _typeof = new Dictionary<TwType, Type> {
            {TwType.Int8,typeof(sbyte)},
            {TwType.Int16,typeof(short)},
            {TwType.Int32,typeof(int)},
            {TwType.UInt8,typeof(byte)},
            {TwType.UInt16,typeof(ushort)},
            {TwType.UInt32,typeof(uint)},
            {TwType.Bool,typeof(TwBool)},
            {TwType.Fix32,typeof(TwFix32)},
            {TwType.Frame,typeof(TwFrame)},
            {TwType.Str32,typeof(TwStr32)},
            {TwType.Str64,typeof(TwStr64)},
            {TwType.Str128,typeof(TwStr128)},
            {TwType.Str255,typeof(TwStr255)},
            {TwType.Str1024,typeof(TwStr1024)},
            {TwType.Uni512,typeof(TwUni512)},
            {TwType.Handle,typeof(IntPtr)}
        };
        private static Dictionary<int, TwType> _typeofAux = new Dictionary<int, TwType> {
            {32,TwType.Str32},
            {64,TwType.Str64},
            {128,TwType.Str128},
            {255,TwType.Str255},
            {1024,TwType.Str1024},
            {512,TwType.Uni512}
        };

        /// <summary>
        /// Returns the corresponding twain type of the managed type.
        /// <para xml:lang="ru">Возвращает соответствующий twain-типу управляемый тип.</para>
        /// </summary>
        /// <param name="type">Type code given by twain.<para xml:lang="ru">Код типа данный twain.</para></param>
        /// <returns>Managed type.<para xml:lang="ru">Управляемый тип.</para></returns>
        internal static Type TypeOf(TwType type) {
            return TwTypeHelper._typeof[type];
        }

        /// <summary>
        /// Returns the corresponding twain type for the managed type.
        /// <para xml:lang="ru">Возвращает соответствующий управляемому типу twain-тип.</para>
        /// </summary>
        /// <param name="type">Managed type.<para xml:lang="ru">Управляемый тип.</para></param>
        /// <returns>Type code given by twain.<para xml:lang="ru">Код типа данный twain.</para></returns>
        internal static TwType TypeOf(Type type) {
            Type _type = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
            foreach(var _item in TwTypeHelper._typeof) {
                if(_item.Value == _type) {
                    return _item.Key;
                }
            }
            if(type == typeof(bool)) {
                return TwType.Bool;
            }
            if(type == typeof(float)) {
                return TwType.Fix32;
            }
            if(type == typeof(RectangleF)) {
                return TwType.Frame;
            }
            throw new KeyNotFoundException();
        }

        /// <summary>
        /// Returns the corresponding twain type.
        /// <para xml:lang="ru">Возвращает соответствующий объекту twain-тип.</para>
        /// </summary>
        /// <param name="obj">An object.<para xml:lang="ru">Объект.</para></param>
        /// <returns>Type code given by twain.<para xml:lang="ru">Код типа данный twain.</para></returns>
        internal static TwType TypeOf(object obj) {
            if(obj is string) {
                return TwTypeHelper._typeofAux[((string)obj).Length];
            }
            return TwTypeHelper.TypeOf(obj.GetType());
        }

        /// <summary>
        /// Returns the size of a twain type in an unmanaged memory block.
        /// <para xml:lang="ru">Возвращает размер twain-типа в неуправляемом блоке памяти.</para>
        /// </summary>
        /// <param name="type">Type code given by twain.<para xml:lang="ru">Код типа данный twain.</para></param>
        /// <returns>Size in bytes.<para xml:lang="ru">Размер в байтах.</para></returns>
        internal static int SizeOf(TwType type) {
            return Marshal.SizeOf(TwTypeHelper._typeof[type]);
        }

        /// <summary>
        /// Converts internal component types to common environment types.
        /// <para xml:lang="ru">Приводит внутренние типы компонента к общим типам среды.</para>
        /// </summary>
        /// <param name="type">Twain type code.<para xml:lang="ru">Код twain-типа.</para></param>
        /// <param name="value">Instance of the object.<para xml:lang="ru">Экземпляр объекта.</para></param>
        /// <returns>Instance of the object.<para xml:lang="ru">Экземпляр объекта.</para></returns>
        internal static object CastToCommon(TwType type, object value) {
            switch(type) {
                case TwType.Bool:
                    return (bool)(TwBool)value;
                case TwType.Fix32:
                    return (float)(TwFix32)value;
                case TwType.Frame:
                    return (RectangleF)(TwFrame)value;
                case TwType.Str128:
                case TwType.Str255:
                case TwType.Str32:
                case TwType.Str64:
                case TwType.Uni512:
                case TwType.Str1024:
                    return value.ToString();
            }
            return value;
        }

        /// <summary>
        /// Converts generic media types to internal component types.
        /// <para xml:lang="ru">Приводит общие типы среды к внутренним типам компонента.</para>
        /// </summary>
        /// <param name="type">Twain type code.<para xml:lang="ru">Код twain-типа.</para></param>
        /// <param name="value">Instance of the object.<para xml:lang="ru">Экземпляр объекта.</para></param>
        /// <returns>Instance of the object.<para xml:lang="ru">Экземпляр объекта.</para></returns>
        internal static object CastToTw(TwType type, object value) {
            switch(type) {
                case TwType.Bool:
                    return (TwBool)(bool)value;
                case TwType.Fix32:
                    return (TwFix32)(float)value;
                case TwType.Frame:
                    return (TwFrame)(RectangleF)value;
                case TwType.Str32:
                    return (TwStr32)value.ToString();
                case TwType.Str64:
                    return (TwStr64)value.ToString();
                case TwType.Str128:
                    return (TwStr128)value.ToString();
                case TwType.Str255:
                    return (TwStr255)value.ToString();
                case TwType.Uni512:
                    return (TwUni512)value.ToString();
                case TwType.Str1024:
                    return (TwStr1024)value.ToString();
            }

            Type _type = value.GetType();
            if(_type.IsEnum && Enum.GetUnderlyingType(_type) == TwTypeHelper.TypeOf(type)) {
                return Convert.ChangeType(value, Enum.GetUnderlyingType(_type));
            }

            return value;
        }

        /// <summary>
        /// Converts a value to an instance of the component's internal type.
        /// <para xml:lang="ru">Выполняет преобразование значения в экземпляр внутреннего типа компонента.</para>
        /// </summary>
        /// <typeparam name="T">Тип значения.</typeparam>
        /// <param name="type">Twain type code.<para xml:lang="ru">Код twain-типа.</para></param>
        /// <param name="value">Value.<para xml:lang="ru">Значение.</para></param>
        /// <returns>Instance of the object.<para xml:lang="ru">Экземпляр объекта.</para></returns>
        internal static object ValueToTw<T>(TwType type, T value) {
            int _size = Marshal.SizeOf(typeof(T));
            IntPtr _mem = Marshal.AllocHGlobal(_size);
            Twain32._Memory.ZeroMemory(_mem, (IntPtr)_size);
            try {
                Marshal.StructureToPtr(value, _mem, true);
                return Marshal.PtrToStructure(_mem, TwTypeHelper.TypeOf(type));
            } finally {
                Marshal.FreeHGlobal(_mem);
            }
        }

        /// <summary>
        /// Converts an instance of an internal component type to a value.
        /// <para xml:lang="ru">Выполняет преобразование экземпляра внутреннего типа компонента в значение.</para>
        /// </summary>
        /// <typeparam name="T">Тип значения.</typeparam>
        /// <param name="value">Instance of the object.<para xml:lang="ru">Экземпляр объекта.</para></param>
        /// <returns>Value.<para xml:lang="ru">Значение.</para></returns>
        internal static T ValueFromTw<T>(object value) {
            int _size = Math.Max(Marshal.SizeOf(typeof(T)), Marshal.SizeOf(value));
            IntPtr _mem = Marshal.AllocHGlobal(_size);
            Twain32._Memory.ZeroMemory(_mem, (IntPtr)_size);
            try {
                Marshal.StructureToPtr(value, _mem, true);
                return (T)Marshal.PtrToStructure(_mem, typeof(T));
            } finally {
                Marshal.FreeHGlobal(_mem);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal sealed class TwTypeAttribute : Attribute {

        public TwTypeAttribute(TwType type) {
            this.TwType = type;
        }

        public TwType TwType {
            get;
            private set;
        }
    }

    /// <summary>
    /// Capability Constants
    /// </summary>
    public enum TwCap : ushort {
        /* image data sources MAY support these caps */
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        XferCount = 0x0001,			// all data sources are REQUIRED to support these caps
        ICompression = 0x0100,		// ICAP_...
        IPixelType = 0x0101,
        IUnits = 0x0102,              //default is TWUN_INCHES
        IXferMech = 0x0103,
        AutoBright = 0x1100,
        Brightness = 0x1101,
        Contrast = 0x1103,
        CustHalftone = 0x1104,
        ExposureTime = 0x1105,
        Filter = 0x1106,
        FlashUsed = 0x1107,
        Gamma = 0x1108,

        [TwType(TwType.Str32)]
        Halftones = 0x1109,

        Highlight = 0x110a,
        ImageFileFormat = 0x110c,
        LampState = 0x110d,
        LightSource = 0x110e,
        Orientation = 0x1110,
        PhysicalWidth = 0x1111,
        PhysicalHeight = 0x1112,
        Shadow = 0x1113,
        Frames = 0x1114,
        XNativeResolution = 0x1116,
        YNativeResolution = 0x1117,
        XResolution = 0x1118,
        YResolution = 0x1119,
        MaxFrames = 0x111a,
        Tiles = 0x111b,
        BitOrder = 0x111c,
        CcittKFactor = 0x111d,
        LightPath = 0x111e,
        PixelFlavor = 0x111f,
        PlanarChunky = 0x1120,
        Rotation = 0x1121,
        SupportedSizes = 0x1122,
        Threshold = 0x1123,
        XScaling = 0x1124,
        YScaling = 0x1125,
        BitOrderCodes = 0x1126,
        PixelFlavorCodes = 0x1127,
        JpegPixelType = 0x1128,
        TimeFill = 0x112a,
        BitDepth = 0x112b,
        BitDepthReduction = 0x112c,  /* Added 1.5 */
        UndefinedImageSize = 0x112d,  /* Added 1.6 */
        ImageDataSet = 0x112e,  /* Added 1.7 */
        ExtImageInfo = 0x112f,  /* Added 1.7 */
        MinimumHeight = 0x1130,  /* Added 1.7 */
        MinimumWidth = 0x1131,  /* Added 1.7 */
        AutoDiscardBlankPages = 0x1134,  /* Added 2.0 */
        FlipRotation = 0x1136,  /* Added 1.8 */
        BarCodeDetectionEnabled = 0x1137,  /* Added 1.8 */
        SupportedBarCodeTypes = 0x1138,  /* Added 1.8 */
        BarCodeMaxSearchPriorities = 0x1139,  /* Added 1.8 */
        BarCodeSearchPriorities = 0x113a,  /* Added 1.8 */
        BarCodeSearchMode = 0x113b,  /* Added 1.8 */
        BarCodeMaxRetries = 0x113c,  /* Added 1.8 */
        BarCodeTimeout = 0x113d,  /* Added 1.8 */
        ZoomFactor = 0x113e,  /* Added 1.8 */
        PatchCodeDetectionEnabled = 0x113f,  /* Added 1.8 */
        SupportedPatchCodeTypes = 0x1140,  /* Added 1.8 */
        PatchCodeMaxSearchPriorities = 0x1141,  /* Added 1.8 */
        PatchCodeSearchPriorities = 0x1142,  /* Added 1.8 */
        PatchCodeSearchMode = 0x1143,  /* Added 1.8 */
        PatchCodeMaxRetries = 0x1144,  /* Added 1.8 */
        PatchCodeTimeout = 0x1145,  /* Added 1.8 */
        FlashUsed2 = 0x1146,  /* Added 1.8 */
        ImageFilter = 0x1147,  /* Added 1.8 */
        NoiseFilter = 0x1148,  /* Added 1.8 */
        OverScan = 0x1149,  /* Added 1.8 */
        AutomaticBorderDetection = 0x1150,  /* Added 1.8 */
        AutomaticDeskew = 0x1151,  /* Added 1.8 */
        AutomaticRotate = 0x1152,  /* Added 1.8 */
        JpegQuality = 0x1153,  /* Added 1.9 */
        FeederType = 0x1154,
        IccProfile = 0x1155,
        AutoSize = 0x1156,
        AutomaticCropUsesFrame = 0x1157,
        AutomaticLengthDetection = 0x1158,
        AutomaticColorEnabled = 0x1159,
        AutomaticColorNonColorPixelType = 0x115a,
        ColorManagementEnabled = 0x115b,
        ImageMerge = 0x115c,
        ImageMergeHeightThreshold = 0x115d,
        SupportedExtImageInfo = 0x115e,
        FilmType = 0x115f,
        Mirror = 0x1160,
        JpegSubSampling = 0x1161,

        /* all data sources MAY support these caps */
        [TwType(TwType.Str128)]
        Author = 0x1000,

        [TwType(TwType.Str255)]
        Caption = 0x1001,

        FeederEnabled = 0x1002,
        FeederLoaded = 0x1003,

        [TwType(TwType.Str32)]
        TimeDate = 0x1004,

        SupportedCaps = 0x1005,
        ExtendedCaps = 0x1006,
        AutoFeed = 0x1007,
        ClearPage = 0x1008,
        FeedPage = 0x1009,
        RewindPage = 0x100a,
        Indicators = 0x100b,   /* Added 1.1 */
        SupportedCapsExt = 0x100c,   /* Added 1.6 */
        PaperDetectable = 0x100d,   /* Added 1.6 */
        UIControllable = 0x100e,   /* Added 1.6 */
        DeviceOnline = 0x100f,   /* Added 1.6 */
        AutoScan = 0x1010,   /* Added 1.6 */
        ThumbnailsEnabled = 0x1011,   /* Added 1.7 */
        Duplex = 0x1012,   /* Added 1.7 */
        DuplexEnabled = 0x1013,   /* Added 1.7 */
        EnableDSUIOnly = 0x1014,   /* Added 1.7 */
        CustomDSData = 0x1015,   /* Added 1.7 */
        Endorser = 0x1016,   /* Added 1.7 */
        JobControl = 0x1017,   /* Added 1.7 */
        Alarms = 0x1018,   /* Added 1.8 */
        AlarmVolume = 0x1019,   /* Added 1.8 */
        AutomaticCapture = 0x101a,   /* Added 1.8 */
        TimeBeforeFirstCapture = 0x101b,   /* Added 1.8 */
        TimeBetweenCaptures = 0x101c,   /* Added 1.8 */
        ClearBuffers = 0x101d,   /* Added 1.8 */
        MaxBatchBuffers = 0x101e,   /* Added 1.8 */

        [TwType(TwType.Str32)]
        DeviceTimeDate = 0x101f,   /* Added 1.8 */

        PowerSupply = 0x1020,   /* Added 1.8 */
        CameraPreviewUI = 0x1021,   /* Added 1.8 */
        DeviceEvent = 0x1022,   /* Added 1.8 */

        [TwType(TwType.Str255)]
        SerialNumber = 0x1024,   /* Added 1.8 */

        Printer = 0x1026,   /* Added 1.8 */
        PrinterEnabled = 0x1027,   /* Added 1.8 */
        PrinterIndex = 0x1028,   /* Added 1.8 */
        PrinterMode = 0x1029,   /* Added 1.8 */

        [TwType(TwType.Str255)]
        PrinterString = 0x102a,   /* Added 1.8 */

        [TwType(TwType.Str255)]
        PrinterSuffix = 0x102b,   /* Added 1.8 */

        Language = 0x102c,   /* Added 1.8 */
        FeederAlignment = 0x102d,   /* Added 1.8 */
        FeederOrder = 0x102e,   /* Added 1.8 */
        ReacquireAllowed = 0x1030,   /* Added 1.8 */
        BatteryMinutes = 0x1032,   /* Added 1.8 */
        BatteryPercentage = 0x1033,   /* Added 1.8 */
        CameraSide = 0x1034,
        Segmented = 0x1035,
        CameraEnabled = 0x1036,
        CameraOrder = 0x1037,
        MicrEnabled = 0x1038,
        FeederPrep = 0x1039,
        FeederPocket = 0x103a,
        AutomaticSenseMedium = 0x103b,

        [TwType(TwType.Str255)]
        CustomInterfaceGuid = 0x103c,

        SupportedCapsSegmentUnique = 0x103d,
        SupportedDats = 0x103e,
        DoubleFeedDetection = 0x103f,
        DoubleFeedDetectionLength = 0x1040,
        DoubleFeedDetectionSensitivity = 0x1041,
        DoubleFeedDetectionResponse = 0x1042,
        PaperHandling = 0x1043,
        IndicatorsMode = 0x1044,
        PrinterVerticalOffset = 0x1045,
        PowerSaveTime = 0x1046,
        PrinterCharRotation = 0x1047,
        PrinterFontStyle = 0x1048,
        PrinterIndexLeadChar = 0x1049,
        PrinterIndexMaxValue = 0x104A,
        PrinterIndexNumDigits = 0x104B,
        PrinterIndexStep = 0x104C,
        PrinterIndexTrigger = 0x104D,
        PrinterStringPreview = 0x104E,
        SheetCount = 0x104F // Controls the number of sheets scanned (compare to CAP_XFERCOUNT that controls images)
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// Bit patterns: for query the operation that are supported by the data source on a capability.
    /// </summary>
    [Flags]
    public enum TwQC : ushort { //TWQC_...

        /// <summary>
        /// Returns the Current, Default and Available settings for a capability.
        /// </summary>
        Get = 0x0001,

        /// <summary>
        /// Allows the application to set the Current value of a capability.
        /// </summary>
        Set = 0x0002,

        /// <summary>
        /// Returns the value of the Source’s preferred Default values.
        /// </summary>
        GetDefault = 0x0004,

        /// <summary>
        /// Returns the Current setting for a capability.
        /// </summary>
        GetCurrent = 0x0008,

        /// <summary>
        /// Returns the capability to its TWAIN Default (power-on) condition (i.e. all previous negotiation is ignored).
        /// </summary>
        Reset = 0x0010,

        /// <summary>
        /// Allows the application to set the Current and Default value(s) and
        /// restrict the Available values to some subset of the Source’s power-on
        /// set of values. Sources are strongly encouraged to allow the
        /// application to set as many of its capabilities as possible, and further to
        /// reflect these changes in the Source’s user interface. This will ensure
        /// that the user can only select images with characteristics that are
        /// useful to the consuming application.
        /// </summary>
        SetConstraint = 0x0020,

#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        ConstrainAble = 0x0040,
        GetHelp = 0x0100,
        GetLabel = 0x0200,
        GetLabelEnum = 0x0400
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// Language Constants
    /// </summary>
    public enum TwLanguage : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        DANISH = 0,             /* Danish                 */
        DUTCH = 1,              /* Dutch                  */
        ENGLISH = 2,            /* International English  */
        FRENCH_CANADIAN = 3,    /* French Canadian        */
        FINNISH = 4,            /* Finnish                */
        FRENCH = 5,             /* French                 */
        GERMAN = 6,             /* German                 */
        ICELANDIC = 7,          /* Icelandic              */
        ITALIAN = 8,            /* Italian                */
        NORWEGIAN = 9,          /* Norwegian              */
        PORTUGUESE = 10,        /* Portuguese             */
        SPANISH = 11,           /* Spanish                */
        SWEDISH = 12,           /* Swedish                */
        ENGLISH_USA = 13,       /* U.S. English           */
        AFRIKAANS = 14,
        ALBANIA = 15,
        ARABIC = 16,
        ARABIC_ALGERIA = 17,
        ARABIC_BAHRAIN = 18,
        ARABIC_EGYPT = 19,
        ARABIC_IRAQ = 20,
        ARABIC_JORDAN = 21,
        ARABIC_KUWAIT = 22,
        ARABIC_LEBANON = 23,
        ARABIC_LIBYA = 24,
        ARABIC_MOROCCO = 25,
        ARABIC_OMAN = 26,
        ARABIC_QATAR = 27,
        ARABIC_SAUDIARABIA = 28,
        ARABIC_SYRIA = 29,
        ARABIC_TUNISIA = 30,
        ARABIC_UAE = 31, /* United Arabic Emirates */
        ARABIC_YEMEN = 32,
        BASQUE = 33,
        BYELORUSSIAN = 34,
        BULGARIAN = 35,
        CATALAN = 36,
        CHINESE = 37,
        CHINESE_HONGKONG = 38,
        CHINESE_PRC = 39, /* People's Republic of China */
        CHINESE_SINGAPORE = 40,
        CHINESE_SIMPLIFIED = 41,
        CHINESE_TAIWAN = 42,
        CHINESE_TRADITIONAL = 43,
        CROATIA = 44,
        CZECH = 45,
        DUTCH_BELGIAN = 46,
        ENGLISH_AUSTRALIAN = 47,
        ENGLISH_CANADIAN = 48,
        ENGLISH_IRELAND = 49,
        ENGLISH_NEWZEALAND = 50,
        ENGLISH_SOUTHAFRICA = 51,
        ENGLISH_UK = 52,
        ESTONIAN = 53,
        FAEROESE = 54,
        FARSI = 55,
        FRENCH_BELGIAN = 56,
        FRENCH_LUXEMBOURG = 57,
        FRENCH_SWISS = 58,
        GERMAN_AUSTRIAN = 59,
        GERMAN_LUXEMBOURG = 60,
        GERMAN_LIECHTENSTEIN = 61,
        GERMAN_SWISS = 62,
        GREEK = 63,
        HEBREW = 64,
        HUNGARIAN = 65,
        INDONESIAN = 66,
        ITALIAN_SWISS = 67,
        JAPANESE = 68,
        KOREAN = 69,
        KOREAN_JOHAB = 70,
        LATVIAN = 71,
        LITHUANIAN = 72,
        NORWEGIAN_BOKMAL = 73,
        NORWEGIAN_NYNORSK = 74,
        POLISH = 75,
        PORTUGUESE_BRAZIL = 76,
        ROMANIAN = 77,
        RUSSIAN = 78,
        SERBIAN_LATIN = 79,
        SLOVAK = 80,
        SLOVENIAN = 81,
        SPANISH_MEXICAN = 82,
        SPANISH_MODERN = 83,
        THAI = 84,
        TURKISH = 85,
        UKRANIAN = 86,
        /* More stuff added for 1.8 */
        ASSAMESE = 87,
        BENGALI = 88,
        BIHARI = 89,
        BODO = 90,
        DOGRI = 91,
        GUJARATI = 92,
        HARYANVI = 93,
        HINDI = 94,
        KANNADA = 95,
        KASHMIRI = 96,
        MALAYALAM = 97,
        MARATHI = 98,
        MARWARI = 99,
        MEGHALAYAN = 100,
        MIZO = 101,
        NAGA = 102,
        ORISSI = 103,
        PUNJABI = 104,
        PUSHTU = 105,
        SERBIAN_CYRILLIC = 106,
        SIKKIMI = 107,
        SWEDISH_FINLAND = 108,
        TAMIL = 109,
        TELUGU = 110,
        TRIPURI = 111,
        URDU = 112,
        VIETNAMESE = 113
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// Country Constantsz
    /// </summary>
    public enum TwCountry : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        AFGHANISTAN = 1001,
        ALGERIA = 213,
        AMERICANSAMOA = 684,
        ANDORRA = 033,
        ANGOLA = 1002,
        ANGUILLA = 8090,
        ANTIGUA = 8091,
        ARGENTINA = 54,
        ARUBA = 297,
        ASCENSIONI = 247,
        AUSTRALIA = 61,
        AUSTRIA = 43,
        BAHAMAS = 8092,
        BAHRAIN = 973,
        BANGLADESH = 880,
        BARBADOS = 8093,
        BELGIUM = 32,
        BELIZE = 501,
        BENIN = 229,
        BERMUDA = 8094,
        BHUTAN = 1003,
        BOLIVIA = 591,
        BOTSWANA = 267,
        BRITAIN = 6,
        BRITVIRGINIS = 8095,
        BRAZIL = 55,
        BRUNEI = 673,
        BULGARIA = 359,
        BURKINAFASO = 1004,
        BURMA = 1005,
        BURUNDI = 1006,
        CAMAROON = 237,
        CANADA = 2,
        CAPEVERDEIS = 238,
        CAYMANIS = 8096,
        CENTRALAFREP = 1007,
        CHAD = 1008,
        CHILE = 56,
        CHINA = 86,
        CHRISTMASIS = 1009,
        COCOSIS = 1009,
        COLOMBIA = 57,
        COMOROS = 1010,
        CONGO = 1011,
        COOKIS = 1012,
        COSTARICA = 506,
        CUBA = 005,
        CYPRUS = 357,
        CZECHOSLOVAKIA = 42,
        DENMARK = 45,
        DJIBOUTI = 1013,
        DOMINICA = 8097,
        DOMINCANREP = 8098,
        EASTERIS = 1014,
        ECUADOR = 593,
        EGYPT = 20,
        ELSALVADOR = 503,
        EQGUINEA = 1015,
        ETHIOPIA = 251,
        FALKLANDIS = 1016,
        FAEROEIS = 298,
        FIJIISLANDS = 679,
        FINLAND = 358,
        FRANCE = 33,
        FRANTILLES = 596,
        FRGUIANA = 594,
        FRPOLYNEISA = 689,
        FUTANAIS = 1043,
        GABON = 241,
        GAMBIA = 220,
        GERMANY = 49,
        GHANA = 233,
        GIBRALTER = 350,
        GREECE = 30,
        GREENLAND = 299,
        GRENADA = 8099,
        GRENEDINES = 8015,
        GUADELOUPE = 590,
        GUAM = 671,
        GUANTANAMOBAY = 5399,
        GUATEMALA = 502,
        GUINEA = 224,
        GUINEABISSAU = 1017,
        GUYANA = 592,
        HAITI = 509,
        HONDURAS = 504,
        HONGKONG = 852,
        HUNGARY = 36,
        ICELAND = 354,
        INDIA = 91,
        INDONESIA = 62,
        IRAN = 98,
        IRAQ = 964,
        IRELAND = 353,
        ISRAEL = 972,
        ITALY = 39,
        IVORYCOAST = 225,
        JAMAICA = 8010,
        JAPAN = 81,
        JORDAN = 962,
        KENYA = 254,
        KIRIBATI = 1018,
        KOREA = 82,
        KUWAIT = 965,
        LAOS = 1019,
        LEBANON = 1020,
        LIBERIA = 231,
        LIBYA = 218,
        LIECHTENSTEIN = 41,
        LUXENBOURG = 352,
        MACAO = 853,
        MADAGASCAR = 1021,
        MALAWI = 265,
        MALAYSIA = 60,
        MALDIVES = 960,
        MALI = 1022,
        MALTA = 356,
        MARSHALLIS = 692,
        MAURITANIA = 1023,
        MAURITIUS = 230,
        MEXICO = 3,
        MICRONESIA = 691,
        MIQUELON = 508,
        MONACO = 33,
        MONGOLIA = 1024,
        MONTSERRAT = 8011,
        MOROCCO = 212,
        MOZAMBIQUE = 1025,
        NAMIBIA = 264,
        NAURU = 1026,
        NEPAL = 977,
        NETHERLANDS = 31,
        NETHANTILLES = 599,
        NEVIS = 8012,
        NEWCALEDONIA = 687,
        NEWZEALAND = 64,
        NICARAGUA = 505,
        NIGER = 227,
        NIGERIA = 234,
        NIUE = 1027,
        NORFOLKI = 1028,
        NORWAY = 47,
        OMAN = 968,
        PAKISTAN = 92,
        PALAU = 1029,
        PANAMA = 507,
        PARAGUAY = 595,
        PERU = 51,
        PHILLIPPINES = 63,
        PITCAIRNIS = 1030,
        PNEWGUINEA = 675,
        POLAND = 48,
        PORTUGAL = 351,
        QATAR = 974,
        REUNIONI = 1031,
        ROMANIA = 40,
        RWANDA = 250,
        SAIPAN = 670,
        SANMARINO = 39,
        SAOTOME = 1033,
        SAUDIARABIA = 966,
        SENEGAL = 221,
        SEYCHELLESIS = 1034,
        SIERRALEONE = 1035,
        SINGAPORE = 65,
        SOLOMONIS = 1036,
        SOMALI = 1037,
        SOUTHAFRICA = 27,
        SPAIN = 34,
        SRILANKA = 94,
        STHELENA = 1032,
        STKITTS = 8013,
        STLUCIA = 8014,
        STPIERRE = 508,
        STVINCENT = 8015,
        SUDAN = 1038,
        SURINAME = 597,
        SWAZILAND = 268,
        SWEDEN = 46,
        SWITZERLAND = 41,
        SYRIA = 1039,
        TAIWAN = 886,
        TANZANIA = 255,
        THAILAND = 66,
        TOBAGO = 8016,
        TOGO = 228,
        TONGAIS = 676,
        TRINIDAD = 8016,
        TUNISIA = 216,
        TURKEY = 90,
        TURKSCAICOS = 8017,
        TUVALU = 1040,
        UGANDA = 256,
        USSR = 7,
        UAEMIRATES = 971,
        UNITEDKINGDOM = 44,
        USA = 1,
        URUGUAY = 598,
        VANUATU = 1041,
        VATICANCITY = 39,
        VENEZUELA = 58,
        WAKE = 1042,
        WALLISIS = 1043,
        WESTERNSAHARA = 1044,
        WESTERNSAMOA = 1045,
        YEMEN = 1046,
        YUGOSLAVIA = 38,
        ZAIRE = 243,
        ZAMBIA = 260,
        ZIMBABWE = 263,
        /* Added for 1.8 */
        ALBANIA = 355,
        ARMENIA = 374,
        AZERBAIJAN = 994,
        BELARUS = 375,
        BOSNIAHERZGO = 387,
        CAMBODIA = 855,
        CROATIA = 385,
        CZECHREPUBLIC = 420,
        DIEGOGARCIA = 246,
        ERITREA = 291,
        ESTONIA = 372,
        GEORGIA = 995,
        LATVIA = 371,
        LESOTHO = 266,
        LITHUANIA = 370,
        MACEDONIA = 389,
        MAYOTTEIS = 269,
        MOLDOVA = 373,
        MYANMAR = 95,
        NORTHKOREA = 850,
        PUERTORICO = 787,
        RUSSIA = 7,
        SERBIA = 381,
        SLOVAKIA = 421,
        SLOVENIA = 386,
        SOUTHKOREA = 82,
        UKRAINE = 380,
        USVIRGINIS = 340,
        VIETNAM = 84
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// Unit of measure
    /// </summary>
    public enum TwUnits : ushort { //ICAP_UNITS values (UN_ means UNits)
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Inches = 0,
        Centimeters = 1,
        Picas = 2,
        Points = 3,
        Twips = 4,
        Pixels = 5,
        Millimeters = 6
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// Pixel types
    /// </summary>
    public enum TwPixelType : ushort { //ICAP_PIXELTYPE values (PT_ means Pixel Type)

        /// <summary>
        /// Black and white
        /// </summary>
        BW = 0, /* Black and White */
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Gray = 1,
        RGB = 2,
        Palette = 3,
        CMY = 4,
        CMYK = 5,
        YUV = 6,
        YUVK = 7,
        CIEXYZ = 8,
        LAB = 9,
        SRGB = 10,
        SCRGB = 11,
        INFRARED = 16
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// Compression values
    /// </summary>
    public enum TwCompression : ushort { //ICAP_COMPRESSION values (CP_ means ComPression )
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        PackBits = 1,

        /// <summary>
        /// Follows CCITT spec (no End Of Line)
        /// </summary>
        Group31D = 2,

        /// <summary>
        /// Follows CCITT spec (has End Of Line)
        /// </summary>
        Group31Deol = 3,

        /// <summary>
        /// Follows CCITT spec (use cap for K Factor)
        /// </summary>
        Group32D = 4,

        /// <summary>
        /// Follows CCITT spec
        /// </summary>
        Group4 = 5,

        /// <summary>
        /// Use capability for more info
        /// </summary>
        Jpeg = 6,

        /// <summary>
        /// Must license from Unisys and IBM to use
        /// </summary>
        Lzw = 7,

        /// <summary>
        /// For Bitonal images  -- Added 1.7 KHL
        /// </summary>
        Jbig = 8,

        /* Added 1.8 */
        Png = 9,
        Rle4 = 10,
        Rle8 = 11,
        BitFields = 12,
        Zip = 13,
        Jpeg2000 = 14
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// Extended Image Info Attributes.
    /// </summary>
    public enum TwEI : ushort { //TWEI_xxxx
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        BarCodeX = 0x1200,
        BarCodeY = 0x1201,
        BarCodeText = 0x1202,
        BarCodeType = 0x1203,
        DeshadeTop = 0x1204,
        DeshadeLeft = 0x1205,
        DeshadeHeight = 0x1206,
        DeshadeWidth = 0x1207,
        DeshadeSize = 0x1208,
        SpecklesRemoved = 0x1209,
        HorzLineXCoord = 0x120A,
        HorzLineYCoord = 0x120B,
        HorzLineLength = 0x120C,
        HorzLineThickness = 0x120D,
        VertLineXCoord = 0x120E,
        VertLinEYCoord = 0x120F,
        VertLineLength = 0x1210,
        VertLineThickness = 0x1211,
        PatchCode = 0x1212,
        EndOrSedText = 0x1213,
        FormConfidence = 0x1214,
        FormTemplateMatch = 0x1215,
        FormTemplatePageMatch = 0x1216,
        FormHorzDocOffset = 0x1217,
        FormVertDocOffset = 0x1218,
        BarCodeCount = 0x1219,
        BarCodeConfidence = 0x121A,
        BarCodeRotation = 0x121B,
        BarCodeTextLength = 0x121C,
        DeshadeCount = 0x121D,
        DeshadeBlackCountOld = 0x121E,
        DeshadeBlackCountNew = 0x121F,
        DeshadeBlackRLMin = 0x1220,
        DeshadeBlackRLMax = 0x1221,
        DeshadeWhiteCountOld = 0x1222,
        DeshadeWhiteCountNew = 0x1223,
        DeshadeWhiteRLMin = 0x1224,
        DeshadeWhiteRLAve = 0x1225,
        DeshadeWhiteRLMax = 0x1226,
        BlackSpecklesRemoved = 0x1227,
        WhiteSpecklesRemoved = 0x1228,
        HorzLineCount = 0x1229,
        VertLineCount = 0x122A,
        DeskewStatus = 0x122B,
        SkewOriginalAngle = 0x122C,
        SkewFinalAngle = 0x122D,
        SkewConfidence = 0x122E,
        SkewWindowX1 = 0x122F,
        SkewWindowY1 = 0x1230,
        SkewWindowX2 = 0x1231,
        SkewWindowY2 = 0x1232,
        SkewWindowX3 = 0x1233,
        SkewWindowY3 = 0x1234,
        SkewWindowX4 = 0x1235,
        SkewWindowY4 = 0x1236,
        BookName = 0x1238,  /* added 1.9 */
        ChapterNumber = 0x1239,  /* added 1.9 */
        DocumentNumber = 0x123A,  /* added 1.9 */
        PageNumber = 0x123B,  /* added 1.9 */
        Camera = 0x123C,  /* added 1.9 */
        FrameNumber = 0x123D,  /* added 1.9 */
        Frame = 0x123E,  /* added 1.9 */
        PixelFlavor = 0x123F,  /* added 1.9 */
        IccProFile = 0x1240,  /* added 1.91 */
        LastSegment = 0x1241,  /* added 1.91 */
        SegmentNumber = 0x1242,  /* added 1.91 */
        MagData = 0x1243,  /* added 2.0 */
        MagType = 0x1244,  /* added 2.0 */
        PageSide = 0x1245,
        FileSystemSource = 0x1246,
        ImageMerged = 0x1247,
        MagDataLength = 0x1248,
        PaperCount = 0x1249,
        PrinterText = 0x124A,
        TwainDirectMetadata = 0x124B // Metadata returned in TWAIN Direct JSON format
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_XFERMECH values (SX_ means Setup XFer)
    /// </summary>
    public enum TwSX : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Native = 0,
        File = 1,
        Memory = 2,
        MemFile = 4    /* added 1.91 */
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// Flags used in TW_MEMORY structure.
    /// </summary>
    [Flags]
    internal enum TwMF : uint {
        AppOwns = 0x1,
        DsmOwns = 0x2,
        DSOwns = 0x4,
        Pointer = 0x8,
        Handle = 0x10
    }

    /// <summary>
    /// ICAP_SUPPORTEDSIZES values (SS_ means Supported Sizes).
    /// </summary>
    public enum TwSS : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        A4Letter = 1,
        B5Letter = 2,
        USLetter = 3,
        USLegal = 4,
        /* Added 1.5 */
        A5 = 5,
        B4 = 6,
        B6 = 7,
        /* Added 1.7 */
        USLedger = 9,
        USExecutive = 10,
        A3 = 11,
        B3 = 12,
        A6 = 13,
        C4 = 14,
        C5 = 15,
        C6 = 16,
        /* Added 1.8 */
        _4A0 = 17,
        _2A0 = 18,
        A0 = 19,
        A1 = 20,
        A2 = 21,
        A4 = A4Letter,
        A7 = 22,
        A8 = 23,
        A9 = 24,
        A10 = 25,
        ISOB0 = 26,
        ISOB1 = 27,
        ISOB2 = 28,
        ISOB3 = B3,
        ISOB4 = B4,
        ISOB5 = 29,
        ISOB6 = B6,
        ISOB7 = 30,
        ISOB8 = 31,
        ISOB9 = 32,
        ISOB10 = 33,
        JISB0 = 34,
        JISB1 = 35,
        JISB2 = 36,
        JISB3 = 37,
        JISB4 = 38,
        JISB5 = B5Letter,
        JISB6 = 39,
        JISB7 = 40,
        JISB8 = 41,
        JISB9 = 42,
        JISB10 = 43,
        C0 = 44,
        C1 = 45,
        C2 = 46,
        C3 = 47,
        C7 = 48,
        C8 = 49,
        C9 = 50,
        C10 = 51,
        USStatement = 52,
        BusinessCard = 53
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_IMAGEFILEFORMAT values (FF_means File Format).
    /// </summary>
    public enum TwFF : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена

        /// <summary>
        /// Tagged Image File Format.
        /// </summary>
        Tiff = 0,

        /// <summary>
        /// Macintosh PICT.
        /// </summary>
        Pict = 1,

        /// <summary>
        /// Windows Bitmap.
        /// </summary>
        Bmp = 2,

        /// <summary>
        /// X-Windows Bitmap.
        /// </summary>
        Xbm = 3,

        /// <summary>
        /// Jpeg File Interchange Format.
        /// </summary>
        Jfif = 4,

        /// <summary>
        /// Flash Pix.
        /// </summary>
        Fpx = 5,

        /// <summary>
        /// Multi-page tiff file.
        /// </summary>
        TiffMulti = 6,

        Png = 7,
        Spiff = 8,
        Exif = 9,

        /// <summary>
        /// 1.91 NB: this is not PDF/A
        /// </summary>
        Pdf = 10,

        /// <summary>
        /// 1.91
        /// </summary>
        Jp2 = 11,

        /// <summary>
        /// 1.91
        /// </summary>
        Jpx = 13,

        /// <summary>
        /// 1.91
        /// </summary>
        Dejavu = 14,

        /// <summary>
        /// 2.0 Adobe PDF/A, Version 1.
        /// </summary>
        PdfA = 15,

        /// <summary>
        /// 2.1 Adobe PDF/A, Version.
        /// </summary>
        PdfA2 = 16,

        /// <summary>
        /// PDF/raster.
        /// </summary>
        PdfRaster = 17 // Added support for PDF/raster
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// Palette types for TW_PALETTE8.
    /// </summary>
    public enum TwPA : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        RGB = 0,
        Gray = 1,
        CMY = 2
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_DEVICEEVENT values (DE_ means device event).
    /// </summary>
    public enum TwDE : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        CustomEvents = 0x8000,
        CheckAutomaticCapture = 0,
        CheckBattery = 1,
        CheckDeviceOnline = 2,
        CheckFlash = 3,
        CheckPowerSupply = 4,
        CheckResolution = 5,
        DeviceAdded = 6,
        DeviceOffline = 7,
        DeviceReady = 8,
        DeviceRemoved = 9,
        ImageCaptured = 10,
        ImageDeleted = 11,
        PaperDoubleFeed = 12,
        PaperJam = 13,
        LampFailure = 14,
        PowerSave = 15,
        PowerSaveNotify = 16
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_DUPLEX values.
    /// </summary>
    public enum TwDX : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,          // TWDX_NONE
        OnePassDuplex = 1, // TWDX_1PASSDUPLEX
        TwoPassDuplex = 2  // TWDX_2PASSDUPLEX
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_AUTODISCARDBLANKPAGES values.
    /// </summary>
    public enum TwBP : int {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Disable = -2, // TWBP_DISABLE
        Auto = -1  // TWBP_AUTO
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_AUTOSIZE values.
    /// </summary>
    public enum TwAS : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        Auto = 1,
        Current = 2
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_FLIPROTATION values (FR_ means flip rotation).
    /// </summary>
    public enum TwFR : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Book = 0,
        Fanfold = 1
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_IMAGEMERGE values.
    /// </summary>
    public enum TwIM : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        FrontOnTop = 1,
        FrontOnBottom = 2,
        FrontOnLeft = 3,
        FrontOnRight = 4
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_CAMERASIDE and TWEI_PAGESIDE values.
    /// </summary>
    public enum TwCS : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Both = 0,
        Top = 1,
        Bottom = 2
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_CLEARBUFFERS values.
    /// </summary>
    public enum TwCB : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Auto = 0,
        Clear = 1,
        NoClear = 2
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_SUPPORTEDBARCODETYPES and TWEI_BARCODETYPE values.
    /// </summary>
    public enum TwBT : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Code3Of9 = 0,
        Code2Of5Interleaved = 1,
        Code2Of5NonInterleaved = 2,
        Code93 = 3,
        Code128 = 4,
        Ucc128 = 5,
        CodaBar = 6,
        Upca = 7,
        Upce = 8,
        Ean8 = 9,
        Ean13 = 10,
        PostNet = 11,
        Pdf417 = 12,
        Code2Of5Industrial = 13,
        Code2Of5Matrix = 14,
        Code2Of5DataLogic = 15,
        Code2Of5Iata = 16,
        Code3Of9FullAscii = 17,
        CodaBarWithStartStop = 18,
        MaxiCode = 19,
        QRCode = 20
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_BARCODESEARCHMODE values.
    /// </summary>
    public enum TwBD : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Horz = 0,
        Vert = 1,
        HorzVert = 2,
        VertHorz = 3
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_FILTER values.
    /// </summary>
    public enum TwFT : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Red = 0,
        Green = 1,
        Blue = 2,
        None = 3,
        White = 4,
        Cyan = 5,
        Magenta = 6,
        Yellow = 7,
        Black = 8
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_ICCPROFILE values.
    /// </summary>
    public enum TwIC : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        Link = 1,
        Embed = 2
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_PLANARCHUNKY values.
    /// </summary>
    public enum TwPC : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Chunky = 0,
        Planar = 1
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_BITORDER values.
    /// </summary>
    public enum TwBO : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        LsbFirst = 0,
        MsbFirst = 1
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_JPEGQUALITY values.
    /// </summary>
    public enum TwJQ : short {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Unknown = -4,
        Low = -3,
        Medium = -2,
        High = -1
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_JPEGSUBSAMPLING values.
    /// </summary>
    public enum TwJS : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        _444Ycbcr = 0,
        _444Rgb = 1,
        _422 = 2,
        _421 = 3,
        _411 = 4,
        _420 = 5,
        _410 = 6,
        _311 = 7
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_PIXELFLAVOR values.
    /// </summary>
    public enum TwPF : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Chocolate = 0,
        Vanilla = 1
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_FLASHUSED2 values.
    /// </summary>
    public enum TwFL : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        Off = 1,
        On = 2,
        Auto = 3,
        RedEye = 4
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_IMAGEFILTER values.
    /// </summary>
    public enum TwIF : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        Auto = 1,
        LowPass = 2,
        BandPass = 3,
        HighPass = 4,
        Text = BandPass,
        FineLine = HighPass
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_LIGHTPATH values.
    /// </summary>
    public enum TwLP : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Reflective = 0,
        Transmissive = 1
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_LIGHTSOURCE values.
    /// </summary>
    public enum TwLS : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Red = 0,
        Green = 1,
        Blue = 2,
        None = 3,
        White = 4,
        UV = 5,
        IR = 6
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_NOISEFILTER values.
    /// </summary>
    public enum TwNF : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        Auto = 1,
        LonePixel = 2,
        MajorityRule = 3
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_OVERSCAN values.
    /// </summary>
    public enum TwOV : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        Auto = 1,
        TopBottom = 2,
        LeftRight = 3,
        All = 4
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_DOUBLEFEEDDETECTION.
    /// </summary>
    public enum TwDF : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Ultrasonic = 0,
        ByLength = 1,
        Infrared = 2
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_DOUBLEFEEDDETECTIONSENSITIVITY.
    /// </summary>
    public enum TwUS : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Low = 0,
        Medium = 1,
        High = 2
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_DOUBLEFEEDDETECTIONRESPONSE.
    /// </summary>
    public enum TwDP : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Stop = 0,
        StopAndWait = 1,
        Sound = 2,
        DoNotImprint = 3
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_PRINTER values.
    /// </summary>
    public enum TwPR : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        ImprinterTopBefore = 0,
        ImprinterTopAfter = 1,
        ImprinterBottomBefore = 2,
        ImprinterBottomAfter = 3,
        EndorserTopBefore = 4,
        EndorserTopAfter = 5,
        EndorserBottomBefore = 6,
        EndorserBottomAfter = 7
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_PRINTERMODE values.
    /// </summary>
    public enum TwPM : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        SingleString = 0,
        MultiString = 1,
        CompoundString = 2
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_ORIENTATION values.
    /// </summary>
    public enum TwOR : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Rot0 = 0,
        Rot90 = 1,
        Rot180 = 2,
        Rot270 = 3,
        Portrait = Rot0,
        Landscape = Rot270,
        Auto = 4,
        AutoText = 5,
        AutoPicture = 6
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_BITDEPTHREDUCTION values.
    /// </summary>
    public enum TwBR : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Threshold = 0,
        Halftone = 1,
        CustHalftone = 2,
        Diffusion = 3,
        DynamicThreshold = 4
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_SEGMENTED values.
    /// </summary>
    public enum TwSG : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        Auto = 1,
        Manual = 2
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_FEEDERALIGNMENT values.
    /// </summary>
    public enum TwFA : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        Left = 1,
        Center = 2,
        Right = 3
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_FEEDERORDER values.
    /// </summary>
    public enum TwFO : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        FirstPageFirst = 0,
        LastPageFirst = 1
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_FEEDERPOCKET values.
    /// </summary>
    public enum TwFP : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        PocketError = 0,
        Pocket1 = 1,
        Pocket2 = 2,
        Pocket3 = 3,
        Pocket4 = 4,
        Pocket5 = 5,
        Pocket6 = 6,
        Pocket7 = 7,
        Pocket8 = 8,
        Pocket9 = 9,
        Pocket10 = 10,
        Pocket11 = 11,
        Pocket12 = 12,
        Pocket13 = 13,
        Pocket14 = 14,
        Pocket15 = 15,
        Pocket16 = 16
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_PAPERHANDLING values.
    /// </summary>
    public enum TwPH : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Normal = 0,
        Fragile = 1,
        Thick = 2,
        Trifold = 3,
        Photograph = 4
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// ICAP_FEEDERTYPE values.
    /// </summary>
    public enum TwFE : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        General = 0,
        Photo = 1,
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// TWEI_PATCHCODE values.
    /// </summary>
    public enum TwPch : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Patch1 = 0,
        Patch2 = 1,
        Patch3 = 2,
        Patch4 = 3,
        Patch6 = 4,
        PatchT = 5
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_BATTERYMINUTES values.
    /// </summary>
    public enum TwBM1 : int {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Infinite = -2,
        CannotReport = -1
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_BATTERYPERCENTAGE values.
    /// </summary>
    public enum TwBM2 : short {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Infinite = -2,
        CannotReport = -1
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_POWERSUPPLY values.
    /// </summary>
    public enum TwPS : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        External = 0,
        Battery = 1
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_JOBCONTROL values.
    /// </summary>
    public enum TwJC : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        None = 0,
        Jsic = 1,
        Jsis = 2,
        Jsxc = 3,
        Jsxs = 4
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_INDICATORSMODE values.
    /// </summary>
    public enum TwCI : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Info = 0,
        Warning = 1,
        Error = 2,
        WarmUp = 3
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    /// <summary>
    /// CAP_ALARMS values.
    /// </summary>
    public enum TwAL : ushort {
#pragma warning disable CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
        Alarm = 0,
        FeederError = 1,
        FeederWarning = 2,
        BarCode = 3,
        DoubleFeed = 4,
        Jam = 5,
        PatchCode = 6,
        Power = 7,
        Skew = 8
#pragma warning restore CS1591 // Missing XML comment for public visible type or member / Отсутствует комментарий XML для открытого видимого типа или члена
    }

    #endregion

    #region Type Definitions

    /// <summary>
    /// A string of fixed length 32 characters.
    /// <para xml:lang="ru">Строка фиксированной длинны 32 символа.</para>
    /// </summary>
    [DebuggerDisplay("{Value}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Ansi)]
    internal sealed class TwStr32 {

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 34)]
        public string Value;

        public override string ToString() {
            return this.Value;
        }

        public static implicit operator string(TwStr32 value) {
            return value != null ? value.Value : null;
        }

        public static implicit operator TwStr32(string value) {
            return new TwStr32 {
                Value = value
            };
        }
    }

    /// <summary>
    /// A fixed-length string of 64 characters.
    /// <para xml:lang="ru">Строка фиксированной длинны 64 символа.</para>
    /// </summary>
    [DebuggerDisplay("{Value}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Ansi)]
    internal sealed class TwStr64 {

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 66)]
        public string Value;

        public override string ToString() {
            return this.Value;
        }

        public static implicit operator string(TwStr64 value) {
            return value != null ? value.Value : null;
        }

        public static implicit operator TwStr64(string value) {
            return new TwStr64 {
                Value = value
            };
        }
    }

    /// <summary>
    /// A string of fixed length 128 characters.
    /// <para xml:lang="ru">Строка фиксированной длинны 128 символов.</para>
    /// </summary>
    [DebuggerDisplay("{Value}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Ansi)]
    internal sealed class TwStr128 {

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 130)]
        public string Value;

        public override string ToString() {
            return this.Value;
        }

        public static implicit operator string(TwStr128 value) {
            return value != null ? value.Value : null;
        }

        public static implicit operator TwStr128(string value) {
            return new TwStr128 {
                Value = value
            };
        }
    }

    /// <summary>
    /// A string of fixed length 255 characters.
    /// <para xml:lang="ru">Строка фиксированной длинны 255 символов.</para>
    /// </summary>
    [DebuggerDisplay("{Value}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Ansi)]
    internal sealed class TwStr255 {

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Value;

        public override string ToString() {
            return this.Value;
        }

        public static implicit operator string(TwStr255 value) {
            return value != null ? value.Value : null;
        }

        public static implicit operator TwStr255(string value) {
            return new TwStr255 {
                Value = value
            };
        }
    }

    /// <summary>
    /// A Unicode string of fixed length 512 characters.
    /// <para xml:lang="ru">Строка юникода фиксированной длинны 512 символов.</para>
    /// </summary>
    [DebuggerDisplay("{Value}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Unicode)]
    internal sealed class TwUni512 {

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public string Value;

        public override string ToString() {
            return this.Value;
        }

        public static implicit operator string(TwUni512 value) {
            return value != null ? value.Value : null;
        }

        public static implicit operator TwUni512(string value) {
            return new TwUni512 {
                Value = value
            };
        }
    }

    /// <summary>
    /// A fixed-length string of 1024 characters.
    /// <para xml:lang="ru">Строка фиксированной длинны 1024 символов.</para>
    /// </summary>
    [DebuggerDisplay("{Value}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Ansi)]
    internal sealed class TwStr1024 {

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1026)]
        public string Value;

        public override string ToString() {
            return this.Value;
        }

        public static implicit operator string(TwStr1024 value) {
            return value != null ? value.Value : null;
        }

        public static implicit operator TwStr1024(string value) {
            return new TwStr1024 {
                Value = value
            };
        }
    }

    /// <summary>
    /// Boolean value.
    /// </summary>
    [DebuggerDisplay("{ToBool()}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct TwBool {

        public ushort Value;

        private bool ToBool() {
            return this.Value != 0;
        }

        public static implicit operator bool(TwBool value) {
            return value.ToBool();
        }

        public static implicit operator TwBool(bool value) {
            return new TwBool {
                Value = (ushort)(value ? 1 : 0)
            };
        }
    }

    /// <summary>
    /// Fixed point structure type.
    /// </summary>
    [DebuggerDisplay("{ToFloat()}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct TwFix32 {									// TW_FIX32

        /// <summary>
        /// The whole part.
        /// <para xml:lang="ru">Целая часть.</para>
        /// </summary>
        public short Whole;

        /// <summary>
        /// Fractional part.
        /// <para xml:lang="ru">Дробная часть.</para>
        /// </summary>
        public ushort Frac;

        /// <summary>
        /// Coerces the type to a floating point number.
        /// <para xml:lang="ru">Приводит тип к числу с плавающей запятой.</para>
        /// </summary>
        /// <returns>Floating point number.<para xml:lang="ru">Число с плавающей точкой.</para></returns>
        private float ToFloat() {
            return (float)this.Whole + ((float)this.Frac / 65536.0f);
        }

        /// <summary>
        /// Creates an instance of TwFix32 from a floating-point number.
        /// <para xml:lang="ru">Создает экземпляр TwFix32 из числа с плавающей точкой.</para>
        /// </summary>
        /// <param name="f">Floating point number.<para xml:lang="ru">Число с плавающей точкой.</para></param>
        /// <returns>Instance of TwFix32.<para xml:lang="ru">Экземпляр TwFix32.</para></returns>
        public static implicit operator TwFix32(float f) {
            int i = (int)((f * 65536.0f) + 0.5f);
            return new TwFix32() {
                Whole = (short)(i >> 16),
                Frac = (ushort)(i & 0x0000ffff)
            };
        }

        /// <summary>
        /// Creates an instance of TwFix32 from an integer.
        /// <para xml:lang="ru">Создает экземпляр TwFix32 из целого числа.</para>
        /// </summary>
        /// <param name="value">Integer.<para xml:lang="ru">Целое число.</para></param>
        /// <returns>Instance of TwFix32.<para xml:lang="ru">Экземпляр TwFix32.</para></returns>
        public static explicit operator TwFix32(uint value) {
            return new TwFix32() {
                Whole = (short)(value & 0x0000ffff),
                Frac = (ushort)(value >> 16)
            };
        }

        /// <summary>
        /// Coerces the type to a floating point number.
        /// <para xml:lang="ru">Приводит тип к числу с плавающей запятой.</para>
        /// </summary>
        /// <param name="value">Instance of TwFix32.<para xml:lang="ru">Экземпляр TwFix32.</para></param>
        /// <returns>Floating point number.<para xml:lang="ru">Число с плавающей точкой.</para></returns>
        public static implicit operator float(TwFix32 value) {
            return value.ToFloat();
        }

        /// <summary>
        /// Converts a type to an integer.
        /// <para xml:lang="ru">Приводит тип к целому числу.</para>
        /// </summary>
        /// <param name="value">Instance of TwFix32.<para xml:lang="ru">Экземпляр TwFix32.</para></param>
        /// <returns>Integer.<para xml:lang="ru">Целое число.</para></returns>
        public static explicit operator uint(TwFix32 value) {
            return (uint)(ushort)value.Whole + ((uint)value.Frac << 16);
        }
    }

    /// <summary>
    /// Defines a frame rectangle in ICAP_UNITS coordinates.
    /// </summary>
    [DebuggerDisplay("{ToRectangle()}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct TwFrame {

        /// <summary>
        /// Gets or sets the x-coordinate of the left edge.
        /// </summary>
        public TwFix32 Left;

        /// <summary>
        /// Gets or sets the y-coordinate of the top edge.
        /// </summary>
        public TwFix32 Top;

        /// <summary>
        /// Gets or sets the x-coordinate that is the sum of TwFrame.Left and width of image.
        /// </summary>
        public TwFix32 Right;

        /// <summary>
        /// Gets or sets the y-coordinate that is the sum of TwFrame.Top and length of image.
        /// </summary>
        public TwFix32 Bottom;

        private RectangleF ToRectangle() {
            return new RectangleF(
                this.Left,
                this.Top,
                this.Right - this.Left,
                this.Bottom - this.Top);
        }

        public static implicit operator RectangleF(TwFrame value) {
            return value.ToRectangle();
        }

        public static implicit operator TwFrame(RectangleF value) {
            return new TwFrame() {
                Left = value.Left,
                Top = value.Top,
                Right = value.Right,
                Bottom = value.Bottom
            };
        }
    }

    #endregion

    #region Structure Definitions

    /// <summary>
    /// Identifies the program/library/code resource.
    /// </summary>
    [DebuggerDisplay("{ProductName}, Version = {Version.Info}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Ansi)]
    internal class TwIdentity {									// TW_IDENTITY

        /// <summary>
        /// Unique number.  In Windows, application hWnd.
        /// </summary>
        public uint Id;

        /// <summary>
        /// Identifies the piece of code
        /// </summary>
        public TwVersion Version;

        /// <summary>
        /// Application and DS must set to TWON_PROTOCOLMAJOR
        /// </summary>
        public ushort ProtocolMajor;

        /// <summary>
        /// Application and DS must set to TWON_PROTOCOLMINOR
        /// </summary>
        public ushort ProtocolMinor;

        /// <summary>
        /// Bit field OR combination of DG_ constants
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public TwDG SupportedGroups;

        /// <summary>
        /// Manufacturer name, e.g. "Hewlett-Packard"
        /// </summary>
        public TwStr32 Manufacturer;

        /// <summary>
        /// Product family name, e.g. "ScanJet"
        /// </summary>
        public TwStr32 ProductFamily;

        /// <summary>
        /// Product name, e.g. "ScanJet Plus"
        /// </summary>
        public TwStr32 ProductName;

        public override bool Equals(object obj) {
            if(obj != null && obj is TwIdentity) {
                return ((TwIdentity)obj).Id == this.Id;
            }
            return false;
        }

        public override int GetHashCode() {
            return this.Id.GetHashCode();
        }
    }

    /// <summary>
    /// Describes version of software currently running.
    /// </summary>
    [DebuggerDisplay("{Info}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Ansi)]
    internal struct TwVersion {									// TW_VERSION

        /// <summary>
        /// Major revision number of the software.
        /// </summary>
        public ushort MajorNum;

        /// <summary>
        /// Incremental revision number of the software.
        /// </summary>
        public ushort MinorNum;

        /// <summary>
        /// e.g. TWLG_SWISSFRENCH
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwLanguage Language;

        /// <summary>
        /// e.g. TWCY_SWITZERLAND
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwCountry Country;

        /// <summary>
        /// e.g. "1.0b3 Beta release"
        /// </summary>
        public TwStr32 Info;
    }

    /// <summary>
    /// Coordinates UI between application and data source.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwUserInterface {						    // TW_USERINTERFACE

        /// <summary>
        /// TRUE if DS should bring up its UI
        /// </summary>
        public TwBool ShowUI;				// bool is strictly 32 bit, so use short

        /// <summary>
        /// For Mac only - true if the DS's UI is modal
        /// </summary>
        public TwBool ModalUI;

        /// <summary>
        /// For windows only - Application window handle
        /// </summary>
        public IntPtr ParentHand;
    }

    /// <summary>
    /// Application gets detailed status info from a data source with this.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwStatus {									// TW_STATUS

        /// <summary>
        /// Any TwCC constant
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwCC ConditionCode;		// TwCC

        /// <summary>
        /// Future expansion space
        /// </summary>
        public ushort Reserved;
    }

    /// <summary>
    /// For passing events down from the application to the DS.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwEvent {									// TW_EVENT

        /// <summary>
        /// Windows pMSG or Mac pEvent.
        /// </summary>
        public IntPtr EventPtr;

        /// <summary>
        /// TwMSG from data source, e.g. TwMSG.XFerReady
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwMSG Message;
    }

    /// <summary>
    /// Application gets detailed image info from DS with this.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwImageInfo {								// TW_IMAGEINFO

        /// <summary>
        /// Resolution in the horizontal
        /// </summary>
        public TwFix32 XResolution;

        /// <summary>
        /// Resolution in the vertical
        /// </summary>
        public TwFix32 YResolution;

        /// <summary>
        /// Columns in the image, -1 if unknown by DS
        /// </summary>
        public int ImageWidth;

        /// <summary>
        /// Rows in the image, -1 if unknown by DS
        /// </summary>
        public int ImageLength;

        /// <summary>
        /// Number of samples per pixel, 3 for RGB
        /// </summary>
        public short SamplesPerPixel;

        /// <summary>
        /// Number of bits for each sample
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public short[] BitsPerSample;

        /// <summary>
        /// Number of bits for each padded pixel
        /// </summary>
        public short BitsPerPixel;

        /// <summary>
        /// True if Planar, False if chunky
        /// </summary>
        public TwBool Planar;

        /// <summary>
        /// How to interp data; photo interp
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwPixelType PixelType;

        /// <summary>
        /// How the data is compressed
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwCompression Compression;
    }

    /// <summary>
    /// This structure is used to pass specific information between the data source and the application.
    /// </summary>
    [DebuggerDisplay("InfoId = {InfoId}, ItemType = {ItemType}, ReturnCode = {ReturnCode}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwInfo : IDisposable {

        /// <summary>
        /// Tag identifying an information.
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwEI InfoId;

        /// <summary>
        /// Item data type.
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwType ItemType;

        /// <summary>
        /// Number of items for this field.
        /// </summary>
        public ushort NumItems;

        /// <summary>
        /// This is the return code of availability of data for extended image attribute requested.
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwRC ReturnCode;

        /// <summary>
        /// Contains either data or a handle to data.
        /// </summary>
        public IntPtr Item;

        /// <summary>
        /// Returns true if the element value is an unmanaged memory descriptor; otherwise false.
        /// <para xml:lang="ru">Возвращает true, если значение элемента является дескриптором неуправляемой памяти; иначе, false.</para>
        /// </summary>
        private bool _IsValue {
            get {
                return this.ItemType != TwType.Handle && TwTypeHelper.SizeOf(this.ItemType) * this.NumItems <= TwTypeHelper.SizeOf(TwType.Handle);
            }
        }

        /// <summary>
        /// Returns the value of an element.
        /// <para xml:lang="ru">Возвращает значение элемента.</para>
        /// </summary>
        /// <returns>The value of the item.<para xml:lang="ru">Значение элемента.</para></returns>
        public object GetValue() {
            var _result = new object[this.NumItems];
            if(this._IsValue) {
                for(long i = 0, _data = this.Item.ToInt64(), _mask = ((1L << TwTypeHelper.SizeOf(this.ItemType) * 7) << TwTypeHelper.SizeOf(this.ItemType)) - 1; i < this.NumItems; i++, _data >>= TwTypeHelper.SizeOf(this.ItemType) * 8) {
                    _result[i] = TwTypeHelper.CastToCommon(this.ItemType, TwTypeHelper.ValueToTw<long>(this.ItemType, _data & _mask));
                }
            } else {
                IntPtr _data = Twain32._Memory.Lock(this.Item);
                try {
                    for(int i = 0; i < this.NumItems; i++) {
                        if(this.ItemType != TwType.Handle) {
                            _result[i] = TwTypeHelper.CastToCommon(this.ItemType, Marshal.PtrToStructure((IntPtr)((long)_data + (TwTypeHelper.SizeOf(this.ItemType) * i)), TwTypeHelper.TypeOf(this.ItemType)));
                        } else {
                            _result[i] = Marshal.PtrToStringAnsi(_data);
                            _data = (IntPtr)((long)_data + _result[i].ToString().Length + 1);
                        }
                    }
                } finally {
                    Twain32._Memory.Unlock(this.Item);
                }
            }
            return _result.Length == 1 ? _result[0] : _result;
        }

        #region IDisposable

        public void Dispose() {
            if(this.Item != IntPtr.Zero && !this._IsValue) {
                Twain32._Memory.Free(this.Item);
                this.Item = IntPtr.Zero;
            }
        }

        #endregion
    }

    /// <summary>
    /// This structure is used to pass extended image information from the Data Source to the Application at the end of State 7.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwExtImageInfo {

        /// <summary>
        /// The number of elements in the extended image description.
        /// <para xml:lang="ru">Количество элементов расширенного описания изображения.</para>
        /// </summary>
        public uint NumInfos;

        //[MarshalAs(UnmanagedType.ByValArray,SizeConst=1)]
        //public TwInfo[] Info;

        /// <summary>
        /// Converts to an unmanaged memory block.
        /// <para xml:lang="ru">Выполняет преображование в неуправляемый блок памяти.</para>
        /// </summary>
        /// <param name="info">A set of elements for an extended image description.<para xml:lang="ru">Набор элементов расширенного описания изображения.</para></param>
        /// <returns>Pointer to an unmanaged memory block.<para xml:lang="ru">Указатель на блок неуправляемой памяти.</para></returns>
        public static IntPtr ToPtr(TwInfo[] info) {
            var _twExtImageInfoSize = Marshal.SizeOf(typeof(TwExtImageInfo));
            var _twInfoSize = Marshal.SizeOf(typeof(TwInfo));
            var _data = Marshal.AllocHGlobal(_twExtImageInfoSize + (_twInfoSize * info.Length));
            Marshal.StructureToPtr(new TwExtImageInfo { NumInfos = (uint)info.Length }, _data, true);
            for(int i = 0; i < info.Length; i++) {
                Marshal.StructureToPtr(info[i], (IntPtr)(_data.ToInt64() + _twExtImageInfoSize + (_twInfoSize * i)), true);
            }
            return _data;
        }
    }

    /// <summary>
    /// Provides image layout information in current units.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwImageLayout {

        /// <summary>
        /// Frame coords within larger document.
        /// </summary>
        public TwFrame Frame;

        /// <summary>
        /// Document Number.
        /// <para xml:lang="ru">Номер документа.</para>
        /// </summary>
        public uint DocumentNumber;

        /// <summary>
        /// Page number.
        /// <para xml:lang="ru">Номер страницы.</para>
        /// </summary>
        public uint PageNumber; //Reset when you go to next document

        /// <summary>
        /// Frame number.
        /// <para xml:lang="ru">Номер кадра.</para>
        /// </summary>
        public uint FrameNumber; //Reset when you go to next page
    }

    /// <summary>
    /// Used with TwMSG.EndXfer to indicate additional data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwPendingXfers {								// TW_PENDINGXFERS
        public ushort Count;
        public uint EOJ;
    }

    /// <summary>
    /// Used by application to get/set capability from/in a data source.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwCapability : IDisposable {					// TW_CAPABILITY

        /// <summary>
        /// Id of capability to set or get, e.g. TwCap.Brightness
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwCap Cap;

        /// <summary>
        /// TwOn.One, TwOn.Range, TwOn.Array or TwOn.Enum
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwOn ConType;

        /// <summary>
        /// Handle to container of type Dat
        /// </summary>
        public IntPtr Handle;

        private TwCapability() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwCapability"/> class.
        /// </summary>
        /// <param name="cap">The cap.</param>
        public TwCapability(TwCap cap) {
            this.Cap = cap;
            this.ConType = TwOn.DontCare;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwCapability"/> class.
        /// </summary>
        /// <param name="cap">The cap.</param>
        /// <param name="value">The value.</param>
        /// <param name="type">The type.</param>
        public TwCapability(TwCap cap, uint value, TwType type) {
            this.Cap = cap;
            this.ConType = TwOn.One;
            this._SetValue(new TwOneValue() {
                ItemType = type,
                Item = value
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwCapability"/> class.
        /// </summary>
        /// <param name="cap">The cap.</param>
        /// <param name="value">The value.</param>
        /// <param name="type">The type.</param>
        public TwCapability(TwCap cap, string value, TwType type) {
            this.Cap = cap;
            this.ConType = TwOn.One;
            int _twOneCustumValueSize = Marshal.SizeOf(typeof(TwOneCustumValue));
            this.Handle = Twain32._Memory.Alloc(_twOneCustumValueSize + Marshal.SizeOf(TwTypeHelper.TypeOf(type)));
            IntPtr _ptr = Twain32._Memory.Lock(this.Handle);
            try {
                Marshal.StructureToPtr(new TwOneCustumValue { ItemType = type }, _ptr, true);
                Marshal.StructureToPtr(TwTypeHelper.CastToTw(type, value), (IntPtr)(_ptr.ToInt64() + _twOneCustumValueSize), true);
            } finally {
                Twain32._Memory.Unlock(this.Handle);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwCapability"/> class.
        /// </summary>
        /// <param name="cap">The cap.</param>
        /// <param name="range">The range.</param>
        public TwCapability(TwCap cap, TwRange range) {
            this.Cap = cap;
            this.ConType = TwOn.Range;
            this._SetValue(range);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwCapability"/> class.
        /// </summary>
        /// <param name="cap">The cap.</param>
        /// <param name="array">The array.</param>
        /// <param name="arrayValue">The array value.</param>
        public TwCapability(TwCap cap, TwArray array, object[] arrayValue) {
            this.Cap = cap;
            this.ConType = TwOn.Array;
            int _twArraySize = Marshal.SizeOf(typeof(TwArray));
            int _twItemSize = Marshal.SizeOf(TwTypeHelper.TypeOf(array.ItemType));
            this.Handle = Twain32._Memory.Alloc(_twArraySize + (_twItemSize * arrayValue.Length));
            IntPtr _pTwArray = Twain32._Memory.Lock(this.Handle);
            try {
                Marshal.StructureToPtr(array, _pTwArray, true);
                for(long i = 0, _ptr = _pTwArray.ToInt64() + _twArraySize; i < arrayValue.Length; i++, _ptr += _twItemSize) {
                    Marshal.StructureToPtr(TwTypeHelper.CastToTw(array.ItemType, arrayValue[i]), (IntPtr)_ptr, true);
                }
            } finally {
                Twain32._Memory.Unlock(this.Handle);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwCapability"/> class.
        /// </summary>
        /// <param name="cap">The cap.</param>
        /// <param name="enumeration">The enumeration.</param>
        /// <param name="enumerationValue">The enumeration value.</param>
        public TwCapability(TwCap cap, TwEnumeration enumeration, object[] enumerationValue) {
            this.Cap = cap;
            this.ConType = TwOn.Enum;
            int _twEnumerationSize = Marshal.SizeOf(typeof(TwEnumeration));
            int _twItemSize = Marshal.SizeOf(TwTypeHelper.TypeOf(enumeration.ItemType));
            this.Handle = Twain32._Memory.Alloc(_twEnumerationSize + (_twItemSize * enumerationValue.Length));
            IntPtr _pTwEnumeration = Twain32._Memory.Lock(this.Handle);
            try {
                Marshal.StructureToPtr(enumeration, _pTwEnumeration, true);
                for(long i = 0, _ptr = _pTwEnumeration.ToInt64() + _twEnumerationSize; i < enumerationValue.Length; i++, _ptr += _twItemSize) {
                    Marshal.StructureToPtr(TwTypeHelper.CastToTw(enumeration.ItemType, enumerationValue[i]), (IntPtr)_ptr, true);
                }
            } finally {
                Twain32._Memory.Unlock(this.Handle);
            }
        }

        /// <summary>
        /// Returns the result for the specified feature.
        /// <para xml:lang="ru">Возвращает результат для указаной возможности.</para>
        /// </summary>
        /// <returns>Instance TwArray, TwEnumeration, _TwRange or _TwOneValue.<para xml:lang="ru">Экземпляр TwArray, TwEnumeration, _TwRange или _TwOneValue.</para></returns>
        public object GetValue() {
            IntPtr _ptr = Twain32._Memory.Lock(this.Handle);
            try {
                switch(this.ConType) {
                    case TwOn.Array:
                        return new __TwArray((TwArray)Marshal.PtrToStructure(_ptr, typeof(TwArray)), (IntPtr)(_ptr.ToInt64() + Marshal.SizeOf(typeof(TwArray))));
                    case TwOn.Enum:
                        return new __TwEnumeration((TwEnumeration)Marshal.PtrToStructure(_ptr, typeof(TwEnumeration)), (IntPtr)(_ptr.ToInt64() + Marshal.SizeOf(typeof(TwEnumeration))));
                    case TwOn.Range:
                        return Marshal.PtrToStructure(_ptr, typeof(TwRange));
                    case TwOn.One:
                        TwOneCustumValue _value = Marshal.PtrToStructure(_ptr, typeof(TwOneCustumValue)) as TwOneCustumValue;
                        switch(_value.ItemType) {
                            case TwType.Str32:
                            case TwType.Str64:
                            case TwType.Str128:
                            case TwType.Str255:
                            case TwType.Str1024:
                            case TwType.Uni512:
                                return Marshal.PtrToStructure((IntPtr)(_ptr.ToInt64() + Marshal.SizeOf(typeof(TwOneCustumValue))), TwTypeHelper.TypeOf(_value.ItemType)).ToString();
                            default:
                                return Marshal.PtrToStructure(_ptr, typeof(TwOneValue));
                        }
                }
                return null;
            } finally {
                Twain32._Memory.Unlock(this.Handle);
            }
        }

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            if(this.Handle != IntPtr.Zero) {
                Twain32._Memory.Free(this.Handle);
                this.Handle = IntPtr.Zero;
            }
        }

        #endregion

        private void _SetValue<T>(T value) {
            this.Handle = Twain32._Memory.Alloc(Marshal.SizeOf(typeof(T)));
            IntPtr _ptr = Twain32._Memory.Lock(this.Handle);
            try {
                Marshal.StructureToPtr(value, _ptr, true);
            } finally {
                Twain32._Memory.Unlock(this.Handle);
            }
        }
    }

    internal interface ITwArray {

        TwType ItemType {
            get;
            set;
        }

        uint NumItems {
            get;
            set;
        }
    }

    /// <summary>
    /// Container for array of values.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwArray : ITwArray {                                    //TWON_ARRAY. Container for array of values (a simplified TW_ENUMERATION)
        [MarshalAs(UnmanagedType.U2)]
        private TwType _itemType;
        private uint _numItems;    /* How many items in ItemList           */
        //[MarshalAs(UnmanagedType.ByValArray,SizeConst=1)]
        //public byte[] ItemList; /* Array of ItemType values starts here */

        public TwType ItemType {
            get {
                return this._itemType;
            }
            set {
                this._itemType = value;
            }
        }

        public uint NumItems {
            get {
                return this._numItems;
            }
            set {
                this._numItems = value;
            }
        }
    }

    /// <summary>
    /// Container for a collection of values.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwEnumeration : ITwArray {                              //TWON_ENUMERATION. Container for a collection of values.
        [MarshalAs(UnmanagedType.U2)]
        private TwType _ItemType;
        private uint _numItems;     /* How many items in ItemList                 */
        private uint _currentIndex; /* Current value is in ItemList[CurrentIndex] */
        private uint _defaultIndex; /* Powerup value is in ItemList[DefaultIndex] */
        //[MarshalAs(UnmanagedType.ByValArray,SizeConst=1)]
        //public byte[] ItemList;  /* Array of ItemType values starts here       */

        public TwType ItemType {
            get {
                return this._ItemType;
            }
            set {
                this._ItemType = value;
            }
        }

        public uint NumItems {
            get {
                return this._numItems;
            }
            set {
                this._numItems = value;
            }
        }

        public uint CurrentIndex {
            get {
                return this._currentIndex;
            }
            set {
                this._currentIndex = value;
            }
        }

        public uint DefaultIndex {
            get {
                return this._defaultIndex;
            }
            set {
                this._defaultIndex = value;
            }
        }
    }

    /// <summary>
    /// Container for one value.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwOneValue {                                 //TW_ONEVALUE. Container for one value.
        [MarshalAs(UnmanagedType.U2)]
        public TwType ItemType;
        public uint Item;
    }

    /// <summary>
    /// Container for one custom value.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwOneCustumValue {                                 //TW_ONEVALUE. Container for one value.
        [MarshalAs(UnmanagedType.U2)]
        public TwType ItemType;
    }

    /// <summary>
    /// Container for a range of values.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwRange {                                    //TWON_RANGE. Container for a range of values.
        [MarshalAs(UnmanagedType.U2)]
        public TwType ItemType;
        public uint MinValue;     /* Starting value in the range.           */
        public uint MaxValue;     /* Final value in the range.              */
        public uint StepSize;     /* Increment from MinValue to MaxValue.   */
        public uint DefaultValue; /* Power-up value.                        */
        public uint CurrentValue; /* The value that is currently in effect. */
    }

    /// <summary>
    /// Sets up DS to application data transfer via a memory buffer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwSetupMemXfer {

        /// <summary>
        /// Minimum buffer size in bytes
        /// </summary>
        public uint MinBufSize;

        /// <summary>
        /// Maximum buffer size in bytes
        /// </summary>
        public uint MaxBufSize;

        /// <summary>
        /// Preferred buffer size in bytes
        /// </summary>
        public uint Preferred;
    }

    /// <summary>
    /// Used to pass image data (e.g. in strips) from DS to application.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwImageMemXfer {

        public TwImageMemXfer() {
            this.Compression = (TwCompression)ushort.MaxValue;
            this.BytesPerRow = this.BytesWritten = this.Columns = this.Rows = this.XOffset = this.YOffset = uint.MaxValue;
        }

        /// <summary>
        /// How the data is compressed.
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwCompression Compression;

        /// <summary>
        /// Number of bytes in a row of data.
        /// </summary>
        public uint BytesPerRow;

        /// <summary>
        /// How many columns.
        /// </summary>
        public uint Columns;

        /// <summary>
        /// How many rows.
        /// </summary>
        public uint Rows;

        /// <summary>
        /// How far from the side of the image.
        /// </summary>
        public uint XOffset;

        /// <summary>
        /// How far from the top of the image.
        /// </summary>
        public uint YOffset;

        /// <summary>
        /// How many bytes written in Memory.
        /// </summary>
        public uint BytesWritten;

        /// <summary>
        /// Mem struct used to pass actual image data.
        /// </summary>
        public TwMemory Memory;
    }

    /// <summary>
    /// Used to manage memory buffers.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwMemory {

        /// <summary>
        /// Any combination of the TWMF_ constants.
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public TwMF Flags;

        /// <summary>
        /// Number of bytes stored in buffer TheMem.
        /// </summary>
        public uint Length;

        /// <summary>
        /// Pointer or handle to the allocated memory buffer.
        /// </summary>
        public IntPtr TheMem;
    }

    /// <summary>
    /// Sets up DS to application data transfer via a file.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwSetupFileXfer {

        /// <summary>
        /// File to contain data.
        /// </summary>
        public TwStr255 FileName;

        /// <summary>
        /// A TWFF_xxxx constant.
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwFF Format;

        /// <summary>
        /// Used for Macintosh only.
        /// </summary>
        public IntPtr VrefNum;
    }

    /// <summary>
    /// DAT_PALETTE8. Color palette when TWPT_PALETTE pixels xfer'd in mem buf.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwPalette8 {

        /// <summary>
        /// Number of colors in the color table.
        /// </summary>
        public ushort NumColors;

        /// <summary>
        /// TWPA_xxxx, specifies type of palette.
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwPA PaletteType;

        /// <summary>
        /// Array of palette values starts here.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public TwElement8[] Colors;

        public static implicit operator Twain32.ColorPalette(TwPalette8 palette) {
            return Twain32.ColorPalette.Create(palette);
        }

        public static implicit operator TwPalette8(Twain32.ColorPalette palette) {
            TwPalette8 _result = new TwPalette8 {
                PaletteType = palette.PaletteType,
                NumColors = (ushort)palette.Colors.Length,
                Colors = new TwElement8[256]
            };
            for(int i = 0; i < palette.Colors.Length; i++) {
                _result.Colors[i] = palette.Colors[i];
            }
            return _result;
        }
    }

    /// <summary>
    /// No DAT needed.
    /// </summary>
    [DebuggerDisplay("RGB=({Channel1},{Channel2},{Channel3}), Index={Index}")]
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct TwElement8 {

        /// <summary>
        /// Value used to index into the color table.
        /// </summary>
        public byte Index;

        /// <summary>
        /// First  tri-stimulus value (e.g Red).
        /// </summary>
        public byte Channel1;

        /// <summary>
        /// Second tri-stimulus value (e.g Green).
        /// </summary>
        public byte Channel2;

        /// <summary>
        /// Third  tri-stimulus value (e.g Blue).
        /// </summary>
        public byte Channel3;

        public static implicit operator Color(TwElement8 element) {
            return Color.FromArgb(element.Channel1, element.Channel2, element.Channel3);
        }

        public static implicit operator TwElement8(Color color) {
            return new TwElement8 {
                Channel1 = color.R,
                Channel2 = color.G,
                Channel3 = color.B
            };
        }
    }

    /// <summary>
    /// DAT_DEVICEEVENT, information about events.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwDeviceEvent {

        /// <summary>
        /// One of the TWDE_xxxx values.
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public TwDE Event;

        private ushort reserved;

        /// <summary>
        /// The name of the device that generated the event.
        /// </summary>
        public TwStr255 DeviceName;

        /// <summary>
        /// Battery Minutes Remaining.
        /// </summary>
        public uint BatteryMinutes;

        /// <summary>
        /// Battery Percentage Remaining.
        /// </summary>
        public short BatteryPercentAge;

        /// <summary>
        /// Power Supply.
        /// </summary>
        public int PowerSupply;

        /// <summary>
        /// Resolution.
        /// </summary>
        public TwFix32 XResolution;

        /// <summary>
        /// Resolution.
        /// </summary>
        public TwFix32 YResolution;

        /// <summary>
        /// Flash Used2.
        /// </summary>
        public uint FlashUsed2;

        /// <summary>
        /// Automatic Capture.
        /// </summary>
        public uint AutomaticCapture;

        /// <summary>
        /// Automatic Capture.
        /// </summary>
        public uint TimeBeforeFirstCapture;

        /// <summary>
        /// Automatic Capture.
        /// </summary>
        public uint TimeBetweenCaptures;
    }

    /// <summary>
    /// Used to register callbacks.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwCallback {

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public CallBackProc CallBackProc;

        public uint RefCon;

        public short Message;
    }

    /// <summary>
    /// Used to register callbacks.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwCallback2 {

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public CallBackProc CallBackProc;

        public UIntPtr RefCon;

        public short Message;
    }

    /// <summary>
    /// Allows for a data source and application to pass custom data to each other.
    /// </summary>
    /// <remarks>TW_CUSTOMDSDATA</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwCustomDSData {

        public uint InfoLength;

        public IntPtr hData;
    }

    [return: MarshalAs(UnmanagedType.U2)]
    internal delegate TwRC CallBackProc(TwIdentity srcId, TwIdentity appId, [MarshalAs(UnmanagedType.U4)] TwDG dg, [MarshalAs(UnmanagedType.U2)] TwDAT dat, [MarshalAs(UnmanagedType.U2)] TwMSG msg, IntPtr data);

    #endregion

    #region Internal Type Definitions

    internal interface __ITwArray {

        TwType ItemType {
            get;
        }

        uint NumItems {
            get;
        }

        object[] Items {
            get;
        }
    }

    internal interface __ITwEnumeration : __ITwArray {

        int CurrentIndex {
            get;
        }

        int DefaultIndex {
            get;
        }
    }

    /// <summary>
    /// Container for array of values.
    /// </summary>
    internal class __TwArray : __ITwArray {
        private ITwArray _data;
        private object[] _items;

        internal __TwArray(ITwArray data, IntPtr items) {
            this._data = data;
            this._items = new object[this._data.NumItems];
            for(long i = 0, _offset = 0, _sizeof = TwTypeHelper.SizeOf(this._data.ItemType); i < this._data.NumItems; i++, _offset += _sizeof) {
                this._items[i] = TwTypeHelper.CastToCommon(this._data.ItemType, Marshal.PtrToStructure((IntPtr)(items.ToInt64() + _offset), TwTypeHelper.TypeOf(this._data.ItemType)));
            }
        }

        public TwType ItemType {
            get {
                return this._data.ItemType;
            }
        }

        public uint NumItems {
            get {
                return this._data.NumItems;
            }
        }

        public object[] Items {
            get {
                return this._items;
            }
        }
    }

    /// <summary>
    /// Container for a collection of values.
    /// </summary>
    internal class __TwEnumeration : __TwArray, __ITwEnumeration {
        private TwEnumeration _data;

        internal __TwEnumeration(TwEnumeration data, IntPtr items) : base(data, items) {
            this._data = data;
        }

        public int CurrentIndex {
            get {
                return (int)this._data.CurrentIndex;
            }
        }

        public int DefaultIndex {
            get {
                return (int)this._data.DefaultIndex;
            }
        }
    }

    #endregion

    #region Entry Points

    /// <summary>
    /// DAT_ENTRYPOINT. returns essential entry points.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwEntryPoint {

        public TwEntryPoint() {
            this.Size = Marshal.SizeOf(this);
        }

        /// <summary>
        /// Size of the structure in bytes. 
        /// The application must set this before calling MSG_GET. 
        /// The Source should examine this when processing a MSG_SET.
        /// </summary>
        public int Size;

        /// <summary>
        /// A pointer to the DSM_Entry function. 
        /// TWAIN 2.0 Sources must use this value instead of getting it themselves.
        /// </summary>
        public IntPtr DSM_Entry;

        /// <summary>
        /// A pointer to the memory allocation function, taking the form TW_HANDLE PASCAL DSM_MemAllocate (TW_UINT32).
        /// </summary>
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public DSM_MemoryAllocate MemoryAllocate;

        /// <summary>
        /// A pointer to the memory free function, taking the form void PASCAL DSM_MemAllocate (TW_HANDLE).
        /// </summary>
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public DSM_MemoryFree MemoryFree;

        /// <summary>
        /// A pointer to the memory lock function, taking the form TW_MEMREF PASCAL DSM_MemAllocate (TW_HANDLE).
        /// </summary>
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public DSM_MemoryLock MemoryLock;

        /// <summary>
        /// A pointer to the memory unlock function, taking the form void PASCAL DSM_MemUnlock (TW_HANDLE).
        /// </summary>
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public DSM_MemoryUnlock MemoryUnlock;
    }

    internal delegate IntPtr DSM_MemoryAllocate(int size);

    internal delegate void DSM_MemoryFree(IntPtr handle);

    internal delegate IntPtr DSM_MemoryLock(IntPtr handle);

    internal delegate void DSM_MemoryUnlock(IntPtr handle);

    #endregion
}