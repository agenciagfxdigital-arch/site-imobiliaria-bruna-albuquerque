#!/bin/sh
# Copia as fotos do volume para fora da VPS (ADR-010, docs/DEPLOY.md). Rodar pelo cron, uma vez por dia.
# Usa "copy", não "sync": apagar uma foto aqui, sem querer ou de propósito, não apaga a cópia do backup.
set -eu

cd "$(dirname "$0")"
BACKUP_DESTINO=$(sed -n 's/^BACKUP_DESTINO=//p' .env)
if [ -z "$BACKUP_DESTINO" ]; then
    echo "Defina BACKUP_DESTINO no deploy/.env" >&2
    exit 1
fi

docker run --rm \
    -v bruna_fotos:/fotos:ro \
    -v "$HOME/.config/rclone:/config/rclone:ro" \
    rclone/rclone:1.71 copy /fotos "$BACKUP_DESTINO" --immutable
echo "Backup das fotos concluído em $(date '+%d/%m/%Y %H:%M')"
