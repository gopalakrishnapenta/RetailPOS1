$databases = @("RetailPOS_Identity", "RetailPOS_Catalog", "RetailPOS_Orders", "RetailPOS_Admin", "RetailPOS_Returns", "RetailPOS_Payments", "RetailPOS_Notifications")
$backupDir = "C:\Users\Gopala Krishna\Desktop\Project\backups"

foreach ($db in $databases) {
    $backupFile = Join-Path $backupDir "$db.bak"
    Write-Host "Backing up $db to $backupFile..."
    sqlcmd -S ".\SQLEXPRESS" -E -C -Q "BACKUP DATABASE [$db] TO DISK = '$backupFile' WITH FORMAT, INIT, NAME = '$db-Full Database Backup'"
}
