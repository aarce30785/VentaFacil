#!/bin/bash
# audit_disk.sh
# Script para identificar qué carpetas dentro de /var están consumiendo espacio

echo "========================================================"
echo "📊  AUDITORÍA DE ESPACIO DE DISCO (VPS)"
echo "========================================================"

echo ""
echo "📂 Top 10 directorios más grandes en /var:"
sudo du -h /var | sort -rh | head -n 10

echo ""
echo "--------------------------------------------------------"
echo "🐳 Desglose de /var/lib/docker (si existe):"
if [ -d "/var/lib/docker" ]; then
    sudo du -h --max-depth=1 /var/lib/docker | sort -h
else
    echo "⚠️  /var/lib/docker no encontrado."
fi

echo ""
echo "--------------------------------------------------------"
echo "📝 Desglose de /var/log (Logs del sistema):"
sudo du -h --max-depth=1 /var/log | sort -h

echo ""
echo "========================================================"
echo "Consejo: Si /var/lib/docker/overlay2 es gigante, ejecuta ./cleanup_docker.sh"
echo "Consejo: Si /var/log/journal es gigante, ejecuta: 'journalctl --vacuum-time=2d'"
echo "========================================================"
