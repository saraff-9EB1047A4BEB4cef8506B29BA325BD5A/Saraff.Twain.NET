using System.Collections.Generic;

#if TWAINX
namespace Saraff.TwainX {
#else
namespace Saraff.Twain {
#endif

    internal interface IImageSink<in TImage> {
        void Begin();

        void Add(TImage image);
    }

    internal sealed class CollectionImageSink<TImage> : IImageSink<TImage> {
        private readonly ICollection<TImage> _images;

        internal CollectionImageSink(ICollection<TImage> images) {
            this._images = images;
        }

        public void Begin() => this._images.Clear();

        public void Add(TImage image) => this._images.Add(image);
    }

    internal sealed class NullImageSink<TImage> : IImageSink<TImage> {
        internal static readonly NullImageSink<TImage> Instance = new NullImageSink<TImage>();

        private NullImageSink() {
        }

        public void Begin() {
        }

        public void Add(TImage image) {
        }
    }
}
