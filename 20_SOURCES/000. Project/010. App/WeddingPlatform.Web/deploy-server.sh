#!/bin/bash
# ============================================================
# WeddingPlatform 서버 배포 스크립트 (Linux)
# 서버에서 실행: bash deploy-server.sh
# ============================================================

set -e

INSTALL_DIR="/opt/wedding"
DATA_DIR="/var/wedding-data"
SERVICE_NAME="wedding"
APP_USER="www-data"

echo "▶ WeddingPlatform 배포 시작..."

mkdir -p "$INSTALL_DIR"
mkdir -p "$DATA_DIR"
chown "$APP_USER":"$APP_USER" "$DATA_DIR"

# 1. 앱 압축 해제
if [ -f "wedding-deploy.zip" ]; then
    echo "▶ 앱 패키지 압축 해제..."
    unzip -o wedding-deploy.zip -d "$INSTALL_DIR"
    chmod +x "$INSTALL_DIR/WeddingPlatform.Web"
else
    echo "⚠ wedding-deploy.zip 없음 — publish.ps1 실행 후 업로드하세요"
fi

# 2. App_Data/Wedding → /var/wedding-data 자동 이전 (사진·config 포함)
APP_DATA_SRC="$INSTALL_DIR/App_Data/Wedding"
if [ -d "$APP_DATA_SRC" ]; then
    echo "▶ 테넌트 데이터 이전: $APP_DATA_SRC → $DATA_DIR"
    # 기존 데이터는 덮어쓰지 않음 (--ignore-existing)
    # 새 슬러그만 추가하고 기존 사진은 보존
    rsync -av --ignore-existing "$APP_DATA_SRC/" "$DATA_DIR/"
    chown -R "$APP_USER":"$APP_USER" "$DATA_DIR"
    echo "✅ 데이터 이전 완료"
else
    echo "⚠ App_Data/Wedding 없음 — 데이터 이전 건너뜀"
fi

# 3. appsettings.Production.json 생성 (없는 경우)
SETTINGS_FILE="$INSTALL_DIR/appsettings.Production.json"
if [ ! -f "$SETTINGS_FILE" ]; then
    echo "▶ appsettings.Production.json 생성..."
    cat > "$SETTINGS_FILE" << 'EOF'
{
  "WeddingServer": {
    "Port": 80,
    "ListenAnyIp": true
  },
  "Wedding": {
    "DataPath": "/var/wedding-data",
    "SuperAdminPassword": "여기를-강력한-비번으로-바꾸세요"
  }
}
EOF
    echo "⚠ $SETTINGS_FILE 의 SuperAdminPassword 를 반드시 변경하세요!"
fi

# 4. systemd 서비스 등록
cat > "/etc/systemd/system/${SERVICE_NAME}.service" << EOF
[Unit]
Description=Codemaru Wedding Platform
After=network.target

[Service]
Type=simple
User=$APP_USER
WorkingDirectory=$INSTALL_DIR
ExecStart=$INSTALL_DIR/WeddingPlatform.Web
Restart=on-failure
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
systemctl restart "$SERVICE_NAME"

echo ""
echo "✅ 배포 완료!"
echo "   서비스 상태: systemctl status $SERVICE_NAME"
echo "   로그 확인:  journalctl -u $SERVICE_NAME -f"
