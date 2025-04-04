
$dir = $PSScriptRoot
$ErrorActionPreference = "Stop"

Write-Host "Running build.ps1..."
& "$dir\windows_build.ps1"
if ($LASTEXITCODE) {
    exit $LASTEXITCODE
}

#
#

$mod_name = "SimplifiedMoveset"
$dll_name = "$mod_name.dll"
$pdb_name = "$mod_name.pdb"

$downloads_path = "$HOME\downloads"
$zip_path = "$downloads_path\$mod_name.zip"

Push-Location $dir

7z a $zip_path "$mod_name\plugins\$dll_name" "$mod_name\plugins\$pdb_name" "$mod_name\modinfo.json" "$mod_name\thumbnail.png" "$mod_name\workshopdata.json"

Pop-Location

Write-Host "Archive created at: $zip_path"

