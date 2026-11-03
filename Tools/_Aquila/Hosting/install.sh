#!/usr/bin/env bash
# Установка сервера Aquila на чистую Ubuntu 24.04: PostgreSQL, Robust.Cdn, SS14.Watchdog, nginx, HTTPS, файрвол, бэкапы.
# Запуск: sudo ./install.sh <домен сервера> <домен CDN> <почта для Let's Encrypt>
# Пример: sudo ./install.sh aquila.example.ru cdn.aquila.example.ru admin@example.ru
# Скрипт можно запускать повторно: он обновит Watchdog и CDN до свежих версий и перепишет конфиги, сохранив токены и пароль базы.
set -euo pipefail

if [[ $EUID -ne 0 ]]; then
    echo "Запусти через sudo." >&2
    exit 1
fi

if [[ $# -lt 3 ]]; then
    echo "Использование: sudo $0 <домен сервера> <домен CDN> <почта>" >&2
    exit 1
fi

DOMAIN="$1"
CDN_DOMAIN="$2"
EMAIL="$3"

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEMPLATES="$HERE/templates"
SECRETS_FILE="/root/aquila-secrets.env"

SS14_HOME="/opt/ss14"
WATCHDOG_DIR="$SS14_HOME/watchdog"
CDN_DIR="/opt/robust-cdn"
SRC_DIR="$SS14_HOME/src"
DOTNET_DIR="/usr/share/dotnet"

step() { echo; echo "==> $*"; }

step "Пакеты"
apt-get update
apt-get install -y git curl openssl nginx postgresql certbot python3-certbot-nginx ufw

step ".NET 10 SDK"
if ! "$DOTNET_DIR/dotnet" --list-sdks 2>/dev/null | grep -q '^10\.'; then
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$DOTNET_DIR"
    ln -sf "$DOTNET_DIR/dotnet" /usr/bin/dotnet
fi
mkdir -p /etc/dotnet
echo "$DOTNET_DIR" > /etc/dotnet/install_location
export DOTNET_ROOT="$DOTNET_DIR"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

step "Секреты"
if [[ ! -f "$SECRETS_FILE" ]]; then
    {
        echo "WATCHDOG_TOKEN=$(openssl rand -hex 32)"
        echo "PUBLISH_TOKEN=$(openssl rand -hex 32)"
        echo "DB_PASSWORD=$(openssl rand -hex 24)"
    } > "$SECRETS_FILE"
    chmod 600 "$SECRETS_FILE"
fi
# shellcheck disable=SC1090
source "$SECRETS_FILE"

step "Пользователь ss14"
if ! id ss14 &>/dev/null; then
    useradd --system --create-home --home-dir "$SS14_HOME" --shell /usr/sbin/nologin ss14
fi
mkdir -p "$SRC_DIR" "$WATCHDOG_DIR/instances/aquila" "$CDN_DIR/bin" "$CDN_DIR/builds" "$CDN_DIR/database" "$SS14_HOME/backups"

step "PostgreSQL"
if ! sudo -u postgres psql -tAc "SELECT 1 FROM pg_roles WHERE rolname='ss14'" | grep -q 1; then
    sudo -u postgres psql -c "CREATE ROLE ss14 LOGIN PASSWORD '$DB_PASSWORD';"
fi
if ! sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='ss14'" | grep -q 1; then
    sudo -u postgres createdb -O ss14 ss14
fi

# Клонирует или обновляет репозиторий и публикует его в папку назначения.
publish() {
    local repo="$1" name="$2" project="$3" out="$4"
    if [[ -d "$SRC_DIR/$name/.git" ]]; then
        git -C "$SRC_DIR/$name" pull --ff-only
        git -C "$SRC_DIR/$name" submodule update --init --recursive
    else
        git clone --recursive "$repo" "$SRC_DIR/$name"
    fi
    dotnet publish "$SRC_DIR/$name/$project" -c Release -r linux-x64 --no-self-contained -o "$out"
}

render() {
    sed -e "s|@DOMAIN@|$DOMAIN|g" \
        -e "s|@CDN_DOMAIN@|$CDN_DOMAIN|g" \
        -e "s|@WATCHDOG_TOKEN@|$WATCHDOG_TOKEN|g" \
        -e "s|@PUBLISH_TOKEN@|$PUBLISH_TOKEN|g" \
        -e "s|@DB_PASSWORD@|$DB_PASSWORD|g" \
        "$TEMPLATES/$1" > "$2"
}

systemctl stop ss14-watchdog robust-cdn 2>/dev/null || true

step "Robust.Cdn"
publish https://github.com/space-wizards/Robust.Cdn.git Robust.Cdn Robust.Cdn/Robust.Cdn.csproj "$CDN_DIR/bin"
render cdn-appsettings.json "$CDN_DIR/appsettings.json"

step "SS14.Watchdog"
publish https://github.com/space-wizards/SS14.Watchdog.git SS14.Watchdog SS14.Watchdog/SS14.Watchdog.csproj "$WATCHDOG_DIR"
render watchdog-appsettings.yml "$WATCHDOG_DIR/appsettings.yml"
render config.toml "$WATCHDOG_DIR/instances/aquila/config.toml"

chown -R ss14:ss14 "$SS14_HOME" "$CDN_DIR"
chmod 600 "$WATCHDOG_DIR/appsettings.yml" "$WATCHDOG_DIR/instances/aquila/config.toml" "$CDN_DIR/appsettings.json"

step "systemd"
cp "$TEMPLATES/robust-cdn.service" /etc/systemd/system/robust-cdn.service
cp "$TEMPLATES/ss14-watchdog.service" /etc/systemd/system/ss14-watchdog.service
systemctl daemon-reload
systemctl enable --now robust-cdn
systemctl enable --now ss14-watchdog

step "nginx и HTTPS"
render nginx-aquila.conf /etc/nginx/sites-available/aquila.conf
ln -sf /etc/nginx/sites-available/aquila.conf /etc/nginx/sites-enabled/aquila.conf
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl reload nginx
certbot --nginx --non-interactive --agree-tos --redirect --reinstall -m "$EMAIL" -d "$DOMAIN" -d "$CDN_DOMAIN"

step "Файрвол"
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 1212/udp
ufw --force enable

step "Бэкапы"
install -m 755 "$HERE/backup-db.sh" /usr/local/bin/aquila-backup-db
chown postgres:postgres "$SS14_HOME/backups"
echo "30 5 * * * postgres BACKUP_DIR=$SS14_HOME/backups /usr/local/bin/aquila-backup-db" > /etc/cron.d/aquila-backup

echo
echo "Готово. Секреты лежат в $SECRETS_FILE."
echo "PUBLISH_TOKEN из этого файла положи в секрет PUBLISH_TOKEN репозитория на GitHub."
echo "Сервер запустится, когда на CDN появится первый билд: запусти воркфлоу Publish."
