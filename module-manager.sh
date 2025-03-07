#!/bin/bash

# Config
MODULES_DIR="/app/modules"
COMPOSE_OUTPUT="docker-compose.generated.yml"
LOG_FILE="/app/module-manager.log"

# Enable logging
exec > >(tee -a "$LOG_FILE") 2>&1
echo "================ $(date) ================"
echo "Simplified module manager starting"

function generate_compose_file() {
    echo "Generating docker-compose file..."
    
    # Start with a base template
    cat > "$COMPOSE_OUTPUT" << EOF
version: '3'

services:
  frontend:
    build:
      context: .
      dockerfile: Dockerfile.frontend
    restart: always
    ports:
      - "1234:5173"
    networks:
      - app-network

  backend:
    build:
      context: .
      dockerfile: Dockerfile.backend
    restart: always
    ports:
      - "1235:5000"
    networks:
      - app-network

  module-runner:
    build:
      context: .
      dockerfile: Dockerfile.modules
    restart: always
    volumes:
      - ./modules:/app/modules
      - /var/run/docker.sock:/var/run/docker.sock
    networks:
      - app-network
    stop_grace_period: 20s
EOF
    
    # Find all modules with ports defined
    FOUND_MODULES=false

    # Check if any modules have ports
    while IFS= read -r config_file; do
        port=$(jq -r ".port // \"\"" "$config_file")
        if [ -n "$port" ] && [ "$port" != "null" ]; then
            FOUND_MODULES=true
            break
        fi
    done < <(find "$MODULES_DIR" -type f -name "compozerr.json")

    # Only add ports section if modules with ports were found
    if [ "$FOUND_MODULES" = true ]; then
        echo "    ports:" >> "$COMPOSE_OUTPUT"
        # Add port mappings from modules
        while IFS= read -r config_file; do
            module_dir=$(dirname "$config_file")
            module_name=$(basename "$module_dir")
            
            port=$(jq -r ".port // \"\"" "$config_file")
            if [ -n "$port" ] && [ "$port" != "null" ]; then
                echo "      - \"$port:$port\"  # Module: $module_name" >> "$COMPOSE_OUTPUT"
            fi
        done < <(find "$MODULES_DIR" -type f -name "compozerr.json")
    fi

    # Add the networks configuration
    cat >> "$COMPOSE_OUTPUT" << EOF

networks:
  app-network:
    driver: bridge
    external: true
EOF

    echo "Docker compose file generated at $COMPOSE_OUTPUT"
}

function process_modules() {
    echo "Processing modules..."
    
    # Find all modules with compozerr.json files
    find "$MODULES_DIR" -type f -name "compozerr.json" | while read -r config_file; do
        module_dir=$(dirname "$config_file")
        module_name=$(basename "$module_dir")
        
        echo "Processing module in: $module_dir"
        
        # Extract start command
        start_cmd=$(jq -r ".start" "$config_file")
        
        # Check for a valid start command
        if [ "$start_cmd" != "null" ] && [ -n "$start_cmd" ]; then
            echo "Executing start command: $start_cmd"
            
            # Run the start command in the module directory
            (cd "$module_dir" && eval "$start_cmd") &
        else
            echo "No start command found in $config_file"
        fi
    done
}

# Main execution
case "$1" in
    generate)
        generate_compose_file
        ;;
    start)
        process_modules
        ;;
    *)
        echo "Usage: $0 {generate|start}"
        exit 1
        ;;
esac

exit 0