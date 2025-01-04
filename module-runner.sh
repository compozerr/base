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

# Find and process all compozerr.json files
find /app/modules -type f -name "compozerr.json" | while read -r file; do
    module_dir=$(dirname "$file")
    process_module "$module_dir"
done

# Handle shutdown gracefully
cleanup() {
    echo "Shutting down services..."
    
    # Execute end commands
    find /app/modules -type f -name "compozerr.json" | while read -r file; do
        module_dir=$(dirname "$file")
        if [ -f "$module_dir/compozerr.json" ]; then
            end_cmd=$(jq -r '.end' "$module_dir/compozerr.json")
            if [ "$end_cmd" != "null" ] && [ -n "$end_cmd" ]; then
                echo "Executing end command in $module_dir: $end_cmd"
                (cd "$module_dir" && eval "$end_cmd")
            fi
        fi
    done
}

# Set up trap for cleanup
trap cleanup SIGTERM SIGINT

# Keep container running
tail -f /dev/null