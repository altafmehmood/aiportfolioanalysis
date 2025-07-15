#!/bin/sh
echo "=== Minimal Caddy startup script ==="
echo "Version: 2.0 - Minimal working configuration"

# Test basic commands
echo "Testing caddy command..."
caddy version || echo "caddy version failed"

# Use the minimal Caddyfile directly
echo "Using minimal Caddyfile configuration..."
if [ -f "/etc/caddy/Caddyfile.minimal" ]; then
    cp /etc/caddy/Caddyfile.minimal /etc/caddy/Caddyfile
    echo "Copied minimal Caddyfile"
else
    echo "Creating basic Caddyfile..."
    cat > /etc/caddy/Caddyfile << 'EOF'
:80 {
    reverse_proxy aspnet-backend:8080 {
        header_up Host {host}
        header_up X-Real-IP {remote_host}
        header_up X-Forwarded-For {remote_host}
        header_up X-Forwarded-Proto {scheme}
        header_up X-Forwarded-Host {host}
    }
    encode gzip zstd
    handle /health {
        respond "OK" 200
    }
    log {
        output stdout
        format console
        level INFO
    }
}
EOF
fi

echo "Final Caddyfile contents:"
cat /etc/caddy/Caddyfile

echo "Validating Caddy configuration..."
if caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile; then
    echo "✅ Caddy configuration is valid"
else
    echo "❌ Caddy configuration validation failed"
    exit 1
fi

echo "Starting Caddy..."
exec caddy run --config /etc/caddy/Caddyfile --adapter caddyfile