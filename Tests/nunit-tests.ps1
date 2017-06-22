$script_dir = split-path -parent $MyInvocation.MyCommand.Definition
$prog_files_86 = ${env:ProgramFiles(x86)}
if ($prog_files_86 -eq $null)
{
	$prog_files_86 = ${env:ProgramFiles}
}
$nunit_dir = (Get-ChildItem $prog_files_86\nunit*\bin | `
		Sort-Object -Descending)[0]
if ($nunit_dir -eq $null)
{
	$host.ui.WriteErrorLine("Nunit framework can not be found!")
	Exit 1
}

echo $nunit_dir
& "$nunit_dir\nunit-console.exe" `
bin\Debug\Tests.dll
$saved = $LASTEXITCODE

Remove-Item TestResult.xml

Exit $saved
