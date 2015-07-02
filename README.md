# log4net.Appenders.Contrib
DRY up your log4net config by using these log4net Appenders with sensible defaults

### Use

0.  Add to your project using from Nuget - `Install-Package log4net.Appenders.Contrib`
0.  In your log4net config, add an additional appender:
```xml
<appender name="RemoteAppender" type="log4net.Appenders.Contrib.RemoteSyslog5424Appender,log4net.Appenders.Contrib">
  <AppName>your app name</AppName>
  <Server>ingestor.cityindex.logsearch.io</Server>
  <Port>443</Port>
  <Certificate>
-----BEGIN CERTIFICATE-----
MIIDBzCCAe+gAwIBAgIJAJ4oZAZ2ngs1MA0GCSqGSIb3DQEBBQUAMBoxGDAWBgNV
... 
any valid self signed SSL certificate 
- see below for how to generate a new one
...
Bmo1t/kphLKZnmo=
-----END CERTIFICATE-----
  </Certificate>
  <layout type="log4net.Layout.PatternLayout">
    <conversionPattern value="%message" />
  </layout>
</appender>
...
<root>
 ...
  <appender-ref ref="RemoteAppender" />
</root>
```

0. Generate your own self signed certificate using http://www.selfsignedcertificate.com/, using any domain name, 
downloading the .cert file and opening with your favourite text editor 

### Development

#### Build

0. Update `VERSION.txt`
0. From a Powershell 4.0 prompt:
```
build.ps1
```

#### Publish to NuGet.org
```
build.ps1 -nuget_api_key XXXXXX -nuget_api_url https://www.nuget.org/api/v2/package NugetPublish
```