#!/bin/bash
# cleanup_docker.sh
# Script para liberar espacio en el VPS eliminando recursos no utilizados de Docker

echo "⚠️  INICIANDO LIMPIEZA DE DOCKER..."
echo "Espacio ANTES de la limpieza:"
docker system df

# 1. Eliminar contenedores detenidos
echo "🗑️  Eliminando contenedores detenidos..."
docker container prune -f

# 2. Eliminar imágenes 'dangling' (sin nombre/tag, residuos de builds anteriores)
echo "🗑️  Eliminando imágenes sin uso (dangling)..."
docker image prune -f

# 3. Eliminar caché de construcción (Esto es lo que más espacio ocupa usualmente)
echo "🗑️  Eliminando caché de Build..."
docker builder prune -f

# Opcional: Eliminar volumenes huérfanos (¡CUIDADO! Asegúrate que no tengas datos importantes en volumenes no atados a contenedores activos)
# docker volume prune -f

echo "✅ LIMPIEZA COMPLETADA."
echo "Espacio DESPUÉS de la limpieza:"
docker system df
