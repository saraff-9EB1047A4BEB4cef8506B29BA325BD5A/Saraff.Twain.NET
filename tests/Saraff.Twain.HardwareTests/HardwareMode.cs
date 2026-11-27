using System;
using System.IO;

namespace Saraff.Twain.HardwareTests;

internal sealed class HardwareMode
{
    private HardwareMode(string name, bool is64Bit, bool isTwain2, string dsmPath)
    {
        this.Name = name;
        this.Is64Bit = is64Bit;
        this.IsTwain2 = isTwain2;
        this.DsmPath = dsmPath;
    }

    public string Name { get; }

    public bool Is64Bit { get; }

    public bool IsTwain2 { get; }

    public string DsmPath { get; }

    public ushort ExpectedMachine => this.Is64Bit ? (ushort)0x8664 : (ushort)0x014c;

    public static HardwareMode FromEnvironment()
    {
        var name = Environment.GetEnvironmentVariable("TWAIN_TEST_MODE");
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("TWAIN_TEST_MODE is required: x86-twain1, x86-twain2 or x64-twain2.");
        }

        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        HardwareMode mode;
        switch (name.Trim().ToLowerInvariant())
        {
            case "x86-twain1":
                mode = new HardwareMode("x86-twain1", false, false, Path.Combine(windows, "twain_32.dll"));
                break;
            case "x86-twain2":
                mode = new HardwareMode("x86-twain2", false, true, Path.Combine(windows, "SysWOW64", "TWAINDSM.dll"));
                break;
            case "x64-twain2":
                mode = new HardwareMode("x64-twain2", true, true, Path.Combine(windows, "System32", "TWAINDSM.dll"));
                break;
            default:
                throw new InvalidOperationException($"Unknown TWAIN_TEST_MODE '{name}'. Expected x86-twain1, x86-twain2 or x64-twain2.");
        }

        return mode;
    }

    public override string ToString() => $"{this.Name} ({(this.Is64Bit ? "x64" : "x86")}, TWAIN {(this.IsTwain2 ? "2.x" : "1.x")}, {this.DsmPath})";
}

internal static class PeArchitecture
{
    public static ushort GetMachine(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var reader = new BinaryReader(stream))
        {
            stream.Position = 0x3c;
            var peOffset = reader.ReadInt32();
            stream.Position = peOffset + 4;
            return reader.ReadUInt16();
        }
    }
}
