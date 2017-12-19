:run.bat

cd  ClientGUI/bin/debug

START ClientGUI.exe "2"

cd ../../..

cd Repository/bin/debug
START Repository.exe

cd ../../..

cd MotherBuildServer/bin/debug
START MotherBuildServer.exe

cd ../../..
cd TestHarness/bin/debug
START TestHarness.exe

@echo:

cd ../..