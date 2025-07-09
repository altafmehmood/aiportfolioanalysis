#!/bin/sh
set -e

echo "Caddy startup script starting..."

# Default values if environment variables are not set
LETS_ENCRYPT_EMAIL=${LETS_ENCRYPT_EMAIL:-"noreply@example.com"}
CADDY_DOMAIN=${CADDY_DOMAIN:-"localhost"}

# Handle empty environment variables (Azure Container Instances can pass empty strings)
if [ -z "$LETS_ENCRYPT_EMAIL" ]; then
    LETS_ENCRYPT_EMAIL="noreply@example.com"
fi

if [ -z "$CADDY_DOMAIN" ]; then
    CADDY_DOMAIN="localhost"
fi

echo "Building for test environment"
echo "Generated Caddyfile for domain: $CADDY_DOMAIN"

# Debug: Show environment variables
echo "Environment variables:"
echo "  LETS_ENCRYPT_EMAIL: $LETS_ENCRYPT_EMAIL"
echo "  CADDY_DOMAIN: $CADDY_DOMAIN"

# Check if template file exists
if [ ! -f /etc/caddy/Caddyfile.template ]; then
    echo "ERROR: Template file not found at /etc/caddy/Caddyfile.template"
    exit 1
fi

echo "Template file found, generating Caddyfile..."

# Generate Caddyfile from template with safer substitution
sed -e "s|__LETS_ENCRYPT_EMAIL__|$LETS_ENCRYPT_EMAIL|g" \
    -e "s|__CADDY_DOMAIN__|$CADDY_DOMAIN|g" \
    /etc/caddy/Caddyfile.template > /etc/caddy/Caddyfile

echo "Caddyfile generated successfully. Contents:"
cat /etc/caddy/Caddyfile

echo "Starting Caddy..."
# Start Caddy
exec caddy run --config /etc/caddy/Caddyfile --adapter caddyfile