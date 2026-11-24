using System;
using System.IO;

#if TWAINX
namespace Saraff.TwainX {

    public sealed partial class TwainX {
#else
namespace Saraff.Twain {

    public sealed partial class Twain32 {
#endif

        /// <summary>
        /// Performs image transfer (Native Mode Transfer).
        /// <para xml:lang="ru">Выполняет передачу изображения (Native Mode Transfer).</para>
        /// </summary>
        private void _NativeTransferPictures() {
            if(this._srcds.Id == 0) {
                return;
            }
            IntPtr _hBitmap = IntPtr.Zero;
            TwPendingXfers _pxfr = new TwPendingXfers();
            try {
                this._imageSink.Begin();

                do {
                    _pxfr.Count = 0;
                    _hBitmap = IntPtr.Zero;
                    try {
                        for(TwRC _rc = this._dsmEntry.DSImageXfer(this._AppId, this._srcds, TwDG.Image, TwDAT.ImageNativeXfer, TwMSG.Get, ref _hBitmap); _rc != TwRC.XferDone;) {
                            throw new TwainException(this._GetTwainStatus(), _rc);
                        }
                        if(this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo, this._GetExtImageInfo))) {
                            return;
                        }

                        IntPtr _pBitmap = _Memory.Lock(_hBitmap);
                        try {
                            IImageHandler _handler = this.GetService(typeof(IImageHandler)) as IImageHandler;
                            if(_handler == null) {
                                switch(_Platform.Platform) {
                                    case PlatformID.Unix:
                                        _handler = new Tiff();
                                        break;
                                    case PlatformID.MacOSX:
                                        _handler = new Pict();
                                        break;
                                    default:
                                        _handler = new DibToImage();
                                        break;
                                }
                            }
                            _Image _img = _handler.PtrToStream(_pBitmap, this.GetService(typeof(IStreamProvider)) as IStreamProvider);

                            this._imageSink.Add(_img);
                            if(this._OnEndXfer(new EndXferEventArgs(_img))) {
                                return;
                            }
                        } finally {
                            _Memory.Unlock(_hBitmap);
                        }
                        for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.EndXfer, ref _pxfr); _rc != TwRC.Success;) {
                            throw new TwainException(this._GetTwainStatus(), _rc);
                        }
                    } finally {
                        if(_hBitmap != IntPtr.Zero) {
                            _Memory.Free(_hBitmap);
                            _hBitmap = IntPtr.Zero;
                        }
                    }
                } while(_pxfr.Count != 0);
            } finally {
                TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.Reset, ref _pxfr);
            }
        }

        /// <summary>
        /// Performs image transfer (Disk File Mode Transfer).
        /// <para xml:lang="ru">Выполняет передачу изображения (Disk File Mode Transfer).</para>
        /// </summary>
        private void _FileTransferPictures() {
            if(this._srcds.Id == 0) {
                return;
            }

            TwPendingXfers _pxfr = new TwPendingXfers();
            try {
                this._imageSink.Begin();
                do {
                    _pxfr.Count = 0;

                    SetupFileXferEventArgs _args = new SetupFileXferEventArgs();
                    if(this._OnSetupFileXfer(_args)) {
                        return;
                    }

                    TwSetupFileXfer _fileXfer = new TwSetupFileXfer {
                        Format = this.Capabilities.ImageFileFormat.IsSupported(TwQC.GetCurrent) ? this.Capabilities.ImageFileFormat.GetCurrent() : TwFF.Bmp,
                        FileName = string.IsNullOrEmpty(_args.FileName) ? Path.GetTempFileName() : _args.FileName
                    };

                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.SetupFileXfer, TwMSG.Set, ref _fileXfer); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }

                    for(TwRC _rc = this._dsmEntry.DsRaw(this._AppId, this._srcds, TwDG.Image, TwDAT.ImageFileXfer, TwMSG.Get, IntPtr.Zero); _rc != TwRC.XferDone;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    if(this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo, this._GetExtImageInfo))) {
                        return;
                    }

                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.EndXfer, ref _pxfr); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.SetupFileXfer, TwMSG.Get, ref _fileXfer); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    if(this._OnFileXfer(new FileXferEventArgs(ImageFileXfer.Create(_fileXfer)))) {
                        return;
                    }
                } while(_pxfr.Count != 0);
            } finally {
                TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.Reset, ref _pxfr);
            }
        }

        /// <summary>
        /// Performs image transfer (Buffered Memory Mode Transfer and Memory File Mode Transfer).
        /// <para xml:lang="ru">Выполняет передачу изображения (Buffered Memory Mode Transfer and Memory File Mode Transfer).</para>
        /// </summary>
        private void _MemoryTransferPictures(bool isMemFile) {
            if(this._srcds.Id == 0) {
                return;
            }

            TwPendingXfers _pxfr = new TwPendingXfers();
            try {
                this._imageSink.Begin();
                do {
                    _pxfr.Count = 0;
                    ImageInfo _info = this._GetImageInfo();

                    if(isMemFile) {
                        if((this.Capabilities.ImageFileFormat.IsSupported() & TwQC.GetCurrent) != 0) {
                            TwSetupFileXfer _fileXfer = new TwSetupFileXfer {
                                Format = this.Capabilities.ImageFileFormat.GetCurrent()
                            };
                            for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.SetupFileXfer, TwMSG.Set, ref _fileXfer); _rc != TwRC.Success;) {
                                throw new TwainException(this._GetTwainStatus(), _rc);
                            }
                        }
                    }

                    TwSetupMemXfer _memBufSize = new TwSetupMemXfer();

                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.SetupMemXfer, TwMSG.Get, ref _memBufSize); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                    if(this._OnSetupMemXfer(new SetupMemXferEventArgs(_info, _memBufSize.Preferred))) {
                        return;
                    }

                    using(TwainDsmBuffer _hMem = TwainDsmBuffer.Alloc((int)_memBufSize.Preferred)) {
                        if(_hMem.IsInvalid) {
                            throw new TwainException("Error allocating memory. Ошибка выделения памяти.");
                        }
                        try {
                            TwMemory _mem = new TwMemory {
                                Flags = TwMF.AppOwns | TwMF.Pointer,
                                Length = _memBufSize.Preferred,
                                TheMem = _Memory.Lock(_hMem.Handle)
                            };

                            do {
                                TwImageMemXfer _memXferBuf = new TwImageMemXfer { Memory = _mem };
                                _Memory.ZeroMemory(_memXferBuf.Memory.TheMem, (IntPtr)_memXferBuf.Memory.Length);

                                TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Image, isMemFile ? TwDAT.ImageMemFileXfer : TwDAT.ImageMemXfer, TwMSG.Get, ref _memXferBuf);
                                if(_rc != TwRC.Success && _rc != TwRC.XferDone) {
                                    TwCC _cc = this._GetTwainStatus();
                                    this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.EndXfer, ref _pxfr);
                                    throw new TwainException(_cc, _rc);
                                }
                                if(this._OnMemXfer(new MemXferEventArgs(_info, ImageMemXfer.Create(_memXferBuf)))) {
                                    return;
                                }
                                if(_rc == TwRC.XferDone) {
                                    if(this._OnXferDone(new XferDoneEventArgs(this._GetImageInfo, this._GetExtImageInfo))) {
                                        return;
                                    }
                                    break;
                                }
                            } while(true);
                        } finally {
                            _Memory.Unlock(_hMem.Handle);
                        }
                    }
                    for(TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.EndXfer, ref _pxfr); _rc != TwRC.Success;) {
                        throw new TwainException(this._GetTwainStatus(), _rc);
                    }
                } while(_pxfr.Count != 0);
            } finally {
                TwRC _rc = this._dsmEntry.DsInvoke(this._AppId, this._srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.Reset, ref _pxfr);
            }
        }
    }
}
