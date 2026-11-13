#Requires -RunAsAdministrator
<#
  Potpun reset agenta CimProvera (v2.8.2) – pokreće se NA SERVERU, kao administrator, iz foldera programa:
      powershell -ExecutionPolicy Bypass -File .\Reset-CimProvera.ps1            (spisak, potvrda „DA“, reset)
      powershell -ExecutionPolicy Bypass -File .\Reset-CimProvera.ps1 -Suvo      (samo spisak, ništa se ne briše)

  BRIŠE SE:
    - StanjeFolder (C:\ProgramData\Saobracaj\CimProvera\stanje\*): stanje obrađenih mailova (UID, Message-ID) za oba
      sandučeta (agenti@ i staro stefan.obradovic@), listovi.json, keš pročitanih strana i OCR-a, kopija pravila
      (pravila_kes – pravi se ponovo), arhive ranijih resetova;
    - RezultatiFolder (testovi\rezultati\*): izveštaji, .eml, .html, .txt, PDF primeri, slike, _upisi.txt;
    - logs\CimProvera-*.log.
  NE BRIŠE SE: config (.exe.config, šifrovana lozinka Lozinka.dpapi u config-u), Syncfusion.dpapi, pravila\ i
    pravila\istorija\, program (exe, dll), istorija\, testovi\nezavisni\, zakazani zadatak.
    (Word/Excel → PDF radi u memoriji – privremenih fajlova nema.)

  Redosled (radi ga program, komanda reset-potpun): isključi zakazani zadatak → sačekaj da se tekući prolaz završi →
  zapiši vreme reseta (nulta tačka) → obriši →
  ponovo uključi zadatak. Na kraju skripta odmah pokreće zadatak: u novom logu piše verzija programa i pravila i
  „Nulta tačka zapisana: vreme reseta …, UID …“ (najveći UID poruka primljenih pre reseta – one se nikad ne obrađuju;
  mail primljen posle reseta, i pre prvog kruga, se obrađuje). Nalogu zadatka se daje samo pravo Modify, samo nad
  folderima stanja, logova i rezultata.
#>
param([switch]$Suvo, [string]$Zadatak = 'Saobracaj\Agent CimProvera')
$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe = Join-Path $dir 'Saobracaj.Agent.CimProvera.exe'
if (-not (Test-Path $exe)) { throw "Nema $exe – skripta se pokreće iz foldera programa." }

Write-Host '=== Spisak za brisanje ===' -ForegroundColor Cyan
& $exe reset-potpun --suvo --zadatak $Zadatak
if ($LASTEXITCODE -ne 0) { throw 'Spisak nije napravljen – ništa nije obrisano.' }
if ($Suvo) { Write-Host 'Režim -Suvo: ništa nije obrisano.'; return }

$odg = Read-Host 'Obrisati navedeno? Otkucajte DA'
if ($odg -cne 'DA') { Write-Host 'Prekinuto – ništa nije obrisano.'; return }

Write-Host '=== Reset ===' -ForegroundColor Cyan
& $exe reset-potpun --zadatak $Zadatak
if ($LASTEXITCODE -ne 0) { throw 'Reset nije uspeo – pogledajte poruke iznad (zadatak je ponovo uključen ako je bio isključen).' }

# Fajlove koje je napravio administrator (novo stanje.json, log) mora da menja nalog pod kojim radi zadatak
$xml = (schtasks.exe /Query /TN "$Zadatak" /XML) -join "`n"
$korisnik = [regex]::Match($xml, '<UserId>([^<]+)</UserId>').Groups[1].Value
[xml]$cfg = Get-Content (Join-Path $dir 'Saobracaj.Agent.CimProvera.exe.config') -Raw
function Podesavanje($k, $d) { $v = ($cfg.configuration.appSettings.add | Where-Object { $_.key -eq $k }).value; if (-not $v) { $v = $d }; if ([IO.Path]::IsPathRooted($v)) { $v } else { Join-Path $dir $v } }
if ($korisnik) {
  $nalog = if ($korisnik -match '^S-1-') { "*$korisnik" } else { $korisnik }
  foreach ($p in @((Podesavanje 'StanjeFolder' 'stanje'), (Podesavanje 'LogFolder' 'logs'), (Podesavanje 'RezultatiFolder' 'testovi\rezultati'))) {
    if (Test-Path $p) { icacls.exe "$p" /grant "${nalog}:(OI)(CI)M" /T /C | Out-Null; Write-Host "Pravo Modify za nalog zadatka ($korisnik): $p" }
  }
} else { Write-Warning 'Nalog zadatka nije pročitan – proverite da nalog zadatka može da menja stanje i logove.' }

Write-Host '=== Prvi prolaz ===' -ForegroundColor Cyan
schtasks.exe /Run /TN "$Zadatak" | Out-Host
Write-Host 'Gotovo. Za 1–2 minuta u logs\ nastaje novi log sa verzijom 2.8.2 i nultom tačkom (UID).'
