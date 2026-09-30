#!/usr/bin/env bash
# Start both backend and frontend for local development.
# Usage: ./dev.sh          (real Claude + Spotify; needs backend/.env and frontend/.env.local)
#        ./dev.sh --demo   (fictional festival, no keys needed)
#   Backend: http://localhost:8080
#   Frontend: http://localhost:5175 (proxies /api to backend)

set -e

PROFILE=http
if [ "$1" = "--demo" ]; then PROFILE=demo; fi

ROOT="$(cd "$(dirname "$0")" && pwd)"

cleanup() {
  echo "Shutting down..."
  kill $BACKEND_PID $FRONTEND_PID 2>/dev/null
  wait $BACKEND_PID $FRONTEND_PID 2>/dev/null
}
trap cleanup EXIT

# Start backend
echo "Starting backend ($PROFILE) on http://localhost:8080..."
cd "$ROOT/backend/src/ConflictedLineup.Api"
dotnet run --launch-profile "$PROFILE" &
BACKEND_PID=$!

# Start frontend
echo "Starting frontend on http://localhost:5175..."
cd "$ROOT/frontend"
npm run dev &
FRONTEND_PID=$!

echo ""
echo "Both servers running. Press Ctrl+C to stop."
wait
