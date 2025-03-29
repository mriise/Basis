# Get the directory where the script is located
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Navigate up to the "Basis Foundation" level
$basisFoundationDir = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $scriptDir))

# Define the source and destination directories relative to "Basis Foundation"
$source = Join-Path $basisFoundationDir "Basis Unity\Basis Server"
$destination = Join-Path $basisFoundationDir "Basis Unity\Basis\Packages\com.basis.server"

# Ensure source exists before proceeding
if (-Not (Test-Path -Path $source)) {
    Write-Host "Source directory not found: $source"
    exit 1
}

# Remove all .cs files in the destination directory
Get-ChildItem -Path $destination -Recurse -Include *.cs | Remove-Item -Force

# Get all files from the source, excluding .dll files, .asmdef files, and obj folders
Get-ChildItem -Path $source -Recurse | Where-Object { 
    $_.Extension -notin @('.dll', '.asmdef') -and $_.FullName -notmatch '\\obj\\'
} | ForEach-Object {
    # Compute the relative path
    $relativePath = $_.FullName.Substring($source.Length)
    $destinationPath = Join-Path $destination $relativePath

    # Ensure the destination folder exists
    $destinationFolder = Split-Path -Parent $destinationPath
    if (-not (Test-Path -Path $destinationFolder)) {
        New-Item -ItemType Directory -Path $destinationFolder -Force
    }

    # Copy the file to the destination
    if (-not $_.PSIsContainer) {
        Copy-Item -Path $_.FullName -Destination $destinationPath -Force
    }
}

Write-Host "Files copied successfully!"