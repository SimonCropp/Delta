# PowerShell script to run tests with coverage and generate HTML report

Write-Host "Running tests with code coverage..." -ForegroundColor Cyan
dotnet test --collect:"XPlat Code Coverage" --settings coverage.runsettings

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "`nGenerating HTML coverage report (excluding WebApplication* projects)..." -ForegroundColor Cyan
reportgenerator -reports:"**/TestResults/**/coverage.cobertura.xml" -targetdir:"../coverage-report" -reporttypes:"Html;HtmlSummary" -assemblyfilters:"-WebApplication*"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nCoverage report generated successfully!" -ForegroundColor Green
    Write-Host "Open ..\coverage-report\index.html to view the report" -ForegroundColor Yellow

    # Open the report in default browser
    Start-Process "..\coverage-report\index.html"
}
else {
    Write-Host "Failed to generate coverage report!" -ForegroundColor Red
    exit $LASTEXITCODE
}
