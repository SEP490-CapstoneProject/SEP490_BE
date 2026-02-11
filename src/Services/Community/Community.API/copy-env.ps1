# Copy .env.local to publish directory for Docker
Copy-Item -Path ".env.local" -Destination ".\publish\.env.local" -Force
