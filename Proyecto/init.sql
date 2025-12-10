-- Tabla Rol
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Rol]') AND type in (N'U'))
BEGIN
    CREATE TABLE Rol (
        Id_Rol INT IDENTITY (1,1),
        Nombre_Rol VARCHAR(20),
        Descripcion VARCHAR(255),
        CONSTRAINT Rol_Pk PRIMARY KEY (Id_Rol)
    );
END

-- Tabla Usuario
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Usuario]') AND type in (N'U'))
BEGIN
    CREATE TABLE Usuario (
        Id_Usr INT IDENTITY (1,1),
        Nombre VARCHAR(255),
        Correo VARCHAR(255),
        Contrasena VARCHAR(255),
        Estado BIT DEFAULT 1,
        FechaCreacion DATETIME DEFAULT GETDATE(),
        Rol INT,
        CONSTRAINT Usr_Pk PRIMARY KEY (Id_Usr),
        CONSTRAINT UsrRol_Fk FOREIGN KEY (Rol) REFERENCES Rol(Id_Rol)
    );
END

-- Tabla Nómina
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Nomina]') AND type in (N'U'))
BEGIN
    CREATE TABLE Nomina (
        Id_Nomina INT IDENTITY(1,1) PRIMARY KEY,
        FechaInicio DATETIME NOT NULL,
        FechaFinal DATETIME NOT NULL,
        FechaGeneracion DATETIME NOT NULL DEFAULT GETDATE(),
        Estado VARCHAR(20) NOT NULL,
        TotalBruto DECIMAL(10,2) NOT NULL,
        TotalDeducciones DECIMAL(10,2) NOT NULL,
        TotalNeto DECIMAL(10,2) NOT NULL
    );
END

-- Tabla Planilla
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Planilla]') AND type in (N'U'))
BEGIN
    CREATE TABLE Planilla (
        Id_Planilla INT IDENTITY (1,1),
        Id_Usr INT,
        FechaInicio DATETIME,
        FechaFinal DATETIME,
        HorasTrabajadas INT,
        Salario DECIMAL(10,2),
        Bonificaciones     DECIMAL(10,2) NULL,
        Deducciones        DECIMAL(10,2) NULL,
        EstadoRegistro     VARCHAR(50)   NULL,
        HorasExtras        DECIMAL(10,2) NULL,
        Id_Nomina          INT           NULL,
        SalarioBruto       DECIMAL(10,2) NULL,
        SalarioNeto        DECIMAL(10,2) NULL,
        Observaciones      VARCHAR(400)  NULL,
        CONSTRAINT Plan_Pk PRIMARY KEY (Id_Planilla),
        CONSTRAINT PlUsr_fk FOREIGN KEY (Id_Usr) REFERENCES Usuario(Id_Usr),
        CONSTRAINT FK_Planilla_Nomina FOREIGN KEY (Id_Nomina) REFERENCES Nomina(Id_Nomina)
    );
END

-- Tabla Categoria
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categoria]') AND type in (N'U'))
BEGIN
    CREATE TABLE Categoria (
        Id_Categoria INT IDENTITY (1,1),
        Nombre VARCHAR(255),
        Descripcion VARCHAR(1024),
        CONSTRAINT Cat_Pk PRIMARY KEY (Id_Categoria)
    );
END

-- Tabla Producto
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Producto]') AND type in (N'U'))
BEGIN
    CREATE TABLE Producto (
        Id_Producto INT IDENTITY (1,1),
        Nombre VARCHAR(255),
        Descripcion VARCHAR(1024),
        Precio DECIMAL(10,2),
        Imagen VARCHAR(2048),
        StockMinimo INT,
        Estado BIT DEFAULT 1,
        Id_Categoria INT,
        CONSTRAINT Pro_Pk PRIMARY KEY (Id_Producto),
        CONSTRAINT CatPro_Fk FOREIGN KEY (Id_Categoria) REFERENCES Categoria(Id_Categoria)
    );
END

-- Tabla Inventario
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Inventario]') AND type in (N'U'))
BEGIN
    CREATE TABLE Inventario (
        Id_Inventario INT IDENTITY (1,1),
        Nombre VARCHAR(255) NOT NULL,
        StockActual INT NOT NULL,
        StockMinimo INT NOT NULL,
        UnidadMedida INT NOT NULL,
        CONSTRAINT Inv_Pk PRIMARY KEY (Id_Inventario)
    );
END

-- Tabla InventarioMovimiento
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InventarioMovimiento]') AND type in (N'U'))
BEGIN
    CREATE TABLE InventarioMovimiento (
        Id_Movimiento INT IDENTITY (1,1),
        Id_Inventario INT NOT NULL,
        Tipo_Movimiento VARCHAR(255),
        Cantidad INT NOT NULL,
        Fecha DATETIME,
        Id_Usuario INT NOT NULL,
        Observaciones VARCHAR(1024),
        CONSTRAINT IMov_Pk PRIMARY KEY (Id_Movimiento),
        CONSTRAINT IMovInv_Fk FOREIGN KEY (Id_Inventario) REFERENCES Inventario(Id_Inventario),
        CONSTRAINT IMovUsr_Fk FOREIGN KEY (Id_Usuario) REFERENCES Usuario(Id_Usr)
    );
END

-- Tabla Venta
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Venta]') AND type in (N'U'))
BEGIN
    CREATE TABLE Venta (
        Id_Venta INT IDENTITY (1,1),
        Fecha DATETIME,
        Total DECIMAL(10,2),
        MetodoPago VARCHAR(255),
        Estado BIT DEFAULT 1,
        Id_Usuario INT,
        CONSTRAINT Ven_Pk PRIMARY KEY (Id_Venta),
        CONSTRAINT VUsr_Fk FOREIGN KEY (Id_Usuario) REFERENCES Usuario(Id_Usr)
    );
END

-- Tabla Factura
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Factura]') AND type in (N'U'))
BEGIN
    CREATE TABLE Factura (
        Id_Factura INT IDENTITY (1,1),
        Id_Venta INT,
        Cliente VARCHAR(255),
        FechaEmision DATETIME,
        Total DECIMAL(10,2),
        MontoPagado DECIMAL(10,2) DEFAULT 0,
        Cambio DECIMAL(10,2) DEFAULT 0,
        Moneda VARCHAR(3) DEFAULT 'CRC',
        MetodoPago VARCHAR(20) DEFAULT 'Efectivo',
        TasaCambio DECIMAL(10,4) NULL,
        Estado INT DEFAULT 0, 
        Justificacion VARCHAR(MAX) NULL, 
        FacturaOriginalId INT NULL, 
        PdfData VARBINARY(MAX) NULL,        
        PdfFileName VARCHAR(255) NULL,    
        CONSTRAINT Fac_Pk PRIMARY KEY (Id_Factura),
        CONSTRAINT FVen_Fk FOREIGN KEY (Id_Venta) REFERENCES Venta(Id_Venta),
        CONSTRAINT FK_Factura_Original FOREIGN KEY (FacturaOriginalId) REFERENCES Factura(Id_Factura)
    );
END

-- Tabla PagoFactura
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PagoFactura]') AND type in (N'U'))
BEGIN
    CREATE TABLE PagoFactura (
        Id INT IDENTITY(1,1),
        FacturaId INT NOT NULL,
        MetodoPago VARCHAR(20) NOT NULL,
        Monto DECIMAL(10,2) NOT NULL,
        Moneda VARCHAR(3) DEFAULT 'CRC' NOT NULL,
        TasaCambio DECIMAL(10,4) NULL,
        CONSTRAINT PK_PagoFactura PRIMARY KEY (Id),
        CONSTRAINT FK_PagoFactura_Factura FOREIGN KEY (FacturaId) REFERENCES Factura(Id_Factura) ON DELETE CASCADE
    );
END

-- Tabla DetalleVenta
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DetalleVenta]') AND type in (N'U'))
BEGIN
    CREATE TABLE DetalleVenta (
        Id_Detalle INT IDENTITY (1,1),
        Id_Venta INT,
        Id_Producto INT,
        Cantidad INT,
        PrecioUnitario DECIMAL(10,2),
        Descuento DECIMAL(10,2),
        CONSTRAINT Det_Pk PRIMARY KEY (Id_Detalle),
        CONSTRAINT DetVen_Fk FOREIGN KEY (Id_Venta) REFERENCES Venta(Id_Venta),
        CONSTRAINT DetProd_Fk FOREIGN KEY (Id_Producto) REFERENCES Producto(Id_Producto)
    );
END

-- Tabla Promocion
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Promocion]') AND type in (N'U'))
BEGIN
    CREATE TABLE Promocion (
        Id_Promocion INT IDENTITY (1,1),
        Nombre VARCHAR(233),
        Descripcion VARCHAR(1024),
        FechaInicio DATETIME,
        FechaFin DATETIME,
        Condiciones VARCHAR(2048),
        Estado BIT DEFAULT 1,
        CONSTRAINT Prm_PK PRIMARY KEY (Id_Promocion)
    );
END

-- Tabla AplicacionPromocion
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AplicacionPromocion]') AND type in (N'U'))
BEGIN
    CREATE TABLE AplicacionPromocion (
        Id_Aplicacion INT IDENTITY (1,1),
        Id_Promocion INT,
        Id_Venta INT,
        MontoDescuento DECIMAL(10,2),
        CONSTRAINT Ap_PK PRIMARY KEY (Id_Aplicacion),
        CONSTRAINT ApVen_Fk FOREIGN KEY (Id_Venta) REFERENCES Venta(Id_Venta),
        CONSTRAINT ApPro_Fk FOREIGN KEY (Id_Promocion) REFERENCES Promocion(Id_Promocion)
    );
END

-- Tabla BitacoraAccion
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BitacoraAccion]') AND type in (N'U'))
BEGIN
    CREATE TABLE BitacoraAccion (
        Id_Bitacora INT IDENTITY (1,1),
        Id_Usuario INT,
        Accion VARCHAR(255),
        FechaHora DATETIME,
        Descripcion VARCHAR (1024),
        CONSTRAINT Bit_Pk PRIMARY KEY (Id_Bitacora),
        CONSTRAINT BitUsr_Fk FOREIGN KEY (Id_Usuario) REFERENCES Usuario(Id_Usr)
    );
END

-- Tabla Caja
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Caja]') AND type in (N'U'))
BEGIN
    CREATE TABLE Caja (
        Id_Caja INT IDENTITY(1,1) PRIMARY KEY,
        Id_Usuario INT NOT NULL,
        Fecha_Apertura DATETIME NOT NULL DEFAULT GETDATE(),
        Fecha_Cierre DATETIME NULL,
        Monto_Inicial DECIMAL(10,2) NOT NULL,
        Monto DECIMAL(10,2) NULL,
        Estado VARCHAR(20) NOT NULL,
        CONSTRAINT FK_Caja_Usuario FOREIGN KEY (Id_Usuario) REFERENCES Usuario(Id_Usr)
    );
END

-- Tabla CajaRetiro
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CajaRetiro]') AND type in (N'U'))
BEGIN
    CREATE TABLE CajaRetiro (
        Id_Retiro INT IDENTITY(1,1) PRIMARY KEY,
        Id_Caja INT NOT NULL,
        Id_Usuario INT NOT NULL,
        Monto DECIMAL(10,2) NOT NULL,
        Motivo VARCHAR(255) NOT NULL,
        FechaHora DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_CajaRetiro_Caja FOREIGN KEY (Id_Caja) REFERENCES Caja(Id_Caja),
        CONSTRAINT FK_CajaRetiro_Usuario FOREIGN KEY (Id_Usuario) REFERENCES Usuario(Id_Usr)
    );
END

-- Tabla InventarioMovimientoAuditoria
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InventarioMovimientoAuditoria]') AND type in (N'U'))
BEGIN
    CREATE TABLE InventarioMovimientoAuditoria (
        Id_Auditoria INT IDENTITY(1,1) PRIMARY KEY,
        Id_Movimiento INT NOT NULL,
        Id_Inventario INT NOT NULL,
        CantidadAnterior INT NOT NULL,
        CantidadNueva INT NOT NULL,
        TipoMovimientoAnterior VARCHAR(255),
        TipoMovimientoNuevo VARCHAR(255),
        MotivoCambio VARCHAR(1024),
        FechaCambio DATETIME DEFAULT GETDATE(),
        Id_UsuarioResponsable INT NOT NULL,
        CONSTRAINT FK_Auditoria_Movimiento FOREIGN KEY (Id_Movimiento) REFERENCES InventarioMovimiento(Id_Movimiento),
        CONSTRAINT FK_Auditoria_Inventario FOREIGN KEY (Id_Inventario) REFERENCES Inventario(Id_Inventario),
        CONSTRAINT FK_Auditoria_Usuario FOREIGN KEY (Id_UsuarioResponsable) REFERENCES Usuario(Id_Usr)
    );
END

-- Tabla DeduccionLey
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeduccionLey]') AND type in (N'U'))
BEGIN
    CREATE TABLE DeduccionLey (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre VARCHAR(50) NOT NULL,
        Porcentaje DECIMAL(5,2) NOT NULL,
        Activo BIT DEFAULT 1
    );
END

-- Tabla ImpuestoRenta
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ImpuestoRenta]') AND type in (N'U'))
BEGIN
    CREATE TABLE ImpuestoRenta (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Anio INT NOT NULL,
        LimiteInferior DECIMAL(18,2) NOT NULL,
        LimiteSuperior DECIMAL(18,2) NULL,
        Porcentaje DECIMAL(5,2) NOT NULL
    );
END

-- Tabla Bonificacion
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Bonificacion]') AND type in (N'U'))
BEGIN
    CREATE TABLE Bonificacion (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Id_Planilla INT NOT NULL,
        Monto DECIMAL(10,2) NOT NULL,
        Motivo VARCHAR(255) NOT NULL,
        Fecha DATETIME NOT NULL,
        FechaRegistro DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_Bonificacion_Planilla FOREIGN KEY (Id_Planilla) REFERENCES Planilla(Id_Planilla)
    );
END

-- Tabla BonificacionAuditoria
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BonificacionAuditoria]') AND type in (N'U'))
BEGIN
    CREATE TABLE BonificacionAuditoria (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Id_Bonificacion INT NOT NULL,
        MontoAnterior DECIMAL(10,2) NOT NULL,
        MontoNuevo DECIMAL(10,2) NOT NULL,
        MotivoCambio VARCHAR(1024) NULL,
        FechaCambio DATETIME DEFAULT GETDATE(),
        Id_UsuarioResponsable INT NOT NULL,
        CONSTRAINT FK_BonificacionAuditoria_Bonificacion FOREIGN KEY (Id_Bonificacion) REFERENCES Bonificacion(Id),
        CONSTRAINT FK_BonificacionAuditoria_Usuario FOREIGN KEY (Id_UsuarioResponsable) REFERENCES Usuario(Id_Usr)
    );
END

-- Alterar Tabla Usuario para Horario
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'HoraEntrada' AND Object_ID = Object_ID(N'Usuario'))
BEGIN
    ALTER TABLE Usuario ADD HoraEntrada TIME NULL
END

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'HoraSalida' AND Object_ID = Object_ID(N'Usuario'))
BEGIN
    ALTER TABLE Usuario ADD HoraSalida TIME NULL
END

-- Roles base
IF NOT EXISTS (SELECT * FROM Rol WHERE Nombre_Rol = 'Administrador')
    INSERT INTO Rol (Nombre_Rol, Descripcion) VALUES ('Administrador', 'Acceso completo al sistema');

IF NOT EXISTS (SELECT * FROM Rol WHERE Nombre_Rol = 'Cajero')
    INSERT INTO Rol (Nombre_Rol, Descripcion) VALUES ('Cajero', 'Acceso a ventas y caja');

-- Seed Deducciones
IF NOT EXISTS (SELECT * FROM DeduccionLey WHERE Nombre = 'SEM')
    INSERT INTO DeduccionLey (Nombre, Porcentaje, Activo) VALUES ('SEM', 5.50, 1);
IF NOT EXISTS (SELECT * FROM DeduccionLey WHERE Nombre = 'IVM')
    INSERT INTO DeduccionLey (Nombre, Porcentaje, Activo) VALUES ('IVM', 4.17, 1);
IF NOT EXISTS (SELECT * FROM DeduccionLey WHERE Nombre = 'LPT')
    INSERT INTO DeduccionLey (Nombre, Porcentaje, Activo) VALUES ('LPT', 1.00, 1);

-- Seed Impuesto Renta 2025
IF NOT EXISTS (SELECT * FROM ImpuestoRenta WHERE Anio = 2025 AND LimiteInferior = 0)
    INSERT INTO ImpuestoRenta (Anio, LimiteInferior, LimiteSuperior, Porcentaje) VALUES (2025, 0, 922000, 0);
IF NOT EXISTS (SELECT * FROM ImpuestoRenta WHERE Anio = 2025 AND LimiteInferior = 922000)
    INSERT INTO ImpuestoRenta (Anio, LimiteInferior, LimiteSuperior, Porcentaje) VALUES (2025, 922000, 1363000, 10);
IF NOT EXISTS (SELECT * FROM ImpuestoRenta WHERE Anio = 2025 AND LimiteInferior = 1363000)
    INSERT INTO ImpuestoRenta (Anio, LimiteInferior, LimiteSuperior, Porcentaje) VALUES (2025, 1363000, 2374000, 15);
IF NOT EXISTS (SELECT * FROM ImpuestoRenta WHERE Anio = 2025 AND LimiteInferior = 2374000)
    INSERT INTO ImpuestoRenta (Anio, LimiteInferior, LimiteSuperior, Porcentaje) VALUES (2025, 2374000, 4745000, 20);
IF NOT EXISTS (SELECT * FROM ImpuestoRenta WHERE Anio = 2025 AND LimiteInferior = 4745000)
    INSERT INTO ImpuestoRenta (Anio, LimiteInferior, LimiteSuperior, Porcentaje) VALUES (2025, 4745000, NULL, 25);

-- Tabla PasswordResetToken
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[PasswordResetToken]') AND type in (N'U'))
BEGIN
    CREATE TABLE PasswordResetToken (
        Id INT IDENTITY (1,1),
        UsuarioId INT NOT NULL,
        Token VARCHAR(255) NOT NULL,
        ExpirationDate DATETIME NOT NULL,
        IsUsed BIT DEFAULT 0,
        CONSTRAINT Tok_Pk PRIMARY KEY (Id),
        CONSTRAINT TokUsr_Fk FOREIGN KEY (UsuarioId) REFERENCES Usuario(Id_Usr)
    )
END;
