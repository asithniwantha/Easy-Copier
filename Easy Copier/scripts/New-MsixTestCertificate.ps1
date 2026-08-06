param(
	[string]$Subject = 'CN=asith',
	[string]$OutputDirectory = (Join-Path $PSScriptRoot '..\Certificates'),
	[string]$PublishProfilePath = (Join-Path $PSScriptRoot '..\Properties\PublishProfiles\msix-x64.pubxml'),
	[string]$Password = 'EasyCopierLocalTest123!',
	[int]$ValidYears = 3
)

$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$resolvedPublishProfilePath = [System.IO.Path]::GetFullPath($PublishProfilePath)
New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

$pfxPath = Join-Path $resolvedOutputDirectory 'EasyCopier_TestSigning.pfx'
$cerPath = Join-Path $resolvedOutputDirectory 'EasyCopier_TestSigning.cer'
$securePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force

$certificate = New-SelfSignedCertificate -Type CodeSigningCert `
	-Subject $Subject `
	-FriendlyName 'Easy Copier MSIX Test Signing' `
	-KeyExportPolicy Exportable `
	-CertStoreLocation 'Cert:\CurrentUser\My' `
	-NotAfter (Get-Date).AddYears($ValidYears)

Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $securePassword | Out-Null
Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null

[xml]$publishProfile = Get-Content -Path $resolvedPublishProfilePath
$propertyGroup = $publishProfile.Project.PropertyGroup
$thumbprintNode = $propertyGroup.PackageCertificateThumbprint
if (-not $thumbprintNode) {
	throw "PackageCertificateThumbprint element was not found in $resolvedPublishProfilePath"
}

$thumbprintNode.'#text' = $certificate.Thumbprint
$publishProfile.Save($resolvedPublishProfilePath)

Write-Host "Created test signing certificate: $($certificate.Thumbprint)"
Write-Host "PFX: $pfxPath"
Write-Host "CER: $cerPath"
Write-Host "Updated publish profile thumbprint: $resolvedPublishProfilePath"
Write-Host 'Install the CER into Trusted People on each test PC before installing the MSIX.'
Write-Host 'Publish with the msix-x64 profile after generating the certificate.'
