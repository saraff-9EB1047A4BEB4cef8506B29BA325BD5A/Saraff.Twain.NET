using System;
using System.Runtime.InteropServices;

#if TWAINX
namespace Saraff.TwainX {
#else
namespace Saraff.Twain {
#endif

        /// <summary>
        /// Entry points for memory management functions.
        /// <para xml:lang="ru">Точки входа для функций управления памятью.</para>
        /// </summary>
        internal sealed class _Memory {
            private static TwEntryPoint _entryPoint;

            /// <summary>
            /// Allocates a memory block of the specified size.
            /// <para xml:lang="ru">Выделяет блок памяти указанного размера.</para>
            /// </summary>
            /// <param name="size">The size of the memory block.<para xml:lang="ru">Размер блока памяти.</para></param>
            /// <returns>Memory descriptor.<para xml:lang="ru">Дескриптор памяти.</para></returns>
            public static IntPtr Alloc(int size) {
                if(_Memory._entryPoint != null && _Memory._entryPoint.MemoryAllocate != null) {
                    return _Memory._entryPoint.MemoryAllocate(size);
                }
                switch(_Platform.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        return _Memory.GlobalAlloc(0x42, size);
                }
            }

            /// <summary>
            /// Frees up memory.
            /// <para xml:lang="ru">Освобождает память.</para>
            /// </summary>
            /// <param name="handle">Memory descriptor.<para xml:lang="ru">Дескриптор памяти.</para></param>
            public static void Free(IntPtr handle) {
                if(_Memory._entryPoint != null && _Memory._entryPoint.MemoryFree != null) {
                    _Memory._entryPoint.MemoryFree(handle);
                    return;
                }
                switch(_Platform.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        _Memory.GlobalFree(handle);
                        break;
                }
            }

            /// <summary>
            /// Performs a memory lock.
            /// <para xml:lang="ru">Выполняет блокировку памяти.</para>
            /// </summary>
            /// <param name="handle">Memory descriptor.<para xml:lang="ru">Дескриптор памяти.</para></param>
            /// <returns>Pointer to a block of memory.<para xml:lang="ru">Указатель на блок памяти.</para></returns>
            public static IntPtr Lock(IntPtr handle) {
                if(_Memory._entryPoint != null && _Memory._entryPoint.MemoryLock != null) {
                    return _Memory._entryPoint.MemoryLock(handle);
                }
                switch(_Platform.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        return _Memory.GlobalLock(handle);
                }
            }

            /// <summary>
            /// Unlocks memory.
            /// <para xml:lang="ru">Выполняет разблокировку памяти.</para>
            /// </summary>
            /// <param name="handle">Memory descriptor.<para xml:lang="ru">Дескриптор памяти.</para></param>
            public static void Unlock(IntPtr handle) {
                if(_Memory._entryPoint != null && _Memory._entryPoint.MemoryUnlock != null) {
                    _Memory._entryPoint.MemoryUnlock(handle);
                    return;
                }
                switch(_Platform.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        throw new NotSupportedException();
                    default:
                        _Memory.GlobalUnlock(handle);
                        break;
                }
            }

            public static void ZeroMemory(IntPtr dest, IntPtr size) {
                switch(_Platform.Platform) {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        byte[] _data = new byte[size.ToInt32()];
                        Marshal.Copy(_data, 0, dest, _data.Length);
                        break;
                    default:
                        _Memory._ZeroMemory(dest, size);
                        break;
                }
            }

            /// <summary>
            /// Sets entry points.
            /// <para xml:lang="ru">Устаначливает точки входа.</para>
            /// </summary>
            /// <param name="entry">Entry points.<para xml:lang="ru">Точки входа.</para></param>
            internal static void _SetEntryPoints(TwEntryPoint entry) => _Memory._entryPoint = entry;

            #region import kernel32.dll

            [DllImport("kernel32.dll", ExactSpelling = true)]
            private static extern IntPtr GlobalAlloc(int flags, int size);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            private static extern IntPtr GlobalLock(IntPtr handle);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            private static extern bool GlobalUnlock(IntPtr handle);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            private static extern IntPtr GlobalFree(IntPtr handle);

            [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
            private static extern void _ZeroMemory(IntPtr dest, IntPtr size);


            #endregion
        }

        /// <summary>
        /// Entry points for platform features.
        /// <para xml:lang="ru">Точки входа для функций платформы.</para>
        /// </summary>
        internal sealed class _Platform {

            /// <summary>
            /// Returns the normalized platform identifier.
            /// <para xml:lang="ru">Возвращает нормализованный идентификатор платформы.</para>
            /// </summary>
            internal static PlatformID Platform {
#if NETFRAMEWORK
                get => System.Environment.OSVersion.Platform;
#else
                get => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? PlatformID.MacOSX : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? PlatformID.Unix : PlatformID.Win32NT;
#endif
            }

            /// <summary>
            /// Loads the specified library into the process memory.
            /// <para xml:lang="ru">Загружает указаную библиотеку в память процесса.</para>
            /// </summary>
            /// <param name="fileName">The name of the library.<para xml:lang="ru">Имя библиотеки.</para></param>
            /// <returns>Module descriptor.<para xml:lang="ru">Дескриптор модуля.</para></returns>
            internal static IntPtr Load(string fileName) {
                switch(_Platform.Platform) {
                    case PlatformID.Unix:
                        try {
                            return _Platform.LinuxDlOpen(fileName, 2);
                        } catch(DllNotFoundException) {
                            return IntPtr.Zero;
                        } catch(EntryPointNotFoundException) {
                            return IntPtr.Zero;
                        }
                    case PlatformID.MacOSX:
                        try {
                            return _Platform.MacosxDlOpen(fileName, 2);
                        } catch(DllNotFoundException) {
                            return IntPtr.Zero;
                        } catch(EntryPointNotFoundException) {
                            return IntPtr.Zero;
                        }
                    default:
                        return _Platform.LoadLibrary(fileName);
                }
            }

            /// <summary>
            /// Unloads the specified library from the process memory.
            /// <para xml:lang="ru">Выгружает указаную библиотеку из памяти процесса.</para>
            /// </summary>
            /// <param name="hModule">Module descriptor<para xml:lang="ru">Дескриптор модуля</para></param>
            internal static void Unload(IntPtr hModule) {
                switch(_Platform.Platform) {
                    case PlatformID.Unix:
                        try {
                            _Platform.LinuxDlClose(hModule);
                        } catch {
                        }
                        break;
                    case PlatformID.MacOSX:
                        try {
                            _Platform.MacosxDlClose(hModule);
                        } catch {
                        }
                        break;
                    default:
                        _Platform.FreeLibrary(hModule);
                        break;
                }
            }

            /// <summary>
            /// Returns the address of the specified procedure.
            /// <para xml:lang="ru">Возвращает адрес указанной процедуры.</para>
            /// </summary>
            /// <param name="hModule">Module descriptor.<para xml:lang="ru">Дескриптор модуля.</para></param>
            /// <param name="procName">The name of the procedure.<para xml:lang="ru">Имя процедуры.</para></param>
            /// <returns>Pointer to a procedure.<para xml:lang="ru">Указатель на процедуру.</para></returns>
            internal static IntPtr GetProcAddr(IntPtr hModule, string procName) {
                switch(_Platform.Platform) {
                    case PlatformID.Unix:
                        return _Platform.LinuxDlsym(hModule, procName);
                    case PlatformID.MacOSX:
                        return _Platform.MacosxDlsym(hModule, procName);
                    default:
                        return _Platform.GetProcAddress(hModule, procName);
                }
            }

            [DllImport("libdl.so.2", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
            private static extern IntPtr LinuxDlOpen(string fileName, int flags);

            [DllImport("libdl.so.2", EntryPoint = "dlsym", CharSet = CharSet.Ansi)]
            private static extern IntPtr LinuxDlsym(IntPtr handle, string symbol);

            [DllImport("libdl.so.2", EntryPoint = "dlclose")]
            private static extern int LinuxDlClose(IntPtr handle);

            [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
            private static extern IntPtr MacosxDlOpen(string fileName, int flags);

            [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlsym", CharSet = CharSet.Ansi)]
            private static extern IntPtr MacosxDlsym(IntPtr handle, string symbol);

            [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlclose")]
            private static extern int MacosxDlClose(IntPtr handle);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            private static extern IntPtr LoadLibrary(string fileName);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            private static extern bool FreeLibrary(IntPtr hModule);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
            private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        }
}
