#!/bin/sh
set -e

# Default values if environment variables are not set
LETS_ENCRYPT_EMAIL=${LETS_ENCRYPT_EMAIL:-"noreply@example.com"}
CADDY_DOMAIN=${CADDY_DOMAIN:-"localhost"}

echo "Building for test environment"
echo "Generated Caddyfile for domain: $CADDY_DOMAIN"

# Debug: Show environment variables
echo "Environment variables:"
echo "  LETS_ENCRYPT_EMAIL: $LETS_ENCRYPT_EMAIL"
echo "  CADDY_DOMAIN: $CADDY_DOMAIN"

# Generate Caddyfile from template
sed -e "s/__LETS_ENCRYPT_EMAIL__/$LETS_ENCRYPT_EMAIL/g" \
    -e "s/__CADDY_DOMAIN__/$CADDY_DOMAIN/g" \
    /etc/caddy/Caddyfile.template > /etc/caddy/Caddyfile

echo "Caddyfile contents:"
cat /etc/caddy/Caddyfile

# Start Caddy
exec caddy run --config /etc/caddy/Caddyfile --adapter caddyfile