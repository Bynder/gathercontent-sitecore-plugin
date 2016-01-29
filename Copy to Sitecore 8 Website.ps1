# Website root folder (website is deployed here)
$website_root = "D:\Data\Inetpub\GatherContent\Sitecore 8.1\Website"

# Temporarily change to the correct folder containing script
$scriptPath = (Get-Variable MyInvocation -Scope Script).Value.MyCommand.Path
$currentFolder = Split-Path $scriptPath
Push-Location $currentFolder

# Set src folder based on location of script location in 
$src = "Sitecore 8.0\Website"

# Exclude files and folders from deploy, usually these are
# source code files, proj files from Visual Studio, and other 
# files not required at runtime
$options = @("/E", "/S", "/xf", "*.cs", "/xf", "*.??proj", "/xf", "*.user", "/xf", "*.old", "/xf", "*.vspscc", "/xf", "xsltExtensions.config", "/xf", "*.tmp", "/xd", "_Resharper*", "/xd", ".svn", "/xd", "_svn", "/xf", "ServiceStack.*", "/xf", "System.*.dll") 

# Copy all site specific files into the website
& robocopy "$website_root\sitecore modules\Shell\GatherContent" "$src\sitecore modules\Shell\GatherContent" $options
# Now back to original directory
Pop-Location