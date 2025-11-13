

-- Tabla Rol
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Rol]') AND type in (N'U'))
BEGIN
    CREATE TABLE Rol (
        Id_Rol INT IDENTITY (1,1),
        Nombre_Rol VARCHAR(20),
        Descripcion VARCHAR(255),
        CONSTRAINT Rol_Pk PRIMARY KEY (Id_Rol)
    );
END;


-- Tabla Usuario
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Usuario]') AND type in (N'U'))
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
END;


-- Tabla Planilla
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Planilla]') AND type in (N'U'))
BEGIN
    CREATE TABLE Planilla (
        Id_Planilla INT IDENTITY (1,1),
        Id_Usr INT,
        FechaInicio DATETIME,
        FechaFinal DATETIME,
        HorasTrabajadas INT,
        Salario DECIMAL(10,2),
        CONSTRAINT Plan_Pk PRIMARY KEY (Id_Planilla),
        CONSTRAINT PlUsr_fk FOREIGN KEY (Id_Usr) REFERENCES Usuario(Id_Usr)
    );
END;


-- Tabla Categoria
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Categoria]') AND type in (N'U'))
BEGIN
    CREATE TABLE Categoria (
        Id_Categoria INT IDENTITY (1,1),
        Nombre VARCHAR(255),
        Descripcion VARCHAR(1024),
        CONSTRAINT Cat_Pk PRIMARY KEY (Id_Categoria)
    );
END;

-- Tabla Producto
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Producto]') AND type in (N'U'))
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
END;

-- Tabla Inventario
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Inventario]') AND type in (N'U'))
BEGIN
    CREATE TABLE Inventario (
        Id_Inventario INT IDENTITY (1,1),
        Nombre VARCHAR(255) NOT NULL,
        StockActual INT NOT NULL,
        StockMinimo INT NOT NULL,
        CONSTRAINT Inv_Pk PRIMARY KEY (Id_Inventario)
    );
END
GO


-- Tabla InventarioMovimiento
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[InventarioMovimiento]') AND type in (N'U'))
BEGIN
    CREATE TABLE InventarioMovimiento (
        Id_Movimiento INT IDENTITY (1,1),
        Id_Inventario INT NOT NULL,
        Tipo_Movimiento VARCHAR(255),
        Cantidad INT NOT NULL,
        Fecha DATETIME,
        Id_Usuario INT NOT NULL,
        CONSTRAINT IMov_Pk PRIMARY KEY (Id_Movimiento),
        CONSTRAINT IMovInv_Fk FOREIGN KEY (Id_Inventario) REFERENCES Inventario(Id_Inventario),
        CONSTRAINT IMovUsr_Fk FOREIGN KEY (Id_Usuario) REFERENCES Usuario(Id_Usr)
    );
END;

-- Tabla Venta
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Venta]') AND type in (N'U'))
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
END;

-- Tabla Factura
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Factura]') AND type in (N'U'))
BEGIN
    CREATE TABLE Factura (
        Id_Factura INT IDENTITY (1,1),
        Id_Venta INT,
        FechaEmision DATETIME,
        Total DECIMAL(10,2),
        Estado BIT DEFAULT 1,
        CONSTRAINT Fac_Pk PRIMARY KEY (Id_Factura),
        CONSTRAINT FVen_Fk FOREIGN KEY (Id_Venta) REFERENCES Venta(Id_Venta)
    );
END;

-- Tabla DetalleVenta
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[DetalleVenta]') AND type in (N'U'))
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
END;

-- Tabla Promocion
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Promocion]') AND type in (N'U'))
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
END;

-- Tabla AplicacionPromocion
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[AplicacionPromocion]') AND type in (N'U'))
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
END;

-- Tabla BitacoraAccion
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[BitacoraAccion]') AND type in (N'U'))
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
END;

-- Tabla Caja
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Caja]') AND type in (N'U'))
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
END;

-- Tabla CajaRetiro
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[CajaRetiro]') AND type in (N'U'))
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

-- Roles base
IF NOT EXISTS (SELECT 1 FROM Rol WHERE Nombre_Rol = 'Administrador')
    INSERT INTO Rol (Nombre_Rol, Descripcion) VALUES ('Administrador', 'Acceso completo al sistema');
IF NOT EXISTS (SELECT 1 FROM Rol WHERE Nombre_Rol = 'Cajero')
    INSERT INTO Rol (Nombre_Rol, Descripcion) VALUES ('Cajero', 'Acceso a ventas y caja');

