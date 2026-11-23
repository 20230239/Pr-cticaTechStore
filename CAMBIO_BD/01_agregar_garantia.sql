USE TechStoreDB;
GO
ALTER TABLE dbo.Productos
  ADD GarantiaMeses INT NOT NULL CONSTRAINT DF_Productos_Garantia DEFAULT 12;
ALTER TABLE dbo.Productos
  ADD CONSTRAINT CK_Productos_Garantia CHECK (GarantiaMeses >= 0);
GO
