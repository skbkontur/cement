$regex = "($env:USERPROFILE\bin\|$env:USERPROFILE\bin)" -replace '\\','\\'; 
$env:Path = $env:Path -replace $regex,'';
$projRoot = (Resolve-Path $PSScriptRoot\..).Path
$env:Path = "$env:Path;$projRoot\Cement.Net\bin\Debug";


try {
	$cmPath  = $(Get-Command cm -ErrorAction SilentlyContinue).Path 
}
finally {
	if($cmPath)	
	{
		Write-Host "cement running from $cmPath" -ForegroundColor Blue
	}
	else
	{
		Write-Host "cm.exe command not found in PATH" -ForegroundColor Red
	}
}