# Instalacija agenta Saobracaj.Agent.CimProvera na serveru (pokrenuti NA SERVERU, kao administrator):
#
#   powershell -ExecutionPolicy Bypass -File "\\192.168.1.6\Cloude\Agenti\CimProvera\Instaliraj.ps1"
#
# Po obrascu TelegramiImport: provera .NET-a, portova i OCR-a, servisni nalog (lozinka se generise sama), prava na
# folder, sifrovanje lozinke maila (DPAPI pod servisnim nalogom), test, probno pokretanje i zadatak u Task
# Scheduler-u na 2 minuta. Moze da se pokrene vise puta (i za promenu lozinke). Bez API kljuceva: dokumenti se
# citaju lokalno (tekst iz PDF-a + Tesseract OCR), nista ne izlazi iz firme.
# Bez baze: stanje i kes procitanih dokumenata su lokalno na serveru (C:\ProgramData\Saobracaj\CimProvera),
# dostupni samo servisnom nalogu i administratorima.
# Skripta je namerno bez slova sa kvacicama (PowerShell 5.1 cita .ps1 bez BOM-a kao ANSI).

$ErrorActionPreference = 'Stop'
$Nalog   = 'svc_cimprovera'
$Zadatak = 'Saobracaj\Agent CimProvera'
$Stanje  = 'C:\ProgramData\Saobracaj\CimProvera\stanje'

function Korak($t) { Write-Host ''; Write-Host "=== $t" -ForegroundColor Cyan }
function Ok($t)    { Write-Host "    OK  $t" -ForegroundColor Green }
function Stop-Greska($t) { Write-Host "    GRESKA  $t" -ForegroundColor Red; Write-Host ''; Read-Host 'Enter za izlaz'; exit 1 }
function Obican([Security.SecureString]$s) {
    $p = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($s)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($p) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($p) }
}
function Port($h, $p) {
    try {
        $tcp = New-Object Net.Sockets.TcpClient
        $ar = $tcp.BeginConnect($h, $p, $null, $null)
        if (-not $ar.AsyncWaitHandle.WaitOne(8000) -or -not $tcp.Connected) { throw 'timeout' }
        $tcp.Close(); Ok "${h}:$p dostupan"
    } catch { Stop-Greska "Server ne moze da se poveze na ${h}:$p (firewall?)." }
}

# Spoljni alati (takeown, icacls, secedit, schtasks): poruke na stderr ne prekidaju skriptu; uspeh = izlazni kod.
function Alat($exe, [string[]]$argumenti) {
    $stari = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { & $exe @argumenti 2>$null | Out-Null; return $LASTEXITCODE } finally { $ErrorActionPreference = $stari }
}
# ------------------------------------------------------------------ 0. administrator i lokalni folder
$id = [Security.Principal.WindowsIdentity]::GetCurrent()
if (-not (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Stop-Greska 'Pokrenite PowerShell kao administrator (desni klik > Run as administrator).'
}
$share = Get-WmiObject Win32_Share -Filter "Name='Cloude'"
if ($share -eq $null) { Stop-Greska 'Share Cloude ne postoji na ovom racunaru. Skripta se pokrece na serveru 192.168.1.6.' }
$Folder = Join-Path $share.Path 'Agenti\CimProvera'
$Exe    = Join-Path $Folder 'Saobracaj.Agent.CimProvera.exe'
$Cfg    = $Exe + '.config'
if (-not (Test-Path $Exe)) { Stop-Greska "Nema $Exe" }
$Racunar = $env:COMPUTERNAME
$PunNalog = "$Racunar\$Nalog"
Korak "Folder agenta: $Folder"

# ------------------------------------------------------------------ 1. .NET, mail server, Claude API
Korak '1. Provera servera'
$rel = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue).Release
if ($rel -eq $null -or $rel -lt 394802) { Stop-Greska ".NET Framework 4.6.2 ili noviji nije instaliran (Release=$rel)." }
Ok ".NET Framework (Release $rel)"
Port 'mail.kprevoz.co.rs' 993
Port 'mail.kprevoz.co.rs' 465
# Tesseract OCR (lokalno, bez interneta) trazi Visual C++ 2015-2022 Redistributable (x86)
$vc = Get-ItemProperty 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x86' -ErrorAction SilentlyContinue
if ($vc -eq $null) { $vc = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86' -ErrorAction SilentlyContinue }
if ($vc -eq $null -or $vc.Installed -ne 1 -or $vc.Minor -lt 20) {
    Stop-Greska 'Nije instaliran Visual C++ 2015-2022 Redistributable (x86), potreban za OCR. Instalirajte vc_redist.x86.exe (Microsoft, besplatno) i ponovo pokrenite skriptu.'
}
Ok "Visual C++ Redistributable x86 ($($vc.Version))"
if (-not (Test-Path (Join-Path $Folder 'tessdata\eng.traineddata'))) { Stop-Greska "Nema OCR jezika u $Folder\tessdata (pokrenuti tessdata\Preuzmi.ps1)." }
Ok 'OCR jezici (tessdata)'

# ------------------------------------------------------------------ 2. servisni nalog
Korak "2. Servisni nalog $PunNalog"
$znakovi = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!#%+=-_'
$bajtovi = New-Object byte[] 24
(New-Object Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bajtovi)
$SvcLozinka = 'Aa1!' + (-join ($bajtovi | ForEach-Object { $znakovi[$_ % $znakovi.Length] }))

$racunarAdsi = [ADSI]"WinNT://$Racunar,computer"
$postoji = @(Get-WmiObject Win32_UserAccount -Filter "LocalAccount=True AND Name='$Nalog'").Count -gt 0
if ($postoji) {
    $u = [ADSI]"WinNT://$Racunar/$Nalog,user"
    $u.SetPassword($SvcLozinka)
    Ok 'nalog postoji, lozinka je nova (zadatak i sifrovane vrednosti se prave ponovo)'
} else {
    $u = $racunarAdsi.Create('User', $Nalog)
    $u.SetPassword($SvcLozinka)
    $u.SetInfo()
    $u.Put('Description', 'Agent Saobracaj CimProvera (Task Scheduler)')
    $u.SetInfo()
    Ok 'nalog napravljen'
    $u = [ADSI]"WinNT://$Racunar/$Nalog,user"
}
$u.UserFlags.Value = $u.UserFlags.Value -bor 0x10000   # lozinka ne istice
$u.SetInfo()
$imeUsers = (New-Object Security.Principal.SecurityIdentifier('S-1-5-32-545')).Translate([Security.Principal.NTAccount]).Value.Split('\')[-1]
$grupa = [ADSI]"WinNT://$Racunar/$imeUsers,group"
$clan = $false
foreach ($m in @($grupa.psbase.Invoke('Members'))) { if ($m.GetType().InvokeMember('Name', 'GetProperty', $null, $m, $null) -eq $Nalog) { $clan = $true } }
if (-not $clan) { $grupa.Add("WinNT://$Racunar/$Nalog,user") }
Ok "clan grupe $imeUsers, lozinka ne istice"

if ((Alat 'icacls' @($Folder, '/grant', "${Nalog}:(OI)(CI)M", '/T', '/C', '/Q')) -ne 0) { Stop-Greska 'icacls nije uspeo (folder agenta)' }
Ok 'pravo izmene na folder agenta (logovi, istorija pravila)'

New-Item -ItemType Directory -Force $Stanje | Out-Null
$StanjeKoren = Split-Path $Stanje
# Fajlove je pri ranijem pokretanju pravio servisni nalog (vlasnik): administratori prvo preuzimaju vlasnistvo,
# zatim se zakljucava samo koren foldera, a sve ispod nasledjuje ista prava.

[void](Alat 'takeown' @('/F', $StanjeKoren, '/A', '/R', '/D', 'Y'))   # vlasnik = Administrators (i fajlovi bez ikakvih prava)
if ((Alat 'icacls' @($StanjeKoren, '/inheritance:r', '/grant:r', '*S-1-5-32-544:(OI)(CI)F', '*S-1-5-18:(OI)(CI)F', "${Nalog}:(OI)(CI)M", '/Q')) -ne 0) {
    Stop-Greska 'icacls nije uspeo (folder stanja)'
}
if (Get-ChildItem $StanjeKoren -Force) {
    if ((Alat 'icacls' @((Join-Path $StanjeKoren '*'), '/reset', '/T', '/C', '/Q')) -ne 0) {
        # Poslednja mera: kes i stanje mogu da se naprave iznova (stanje.json se cuva ako je dostupan).
        Write-Host '    PAZNJA  neki fajlovi u folderu stanja ne mogu da se otkljucaju; kes se brise i pravi iznova' -ForegroundColor Yellow
        Get-ChildItem $StanjeKoren -Recurse -Force -File | Where-Object { $_.Name -ne 'stanje.json' } | Remove-Item -Force -ErrorAction SilentlyContinue
    }
}
Ok "lokalno stanje i kes: $Stanje (samo administratori i $Nalog)"

# Pravo "Log on as a batch job" (SeBatchLogonRight): bez njega Task Scheduler ne pokrece zadatak pod ovim nalogom
# kada niko nije prijavljen. Postojeci nalozi sa tim pravom se zadrzavaju.
$sid = (New-Object Security.Principal.NTAccount($PunNalog)).Translate([Security.Principal.SecurityIdentifier]).Value
$tmpDir = Join-Path $env:TEMP ('cimprovera_secedit_' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $tmpDir | Out-Null
try {
    $izvoz = Join-Path $tmpDir 'izvoz.inf'
    [void](Alat 'secedit' @('/export', '/cfg', $izvoz, '/areas', 'USER_RIGHTS', '/quiet'))
    $red = (Get-Content $izvoz -Encoding Unicode | Where-Object { $_ -match '^\s*SeBatchLogonRight\s*=' } | Select-Object -First 1)
    $postojeci = if ($red) { ($red -split '=', 2)[1].Trim() } else { '' }
    if ($postojeci -match [regex]::Escape($sid)) {
        Ok 'pravo "Log on as a batch job" vec postoji'
    } else {
        $novi = if ($postojeci) { "$postojeci,*$sid" } else { "*$sid" }
        $uvoz = Join-Path $tmpDir 'uvoz.inf'
        @('[Unicode]', 'Unicode=yes', '[Version]', 'signature="$CHICAGO$"', 'Revision=1', '[Privilege Rights]', "SeBatchLogonRight = $novi") |
            Set-Content $uvoz -Encoding Unicode
        if ((Alat 'secedit' @('/configure', '/db', (Join-Path $tmpDir 'secedit.sdb'), '/cfg', $uvoz, '/areas', 'USER_RIGHTS', '/quiet')) -ne 0) { Stop-Greska 'Pravo "Log on as a batch job" nije dodeljeno (secedit). Dodelite ga rucno: secpol.msc > Local Policies > User Rights Assignment.' }
        Ok 'dodeljeno pravo "Log on as a batch job"'
    }
} finally { Remove-Item $tmpDir -Recurse -Force -ErrorAction SilentlyContinue }

# ------------------------------------------------------------------ 3. tajne (DPAPI pod servisnim nalogom)
$xml = New-Object Xml.XmlDocument
$xml.PreserveWhitespace = $true
$xml.Load($Cfg)
$app = $xml.SelectSingleNode('/configuration/appSettings')
$cs = $app.SelectSingleNode("add[@key='CitajSanduce']")   # v2.7.1: CitajSanduce (stari kljuc: Nalog)
if ($cs -ne $null -and $cs.GetAttribute('value').Trim().Length -gt 0) { $mail = $cs.GetAttribute('value').Trim() } else { $mail = $app.SelectSingleNode("add[@key='Nalog']").GetAttribute('value') }

Korak "3. Lozinka sanduceta $mail"
$loz = Obican (Read-Host "    Lozinka za $mail" -AsSecureString)
if ([string]::IsNullOrEmpty($loz)) { Stop-Greska 'Lozinka nije uneta.' }
if ($loz.Contains('"')) { Stop-Greska 'Lozinka sadrzi znak navodnika (") koji skripta ne podrzava.' }

$svcCred = New-Object Management.Automation.PSCredential($PunNalog, (ConvertTo-SecureString $SvcLozinka -AsPlainText -Force))
$sl = Get-WmiObject Win32_Service -Filter "Name='seclogon'"
if ($sl.StartMode -eq 'Disabled') { Set-Service seclogon -StartupType Manual }
if ($sl.State -ne 'Running') { Start-Service seclogon }

$GreskaFajl = Join-Path $Folder 'greska.tmp'
function KaoServis($argumenti, $izlaz) {
    $p = Start-Process -FilePath $Exe -ArgumentList $argumenti -Credential $svcCred -LoadUserProfile -WorkingDirectory $Folder `
        -RedirectStandardOutput $izlaz -RedirectStandardError $GreskaFajl -NoNewWindow -Wait -PassThru
    if (Test-Path $GreskaFajl) {
        $g = Get-Content $GreskaFajl -Encoding UTF8
        Remove-Item $GreskaFajl -Force -ErrorAction SilentlyContinue
        if ($g) { Write-Host '    --- poruke agenta ---' -ForegroundColor DarkGray; $g | Select-Object -Last 25 | ForEach-Object { Write-Host "    $_" } }
    }
    return $p.ExitCode
}
function Zastiti($vrednost) {
    $tmp = Join-Path $Folder 'zastiti.tmp'
    try {
        $kod = KaoServis @('--zastiti', ('"' + $vrednost + '"')) $tmp
        $d = ((Get-Content $tmp) -join '').Trim()
    } finally { Remove-Item $tmp -Force -ErrorAction SilentlyContinue }
    if (($kod -ne $null -and $kod -ne 0) -or $d -notmatch '^[A-Za-z0-9+/=]{100,}$') { Stop-Greska "Sifrovanje nije uspelo (exit $kod)." }
    return $d
}
function Postavi($kljucCfg, $vrednost) {
    $red = $app.SelectSingleNode("add[@key='$kljucCfg']")
    if ($red -eq $null) { $red = $xml.CreateElement('add'); $red.SetAttribute('key', $kljucCfg); [void]$app.AppendChild($red) }
    $red.SetAttribute('value', $vrednost)
}
Postavi 'Lozinka.dpapi' (Zastiti $loz); $loz = $null
Postavi 'StanjeFolder' $Stanje
$xml.Save($Cfg)
Ok 'sifrovana lozinka i StanjeFolder upisani u Saobracaj.Agent.CimProvera.exe.config'

# Syncfusion licencni kljuc (Word -> PDF): DPAPI pod servisnim nalogom, poseban fajl Syncfusion.dpapi (ne config, ne log).
# Servisni nalog je gore dobio novu lozinku, pa stari Syncfusion.dpapi vise ne moze da se desifruje: kljuc se unosi svaki put.
Korak '3b. Syncfusion licencni kljuc (Word prilozi -> PDF)'
$SfFajl = Join-Path $Folder 'Syncfusion.dpapi'
# Kljuc je dugacak: moze i iz fajla (samo administratori i servisni nalog ga vide); fajl se brise odmah posle citanja.
$SfUlaz = Join-Path (Split-Path $Stanje) 'syncfusion_kljuc.txt'
if (Test-Path $SfUlaz) {
    $sf = ((Get-Content $SfUlaz -Raw) -replace '\s', '')
    Remove-Item $SfUlaz -Force
    if (Test-Path $SfUlaz) { Stop-Greska "Fajl $SfUlaz ne moze da se obrise - obrisite ga rucno." }
    Ok "kljuc procitan iz $SfUlaz (fajl je obrisan)"
} else {
    Write-Host "    (kljuc moze i iz fajla $SfUlaz - skripta ga cita i brise)"
    $sf = Obican (Read-Host '    Syncfusion kljuc (desni klik = nalepi; Enter = bez kljuca, Word prilozi tada idu na rucnu proveru)' -AsSecureString)
}
if ([string]::IsNullOrWhiteSpace($sf)) {
    Remove-Item $SfFajl -Force -ErrorAction SilentlyContinue
    Write-Host '    PAZNJA  kljuc nije unet: Word prilozi se ne pretvaraju u PDF (rucna provera).' -ForegroundColor Yellow
} else {
    if ($sf.Contains('"')) { Stop-Greska 'Kljuc sadrzi znak navodnika (") koji skripta ne podrzava.' }
    $sifra = Zastiti $sf.Trim(); $sf = $null
    [IO.File]::WriteAllText($SfFajl, $sifra, (New-Object Text.UTF8Encoding($false)))
    $sifra = $null
    if ((Alat 'icacls' @($SfFajl, '/inheritance:r', '/grant:r', '*S-1-5-32-544:F', "${Nalog}:R", '/Q')) -ne 0) { Write-Host '    PAZNJA  prava na Syncfusion.dpapi nisu suzena (icacls).' -ForegroundColor Yellow }
    Ok 'sifrovan kljuc upisan u Syncfusion.dpapi (citaju ga samo administratori i servisni nalog)'
}

# ------------------------------------------------------------------ 4. test veza i prvo pokretanje
Korak '4. Test (pravila, sablon, OCR, Syncfusion kljuc, IMAP, SMTP) pod servisnim nalogom'
$out = Join-Path $Folder 'test_veze.txt'
$kod = KaoServis @('--test-veze') $out
Get-Content $out -Encoding UTF8 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "    $_" }
Remove-Item $out -Force -ErrorAction SilentlyContinue
if ($kod -ne $null -and $kod -ne 0) { Stop-Greska 'Test veza nije prosao (vidi gore).' }
Ok 'veze rade'

Korak '5. Prvo pokretanje agenta (rezim iz config-a; u rezimu Test izvestaji idu samo na IzvestajAdresa)'
$out = Join-Path $Folder 'prvo_pokretanje.txt'
$kod = KaoServis @('--once') $out
Remove-Item $out -Force -ErrorAction SilentlyContinue
if ($kod -ne $null -and $kod -ne 0) { Stop-Greska "Agent je zavrsio sa greskom (exit $kod). Log: $Folder\logs" }
Ok 'agent radi'

# ------------------------------------------------------------------ 6. Task Scheduler
Korak "6. Zadatak '$Zadatak' na 2 minuta"
$tr = '"' + $Exe + '" --once'
if ((Alat 'schtasks' @('/Create', '/TN', $Zadatak, '/RU', $PunNalog, '/RP', $SvcLozinka, '/SC', 'MINUTE', '/MO', '2', '/TR', $tr, '/F')) -ne 0) { Stop-Greska 'schtasks /Create nije uspeo' }
$SvcLozinka = $null
Ok 'zadatak napravljen (radi i kada niko nije prijavljen; nova instanca se ne pokrece dok prethodna radi)'

# Provera da Task Scheduler stvarno moze da pokrene zadatak pod servisnim nalogom
[void](Alat 'schtasks' @('/Run', '/TN', $Zadatak))
Start-Sleep -Seconds 20
$info = & schtasks /Query /TN $Zadatak /V /FO CSV | ConvertFrom-Csv | Select-Object -First 1
$rez = $info.'Last Result'
if ($rez -eq '0' -or $rez -eq '267009' -or $rez -eq '267011') { Ok "zadatak se pokrece (Last Result: $rez)" }
else { Write-Host "    PAZNJA  zadatak je vratio Last Result = $rez. Proverite Task Scheduler > Saobracaj > Agent CimProvera > History." -ForegroundColor Yellow }

Write-Host ''
Write-Host 'GOTOVO. Agent proverava sanduce na svaka 2 minuta.' -ForegroundColor Green
Write-Host "Log: $Folder\logs"
Write-Host ''
Read-Host 'Enter za izlaz'
