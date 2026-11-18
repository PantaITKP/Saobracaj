# Instalacija CimRadionica na laptop: kopija u C:\CimRadionica\Program, prečica na Desktopu.
# Pokreće se iz foldera sa build-om (bin\Release\net462) ili sa share-a \\192.168.1.6\Cloude\Agenti\CimRadionica\Program.
# Bez tajni: konekcija se podešava u alatu (Podešavanja → Podesi konekciju) i čuva DPAPI-jem za trenutnog korisnika.
param([string]$Cilj = 'C:\CimRadionica')
$ErrorActionPreference = 'Stop'
$izvor = Split-Path -Parent $MyInvocation.MyCommand.Path
$program = Join-Path $Cilj 'Program'
New-Item -ItemType Directory -Force $program, (Join-Path $Cilj 'Radni\Osnovni'), (Join-Path $Cilj 'kes'), (Join-Path $Cilj 'sifarnici') | Out-Null
robocopy $izvor $program /E /XO /NFL /NDL /NJH /NJS /XF *.dpapi *.pdb | Out-Null
$sh = New-Object -ComObject WScript.Shell
$lnk = $sh.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'CimRadionica.lnk'))
$lnk.TargetPath = Join-Path $program 'CimRadionica.exe'
$lnk.WorkingDirectory = $program
$lnk.Description = 'CimRadionica – testiranje CimProvera (ne šalje mail)'
$lnk.Save()
Write-Host "CimRadionica instalirana u $program; prečica na Desktopu."
