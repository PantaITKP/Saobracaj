# Ukljucivanje TEST rezima agenta CimProvera (v3.0.5) - pokrenuti NA SERVERU, kao administrator:
#
#   powershell -ExecutionPolicy Bypass -File "\\192.168.1.6\Cloude\Agenti\CimProvera\Ukljuci-TestRezim.ps1" -Suvo
#       samo provera: config, zadaci, Syncfusion, i koja bi nulta tacka bila (iz poslednjeg arhiviranja) - nista se ne menja
#   powershell -ExecutionPolicy Bypass -File "\\192.168.1.6\Cloude\Agenti\CimProvera\Ukljuci-TestRezim.ps1"
#       ukljucivanje (redosled je obavezan):
#         1. provera config-a: Rezim = Test, TestPrimalac = agenti@kprevoz.co.rs, IzvorUpisa = Agent, UpisUBazu = true, tajne postoje
#         2. zadatak obrade 'Saobracaj\Agent CimProvera' mora biti ISKLJUCEN, zadatak arhiviranja mora postojati
#         3. arhiviranje se pokrece odmah i ceka se kraj (arhiva\izvestaj.txt, bez rupa)
#         4. nulta tacka = poslednji UID pokriven arhivom (komanda nulta-tacka-arhiva; rezervna kopija stanja; red u nulta_tacka.txt)
#         5. uklanja se fajl hitnog iskljucivanja SLANJE_ZAUSTAVLJENO (ako postoji)
#         6. zadatak obrade se ukljucuje i odmah pokrece; prikazuje se pocetak loga
#   Hitno iskljucivanje: Iskljuci-Hitno.ps1 (isti folder).
#
# Agent upisuje u planiranje.tmp* sa Izvor = 'Agent'; podatke alata (Izvor = 'Alat') ne dira. U rezimu Test sve ide ISKLJUCIVO na
# agenti@kprevoz.co.rs (provera pri svakom slanju). Skripta je namerno bez slova sa kvacicama (PowerShell 5.1 cita .ps1 bez BOM-a kao ANSI).
param([switch]$Suvo)

$ErrorActionPreference = 'Stop'
$Zadatak  = 'Saobracaj\Agent CimProvera'
$ZadArh   = 'Arhiva CimProvera'
$Testna   = 'agenti@kprevoz.co.rs'
$Nalog    = 'svc_cimprovera'

function Korak($t) { Write-Host ''; Write-Host "=== $t" -ForegroundColor Cyan }
function Ok($t)    { Write-Host "    OK  $t" -ForegroundColor Green }
function Paznja($t){ Write-Host "    PAZNJA  $t" -ForegroundColor Yellow }
function Stop-Greska($t) { Write-Host "    GRESKA  $t" -ForegroundColor Red; Write-Host '    Nista nije ukljuceno.' -ForegroundColor Red; Write-Host ''; Read-Host 'Enter za izlaz'; exit 1 }

$id = [Security.Principal.WindowsIdentity]::GetCurrent()
if (-not (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { Stop-Greska 'Pokrenite PowerShell kao administrator.' }
$share = Get-WmiObject Win32_Share -Filter "Name='Cloude'"
if ($share -eq $null) { Stop-Greska 'Share Cloude ne postoji na ovom racunaru. Skripta se pokrece na serveru 192.168.1.6.' }
$Folder = Join-Path $share.Path 'Agenti\CimProvera'
$Exe    = Join-Path $Folder 'Saobracaj.Agent.CimProvera.exe'
$Cfg    = $Exe + '.config'
if (-not (Test-Path $Exe)) { Stop-Greska "Nema $Exe" }
$verzija = (Get-Item (Join-Path $Folder 'Saobracaj.Agent.CimProvera.Jezgro.dll')).VersionInfo.ProductVersion
Korak "Agent: $Folder (program $((Get-Item $Exe).VersionInfo.ProductVersion), jezgro $verzija)$(if ($Suvo) { ' - SUVO, nista se ne menja' })"

# ------------------------------------------------------------------ 1. config
Korak '1. Config (Saobracaj.Agent.CimProvera.exe.config)'
[xml]$x = Get-Content $Cfg -Encoding UTF8
function K($k) { $n = $x.configuration.appSettings.add | Where-Object { $_.key -eq $k } | Select-Object -First 1; if ($n) { $n.value } else { $null } }
if ((K 'Rezim') -ne 'Test') { Stop-Greska "Rezim = '$(K 'Rezim')' - za Test rezim mora tacno 'Test'." }
Ok 'Rezim = Test (prekidac: slanje iskljucivo na agenti@, provera pri svakom slanju)'
foreach ($k in 'TestPrimalac', 'IzvestajAdresa') { $v = K $k; if ($v -and $v -ne $Testna) { Stop-Greska "$k = '$v' - mora biti $Testna ili prazno." } }
Ok "TestPrimalac / IzvestajAdresa = $Testna"
$izv = K 'IzvorUpisa'; if ($izv -and $izv -ne 'Agent') { Stop-Greska "IzvorUpisa = '$izv' - agent pise samo sa Izvor = 'Agent'." }
Ok "IzvorUpisa = Agent (podaci alata, Izvor = 'Alat', se ne diraju)"
if ((K 'UpisUBazu') -ne 'true') { Paznja "UpisUBazu = '$(K 'UpisUBazu')' - poslovi se NE upisuju u planiranje.tmp*." } else { Ok 'UpisUBazu = true' }
foreach ($k in 'Lozinka.dpapi', 'Konekcija.dpapi') { if (-not (K $k)) { Stop-Greska "Nema $k u config-u (pokrenuti Instaliraj.ps1 -ObradaIskljucena)." } }
Ok 'Lozinka.dpapi i Konekcija.dpapi postoje (sadrzaj se ne prikazuje)'
$sat = K 'DnevniSazetakSat'; if (-not $sat) { $sat = '7' }
Ok "dnevni sazetak na $Testna u prvom prolazu posle ${sat}:00 (za prethodni dan)"

# Syncfusion kljuc (Word -> PDF): sifrovan pod servisnim nalogom; posle nove lozinke naloga stari Syncfusion.dpapi ne vazi
$sf = Join-Path $Folder 'Syncfusion.dpapi'
$u = [ADSI]"WinNT://$env:COMPUTERNAME/$Nalog,user"
$lozinkaOd = (Get-Date).AddSeconds(-[double]$u.PasswordAge.Value)
if (-not (Test-Path $sf)) { Paznja 'nema Syncfusion.dpapi: Word prilozi idu na rucnu proveru.' }
elseif ((Get-Item $sf).LastWriteTime -lt $lozinkaOd.AddMinutes(-1)) {
    Paznja "Syncfusion.dpapi ($((Get-Item $sf).LastWriteTime.ToString('dd.MM.yyyy HH:mm'))) je stariji od lozinke $Nalog ($($lozinkaOd.ToString('dd.MM.yyyy HH:mm'))) - ne moze da se desifruje: Word prilozi idu na rucnu proveru."
    Write-Host '            Za kljuc: PRE ove skripte pokrenuti Instaliraj.ps1 -ObradaIskljucena -SaSyncfusion (trazi lozinku sanduceta i Syncfusion kljuc).'
} else { Ok 'Syncfusion.dpapi je noviji od lozinke servisnog naloga' }

# ------------------------------------------------------------------ 2. zadaci
Korak '2. Zadaci u Task Scheduler-u'
$zad = Get-ScheduledTask -TaskPath '\Saobracaj\' -TaskName 'Agent CimProvera' -ErrorAction SilentlyContinue
if ($zad -eq $null) { Stop-Greska "Nema zadatka '$Zadatak' (pokrenuti Instaliraj.ps1 -ObradaIskljucena)." }
if ($zad.State -ne 'Disabled') { Stop-Greska "Zadatak '$Zadatak' nije iskljucen (stanje: $($zad.State)) - nulta tacka se postavlja samo dok je iskljucen. Za hitno: Iskljuci-Hitno.ps1." }
Ok "'$Zadatak' je iskljucen"
$arh = Get-ScheduledTask -TaskPath '\Saobracaj\' -TaskName $ZadArh -ErrorAction SilentlyContinue
if ($arh -eq $null) { Stop-Greska "Nema zadatka 'Saobracaj\$ZadArh' (Instaliraj.ps1 -ObradaIskljucena)." }
Ok "'Saobracaj\$ZadArh' postoji ($($arh.State))"
$stop = Join-Path $Folder 'SLANJE_ZAUSTAVLJENO'
if (Test-Path $stop) { Paznja "postoji fajl hitnog iskljucivanja SLANJE_ZAUSTAVLJENO ($((Get-Content $stop -Raw).Trim())) - uklanja se u koraku 5." }

if ($Suvo) {
    Korak 'SUVO: nulta tacka iz poslednjeg arhiviranja (nista se ne upisuje)'
    & $Exe 'nulta-tacka-arhiva' '--suvo' '--max-min' '100000' | ForEach-Object { Write-Host "    $_" }
    Write-Host ''
    Write-Host 'SUVO - nista nije promenjeno. Za ukljucivanje pokrenuti skriptu bez -Suvo.' -ForegroundColor Green
    Read-Host 'Enter za izlaz'; exit 0
}

Write-Host ''
$potvrda = Read-Host "Ukljuciti Test rezim (arhiviranje -> nulta tacka -> zadatak obrade UKLJUCEN)? Upisite DA"
if ($potvrda -ne 'DA') { Write-Host 'Odustalo - nista nije promenjeno.'; exit 0 }

# ------------------------------------------------------------------ 3. arhiviranje sada
Korak "3. Arhiviranje sada (zadatak 'Saobracaj\$ZadArh')"
$pocetak = Get-Date
if ($arh.State -eq 'Running') { Write-Host '    arhiviranje vec radi - ceka se kraj' } else { Start-ScheduledTask -TaskPath '\Saobracaj\' -TaskName $ZadArh }
Start-Sleep -Seconds 5
$rok = (Get-Date).AddMinutes(45)
do {
    Start-Sleep -Seconds 10
    $st = (Get-ScheduledTask -TaskPath '\Saobracaj\' -TaskName $ZadArh).State
    Write-Host "    ... $((Get-Date).ToString('HH:mm:ss')) $st"
} while ($st -eq 'Running' -and (Get-Date) -lt $rok)
if ($st -eq 'Running') { Stop-Greska 'Arhiviranje traje duze od 45 min - sacekati kraj i pokrenuti skriptu ponovo.' }
$info = Get-ScheduledTaskInfo -TaskPath '\Saobracaj\' -TaskName $ZadArh
if ($info.LastTaskResult -ne 0) { Stop-Greska "Arhiviranje je zavrsilo sa kodom $($info.LastTaskResult) (log: $Folder\logs)." }
$izvF = Join-Path $Folder 'arhiva\izvestaj.txt'
if (-not (Test-Path $izvF) -or (Get-Item $izvF).LastWriteTime -lt $pocetak.AddMinutes(-1)) { Stop-Greska 'arhiva\izvestaj.txt nije osvezen ovim arhiviranjem.' }
Get-Content $izvF -Encoding UTF8 | ForEach-Object { Write-Host "    $_" }
Ok 'arhiviranje zavrseno'

# ------------------------------------------------------------------ 4. nulta tacka
Korak '4. Nulta tacka = poslednji UID pokriven arhivom'
$ErrorActionPreference = 'Continue'
$izlaz = & $Exe 'nulta-tacka-arhiva' '--max-min' '30' 2>&1
$kod = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
$izlaz | ForEach-Object { Write-Host "    $_" }
if ($kod -ne 0) { Stop-Greska 'Nulta tacka nije postavljena (vidi gore).' }
$red = Get-Content (Join-Path $Folder 'nulta_tacka.txt') -Encoding UTF8 | Select-Object -Last 1
Ok 'nulta tacka upisana (nulta_tacka.txt)'

# ------------------------------------------------------------------ 5. fajl hitnog iskljucivanja
Korak '5. Fajl hitnog iskljucivanja'
if (Test-Path $stop) { Remove-Item $stop -Force; Ok 'SLANJE_ZAUSTAVLJENO uklonjen' } else { Ok 'ne postoji' }

# ------------------------------------------------------------------ 6. ukljucivanje zadatka obrade
Korak "6. Zadatak '$Zadatak' se ukljucuje (na 2 minuta) i pokrece"
Enable-ScheduledTask -TaskPath '\Saobracaj\' -TaskName 'Agent CimProvera' | Out-Null
Start-ScheduledTask -TaskPath '\Saobracaj\' -TaskName 'Agent CimProvera'
Ok 'zadatak ukljucen i pokrenut'
Write-Host '    ceka se prvi prolaz (do 3 min) ...'
$log = Join-Path $Folder ("logs\CimProvera-" + (Get-Date).ToString('yyyyMMdd') + '.log')
$rok = (Get-Date).AddMinutes(3)
do { Start-Sleep -Seconds 15; $gotov = (Test-Path $log) -and ((Get-Content $log -Encoding UTF8 -Tail 400 | Select-String 'Kraj: obra|INTERNO: prolaz|Rad sa sandu') -ne $null) } while (-not $gotov -and (Get-Date) -lt $rok)
if (Test-Path $log) {
    Get-Content $log -Encoding UTF8 -Tail 400 | Where-Object { $_ -match 'Program |Prekida|Nulta|Sandu|novih poruka|Kraj:|ERROR|INTERNO' } | Select-Object -Last 15 | ForEach-Object { Write-Host "    $_" }
}

Write-Host ''
Write-Host 'GOTOVO. Test rezim je UKLJUCEN: agent obradjuje postu posle nulte tacke, sve odgovore salje ISKLJUCIVO na agenti@kprevoz.co.rs.' -ForegroundColor Green
Write-Host "Nulta tacka (za ODLUKE 15): $red"
Write-Host "Hitno iskljucivanje: powershell -ExecutionPolicy Bypass -File `"$Folder\Iskljuci-Hitno.ps1`""
Write-Host ''
Read-Host 'Enter za izlaz'
