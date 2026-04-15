-- =========================================================================
-- SCRIPT DE AUDITORÍA UNIFICADA DE OPERACIONES: INVENTARIO
-- (Se integra a la tabla genérica evitando exposición de IDs o contraseñas)
-- =========================================================================

-- Asegurarse de que la tabla compartida AuditoriaOperacion existe 
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditoriaOperacion]') AND type in (N'U'))
BEGIN
    CREATE TABLE AuditoriaOperacion (
        Id_Auditoria INT IDENTITY(1,1) PRIMARY KEY,
        Modulo VARCHAR(50) NOT NULL,             
        AccionAuditoria VARCHAR(50) NOT NULL,    
        DetalleRegistro VARCHAR(255) NULL,       
        FechaAuditoria DATETIME DEFAULT GETDATE(),
        UsuarioResponsable VARCHAR(255) NULL
    );
END
GO

-- ==========================================
-- 1. Triggers para Tabla Principal INVENTARIO
-- ==========================================
CREATE OR ALTER TRIGGER [dbo].[trg_Audit_Inventario_Update]
ON [dbo].[Inventario]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdResponsable INT;
    DECLARE @NombreResponsable VARCHAR(255);
    
    -- Extracción segura del contexto
    SELECT @IdResponsable = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    SELECT @NombreResponsable = Nombre FROM Usuario WHERE Id_Usr = @IdResponsable;

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        -- UPDATE
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'INVENTARIO',
            'MODIFICACION',
            'Producto de inventario actualizado: ' + i.Nombre + ' (Stock: ' + CAST(i.StockActual AS VARCHAR(20)) + ')',
            GETDATE(),
            @NombreResponsable
        FROM inserted i;
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        -- INSERT 
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'INVENTARIO',
            'CREACION',
            'Nuevo producto registrado en inventario: ' + i.Nombre + ' (Stock: ' + CAST(i.StockActual AS VARCHAR(20)) + ')',
            GETDATE(),
            @NombreResponsable
        FROM inserted i;
    END
    ELSE IF EXISTS (SELECT * FROM deleted)
    BEGIN
        -- DELETE
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'INVENTARIO',
            'ELIMINACION',
            'Producto eliminado del inventario: ' + d.Nombre,
            GETDATE(),
            @NombreResponsable
        FROM deleted d;
    END
END
GO


-- ==========================================
-- 2. Triggers para Movimientos de INVENTARIO
-- ==========================================
CREATE OR ALTER TRIGGER [dbo].[trg_Audit_InventarioMov_Update]
ON [dbo].[InventarioMovimiento]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdResponsable INT;
    DECLARE @NombreResponsable VARCHAR(255);
    
    -- Extracción segura del contexto
    SELECT @IdResponsable = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    SELECT @NombreResponsable = Nombre FROM Usuario WHERE Id_Usr = @IdResponsable;

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        -- UPDATE
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'MOVIMIENTO INVENTARIO',
            'MODIFICACION',
            'Movimiento modificado: ' + ISNULL(i.Tipo_Movimiento, 'N/A') + ' (Catidad configurada: ' + CAST(i.Cantidad AS VARCHAR(20)) + ')',
            GETDATE(),
            @NombreResponsable
        FROM inserted i;
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        -- INSERT 
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'MOVIMIENTO INVENTARIO',
            'CREACION',
            'Nuevo movimiento procesado: ' + ISNULL(i.Tipo_Movimiento, 'N/A') + ' de ' + CAST(i.Cantidad AS VARCHAR(20)) + ' unidades',
            GETDATE(),
            @NombreResponsable
        FROM inserted i;
    END
    ELSE IF EXISTS (SELECT * FROM deleted)
    BEGIN
        -- DELETE
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'MOVIMIENTO INVENTARIO',
            'ELIMINACION',
            'Registro de movimiento deshecho. Era un ' + ISNULL(d.Tipo_Movimiento, 'N/A') + ' de ' + CAST(d.Cantidad AS VARCHAR(20)) + ' unidades',
            GETDATE(),
            @NombreResponsable
        FROM deleted d;
    END
END
GO
