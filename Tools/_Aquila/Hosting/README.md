# Хостинг Aquila

Набор для запуска сервера Aquila на своём VPS: свой репозиторий на GitHub, свой CDN с билдами, Watchdog, PostgreSQL, nginx с HTTPS и ежедневные бэкапы базы.

Как это работает:

1. GitHub Actions (воркфлоу `Publish`) собирает сервер и клиент и заливает билд на твой Robust.Cdn.
2. CDN сообщает Watchdog, что вышел новый билд.
3. Watchdog скачивает сервер и перезапускает его в конце раунда.
4. Лаунчер игроков качает клиент с того же CDN.

## 1. Свой репозиторий

Код открыт (AGPL-3.0 и MIT), поэтому его можно перенести к себе с полной историей. Лицензии, `SPDX`-заголовки и авторство в коммитах трогать нельзя. AGPL требует, чтобы исходники сервера были доступны игрокам, поэтому репозиторий должен быть публичным, а ссылка на него должна стоять в `infolinks.github`.

1. Создай организацию на GitHub и в ней пустой репозиторий, например `Aquila`. Без README, лицензии и .gitignore, и **не** через кнопку Fork.
2. Перенеси историю и ветки:

```bash
git remote rename origin old-origin
git remote add origin https://github.com/<организация>/Aquila.git
git push -u origin --all
git push origin --tags
```

3. Когда всё на месте, старый remote можно убрать: `git remote remove old-origin`. Remote `upstream` (Goob) оставь для апстримов.
4. В настройках репозитория, в разделе Settings > Secrets and variables > Actions, заведи:
   - переменную `FORK_ID` = `aquila`;
   - переменную `ROBUST_CDN_URL` = `https://<домен CDN>/`;
   - секрет `PUBLISH_TOKEN`, он появится после установки сервера (шаг 4).
5. Для воркфлоу чейнджлогов и лейблеров нужны секрет `BOT_TOKEN` и переменные `CHANGELOG_*`. Если они не нужны, отключи эти воркфлоу в разделе Actions.

Старую базу игроков (баны, персонажи, время) переносить можно только с согласия старого владельца: это его данные и данные игроков.

## 2. Сервер в Timeweb Cloud

- Регион: Амстердам. Образ: Ubuntu 24.04.
- Конфигурация: лучше тариф с **выделенными** ядрами, а не с общими vCPU. Ориентир для 30-40 игроков: 4 ядра, 16 ГБ RAM, NVMe от 80 ГБ.
- В панели Timeweb, в разделе "Сетевые правила" (файрвол), открой входящие 22/tcp, 80/tcp, 443/tcp и 1212/udp. Скрипт дополнительно включит ufw с теми же правилами.
- Если у тарифа есть защита от DDoS, включи её. Важно, чтобы она фильтровала UDP, игра ходит по UDP 1212.

## 3. DNS

У регистратора домена заведи две A-записи на IP сервера:

- `aquila.example.ru` - сам сервер (статус для лаунчера и хаба);
- `cdn.aquila.example.ru` - CDN с билдами.

Подожди, пока записи начнут резолвиться (`ping aquila.example.ru`), иначе HTTPS-сертификат не выпустится. Cloudflare-прокси для CDN использовать нельзя, только DNS без оранжевого облачка.

## 4. Установка

На сервере:

```bash
git clone https://github.com/<организация>/Aquila.git ~/aquila
cd ~/aquila/Tools/_Aquila/Hosting
chmod +x install.sh backup-db.sh
sudo ./install.sh aquila.example.ru cdn.aquila.example.ru you@example.ru
```

Клонировать нужно именно на сервере. Если копировать папку с Windows, скрипты могут прийти с переводами строк CRLF и не запустятся.

Скрипт:
- ставит пакеты и .NET 10;
- создаёт пользователя `ss14` и базу `ss14`;
- собирает Robust.Cdn и SS14.Watchdog из исходников;
- раскладывает конфиги из `templates/`, подставляя домены и токены;
- поднимает systemd-сервисы, nginx и HTTPS через Let's Encrypt;
- включает файрвол и ежедневный бэкап базы в 05:30.

Токены и пароль базы генерируются один раз и лежат в `/root/aquila-secrets.env`. Значение `PUBLISH_TOKEN` оттуда положи в секрет `PUBLISH_TOKEN` на GitHub. Повторный запуск скрипта обновляет Watchdog и CDN и не меняет секреты.

## 5. Первый билд

На GitHub, в разделе Actions, выбери воркфлоу `Publish` и нажми "Run workflow". Дальше он запускается сам каждую ночь в 02:00 UTC. Когда билд дойдёт до CDN, Watchdog скачает его и запустит сервер. Сервер появится в хабе под названием из `game.hostname`.

## 6. Права хоста

Зайди на сервер один раз, чтобы аккаунт попал в базу. Потом выдай себе флаг хоста (поменяй ник):

```bash
sudo -u postgres psql ss14 <<'SQL'
INSERT INTO admin (user_id, title, deadminned, suspended)
SELECT user_id, 'Хост', false, false FROM player WHERE last_seen_user_name = 'CrookLv';
INSERT INTO admin_flag (flag, negative, admin_id)
SELECT 'HOST', false, user_id FROM player WHERE last_seen_user_name = 'CrookLv';
SQL
```

Перезайди. Остальных админов дальше удобнее назначать в игре через панель прав (`permissions`).

## 7. Вайтлист

Вайтлист включён в пресете `_Aquila/main`. Пока игрока нет в вайтлисте, он не зайдёт, а админы заходят всегда. Добавить игрока можно командой `whitelistadd <ник>` в админ-консоли, убрать командой `whitelistremove <ник>`. Самого себя добавлять не нужно, если у тебя уже есть права админа.

## Повседневное

| Что | Команда |
| --- | --- |
| Логи сервера и Watchdog | `journalctl -u ss14-watchdog -f` |
| Логи CDN | `journalctl -u robust-cdn -f` |
| Перезапуск всего | `sudo systemctl restart robust-cdn ss14-watchdog` |
| Логи раундов | `/opt/ss14/watchdog/instances/aquila/logs/` |
| Конфиг сервера | `/opt/ss14/watchdog/instances/aquila/config.toml` |
| Бэкапы базы | `/opt/ss14/backups/` |
| Ручной бэкап | `sudo -u postgres /usr/local/bin/aquila-backup-db` |
| Восстановление | `sudo -u postgres pg_restore --clean --if-exists -d ss14 /opt/ss14/backups/<файл>.dump` |

Перед восстановлением базы останови сервер: `sudo systemctl stop ss14-watchdog`.

Бэкапы лежат на том же сервере, поэтому при поломке VPS пропадут вместе с ним. Раз в неделю копируй их куда-нибудь ещё, например в S3-хранилище Timeweb или себе на компьютер через `scp`.

## Что поменять под себя

- `Resources/ConfigPresets/_Aquila/main.toml` - ссылки в лаунчере (Discord, GitHub, вики), теги хаба, вайтлист.
- `Resources/ConfigPresets/Corvax/common.toml` - название сервера, описание, лимиты игроков, режимы.
- `templates/config.toml` - то, что зависит от машины: база, адреса. Правки здесь применяются при следующем запуске `install.sh`. Если править файл прямо на сервере, повторный запуск скрипта его перезапишет.
