using Saraff.TwainX;
using Xunit;

namespace Saraff.TwainX.Tests;

public sealed class RangeAndEnumerationTests
{
    [Fact]
    public void FromRange_BuildsItemsAndIndices() {
        var range = TwainX.Range.CreateRange(0f, 4f, 1f, 2f, 3f);

        var enumeration = TwainX.Enumeration.FromRange(range);

        Assert.Equal(5, enumeration.Count);
        Assert.Equal(3f, (float)enumeration[3]);
        Assert.Equal(3, enumeration.CurrentIndex);
        Assert.Equal(2, enumeration.DefaultIndex);
    }

    [Fact]
    public void FromArray_UsesZeroIndices() {
        var enumeration = TwainX.Enumeration.FromArray(new object[] { 1, 3, 5 });

        Assert.Equal(3, enumeration.Count);
        Assert.Equal(0, enumeration.CurrentIndex);
        Assert.Equal(0, enumeration.DefaultIndex);
    }

    [Fact]
    public void FromOneValue_CreatesSingleItem() {
        var enumeration = TwainX.Enumeration.FromOneValue(42);

        Assert.Equal(1, enumeration.Count);
        Assert.Equal(42, enumeration[0]);
    }
}
