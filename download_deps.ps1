$targetDir = "D:\UnityProject\Unity_new_version\Assets\Plugins\NpgsqlDeps"
New-Item -ItemType Directory -Force -Path $targetDir

$packages = @(
    @{ Name = "System.Text.Json"; Version = "4.6.0"; LibPath = "lib\netstandard2.0\System.Text.Json.dll" },
    @{ Name = "System.Text.Encodings.Web"; Version = "4.6.0"; LibPath = "lib\netstandard2.0\System.Text.Encodings.Web.dll" },
    @{ Name = "Microsoft.Bcl.AsyncInterfaces"; Version = "1.1.0"; LibPath = "lib\netstandard2.0\Microsoft.Bcl.AsyncInterfaces.dll" },
    @{ Name = "System.Runtime.CompilerServices.Unsafe"; Version = "4.6.0"; LibPath = "lib\netstandard2.0\System.Runtime.CompilerServices.Unsafe.dll" },
    @{ Name = "System.Threading.Channels"; Version = "4.7.0"; LibPath = "lib\netstandard2.0\System.Threading.Channels.dll" }
)

foreach ($pkg in $packages) {
    $url = "https://www.nuget.org/api/v2/package/" + $pkg.Name + "/" + $pkg.Version
    $zipFile = $pkg.Name + ".zip"
    $extractDir = $pkg.Name + "_extracted"
    
    Write-Host "Downloading $($pkg.Name)..."
    Invoke-WebRequest -Uri $url -OutFile $zipFile
    Expand-Archive -Path $zipFile -DestinationPath $extractDir -Force
    
    $dllPath = Join-Path $extractDir $pkg.LibPath
    Copy-Item $dllPath -Destination $targetDir -Force
    
    Remove-Item $zipFile -Force
    Remove-Item $extractDir -Recurse -Force
}

Write-Host "Done downloading dependencies to $targetDir"
