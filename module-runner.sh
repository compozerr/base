#!/bin/sh

# Function to process a single compozerr.json file
process_module() {
    local module_dir="$1"
    local compozerr_file="$module_dir/compozerr.json"
    
    if [ -f "$compozerr_file" ]; then
        echo "Processing module in: $module_dir"
        
        # Extract start command from compozerr.json
        start_cmd=$(jq -r '.start' "$compozerr_file")
        
        if [ "$start_cmd" != "null" ]; then
            echo "Executing start command: $start_cmd"
            cd "$module_dir" || exit 1
            eval "$start_cmd"
            cd - || exit 1
        else
            echo "No start command found in $compozerr_file"
        fi
    fi
}

# Function for cleanup
cleanup() {
    echo "Starting cleanup process..."
    
    # Execute end commands in reverse order
    find /app/modules -type f -name "compozerr.json" | sort -r | while read -r file; do
        module_dir=$(dirname "$file")
        if [ -f "$module_dir/compozerr.json" ]; then
            end_cmd=$(jq -r '.end' "$module_dir/compozerr.json")
            if [ "$end_cmd" != "null" ] && [ -n "$end_cmd" ]; then
                echo "Executing end command in $module_dir: $end_cmd"
                (cd "$module_dir" && eval "$end_cmd")
            fi
        fi
    done
    
    # Ensure we exit after cleanup
    exit 0
}

# Set up trap for cleanup before doing anything else
trap cleanup SIGTERM SIGINT SIGQUIT

# Find and process all compozerr.json files
find /app/modules -type f -name "compozerr.json" | while read -r file; do
    module_dir=$(dirname "$file")
    process_module "$module_dir"
done

# Wait for signals in a way that allows trap to work
while true; do
    sleep 1 & wait $!
done