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

# Function for cleanup that ensures completion
cleanup() {
    # First echo to ensure this function is being called
    echo "Starting cleanup process..."
    
    # Execute end commands with explicit feedback
    find /app/modules -type f -name "compozerr.json" | sort -r | while read -r file; do
        module_dir=$(dirname "$file")
        if [ -f "$module_dir/compozerr.json" ]; then
            end_cmd=$(jq -r '.end' "$module_dir/compozerr.json")
            if [ "$end_cmd" != "null" ] && [ -n "$end_cmd" ]; then
                echo "Executing end command in $module_dir: $end_cmd"
                cd "$module_dir"
                # Run command and wait for completion
                eval "$end_cmd"
                echo "Finished end command in $module_dir"
                cd - || true
            fi
        fi
    done
    
    echo "Cleanup complete, exiting..."
    # Force exit to ensure we don't hang
    kill -9 $$
}

# Set up traps for multiple signals
trap cleanup SIGTERM SIGINT SIGQUIT EXIT

# Store our PID
echo $$ > /tmp/module-runner.pid

# Find and process all compozerr.json files
find /app/modules -type f -name "compozerr.json" | while read -r file; do
    module_dir=$(dirname "$file")
    process_module "$module_dir"
done

echo "All modules started, waiting for signals..."

# Wait loop that checks if we're being shut down
while true; do
    sleep 1
done