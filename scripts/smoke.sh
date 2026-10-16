#!/usr/bin/env bash
set -euo pipefail

site_url="${1:?Usage: smoke.sh <site-url> <api-url>}"
api_url="${2:?Usage: smoke.sh <site-url> <api-url>}"

curl --fail --silent --show-error --retry 5 --retry-delay 5 "${api_url%/}/health" |
  grep --quiet '"status":"ok"'
curl --fail --silent --show-error --retry 5 --retry-delay 5 "${site_url%/}/" |
  grep --quiet 'id="app"'
