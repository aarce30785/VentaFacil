-- =========================================================================
-- SCRIPT DE AUDITORÍA UNIFICADA DE OPERACIONES: CAJA, VENTA Y FACTURACION
-- (Cumple estrictamente con la política de seguridad: sin IDs expuestos)
-- =========================================================================

-- 1. Crear Tabla Unificada de Auditoría de Operaciones
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditoriaOperacion]') AND type in (N'U'))
BEGIN
    CREATE TABLE AuditoriaOperacion (
        Id_Auditoria INT IDENTITY(1,1) PRIMARY KEY,
        
        -- Datos principales del evento
        Modulo VARCHAR(50) NOT NULL,             -- Módulo afectado: 'CAJA', 'VENTA', o 'FACTURACION'
        AccionAuditoria VARCHAR(50) NOT NULL,    -- Tipo de operación: 'APERTURA', 'MODIFICACION', 'CIERRE', 'CREACION', 'ELIMINACION'
        
        -- Campo genérico para exponer un dato clave pero seguro. 
        DetalleRegistro VARCHAR(255) NULL,       -- Ej: "Cliente: Jose", o "Estado de caja: Abierta". (Ningún dato confidencial)
        
        FechaAuditoria DATETIME DEFAULT GETDATE(),
        
        -- Obtenido del contexto de aplicación, guarda únicamente el TEXTO del nombre
        UsuarioResponsable VARCHAR(255) NULL
    );
END
GO


-- ==========================================
-- 2. Triggers para Módulo CAJA
-- ==========================================
CREATE OR ALTER TRIGGER [dbo].[trg_Audit_Caja_Update]
ON [dbo].[Caja]
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
        -- Es un UPDATE (Puede ser modificación normal o Cierre)
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'CAJA',
            CASE 
                -- Detectar si se está cerrando la caja (de estado distinto a cerrado -> a cerrado)
                WHEN d.Estado != 'Cerrada' AND i.Estado = 'Cerrada' THEN 'CIERRE' 
                ELSE 'MODIFICACION' 
            END,
            'Caja con estado resultante: ' + i.Estado,
            GETDATE(),
            @NombreResponsable
        FROM inserted i
        INNER JOIN deleted d ON i.Id_Caja = d.Id_Caja;
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        -- Es un INSERT (Apertura)
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'CAJA',
            'APERTURA',
            'Nueva Caja registrada. Estado inicial: ' + i.Estado,
            GETDATE(),
            @NombreResponsable
        FROM inserted i;
    END
    ELSE IF EXISTS (SELECT * FROM deleted)
    BEGIN
        -- Es un DELETE
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'CAJA',
            'ELIMINACION',
            'Caja eliminada del sistema directamente.',
            GETDATE(),
            @NombreResponsable
        FROM deleted d;
    END
END
GO


-- ==========================================
-- 3. Triggers para Módulo VENTA
-- ==========================================
CREATE OR ALTER TRIGGER [dbo].[trg_Audit_Venta_Update]
ON [dbo].[Venta]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdResponsable INT;
    DECLARE @NombreResponsable VARCHAR(255);
    
    SELECT @IdResponsable = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    SELECT @NombreResponsable = Nombre FROM Usuario WHERE Id_Usr = @IdResponsable;

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'VENTA',
            'MODIFICACION',
            'Venta modificada. El estado actual es: ' + CAST(i.Estado AS VARCHAR(10)),
            GETDATE(),
            @NombreResponsable
        FROM inserted i;
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'VENTA',
            'CREACION',
            'Nueva venta procesada utilizando: ' + i.MetodoPago,
            GETDATE(),
            @NombreResponsable
        FROM inserted i;
    END
    ELSE IF EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'VENTA',
            'ELIMINACION',
            'Registro de venta anterior eliminado.',
            GETDATE(),
            @NombreResponsable
        FROM deleted d;
    END
END
GO


-- ==========================================
-- 4. Triggers para Módulo FACTURACION
-- ==========================================
CREATE OR ALTER TRIGGER [dbo].[trg_Audit_Factura_Update]
ON [dbo].[Factura]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdResponsable INT;
    DECLARE @NombreResponsable VARCHAR(255);
    
    SELECT @IdResponsable = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    SELECT @NombreResponsable = Nombre FROM Usuario WHERE Id_Usr = @IdResponsable;

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'FACTURACION',
            'MODIFICACION',
            'Factura ha sido modificada. Cliente atado: ' + ISNULL(i.Cliente, 'S/N'),
            GETDATE(),
            @NombreResponsable
        FROM inserted i;
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'FACTURACION',
            'CREACION',
            'Nueva factura emitida a nombre del cliente: ' + ISNULL(i.Cliente, 'S/N'),
            GETDATE(),
            @NombreResponsable
        FROM inserted i;
    END
    ELSE IF EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditoriaOperacion (Modulo, AccionAuditoria, DetalleRegistro, FechaAuditoria, UsuarioResponsable)
        SELECT 
            'FACTURACION',
            'ELIMINACION',
            'Factura removida del sistema. Cliente anterior: ' + ISNULL(d.Cliente, 'S/N'),
            GETDATE(),
            @NombreResponsable
        FROM deleted d;
    END
END
GO
