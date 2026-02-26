#!/bin/bash
# migrate_docker_snap_to_official.sh
# MIGRACIÓN DE DOCKER SNAP A DOCKER OFICIAL (DEBIAN/UBUNTU)

echo "🚀 Iniciando migración de Docker..."

# 1. Detener contenedores actuales
if command -v docker &> /dev/null; then
    echo "⏹️ Deteniendo contenedores activos..."
    docker-compose down || true
fi

# 2. Desinstalar versión Snap
echo "🗑️ Desinstalando Docker Snap..."
sudo snap remove docker

# 3. Eliminar datos antiguos (usando el script existente o comandos directos)
echo "🧹 Limpiando restos de instalaciones antiguas..."
sudo rm -rf /var/lib/docker
sudo rm -rf /var/lib/containerd

# 4. Instalar Docker oficial
echo "📦 Instalando dependencias..."
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg

echo "🔑 Añadiendo llave GPG oficial de Docker..."
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo "📝 Configurando repositorio..."
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update

echo "🛠️ Instalando Docker Engine y Docker Compose plugin..."
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# 5. Configurar permisos del usuario actual
echo "👤 Añadiendo usuario al grupo docker..."
sudo usermod -aG docker $USER

echo "✅ Migración completada."
echo "⚠️  IMPORTANTE: Cierra sesión y vuelve a entrar (o reinicia) para que los cambios de grupo surtan efecto."
echo "Luego, usa 'docker compose up -d' para reiniciar tus servicios."
