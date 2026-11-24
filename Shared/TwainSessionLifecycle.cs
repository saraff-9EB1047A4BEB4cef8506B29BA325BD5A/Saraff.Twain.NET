using System;

#if TWAINX
namespace Saraff.TwainX {

    public sealed partial class TwainX {
#else
namespace Saraff.Twain {

    public sealed partial class Twain32 {
#endif

        /// <summary>
        /// Opens the data source manager.
        /// <para xml:lang="ru">Открывает менеджер источников данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool OpenDSM() {
            this._CheckThread();
            if(!this._session.IsDsmOpen) {
                this._LoadDsm();

                IntPtr _hwnd = this._GetDsmOpenParentHandle();
                for(TwRC _rc = this._dsmEntry.DsmParent(this._AppId, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.OpenDSM, ref _hwnd); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                this._session.OpenDsm();
                this._SyncTwainState();

                if(this.IsTwain2Supported) {
                    TwEntryPoint _entry = new TwEntryPoint();
                    for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.EntryPoint, TwMSG.Get, ref _entry); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    _Memory._SetEntryPoints(_entry);
                } else if(this._RequireTwain2OnOpen) {
                    throw new TwainException("DSM don't supported TWAIN 2.x specification.");
                }

                this._GetAllSorces();
            }
            return this._session.IsDsmOpen;
        }

        /// <summary>
        /// Displays a dialog box for selecting a data source.
        /// <para xml:lang="ru">Отображает диалоговое окно для выбора источника данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool SelectSource() {
            this._CheckThread();
            if(_Platform.Platform == PlatformID.Unix) {
                throw new NotSupportedException("DG_CONTROL / DAT_IDENTITY / MSG_USERSELECT is not available on Linux.");
            }
            if(!this._session.IsDsOpen) {
                if(!this._session.IsDsmOpen) {
                    this.OpenDSM();
                    if(!this._session.IsDsmOpen) {
                        return false;
                    }
                }
                TwIdentity _src = new TwIdentity();
                for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.UserSelect, ref _src); _rc != TwRC.Success;) {
                    if(_rc == TwRC.Cancel) {
                        return false;
                    }
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                this._srcds = _src;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Opens a data source.
        /// <para xml:lang="ru">Открывает источник данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool OpenDataSource() {
            this._CheckThread();
            if(this._session.IsDsmOpen && !this._session.IsDsOpen) {
                for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.OpenDS, ref this._srcds); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                this._session.OpenDs();
                this._SyncTwainState();
                this._OnDataSourceOpened();
            }
            return this._session.IsDsOpen;
        }

        /// <summary>
        /// Registers a data source event handler.
        /// <para xml:lang="ru">Регистрирует обработчик событий источника данных.</para>
        /// </summary>
        private void _RegisterCallback() {
            TwCallback2 _callback = new TwCallback2 {
                CallBackProc = this._callbackProc
            };
            TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.Callback2, TwMSG.RegisterCallback, ref _callback);
            if(_rc != TwRC.Success) {
                throw new TwainException(this._GetTwainStatus(), _rc);
            }
        }

        /// <summary>
        /// Activates a data source.
        /// <para xml:lang="ru">Активирует источник данных.</para>
        /// </summary>
        private bool _EnableDataSource() {
            if(this._session.IsDsOpen && !this._session.IsDsEnabled) {
                TwUserInterface _guif = new TwUserInterface() {
                    ShowUI = this.ShowUI,
                    ModalUI = this.ModalUI,
                    ParentHand = this._GetEnableDsParentHandle()
                };
                for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.UserInterface, TwMSG.EnableDS, ref _guif); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                this.ModalUI = _guif.ModalUI;
                this._session.EnableDs();
                this._SyncTwainState();
            }
            return this._session.IsDsEnabled;
        }

        /// <summary>
        /// Deactivates a data source.
        /// <para xml:lang="ru">Деактивирует источник данных.</para>
        /// </summary>
        private bool _DisableDataSource() {
            if(this._session.IsDsEnabled) {
                try {
                    TwUserInterface _guif = new TwUserInterface() {
                        ParentHand = this._GetDisableDsParentHandle(),
                        ShowUI = false
                    };
                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.UserInterface, TwMSG.DisableDS, ref _guif); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                } finally {
                    this._session.DisableDs();
                    this._SyncTwainState();
                    this._OnDataSourceDisabled();
                }
                return !this._session.IsDsEnabled;
            }
            return false;
        }

        /// <summary>
        /// Closes the data source.
        /// <para xml:lang="ru">Закрывает источник данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool CloseDataSource() {
            this._CheckThread();
            if(this._session.IsDsOpen && !this._session.IsDsEnabled) {
                this._OnDataSourceClosing();
                for(TwRC _rc = this._dsmEntry.DsmInvoke(this._AppId, TwDG.Control, TwDAT.Identity, TwMSG.CloseDS, ref this._srcds); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                this._session.CloseDs();
                this._SyncTwainState();
                return !this._session.IsDsOpen;
            }
            return false;
        }

        /// <summary>
        /// Closes the data source manager.
        /// <para xml:lang="ru">Закрывает менеджер источников данных.</para>
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool CloseDSM() {
            this._CheckThread();
            if(this._session.IsDsEnabled) {
                this._DisableDataSource();
            }
            if(this._session.IsDsOpen) {
                this.CloseDataSource();
            }
            if(this._session.IsDsmOpen && !this._session.IsDsOpen) {
                IntPtr _hwnd = this._GetDsmCloseParentHandle();
                for(TwRC _rc = this._dsmEntry.DsmParent(this._AppId, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.CloseDSM, ref _hwnd); _rc != TwRC.Success;) {
                    throw new TwainException(this._GetTwainStatus(), _rc);
                }
                this._session.CloseDsm();
                this._SyncTwainState();
                this._UnloadDSM();
                return !this._session.IsDsmOpen;
            }
            return false;
        }

        private void _UnloadDSM() {
            this._AppId = null;
            if(this._hTwainDll != IntPtr.Zero) {
                _Platform.Unload(this._hTwainDll);
                this._hTwainDll = IntPtr.Zero;
            }
            this._threadId = 0;
        }

        private void _CheckThread() {
            int _id = Environment.CurrentManagedThreadId;
            if(this._threadId == 0) {
                this._threadId = _id;
                return;
            }
            if(this._threadId != _id) {
                throw new InvalidOperationException("TWAIN operations must be performed on the same thread. Операции TWAIN должны выполняться в одном потоке.");
            }
        }

        private IntPtr _GetEnableDsParentHandle() {
#if TWAINX
            return this.Parent;
#else
            return this._hwnd;
#endif
        }

        private IntPtr _GetDisableDsParentHandle() {
#if TWAINX
            return IntPtr.Zero;
#else
            return this._hwnd;
#endif
        }

        private IntPtr _GetDsmOpenParentHandle() {
#if TWAINX
            return IntPtr.Zero;
#else
            return this._hwnd;
#endif
        }

        private IntPtr _GetDsmCloseParentHandle() {
#if TWAINX
            return IntPtr.Zero;
#else
            return this._hwnd;
#endif
        }

        private bool _RequireTwain2OnOpen {
            get {
#if TWAINX
                return true;
#else
                return false;
#endif
            }
        }

        private void _OnDataSourceClosing() {
#if !TWAINX
            this._images.Clear();
#endif
        }

        private void _OnDataSourceDisabled() {
#if !TWAINX
            if(this._context != null) {
                this._context.ExitThread();
                this._context.Dispose();
                this._context = null;
            }
#endif
        }
    }
}
