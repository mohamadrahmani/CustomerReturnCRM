#!/usr/bin/env bash
set -euo pipefail
REPO_URL="https://github.com/mohamadrahmani/CustomerReturnCRM.git"
APP_DIR="/opt/CustomerReturnCRM"
DEPLOY_USER="deploy"
if [ "$(id -u)" -ne 0 ]; then echo "Run this script as root."; exit 1; fi
apt-get update
apt-get install -y ca-certificates curl gnupg git nginx certbot python3-certbot-nginx
install -m 0755 -d /etc/apt/keyrings
if [ ! -f /etc/apt/keyrings/docker.asc ]; then
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
  chmod a+r /etc/apt/keyrings/docker.asc
fi
ARCH="$(dpkg --print-architecture)"
. /etc/os-release
cat > /etc/apt/sources.list.d/docker.list <<EOF
deb [arch=${ARCH} signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu ${VERSION_CODENAME} stable
EOF
apt-get update
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl enable --now docker
if ! id "$DEPLOY_USER" >/dev/null 2>&1; then adduser --disabled-password --gecos "" "$DEPLOY_USER"; fi
usermod -aG docker "$DEPLOY_USER"
mkdir -p /opt
if [ ! -d "$APP_DIR/.git" ]; then git clone "$REPO_URL" "$APP_DIR"; fi
chown -R "$DEPLOY_USER:$DEPLOY_USER" "$APP_DIR"
install -d -m 0755 /etc/nginx/sites-available /etc/nginx/sites-enabled
install -m 0644 "$APP_DIR/deploy/nginx/bemooni.ir.conf" /etc/nginx/sites-available/bemooni.ir
ln -sfn /etc/nginx/sites-available/bemooni.ir /etc/nginx/sites-enabled/bemooni.ir
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl enable --now nginx
systemctl reload nginx
echo "Bootstrap complete."
echo "Restore .env, configure the deploy user's GitHub SSH key, then run migrations and start the containers."
echo "After DNS points to this server, run: certbot --nginx -d bemooni.ir -d www.bemooni.ir"
