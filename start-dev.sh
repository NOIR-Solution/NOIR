#!/bin/bash
# NOIR Development Startup Script (macOS/Linux)
# Starts both backend (.NET) and frontend (React/Vite)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FRONTEND_DIR="$SCRIPT_DIR/src/NOIR.Web/frontend"
BACKEND_DIR="$SCRIPT_DIR/src/NOIR.Web"

echo "=========================================="
echo "  NOIR Development Environment Startup"
echo "=========================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to check if port is in use
check_port() {
    if lsof -ti :$1 > /dev/null 2>&1; then
        echo -e "${YELLOW}Port $1 is already in use. Killing existing process...${NC}"
        lsof -ti :$1 | xargs kill -9 2>/dev/null || true
        sleep 1
    fi
}

# Check and free ports
echo ""
echo "Checking ports..."
check_port 4000
check_port 3000

# Install frontend dependencies
echo ""
echo -e "${GREEN}Installing frontend dependencies...${NC}"
cd "$FRONTEND_DIR"
npm install

# Start backend in background
echo ""
echo -e "${GREEN}Starting backend on port 4000...${NC}"
cd "$BACKEND_DIR"
dotnet run --no-build &
BACKEND_PID=$!

# Wait for backend to be ready
echo "Waiting for backend to start..."
for i in {1..30}; do
    if curl -s http://localhost:4000/ > /dev/null 2>&1; then
        echo -e "${GREEN}Backend is ready!${NC}"
        break
    fi
    sleep 1
done

# Start frontend
echo ""
echo -e "${GREEN}Starting frontend on port 3000...${NC}"
cd "$FRONTEND_DIR"
npm run dev &
FRONTEND_PID=$!

# Wait for frontend to be ready
sleep 3

echo ""
echo "=========================================="
echo -e "${GREEN}  NOIR is running!${NC}"
echo "=========================================="
echo ""
echo "  Frontend: http://localhost:3000"
echo "  Backend:  http://localhost:4000"
echo ""
echo "  Login: admin@noir.local / 123qwe"
echo ""
echo "  Press Ctrl+C to stop all services"
echo "=========================================="

# Handle Ctrl+C to cleanup
trap "echo ''; echo 'Shutting down...'; kill $BACKEND_PID $FRONTEND_PID 2>/dev/null; exit 0" SIGINT SIGTERM

# Wait for processes
wait
