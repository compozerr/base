FROM node:18 AS builder

WORKDIR /app
COPY . .

# Install dependencies
RUN npm ci

# Build the frontend
RUN npm run build

# Stage 2: Production stage
FROM node:18-alpine

WORKDIR /app

# Install serve globally for serving static content with SPA support
RUN npm install -g serve

# Copy built files from builder stage
COPY --from=builder /app/build ./build

EXPOSE 1236

# Use serve with SPA mode (-s flag) to handle client-side routing
CMD ["serve", "-s", "build", "-l", "1236"]