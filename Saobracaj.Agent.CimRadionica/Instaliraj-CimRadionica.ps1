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

# v3.0.1 (4.3): Syncfusion licencni kljuc (Word prilozi -> PDF) za nalog korisnika na laptopu: unosi ga korisnik, cuva se samo
# sifrovan (DPAPI, trenutni korisnik) u %LOCALAPPDATA%\Saobracaj\CimProvera\Syncfusion.dpapi. Kljuc ne ide u repo, log ni na ekran.
$sfFajl = Join-Path $env:LOCALAPPDATA 'Saobracaj\CimProvera\Syncfusion.dpapi'
$pitaj = $true
if (Test-Path $sfFajl) { $pitaj = (Read-Host "Syncfusion.dpapi vec postoji ($sfFajl). Uneti novi kljuc? (d/N)") -match '^[dDyY]' }
if ($pitaj) {
    $sec = Read-Host 'Syncfusion kljuc (desni klik = nalepi; Enter = bez kljuca, Word prilozi tada idu na rucnu proveru)' -AsSecureString
    $b = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
    try { $env:CIMPROVERA_SYNCFUSION = ([Runtime.InteropServices.Marshal]::PtrToStringBSTR($b) -replace '\s', '') } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($b) }
    if ([string]::IsNullOrWhiteSpace($env:CIMPROVERA_SYNCFUSION)) {
        Write-Host 'PAZNJA  kljuc nije unet: Word prilozi se ne pretvaraju u PDF (rucna provera); alat to javlja na pocetku obrade.' -ForegroundColor Yellow
    } else {
        $pr = Start-Process -FilePath (Join-Path $program 'CimRadionica.exe') -ArgumentList '--podesi-syncfusion' -Wait -PassThru -NoNewWindow
        if ($pr.ExitCode -eq 0) { Write-Host "Syncfusion kljuc sacuvan (DPAPI) u $sfFajl." } else { Write-Host "PAZNJA  kljuc nije sacuvan ili nije ispravan (kod $($pr.ExitCode))." -ForegroundColor Yellow }
    }
    Remove-Item Env:\CIMPROVERA_SYNCFUSION -ErrorAction SilentlyContinue
}
