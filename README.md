# ESDMonitoringPOC
This is a repository to store all the code for a minimalist monitoring system for Energy Storing Devices, including a mockup database design, python code for a micro-service for querying hardware information, design documents and configuration files for the monitoring, C# code for data collection on the devices and various scripts for building the solution.

In principle, people should be able to setup a working monitoring system out of the code and documentations from this repository.

## Handling of Modbus Protocol
There are quite a few dotnet projects implemented modbus protocol. However, most of them are already several years old. Due to the dizzling change of dotnet in the past years, finding one that's works with modern dotnet on Linux is not easy.

In the end, I decided to go with NModbus4 as it appears to be the most used one.

### Incorperate NModbus4

Directly installing from Nuget resulted in some issues. However, The process of porting NModbus4 to modern dotnet is rather painless.
The following steps has to be performed on each of the C# projects, first the NModbus4, then NModbus4.Serial, and then the rest projects.
1. Old project file (.xproj file) has to be removed.
2. Create empty project using dotnet CLI. (dotnet new console or dotnet new classlib, depending on the project)
3. Add references to the newly created projects. NModbus4.Serial referenced NModbus4, then the other projects referenced these 2.
4. Add references to external packages.
    For SerialPort, you need "dotnet add package System.IO.Ports --version 8.0.0".
    For Moq, you need "dotnet add package Moq --version 4.20.70".
5. Then you can change the .sln file to refer to the new .csproj files.

That's basically it. ~~No code change is needed.~~ Now the project can be built under dotnet 8.

In order for it to work with the current project, I've made the following changes to the code:

