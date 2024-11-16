# Stage 1: Install turbo globally and prune the frontend
FROM node:18 AS turbo-stage

# Install turbo globally
RUN npm install -g turbo

# Copy the project files
WORKDIR /app
COPY . .

# Prune the frontend for Docker
RUN turbo prune frontend --docker

# Copy compozerr.json files
RUN find ./modules -name "compozerr.json" -exec bash -c ' \
    for file; do \
        dest="./out/full/${file#./}"; \
        mkdir -p "$(dirname "$dest")"; \
        cp "$file" "$dest"; \
    done' _ {} +

# Stage 2: Build the frontend
FROM node:18 AS build-stage

# Copy the pruned files
WORKDIR /app
COPY --from=turbo-stage /app/out/full .

# Install dependencies and build the project
WORKDIR /app/frontend
RUN npm install
RUN npm run build

EXPOSE 4173

# Serve the application
CMD ["npm", "run", "serve", "--", "--host"]
