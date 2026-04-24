#!/bin/sh
set -e

echo "Waiting for eKuiper REST API..."
until wget -qO- http://ekuiper:9081/rules >/dev/null 2>&1; do
  sleep 2
done

echo "Importing eKuiper ruleset..."
wget -qO- \
  --header="Content-Type: application/json" \
  --post-data='{"file":"file:///kuiper/etc/pm25-ruleset.json"}' \
  http://ekuiper:9081/ruleset/import

echo "eKuiper ruleset imported."