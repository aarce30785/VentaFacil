-- =========================================================================
-- SCRIPT DE AUDITORÍA PARA LA TABLA USUARIO
-- =========================================================================

-- 1. Crear Tabla de Auditoría (Excluyendo ID y contraseñas por seguridad)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UsuarioAuditoria]') AND type in (N'U'))
BEGIN
    CREATE TABLE UsuarioAuditoria (
        Id_Auditoria INT IDENTITY(1,1) PRIMARY KEY,
        
        -- Solo guardamos el nombre del usuario afectado (la acción que se realizó sobre él)
        NombreUsuarioAfectado VARCHAR(255), 
        
        -- Acción y Metadatos
        AccionAuditoria VARCHAR(50) NOT NULL,      -- 'CREACION', 'MODIFICACION', 'ELIMINACION'
        FechaAuditoria DATETIME DEFAULT GETDATE(), -- Cuándo ocurrió
        
        -- Nombre del usuario que ejecutó la acción (obtenida mediante el contexto activo)
        UsuarioResponsable VARCHAR(255) NULL
    );
END
GO

-- 2. Trigger para UPDATE (Modificación)
CREATE OR ALTER TRIGGER [dbo].[trg_Audit_Usuario_Update]
ON [dbo].[Usuario]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdResponsable INT;
    DECLARE @NombreResponsable VARCHAR(255);
    
    -- Obtener el ID responsable del contexto
    SELECT @IdResponsable = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    -- Buscar el nombre del responsable
    SELECT @NombreResponsable = Nombre FROM Usuario WHERE Id_Usr = @IdResponsable;

    INSERT INTO UsuarioAuditoria (
        NombreUsuarioAfectado,
        AccionAuditoria, 
        FechaAuditoria, 
        UsuarioResponsable
    )
    SELECT 
        i.Nombre, 
        'MODIFICACION', 
        GETDATE(), 
        @NombreResponsable
    FROM inserted i;
END
GO

-- 3. Trigger para INSERT (Creación)
CREATE OR ALTER TRIGGER [dbo].[trg_Audit_Usuario_Insert]
ON [dbo].[Usuario]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdResponsable INT;
    DECLARE @NombreResponsable VARCHAR(255);
    
    SELECT @IdResponsable = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    SELECT @NombreResponsable = Nombre FROM Usuario WHERE Id_Usr = @IdResponsable;

    INSERT INTO UsuarioAuditoria (
        NombreUsuarioAfectado,
        AccionAuditoria, 
        FechaAuditoria, 
        UsuarioResponsable
    )
    SELECT 
        i.Nombre, 
        'CREACION', 
        GETDATE(), 
        @NombreResponsable
    FROM inserted i;
END
GO

-- 4. Trigger para DELETE (Eliminación)
CREATE OR ALTER TRIGGER [dbo].[trg_Audit_Usuario_Delete]
ON [dbo].[Usuario]
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdResponsable INT;
    DECLARE @NombreResponsable VARCHAR(255);
    
    SELECT @IdResponsable = CAST(SESSION_CONTEXT(N'Id_Usuario') AS INT);
    SELECT @NombreResponsable = Nombre FROM Usuario WHERE Id_Usr = @IdResponsable;

    INSERT INTO UsuarioAuditoria (
        NombreUsuarioAfectado,
        AccionAuditoria, 
        FechaAuditoria, 
        UsuarioResponsable
    )
    SELECT 
        d.Nombre, 
        'ELIMINACION', 
        GETDATE(), 
        @NombreResponsable
    FROM deleted d;
END
GO
