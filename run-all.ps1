param()

Write-Host "Cleaning up existing dotnet processes..."
Stop-Process -Name dotnet -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host "Starting Identity Service on Port 5001..."
Start-Process "dotnet" -ArgumentList "run --project `"IdentityService/IdentityService.csproj`" --urls http://127.0.0.1:5001" -WindowStyle Normal
Start-Sleep -Seconds 5

Write-Host "Starting Admin Service on Port 5004..."
Start-Process "dotnet" -ArgumentList "run --project `"AdminService/AdminService.csproj`" --urls http://127.0.0.1:5004" -WindowStyle Normal
Start-Sleep -Seconds 5

Write-Host "Starting Catalog Service on Port 5002..."
Start-Process "dotnet" -ArgumentList "run --project `"CatalogService/CatalogService.csproj`" --urls http://127.0.0.1:5002" -WindowStyle Normal
Start-Sleep -Seconds 5

Write-Host "Starting Orders Service on Port 5003..."
Start-Process "dotnet" -ArgumentList "run --project `"OrdersService/OrdersService.csproj`" --urls http://127.0.0.1:5003" -WindowStyle Normal
Start-Sleep -Seconds 5

Write-Host "Starting Payment Service on Port 5005..."
Start-Process "dotnet" -ArgumentList "run --project `"PaymentService/PaymentService.csproj`" --urls http://127.0.0.1:5005" -WindowStyle Normal
Start-Sleep -Seconds 5

Write-Host "Starting Returns Service on Port 5006..."
Start-Process "dotnet" -ArgumentList "run --project `"ReturnsService/ReturnsService.csproj`" --urls http://127.0.0.1:5006" -WindowStyle Normal
Start-Sleep -Seconds 5

Write-Host "Starting Notification Service on Port 5176..."
Start-Process "dotnet" -ArgumentList "run --project `"NotificationService/NotificationService.csproj`" --urls http://127.0.0.1:5176" -WindowStyle Normal
Start-Sleep -Seconds 5

Write-Host "Starting Ocelot API Gateway on Port 5000..."
Start-Process "dotnet" -ArgumentList "run --project `"ApiGateway/ApiGateway.csproj`" --urls http://127.0.0.1:5000" -WindowStyle Normal
Start-Sleep -Seconds 5

Write-Host "Starting Angular UI on Port 4200..."
Start-Process "powershell" -ArgumentList "-NoExit", "-Command", "cd pos-ui; npm start" -WindowStyle Normal

Write-Host ""
Write-Host "All system components launched sequentially!"
Write-Host "Swagger UIs & Frontend available at:"
Write-Host "  Front-End UI      -> http://localhost:4200"
Write-Host "  API Gateway       -> http://localhost:5000"
Write-Host "  Identity Service  -> http://localhost:5001/swagger"
Write-Host "  Catalog Service   -> http://localhost:5002/swagger"
Write-Host "  Orders Service    -> http://localhost:5003/swagger"
Write-Host "  Admin Service     -> http://localhost:5004/swagger"
Write-Host "  Payment Service   -> http://localhost:5005/swagger"
Write-Host "  Returns Service   -> http://localhost:5006/swagger"
Write-Host "  Notification Service -> http://localhost:5176/swagger"

Write-Host "Please wait a few moments for the UI to compile..."
