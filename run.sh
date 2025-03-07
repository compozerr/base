#!/bin/bash

./module-manager.sh generate

docker-compose -f docker-compose.generated.yml up -d