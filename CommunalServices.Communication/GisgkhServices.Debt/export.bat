@echo off
cls
 
set input1=.\hcs_wsdl_xsd_v.13.1.2.6\debt-requests2\*.wsdl
set input2=.\hcs_wsdl_xsd_v.13.1.2.6\debt-requests2\*.xsd
set output=/out:.\GKHDebtService 
set output_config=/config:GKHDebtService
set output_namespace=/namespace:*,GisgkhServices.Debt
set params= /targetClientVersion:Version35 /async /language:C#  /enableDataBinding
 
"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\Bin\NETFX 4.0 Tools\svcutil.exe" %input1% %input2% %output% %output_namespace% %output_config% %params%

PAUSE
