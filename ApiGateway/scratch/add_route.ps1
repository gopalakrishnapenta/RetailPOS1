$path = "c:\Users\Gopala Krishna\Desktop\Project\ApiGateway\ocelot.json"
$config = Get-Content $path -Raw | ConvertFrom-Json
$newRoute = [PSCustomObject]@{
    DownstreamPathTemplate = "/api/Inventory/adjust"
    DownstreamScheme = "http"
    DownstreamHostAndPorts = @(
        [PSCustomObject]@{
            Host = "localhost"
            Port = 5004
        }
    )
    UpstreamPathTemplate = "/gateway/admin/inventory/adjust"
    UpstreamHttpMethod = @("POST", "OPTIONS")
    AuthenticationOptions = [PSCustomObject]@{
        AllowedScopes = @()
        AuthenticationProviderKey = "Bearer"
    }
}
$config.Routes += $newRoute
$config | ConvertTo-Json -Depth 20 | Set-Content $path
