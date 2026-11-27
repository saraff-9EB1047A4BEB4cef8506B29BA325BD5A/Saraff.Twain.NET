using System;
using Xunit;

namespace Saraff.Twain.HardwareTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class HardwareFactAttribute : FactAttribute
{
    public HardwareFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("TWAIN_HARDWARE_TESTS"), "1", StringComparison.Ordinal))
        {
            this.Skip = "Hardware tests are disabled. Set TWAIN_HARDWARE_TESTS=1 and TWAIN_TEST_MODE=x86-twain1|x86-twain2|x64-twain2.";
        }
    }
}
