#!/usr/bin/env bash
# Start both backend and frontend for local development.
# Usage: ./dev.sh
#   Backend: http://localhost:8080
#   Frontend: http://localhost:5175 (proxies /api to backend)

set -e

ROOT="$(cd "$(dirname "$0")" && pwd)"

cleanup() {
  echo "Shutting down..."
  kill $BACKEND_PID $FRONTEND_PID 2>/dev/null
  wait $BACKEND_PID $FRONTEND_PID 2>/dev/null
}
trap cleanup EXIT

# Start backend
echo "Starting backend on http://localhost:8080..."
cd "$ROOT/backend/src/ConflictedLineup.Api"
dotnet run &
BACKEND_PID=$!

# Start frontend
echo "Starting frontend on http://localhost:5175..."
cd "$ROOT/frontend"
npm run dev &
FRONTEND_PID=$!

echo ""
echo "Both servers running. Press Ctrl+C to stop."
wait
