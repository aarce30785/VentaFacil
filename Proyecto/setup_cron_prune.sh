#!/bin/bash
# setup_cron_prune.sh
# Configura un cron job para limpiar Docker semanalmente

# Comando de limpieza
CRON_JOB="0 3 * * 0 /usr/bin/docker system prune -f"

# Intentar añadir al crontab del usuario actual si no existe
(crontab -l 2>/dev/null | grep -F "$CRON_JOB") || (crontab -l 2>/dev/null; echo "$CRON_JOB") | crontab -

echo "✅ Cron job configurado: Todos los domingos a las 3:00 AM se ejecutará 'docker system prune -f'"
crontab -l | grep "docker system prune"
