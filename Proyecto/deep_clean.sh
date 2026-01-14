#!/bin/bash
# deep_clean.sh
# Limpieza profunda para containerd y snap

echo "========================================================"
echo "🧹 LIMPIEZA PROFUNDA DE SISTEMA"
echo "========================================================"

# 1. Limpiar imágenes de Docker no utilizadas (No solo las 'dangling')
# Esto borrará cualquier imagen que no esté siendo usada por un contenedor ACTIVO.
echo ""
echo "🐳 Limpiando imágenes de Docker no usadas..."
docker system prune -a -f

# 2. Limpiar versiones antiguas de Snap (conservar solo 2 versiones)
echo ""
echo "📦 Configurando retención de Snap a 2 versiones..."
sudo snap set system refresh.retain=2

echo "🗑️  Eliminando snaps antiguos..."
# Script simple para remover snaps viejos
set -eu
LANG=C snap list --all | awk '/disabled/{print $1, $3}' |
    while read snapname revision; do
        sudo snap remove "$snapname" --revision="$revision"
    done

# 3. Limpiar caché de apt
echo ""
echo "🍬 Limpiando caché de paquetes APT..."
sudo apt-get clean
sudo apt-get autoremove -y

echo "========================================================"
echo "✅ Limpieza finalizada. Verificando espacio..."
sudo du -h --max-depth=1 /var | sort -rh | head -n 5
