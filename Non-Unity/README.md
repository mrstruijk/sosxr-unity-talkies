# README


## Compilation of Serial Plugin for MacOS and Windows
Rename file `SerialPlugin` to `SerialPlugin.cpp` prior to compiling.

### Compilation Windows:
Windows (Visual Studio CPP Build Tools), from https://visualstudio.microsoft.com/visual-cpp-build-tools/:
`cl /EHsc /MD /LD SerialPlugin.cpp /Fe:SerialPlugin.dll`


### Compilation MacOS:
`clang++ -dynamiclib -o SerialPlugin.bundle SerialPlugin.cpp -framework IOKit -framework CoreFoundation`