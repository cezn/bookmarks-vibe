#!/usr/bin/env bash

set -e

CONFIG_DEV="appsettings.Development.json"
CONFIG="appsettings.json"

KEY=$(jq -er '.Jwt.Key' "$CONFIG_DEV") || { echo "Missing .Jwt.Key in $CONFIG"; exit 1; }
AUD=$(jq -er '.Jwt.Audience' "$CONFIG") || { echo "Missing .Jwt.Audience in $CONFIG"; exit 1; }
ISS=$(jq -er '.Jwt.Issuer' "$CONFIG") || { echo "Missing .Jwt.Issuer in $CONFIG"; exit 1; }

if [[ -z "$KEY" || -z "$AUD" || -z "$ISS" ]]; then
  echo "Missing Jwt config in $CONFIG"
  exit 1
fi

SUB="${1:-570caa91-8a71-467c-9ff7-1e104d84b2f1}"

HEADER='{"alg":"HS256","typ":"JWT"}'
NOW=$(date +%s)
EXP=$((NOW + 3600 * 8))
PAYLOAD="{\"sub\":\"$SUB\",\"aud\":\"$AUD\",\"iss\":\"$ISS\",\"exp\":$EXP}"

b64enc() {
  echo -n "$1" | openssl base64 -A | tr '+/' '-_' | tr -d '='
}

HEADER_B64=$(b64enc "$HEADER")
PAYLOAD_B64=$(b64enc "$PAYLOAD")
SIGNING_INPUT="$HEADER_B64.$PAYLOAD_B64"

SIGNATURE=$(echo -n "$SIGNING_INPUT" | \
  openssl dgst -sha256 -hmac "$KEY" -binary | \
  openssl base64 -A | tr '+/' '-_' | tr -d '=')

echo "$SIGNING_INPUT.$SIGNATURE"
