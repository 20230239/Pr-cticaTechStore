USE TechStoreDB;
GO
ALTER TABLE dbo.Productos DROP CONSTRAINT CK_Productos_Garantia;
ALTER TABLE dbo.Productos DROP CONSTRAINT DF_Productos_Garantia;
ALTER TABLE dbo.Productos DROP COLUMN GarantiaMeses;
GO
