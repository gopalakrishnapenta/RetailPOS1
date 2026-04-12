$json = Get-Content "ocelot.json" | ConvertFrom-Json
$publicPaths = @(
    "/gateway/auth/login",
    "/gateway/auth/verify-login-otp",
    "/gateway/auth/resend-login-otp",
    "/gateway/auth/register",
    "/gateway/auth/verify-email",
    "/gateway/auth/resend-verification",
    "/gateway/auth/send-otp",
    "/gateway/auth/reset-password",
    "/gateway/auth/refresh",
    "/gateway/auth/google-login",
    "/gateway/auth/stores/active",
    "/gateway/auth/stores/{id}"
)

foreach ($route in $json.Routes) {
    $isPublic = $false
    foreach ($public in $publicPaths) {
        if ($route.UpstreamPathTemplate -eq $public) {
            $isPublic = $true
            break
        }
    }

    if (-not $isPublic -and -not $route.AuthenticationOptions) {
        $route | Add-Member -MemberType NoteProperty -Name "AuthenticationOptions" -Value @{
            "AuthenticationProviderKey" = "Bearer"
            "AllowedScopes" = @()
        }
    }
}

$json | ConvertTo-Json -Depth 100 | Set-Content "ocelot.json"
