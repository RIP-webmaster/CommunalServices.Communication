@echo off
cls
 
set input1=.\hcs_wsdl_xsd_v.13.2.3.3\all\*.wsdl
set input2=.\hcs_wsdl_xsd_v.13.2.3.3\all\*.xsd
set output=/out:.\GKHService 
set output_config=/config:GKHService
set output_namespace=/namespace:*,GKH
rem set params=/messageContract  /targetClientVersion:Version35 /async /language:C#  /enableDataBinding
set params= /targetClientVersion:Version35 /async /language:C#  /enableDataBinding
 
"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\Bin\NETFX 4.0 Tools\svcutil.exe" %input1% %input2% %output% %output_namespace% %output_config% %params%

PAUSE
