#!/usr/bin/env zsh
set -euo pipefail

BASE_URL="${1:-http://localhost:5001}"
TARGET_EMAIL="${2:-}"

if [[ -z "${TARGET_EMAIL}" ]]; then
  TARGET_EMAIL="sunildhawan007+otp$(date +%s)@gmail.com"
fi

PAYLOAD=$(python3 - <<PY
import json
print(json.dumps({
  "name": "OTP Smoke Test",
  "email": "${TARGET_EMAIL}",
  "password": "Otp@Test123!",
  "phoneNumber": "9876543210"
}))
PY
)

echo "Base URL: ${BASE_URL}"
echo "Test Email: ${TARGET_EMAIL}"

echo "\n[1] Register (sends OTP + verification link)"
REG_HTTP=$(curl -s -o /tmp/smoke_register.json -w "%{http_code}" \
  -X POST "${BASE_URL}/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "${PAYLOAD}")
echo "HTTP: ${REG_HTTP}"
cat /tmp/smoke_register.json; echo

echo "\n[2] Resend OTP"
RESEND_HTTP=$(curl -s -o /tmp/smoke_resend.json -w "%{http_code}" \
  -X POST "${BASE_URL}/api/auth/resend-otp" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"${TARGET_EMAIL}\",\"purpose\":\"EmailVerification\"}")
echo "HTTP: ${RESEND_HTTP}"
cat /tmp/smoke_resend.json; echo

echo "\nDone. Check inbox for OTP and verification link for: ${TARGET_EMAIL}"
