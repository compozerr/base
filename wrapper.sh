#!/bin/sh

# Start the module runner in the background
/app/module-runner.sh &
RUNNER_PID=$!

# Function to handle shutdown
shutdown() {
    echo "Wrapper received shutdown signal, starting cleanup..."
    
    # Find and execute end commands
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
    
    # Kill the module runner if it's still running
    if kill -0 $RUNNER_PID 2>/dev/null; then
        kill $RUNNER_PID
    fi
    
    echo "Cleanup complete"
    exit 0
}

# Set up signal handlers
trap shutdown SIGTERM SIGINT SIGQUIT

# Wait for the module runner
wait $RUNNER_PID