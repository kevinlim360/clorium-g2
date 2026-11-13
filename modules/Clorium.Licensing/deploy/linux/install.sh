#!/bin/bash
# Installs the uploaded release and restores the company data once. Prints no secret contents.
set -euo pipefail
up=/tmp/clorium-upload
rel=/opt/clorium-license/releases/$(date -u +%Y%m%d-%H%M%S)
data=/var/lib/clorium-license/data

sudo install -d -o root -g root -m 755 /opt/clorium-license/releases "$rel"
sudo cp "$up"/app/* "$rel"/
sudo chown -R root:root "$rel"; sudo chmod 644 "$rel"/*
sudo ln -sfn "$rel" /opt/clorium-license/current

if sudo test -e "$data/settings.json"; then
  echo "data already restored - not overwriting"
else
  sudo test ! -e "$data" || [ -z "$(sudo ls -A "$data")" ] || { echo "data folder not empty - stop"; exit 1; }
  sudo install -d -o clorium-license -g clorium-license -m 700 "$data"
  sudo python3 -c "import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])" "$up/restore.zip" "$data"
  sudo chown -R clorium-license:clorium-license "$data"; sudo find "$data" -type f -exec chmod 600 {} +
  echo "restored: $(sudo ls "$data" | tr '\n' ' ')"
fi
sudo shred -u "$up/restore.zip"; rm -rf "$up"

sudo install -m 644 -o root -g root /tmp/clorium-license.service /etc/systemd/system/clorium-license.service; rm -f /tmp/clorium-license.service
sudo systemctl daemon-reload
sudo systemctl enable clorium-license >/dev/null 2>&1
sudo systemctl restart clorium-license
for i in $(seq 1 30); do curl -fsS http://127.0.0.1:5088/health >/dev/null 2>&1 && break; sleep 1; done
echo "service: $(systemctl is-active clorium-license)"
echo "local health: $(curl -sS http://127.0.0.1:5088/health)"
sudo ls -la "$data"
sudo journalctl -u clorium-license --no-pager -n 15 -o cat | grep -viE 'token|key' || true
