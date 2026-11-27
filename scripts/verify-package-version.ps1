[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,

    [Parameter(Mandatory)]
    [string]$PackageId,

    [Parameter(Mandatory)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

function ConvertTo-ComparableVersion([string]$value) {
    $value = $value.Trim()

    $metadataIndex = $value.IndexOf('+')
    if ($metadataIndex -ge 0) {
        $value = $value.Substring(0, $metadataIndex)
    }

    $prerelease = ''
    $dashIndex = $value.IndexOf('-')
    if ($dashIndex -ge 0) {
        $prerelease = $value.Substring($dashIndex + 1).ToLowerInvariant()
        $value = $value.Substring(0, $dashIndex)
    }

    $parts = @($value.Split('.') | ForEach-Object { [int]$_ })
    while ($parts.Count -lt 4) {
        $parts += 0
    }

    $core = $parts -join '.'
    if ($prerelease) {
        return "$core-$prerelease"
    }

    return $core
}

function ConvertTo-AssemblyVersion([string]$value) {
    return (ConvertTo-ComparableVersion $value).Split('-', 2)[0]
}

$expectedAssemblyVersion = ConvertTo-AssemblyVersion $Version
$expectedPackageVersion = ConvertTo-ComparableVersion $Version

if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) {
    throw "Package '$PackagePath' was not found."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)

try {
    $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
    if (-not $nuspecEntry) {
        throw "Package '$PackagePath' does not contain a nuspec."
    }

    $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
    try {
        $nuspec = [xml]$reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }

    $packageIdInNuspec = [string]$nuspec.package.metadata.id
    $packageVersionInNuspec = [string]$nuspec.package.metadata.version

    if ($packageIdInNuspec -ne $PackageId) {
        throw "Unexpected package id '$packageIdInNuspec' in '$PackagePath'; expected '$PackageId'."
    }

    if ((ConvertTo-ComparableVersion $packageVersionInNuspec) -ne $expectedPackageVersion) {
        throw "Package version '$packageVersionInNuspec' in '$PackagePath' does not match expected version '$Version'."
    }

    $assemblyEntries = @($archive.Entries | Where-Object {
            $_.FullName -match "^lib/[^/]+/$([regex]::Escape($PackageId))\.dll$"
        })

    if ($assemblyEntries.Count -eq 0) {
        throw "Package '$PackagePath' does not contain lib/*/$PackageId.dll."
    }

    $tempAssembly = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString('N') + '.dll')

    try {
        foreach ($entry in $assemblyEntries) {
            $input = $entry.Open()
            try {
                $output = [System.IO.File]::Create($tempAssembly)
                try {
                    $input.CopyTo($output)
                }
                finally {
                    $output.Dispose()
                }
            }
            finally {
                $input.Dispose()
            }

            $assemblyVersion = ([System.Reflection.AssemblyName]::GetAssemblyName($tempAssembly)).Version.ToString()
            $fileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($tempAssembly).FileVersion

            if ($assemblyVersion -ne $expectedAssemblyVersion) {
                throw "Assembly version '$assemblyVersion' in '$($entry.FullName)' does not match package version '$Version' (expected '$expectedAssemblyVersion')."
            }

            if ($fileVersion -ne $expectedAssemblyVersion) {
                throw "File version '$fileVersion' in '$($entry.FullName)' does not match package version '$Version' (expected '$expectedAssemblyVersion')."
            }

            Write-Output "$($entry.FullName): assembly $assemblyVersion, file $fileVersion"
        }
    }
    finally {
        Remove-Item -LiteralPath $tempAssembly -Force -ErrorAction SilentlyContinue
    }
}
finally {
    $archive.Dispose()
}

Write-Output "Verified $PackageId $Version (assembly $expectedAssemblyVersion)."
