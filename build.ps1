$PAKET='.paket\paket.exe'
$PAKET_BOOT='.paket\paket.bootstrapper.exe'
$FAKE='packages\FAKE\tools\FAKE.exe'

& $PAKET_BOOT

& $PAKET install

& $FAKE 'build.fsx' @args
exit $LastExitCode
