using System;

namespace Saraff.TwainX {

    /// <summary>
    /// Options for an asynchronous TWAIN acquisition.
    /// </summary>
    public sealed class TwainXAcquireOptions {

        public bool ShowUI { get; set; }

        public bool ModalUI { get; set; }

        public IntPtr Parent { get; set; }

        public string DsmPath { get; set; }

        /// <summary>
        /// Maximum number of pages to transfer. null means no explicit limit.
        /// </summary>
        public int? MaxImages { get; set; }

        /// <summary>
        /// Disables the data source automatically after the first transfer.
        /// </summary>
        public bool DisableAfterAcquire { get; set; } = true;

        public IProgress<TwainXAcquisitionProgress> Progress { get; set; }
    }
}
