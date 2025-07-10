#!/bin/sh
echo "=== Caddy startup script starting ==="

# Try to output to both stdout and stderr
exec 1>&2

echo "Current user: $(whoami)"
echo "Current directory: $(pwd)"
echo "PATH: $PATH"

# Test basic commands
echo "Testing basic commands..."
which caddy || echo "caddy not found in PATH"
caddy version || echo "caddy version failed"

echo "Environment variables:"
env | sort

# Simple test - just try to run caddy help
echo "Testing caddy help..."
caddy help || echo "caddy help failed"

echo "Creating minimal Caddyfile for testing..."
cat > /etc/caddy/Caddyfile << 'EOF'
:80 {
    respond "Hello from Caddy!"
}
EOF

echo "Caddyfile created. Contents:"
cat /etc/caddy/Caddyfile

echo "Testing Caddy configuration..."
caddy validate --config /etc/caddy/Caddyfile || echo "Caddy config validation failed"

echo "Starting Caddy..."
exec caddy run --config /etc/caddy/Caddyfile