$PAKET='.paket\paket.exe'
$PAKET_BOOT='.paket\paket.bootstrapper.exe'
$FAKE='packages\FAKE\tools\FAKE.exe'

if (-not (test-path $PAKET)) {
  & $PAKET_BOOT
}

& $PAKET restore

& $FAKE 'build.fsx' @args
exit $LastExitCode
