$BaseFolder = ".\src\liblsl"
function Get-Dependency {
    param (
        $TargetFile,
        $TargetHash,
        $Uri
    )

    $destinationPath = [System.IO.Path]::GetFileNameWithoutExtension($TargetFile)
    $destinationPath = Join-Path -Path "$BaseFolder" -ChildPath $destinationPath
    if (Test-Path $destinationPath) { return }

    $webclient = New-Object System.Net.WebClient
    $webclient.DownloadFile($Uri, $TargetFile)
    $fileHash = Get-FileHash $TargetFile -Algorithm MD5
    Write-Output $fileHash
    if ($TargetHash -eq $fileHash.Hash)
    {
        Expand-Archive -LiteralPath "$TargetFile" -DestinationPath "$destinationPath"
    }
    Remove-Item $TargetFile
}

Write-Host ("Downloading Dependencies")
Get-Dependency -TargetFile ".\liblsl-1.16.0-Win_amd64.zip" `
               -TargetHash "6C70ACF16ECDA3CBD657F17D388079CD" `
               -Uri "https://github.com/sccn/liblsl/releases/download/v1.16.0/liblsl-1.16.0-Win_amd64.zip"
Get-Dependency -TargetFile ".\liblsl-1.16.0-Win_i386.zip" `
               -TargetHash "E5CAB1330CB42E665FB74B343A859DA4" `
               -Uri "https://github.com/sccn/liblsl/releases/download/v1.16.0/liblsl-1.16.0-Win_i386.zip"
Write-Host ("Building Bonsai.Lsl")
& dotnet build -c Release .\src\Bonsai.Lsl.sln