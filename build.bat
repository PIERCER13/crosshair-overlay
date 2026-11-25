@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /optimize+ /out:piercer_crosshair_overlay.exe Program.cs
if %ERRORLEVEL% NEQ 0 (
  echo [ERRO] Falha ao compilar.
  pause
  exit /b 1
)
echo [OK] Compilado: piercer_crosshair_overlay.exe
