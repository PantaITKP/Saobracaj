# HITNO ISKLJUCIVANJE agenta CimProvera (v3.0.5) - pokrenuti NA SERVERU, kao administrator:
#
#   powershell -ExecutionPolicy Bypass -File "\\192.168.1.6\Cloude\Agenti\CimProvera\Iskljuci-Hitno.ps1"
#
# Redosled (bez potvrde - svaki korak odmah):
#   1. fajl SLANJE_ZAUSTAVLJENO u folderu agenta: od ovog trenutka agent NE SALJE NISTA (proverava se pri svakom slanju i na
#      pocetku svakog prolaza - i proces koji upravo radi ga vidi pre sledeceg slanja)
#   2. zadatak obrade 'Saobracaj\Agent CimProvera' se iskljucuje (Disabled) i zaustavlja
#   3. proces agenta koji obradjuje postu se prekida (proces arhiviranja se ne dira)
#   4. provera i kratak izvestaj (stanje zadataka, poslednje linije loga, broj zadrzanih poruka u stanje\neposlato)
# Arhiviranje (07:10 i 15:10) radi i dalje. Iz sanduceta i baze se nista ne brise.
# Ponovno ukljucivanje: Ukljuci-TestRezim.ps1 (nova nulta tacka iz arhive, uklanja SLANJE_ZAUSTAVLJENO).
$ErrorActionPreference = 'Continue'
$Zadatak = 'Saobracaj\Agent CimProvera'

$id = [Security.Principal.WindowsIdentity]::GetCurrent()
if (-not (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host 'GRESKA  Pokrenite PowerShell kao administrator.' -ForegroundColor Red; Read-Host 'Enter za izlaz'; exit 1
}
$share = Get-WmiObject Win32_Share -Filter "Name='Cloude'"
$Folder = if ($share) { Join-Path $share.Path 'Agenti\CimProvera' } else { Split-Path -Parent $MyInvocation.MyCommand.Path }

# 1. zaustavljanje slanja
$stop = Join-Path $Folder 'SLANJE_ZAUSTAVLJENO'
Set-Content -Path $stop -Value ((Get-Date).ToString('yyyy-MM-dd HH:mm:ss') + ' ' + $env:USERNAME + ' - Iskljuci-Hitno.ps1') -Encoding ASCII
if (Test-Path $stop) { Write-Host "1. OK  SLANJE_ZAUSTAVLJENO: agent ne salje nista ($stop)" -ForegroundColor Green }
else { Write-Host "1. GRESKA  fajl $stop nije napravljen - nastavlja se sa iskljucivanjem zadatka" -ForegroundColor Red }

# 2. zadatak obrade
& schtasks /Change /TN $Zadatak /DISABLE 2>$null | Out-Null
$k1 = $LASTEXITCODE
& schtasks /End /TN $Zadatak 2>$null | Out-Null
$st = (Get-ScheduledTask -TaskPath '\Saobracaj\' -TaskName 'Agent CimProvera' -ErrorAction SilentlyContinue).State
if ($st -eq 'Disabled') { Write-Host "2. OK  zadatak '$Zadatak' je iskljucen" -ForegroundColor Green }
else { Write-Host "2. GRESKA  zadatak '$Zadatak' je '$st' (schtasks kod $k1) - iskljucite ga RUCNO u Task Scheduler-u" -ForegroundColor Red }

# 3. proces obrade (ne arhiviranje)
$procesi = @(Get-CimInstance Win32_Process -Filter "Name='Saobracaj.Agent.CimProvera.exe'" | Where-Object { $_.CommandLine -notmatch 'arhiviraj' })
foreach ($p in $procesi) { Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 2
$ostali = @(Get-CimInstance Win32_Process -Filter "Name='Saobracaj.Agent.CimProvera.exe'" | Where-Object { $_.CommandLine -notmatch 'arhiviraj' })
if ($ostali.Count -eq 0) { Write-Host "3. OK  proces obrade ne radi (prekinuto: $($procesi.Count))" -ForegroundColor Green }
else { Write-Host "3. GRESKA  proces obrade i dalje radi (PID $($ostali.ProcessId -join ', ')) - zaustavite ga u Task Manager-u" -ForegroundColor Red }

# 4. izvestaj
Write-Host ''
Write-Host '4. Stanje:'
$arh = Get-ScheduledTask -TaskPath '\Saobracaj\' -TaskName 'Arhiva CimProvera' -ErrorAction SilentlyContinue
Write-Host "   arhiviranje: $(if ($arh) { $arh.State } else { 'nema zadatka' }) (ne dira se)"
$cfgF = Join-Path $Folder 'Saobracaj.Agent.CimProvera.exe.config'
[xml]$x = Get-Content $cfgF -Encoding UTF8
$st = $x.configuration.appSettings.add | Where-Object { $_.key -eq 'StanjeFolder' } | Select-Object -First 1
$nep = if ($st) { Join-Path $st.value 'neposlato' } else { $null }
if ($nep -and (Test-Path $nep)) { Write-Host "   zadrzane (neposlate) poruke: $(@(Get-ChildItem $nep -Filter *.eml).Count) u $nep" }
$log = Join-Path $Folder ("logs\CimProvera-" + (Get-Date).ToString('yyyyMMdd') + '.log')
if (Test-Path $log) { Write-Host "   poslednje linije loga ($log):"; Get-Content $log -Encoding UTF8 -Tail 8 | ForEach-Object { Write-Host "     $_" } }
Write-Host ''
Write-Host 'Agent je ZAUSTAVLJEN. Ponovno ukljucivanje: Ukljuci-TestRezim.ps1 (nova nulta tacka iz arhive).' -ForegroundColor Yellow
Read-Host 'Enter za izlaz'
