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
Get-Dependency -TargetFile ".\win-x64.zip" `
               -TargetHash "3B53DFE537C057DF1AF35AAE05D79F19" `
               -Uri "https://github.com/sccn/liblsl/releases/download/v1.14.0/liblsl-1.14.0-Win_amd64.zip"
Get-Dependency -TargetFile ".\win-x86.zip" `
               -TargetHash "359E62226CABEFF98D7646BCED3173DD" `
               -Uri "https://github.com/sccn/liblsl/releases/download/v1.14.0/liblsl-1.14.0-Win_i386.zip"
Write-Host ("Building EmotionalCities.Lsl")
& dotnet build -c Release .\src\EmotionalCities.Lsl.sln