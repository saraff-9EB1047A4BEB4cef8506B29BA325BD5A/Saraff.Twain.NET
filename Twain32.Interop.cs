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
        /// Entry points for working with DSM.
        /// <para xml:lang="ru">Точки входа для работы с DSM.</para>
        /// </summary>
        private sealed class _DsmEntry {

            /// <summary>
            /// Initializes a new instance of the class <see cref="_DsmEntry"/>.
            /// <para xml:lang="ru">Инициализирует новый экземпляр класса <see cref="_DsmEntry"/>.</para>
            /// </summary>
            /// <param name="ptr">Pointer to DSM_Entry.<para xml:lang="ru">Указатель на DSM_Entry.</para></param>
            private _DsmEntry(IntPtr ptr) {
                if(ptr != IntPtr.Zero) {
                    this.DsmParent = _DsmEntry.CreateDelegate<_DSMparent>(ptr);
                    this.DsmRaw = _DsmEntry.CreateDelegate<_DSMraw>(ptr);
                    this.DSImageXfer = _DsmEntry.CreateDelegate<_DSixfer>(ptr);
                    this.DsRaw = _DsmEntry.CreateDelegate<_DSraw>(ptr);
                    return;
                }
                switch(_Platform.Platform) {
                    case PlatformID.Unix:
                        this.DsmParent = _DsmEntry._LinuxDsmParent;
                        this.DsmRaw = _DsmEntry._LinuxDsmRaw;
                        this.DSImageXfer = _DsmEntry._LinuxDsImageXfer;
                        this.DsRaw = _DsmEntry._LinuxDsRaw;
                        break;
                    case PlatformID.MacOSX:
                        this.DsmParent = _DsmEntry._MacosxDsmParent;
                        this.DsmRaw = _DsmEntry._MacosxDsmRaw;
                        this.DSImageXfer = _DsmEntry._MacosxDsImageXfer;
                        this.DsRaw = _DsmEntry._MacosxDsRaw;
                        break;
                    default:
                        throw new ArgumentException("DSM_Entry pointer is not specified.", nameof(ptr));
                }
            }

            /// <summary>
            /// Creates and returns a new instance of the class <see cref="_DsmEntry"/>.
            /// <para xml:lang="ru">Создает и возвращает новый экземпляр класса <see cref="_DsmEntry"/>.</para>
            /// </summary>
            /// <param name="ptr">Pointer to DSM_Entry.<para xml:lang="ru">Указатель на DSM_Entry.</para></param>
            /// <returns>Class instance <see cref="_DsmEntry"/>.<para xml:lang="ru">Экземпляр класса <see cref="_DsmEntry"/>.</para></returns>
            public static _DsmEntry Create(IntPtr ptr) => new _DsmEntry(ptr);

            /// <summary>
            /// Casts a pointer to the requested delegate.
            /// <para xml:lang="ru">Приводит указатель к требуемомы делегату.</para>
            /// </summary>
            /// <typeparam name="T">Требуемый делегат.</typeparam>
            /// <param name="ptr">Pointer to DSM_Entry.<para xml:lang="ru">Указатель на DSM_Entry.</para></param>
            /// <returns>Delegate.<para xml:lang="ru">Делегат.</para></returns>
            private static T CreateDelegate<T>(IntPtr ptr) where T : class => Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)) as T;

            public TwRC DsmInvoke<T>(TwIdentity origin, TwDG dg, TwDAT dat, TwMSG msg, ref T data) where T : class {
                if(data == null) {
                    throw new ArgumentNullException();
                }
                IntPtr _data = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
                try {
                    Marshal.StructureToPtr(data, _data, true);

                    TwRC _rc = this.DsmRaw(origin, IntPtr.Zero, dg, dat, msg, _data);
                    if(_rc == TwRC.Success) {
                        data = (T)Marshal.PtrToStructure(_data, typeof(T));
                    }
                    return _rc;
                } finally {
                    Marshal.FreeHGlobal(_data);
                }
            }

            public TwRC DsInvoke<T>(TwIdentity origin, TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref T data) where T : class {
                if(data == null) {
                    throw new ArgumentNullException();
                }
                IntPtr _data = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
                try {
                    Marshal.StructureToPtr(data, _data, true);

                    TwRC _rc = this.DsRaw(origin, dest, dg, dat, msg, _data);
                    if(_rc == TwRC.Success || _rc == TwRC.DSEvent || _rc == TwRC.XferDone) {
                        data = (T)Marshal.PtrToStructure(_data, typeof(T));
                    }
                    return _rc;
                } finally {
                    Marshal.FreeHGlobal(_data);
                }
            }

            #region Properties

            public _DSMparent DsmParent { get; private set; }

            public _DSMraw DsmRaw { get; private set; }

            public _DSixfer DSImageXfer { get; private set; }

            public _DSraw DsRaw { get; private set; }

            #endregion

            #region import libtwaindsm.so (Unix)

            [DllImport("/usr/local/lib/libtwaindsm.so", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _LinuxDsmParent([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr refptr);

            [DllImport("/usr/local/lib/libtwaindsm.so", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _LinuxDsmRaw([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, IntPtr rawData);

            [DllImport("/usr/local/lib/libtwaindsm.so", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _LinuxDsImageXfer([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr hbitmap);

            [DllImport("/usr/local/lib/libtwaindsm.so", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _LinuxDsRaw([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, IntPtr arg);

            #endregion

            #region import TWAIN.framework/TWAIN (MacOSX)

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsmParent([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr refptr);

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsmRaw([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, IntPtr rawData);

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsImageXfer([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr hbitmap);

            [DllImport("/System/Library/Frameworks/TWAIN.framework/TWAIN", EntryPoint = "DSM_Entry", CharSet = CharSet.Ansi)]
            private static extern TwRC _MacosxDsRaw([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, IntPtr arg);

            #endregion

        }

        /// <summary>
        /// Win32 message filter.
        /// <para xml:lang="ru">Фильтр win32-сообщений.</para>
        /// </summary>
        private sealed class _MessageFilter : IMessageFilter, IDisposable {
            private Twain32 _twain;
            private bool _is_set_filter = false;
            private TwEvent _evtmsg = new TwEvent();

            public _MessageFilter(Twain32 twain) {
                this._twain = twain;
                this._evtmsg.EventPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINMSG)));
            }

            #region IMessageFilter

            public bool PreFilterMessage(ref Message m) {
                try {
                    if(this._twain._srcds.Id == 0) {
                        return false;
                    }
                    Marshal.StructureToPtr(new WINMSG { hwnd = m.HWnd, message = m.Msg, wParam = m.WParam, lParam = m.LParam }, this._evtmsg.EventPtr, true);
                    this._evtmsg.Message = TwMSG.Null;

                    switch(this._twain._dsmEntry.DsInvoke(this._twain._AppId, this._twain._srcds, TwDG.Control, TwDAT.Event, TwMSG.ProcessEvent, ref this._evtmsg)) {
                        case TwRC.DSEvent:
                            this._twain._TwCallbackProcCore(this._evtmsg.Message, isCloseReq => {
                                if(isCloseReq || this._twain.DisableAfterAcquire) {
                                    this._RemoveFilter();
                                    this._twain._DisableDataSource();
                                }
                            });
                            break;
                        case TwRC.NotDSEvent:
                            return false;
                        case TwRC.Failure:
                            throw new TwainException(this._twain._GetTwainStatus(), TwRC.Failure);
                        default:
                            throw new InvalidOperationException("Получен неверный код результата операции. Invalid a Return Code value.");
                    }
                } catch(TwainException ex) {
                    this._twain._OnAcquireError(new AcquireErrorEventArgs(ex));
                } catch(Exception ex) {
                    this._twain._OnAcquireError(new AcquireErrorEventArgs(new TwainException(ex.Message, ex)));
                }
                return true;
            }

            #endregion

            #region IDisposable

            public void Dispose() {
                if(this._evtmsg != null && this._evtmsg.EventPtr != IntPtr.Zero) {
                    Marshal.FreeHGlobal(this._evtmsg.EventPtr);
                    this._evtmsg.EventPtr = IntPtr.Zero;
                }
            }

            #endregion

            public void SetFilter() {
                if(!this._is_set_filter) {
                    this._is_set_filter = true;
                    Application.AddMessageFilter(this);
                }
            }

            private void _RemoveFilter() {
                Application.RemoveMessageFilter(this);
                this._is_set_filter = false;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 2)]
            internal struct WINMSG {
                public IntPtr hwnd;
                public int message;
                public IntPtr wParam;
                public IntPtr lParam;
            }
        }
    }
}
