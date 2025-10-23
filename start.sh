#!/bin/bash

# Start script for Freelance Music Platform
# Starts backend API and frontend static server

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to print messages
print_error() {
    echo -e "${RED}Error: $1${NC}" >&2
}

print_success() {
    echo -e "${GREEN}$1${NC}"
}

print_warning() {
    echo -e "${YELLOW}$1${NC}"
}

print_info() {
    echo -e "${CYAN}$1${NC}"
}

# Function to show help
show_help() {
    echo "Usage: ./start.sh [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help     Show this help message"
    echo "  --https        Use HTTPS for backend API (port 7107)"
    echo ""
    echo "The application will start on:"
    echo "  Backend API:  http://localhost:5271 (or https://localhost:7107 with --https)"
    echo "  Frontend:     http://localhost:8000"
    exit 0
}

# Parse command line arguments
USE_HTTPS=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            ;;
        --https)
            USE_HTTPS=true
            shift
            ;;
        *)
            print_error "Unknown option: $1"
            echo "Use -h or --help for usage information"
            exit 1
            ;;
    esac
done

# PIDs to track
BACKEND_PID=""
FRONTEND_PID=""

# Cleanup function
cleanup() {
    print_warning "\nShutting down servers..."
    
    if [[ -n "$BACKEND_PID" ]]; then
        kill $BACKEND_PID 2>/dev/null || true
    fi
    
    if [[ -n "$FRONTEND_PID" ]]; then
        kill $FRONTEND_PID 2>/dev/null || true
    fi
    
    # Kill any remaining processes on ports
    lsof -ti:5271 | xargs kill -9 2>/dev/null || true
    lsof -ti:7107 | xargs kill -9 2>/dev/null || true
    lsof -ti:8000 | xargs kill -9 2>/dev/null || true
    
    print_success "Servers stopped."
    exit 0
}

# Set up trap for cleanup
trap cleanup EXIT INT TERM

print_success "Starting Freelance Music Platform..."
echo ""

# Check if we're in the right directory
if [[ ! -f "backend/backend.csproj" ]]; then
    print_error "backend.csproj not found. Please run this script from the project root directory."
    exit 1
fi

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK is not installed or not in PATH."
    exit 1
fi

DOTNET_VERSION=$(dotnet --version 2>/dev/null)
print_info ".NET Version: $DOTNET_VERSION"

# Check if Python is installed
if ! command -v python3 &> /dev/null && ! command -v python &> /dev/null; then
    print_error "Python is not installed or not in PATH. Please install Python to serve the frontend."
    exit 1
fi

PYTHON_CMD="python3"
if ! command -v python3 &> /dev/null; then
    PYTHON_CMD="python"
fi

PYTHON_VERSION=$($PYTHON_CMD --version 2>&1)
print_info "Python Version: $PYTHON_VERSION"

echo ""

# Kill any existing processes on our ports
print_warning "Stopping any existing servers on ports 5271, 7107, and 8000..."
lsof -ti:5271 | xargs kill -9 2>/dev/null || true
lsof -ti:7107 | xargs kill -9 2>/dev/null || true
lsof -ti:8000 | xargs kill -9 2>/dev/null || true
sleep 1

# Start backend API
cd backend

if [[ "$USE_HTTPS" == true ]]; then
    print_warning "Starting Backend API with HTTPS on https://localhost:7107..."
    dotnet run --launch-profile https > /dev/null 2>&1 &
    BACKEND_PID=$!
else
    print_warning "Starting Backend API with HTTP on http://localhost:5271..."
    dotnet run --launch-profile http > /dev/null 2>&1 &
    BACKEND_PID=$!
fi

cd ..

# Wait for backend to start
sleep 3

# Start frontend static server
cd frontend

print_warning "Starting Frontend Server on http://localhost:8000..."
$PYTHON_CMD -m http.server 8000 > /dev/null 2>&1 &
FRONTEND_PID=$!

cd ..

# Wait a moment for servers to fully start
sleep 2

echo ""
print_success "=============================================="
print_success "Application started successfully!"
print_success "=============================================="
echo ""
print_info "Frontend: http://localhost:8000"
if [[ "$USE_HTTPS" == true ]]; then
    print_info "Backend API: https://localhost:7107/api"
else
    print_info "Backend API: http://localhost:5271/api"
fi
echo ""
print_warning "Press Ctrl+C to stop all servers"
echo ""

# Keep script running
while true; do
    sleep 1
done
