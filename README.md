# Saraff.Twain.NET
Saraff.Twain.NET is the skillful scanning component which allows you to control work of flatbed scanner, web and digital camera and any other TWAIN device from .NET environment. You can use this library in your programs written in any programming languages compatible with .NET technology.

![license: LGPL 3.0 or later](https://img.shields.io/badge/license-LGPL%203.0%20or%20later-blue?style=flat&logo=git) ![NuGet version](https://img.shields.io/nuget/v/Saraff.Twain.svg?style=flat&logo=nuget) ![NuGet downloads](https://img.shields.io/nuget/dt/Saraff.Twain.svg?style=flat&logo=nuget)

## Products

This repository contains two separate products. They are not interchangeable: the package id, assembly name, namespace and public API are different.

| Product | Branch | Package | Target | Main class | Image transfer |
|---|---|---|---|---|---|
| **Saraff.Twain** | `master` | `Saraff.Twain` | `net462`, `net8.0-windows` | `Saraff.Twain.Twain32` | `System.Drawing.Image` / WPF `ImageSource` |
| **Saraff.TwainX** | `twainx` | `Saraff.TwainX` | `netstandard2.0` | `Saraff.TwainX.TwainX` | `Stream` via `IImageFactory<T>` |

### Saraff.Twain (this branch)

Windows implementation. Supports TWAIN 1.x and TWAIN 2.x data sources, WinForms/WPF parents, `System.Drawing.Image` results and the `Twain32` component.

```
Install-Package Saraff.Twain
```

### Saraff.TwainX

Cross-platform implementation for TWAIN 2.x (DS2) data sources. It does not depend on WinForms or WPF, uses `IntPtr` for the parent window and transfers images as streams. `ShowUI` defaults to `false`.

```
Install-Package Saraff.TwainX
```

## Documented differences

Do not assume that `Twain32` and `TwainX` have identical defaults or signatures:

- `ShowUI` defaults to `true` in `Twain32` and to `false` in `TwainX`.
- `Parent` is `IWin32Window` in `Twain32` and `IntPtr` in `TwainX`.
- `Twain32` exposes `GetImage`, `ImageCount`, `Image`, `ImageSource`, `IsTwain2Enable` and `IsTwain2Supported`; `TwainX` does not.
- `TwainX` requires a TWAIN 2.x/DS2 source and a TWAIN 2.x DSM.

## Build

Modern Windows library:

```
dotnet build Saraff.Twain.NET.csproj -c Release
```

Legacy .NET Framework 4.8 library:

```
dotnet msbuild Saraff.Twain.csproj -p:Configuration=Release -t:Build
```

Tests:

```
dotnet test tests/Saraff.Twain.Tests/Saraff.Twain.Tests.csproj -c Release
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

   Select the owner (your account or the organization) that owns the package, and allow pushing new packages and new versions for the scopes you release here: `Saraff.Twain` for `master`, and `Saraff.TwainX` if the same policy is used from the `twainx` branch.
2. In GitHub, add the repository secret `NUGET_USER` with your nuget.org username (profile name, not email).
3. Optionally add required reviewers to the `release` environment to gate publishing.

Publish from a tag:

```
git tag v1.0.34
git push origin v1.0.34
```

On the `twainx` branch use the product prefix so both products can have the same version: `twainx-v1.0.34`. The `master` branch also accepts `twain-v1.0.34`.

You can also run the **Publish** workflow manually and pass the version. Enable `dry_run` to build, test and validate the package without logging in to nuget.org or pushing it.

Also, you can see: 
* [NuGet package of the Saraff.Twain.NET](https://www.nuget.org/packages/Saraff.Twain/)
* [Saraff.Twain.NET Extensions (LINQ to TWAIN)](https://saraff-9eb1047a4beb4cef8506b29ba325bd5a.github.io/sarafftwainext/)
* [Wiki of the Saraff.Twain.NET](https://saraff-9eb1047a4beb4cef8506b29ba325bd5a.github.io/sarafftwain/wiki/)
* [Samples](https://saraff-9eb1047a4beb4cef8506b29ba325bd5a.github.io/sarafftwain/#samples)
