CREATE DATABASE VentaFacilDB;
USE VentaFacilDB;

CREATE TABLE Rol (Id_Rol INT IDENTITY (1,1),
				  Nombre_Rol VARCHAR(20),
				  Descripcion VARCHAR(255),
				  CONSTRAINT Rol_Pk PRIMARY KEY (Id_Rol));

CREATE TABLE Usuario (Id_Usr INT IDENTITY (1,1),
					  Nombre VARCHAR(255),
					  Correo VARCHAR(255),
					  Contrasena VARCHAR(255),
					  Estado BIT DEFAULT 1,
					  FechaCreacion datetime,
					  Rol INT,
					  CONSTRAINT Usr_Pk PRIMARY KEY (Id_Usr),
					  CONSTRAINT UsrRol_Fk FOREIGN  KEY (Rol) REFERENCES Rol(Id_Rol));

CREATE TABLE Planilla(Id_Planilla INT IDENTITY (1,1),
					  Id_Usr INT,
					  FechaInicio DATETIME,
					  FechaFinal DATETIME,
					  HorasTrabajadas INT,
					  Salario DECIMAL(10,2),
					  CONSTRAINT Plan_Pk PRIMARY KEY (Id_Planilla),
					  CONSTRAINT PlUsr_fk FOREIGN KEY (Id_Usr) REFERENCES Usuario(Id_Usr));

CREATE TABLE Categoria(Id_Categoria int IDENTITY (1,1),
					   Nombre VARCHAR(255),
					   Descripcion VARCHAR(1024),
					   CONSTRAINT Cat_Pk PRIMARY KEY (Id_Categoria));

CREATE TABLE Producto(Id_Producto int IDENTITY (1,1),
					  Nombre VARCHAR(255),
					  Descripcion VARCHAR(1024),
					  Precio DECIMAL(10,2),
					  Imagen VARCHAR(2048),
					  StockMinimo INT,
					  Estado BIT DEFAULT 1,
					  Id_Categoria INT,
					  CONSTRAINT Pro_Pk PRIMARY KEY (Id_Producto),
					  CONSTRAINT CatPro_Fk FOREIGN KEY (Id_Categoria) REFERENCES Categoria(Id_Categoria));

CREATE TABLE InventarioMovimiento(Id_Movimiento INT IDENTITY (1,1),
								  Id_Producto INT,
								  Tipo_Movimieto VARCHAR(255),
								  Cantidad INT,
								  Fecha DATETIME,
								  Id_Usuario INT,
								  CONSTRAINT IMov_Pk PRIMARY KEY (Id_Movimiento),
								  CONSTRAINT IMovPro_Fk FOREIGN KEY (Id_Producto) REFERENCES Producto(Id_Producto),
								  CONSTRAINT IMovUsr_Fk FOREIGN KEY (Id_Usuario) REFERENCES Usuario(Id_Usr));

CREATE TABLE Venta(Id_Venta INT IDENTITY (1,1),
				   Fecha DATETIME,
				   Total DECIMAL(10,2),
				   MetodoPago VARCHAR(255),
				   Estado BIT DEFAULT 1,
				   Id_Usuario INT,
				   CONSTRAINT Ven_Pk PRIMARY KEY (Id_Venta),
				   CONSTRAINT VUsr_Fk FOREIGN KEY (Id_Usuario) REFERENCES Usuario(Id_Usr));

CREATE TABLE Factura(Id_Factura INT IDENTITY (1,1),
					 Id_Venta INT,
					 FechaEmision DATETIME,
					 Total DECIMAL(10,2),
					 Estado BIT DEFAULT 1,
					 CONSTRAINT Fac_Pk PRIMARY KEY (Id_Factura),
					 CONSTRAINT FVen_Fk FOREIGN KEY (Id_Venta) REFERENCES Venta(Id_Venta));

CREATE TABLE DetalleVenta(Id_Detalle INT IDENTITY (1,1),
						  Id_Venta INT,
						  Id_Producto INT,
						  Cantidad INT,
						  PrecioUnitario DECIMAL(10,2),
						  Descuento DECIMAL(10,2),
						  CONSTRAINT Det_Pk PRIMARY KEY (Id_Detalle),
						  CONSTRAINT DetVen_Fk FOREIGN KEY (Id_Venta) REFERENCES Venta(Id_Venta),
						  CONSTRAINT DetProd_Fk FOREIGN KEY (Id_Producto) REFERENCES Producto(Id_Producto));

CREATE TABLE Promocion(Id_Promocion INT IDENTITY (1,1),
					   Nombre VARCHAR(233),
					   Descripcion VARCHAR(1024),
					   FechaInicio DATETIME,
					   FechaFin DATETIME,
					   Condiciones VARCHAR(2048),
					   Estado BIT DEFAULT 1,
					   CONSTRAINT Prm_PK PRIMARY KEY (Id_Promocion));


CREATE TABLE AplicacionPromocion(Id_Aplicacion INT IDENTITY (1,1),
								 Id_Promocion INT,
								 Id_Venta INT,
								 MontoDescuento DECIMAL(10,2),
								 CONSTRAINT Ap_PK PRIMARY KEY (Id_Aplicacion),
								 CONSTRAINT ApVen_Fk FOREIGN KEY (Id_Venta) REFERENCES Venta(Id_Venta),
								 CONSTRAINT ApPro_Fk FOREIGN KEY (Id_Promocion) REFERENCES Promocion(Id_Promocion));

CREATE TABLE BitacoraAccion(Id_Bitacora INT IDENTITY (1,1),
							Id_Usuario INT,
							Accion VARCHAR(255),
							FechaHora DATETIME,
							Descripcion VARCHAR (1024),
							CONSTRAINT Bit_Pk PRIMARY KEY (Id_Bitacora),
							CONSTRAINT BitUsr_Fk FOREIGN KEY (Id_Usuario) REFERENCES Usuario(Id_Usr));

CREATE TABLE Inventario (
        Id_Inventario INT IDENTITY (1,1),
        Id_Producto INT,
        StockActual INT,
        CONSTRAINT Inv_Pk PRIMARY KEY (Id_Inventario),
        CONSTRAINT InvProd_Fk FOREIGN KEY (Id_Producto) REFERENCES Producto(Id_Producto)
    );

--Datos iniciales
INSERT INTO Categoria (Nombre, Descripcion)
VALUES ('Comida', 'Categoria de comida');

INSERT INTO Categoria (Nombre, Descripcion)
VALUES ('Bebida', 'Categoria de bebidas');

INSERT INTO Categoria (Nombre, Descripcion)
VALUES ('Otros', 'otros productos');				