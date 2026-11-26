# Saraff.TwainX (.NET Standard 2.0)
Saraff.TwainX is the skillful scanning component which allows you to control work of flatbed scanner, web and digital camera and any other TWAIN device from .NET environment. You can use this library in your programs written in any programming languages compatible with .NET technology.

![license: LGPL 3.0 or later](https://img.shields.io/badge/license-LGPL%203.0%20or%20later-blue?style=flat&logo=git) ![NuGet version](https://img.shields.io/nuget/v/Saraff.TwainX.svg?style=flat&logo=nuget) ![NuGet downloads](https://img.shields.io/nuget/dt/Saraff.TwainX.svg?style=flat&logo=nuget)

## Products

This repository contains two separate products. They are not interchangeable: the package id, assembly name, namespace and public API are different.

| Product | Branch | Package | Target | Main class | Image transfer |
|---|---|---|---|---|---|
| **Saraff.Twain** | `master` | `Saraff.Twain` | `net462`, `net8.0-windows` | `Saraff.Twain.Twain32` | `System.Drawing.Image` / WPF `ImageSource` |
| **Saraff.TwainX** | `twainx` | `Saraff.TwainX` | `netstandard2.0` | `Saraff.TwainX.TwainX` | `Stream` via `IImageFactory<T>` |

### Saraff.TwainX (this branch)

Cross-platform implementation for TWAIN 2.x (DS2) data sources. It does not depend on WinForms or WPF, uses `IntPtr` for the parent window and transfers images as streams.

- `ShowUI` defaults to `false`.
- `Parent` is `IntPtr` and is applied to `DG_CONTROL / DAT_USERINTERFACE / MSG_ENABLEDS`.
- `Acquire()` requires a TWAIN 2.x/DS2 data source and a TWAIN 2.x DSM.
- `DsmPath` can be used to point to a non-default `libtwaindsm` / `TWAINDSM` library.

### Asynchronous acquisition

`AcquireAsync` uses a dedicated session thread. On Windows the thread is STA and pumps Win32 messages; on other platforms it is a serialized background thread without a Win32 message pump.

```csharp
using var session = new TwainXAcquisitionSession(new TwainXAcquireOptions {
    ShowUI = true,
    DisableAfterAcquire = true,
    MaxImages = 1
});
using var result = await session.AcquireAsync();
foreach (var stream in result.Streams) {
    // stream is a result-owned MemoryStream copied from the transfer
}
```

The synchronous `Acquire()` API remains available and unchanged.

```
Install-Package Saraff.TwainX
```

### Saraff.Twain

Windows implementation. Supports TWAIN 1.x and TWAIN 2.x data sources, WinForms/WPF parents, `System.Drawing.Image` results and the `Twain32` component.

```
Install-Package Saraff.Twain
```

## Documented differences

Do not assume that `Twain32` and `TwainX` have identical defaults or signatures:

- `ShowUI` defaults to `true` in `Twain32` and to `false` in `TwainX`.
- `Parent` is `IWin32Window` in `Twain32` and `IntPtr` in `TwainX`.
- `Twain32` exposes `GetImage`, `ImageCount`, `Image`, `ImageSource`, `IsTwain2Enable` and `IsTwain2Supported`; `TwainX` does not.
- `TwainX` requires a TWAIN 2.x/DS2 source and a TWAIN 2.x DSM.
- `TwainX.EndXferEventArgs.Stream` exposes the underlying stream; it is rewound to the beginning and remains owned by the event args.

## Build

```
dotnet build Saraff.TwainX.csproj -c Release
dotnet test tests/Saraff.TwainX.Tests/Saraff.TwainX.Tests.csproj -c Release
```

## Publishing

Publishing uses [NuGet Trusted Publishing](https://www.nuget.org/account/trustedpublishing) (OIDC), so no long-lived NuGet API key is stored in the repository. The `Publish` workflow (`.github/workflows/publish.yml`) builds, tests, packs and pushes the package and its `.snupkg` symbols.

One-time setup:

1. On nuget.org open **Trusted Publishing** and add a policy for this repository:

   | Field | Value |
   |---|---|
   | Repository Owner | `saraff-9EB1047A4BEB4cef8506B29BA325BD5A` |
   | Repository | `Saraff.Twain.NET` |
   | Workflow File | `publish.yml` |
   | Environment | `release` |

   Select the owner (your account or the organization) that owns the package, and allow pushing new packages and new versions for the `Saraff.TwainX` scope. Because this package is new on nuget.org, the policy must allow publishing new packages.
2. In GitHub, add the repository secret `NUGET_USER` with the nuget.org username of the Trusted Publishing policy creator (profile name, not email). This secret is already configured for the repository.
3. Optionally add required reviewers to the `release` environment to gate publishing.

Publish from a tag:

```
git tag twainx-v1.0.34
git push origin twainx-v1.0.34
```

On this branch the generic `v*` tag also works, but tag names are shared by both branches, so the `twainx-` prefix avoids a collision with `Saraff.Twain` releases. You can also run the **Publish** workflow manually and pass the version. Enable `dry_run` to build, test and validate the package without logging in to nuget.org or pushing it.

Also, you can see: 
* [NuGet package of the Saraff.TwainX](https://www.nuget.org/packages/Saraff.TwainX/)
* [Saraff.TwainX Extensions (LINQ to TWAIN)](https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/Saraff.Twain.Extensions/tree/twainx)
* [Wiki of the Saraff.TwainX](https://saraff-9eb1047a4beb4cef8506b29ba325bd5a.github.io/sarafftwainx/)
* [Samples](https://saraff-9eb1047a4beb4cef8506b29ba325bd5a.github.io/sarafftwainx/#samples)
