#!/bin/bash
#===============================================================================
#  NOIR Development Startup Script
#  Cross-platform: macOS, Linux, Windows (Git Bash/MSYS2/WSL)
#===============================================================================

set -e

#-------------------------------------------------------------------------------
# Configuration
#-------------------------------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FRONTEND_DIR="$SCRIPT_DIR/src/NOIR.Web/frontend"
BACKEND_DIR="$SCRIPT_DIR/src/NOIR.Web"
BACKEND_PORT=4000
FRONTEND_PORT=3000
HEALTH_TIMEOUT=60

#-------------------------------------------------------------------------------
# Colors (simple portable detection)
#-------------------------------------------------------------------------------
setup_colors() {
    # Default: no colors
    RED="" GREEN="" YELLOW="" BLUE="" CYAN="" WHITE="" DIM="" BOLD="" NC=""

    # Enable colors if terminal supports it
    if [[ -t 1 ]]; then
        # Check various color-capable environments
        if [[ -n "$MSYSTEM" ]] || [[ -n "$WT_SESSION" ]] || [[ -n "$TERM_PROGRAM" ]] || \
           [[ "$TERM" == *"color"* ]] || [[ "$TERM" == "xterm"* ]] || [[ "$TERM" == "screen"* ]]; then
            RED=$'\e[31m'
            GREEN=$'\e[32m'
            YELLOW=$'\e[33m'
            BLUE=$'\e[34m'
            CYAN=$'\e[36m'
            WHITE=$'\e[97m'
            DIM=$'\e[2m'
            BOLD=$'\e[1m'
            NC=$'\e[0m'
        fi
    fi
}

setup_colors

#-------------------------------------------------------------------------------
# OS Detection
#-------------------------------------------------------------------------------
detect_os() {
    case "$(uname -s)" in
        Linux*)
            if grep -qi microsoft /proc/version 2>/dev/null; then
                echo "wsl"
            else
                echo "linux"
            fi
            ;;
        Darwin*)  echo "macos" ;;
        CYGWIN*|MINGW*|MSYS*) echo "windows" ;;
        *) echo "unknown" ;;
    esac
}

OS_TYPE=$(detect_os)

#-------------------------------------------------------------------------------
# UI Functions (using printf for portability)
#-------------------------------------------------------------------------------
print_header() {
    printf "\n"
    printf "%s=========================================%s\n" "$CYAN" "$NC"
    printf "%s  NOIR Development Environment%s\n" "$WHITE$BOLD" "$NC"
    printf "%s  Cross-platform startup script%s\n" "$DIM" "$NC"
    printf "%s=========================================%s\n" "$CYAN" "$NC"
    printf "\n"
}

print_step() {
    printf "%s=> %s%s\n" "$CYAN" "$1" "$NC"
}

print_ok() {
    printf "   %s[OK]%s %s\n" "$GREEN" "$NC" "$1"
}

print_warn() {
    printf "   %s[!]%s %s\n" "$YELLOW" "$NC" "$1"
}

print_error() {
    printf "   %s[X]%s %s\n" "$RED" "$NC" "$1"
}

print_info() {
    printf "   %s%s%s\n" "$DIM" "$1" "$NC"
}

#-------------------------------------------------------------------------------
# Port Management
#-------------------------------------------------------------------------------
kill_port() {
    local port=$1
    local pids=""

    case "$OS_TYPE" in
        macos|linux|wsl)
            pids=$(lsof -ti :"$port" 2>/dev/null || true)
            ;;
        windows)
            pids=$(netstat -ano 2>/dev/null | grep ":$port " | grep "LISTEN" | awk '{print $5}' | sort -u || true)
            ;;
    esac

    if [[ -n "$pids" ]]; then
        print_warn "Port $port in use, killing..."
        for pid in $pids; do
            [[ -z "$pid" || "$pid" == "0" ]] && continue
            case "$OS_TYPE" in
                windows) taskkill //F //PID "$pid" >/dev/null 2>&1 || true ;;
                *) kill -9 "$pid" 2>/dev/null || true ;;
            esac
        done
        sleep 1
    fi
}

#-------------------------------------------------------------------------------
# Health Check
#-------------------------------------------------------------------------------
wait_for_backend() {
    local elapsed=0
    printf "   Waiting for backend"

    while [[ $elapsed -lt $HEALTH_TIMEOUT ]]; do
        if curl -sf "http://localhost:$BACKEND_PORT/robots.txt" >/dev/null 2>&1; then
            printf " %s[OK]%s\n" "$GREEN" "$NC"
            return 0
        fi
        printf "."
        sleep 1
        elapsed=$((elapsed + 1))
    done

    printf " %s[TIMEOUT]%s\n" "$RED" "$NC"
    return 1
}

#-------------------------------------------------------------------------------
# Process Management
#-------------------------------------------------------------------------------
BACKEND_PID=""
FRONTEND_PID=""

cleanup() {
    printf "\n%sShutting down...%s\n" "$YELLOW" "$NC"

    [[ -n "$FRONTEND_PID" ]] && kill "$FRONTEND_PID" 2>/dev/null || true
    [[ -n "$BACKEND_PID" ]] && kill "$BACKEND_PID" 2>/dev/null || true

    # Kill by port on Windows
    if [[ "$OS_TYPE" == "windows" ]]; then
        kill_port $BACKEND_PORT
        kill_port $FRONTEND_PORT
    fi

    printf "%sGoodbye!%s\n" "$GREEN" "$NC"
    exit 0
}

trap cleanup SIGINT SIGTERM EXIT

#-------------------------------------------------------------------------------
# Main Script
#-------------------------------------------------------------------------------
main() {
    print_header

    # Environment info
    print_step "Environment"
    print_info "OS: $OS_TYPE"
    printf "\n"

    # Prerequisites
    print_step "Prerequisites"
    command -v dotnet &>/dev/null || { print_error ".NET SDK not found"; exit 1; }
    print_ok ".NET $(dotnet --version)"

    command -v node &>/dev/null || { print_error "Node.js not found"; exit 1; }
    print_ok "Node $(node --version)"
    printf "\n"

    # Free ports
    print_step "Ports"
    kill_port $BACKEND_PORT
    kill_port $FRONTEND_PORT
    print_ok "Ports $BACKEND_PORT/$FRONTEND_PORT ready"
    printf "\n"

    # Frontend dependencies (skip if node_modules exists)
    print_step "Frontend"
    cd "$FRONTEND_DIR"
    if [[ ! -d "node_modules" ]]; then
        print_info "Installing dependencies..."
        npm install --silent >/dev/null 2>&1
        print_ok "Dependencies installed"
    else
        print_ok "Dependencies ready"
    fi
    printf "\n"

    # Clean Wolverine generated handlers (prevents stale handler errors)
    print_step "Cleaning generated code"
    GENERATED_DIR="$BACKEND_DIR/Internal/Generated"
    if [[ -d "$GENERATED_DIR" ]]; then
        rm -rf "$GENERATED_DIR"
        print_ok "Wolverine handlers cleaned"
    else
        print_ok "No generated code to clean"
    fi
    printf "\n"

    # Build backend
    print_step "Backend build"
    cd "$BACKEND_DIR"
    if ! dotnet build --nologo -v q -c Debug >"${SCRIPT_DIR}/.build.log" 2>&1; then
        print_error "Build failed!"
        grep -E "error [A-Z]+[0-9]+:" "${SCRIPT_DIR}/.build.log" 2>/dev/null | head -5
        exit 1
    fi
    print_ok "Build successful"
    printf "\n"

    # Start backend
    print_step "Starting backend"
    cd "$BACKEND_DIR"
    ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:$BACKEND_PORT" dotnet run --no-build --no-launch-profile >"${SCRIPT_DIR}/.backend.log" 2>&1 &
    BACKEND_PID=$!

    if ! wait_for_backend; then
        print_error "Backend failed to start"
        print_info "Check .backend.log"
        exit 1
    fi
    printf "\n"

    # Start frontend
    print_step "Starting frontend"
    cd "$FRONTEND_DIR"
    npm run dev >"${SCRIPT_DIR}/.frontend.log" 2>&1 &
    FRONTEND_PID=$!
    sleep 2
    print_ok "Frontend started"
    printf "\n"

    # Success
    printf "%s=========================================%s\n" "$GREEN" "$NC"
    printf "%s  NOIR is running!%s\n" "$WHITE$BOLD" "$NC"
    printf "%s=========================================%s\n" "$GREEN" "$NC"
    printf "\n"
    printf "   Frontend:  %shttp://localhost:%s%s\n" "$CYAN" "$FRONTEND_PORT" "$NC"
    printf "   Backend:   %shttp://localhost:%s%s\n" "$CYAN" "$BACKEND_PORT" "$NC"
    printf "   API Docs:  %shttp://localhost:%s/api/docs%s\n" "$CYAN" "$BACKEND_PORT" "$NC"
    printf "\n"
    printf "   Platform Admin: %splatform@noir.local%s / %s123qwe%s\n" "$WHITE" "$NC" "$WHITE" "$NC"
    printf "   Tenant Admin:   %sadmin@noir.local%s / %s123qwe%s\n" "$WHITE" "$NC" "$WHITE" "$NC"
    printf "\n"
    printf "%s   Press Ctrl+C to stop%s\n" "$YELLOW" "$NC"
    printf "\n"

    # Open browser
    case "$OS_TYPE" in
        macos) open "http://localhost:$FRONTEND_PORT" 2>/dev/null || true ;;
        linux) xdg-open "http://localhost:$FRONTEND_PORT" 2>/dev/null || true ;;
        windows) start "http://localhost:$FRONTEND_PORT" 2>/dev/null || true ;;
    esac

    # Wait
    wait $BACKEND_PID $FRONTEND_PID 2>/dev/null || true
}

main "$@"
