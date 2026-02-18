-- =============================================
-- Script: triggersDb.sql
-- Description: Creación de triggers de auditoría (INSERT, UPDATE, DELETE) para todas las tablas.
--              Registra cambios en la tabla BitacoraAccion.
-- =============================================

-- =============================================
-- 1. TABLA: Usuario
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Usuario_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Usuario_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Usuario_Insert] ON [dbo].[Usuario] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Usuario. ID: ' + CAST(i.Id_Usr AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Usuario_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Usuario_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Usuario_Update] ON [dbo].[Usuario] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    DECLARE @NombreRol VARCHAR(50);
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    -- Obtener rol del usuario logueado (simulado como en el ejemplo original)
    SELECT @NombreRol = CAST(Rol AS VARCHAR(50)) FROM [dbo].[Usuario] WHERE Id_Usr = @Id_Usuario_Log;

    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT 
        @Id_Usuario_Log, 'Edición', GETDATE(), 
        LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + 
             ' con permisos de ' + ISNULL(@NombreRol, 'Desconocido') + 
             ' editó la tabla Usuario, el dato ' + C.ColumnName + 
             '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + 
             ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Usr, 'Nombre' AS ColumnName, d.Nombre AS OldValue, i.Nombre AS NewValue FROM inserted i JOIN deleted d ON i.Id_Usr = d.Id_Usr WHERE d.Nombre <> i.Nombre
        UNION ALL
        SELECT i.Id_Usr, 'Correo', d.Correo, i.Correo FROM inserted i JOIN deleted d ON i.Id_Usr = d.Id_Usr WHERE d.Correo <> i.Correo
        UNION ALL
        SELECT i.Id_Usr, 'Contrasena', '******', '******' FROM inserted i JOIN deleted d ON i.Id_Usr = d.Id_Usr WHERE d.Contrasena <> i.Contrasena
        UNION ALL
        SELECT i.Id_Usr, 'Rol', CAST(d.Rol AS VARCHAR(50)), CAST(i.Rol AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Usr = d.Id_Usr WHERE d.Rol <> i.Rol
        UNION ALL
        SELECT i.Id_Usr, 'Estado', CAST(d.Estado AS VARCHAR(50)), CAST(i.Estado AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Usr = d.Id_Usr WHERE d.Estado <> i.Estado
        UNION ALL
        SELECT i.Id_Usr, 'HoraEntrada', CAST(d.HoraEntrada AS VARCHAR(50)), CAST(i.HoraEntrada AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Usr = d.Id_Usr WHERE (d.HoraEntrada <> i.HoraEntrada OR (d.HoraEntrada IS NULL AND i.HoraEntrada IS NOT NULL) OR (d.HoraEntrada IS NOT NULL AND i.HoraEntrada IS NULL))
        UNION ALL
        SELECT i.Id_Usr, 'HoraSalida', CAST(d.HoraSalida AS VARCHAR(50)), CAST(i.HoraSalida AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Usr = d.Id_Usr WHERE (d.HoraSalida <> i.HoraSalida OR (d.HoraSalida IS NULL AND i.HoraSalida IS NOT NULL) OR (d.HoraSalida IS NOT NULL AND i.HoraSalida IS NULL))
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_Usuario_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Usuario_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_Usuario_Delete] ON [dbo].[Usuario] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en Usuario. ID: ' + CAST(d.Id_Usr AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO


-- =============================================
-- 2. TABLA: Rol
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Rol_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Rol_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Rol_Insert] ON [dbo].[Rol] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Rol. ID: ' + CAST(i.Id_Rol AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Rol_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Rol_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Rol_Update] ON [dbo].[Rol] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla Rol, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Rol, 'Nombre_Rol' AS ColumnName, d.Nombre_Rol AS OldValue, i.Nombre_Rol AS NewValue FROM inserted i JOIN deleted d ON i.Id_Rol = d.Id_Rol WHERE d.Nombre_Rol <> i.Nombre_Rol
        UNION ALL
        SELECT i.Id_Rol, 'Descripcion', d.Descripcion, i.Descripcion FROM inserted i JOIN deleted d ON i.Id_Rol = d.Id_Rol WHERE d.Descripcion <> i.Descripcion
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_Rol_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Rol_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_Rol_Delete] ON [dbo].[Rol] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en Rol. ID: ' + CAST(d.Id_Rol AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 3. TABLA: Categoria
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Categoria_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Categoria_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Categoria_Insert] ON [dbo].[Categoria] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Categoria. ID: ' + CAST(i.Id_Categoria AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Categoria_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Categoria_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Categoria_Update] ON [dbo].[Categoria] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla Categoria, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Categoria, 'Nombre' AS ColumnName, d.Nombre AS OldValue, i.Nombre AS NewValue FROM inserted i JOIN deleted d ON i.Id_Categoria = d.Id_Categoria WHERE d.Nombre <> i.Nombre
        UNION ALL
        SELECT i.Id_Categoria, 'Descripcion', d.Descripcion, i.Descripcion FROM inserted i JOIN deleted d ON i.Id_Categoria = d.Id_Categoria WHERE d.Descripcion <> i.Descripcion
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_Categoria_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Categoria_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_Categoria_Delete] ON [dbo].[Categoria] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en Categoria. ID: ' + CAST(d.Id_Categoria AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 4. TABLA: Producto
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Producto_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Producto_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Producto_Insert] ON [dbo].[Producto] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Producto. ID: ' + CAST(i.Id_Producto AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Producto_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Producto_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Producto_Update] ON [dbo].[Producto] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla Producto, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Producto, 'Nombre', d.Nombre, i.Nombre FROM inserted i JOIN deleted d ON i.Id_Producto = d.Id_Producto WHERE d.Nombre <> i.Nombre
        UNION ALL
        SELECT i.Id_Producto, 'Descripcion', d.Descripcion, i.Descripcion FROM inserted i JOIN deleted d ON i.Id_Producto = d.Id_Producto WHERE d.Descripcion <> i.Descripcion
        UNION ALL
        SELECT i.Id_Producto, 'Precio', CAST(d.Precio AS VARCHAR(50)), CAST(i.Precio AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Producto = d.Id_Producto WHERE d.Precio <> i.Precio
        UNION ALL
        SELECT i.Id_Producto, 'StockMinimo', CAST(d.StockMinimo AS VARCHAR(50)), CAST(i.StockMinimo AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Producto = d.Id_Producto WHERE d.StockMinimo <> i.StockMinimo
        UNION ALL
        SELECT i.Id_Producto, 'Estado', CAST(d.Estado AS VARCHAR(50)), CAST(i.Estado AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Producto = d.Id_Producto WHERE d.Estado <> i.Estado
        UNION ALL
        SELECT i.Id_Producto, 'Id_Categoria', CAST(d.Id_Categoria AS VARCHAR(50)), CAST(i.Id_Categoria AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Producto = d.Id_Producto WHERE d.Id_Categoria <> i.Id_Categoria
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_Producto_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Producto_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_Producto_Delete] ON [dbo].[Producto] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en Producto. ID: ' + CAST(d.Id_Producto AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 5. TABLA: Inventario
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Inventario_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Inventario_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Inventario_Insert] ON [dbo].[Inventario] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Inventario. ID: ' + CAST(i.Id_Inventario AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Inventario_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Inventario_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Inventario_Update] ON [dbo].[Inventario] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla Inventario, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Inventario, 'Nombre', d.Nombre, i.Nombre FROM inserted i JOIN deleted d ON i.Id_Inventario = d.Id_Inventario WHERE d.Nombre <> i.Nombre
        UNION ALL
        SELECT i.Id_Inventario, 'StockActual', CAST(d.StockActual AS VARCHAR(50)), CAST(i.StockActual AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Inventario = d.Id_Inventario WHERE d.StockActual <> i.StockActual
        UNION ALL
        SELECT i.Id_Inventario, 'StockMinimo', CAST(d.StockMinimo AS VARCHAR(50)), CAST(i.StockMinimo AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Inventario = d.Id_Inventario WHERE d.StockMinimo <> i.StockMinimo
        UNION ALL
        SELECT i.Id_Inventario, 'UnidadMedida', CAST(d.UnidadMedida AS VARCHAR(50)), CAST(i.UnidadMedida AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Inventario = d.Id_Inventario WHERE d.UnidadMedida <> i.UnidadMedida
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_Inventario_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Inventario_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_Inventario_Delete] ON [dbo].[Inventario] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en Inventario. ID: ' + CAST(d.Id_Inventario AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 6. TABLA: Nomina
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Nomina_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Nomina_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Nomina_Insert] ON [dbo].[Nomina] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Nomina. ID: ' + CAST(i.Id_Nomina AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Nomina_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Nomina_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Nomina_Update] ON [dbo].[Nomina] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla Nomina, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Nomina, 'FechaInicio', CAST(d.FechaInicio AS VARCHAR(50)), CAST(i.FechaInicio AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Nomina = d.Id_Nomina WHERE d.FechaInicio <> i.FechaInicio
        UNION ALL
        SELECT i.Id_Nomina, 'FechaFinal', CAST(d.FechaFinal AS VARCHAR(50)), CAST(i.FechaFinal AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Nomina = d.Id_Nomina WHERE d.FechaFinal <> i.FechaFinal
        UNION ALL
        SELECT i.Id_Nomina, 'FechaGeneracion', CAST(d.FechaGeneracion AS VARCHAR(50)), CAST(i.FechaGeneracion AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Nomina = d.Id_Nomina WHERE d.FechaGeneracion <> i.FechaGeneracion
        UNION ALL
        SELECT i.Id_Nomina, 'Estado', d.Estado, i.Estado FROM inserted i JOIN deleted d ON i.Id_Nomina = d.Id_Nomina WHERE d.Estado <> i.Estado
        UNION ALL
        SELECT i.Id_Nomina, 'TotalBruto', CAST(d.TotalBruto AS VARCHAR(50)), CAST(i.TotalBruto AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Nomina = d.Id_Nomina WHERE d.TotalBruto <> i.TotalBruto
        UNION ALL
        SELECT i.Id_Nomina, 'TotalDeducciones', CAST(d.TotalDeducciones AS VARCHAR(50)), CAST(i.TotalDeducciones AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Nomina = d.Id_Nomina WHERE d.TotalDeducciones <> i.TotalDeducciones
        UNION ALL
        SELECT i.Id_Nomina, 'TotalNeto', CAST(d.TotalNeto AS VARCHAR(50)), CAST(i.TotalNeto AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Nomina = d.Id_Nomina WHERE d.TotalNeto <> i.TotalNeto
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_Nomina_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Nomina_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_Nomina_Delete] ON [dbo].[Nomina] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en Nomina. ID: ' + CAST(d.Id_Nomina AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 7. TABLA: Planilla
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Planilla_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Planilla_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Planilla_Insert] ON [dbo].[Planilla] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Planilla. ID: ' + CAST(i.Id_Planilla AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Planilla_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Planilla_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Planilla_Update] ON [dbo].[Planilla] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla Planilla, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Planilla, 'Id_Usr', CAST(d.Id_Usr AS VARCHAR(50)), CAST(i.Id_Usr AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.Id_Usr <> i.Id_Usr
        UNION ALL
        SELECT i.Id_Planilla, 'FechaInicio', CAST(d.FechaInicio AS VARCHAR(50)), CAST(i.FechaInicio AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.FechaInicio <> i.FechaInicio
        UNION ALL
        SELECT i.Id_Planilla, 'FechaFinal', CAST(d.FechaFinal AS VARCHAR(50)), CAST(i.FechaFinal AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.FechaFinal <> i.FechaFinal
        UNION ALL
        SELECT i.Id_Planilla, 'HorasTrabajadas', CAST(d.HorasTrabajadas AS VARCHAR(50)), CAST(i.HorasTrabajadas AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.HorasTrabajadas <> i.HorasTrabajadas
        UNION ALL
        SELECT i.Id_Planilla, 'Salario', CAST(d.Salario AS VARCHAR(50)), CAST(i.Salario AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.Salario <> i.Salario
        UNION ALL
        SELECT i.Id_Planilla, 'Bonificaciones', CAST(d.Bonificaciones AS VARCHAR(50)), CAST(i.Bonificaciones AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.Bonificaciones <> i.Bonificaciones
        UNION ALL
        SELECT i.Id_Planilla, 'Deducciones', CAST(d.Deducciones AS VARCHAR(50)), CAST(i.Deducciones AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.Deducciones <> i.Deducciones
        UNION ALL
        SELECT i.Id_Planilla, 'EstadoRegistro', d.EstadoRegistro, i.EstadoRegistro FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.EstadoRegistro <> i.EstadoRegistro
        UNION ALL
        SELECT i.Id_Planilla, 'HorasExtras', CAST(d.HorasExtras AS VARCHAR(50)), CAST(i.HorasExtras AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.HorasExtras <> i.HorasExtras
        UNION ALL
        SELECT i.Id_Planilla, 'Id_Nomina', CAST(d.Id_Nomina AS VARCHAR(50)), CAST(i.Id_Nomina AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.Id_Nomina <> i.Id_Nomina
        UNION ALL
        SELECT i.Id_Planilla, 'SalarioBruto', CAST(d.SalarioBruto AS VARCHAR(50)), CAST(i.SalarioBruto AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.SalarioBruto <> i.SalarioBruto
        UNION ALL
        SELECT i.Id_Planilla, 'SalarioNeto', CAST(d.SalarioNeto AS VARCHAR(50)), CAST(i.SalarioNeto AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.SalarioNeto <> i.SalarioNeto
        UNION ALL
        SELECT i.Id_Planilla, 'Observaciones', d.Observaciones, i.Observaciones FROM inserted i JOIN deleted d ON i.Id_Planilla = d.Id_Planilla WHERE d.Observaciones <> i.Observaciones
    ) AS C;
END;
GO

-- =============================================
-- 8. TABLA: InventarioMovimiento
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_InventarioMovimiento_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_InventarioMovimiento_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_InventarioMovimiento_Insert] ON [dbo].[InventarioMovimiento] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en InventarioMovimiento. ID: ' + CAST(i.Id_Movimiento AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_InventarioMovimiento_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_InventarioMovimiento_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_InventarioMovimiento_Update] ON [dbo].[InventarioMovimiento] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla InventarioMovimiento, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Movimiento, 'Id_Inventario', CAST(d.Id_Inventario AS VARCHAR(50)), CAST(i.Id_Inventario AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Movimiento = d.Id_Movimiento WHERE d.Id_Inventario <> i.Id_Inventario
        UNION ALL
        SELECT i.Id_Movimiento, 'Tipo_Movimiento', d.Tipo_Movimiento, i.Tipo_Movimiento FROM inserted i JOIN deleted d ON i.Id_Movimiento = d.Id_Movimiento WHERE d.Tipo_Movimiento <> i.Tipo_Movimiento
        UNION ALL
        SELECT i.Id_Movimiento, 'Cantidad', CAST(d.Cantidad AS VARCHAR(50)), CAST(i.Cantidad AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Movimiento = d.Id_Movimiento WHERE d.Cantidad <> i.Cantidad
        UNION ALL
        SELECT i.Id_Movimiento, 'Fecha', CAST(d.Fecha AS VARCHAR(50)), CAST(i.Fecha AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Movimiento = d.Id_Movimiento WHERE d.Fecha <> i.Fecha
        UNION ALL
        SELECT i.Id_Movimiento, 'Id_Usuario', CAST(d.Id_Usuario AS VARCHAR(50)), CAST(i.Id_Usuario AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Movimiento = d.Id_Movimiento WHERE d.Id_Usuario <> i.Id_Usuario
        UNION ALL
        SELECT i.Id_Movimiento, 'Observaciones', d.Observaciones, i.Observaciones FROM inserted i JOIN deleted d ON i.Id_Movimiento = d.Id_Movimiento WHERE d.Observaciones <> i.Observaciones
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_InventarioMovimiento_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_InventarioMovimiento_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_InventarioMovimiento_Delete] ON [dbo].[InventarioMovimiento] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en InventarioMovimiento. ID: ' + CAST(d.Id_Movimiento AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 9. TABLA: Venta
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Venta_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Venta_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Venta_Insert] ON [dbo].[Venta] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Venta. ID: ' + CAST(i.Id_Venta AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Venta_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Venta_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Venta_Update] ON [dbo].[Venta] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla Venta, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Venta, 'Fecha', CAST(d.Fecha AS VARCHAR(50)), CAST(i.Fecha AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Venta = d.Id_Venta WHERE d.Fecha <> i.Fecha
        UNION ALL
        SELECT i.Id_Venta, 'Total', CAST(d.Total AS VARCHAR(50)), CAST(i.Total AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Venta = d.Id_Venta WHERE d.Total <> i.Total
        UNION ALL
        SELECT i.Id_Venta, 'MetodoPago', d.MetodoPago, i.MetodoPago FROM inserted i JOIN deleted d ON i.Id_Venta = d.Id_Venta WHERE d.MetodoPago <> i.MetodoPago
        UNION ALL
        SELECT i.Id_Venta, 'Estado', CAST(d.Estado AS VARCHAR(50)), CAST(i.Estado AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Venta = d.Id_Venta WHERE d.Estado <> i.Estado
        UNION ALL
        SELECT i.Id_Venta, 'Id_Usuario', CAST(d.Id_Usuario AS VARCHAR(50)), CAST(i.Id_Usuario AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Venta = d.Id_Venta WHERE d.Id_Usuario <> i.Id_Usuario
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_Venta_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Venta_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_Venta_Delete] ON [dbo].[Venta] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en Venta. ID: ' + CAST(d.Id_Venta AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 10. TABLA: Factura
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Factura_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Factura_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Factura_Insert] ON [dbo].[Factura] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Factura. ID: ' + CAST(i.Id_Factura AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Factura_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Factura_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Factura_Update] ON [dbo].[Factura] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla Factura, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Factura, 'Id_Venta', CAST(d.Id_Venta AS VARCHAR(50)), CAST(i.Id_Venta AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.Id_Venta <> i.Id_Venta
        UNION ALL
        SELECT i.Id_Factura, 'Cliente', d.Cliente, i.Cliente FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.Cliente <> i.Cliente
        UNION ALL
        SELECT i.Id_Factura, 'FechaEmision', CAST(d.FechaEmision AS VARCHAR(50)), CAST(i.FechaEmision AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.FechaEmision <> i.FechaEmision
        UNION ALL
        SELECT i.Id_Factura, 'Total', CAST(d.Total AS VARCHAR(50)), CAST(i.Total AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.Total <> i.Total
        UNION ALL
        SELECT i.Id_Factura, 'MontoPagado', CAST(d.MontoPagado AS VARCHAR(50)), CAST(i.MontoPagado AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.MontoPagado <> i.MontoPagado
        UNION ALL
        SELECT i.Id_Factura, 'Cambio', CAST(d.Cambio AS VARCHAR(50)), CAST(i.Cambio AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.Cambio <> i.Cambio
        UNION ALL
        SELECT i.Id_Factura, 'Moneda', d.Moneda, i.Moneda FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.Moneda <> i.Moneda
        UNION ALL
        SELECT i.Id_Factura, 'MetodoPago', d.MetodoPago, i.MetodoPago FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.MetodoPago <> i.MetodoPago
        UNION ALL
        SELECT i.Id_Factura, 'TasaCambio', CAST(d.TasaCambio AS VARCHAR(50)), CAST(i.TasaCambio AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.TasaCambio <> i.TasaCambio
        UNION ALL
        SELECT i.Id_Factura, 'Estado', CAST(d.Estado AS VARCHAR(50)), CAST(i.Estado AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.Estado <> i.Estado
        UNION ALL
        SELECT i.Id_Factura, 'Justificacion', d.Justificacion, i.Justificacion FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.Justificacion <> i.Justificacion
        UNION ALL
        SELECT i.Id_Factura, 'FacturaOriginalId', CAST(d.FacturaOriginalId AS VARCHAR(50)), CAST(i.FacturaOriginalId AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.FacturaOriginalId <> i.FacturaOriginalId
        UNION ALL
        SELECT i.Id_Factura, 'PdfFileName', d.PdfFileName, i.PdfFileName FROM inserted i JOIN deleted d ON i.Id_Factura = d.Id_Factura WHERE d.PdfFileName <> i.PdfFileName
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_Factura_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Factura_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_Factura_Delete] ON [dbo].[Factura] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en Factura. ID: ' + CAST(d.Id_Factura AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 11. TABLA: PagoFactura
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_PagoFactura_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_PagoFactura_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_PagoFactura_Insert] ON [dbo].[PagoFactura] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en PagoFactura. ID: ' + CAST(i.Id AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_PagoFactura_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_PagoFactura_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_PagoFactura_Update] ON [dbo].[PagoFactura] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla PagoFactura, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id, 'FacturaId', CAST(d.FacturaId AS VARCHAR(50)), CAST(i.FacturaId AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.FacturaId <> i.FacturaId
        UNION ALL
        SELECT i.Id, 'MetodoPago', d.MetodoPago, i.MetodoPago FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.MetodoPago <> i.MetodoPago
        UNION ALL
        SELECT i.Id, 'Monto', CAST(d.Monto AS VARCHAR(50)), CAST(i.Monto AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Monto <> i.Monto
        UNION ALL
        SELECT i.Id, 'Moneda', d.Moneda, i.Moneda FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Moneda <> i.Moneda
        UNION ALL
        SELECT i.Id, 'TasaCambio', CAST(d.TasaCambio AS VARCHAR(50)), CAST(i.TasaCambio AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.TasaCambio <> i.TasaCambio
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_PagoFactura_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_PagoFactura_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_PagoFactura_Delete] ON [dbo].[PagoFactura] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en PagoFactura. ID: ' + CAST(d.Id AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 12. TABLA: DetalleVenta
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_DetalleVenta_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_DetalleVenta_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_DetalleVenta_Insert] ON [dbo].[DetalleVenta] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en DetalleVenta. ID: ' + CAST(i.Id_Detalle AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_DetalleVenta_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_DetalleVenta_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_DetalleVenta_Update] ON [dbo].[DetalleVenta] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla DetalleVenta, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Detalle, 'Id_Venta', CAST(d.Id_Venta AS VARCHAR(50)), CAST(i.Id_Venta AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Detalle = d.Id_Detalle WHERE d.Id_Venta <> i.Id_Venta
        UNION ALL
        SELECT i.Id_Detalle, 'Id_Producto', CAST(d.Id_Producto AS VARCHAR(50)), CAST(i.Id_Producto AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Detalle = d.Id_Detalle WHERE d.Id_Producto <> i.Id_Producto
        UNION ALL
        SELECT i.Id_Detalle, 'Cantidad', CAST(d.Cantidad AS VARCHAR(50)), CAST(i.Cantidad AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Detalle = d.Id_Detalle WHERE d.Cantidad <> i.Cantidad
        UNION ALL
        SELECT i.Id_Detalle, 'PrecioUnitario', CAST(d.PrecioUnitario AS VARCHAR(50)), CAST(i.PrecioUnitario AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Detalle = d.Id_Detalle WHERE d.PrecioUnitario <> i.PrecioUnitario
        UNION ALL
        SELECT i.Id_Detalle, 'Descuento', CAST(d.Descuento AS VARCHAR(50)), CAST(i.Descuento AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Detalle = d.Id_Detalle WHERE d.Descuento <> i.Descuento
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_DetalleVenta_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_DetalleVenta_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_DetalleVenta_Delete] ON [dbo].[DetalleVenta] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en DetalleVenta. ID: ' + CAST(d.Id_Detalle AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 13. TABLA: Promocion
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Promocion_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Promocion_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Promocion_Insert] ON [dbo].[Promocion] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Promocion. ID: ' + CAST(i.Id_Promocion AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Promocion_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Promocion_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Promocion_Update] ON [dbo].[Promocion] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla Promocion, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Promocion, 'Nombre', d.Nombre, i.Nombre FROM inserted i JOIN deleted d ON i.Id_Promocion = d.Id_Promocion WHERE d.Nombre <> i.Nombre
        UNION ALL
        SELECT i.Id_Promocion, 'Descripcion', d.Descripcion, i.Descripcion FROM inserted i JOIN deleted d ON i.Id_Promocion = d.Id_Promocion WHERE d.Descripcion <> i.Descripcion
        UNION ALL
        SELECT i.Id_Promocion, 'FechaInicio', CAST(d.FechaInicio AS VARCHAR(50)), CAST(i.FechaInicio AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Promocion = d.Id_Promocion WHERE d.FechaInicio <> i.FechaInicio
        UNION ALL
        SELECT i.Id_Promocion, 'FechaFin', CAST(d.FechaFin AS VARCHAR(50)), CAST(i.FechaFin AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Promocion = d.Id_Promocion WHERE d.FechaFin <> i.FechaFin
        UNION ALL
        SELECT i.Id_Promocion, 'Condiciones', d.Condiciones, i.Condiciones FROM inserted i JOIN deleted d ON i.Id_Promocion = d.Id_Promocion WHERE d.Condiciones <> i.Condiciones
        UNION ALL
        SELECT i.Id_Promocion, 'Estado', CAST(d.Estado AS VARCHAR(50)), CAST(i.Estado AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Promocion = d.Id_Promocion WHERE d.Estado <> i.Estado
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_Promocion_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Promocion_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_Promocion_Delete] ON [dbo].[Promocion] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en Promocion. ID: ' + CAST(d.Id_Promocion AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 14. TABLA: AplicacionPromocion
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_AplicacionPromocion_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_AplicacionPromocion_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_AplicacionPromocion_Insert] ON [dbo].[AplicacionPromocion] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en AplicacionPromocion. ID: ' + CAST(i.Id_Aplicacion AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_AplicacionPromocion_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_AplicacionPromocion_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_AplicacionPromocion_Update] ON [dbo].[AplicacionPromocion] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla AplicacionPromocion, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Aplicacion, 'Id_Promocion', CAST(d.Id_Promocion AS VARCHAR(50)), CAST(i.Id_Promocion AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Aplicacion = d.Id_Aplicacion WHERE d.Id_Promocion <> i.Id_Promocion
        UNION ALL
        SELECT i.Id_Aplicacion, 'Id_Venta', CAST(d.Id_Venta AS VARCHAR(50)), CAST(i.Id_Venta AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Aplicacion = d.Id_Aplicacion WHERE d.Id_Venta <> i.Id_Venta
        UNION ALL
        SELECT i.Id_Aplicacion, 'MontoDescuento', CAST(d.MontoDescuento AS VARCHAR(50)), CAST(i.MontoDescuento AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Aplicacion = d.Id_Aplicacion WHERE d.MontoDescuento <> i.MontoDescuento
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_AplicacionPromocion_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_AplicacionPromocion_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_AplicacionPromocion_Delete] ON [dbo].[AplicacionPromocion] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en AplicacionPromocion. ID: ' + CAST(d.Id_Aplicacion AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 15. TABLA: Caja
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Caja_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Caja_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Caja_Insert] ON [dbo].[Caja] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Caja. ID: ' + CAST(i.Id_Caja AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Caja_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Caja_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Caja_Update] ON [dbo].[Caja] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla Caja, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Caja, 'Id_Usuario', CAST(d.Id_Usuario AS VARCHAR(50)), CAST(i.Id_Usuario AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Caja = d.Id_Caja WHERE d.Id_Usuario <> i.Id_Usuario
        UNION ALL
        SELECT i.Id_Caja, 'Fecha_Apertura', CAST(d.Fecha_Apertura AS VARCHAR(50)), CAST(i.Fecha_Apertura AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Caja = d.Id_Caja WHERE d.Fecha_Apertura <> i.Fecha_Apertura
        UNION ALL
        SELECT i.Id_Caja, 'Fecha_Cierre', CAST(d.Fecha_Cierre AS VARCHAR(50)), CAST(i.Fecha_Cierre AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Caja = d.Id_Caja WHERE (d.Fecha_Cierre <> i.Fecha_Cierre OR (d.Fecha_Cierre IS NULL AND i.Fecha_Cierre IS NOT NULL) OR (d.Fecha_Cierre IS NOT NULL AND i.Fecha_Cierre IS NULL))
        UNION ALL
        SELECT i.Id_Caja, 'Monto_Inicial', CAST(d.Monto_Inicial AS VARCHAR(50)), CAST(i.Monto_Inicial AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Caja = d.Id_Caja WHERE d.Monto_Inicial <> i.Monto_Inicial
        UNION ALL
        SELECT i.Id_Caja, 'Monto', CAST(d.Monto AS VARCHAR(50)), CAST(i.Monto AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Caja = d.Id_Caja WHERE d.Monto <> i.Monto
        UNION ALL
        SELECT i.Id_Caja, 'Estado', d.Estado, i.Estado FROM inserted i JOIN deleted d ON i.Id_Caja = d.Id_Caja WHERE d.Estado <> i.Estado
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_Caja_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Caja_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_Caja_Delete] ON [dbo].[Caja] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en Caja. ID: ' + CAST(d.Id_Caja AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 16. TABLA: CajaRetiro
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_CajaRetiro_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_CajaRetiro_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_CajaRetiro_Insert] ON [dbo].[CajaRetiro] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en CajaRetiro. ID: ' + CAST(i.Id_Retiro AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_CajaRetiro_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_CajaRetiro_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_CajaRetiro_Update] ON [dbo].[CajaRetiro] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla CajaRetiro, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Retiro, 'Id_Caja', CAST(d.Id_Caja AS VARCHAR(50)), CAST(i.Id_Caja AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Retiro = d.Id_Retiro WHERE d.Id_Caja <> i.Id_Caja
        UNION ALL
        SELECT i.Id_Retiro, 'Id_Usuario', CAST(d.Id_Usuario AS VARCHAR(50)), CAST(i.Id_Usuario AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Retiro = d.Id_Retiro WHERE d.Id_Usuario <> i.Id_Usuario
        UNION ALL
        SELECT i.Id_Retiro, 'Monto', CAST(d.Monto AS VARCHAR(50)), CAST(i.Monto AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Retiro = d.Id_Retiro WHERE d.Monto <> i.Monto
        UNION ALL
        SELECT i.Id_Retiro, 'Motivo', d.Motivo, i.Motivo FROM inserted i JOIN deleted d ON i.Id_Retiro = d.Id_Retiro WHERE d.Motivo <> i.Motivo
        UNION ALL
        SELECT i.Id_Retiro, 'FechaHora', CAST(d.FechaHora AS VARCHAR(50)), CAST(i.FechaHora AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Retiro = d.Id_Retiro WHERE d.FechaHora <> i.FechaHora
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_CajaRetiro_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_CajaRetiro_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_CajaRetiro_Delete] ON [dbo].[CajaRetiro] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en CajaRetiro. ID: ' + CAST(d.Id_Retiro AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 17. TABLA: DeduccionLey
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_DeduccionLey_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_DeduccionLey_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_DeduccionLey_Insert] ON [dbo].[DeduccionLey] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en DeduccionLey. ID: ' + CAST(i.Id AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_DeduccionLey_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_DeduccionLey_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_DeduccionLey_Update] ON [dbo].[DeduccionLey] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla DeduccionLey, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id, 'Nombre', d.Nombre, i.Nombre FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Nombre <> i.Nombre
        UNION ALL
        SELECT i.Id, 'Porcentaje', CAST(d.Porcentaje AS VARCHAR(50)), CAST(i.Porcentaje AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Porcentaje <> i.Porcentaje
        UNION ALL
        SELECT i.Id, 'Activo', CAST(d.Activo AS VARCHAR(50)), CAST(i.Activo AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Activo <> i.Activo
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_DeduccionLey_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_DeduccionLey_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_DeduccionLey_Delete] ON [dbo].[DeduccionLey] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en DeduccionLey. ID: ' + CAST(d.Id AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 18. TABLA: ImpuestoRenta
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_ImpuestoRenta_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_ImpuestoRenta_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_ImpuestoRenta_Insert] ON [dbo].[ImpuestoRenta] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en ImpuestoRenta. ID: ' + CAST(i.Id AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_ImpuestoRenta_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_ImpuestoRenta_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_ImpuestoRenta_Update] ON [dbo].[ImpuestoRenta] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla ImpuestoRenta, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id, 'Anio', CAST(d.Anio AS VARCHAR(50)), CAST(i.Anio AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Anio <> i.Anio
        UNION ALL
        SELECT i.Id, 'LimiteInferior', CAST(d.LimiteInferior AS VARCHAR(50)), CAST(i.LimiteInferior AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.LimiteInferior <> i.LimiteInferior
        UNION ALL
        SELECT i.Id, 'LimiteSuperior', CAST(d.LimiteSuperior AS VARCHAR(50)), CAST(i.LimiteSuperior AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.LimiteSuperior <> i.LimiteSuperior
        UNION ALL
        SELECT i.Id, 'Porcentaje', CAST(d.Porcentaje AS VARCHAR(50)), CAST(i.Porcentaje AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Porcentaje <> i.Porcentaje
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_ImpuestoRenta_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_ImpuestoRenta_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_ImpuestoRenta_Delete] ON [dbo].[ImpuestoRenta] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en ImpuestoRenta. ID: ' + CAST(d.Id AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 19. TABLA: Bonificacion
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_Bonificacion_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Bonificacion_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_Bonificacion_Insert] ON [dbo].[Bonificacion] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en Bonificacion. ID: ' + CAST(i.Id AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_Bonificacion_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Bonificacion_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_Bonificacion_Update] ON [dbo].[Bonificacion] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla Bonificacion, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id, 'Id_Planilla', CAST(d.Id_Planilla AS VARCHAR(50)), CAST(i.Id_Planilla AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Id_Planilla <> i.Id_Planilla
        UNION ALL
        SELECT i.Id, 'Monto', CAST(d.Monto AS VARCHAR(50)), CAST(i.Monto AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Monto <> i.Monto
        UNION ALL
        SELECT i.Id, 'Motivo', d.Motivo, i.Motivo FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Motivo <> i.Motivo
        UNION ALL
        SELECT i.Id, 'Fecha', CAST(d.Fecha AS VARCHAR(50)), CAST(i.Fecha AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Fecha <> i.Fecha
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_Bonificacion_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_Bonificacion_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_Bonificacion_Delete] ON [dbo].[Bonificacion] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en Bonificacion. ID: ' + CAST(d.Id AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 20. TABLA: PasswordResetToken
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_PasswordResetToken_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_PasswordResetToken_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_PasswordResetToken_Insert] ON [dbo].[PasswordResetToken] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en PasswordResetToken. ID: ' + CAST(i.Id AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_PasswordResetToken_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_PasswordResetToken_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_PasswordResetToken_Update] ON [dbo].[PasswordResetToken] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla PasswordResetToken, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id, 'UsuarioId', CAST(d.UsuarioId AS VARCHAR(50)), CAST(i.UsuarioId AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.UsuarioId <> i.UsuarioId
        UNION ALL
        SELECT i.Id, 'Token', d.Token, i.Token FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Token <> i.Token
        UNION ALL
        SELECT i.Id, 'ExpirationDate', CAST(d.ExpirationDate AS VARCHAR(50)), CAST(i.ExpirationDate AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.ExpirationDate <> i.ExpirationDate
        UNION ALL
        SELECT i.Id, 'IsUsed', CAST(d.IsUsed AS VARCHAR(50)), CAST(i.IsUsed AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.IsUsed <> i.IsUsed
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_PasswordResetToken_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_PasswordResetToken_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_PasswordResetToken_Delete] ON [dbo].[PasswordResetToken] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en PasswordResetToken. ID: ' + CAST(d.Id AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 21. TABLA: InventarioMovimientoAuditoria
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_InventarioMovimientoAuditoria_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_InventarioMovimientoAuditoria_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_InventarioMovimientoAuditoria_Insert] ON [dbo].[InventarioMovimientoAuditoria] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en InventarioMovimientoAuditoria. ID: ' + CAST(i.Id_Auditoria AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_InventarioMovimientoAuditoria_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_InventarioMovimientoAuditoria_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_InventarioMovimientoAuditoria_Update] ON [dbo].[InventarioMovimientoAuditoria] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla InventarioMovimientoAuditoria, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id_Auditoria, 'CantidadAnterior', CAST(d.CantidadAnterior AS VARCHAR(50)), CAST(i.CantidadAnterior AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Auditoria = d.Id_Auditoria WHERE d.CantidadAnterior <> i.CantidadAnterior
        UNION ALL
        SELECT i.Id_Auditoria, 'CantidadNueva', CAST(d.CantidadNueva AS VARCHAR(50)), CAST(i.CantidadNueva AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id_Auditoria = d.Id_Auditoria WHERE d.CantidadNueva <> i.CantidadNueva
        UNION ALL
        SELECT i.Id_Auditoria, 'TipoMovimientoAnterior', d.TipoMovimientoAnterior, i.TipoMovimientoAnterior FROM inserted i JOIN deleted d ON i.Id_Auditoria = d.Id_Auditoria WHERE d.TipoMovimientoAnterior <> i.TipoMovimientoAnterior
        UNION ALL
        SELECT i.Id_Auditoria, 'TipoMovimientoNuevo', d.TipoMovimientoNuevo, i.TipoMovimientoNuevo FROM inserted i JOIN deleted d ON i.Id_Auditoria = d.Id_Auditoria WHERE d.TipoMovimientoNuevo <> i.TipoMovimientoNuevo
        UNION ALL
        SELECT i.Id_Auditoria, 'MotivoCambio', d.MotivoCambio, i.MotivoCambio FROM inserted i JOIN deleted d ON i.Id_Auditoria = d.Id_Auditoria WHERE d.MotivoCambio <> i.MotivoCambio
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_InventarioMovimientoAuditoria_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_InventarioMovimientoAuditoria_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_InventarioMovimientoAuditoria_Delete] ON [dbo].[InventarioMovimientoAuditoria] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en InventarioMovimientoAuditoria. ID: ' + CAST(d.Id_Auditoria AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO

-- =============================================
-- 22. TABLA: BonificacionAuditoria
-- =============================================

-- INSERT
IF OBJECT_ID('[dbo].[trg_Audit_BonificacionAuditoria_Insert]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_BonificacionAuditoria_Insert];
GO
CREATE TRIGGER [dbo].[trg_Audit_BonificacionAuditoria_Insert] ON [dbo].[BonificacionAuditoria] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Creación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' creó un registro en BonificacionAuditoria. ID: ' + CAST(i.Id AS VARCHAR(MAX)), 1024) FROM inserted i;
END;
GO

-- UPDATE
IF OBJECT_ID('[dbo].[trg_Audit_BonificacionAuditoria_Update]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_BonificacionAuditoria_Update];
GO
CREATE TRIGGER [dbo].[trg_Audit_BonificacionAuditoria_Update] ON [dbo].[BonificacionAuditoria] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Edición', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' editó la tabla BonificacionAuditoria, el dato ' + C.ColumnName + '. Anterior ' + LEFT(ISNULL(CAST(C.OldValue AS VARCHAR(MAX)), 'NULL'), 100) + ', valor actual: ' + LEFT(ISNULL(CAST(C.NewValue AS VARCHAR(MAX)), 'NULL'), 100), 1024)
    FROM (
        SELECT i.Id, 'Id_Bonificacion', CAST(d.Id_Bonificacion AS VARCHAR(50)), CAST(i.Id_Bonificacion AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.Id_Bonificacion <> i.Id_Bonificacion
        UNION ALL
        SELECT i.Id, 'MontoAnterior', CAST(d.MontoAnterior AS VARCHAR(50)), CAST(i.MontoAnterior AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.MontoAnterior <> i.MontoAnterior
        UNION ALL
        SELECT i.Id, 'MontoNuevo', CAST(d.MontoNuevo AS VARCHAR(50)), CAST(i.MontoNuevo AS VARCHAR(50)) FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.MontoNuevo <> i.MontoNuevo
        UNION ALL
        SELECT i.Id, 'MotivoCambio', d.MotivoCambio, i.MotivoCambio FROM inserted i JOIN deleted d ON i.Id = d.Id WHERE d.MotivoCambio <> i.MotivoCambio
    ) AS C;
END;
GO

-- DELETE
IF OBJECT_ID('[dbo].[trg_Audit_BonificacionAuditoria_Delete]', 'TR') IS NOT NULL DROP TRIGGER [dbo].[trg_Audit_BonificacionAuditoria_Delete];
GO
CREATE TRIGGER [dbo].[trg_Audit_BonificacionAuditoria_Delete] ON [dbo].[BonificacionAuditoria] AFTER DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id_Usuario_Log INT;
    SET @Id_Usuario_Log = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    INSERT INTO [dbo].[BitacoraAccion] ([Id_Usuario], [Accion], [FechaHora], [Descripcion])
    SELECT @Id_Usuario_Log, 'Eliminación', GETDATE(), LEFT('El usuario ' + ISNULL(CAST(@Id_Usuario_Log AS VARCHAR), 'Sistema') + ' eliminó un registro en BonificacionAuditoria. ID: ' + CAST(d.Id AS VARCHAR(MAX)), 1024) FROM deleted d;
END;
GO
