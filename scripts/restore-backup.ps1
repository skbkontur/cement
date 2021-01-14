$userDir = $ENV:UserProfile

$binDir = Join-Path $userDir "bin"
$backupDir = Join-Path $userDir "cement.backup"

if(-not (Test-Path $backupDir)){
	throw "Cement binaries backup directory not found at $backupDir"	
}

if(Test-Path $binDir){
	rm $binDir -Recurse -Force | Out-Null
}

copy $backupDir\ $binDir -Recurse -Force 
Write-Host "Cement binaries restored from backup $backupDir to $binDir" -ForegroundColor Green
