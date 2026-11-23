# TechStore: ADO.NET vs Entity Framework Core

Mismo módulo de inventario y ventas implementado dos veces (ADO.NET y EF Core) sobre la misma base de datos
`TechStoreDB`, con pruebas de rendimiento, métricas de desarrollo e informe técnico.

## Estructura

| Carpeta | Contenido |
|---|---|
| `TechStore.Core` | Entidades, DTOs, excepciones e **interfaces compartidas** (`IProductoRepository`, `IClienteRepository`, `IVentaRepository`) |
| `TechStore.AdoNet` | Repositorios con `Microsoft.Data.SqlClient`: consultas parametrizadas, `SqlBulkCopy`, venta con `SqlTransaction` |
| `TechStore.EFCore` | `DbContext` (Fluent API), migraciones (Code First) y repositorios EF Core |
| `TechStore.EFCore.DbFirst` | Proyecto donde se genera el modelo Database First con `scaffold` |
| `TechStore.Benchmarks` | Pruebas de rendimiento (6 escenarios, hasta 100,000 registros) que exportan `resultados.csv` |
| `TechStore.Demo` | Consola para la demostración en clase |
| `Database/TechStoreDB.sql` | Script de la BD (copia también en la raíz como `TechStoreDB.sql`) |
| `CAMBIO_BD/` | Prueba de cambio de esquema (columna `GarantiaMeses`) |
| `scripts/` | `contar_lineas.sh`, `bitacora.csv`, `errores.csv` para las métricas |
| `Informe/` | `generar_informe.py` y el PDF generado |

## 1. Requisitos
- [.NET SDK 8.0](https://dotnet.microsoft.com/download)
- SQL Server 2019+ (local, Express o Docker) y SSMS / Azure Data Studio / `sqlcmd`
- Herramienta de EF: `dotnet tool install --global dotnet-ef`
- Para el informe: Python 3 con `pip install reportlab matplotlib pandas`

SQL Server en Docker (alternativa):
```bash
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='TuClave_Segura123' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

## 2. Crear la base de datos
Ejecuta `Database/TechStoreDB.sql` (crea `TechStoreDB`, tablas, restricciones, índices y **100,000 productos**, 10,000 clientes, 10 categorías):
```bash
sqlcmd -S localhost -E -C -i Database/TechStoreDB.sql          # autenticación de Windows
sqlcmd -S localhost -U sa -P 'TuClave_Segura123' -C -i Database/TechStoreDB.sql   # usuario sa
```
El script se puede volver a ejecutar: recrea las tablas.

## 3. Cadena de conexión
Se lee de la variable de entorno `TECHSTORE_CONN` (no se guardan contraseñas en el repositorio). Si no existe, usa `Server=localhost;Database=TechStoreDB;Trusted_Connection=True;TrustServerCertificate=True;`.
```bash
# Linux / macOS
export TECHSTORE_CONN="Server=localhost,1433;Database=TechStoreDB;User Id=sa;Password=TuClave_Segura123;TrustServerCertificate=True;"
# PowerShell
$env:TECHSTORE_CONN = "Server=localhost,1433;Database=TechStoreDB;User Id=sa;Password=TuClave_Segura123;TrustServerCertificate=True;"
```

## 4. Compilar y ejecutar la demo
```bash
dotnet build
dotnet run --project TechStore.Demo
```
Menú: (1) venta exitosa con ADO.NET y EF Core, (2) **fallo forzado con rollback**, (3) búsqueda paginada.

## 5. Benchmarks
```bash
dotnet run -c Release --project TechStore.Benchmarks -- --quick          # prueba rápida (2 volúmenes menores)
dotnet run -c Release --project TechStore.Benchmarks -- --reps 5 --out resultados.csv   # completo
```
Escenarios: 1a insertar individual · 1b insertar en lote · 2 consulta por ID · 3a búsqueda filtrada · 3b paginación · 4 JOIN · 5a actualización masiva set-based (`UPDATE` vs `ExecuteUpdate`) · 5b actualización por entidades (solo EF) · 6 venta transaccional.
EF Core se mide con tracking y con `AsNoTracking`. Los datos insertados por el benchmark (`SKU` que empieza con `BENCH-`) y las ventas de prueba se eliminan al terminar cada repetición. El benchmark modifica precios/stock de los primeros productos: **vuelve a ejecutar el script SQL** si quieres restaurar los datos originales.

> Cierra SSMS/aplicaciones pesadas, usa Release y ejecuta siempre en la misma máquina para que los resultados sean comparables.

## 6. EF Core Code First (migraciones)
`TechStoreDbContext` reproduce el esquema del script. Las migraciones se ejecutan usando `TechStore.Demo` como proyecto de inicio:
```bash
dotnet ef migrations add Inicial -p TechStore.EFCore -s TechStore.Demo
```
- **Demostrar que Code First genera el mismo esquema:** crea una BD vacía `TechStoreDB_CF` y aplica la migración
  ```bash
  export TECHSTORE_CONN="Server=localhost;Database=TechStoreDB_CF;Trusted_Connection=True;TrustServerCertificate=True;"
  dotnet ef database update -p TechStore.EFCore -s TechStore.Demo
  ```
- **Usar la migración con `TechStoreDB` (ya creada por el script):** no ejecutes `database update`; márcala como aplicada:
  ```sql
  USE TechStoreDB;
  IF OBJECT_ID('__EFMigrationsHistory') IS NULL
    CREATE TABLE __EFMigrationsHistory (MigrationId nvarchar(150) NOT NULL PRIMARY KEY, ProductVersion nvarchar(32) NOT NULL);
  INSERT INTO __EFMigrationsHistory VALUES ('<MigrationId de dotnet ef migrations list>', '8.0.11');
  ```
- Comparar con `dotnet ef migrations script` para documentar diferencias entre el esquema del script y el de la migración.

## 7. EF Core Database First
```bash
./TechStore.EFCore.DbFirst/scaffold.sh          # Linux/macOS
./TechStore.EFCore.DbFirst/scaffold.ps1         # Windows
dotnet run --project TechStore.EFCore.DbFirst -p:DefineConstants=DBFIRST_GENERADO
```
Genera `Models/` y `Data/TechStoreDbFirstContext.cs`. Haz commit de lo generado para poder comparar con el modelo escrito a mano.

## 8. Métricas de desarrollo
```bash
./scripts/contar_lineas.sh                       # líneas de código por tecnología
```
Registra tiempo y errores en `scripts/bitacora.csv` y `scripts/errores.csv`. 
