# Preuzima Tesseract jezike (tessdata_fast, Apache 2.0) u ovaj folder. Pokrenuti jednom posle kloniranja repoa:
#   powershell -ExecutionPolicy Bypass -File tessdata\Preuzmi.ps1
# Jezici: engleski, nemacki, francuski, srpski (cirilica i latinica), ceski, turski, madjarski, italijanski, rumunski,
# bugarski, slovenacki, hrvatski + osd (orijentacija strane).
$ErrorActionPreference = 'Stop'
$folder = Split-Path -Parent $MyInvocation.MyCommand.Path
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
foreach ($l in 'eng','deu','fra','srp','srp_latn','ces','tur','hun','ita','ron','bul','slv','hrv','osd') {
    $f = Join-Path $folder "$l.traineddata"
    if (Test-Path $f) { Write-Host "postoji $l"; continue }
    Invoke-WebRequest -UseBasicParsing "https://github.com/tesseract-ocr/tessdata_fast/raw/main/$l.traineddata" -OutFile $f
    Write-Host "preuzeto $l"
}
