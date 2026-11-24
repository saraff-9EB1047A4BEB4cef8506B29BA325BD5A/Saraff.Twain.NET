namespace Saraff.TwainX {

    /// <summary>
    /// Progress information for an asynchronous acquisition.
    /// </summary>
    public sealed class TwainXAcquisitionProgress {

        internal TwainXAcquisitionProgress(int pageNumber, TwainX.ImageInfo imageInfo) {
            this.PageNumber = pageNumber;
            this.ImageInfo = imageInfo;
        }

        public int PageNumber { get; private set; }

        public TwainX.ImageInfo ImageInfo { get; private set; }
    }
}
