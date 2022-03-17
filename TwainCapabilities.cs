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
using System.Reflection;
using System.Drawing;
using System.Diagnostics;

namespace Saraff.Twain {

    /// <summary>
    /// A set of capabilities
    /// <para xml:lang="ru">Набор возможностей (Capabilities).</para>
    /// </summary>
    [DebuggerDisplay("SupportedCaps = {SupportedCaps.Get().Count}")]
    public sealed class TwainCapabilities : MarshalByRefObject {
        private Dictionary<TwCap, Type> _caps = new Dictionary<TwCap, Type>();

        internal TwainCapabilities(Twain32 twain) {
            MethodInfo _сreateCapability = typeof(TwainCapabilities).GetMethod("CreateCapability", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach(PropertyInfo _prop in typeof(TwainCapabilities).GetProperties()) {
                object[] _attrs = _prop.GetCustomAttributes(typeof(CapabilityAttribute), false);
                if(_attrs.Length > 0) {
                    CapabilityAttribute _attr = _attrs[0] as CapabilityAttribute;
                    this._caps.Add(_attr.Cap, _prop.PropertyType);
                    _prop.SetValue(this, _сreateCapability.MakeGenericMethod(_prop.PropertyType.GetGenericArguments()[0]).Invoke(this, new object[] { twain, _attr.Cap }), null);
                }
            }
        }

        private Capability<T> CreateCapability<T>(Twain32 twain, TwCap cap) => Activator.CreateInstance(typeof(Capability<T>), new object[] { twain, cap }) as Capability<T>;

        #region Properties

        #region Asynchronous Device Events

        /// <summary>
        /// CAP_DEVICEEVENT. MSG_SET selects which events the application wants the source
        /// to report; MSG_RESET resets the capability to the empty array
        /// (no events set).
        /// </summary>
        [Capability(TwCap.DeviceEvent)]
        public ICapability2<TwDE> DeviceEvent { get; private set; }

        #endregion

        #region Audible Alarms

        /// <summary>
        /// CAP_ALARMS. Turns specific audible alarms on and off.
        /// </summary>
        [Capability(TwCap.Alarms)]
        public ICapability2<TwAL> Alarms { get; private set; }

        /// <summary>
        /// CAP_ALARMVOLUME. Controls the volume of a device’s audible alarm.
        /// </summary>
        [Capability(TwCap.AlarmVolume)]
        public ICapability<int> AlarmVolume { get; private set; }

        #endregion

        #region Automatic Adjustments

        /// <summary>
        /// CAP_AUTOMATICSENSEMEDIUM. Configures a Source to check for paper in the Automatic
        /// Document Feeder.
        /// </summary>
        [Capability(TwCap.AutomaticSenseMedium)]
        public ICapability<bool> AutomaticSenseMedium { get; private set; }

        /// <summary>
        /// ICAP_AUTODISCARDBLANKPAGES. Discards blank pages.
        /// </summary>
        [Capability(TwCap.AutoDiscardBlankPages)]
        public ICapability<TwBP> AutoDiscardBlankPages { get; private set; }

        /// <summary>
        /// ICAP_AUTOMATICBORDERDETECTION. Turns automatic border detection on and off.
        /// </summary>
        [Capability(TwCap.AutomaticBorderDetection)]
        public ICapability<bool> AutomaticBorderDetection { get; private set; }

        /// <summary>
        /// ICAP_AUTOMATICCOLORENABLED. Detects the pixel type of the image and returns either a color
        /// image or a non-color image specified by
        /// ICAP_AUTOMATICCOLORNONCOLORPIXELTYPE.
        /// </summary>
        [Capability(TwCap.AutomaticColorEnabled)]
        public ICapability<bool> AutomaticColorEnabled { get; private set; }

        /// <summary>
        /// ICAP_AUTOMATICCOLORNONCOLORPIXELTYPE. Specifies the non-color pixel type to use when automatic color
        /// is enabled.
        /// </summary>
        [Capability(TwCap.AutomaticColorNonColorPixelType)]
        public ICapability<TwPixelType> AutomaticColorNonColorPixelType { get; private set; }

        /// <summary>
        /// ICAP_AUTOMATICCROPUSESFRAME. Reduces the amount of data captured from the device,
        /// potentially improving the performance of the driver.
        /// </summary>
        [Capability(TwCap.AutomaticCropUsesFrame)]
        public ICapability<bool> AutomaticCropUsesFrame { get; private set; }

        /// <summary>
        /// ICAP_AUTOMATICDESKEW. Turns automatic skew correction on and off.
        /// </summary>
        [Capability(TwCap.AutomaticDeskew)]
        public ICapability<bool> AutomaticDeskew { get; private set; }

        /// <summary>
        /// ICAP_AUTOMATICLENGTHDETECTION. Controls the automatic detection of the length of a document,
        /// this is intended for use with an Automatic Document Feeder.
        /// </summary>
        [Capability(TwCap.AutomaticLengthDetection)]
        public ICapability<bool> AutomaticLengthDetection { get; private set; }

        /// <summary>
        /// ICAP_AUTOMATICROTATE. When TRUE, depends on source to automatically rotate the
        /// image.
        /// </summary>
        [Capability(TwCap.AutomaticRotate)]
        public ICapability<bool> AutomaticRotate { get; private set; }

        /// <summary>
        /// ICAP_AUTOSIZE. Force the output image dimensions to match either the current
        /// value of ICAP_SUPPORTEDSIZES or any of its current allowed
        /// values.
        /// </summary>
        [Capability(TwCap.AutoSize)]
        public ICapability<TwAS> AutoSize { get; private set; }

        /// <summary>
        /// ICAP_FLIPROTATION. Orients images that flip orientation every other image.
        /// </summary>
        [Capability(TwCap.FlipRotation)]
        public ICapability<TwFR> FlipRotation { get; private set; }

        /// <summary>
        /// ICAP_IMAGEMERGE. Merges the front and rear image of a document in one of four
        /// orientations: front on the top.
        /// </summary>
        [Capability(TwCap.ImageMerge)]
        public ICapability<TwIM> ImageMerge { get; private set; }

        /// <summary>
        /// ICAP_IMAGEMERGEHEIGHTTHRESHOLD. Specifies a Y-Offset in ICAP_UNITS units.
        /// </summary>
        [Capability(TwCap.ImageMergeHeightThreshold)]
        public ICapability<float> ImageMergeHeightThreshold { get; private set; }

        #endregion

        #region Automatic Capture

        /// <summary>
        /// CAP_AUTOMATICCAPTURE. Specifies the number of images to automatically capture.
        /// </summary>
        [Capability(TwCap.AutomaticCapture)]
        public ICapability<int> AutomaticCapture { get; private set; }

        /// <summary>
        /// CAP_TIMEBEFOREFIRSTCAPTURE. Selects the number of seconds before the first picture taken.
        /// </summary>
        [Capability(TwCap.TimeBeforeFirstCapture)]
        public ICapability<int> TimeBeforeFirstCapture { get; private set; }

        /// <summary>
        /// CAP_TIMEBETWEENCAPTURES. Selects the hundredths of a second to wait between pictures
        /// taken.
        /// </summary>
        [Capability(TwCap.TimeBetweenCaptures)]
        public ICapability<int> TimeBetweenCaptures { get; private set; }

        #endregion

        #region Automatic Scanning

        /// <summary>
        /// CAP_AUTOSCAN. Enables the source’s automatic document scanning process.
        /// </summary>
        [Capability(TwCap.AutoScan)]
        public ICapability<bool> AutoScan { get; private set; }

        /// <summary>
        /// CAP_CAMERAENABLED. Delivers images from the current camera.
        /// </summary>
        [Capability(TwCap.CameraEnabled)]
        public ICapability<bool> CameraEnabled { get; private set; }

        /// <summary>
        /// CAP_CAMERAORDER. Selects the order of output for Single Document Multiple Image
        /// mode.
        /// </summary>
        [Capability(TwCap.CameraOrder)]
        public ICapability2<TwPixelType> CameraOrder { get; private set; }

        /// <summary>
        /// CAP_CAMERASIDE. Sets the top and bottom values of cameras in a scanning device.
        /// </summary>
        [Capability(TwCap.CameraSide)]
        public ICapability<TwCS> CameraSide { get; private set; }

        /// <summary>
        /// CAP_CLEARBUFFERS. MSG_GET reports presence of data in scanner’s buffers;
        /// MSG_SET clears the buffers.
        /// </summary>
        [Capability(TwCap.ClearBuffers)]
        public ICapability<TwCB> ClearBuffers { get; private set; }

        /// <summary>
        /// CAP_MAXBATCHBUFFERS. Describes the number of pages that the scanner can buffer when
        /// CAP_AUTOSCAN is enabled.
        /// </summary>
        [Capability(TwCap.MaxBatchBuffers)]
        public ICapability<uint> MaxBatchBuffers { get; private set; }

        #endregion

        #region Bar Code Detection Search Parameters

        /// <summary>
        /// ICAP_BARCODEDETECTIONENABLED. Turns bar code detection on and off.
        /// </summary>
        [Capability(TwCap.BarCodeDetectionEnabled)]
        public ICapability<bool> BarCodeDetectionEnabled { get; private set; }

        /// <summary>
        /// ICAP_SUPPORTEDBARCODETYPES. Provides a list of bar code types that can be detected by current
        /// data source.
        /// </summary>
        [Capability(TwCap.SupportedBarCodeTypes)]
        public ICapability2<TwBT> SupportedBarCodeTypes { get; private set; }

        /// <summary>
        /// ICAP_BARCODEMAXRETRIES. Restricts the number of times a search will be retried if no bar
        /// codes are found.
        /// </summary>
        [Capability(TwCap.BarCodeMaxRetries)]
        public ICapability<uint> BarCodeMaxRetries { get; private set; }

        /// <summary>
        /// ICAP_BARCODEMAXSEARCHPRIORITIES. Specifies the maximum number of supported search priorities.
        /// </summary>
        [Capability(TwCap.BarCodeMaxSearchPriorities)]
        public ICapability<uint> BarCodeMaxSearchPriorities { get; private set; }

        /// <summary>
        /// ICAP_BARCODESEARCHMODE. Restricts bar code searching to certain orientations, or
        /// prioritizes one orientation over another.
        /// </summary>
        [Capability(TwCap.BarCodeSearchMode)]
        public ICapability<TwBD> BarCodeSearchMode { get; private set; }

        /// <summary>
        /// ICAP_BARCODESEARCHPRIORITIES A prioritized list of bar code types dictating the order in which
        /// they will be sought.
        /// </summary>
        [Capability(TwCap.BarCodeSearchPriorities)]
        public ICapability2<TwBT> BarCodeSearchPriorities { get; private set; }

        /// <summary>
        /// ICAP_BARCODETIMEOUT. Restricts the total time spent on searching for bar codes on a
        /// page.
        /// </summary>
        [Capability(TwCap.BarCodeTimeout)]
        public ICapability<uint> BarCodeTimeout { get; private set; }

        #endregion

        #region Capability Negotiation Parameters

        /// <summary>
        /// CAP_SUPPORTEDCAPS. Inquire Source’s capabilities valid for MSG_GET.
        /// </summary>
        [Capability(TwCap.SupportedCaps)]
        public ICapability2<TwCap> SupportedCaps { get; private set; }

        #endregion

        #region Color

        /// <summary>
        /// ICAP_COLORMANAGEMENTENABLED. Disables the Source’s color and gamma tables for color and
        /// grayscale images, resulting in output that that could be termed “raw”.
        /// </summary>
        [Capability(TwCap.ColorManagementEnabled)]
        public ICapability<bool> ColorManagementEnabled { get; private set; }

        /// <summary>
        /// ICAP_FILTER. Color characteristics of the subtractive filter applied to the
        /// image data.
        /// </summary>
        [Capability(TwCap.Filter)]
        public ICapability2<TwFT> Filter { get; private set; }

        /// <summary>
        /// ICAP_GAMMA. Gamma correction value for the image data.
        /// </summary>
        [Capability(TwCap.Gamma)]
        public ICapability<float> Gamma { get; private set; }

        /// <summary>
        /// ICAP_ICCPROFILE. Embeds or links ICC profiles into files.
        /// </summary>
        [Capability(TwCap.IccProfile)]
        public ICapability<TwIC> IccProfile { get; private set; }

        /// <summary>
        /// ICAP_PLANARCHUNKY. Color data format - Planar or Chunky.
        /// </summary>
        [Capability(TwCap.PlanarChunky)]
        public ICapability<TwPC> PlanarChunky { get; private set; }

        #endregion

        #region Compression

        /// <summary>
        /// ICAP_BITORDERCODES. CCITT Compression.
        /// </summary>
        [Capability(TwCap.BitOrderCodes)]
        public ICapability<TwBO> BitOrderCodes { get; private set; }

        /// <summary>
        /// ICAP_CCITTKFACTOR. CCITT Compression.
        /// </summary>
        [Capability(TwCap.CcittKFactor)]
        public ICapability<ushort> CcittKFactor { get; private set; }

        /// <summary>
        /// ICAP_COMPRESSION. Compression method for Buffered Memory Transfers.
        /// </summary>
        [Capability(TwCap.ICompression)]
        public ICapability<TwCompression> Compression { get; private set; }

        /// <summary>
        /// ICAP_JPEGPIXELTYPE. JPEG Compression.
        /// </summary>
        [Capability(TwCap.JpegPixelType)]
        public ICapability<TwPixelType> JpegPixelType { get; private set; }

        /// <summary>
        /// ICAP_JPEGQUALITY. JPEG quality.
        /// </summary>
        [Capability(TwCap.JpegQuality)]
        public ICapability<TwJQ> JpegQuality { get; private set; }

        /// <summary>
        /// ICAP_JPEGSUBSAMPLING. JPEG subsampling.
        /// </summary>
        [Capability(TwCap.JpegSubSampling)]
        public ICapability<TwJS> JpegSubSampling { get; private set; }

        /// <summary>
        /// ICAP_PIXELFLAVORCODES. CCITT Compression.
        /// </summary>
        [Capability(TwCap.PixelFlavor)]
        public ICapability<TwPF> PixelFlavor { get; private set; }

        /// <summary>
        /// ICAP_TIMEFILL. CCITT Compression.
        /// </summary>
        [Capability(TwCap.TimeFill)]
        public ICapability<ushort> TimeFill { get; private set; }

        #endregion

        #region Device Parameters

        /// <summary>
        /// CAP_DEVICEONLINE. Determines if hardware is on and ready.
        /// </summary>
        [Capability(TwCap.DeviceOnline)]
        public ICapability<bool> DeviceOnline { get; private set; }

        /// <summary>
        /// CAP_DEVICETIMEDATE. Date and time of a device’s clock.
        /// </summary>
        [Capability(TwCap.DeviceTimeDate)] // TW_STR32
        public ICapability<string> DeviceTimeDate { get; private set; }

        /// <summary>
        /// CAP_SERIALNUMBER. The serial number of the currently selected source device.
        /// </summary>
        [Capability(TwCap.SerialNumber)] // TW_STR255
        public ICapability<string> SerialNumber { get; private set; }

        /// <summary>
        /// ICAP_MINIMUMHEIGHT Allows the source to define the minimum height (Y-axis) that
        /// the source can acquire.
        /// </summary>
        [Capability(TwCap.MinimumHeight)]
        public ICapability<float> MinimumHeight { get; private set; }

        /// <summary>
        /// ICAP_MINIMUMWIDTH Allows the source to define the minimum width (X-axis) that
        /// the source can acquire.
        /// </summary>
        [Capability(TwCap.MinimumWidth)]
        public ICapability<float> MinimumWidth { get; private set; }

        /// <summary>
        /// ICAP_EXPOSURETIME. Exposure time used to capture the image, in seconds.
        /// </summary>
        [Capability(TwCap.ExposureTime)]
        public ICapability<float> ExposureTime { get; private set; }

        /// <summary>
        /// ICAP_FLASHUSED2. For devices that support a flash, MSG_SET selects the flash to be
        /// used; MSG_GET reports the current setting.
        /// </summary>
        [Capability(TwCap.FlashUsed2)]
        public ICapability<TwFL> FlashUsed2 { get; private set; }

        /// <summary>
        /// ICAP_IMAGEFILTER. For devices that support image filtering, selects the algorithm to
        /// be used.
        /// </summary>
        [Capability(TwCap.ImageFilter)]
        public ICapability<TwIF> ImageFilter { get; private set; }

        /// <summary>
        /// ICAP_LAMPSTATE. Is the lamp on?
        /// </summary>
        [Capability(TwCap.LampState)]
        public ICapability<bool> LampState { get; private set; }

        /// <summary>
        /// ICAP_LIGHTPATH. Image was captured transmissively or reflectively.
        /// </summary>
        [Capability(TwCap.LightPath)]
        public ICapability<TwLP> LightPath { get; private set; }

        /// <summary>
        /// ICAP_LIGHTSOURCE. Describes the color characteristic of the light source used to
        /// acquire the image.
        /// </summary>
        [Capability(TwCap.LightSource)]
        public ICapability<TwLS> LightSource { get; private set; }

        /// <summary>
        /// ICAP_NOISEFILTER. For devices that support noise filtering, selects the algorithm to
        /// be used.
        /// </summary>
        [Capability(TwCap.NoiseFilter)]
        public ICapability<TwNF> NoiseFilter { get; private set; }

        /// <summary>
        /// ICAP_OVERSCAN. For devices that support overscanning, controls whether
        /// additional rows or columns are appended to the image.
        /// </summary>
        [Capability(TwCap.OverScan)]
        public ICapability<TwOV> OverScan { get; private set; }

        /// <summary>
        /// ICAP_PHYSICALHEIGHT. Maximum height Source can acquire (in ICAP_UNITS).
        /// </summary>
        [Capability(TwCap.PhysicalHeight)]
        public ICapability<float> PhysicalHeight { get; private set; }

        /// <summary>
        /// ICAP_PHYSICALWIDTH. Maximum width Source can acquire (in ICAP_UNITS).
        /// </summary>
        [Capability(TwCap.PhysicalWidth)]
        public ICapability<float> PhysicalWidth { get; private set; }

        /// <summary>
        /// ICAP_UNITS. Unit of measure (inches, centimeters, etc.).
        /// </summary>
        [Capability(TwCap.IUnits)]
        public ICapability<TwUnits> Units { get; private set; }

        /// <summary>
        /// ICAP_ZOOMFACTOR. With MSG_GET, returns all camera supported lens zooming
        /// range.
        /// </summary>
        [Capability(TwCap.ZoomFactor)]
        public ICapability<short> ZoomFactor { get; private set; }

        #endregion

        #region Doublefeed Detection

        /// <summary>
        /// CAP_DOUBLEFEEDDETECTION. Control DFD functionality.
        /// </summary>
        [Capability(TwCap.DoubleFeedDetection)]
        public ICapability2<TwDF> DoubleFeedDetection { get; private set; }

        /// <summary>
        /// CAP_DOUBLEFEEDDETECTIONLENGTH. Set the minimum length.
        /// </summary>
        [Capability(TwCap.DoubleFeedDetectionLength)]
        public ICapability<float> DoubleFeedDetectionLength { get; private set; }

        /// <summary>
        /// CAP_DOUBLEFEEDDETECTIONSENSITIVITY. Set detector sensitivity.
        /// </summary>
        [Capability(TwCap.DoubleFeedDetectionSensitivity)]
        public ICapability<TwUS> DoubleFeedDetectionSensitivity { get; private set; }

        /// <summary>
        /// CAP_DOUBLEFEEDDETECTIONRESPONSE. Describe Source behavior in case of DFD.
        /// </summary>
        [Capability(TwCap.DoubleFeedDetectionResponse)]
        public ICapability2<TwDP> DoubleFeedDetectionResponse { get; private set; }

        #endregion

        #region Imprinter/Endorser Functionality

        /// <summary>
        /// CAP_ENDORSER. Allows the application to specify the starting endorser / imprinter number.
        /// </summary>
        [Capability(TwCap.Endorser)]
        public ICapability<uint> Endorser { get; private set; }

        /// <summary>
        /// CAP_PRINTER. MSG_GET returns current list of available printer devices;
        /// MSG_SET selects the device for negotiation.
        /// </summary>
        [Capability(TwCap.Printer)]
        public ICapability<TwPR> Printer { get; private set; }

        /// <summary>
        /// CAP_PRINTERENABLED. Turns the current CAP_PRINTER device on or off.
        /// </summary>
        [Capability(TwCap.PrinterEnabled)]
        public ICapability<bool> PrinterEnabled { get; private set; }

        /// <summary>
        /// CAP_PRINTERINDEX. Starting number for the CAP_PRINTER device.
        /// </summary>
        [Capability(TwCap.PrinterIndex)]
        public ICapability<uint> PrinterIndex { get; private set; }

        /// <summary>
        /// CAP_PRINTERMODE. Specifies appropriate current CAP_PRINTER device mode.
        /// </summary>
        [Capability(TwCap.PrinterMode)]
        public ICapability<TwPM> PrinterMode { get; private set; }

        /// <summary>
        /// CAP_PRINTERSTRING. String(s) to be used in the string component when
        /// CAP_PRINTER device is enabled.
        /// </summary>
        [Capability(TwCap.PrinterString)] // TW_STR255
        public ICapability<string> PrinterString { get; private set; }

        /// <summary>
        /// CAP_PRINTERSUFFIX. String to be used as current CAP_PRINTER device’s suffix.
        /// </summary>
        [Capability(TwCap.PrinterSuffix)] // TW_STR255
        public ICapability<string> PrinterSuffix { get; private set; }

        /// <summary>
        /// CAP_PRINTERVERTICALOFFSET. Y-Offset for current CAP_PRINTER device.
        /// </summary>
        [Capability(TwCap.PrinterVerticalOffset)]
        public ICapability<float> PrinterVerticalOffset { get; private set; }

        #endregion

        #region Image Information

        /// <summary>
        /// CAP_AUTHOR. Author of acquired image (may include a copyright string).
        /// </summary>
        [Capability(TwCap.Author)] // TW_STR128
        public ICapability<string> Author { get; private set; }

        /// <summary>
        /// CAP_CAPTION. General note about acquired image.
        /// </summary>
        [Capability(TwCap.Caption)] // TW_STR255
        public ICapability<string> Caption { get; private set; }

        /// <summary>
        /// CAP_TIMEDATE Date and Time the image was acquired (entered State 7).
        /// </summary>
        [Capability(TwCap.TimeDate)] // TW_STR32
        public ICapability<string> TimeDate { get; private set; }

        /// <summary>
        /// ICAP_EXTIMAGEINFO. Allows the application to query the data source to see if it
        /// supports the new operation triplet DG_IMAGE / DAT_EXTIMAGEINFO/ MSG_GET.
        /// </summary>
        [Capability(TwCap.ExtImageInfo)]
        public ICapability<bool> ExtImageInfo { get; private set; }

        /// <summary>
        /// ICAP_SUPPORTEDEXTIMAGEINFO. Lists all of the information that the Source is capable of
        /// returning from a call to DAT_EXTIMAGEINFO.
        /// </summary>
        [Capability(TwCap.SupportedExtImageInfo)]
        public ICapability2<TwEI> SupportedExtImageInfo { get; private set; }

        #endregion

        #region Image Parameters for Acquire

        /// <summary>
        /// CAP_THUMBNAILSENABLED. Allows an application to request the delivery of thumbnail
        /// representations for the set of images that are to be delivered.
        /// </summary>
        [Capability(TwCap.ThumbnailsEnabled)]
        public ICapability<bool> ThumbnailsEnabled { get; private set; }

        /// <summary>
        /// ICAP_AUTOBRIGHT. Enable Source’s Auto-brightness function.
        /// </summary>
        [Capability(TwCap.AutoBright)]
        public ICapability<bool> AutoBright { get; private set; }

        /// <summary>
        /// ICAP_BRIGHTNESS. Source brightness values.
        /// </summary>
        [Capability(TwCap.Brightness)]
        public ICapability<float> Brightness { get; private set; }

        /// <summary>
        /// ICAP_CONTRAST. Source contrast values.
        /// </summary>
        [Capability(TwCap.Contrast)]
        public ICapability<float> Contrast { get; private set; }

        /// <summary>
        /// ICAP_HIGHLIGHT. Lightest highlight, values lighter than this value will be set to
        /// this value.
        /// </summary>
        [Capability(TwCap.Highlight)]
        public ICapability<float> Highlight { get; private set; }

        /// <summary>
        /// ICAP_IMAGEDATASET. Gets or sets the image indices that will be delivered during the
        /// standard image transfer done in States 6 and 7.
        /// </summary>
        [Capability(TwCap.ImageDataSet)]
        public ICapability2<uint> ImageDataSet { get; private set; }

        /// <summary>
        /// ICAP_MIRROR. Source can, or should, mirror image.
        /// </summary>
        [Capability(TwCap.Mirror)]
        public ICapability<TwNF> Mirror { get; private set; }

        /// <summary>
        /// ICAP_ORIENTATION Defines which edge of the paper is the top: Portrait or
        /// Landscape.
        /// </summary>
        [Capability(TwCap.Orientation)]
        public ICapability<TwOR> Orientation { get; private set; }

        /// <summary>
        /// ICAP_ROTATION. Source can, or should, rotate image this number of degrees.
        /// </summary>
        [Capability(TwCap.Rotation)]
        public ICapability<float> Rotation { get; private set; }

        /// <summary>
        /// ICAP_SHADOW. Darkest shadow, values darker than this value will be set to this
        /// value.
        /// </summary>
        [Capability(TwCap.Shadow)]
        public ICapability<float> Shadow { get; private set; }

        /// <summary>
        /// ICAP_XSCALING. Source Scaling value (1.0 = 100%) for x-axis.
        /// </summary>
        [Capability(TwCap.XScaling)]
        public ICapability<float> XScaling { get; private set; }

        /// <summary>
        /// ICAP_YSCALING. Source Scaling value (1.0 = 100%) for y-axis.
        /// </summary>
        [Capability(TwCap.YScaling)]
        public ICapability<float> YScaling { get; private set; }

        #endregion

        #region Image Type

        /// <summary>
        /// ICAP_BITDEPTH. Pixel bit depth for Current value of ICAP_PIXELTYPE.
        /// </summary>
        [Capability(TwCap.BitDepth)]
        public ICapability<ushort> BitDepth { get; private set; }

        /// <summary>
        /// ICAP_BITDEPTHREDUCTION. Allows a choice of the reduction method for bit depth loss.
        /// </summary>
        [Capability(TwCap.BitDepthReduction)]
        public ICapability<TwBR> BitDepthReduction { get; private set; }

        /// <summary>
        /// ICAP_BITORDER. Specifies how the bytes in an image are filled by the Source.
        /// </summary>
        [Capability(TwCap.BitOrder)]
        public ICapability<TwBO> BitOrder { get; private set; }

        /// <summary>
        /// ICAP_CUSTHALFTONE. Square-cell halftone (dithering) matrix to be used.
        /// </summary>
        [Capability(TwCap.CustHalftone)]
        public ICapability2<byte> CustHalftone { get; private set; }

        /// <summary>
        /// ICAP_HALFTONES. Source halftone patterns.
        /// </summary>
        [Capability(TwCap.Halftones)] // TW_STR32
        public ICapability<string> Halftones { get; private set; }

        /// <summary>
        /// ICAP_PIXELTYPE. The type of pixel data (B/W, gray, color, etc.)
        /// </summary>
        [Capability(TwCap.IPixelType)]
        public ICapability<TwPixelType> PixelType { get; private set; }

        /// <summary>
        /// ICAP_THRESHOLD. Specifies the dividing line between black and white values.
        /// </summary>
        [Capability(TwCap.Threshold)]
        public ICapability<float> Threshold { get; private set; }

        #endregion

        #region Language Support

        /// <summary>
        /// CAP_LANGUAGE. Allows application and source to identify which languages they
        /// have in common.
        /// </summary>
        [Capability(TwCap.Language)]
        public ICapability<TwLanguage> Language { get; private set; }

        #endregion

        #region MICR

        /// <summary>
        /// CAP_MICRENABLED. Enables actions needed to support check scanning.
        /// </summary>
        [Capability(TwCap.MicrEnabled)]
        public ICapability<bool> MicrEnabled { get; private set; }

        #endregion

        #region Pages

        /// <summary>
        /// CAP_SEGMENTED. Describes the segmentation setting for captured images.
        /// </summary>
        [Capability(TwCap.Segmented)]
        public ICapability<TwSG> Segmented { get; private set; }

        /// <summary>
        /// ICAP_FRAMES. Size and location of frames on page.
        /// </summary>
        [Capability(TwCap.Frames)]
        public ICapability<RectangleF> Frames { get; private set; }

        /// <summary>
        /// ICAP_MAXFRAMES. Maximum number of frames possible per page.
        /// </summary>
        [Capability(TwCap.MaxFrames)]
        public ICapability<ushort> MaxFrames { get; private set; }

        /// <summary>
        /// ICAP_SUPPORTEDSIZES. Fixed frame sizes for typical page sizes.
        /// </summary>
        [Capability(TwCap.SupportedSizes)]
        public ICapability<TwSS> SupportedSizes { get; private set; }

        #endregion

        #region Paper Handling

        /// <summary>
        /// CAP_AUTOFEED. MSG_SET to TRUE to enable Source’s automatic feeding.
        /// </summary>
        [Capability(TwCap.AutoFeed)]
        public ICapability<bool> AutoFeed { get; private set; }

        /// <summary>
        /// CAP_CLEARPAGE. MSG_SET to TRUE to eject current page and leave acquire area empty.
        /// </summary>
        [Capability(TwCap.ClearPage)]
        public ICapability<bool> ClearPage { get; private set; }

        /// <summary>
        /// CAP_DUPLEX. Indicates whether the scanner supports duplex.
        /// </summary>
        [Capability(TwCap.Duplex)]
        public ICapability<TwDX> Duplex { get; private set; }

        /// <summary>
        /// CAP_DUPLEXENABLED. Enables the user to set the duplex option to be TRUE or FALSE.
        /// </summary>
        [Capability(TwCap.DuplexEnabled)]
        public ICapability<bool> DuplexEnabled { get; private set; }

        /// <summary>
        /// CAP_FEEDERALIGNMENT. Indicates the alignment of the document feeder.
        /// </summary>
        [Capability(TwCap.FeederAlignment)]
        public ICapability<TwFA> FeederAlignment { get; private set; }

        /// <summary>
        /// CAP_FEEDERENABLED. If TRUE, Source’s feeder is available.
        /// </summary>
        [Capability(TwCap.FeederEnabled)]
        public ICapability<bool> FeederEnabled { get; private set; }

        /// <summary>
        /// CAP_FEEDERLOADED. If TRUE, Source has documents loaded in feeder (MSG_GET only).
        /// </summary>
        [Capability(TwCap.FeederLoaded)]
        public ICapability<bool> FeederLoaded { get; private set; }

        /// <summary>
        /// CAP_FEEDERORDER. Specifies whether feeder starts with top of first or last page.
        /// </summary>
        [Capability(TwCap.FeederOrder)]
        public ICapability<TwFO> FeederOrder { get; private set; }

        /// <summary>
        /// CAP_FEEDERPOCKET. Report what pockets are available as paper leaves a device.
        /// </summary>
        [Capability(TwCap.FeederPocket)]
        public ICapability2<TwFP> FeederPocket { get; private set; }

        /// <summary>
        /// CAP_FEEDERPREP. Improve the movement of paper through a scanner ADF.
        /// </summary>
        [Capability(TwCap.FeederPrep)]
        public ICapability<bool> FeederPrep { get; private set; }

        /// <summary>
        /// CAP_FEEDPAGE. MSG_SET to TRUE to eject current page and feed next page.
        /// </summary>
        [Capability(TwCap.FeedPage)]
        public ICapability<bool> FeedPage { get; private set; }

        /// <summary>
        /// CAP_PAPERDETECTABLE. Determines whether source can detect documents on the ADF.
        /// </summary>
        [Capability(TwCap.PaperDetectable)]
        public ICapability<bool> PaperDetectable { get; private set; }

        /// <summary>
        /// CAP_PAPERHANDLING. Control paper handling.
        /// </summary>
        [Capability(TwCap.PaperHandling)]
        public ICapability2<TwPH> PaperHandling { get; private set; }

        /// <summary>
        /// CAP_REACQUIREALLOWED. Capable of acquring muliple images of the same page wihtout
        /// changing the physical registraion of that page.
        /// </summary>
        [Capability(TwCap.ReacquireAllowed)]
        public ICapability<bool> ReacquireAllowed { get; private set; }

        /// <summary>
        /// CAP_REWINDPAGE. MSG_SET to TRUE to do a reverse feed.
        /// </summary>
        [Capability(TwCap.RewindPage)]
        public ICapability<bool> RewindPage { get; private set; }

        /// <summary>
        /// ICAP_FEEDERTYPE. Allows application to set scan parameters depending on the
        /// type of feeder being used.
        /// </summary>
        [Capability(TwCap.FeederType)]
        public ICapability<TwFE> FeederType { get; private set; }

        #endregion

        #region Patch Code Detection

        /// <summary>
        /// ICAP_PATCHCODEDETECTIONENABLED. Turns patch code detection on and off.
        /// </summary>
        [Capability(TwCap.PatchCodeDetectionEnabled)]
        public ICapability<bool> PatchCodeDetectionEnabled { get; private set; }

        /// <summary>
        /// ICAP_SUPPORTEDPATCHCODETYPES. List of patch code types that can be detected by current data
        /// source.
        /// </summary>
        [Capability(TwCap.SupportedPatchCodeTypes)]
        public ICapability2<TwPch> SupportedPatchCodeTypes { get; private set; }

        /// <summary>
        /// ICAP_PATCHCODEMAXSEARCHPRIORITIES. Maximum number of search priorities.
        /// </summary>
        [Capability(TwCap.PatchCodeMaxSearchPriorities)]
        public ICapability<uint> PatchCodeMaxSearchPriorities { get; private set; }

        /// <summary>
        /// ICAP_PATCHCODESEARCHPRIORITIES. List of patch code types dictating the order in which patch
        /// codes will be sought.
        /// </summary>
        [Capability(TwCap.PatchCodeSearchPriorities)]
        public ICapability2<TwPch> PatchCodeSearchPriorities { get; private set; }

        /// <summary>
        /// ICAP_PATCHCODESEARCHMODE. Restricts patch code searching to certain orientations, or
        /// prioritizes one orientation over another.
        /// </summary>
        [Capability(TwCap.PatchCodeSearchMode)]
        public ICapability<TwBD> PatchCodeSearchMode { get; private set; }

        /// <summary>
        /// ICAP_PATCHCODEMAXRETRIES. Restricts the number of times a search will be retried if none are
        /// found on a page.
        /// </summary>
        [Capability(TwCap.PatchCodeMaxRetries)]
        public ICapability<uint> PatchCodeMaxRetries { get; private set; }

        /// <summary>
        /// ICAP_PATCHCODETIMEOUT. Restricts total time for searching for a patch code on a page.
        /// </summary>
        [Capability(TwCap.PatchCodeTimeout)]
        public ICapability<uint> PatchCodeTimeout { get; private set; }

        #endregion

        #region Power Monitoring

        /// <summary>
        /// CAP_BATTERYMINUTES. The minutes of battery power remaining on a device.
        /// </summary>
        [Capability(TwCap.BatteryMinutes)]
        public ICapability<TwBM1> BatteryMinutes { get; private set; }

        /// <summary>
        /// CAP_BATTERYPERCENTAGE. With MSG_GET, indicates battery power status.
        /// </summary>
        [Capability(TwCap.BatteryPercentage)]
        public ICapability<TwBM2> BatteryPercentage { get; private set; }

        /// <summary>
        /// CAP_POWERSAVETIME. With MSG_SET, sets the camera power down timer in seconds;
        /// with MSG_GET, returns the current setting of the power down time.
        /// </summary>
        [Capability(TwCap.PowerSaveTime)]
        public ICapability<int> PowerSaveTime { get; private set; }

        /// <summary>
        /// CAP_POWERSUPPLY. MSG_GET reports the kinds of power available;
        /// MSG_GETCURRENT reports the current power supply to use.
        /// </summary>
        [Capability(TwCap.PowerSupply)]
        public ICapability<TwPS> PowerSupply { get; private set; }

        #endregion

        #region Resolution

        /// <summary>
        /// ICAP_XNATIVERESOLUTION. Native optical resolution of device for x-axis.
        /// </summary>
        [Capability(TwCap.XNativeResolution)]
        public ICapability<float> XNativeResolution { get; private set; }

        /// <summary>
        /// ICAP_XRESOLUTION. Current/Available optical resolutions for x-axis.
        /// </summary>
        [Capability(TwCap.XResolution)]
        public ICapability<float> XResolution { get; private set; }

        /// <summary>
        /// ICAP_YNATIVERESOLUTION. Native optical resolution of device for y-axis.
        /// </summary>
        [Capability(TwCap.YNativeResolution)]
        public ICapability<float> YNativeResolution { get; private set; }

        /// <summary>
        /// ICAP_YRESOLUTION. Current/Available optical resolutions for y-axis.
        /// </summary>
        [Capability(TwCap.YResolution)]
        public ICapability<float> YResolution { get; private set; }

        #endregion

        #region Transfers

        /// <summary>
        /// CAP_JOBCONTROL. Allows multiple jobs in batch mode.
        /// </summary>
        [Capability(TwCap.JobControl)]
        public ICapability<TwJC> JobControl { get; private set; }

        /// <summary>
        /// CAP_SHEETCOUNT. Capture the specified number of sheets of paper.
        /// </summary>
        [Capability(TwCap.SheetCount)]
        public ICapability<uint> SheetCount { get; private set; }

        /// <summary>
        /// CAP_XFERCOUNT. Number of images the application is willing to accept this session.
        /// </summary>
        [Capability(TwCap.XferCount)]
        public ICapability<short> XferCount { get; private set; }

        /// <summary>
        /// ICAP_IMAGEFILEFORMAT. File formats for file transfers.
        /// </summary>
        [Capability(TwCap.ImageFileFormat)]
        public ICapability<TwFF> ImageFileFormat { get; private set; }

        /// <summary>
        /// ICAP_TILES. Tiled image data.
        /// </summary>
        [Capability(TwCap.Tiles)]
        public ICapability<bool> Tiles { get; private set; }

        /// <summary>
        /// ICAP_UNDEFINEDIMAGESIZE. The application will accept undefined image size.
        /// </summary>
        [Capability(TwCap.UndefinedImageSize)]
        public ICapability<bool> UndefinedImageSize { get; private set; }

        /// <summary>
        /// ICAP_XFERMECH. Transfer mechanism - used to learn options and set-up for
        /// upcoming transfer.
        /// </summary>
        [Capability(TwCap.IXferMech)]
        public ICapability<TwSX> XferMech { get; private set; }

        #endregion

        #region User Interface

        /// <summary>
        /// CAP_CAMERAPREVIEWUI. Queries the source for UI support for preview mode.
        /// </summary>
        [Capability(TwCap.CameraPreviewUI)]
        public ICapability<bool> CameraPreviewUI { get; private set; }

        /// <summary>
        /// CAP_CUSTOMDSDATA. Allows the application to query the data source to see if it
        /// supports the new operation triplets DG_CONTROL / DAT_CUSTOMDSDATA / MSG_GET and 
        /// DG_CONTROL / DAT_CUSTOMDSDATA / MSG_SET.
        /// </summary>
        [Capability(TwCap.CustomDSData)]
        public ICapability<bool> CustomDSData { get; private set; }

        /// <summary>
        /// CAP_CUSTOMINTERFACEGUID. Uniquely identifies an interface for a Data Source.
        /// </summary>
        [Capability(TwCap.CustomInterfaceGuid)]
        public ICapability<string> CustomInterfaceGuid { get; private set; }

        /// <summary>
        /// CAP_ENABLEDSUIONLY. Queries an application to see if it implements the new user
        /// interface settings dialog.
        /// </summary>
        [Capability(TwCap.EnableDSUIOnly)]
        public ICapability<bool> EnableDSUIOnly { get; private set; }

        /// <summary>
        /// CAP_INDICATORS. Use the Source’s progress indicator? (valid only when
        /// ShowUI==FALSE).
        /// </summary>
        [Capability(TwCap.Indicators)]
        public ICapability<bool> Indicators { get; private set; }

        /// <summary>
        /// CAP_INDICATORSMODE. List of messages types that can be display if
        /// ICAP_INDICATORS is TRUE.
        /// </summary>
        [Capability(TwCap.IndicatorsMode)]
        public ICapability2<TwCI> IndicatorsMode { get; private set; }

        /// <summary>
        /// CAP_UICONTROLLABLE. Indicates that Source supports acquisitions with UI disabled.
        /// </summary>
        [Capability(TwCap.UIControllable)]
        public ICapability<bool> UIControllable { get; private set; }

        #endregion

        #endregion

        [DebuggerDisplay("{ToString()}")]
        private class Capability<T> : ICapability<T>, ICapability2<T> {

            public Capability(Twain32 twain, TwCap cap) {
                this._Twain32 = twain;
                this._Cap = cap;
            }

            public Twain32.Enumeration Get() => this.ToEnumeration(this._Twain32.GetCap(this._Cap));

            public T GetCurrent() => (T)this._Twain32.GetCurrentCap(this._Cap);

            public object[] GetCurrentArray() => this.ToEnumeration(this._Twain32.GetCurrentCap(this._Cap)).Items;

            public T GetDefault() => (T)this._Twain32.GetDefaultCap(this._Cap);

            public object[] GetDefaultArray() => this.ToEnumeration(this._Twain32.GetDefaultCap(this._Cap)).Items;

            public void Set(T value) {
                if(this._Twain32.Capabilities._caps[this._Cap] == typeof(ICapability2<T>) || !this.GetCurrent().Equals(value)) {
                    this._Twain32.SetCap(this._Cap, value);
                }
            }

            public void Set(params T[] value) {
                var _val = new object[value.Length];
                for(var i = 0; i < _val.Length; i++) {
                    _val[i] = value[i];
                }
                this._Twain32.SetCap(this._Cap, _val);
            }

            /// <summary>
            /// Sets a limit on the values of the specified feature.
            /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
            /// </summary>
            /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
            public void SetConstraint(T value) => this._Twain32.SetConstraintCap(this._Cap, value);

            /// <summary>
            /// Sets a limit on the values of the specified feature.
            /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
            /// </summary>
            /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
            public void SetConstraint(params T[] value) {
                var _val = new object[value.Length];
                for(var i = 0; i < _val.Length; i++) {
                    _val[i] = value[i];
                }
                this._Twain32.SetConstraintCap(this._Cap, _val);
            }

            /// <summary>
            /// Sets a limit on the values of the specified feature.
            /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
            /// </summary>
            /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
            public void SetConstraint(Twain32.Range value) {
                if(value.CurrentValue.GetType() != typeof(T)) {
                    throw new ArgumentException();
                }
                this._Twain32.SetConstraintCap(this._Cap, value);
            }

            /// <summary>
            /// Sets a limit on the values of the specified feature.
            /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
            /// </summary>
            /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
            public void SetConstraint(Twain32.Enumeration value) {
                if(value.Items == null || value.Items.Length == 0 || value.Items[0].GetType() != typeof(T)) {
                    throw new ArgumentException();
                }
                this._Twain32.SetConstraintCap(this._Cap, value);
            }

            public void Reset() => this._Twain32.ResetCap(this._Cap);

            public TwQC IsSupported() => this._Twain32.IsCapSupported(this._Cap);

            public bool IsSupported(TwQC operation) => (this.IsSupported() & operation) == operation;

            protected Twain32 _Twain32 { get; private set; }

            protected TwCap _Cap { get; private set; }

            private Twain32.Enumeration ToEnumeration(object value) {
                Twain32.Enumeration _val = Twain32.Enumeration.FromObject(value);
                for(int i = 0; i < _val.Count; i++) {
                    _val[i] = typeof(T).IsEnum ? (T)_val[i] : Convert.ChangeType(_val[i], typeof(T));
                }
                return _val;
            }

            public override string ToString() {
                var _supported = this.IsSupported();
                return string.Format("{0}, {1}{2}{3}{4}", this._Cap, _supported == 0 ? "Not Supported" : "", (_supported & TwQC.GetCurrent) != 0 ? string.Format("Current = {0}, ", this.GetCurrent()) : "", (_supported & TwQC.GetDefault) != 0 ? string.Format("Default = {0}, ", this.GetDefault()) : "", _supported != 0 ? string.Format("Supported = {{{0}}}", this.IsSupported()) : "");
            }
        }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
        private sealed class CapabilityAttribute : Attribute {

            public CapabilityAttribute(TwCap cap) {
                this.Cap = cap;
            }

            public TwCap Cap { get; private set; }
        }
    }

    /// <summary>
    /// Presents an capability.
    /// <para xml:lang="ru">Представляет возможность (Capability).</para>
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    public interface ICapability<T> {

        /// <summary>
        /// Returns capability values.
        /// <para xml:lang="ru">Возвращает значения возможности (capability).</para>
        /// </summary>
        /// <returns>Capability Values.<para xml:lang="ru">Значения возможности (capability).</para></returns>
        Twain32.Enumeration Get();

        /// <summary>
        /// Returns the current capability value.
        /// <para xml:lang="ru">Возвращает текущее значение возможности (capability).</para>
        /// </summary>
        /// <returns>Current Capability Value.<para xml:lang="ru">Текущее значение возможности (capability).</para></returns>
        T GetCurrent();

        /// <summary>
        /// Returns default value of feature (capability).
        /// <para xml:lang="ru">Возвращает значение по умолчанию возможности (capability).</para>
        /// </summary>
        /// <returns>Feature Default (capability).<para xml:lang="ru">Значение по умолчанию возможности (capability).</para></returns>
        T GetDefault();

        /// <summary>
        /// Sets the current value of the capability.
        /// <para xml:lang="ru">Устанавливает текущее значение возможности (capability).</para>
        /// </summary>
        /// <param name="value">Value.<para xml:lang="ru">Значение.</para></param>
        void Set(T value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        void SetConstraint(T value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        void SetConstraint(params T[] value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        void SetConstraint(Twain32.Range value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        void SetConstraint(Twain32.Enumeration value);

        /// <summary>
        /// Resets the current capability value to the default value.
        /// <para xml:lang="ru">Сбрасывает текущее значение возможности (capability) в значение по умолчанию.</para>
        /// </summary>
        void Reset();

        /// <summary>
        /// Returns a set of flags of supported operations.
        /// <para xml:lang="ru">Возвращает набор флагов поддерживаемых операций.</para>
        /// </summary>
        /// <returns>Set of flags of supported operations.<para xml:lang="ru">Набор флагов поддерживаемых операций.</para></returns>
        TwQC IsSupported();

        /// <summary>
        /// Determines whether the specified operation is supported.
        /// <para xml:lang="ru">Определяет поддерживаются ли указанные операции.</para>
        /// </summary>
        /// <param name="operation">A set of bit flags defining the required operations.<para xml:lang="ru">Набор битовых флагов, определяющих требуемые операйии.</para></param>
        /// <returns>True, if all specified operation is supported, otherwise false.<para xml:lang="ru">Истина, если все указанные операции поддерживаются, иначе лож.</para></returns>
        bool IsSupported(TwQC operation);
    }

    /// <summary>
    /// Represents Capability.
    /// <para xml:lang="ru">Представляет возможность (Capability).</para>
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    public interface ICapability2<T> {

        /// <summary>
        /// Returns the capability values.
        /// <para xml:lang="ru">Возвращает значения возможности (capability).</para>
        /// </summary>
        /// <returns>Values of capability.<para xml:lang="ru">Значения возможности (capability).</para></returns>
        Twain32.Enumeration Get();

        /// <summary>
        /// Returns the current capability value.
        /// <para xml:lang="ru">Возвращает текущие значения возможности (capability).</para>
        /// </summary>
        /// <returns>Current values of capability.<para xml:lang="ru">Текущие значения возможности (capability).</para></returns>
        object[] GetCurrentArray();

        /// <summary>
        /// Returns the default values of capability.
        /// <para xml:lang="ru">Возвращает значения по умолчанию возможности (capability).</para>
        /// </summary>
        /// <returns>The default values are capability.<para xml:lang="ru">Значения по умолчанию возможности (capability).</para></returns>
        object[] GetDefaultArray();

        /// <summary>
        /// Sets the current value of capability.
        /// <para xml:lang="ru">Устанавливает текущее значение возможности (capability).</para>
        /// </summary>
        /// <param name="value">Value.<para xml:lang="ru">Значение.</para></param>
        void Set(T value);

        /// <summary>
        /// Sets the current value of capability.
        /// <para xml:lang="ru">Устанавливает текущее значение возможности (capability).</para>
        /// </summary>
        /// <param name="value">Value.<para xml:lang="ru">Значение.</para></param>
        void Set(params T[] value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        void SetConstraint(T value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        void SetConstraint(params T[] value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        void SetConstraint(Twain32.Range value);

        /// <summary>
        /// Sets a limit on the values of the specified feature.
        /// <para xml:lang="ru">Устанавливает ограничение на значения указанной возможности.</para>
        /// </summary>
        /// <param name="value">The value to set.<para xml:lang="ru">Устанавливаемое значение.</para></param>
        void SetConstraint(Twain32.Enumeration value);

        /// <summary>
        /// Resets the current capability value to the default value.
        /// <para xml:lang="ru">Сбрасывает текущее значение возможности (capability) в значение по умолчанию.</para>
        /// </summary>
        void Reset();

        /// <summary>
        /// Returns a set of flags of supported operations.
        /// <para xml:lang="ru">Возвращает набор флагов поддерживаемых операций.</para>
        /// </summary>
        /// <returns>Set of flags of supported operations.<para xml:lang="ru">Набор флагов поддерживаемых операций.</para></returns>
        TwQC IsSupported();

        /// <summary>
        /// Determines whether the specified operation is supported.
        /// <para xml:lang="ru">Определяет поддерживаются ли указанные операции.</para>
        /// </summary>
        /// <param name="operation">A set of bit flags defining the required operations. The operation.<para xml:lang="ru">Набор битовых флагов, определяющих требуемые операйии.</para></param>
        /// <returns>True, if all specified operation is supported, otherwise false.<para xml:lang="ru">Истина, если все указанные операции поддерживаются, иначе лож.</para></returns>
        bool IsSupported(TwQC operation);
    }
}
