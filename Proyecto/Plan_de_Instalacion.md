# Plan de Instalación - VentaFacil

Este documento detalla el procedimiento técnico para el despliegue del sistema **VentaFacil** en el entorno de producción del cliente.

## 1. Introducción
El sistema VentaFacil es una solución web para la gestión de ventas, facturación y operaciones de caja. Este documento sirve como guía autoritativa para la instalación inicial y configuración del servidor VPS para asegurar un entorno estable, seguro y optimizado bajo una arquitectura de contenedores.

## 2. Planificación

*   **¿Cómo se hará?**: El despliegue se realizará utilizando **Docker** y **Docker Compose** para garantizar la portabilidad. La transferencia de archivos y configuración se ejecutará a través de una conexión segura **SSH**.
*   **¿Cuándo se hará?**: Se recomienda realizar la instalación en una ventana de mantenimiento de bajo tráfico (ej. 22:00 - 02:00) para evitar interrupciones en el servicio.
*   **¿Quiénes son los involucrados?**:
    *   Administrador de Sistemas (SysAdmin/DevOps).
    *   Líder Técnico del Proyecto.
    *   Personal de soporte de Hostinger (en caso de requerir ajustes en el panel de control).

## 3. Requisitos Previos

*   Acceso al VPS vía SSH con privilegios de `root` o `sudo`.
*   Dominio `ventafacil-web.com` apuntando a la IP `187.124.248.19`.
*   Git instalado en el servidor.
*   Conectividad a internet para la descarga de imágenes de Docker.

## 4. Ambiente en Producción

### Características
*   **Sistema Operativo**: Ubuntu 25.10 (Oracular Oriole).
*   **IP Pública**: 187.124.248.19.
*   **Hostname**: srv1549830.hstgr.cloud.
*   **Almacenamiento**: SSD recomendado para mejor rendimiento de SQL Server.

### Elementos mínimos por instalar (Paso a paso)

1.  **Actualización del Sistema**:
    ```bash
    sudo apt update && sudo apt upgrade -y
    ```

2.  **Instalación de Dependencias Básicas**:
    ```bash
    sudo apt install -y curl git apt-transport-https ca-certificates software-properties-common
    ```

3.  **Configuración de Zona Horaria**:
    ```bash
    sudo timedatectl set-timezone America/Costa_Rica
    ```

## 5. Instalación del Software

### Requisitos Mínimos
*   Docker Engine v24.0+.
*   Docker Compose v2.20+.
*   4GB de RAM (mínimo para SQL Server en contenedor).

### Paso a paso de instalación

#### A. Preparación de Docker y Docker Compose
Si has elegido una plantilla de VPS con Docker (recomendado en Hostinger), omite la instalación y solo verifica:
```bash
docker --version
docker compose version
```

Si Docker **no** está instalado, usa este comando simplificado (oficial de Docker):
```bash
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
```

#### B. Apertura de Puertos (Firewall UFW)
Es fundamental abrir los puertos necesarios para el funcionamiento de Nginx, Postfix y el acceso SSH:
```bash
sudo ufw allow 22/tcp      # SSH
sudo ufw allow 80/tcp      # HTTP
sudo ufw allow 443/tcp     # HTTPS
sudo ufw allow 25/tcp      # SMTP (Postfix)
sudo ufw allow 1433/tcp    # SQL Server (Solo si se requiere acceso externo)
sudo ufw enable
```

#### C. Despliegue de la Aplicación
1.  Clonar el repositorio:
    ```bash
    git clone https://github.com/tu-usuario/VentaFacil.git
    cd VentaFacil/Proyecto
    ```
2.  Construir e iniciar contenedores:
    ```bash
    sudo docker compose up -d --build
    ```

#### D. Configuración de Nginx y Postfix (Docker)
Al estar en contenedores, la configuración se encuentra automatizada en el archivo `docker-compose.yml`. El servicio Nginx dentro del contenedor escuchará en los puertos 80/443 y Postfix en el puerto 25.

## 6. Pruebas Post Instalación

Para evidenciar que la instalación fue exitosa, realizar las siguientes pruebas:

1.  **Estado de Contenedores**:
    ```bash
    sudo docker ps
    ```
    *Resultado esperado*: Todos los servicios (`ventafacil-web`, `ventafacil-db`, `ventafacil-nginx`, `ventafacil-postfix`) deben estar en estado `Up`.

2.  **Verificación de Acceso Web**:
    Ingresar a `http://ventafacil-web.com`. Debería redirigir automáticamente a HTTPS (si el certificado está configurado) o mostrar el login de la aplicación.

3.  **Prueba de Conectividad a Base de Datos**:
    Revisar los logs de la aplicación para confirmar la conexión exitosa:
    ```bash
    sudo docker logs ventafacil-web
    ```

4.  **Prueba de Envío de Correo**:
    Realizar una acción en el sistema que dispare un correo (ej. recuperación de contraseña) y verificar la salida en el contenedor de postfix:
    ```bash
    sudo docker logs ventafacil-postfix
    ```
