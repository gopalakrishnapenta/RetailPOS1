$databases = @("RetailPOS_Identity", "RetailPOS_Catalog", "RetailPOS_Orders", "RetailPOS_Admin", "RetailPOS_Returns", "RetailPOS_Payments", "RetailPOS_Notifications")

foreach ($db in $databases) {
    Write-Host "Restoring $db..."
    $sql = @"
ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$db] 
FROM DISK = '/var/opt/mssql/backup/$db.bak' 
WITH REPLACE, 
MOVE '$db' TO '/var/opt/mssql/data/$db.mdf', 
MOVE '$db`_log' TO '/var/opt/mssql/data/$db`_log.ldf';
ALTER DATABASE [$db] SET MULTI_USER;
"@
    # Use double quotes for the docker command to avoid issues with the backtick in powershell
    docker exec retailpos-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourSecurePassword123! -C -Q "$sql"
}
