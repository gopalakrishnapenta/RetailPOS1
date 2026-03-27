param()

Write-Host "Starting Identity Service on Port 5001..."
Start-Process "dotnet" -ArgumentList "run --project IdentityService --urls http://127.0.0.1:5001" -WindowStyle Normal

Write-Host "Starting Catalog Service on Port 5002..."
Start-Process "dotnet" -ArgumentList "run --project CatalogService --urls http://127.0.0.1:5002" -WindowStyle Normal

Write-Host "Starting Orders Service on Port 5003..."
Start-Process "dotnet" -ArgumentList "run --project OrdersService --urls http://127.0.0.1:5003" -WindowStyle Normal

Write-Host "Starting Admin Service on Port 5004..."
Start-Process "dotnet" -ArgumentList "run --project AdminService --urls http://127.0.0.1:5004" -WindowStyle Normal

Write-Host "Starting Ocelot API Gateway on Port 5000..."
Start-Process "dotnet" -ArgumentList "run --project ApiGateway --urls http://127.0.0.1:5000" -WindowStyle Normal

Write-Host "Starting Angular POS UI (dev server)..."
Start-Process "cmd.exe" -ArgumentList "/k `"cd pos-ui && npx ng serve --port 4200 --open`"" -WindowStyle Normal

Write-Host ""
Write-Host "All services launched! Swagger UIs available at:"
Write-Host "  Identity Service  -> http://localhost:5001/swagger"
Write-Host "  Catalog Service   -> http://localhost:5002/swagger"
Write-Host "  Orders Service    -> http://localhost:5003/swagger"
Write-Host "  Admin Service     -> http://localhost:5004/swagger"
Write-Host "  Angular POS UI    -> http://localhost:4200"
