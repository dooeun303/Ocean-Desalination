Add-Type -Path "d:\UnityProject\Unity_new_version\Assets\Plugins\Npgsql.dll" -ErrorAction Ignore
if (-not ("Npgsql.NpgsqlConnection" -as [type])) {
    $dll = Get-ChildItem -Path "d:\UnityProject\Unity_new_version" -Filter "Npgsql.dll" -Recurse | Select-Object -First 1
    if ($dll) { Add-Type -Path $dll.FullName }
}

try {
    $connStr = "Host=127.0.0.1;Port=5433;Database=test;Username=postgres;Password=0000;SslMode=Disable;"
    $conn = New-Object Npgsql.NpgsqlConnection($connStr)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT table_name, column_name, data_type FROM information_schema.columns WHERE table_schema = 'aaa' AND table_name IN ('equipment', 'technician', 'manual') ORDER BY table_name, ordinal_position;"
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Output ($reader.GetString(0) + " -> " + $reader.GetString(1) + " (" + $reader.GetString(2) + ")")
    }
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
