using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Saraff.Twain;
using Xunit;

namespace Saraff.Twain.Tests;

public sealed class InternalTypesTests
{
    [Fact]
    public void TwFix32_RoundTripsFloat() {
        TwFix32 value = 1.5f;

        Assert.Equal(1.5f, (float)value);
    }

    [Fact]
    public void TwFix32_RoundTripsRawValue() {
        TwFix32 value = -1.25f;

        var raw = (uint)value;
        var restored = (TwFix32)raw;

        Assert.Equal(-1.25f, (float)restored, 4);
    }

    [Fact]
    public void TwFrame_RoundTripsRectangle() {
        TwFrame frame = new RectangleF(1f, 2f, 3f, 4f);

        var rectangle = (RectangleF)frame;

        Assert.Equal(1f, rectangle.X);
        Assert.Equal(2f, rectangle.Y);
        Assert.Equal(3f, rectangle.Width);
        Assert.Equal(4f, rectangle.Height);
    }

    [Fact]
    public void TwTypeHelper_MapsTwainTypesToManagedTypes() {
        Assert.Same(typeof(sbyte), TwTypeHelper.TypeOf(TwType.Int8));
        Assert.Same(typeof(short), TwTypeHelper.TypeOf(TwType.Int16));
        Assert.Same(typeof(int), TwTypeHelper.TypeOf(TwType.Int32));
        Assert.Same(typeof(uint), TwTypeHelper.TypeOf(TwType.UInt32));
        Assert.Same(typeof(TwFix32), TwTypeHelper.TypeOf(TwType.Fix32));
        Assert.Same(typeof(TwFrame), TwTypeHelper.TypeOf(TwType.Frame));
    }

    [Fact]
    public void TwTypeHelper_MapsManagedTypesToTwainTypes() {
        Assert.Equal(TwType.Bool, TwTypeHelper.TypeOf(typeof(bool)));
        Assert.Equal(TwType.Fix32, TwTypeHelper.TypeOf(typeof(float)));
        Assert.Equal(TwType.Frame, TwTypeHelper.TypeOf(typeof(RectangleF)));
        Assert.Equal(TwType.UInt32, TwTypeHelper.TypeOf(typeof(uint)));
    }

    [Fact]
    public void TwTypeHelper_MapsStringLengths() {
        Assert.Equal(TwType.Str32, TwTypeHelper.TypeOf(new string('x', 32)));
        Assert.Equal(TwType.Str64, TwTypeHelper.TypeOf(new string('x', 64)));
        Assert.Equal(TwType.Str128, TwTypeHelper.TypeOf(new string('x', 128)));
    }

    [Fact]
    public void TwTypeHelper_MapsShortStringsToSmallestCapacity() {
        Assert.Equal(TwType.Str32, TwTypeHelper.TypeOf("abc"));
        Assert.Equal(TwType.Str64, TwTypeHelper.TypeOf(new string('x', 33)));
        Assert.Equal(TwType.Str128, TwTypeHelper.TypeOf(new string('x', 65)));
        Assert.Equal(TwType.Str255, TwTypeHelper.TypeOf(new string('x', 129)));
        Assert.Equal(TwType.Uni512, TwTypeHelper.TypeOf(new string('x', 256)));
        Assert.Equal(TwType.Str1024, TwTypeHelper.TypeOf(new string('x', 513)));
    }

    [Fact]
    public void TwTypeHelper_RejectsTooLongStrings() {
        Assert.Throws<KeyNotFoundException>(() => TwTypeHelper.TypeOf(new string('x', 1025)));
    }

    [Fact]
    public void TwTypeHelper_RejectsNull() {
        Assert.Throws<ArgumentNullException>(() => TwTypeHelper.TypeOf((object)null));
        Assert.Throws<ArgumentNullException>(() => TwTypeHelper.TypeOf((Type)null));
    }

    [Fact]
    public void TwTypeHelper_ReturnsTypeSizes() {
        Assert.Equal(4, TwTypeHelper.SizeOf(TwType.Int32));
        Assert.Equal(4, TwTypeHelper.SizeOf(TwType.Fix32));
        Assert.Equal(16, TwTypeHelper.SizeOf(TwType.Frame));
    }

    [Fact]
    public void TwTypeHelper_CastsValuesRoundTrip() {
        Assert.True((bool)TwTypeHelper.CastToCommon(TwType.Bool, TwTypeHelper.CastToTw(TwType.Bool, true)));

        Assert.Equal(1.25f, (float)TwTypeHelper.CastToCommon(TwType.Fix32, TwTypeHelper.CastToTw(TwType.Fix32, 1.25f)));

        var frame = new RectangleF(1f, 2f, 3f, 4f);
        Assert.Equal(frame, (RectangleF)TwTypeHelper.CastToCommon(TwType.Frame, TwTypeHelper.CastToTw(TwType.Frame, frame)));
    }

    [Fact]
    public void TwCapability_StoresOneValue() {
        using var capability = new TwCapability(TwCap.Brightness, 42u, TwType.UInt32);

        var value = (TwOneValue)capability.GetValue();

        Assert.Equal(TwType.UInt32, value.ItemType);
        Assert.Equal(42u, value.Item);
    }

    [Fact]
    public void TwCapability_StoresStringValue() {
        using var capability = new TwCapability(TwCap.Brightness, "abc", TwType.Str32);

        Assert.Equal("abc", capability.GetValue());
    }

    [Fact]
    public void CollectionImageSink_ClearsAndAddsImages() {
        var images = new List<int> { 9 };
        IImageSink<int> sink = new CollectionImageSink<int>(images);

        sink.Begin();
        Assert.Empty(images);

        sink.Add(1);
        sink.Add(2);

        Assert.Equal(new[] { 1, 2 }, images);
    }

    [Fact]
    public void NullImageSink_IgnoresImages() {
        IImageSink<int> sink = NullImageSink<int>.Instance;

        sink.Begin();
        sink.Add(1);
    }

    [Fact]
    public void SessionState_TracksLifecycle() {
        var state = new TwainSessionState();

        Assert.False(state.IsDsmOpen);
        state.OpenDsm();
        Assert.True(state.IsDsmOpen);
        Assert.False(state.IsDsOpen);

        state.OpenDs();
        Assert.True(state.IsDsOpen);
        Assert.False(state.IsDsEnabled);

        state.EnableDs();
        Assert.True(state.IsDsEnabled);

        state.DisableDs();
        Assert.False(state.IsDsEnabled);
        Assert.True(state.IsDsOpen);

        state.CloseDs();
        Assert.False(state.IsDsOpen);
        Assert.True(state.IsDsmOpen);

        state.CloseDsm();
        Assert.False(state.IsDsmOpen);
    }

    [Fact]
    public void SessionState_EnableDsConsumesReadyFlag() {
        var state = new TwainSessionState();
        state.OpenDsm();
        state.OpenDs();
        state.SetEnabledAndReady();

        Assert.True(state.IsDsEnabled);
        Assert.True(state.IsDsReady);

        state.EnableDs();

        Assert.True(state.IsDsEnabled);
        Assert.False(state.IsDsReady);
    }

    [Fact]
    public void OpenDSM_WithInvalidDsmPath_ThrowsTwainException() {
        using var twain = new Twain32 {
            DsmPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dll")
        };

        Assert.Throws<TwainException>(() => twain.OpenDSM());
    }

    [Fact]
    public void Dispose_IsIdempotent() {
        var twain = new Twain32();

        twain.Dispose();
        twain.Dispose();
    }

    [Fact]
    public void TwCapability_StoresRangeValue() {
        var range = new TwRange {
            ItemType = TwType.UInt32,
            MinValue = 1,
            MaxValue = 10,
            StepSize = 1,
            DefaultValue = 5,
            CurrentValue = 7
        };

        using var capability = new TwCapability(TwCap.Brightness, range);

        var value = (TwRange)capability.GetValue();
        Assert.Equal(TwType.UInt32, value.ItemType);
        Assert.Equal(1u, value.MinValue);
        Assert.Equal(10u, value.MaxValue);
        Assert.Equal(7u, value.CurrentValue);
    }
}
