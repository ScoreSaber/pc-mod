param(
    [string]$Input = "https://scoresaber.com/api/openapi.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "../..")).Path
$specFile = (New-TemporaryFile).FullName

try {
    $generatedDir = Join-Path $repoRoot "src/Core/Api/Generated"
    $generatedFile = Join-Path $generatedDir "ScoreSaberApiGeneratedClient.cs"

    New-Item -ItemType Directory -Path $generatedDir -Force | Out-Null

    dotnet run --project (Join-Path $repoRoot "tools/openapi/ScoreSaber.OpenApiTools/ScoreSaber.OpenApiTools.csproj") -- `
        --input $Input `
        --output $specFile

    dotnet nswag openapi2csclient `
        "/input:$specFile" `
        "/output:$generatedFile" `
        "/namespace:ScoreSaber.Core.Api.Generated" `
        "/classname:ScoreSaberApiGeneratedClient" `
        "/GenerateClientClasses:true" `
        "/GenerateDtoTypes:true" `
        "/GenerateJsonMethods:false" `
        "/GenerateDataAnnotations:false" `
        "/GenerateNullableReferenceTypes:false" `
        "/RequiredPropertiesMustBeDefined:false" `
        "/JsonLibrary:NewtonsoftJson" `
        "/DateTimeType:System.DateTime" `
        "/ArrayType:System.Collections.Generic.List" `
        "/ArrayInstanceType:System.Collections.Generic.List" `
        "/ResponseArrayType:System.Collections.Generic.List" `
        "/NewLineBehavior:LF" `
        "/OperationGenerationMode:SingleClientFromOperationId" `
        "/GenerateOptionalParameters:true" `
        "/InjectHttpClient:true" `
        "/DisposeHttpClient:false"

    $content = Get-Content -LiteralPath $generatedFile -Raw
    $content = $content -replace "(?m)[ `t]+$", ""
    $content = $content -replace "\bLegacyHm[d]Id\b", "LegacyHMDId"
    $content = $content -replace "\bHm[d]\b", "HMD"
    $content = $content -replace "\bP[p]\b", "PP"
    Set-Content -LiteralPath $generatedFile -Value $content -NoNewline
} finally {
    Remove-Item -LiteralPath $specFile -Force -ErrorAction SilentlyContinue
}
