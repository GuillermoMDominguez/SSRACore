# SSRACore
Emulator for the Simple Software RISC Architecture core, a 64-bit simulated computer architecture and assembly language
meant to introduce low level programming concepts in an easy way and without the need of additional toolchains.  
This repository includes the main emulator core which implements the architecture specifications and a test CLI application
for running programs written in the SSRA assembly language.
Note that this is a work in progress and the necesary documentation will be updated to this site shortly.
# Usage
This project is built with Visual Studio 2019 targeting .Net Core 3.1, after compiling run the CLI application with:  
**_SSRACLI [path to code file]_**  
If a path is not provided the application will prompt you for the desired file.  
Code examples can be found in the "examples" folder.
