#!/bin/sh
echo "=== Caddy startup script starting ==="

# Don't exit on errors initially - let's see what happens
set +e

echo "Current user: $(whoami)"
echo "Current directory: $(pwd)"
echo "Environment variables:"
env | grep -E "(LETS_ENCRYPT|CADDY)" || echo "No LETS_ENCRYPT or CADDY env vars found"

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
echo "Final environment variables:"
echo "  LETS_ENCRYPT_EMAIL: $LETS_ENCRYPT_EMAIL"
echo "  CADDY_DOMAIN: $CADDY_DOMAIN"

# Check if template file exists
echo "Checking for template file..."
if [ ! -f /etc/caddy/Caddyfile.template ]; then
    echo "ERROR: Template file not found at /etc/caddy/Caddyfile.template"
    echo "Files in /etc/caddy:"
    ls -la /etc/caddy/ || echo "Cannot list /etc/caddy"
    exit 1
fi

echo "Template file found, generating Caddyfile..."

# Generate Caddyfile from template with safer substitution
sed -e "s|__LETS_ENCRYPT_EMAIL__|$LETS_ENCRYPT_EMAIL|g" \
    -e "s|__CADDY_DOMAIN__|$CADDY_DOMAIN|g" \
    /etc/caddy/Caddyfile.template > /etc/caddy/Caddyfile

if [ $? -eq 0 ]; then
    echo "Caddyfile generated successfully. Contents:"
    cat /etc/caddy/Caddyfile
else
    echo "ERROR: Failed to generate Caddyfile"
    exit 1
fi

echo "Starting Caddy..."
# Now set -e for Caddy execution
set -e
exec caddy run --config /etc/caddy/Caddyfile --adapter caddyfile