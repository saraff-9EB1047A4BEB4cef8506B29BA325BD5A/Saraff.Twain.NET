using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Saraff.Twain;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Saraff.Twain.HardwareTests
{
    public sealed class PhysicalScannerTests
    {
        [HardwareFact]
        public void Mode_CanEnumerateSourcesAndAcquireImage()
        {
            var mode = HardwareMode.FromEnvironment();
            var outputDirectory = ResolveOutputDirectory(mode);
            Directory.CreateDirectory(outputDirectory);

            var report = new List<string>
            {
                $"Started: {DateTimeOffset.Now:O}",
                $"Mode: {mode}",
                $"Process: {(Environment.Is64BitProcess ? "x64" : "x86")}"
            };

            try
            {
                StaTestHost.Run(() => this.Run(mode, outputDirectory, report), TimeSpan.FromMinutes(3));
            }
            finally
            {
                report.Add($"Finished: {DateTimeOffset.Now:O}");
                File.WriteAllLines(Path.Combine(outputDirectory, "report.txt"), report);
            }
        }

        private void Run(HardwareMode mode, string outputDirectory, List<string> report)
        {
            Assert.Equal(mode.Is64Bit, Environment.Is64BitProcess);

            var dsmOverride = Environment.GetEnvironmentVariable("TWAIN_TEST_DSM_PATH");
            var dsmPath = string.IsNullOrWhiteSpace(dsmOverride) ? null : Path.GetFullPath(dsmOverride);
            var dsmToCheck = dsmPath ?? mode.DsmPath;
            Assert.True(File.Exists(dsmToCheck), $"DSM not found: {dsmToCheck}");

            var machine = PeArchitecture.GetMachine(dsmToCheck);
            report.Add(dsmPath == null
                ? $"DSM: library default (expected {mode.DsmPath}, machine 0x{machine:X4})"
                : $"DSM override: {dsmPath}, machine 0x{machine:X4}");
            Assert.Equal(mode.ExpectedMachine, machine);

            using (var window = new HiddenWindow())
            using (var twain = new Twain32
            {
                DsmPath = dsmPath,
                IsTwain2Enable = mode.IsTwain2,
                ShowUI = false,
                ModalUI = false,
                DisableAfterAcquire = true,
                Parent = window.Form
            })
            {
                Exception acquireError = null;
                twain.AcquireError += (_, e) => acquireError = e.Exception;

                Assert.True(twain.OpenDSM(), "OpenDSM failed.");
                try
                {
                    var names = new List<string>();
                    for (var i = 0; i < twain.SourcesCount; i++)
                    {
                        names.Add(twain.GetSourceProductName(i));
                    }

                    report.Add($"Sources ({names.Count}):");
                    for (var i = 0; i < names.Count; i++)
                    {
                        report.Add($"  [{i}] {names[i]}");
                    }

                    Assert.NotEmpty(names);
                    var sourceIndex = SelectSource(twain, names);
                    report.Add($"Selected source: [{sourceIndex}] {names[sourceIndex]}");
                    twain.SourceIndex = sourceIndex;

                    Assert.True(twain.OpenDataSource(), "OpenDataSource failed.");
                    TrySetCapabilities(twain, report);
                    report.Add($"IsTwain2Supported: {twain.IsTwain2Supported}");

                    var stopwatch = Stopwatch.StartNew();
                    twain.Acquire();
                    stopwatch.Stop();
                    report.Add($"Acquire elapsed: {stopwatch.Elapsed}");

                    if (acquireError != null)
                    {
                        throw acquireError;
                    }

                    Assert.True(twain.ImageCount > 0, "No images were acquired.");
                    using (var image = twain.GetImage(0))
                    {
                        Assert.True(image.Width > 0 && image.Height > 0, "Acquired image is empty.");

                        var fileName = $"{mode.Name}-{SanitizeFileName(names[sourceIndex])}.png";
                        var imagePath = Path.Combine(outputDirectory, fileName);
                        image.Save(imagePath, ImageFormat.Png);
                        report.Add($"Image: {imagePath} ({image.Width}x{image.Height}, {new FileInfo(imagePath).Length} bytes)");
                    }
                }
                finally
                {
                    try
                    {
                        twain.CloseDSM();
                    }
                    catch (Exception ex)
                    {
                        report.Add($"CloseDSM error: {ex}");
                    }
                }
            }
        }

        private static int SelectSource(Twain32 twain, IList<string> names)
        {
            var requested = Environment.GetEnvironmentVariable("TWAIN_TEST_SOURCE");
            if (!string.IsNullOrWhiteSpace(requested))
            {
                var index = FindSource(names, requested.Trim());
                if (index < 0)
                {
                    throw new InvalidOperationException(
                        $"TWAIN_TEST_SOURCE '{requested}' was not found. Available sources: {string.Join(", ", names)}");
                }

                return index;
            }

            var defaultIndex = twain.SourceIndex;
            return defaultIndex >= 0 && defaultIndex < names.Count ? defaultIndex : 0;
        }

        private static int FindSource(IList<string> names, string requested)
        {
            for (var i = 0; i < names.Count; i++)
            {
                if (string.Equals(names[i], requested, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            for (var i = 0; i < names.Count; i++)
            {
                if (names[i].IndexOf(requested, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void TrySetCapabilities(Twain32 twain, List<string> report)
        {
            TrySetCapability("XResolution=100", () => twain.Capabilities.XResolution.Set(100f), report);
            TrySetCapability("YResolution=100", () => twain.Capabilities.YResolution.Set(100f), report);

            var feeder = Environment.GetEnvironmentVariable("TWAIN_TEST_FEEDER");
            if (feeder == "0" || feeder == "1")
            {
                TrySetCapability($"FeederEnabled={feeder}", () => twain.Capabilities.FeederEnabled.Set(feeder == "1"), report);
            }
        }

        private static void TrySetCapability(string name, Action action, List<string> report)
        {
            try
            {
                action();
                report.Add($"Capability set: {name}");
            }
            catch (Exception ex)
            {
                report.Add($"Capability skipped: {name}: {ex.Message}");
            }
        }

        private static string ResolveOutputDirectory(HardwareMode mode)
        {
            var output = Environment.GetEnvironmentVariable("TWAIN_TEST_OUTPUT");
            if (!string.IsNullOrWhiteSpace(output))
            {
                return Path.GetFullPath(output);
            }

            return Path.Combine(Path.GetTempPath(), "Saraff.Twain.HardwareTests", mode.Name);
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        }
    }
}
