IF DB_ID('TechStoreDB') IS NULL CREATE DATABASE TechStoreDB;
GO
USE TechStoreDB;
GO

IF OBJECT_ID('dbo.DetalleVentas') IS NOT NULL DROP TABLE dbo.DetalleVentas;
IF OBJECT_ID('dbo.Ventas')        IS NOT NULL DROP TABLE dbo.Ventas;
IF OBJECT_ID('dbo.Productos')     IS NOT NULL DROP TABLE dbo.Productos;
IF OBJECT_ID('dbo.Clientes')      IS NOT NULL DROP TABLE dbo.Clientes;
IF OBJECT_ID('dbo.Categorias')    IS NOT NULL DROP TABLE dbo.Categorias;
GO

CREATE TABLE dbo.Categorias (
    Id     INT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(100)     NOT NULL,
    CONSTRAINT PK_Categorias PRIMARY KEY (Id),
    CONSTRAINT UQ_Categorias_Nombre UNIQUE (Nombre)
);

CREATE TABLE dbo.Productos (
    Id          INT IDENTITY(1,1) NOT NULL,
    Nombre      NVARCHAR(150)     NOT NULL,
    SKU         VARCHAR(20)       NOT NULL,
    Precio      DECIMAL(10,2)     NOT NULL,
    Stock       INT               NOT NULL CONSTRAINT DF_Productos_Stock DEFAULT 0,
    CategoriaId INT               NOT NULL,
    CONSTRAINT PK_Productos PRIMARY KEY (Id),
    CONSTRAINT UQ_Productos_SKU UNIQUE (SKU),
    CONSTRAINT CK_Productos_Precio CHECK (Precio > 0),
    CONSTRAINT CK_Productos_Stock  CHECK (Stock >= 0),
    CONSTRAINT FK_Productos_Categorias FOREIGN KEY (CategoriaId) REFERENCES dbo.Categorias(Id)
);

CREATE TABLE dbo.Clientes (
    Id     INT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(150)     NOT NULL,
    Email  NVARCHAR(150)     NOT NULL,
    CONSTRAINT PK_Clientes PRIMARY KEY (Id),
    CONSTRAINT UQ_Clientes_Email UNIQUE (Email)
);

CREATE TABLE dbo.Ventas (
    Id        INT IDENTITY(1,1) NOT NULL,
    ClienteId INT               NOT NULL,
    Fecha     DATETIME2         NOT NULL CONSTRAINT DF_Ventas_Fecha DEFAULT SYSUTCDATETIME(),
    Total     DECIMAL(12,2)     NOT NULL,
    CONSTRAINT PK_Ventas PRIMARY KEY (Id),
    CONSTRAINT CK_Ventas_Total CHECK (Total >= 0),
    CONSTRAINT FK_Ventas_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes(Id)
);

CREATE TABLE dbo.DetalleVentas (
    Id             INT IDENTITY(1,1) NOT NULL,
    VentaId        INT               NOT NULL,
    ProductoId     INT               NOT NULL,
    Cantidad       INT               NOT NULL,
    PrecioUnitario DECIMAL(10,2)     NOT NULL,
    CONSTRAINT PK_DetalleVentas PRIMARY KEY (Id),
    CONSTRAINT CK_Detalle_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_Detalle_Precio   CHECK (PrecioUnitario > 0),
    CONSTRAINT FK_Detalle_Ventas    FOREIGN KEY (VentaId)    REFERENCES dbo.Ventas(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Detalle_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos(Id)
);

CREATE INDEX IX_Productos_CategoriaId ON dbo.Productos(CategoriaId);
CREATE INDEX IX_Productos_Nombre      ON dbo.Productos(Nombre);
CREATE INDEX IX_Ventas_ClienteId      ON dbo.Ventas(ClienteId);
CREATE INDEX IX_Detalle_VentaId       ON dbo.DetalleVentas(VentaId);
CREATE INDEX IX_Detalle_ProductoId    ON dbo.DetalleVentas(ProductoId);
GO

INSERT INTO dbo.Categorias (Nombre) VALUES
 (N'Laptops'),(N'Smartphones'),(N'Tablets'),(N'Monitores'),(N'Teclados'),
 (N'Mouse'),(N'Audio'),(N'Almacenamiento'),(N'Redes'),(N'Accesorios');

;WITH N AS (
    SELECT TOP (10000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_columns a CROSS JOIN sys.all_columns b
)
INSERT INTO dbo.Clientes (Nombre, Email)
SELECT CONCAT(N'Cliente ', n), CONCAT('cliente', n, '@techstore.test') FROM N;

;WITH N AS (
    SELECT TOP (100000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_columns a CROSS JOIN sys.all_columns b
)
INSERT INTO dbo.Productos (Nombre, SKU, Precio, Stock, CategoriaId)
SELECT CONCAT(c.Nombre, N' Modelo ', N.n),
       CONCAT('SKU-', RIGHT('000000' + CAST(N.n AS VARCHAR(10)), 6)),
       CAST(5 + (ABS(CHECKSUM(NEWID())) % 150000) / 100.0 AS DECIMAL(10,2)),
       ABS(CHECKSUM(NEWID())) % 500 + 10,
       c.Id
FROM N
JOIN dbo.Categorias c ON c.Id = 1 + (N.n % 10);
GO

SELECT 'Categorias' AS Tabla, COUNT(*) AS Filas FROM dbo.Categorias UNION ALL
SELECT 'Clientes',  COUNT(*) FROM dbo.Clientes   UNION ALL
SELECT 'Productos', COUNT(*) FROM dbo.Productos;
GO
