
To re-install service:

-> instalutil 
->
-> 

To update path to executable:
sc config KinectWindowsService binPath="C:\Users\scheihal\Documents\Visual Studio 2015\Projects\Kinect.Server\Kinect.Toolbox.Service\bin\Debug\KinectToolboxService.exe"

To remove:
sc delete KinectWindowsService


To start service:
   net start KinectWindowsService

##Use from C++ project
Required a change to Assembly.cs :
    [assembly: ComVisible(true)] 
Then:
    RegAsm.exe KinectToolboxService.dll /tlb:KinectToolboxService.tlb /codebase

Output:
C:\Users\scheihal\Documents\Visual Studio 2015\Projects\Kinect.Server\Kinect.Toolbox.Service\bin\Debug>RegAsm.exe KinectToolboxService.dll /tlb:KinectToolboxService.tlb /codebase
Microsoft .NET Framework Assembly Registration Utility version 4.6.1590.0
for Microsoft .NET Framework version 4.6.1590.0
Copyright (C) Microsoft Corporation.  All rights reserved.

Types registered successfully
Assembly exported to 'C:\Users\scheihal\Documents\Visual Studio 2015\Projects\Kinect.Server\Kinect.Toolbox.Service\bin\Debug\KinectToolboxService.tlb', and the type library was registered successfully


Procedure is described in this article:
How to call a managed DLL from native Visual C++ code in Visual Studio.NET or in Visual Studio 2005
https://support.microsoft.com/en-us/kb/828736