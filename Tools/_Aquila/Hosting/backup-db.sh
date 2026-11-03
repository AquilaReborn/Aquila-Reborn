#!/usr/bin/env bash
# Бэкап базы Aquila. Запускается кроном от пользователя postgres, хранит дампы BACKUP_DAYS дней.
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/opt/ss14/backups}"
BACKUP_DAYS="${BACKUP_DAYS:-14}"
DB_NAME="${DB_NAME:-ss14}"

mkdir -p "$BACKUP_DIR"
file="$BACKUP_DIR/$DB_NAME-$(date +%Y%m%d-%H%M).dump"

pg_dump --format=custom --file="$file.tmp" "$DB_NAME"
mv "$file.tmp" "$file"

find "$BACKUP_DIR" -name "$DB_NAME-*.dump" -mtime "+$BACKUP_DAYS" -delete
