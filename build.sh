#!/bin/bash

PAKETBOOTSTRAPPER=./.paket/paket.bootstrapper.exe
PAKET=./.paket/paket.exe
FAKE=./packages/FAKE/tools/FAKE.exe

mono $PAKETBOOTSTRAPPER
mono $PAKET install
mono $FAKE ./build.fsx "$@"