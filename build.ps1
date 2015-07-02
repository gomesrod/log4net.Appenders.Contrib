param(
    [Parameter(Position=0,Mandatory=0)]
    [string[]]$tasklist = @('Default'),
    [Parameter(Position=1,Mandatory=0)]
    [string]$configuration = 'Debug',
    [int]$build_number = 0,
	[switch]$teamcity_build,
    [string]$chocolatey_api_key = "",
    [string]$chocolatey_api_url = "",
    [string]$nuget_api_key = "",
    [string]$nuget_api_url = "https://www.nuget.org/api/v2/"
)

Write-Host $teamcityBuild
$scriptPath = Split-Path $MyInvocation.InvocationName
Import-Module (join-path $scriptPath 'build\psake.psm1')
Invoke-Psake -framework '4.0' -buildFile (join-path $scriptPath 'build\default.ps1') -taskList $tasklist -properties @{
	"configuration"="$configuration";
	"build_number"="$build_number";
	"teamcityBuild"="$($teamcity_build.ToBool())";
    "chocolateyApiKey" = "$chocolatey_api_key";
    "chocolateyApiUrl" = "$chocolatey_api_url";
    "nugetApiKey" = "$nuget_api_key";
    "nugetApiUrl" = "$nuget_api_url";
    "verbose" = "$($PSBoundParameters['Verbose'])"}