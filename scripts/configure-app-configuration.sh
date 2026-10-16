#!/usr/bin/env bash
set -euo pipefail

resource_group="${1:?Usage: configure-app-configuration.sh <resource-group> <environment>}"
environment="${2:?Usage: configure-app-configuration.sh <resource-group> <environment>}"
store_name="afc-appconfig-${environment}"

flags=(
  'trophy-screen:true'
  'lineup-notification:true'
  'player-ratings:false'
  'transfer-news:false'
  'search:false'
  'premium-tier:false'
)

for entry in "${flags[@]}"; do
  flag="${entry%%:*}"
  enabled="${entry##*:}"
  arguments=(
    --name "$store_name"
    --feature "$flag"
    --yes
    --auth-mode login
    --output none
  )
  az appconfig feature set \
    "${arguments[@]}"
  if [ "$enabled" = true ]; then
    az appconfig feature enable "${arguments[@]}"
  else
    az appconfig feature disable "${arguments[@]}"
  fi
done
