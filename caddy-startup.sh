#!/bin/sh
echo "=== Caddy startup script starting ==="
echo "Version: 1.1 - Fixed template processing"

echo "Current user: $(whoami)"
echo "Current directory: $(pwd)"
echo "PATH: $PATH"

# Test basic commands
echo "Testing basic commands..."
which caddy || echo "caddy not found in PATH"
caddy version || echo "caddy version failed"

echo "Environment variables:"
env

# Get domain from environment or use default
CADDY_DOMAIN="${CADDY_DOMAIN:-localhost}"
echo "Using domain: $CADDY_DOMAIN"

# Process Caddyfile template
echo "Processing Caddyfile template..."
if [ -f "/etc/caddy/Caddyfile.template" ]; then
    echo "Template found, processing with domain: $CADDY_DOMAIN"
    sed "s/__CADDY_DOMAIN__/$CADDY_DOMAIN/g" /etc/caddy/Caddyfile.template > /etc/caddy/Caddyfile
    echo "Caddyfile generated from template. Contents:"
    cat /etc/caddy/Caddyfile
elif [ -f "/etc/caddy/Caddyfile" ]; then
    echo "Pre-built Caddyfile found. Contents:"
    cat /etc/caddy/Caddyfile
else
    echo "No template or Caddyfile found, creating minimal fallback..."
    cat > /etc/caddy/Caddyfile << EOF
:80 {
    reverse_proxy aspnet-backend:8080 {
        header_up Host {host}
        header_up X-Real-IP {remote_host}
        header_up X-Forwarded-For {remote_host}
        header_up X-Forwarded-Proto {scheme}
        header_up X-Forwarded-Host {host}
    }
    encode gzip zstd
}
EOF
    echo "Fallback Caddyfile created. Contents:"
    cat /etc/caddy/Caddyfile
fi

echo "Testing Caddy configuration..."
if caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile; then
    echo "✅ Caddy configuration is valid"
else
    echo "❌ Caddy configuration validation failed"
    echo "Configuration contents:"
    cat /etc/caddy/Caddyfile
    exit 1
fi

echo "Starting Caddy..."
exec caddy run --config /etc/caddy/Caddyfile --adapter caddyfile