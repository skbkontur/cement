$userDir = $ENV:UserProfile

$binDir = Join-Path $userDir "bin"
$backupDir = Join-Path $userDir "cement.backup"
if(-not (Test-Path $binDir)){
	throw "Cement binaries directory not found at $binDir"	
}

if(Test-Path $backupDir){
	rm -Recurse -Force $backupDir
}

mkdir $backupDir | Out-Null

copy $binDir\* $backupDir -Recurse | Out-Null
Write-Host "Cement binaries backup created at $backupDir" -ForegroundColor Green


