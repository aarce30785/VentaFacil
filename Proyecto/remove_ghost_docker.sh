#!/bin/bash
# remove_ghost_docker.sh
# ELIMINA DATOS HUÉRFANOS DE INSTALACIONES ANTIGUAS DE DOCKER (APT)
# Úsalo SOLO si tu Docker actual es SNAP.

echo "========================================================"
echo "👻 ELIMINADOR DE DATOS FANTASMA (DOCKER)"
echo "========================================================"

# 1. VERIFICACIÓN DE SEGURIDAD
CURRENT_ROOT=$(docker info 2>/dev/null | grep "Docker Root Dir" | awk '{print $4}')

echo "🔎 Directorio Docker actual: $CURRENT_ROOT"

if [[ "$CURRENT_ROOT" == *"/snap/"* ]]; then
    echo "✅ CONFIRMADO: Estás usando Docker versión SNAP."
else
    echo "⛔ PELIGRO: Tu Docker NO parece ser Snap o no se pudo verificar."
    echo "   Root detectado: $CURRENT_ROOT"
    echo "   ABORTANDO PARA NO BORRAR TUS DATOS ACTIVOS."
    exit 1
fi

echo ""
echo "--------------------------------------------------------"
echo "⚠️  ADVERTENCIA FINAL: Se eliminarán:"
echo "   - /var/lib/docker"
echo "   - /var/lib/containerd"
echo "   Esto borrará cualquier contenedor/imagen de instalaciones antiguas (APT)."
echo "--------------------------------------------------------"
read -p "¿Estás seguro de continuar? (s/n): " confirm

if [[ "$confirm" != "s" && "$confirm" != "S" ]]; then
    echo "Cancelado."
    exit 0
fi

# 2. ELIMINACIÓN
echo ""
echo "🗑️  Eliminando /var/lib/containerd..."
if [ -d "/var/lib/containerd" ]; then
    sudo rm -rf /var/lib/containerd
    echo "   ✅ Eliminado."
else
    echo "   (No existía)"
fi

echo "🗑️  Eliminando /var/lib/docker..."
if [ -d "/var/lib/docker" ]; then
    sudo rm -rf /var/lib/docker
    echo "   ✅ Eliminado."
else
    echo "   (No existía)"
fi

echo ""
echo "🎉 ¡LISTO! Espacio reclamado."
echo "Verificando espacio actual:"
sudo du -h --max-depth=1 /var | sort -rh | head -n 5
