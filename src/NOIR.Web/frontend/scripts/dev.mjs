#!/usr/bin/env node
/**
 * Cross-platform development server orchestration script.
 * Starts backend, waits for it to be ready, generates API types, then starts frontend.
 *
 * Usage:
 *   node scripts/dev.mjs
 *   npm run dev:full
 */

import { spawn } from "node:child_process"
import { resolve, dirname } from "node:path"
import { fileURLToPath } from "node:url"

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)
const frontendDir = resolve(__dirname, "..")
const webProjectDir = resolve(frontendDir, "..")
const solutionDir = resolve(webProjectDir, "../..")

const BACKEND_URL = "http://localhost:5000"
const HEALTH_CHECK_URL = `${BACKEND_URL}/api/health`
const OPENAPI_URL = `${BACKEND_URL}/api/openapi/v1.json`

const isWindows = process.platform === "win32"

// Colors for console output
const colors = {
  reset: "\x1b[0m",
  bright: "\x1b[1m",
  dim: "\x1b[2m",
  red: "\x1b[31m",
  green: "\x1b[32m",
  yellow: "\x1b[33m",
  blue: "\x1b[34m",
  magenta: "\x1b[35m",
  cyan: "\x1b[36m",
}

function log(prefix, color, message) {
  const timestamp = new Date().toLocaleTimeString()
  console.log(`${colors.dim}[${timestamp}]${colors.reset} ${color}[${prefix}]${colors.reset} ${message}`)
}

function logBackend(message) {
  log("BE", colors.magenta, message)
}

function logFrontend(message) {
  log("FE", colors.cyan, message)
}

function logSystem(message) {
  log("SYS", colors.yellow, message)
}

function logError(message) {
  log("ERR", colors.red, message)
}

function logSuccess(message) {
  log("OK", colors.green, message)
}

async function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms))
}

async function waitForBackend(maxAttempts = 60, intervalMs = 1000) {
  logSystem(`Waiting for backend at ${BACKEND_URL}...`)

  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    try {
      const response = await fetch(HEALTH_CHECK_URL)
      if (response.ok) {
        logSuccess(`Backend is ready (attempt ${attempt}/${maxAttempts})`)
        return true
      }
    } catch {
      // Backend not ready yet
    }

    if (attempt < maxAttempts) {
      process.stdout.write(`\r${colors.dim}Waiting... (${attempt}/${maxAttempts})${colors.reset}`)
      await sleep(intervalMs)
    }
  }

  process.stdout.write("\n")
  logError(`Backend failed to start after ${maxAttempts} attempts`)
  return false
}

async function generateApiTypes() {
  logSystem("Generating API types from backend...")

  return new Promise((resolve, reject) => {
    const cmd = isWindows ? "npx.cmd" : "npx"
    const outputFile = "src/types/api.generated.ts"

    const child = spawn(cmd, ["openapi-typescript", OPENAPI_URL, "-o", outputFile], {
      cwd: frontendDir,
      stdio: ["inherit", "pipe", "pipe"],
      shell: isWindows,
    })

    let stdout = ""
    let stderr = ""

    child.stdout?.on("data", (data) => {
      stdout += data.toString()
    })

    child.stderr?.on("data", (data) => {
      stderr += data.toString()
    })

    child.on("close", (code) => {
      if (code === 0) {
        logSuccess(`API types generated: ${outputFile}`)
        resolve()
      } else {
        logError(`Failed to generate API types: ${stderr || stdout}`)
        reject(new Error(`openapi-typescript failed with code ${code}`))
      }
    })

    child.on("error", (err) => {
      logError(`Failed to spawn openapi-typescript: ${err.message}`)
      reject(err)
    })
  })
}

function startBackend() {
  logBackend("Starting .NET backend...")

  const child = spawn("dotnet", ["run", "--project", "src/NOIR.Web"], {
    cwd: solutionDir,
    stdio: ["inherit", "pipe", "pipe"],
    shell: isWindows,
  })

  child.stdout?.on("data", (data) => {
    const lines = data.toString().trim().split("\n")
    lines.forEach((line) => {
      if (line.trim()) {
        logBackend(line)
      }
    })
  })

  child.stderr?.on("data", (data) => {
    const lines = data.toString().trim().split("\n")
    lines.forEach((line) => {
      if (line.trim()) {
        logBackend(`${colors.red}${line}${colors.reset}`)
      }
    })
  })

  child.on("error", (err) => {
    logError(`Backend process error: ${err.message}`)
  })

  child.on("close", (code) => {
    if (code !== 0 && code !== null) {
      logError(`Backend exited with code ${code}`)
    }
  })

  return child
}

function startFrontend() {
  logFrontend("Starting Vite dev server...")

  const cmd = isWindows ? "npm.cmd" : "npm"

  const child = spawn(cmd, ["run", "dev"], {
    cwd: frontendDir,
    stdio: ["inherit", "pipe", "pipe"],
    shell: isWindows,
  })

  child.stdout?.on("data", (data) => {
    const lines = data.toString().trim().split("\n")
    lines.forEach((line) => {
      if (line.trim()) {
        logFrontend(line)
      }
    })
  })

  child.stderr?.on("data", (data) => {
    const lines = data.toString().trim().split("\n")
    lines.forEach((line) => {
      if (line.trim()) {
        logFrontend(line)
      }
    })
  })

  child.on("error", (err) => {
    logError(`Frontend process error: ${err.message}`)
  })

  child.on("close", (code) => {
    if (code !== 0 && code !== null) {
      logError(`Frontend exited with code ${code}`)
    }
  })

  return child
}

async function main() {
  console.log(`
${colors.bright}${colors.cyan}╔═══════════════════════════════════════════════════════════╗
║                    NOIR Dev Server                        ║
╚═══════════════════════════════════════════════════════════╝${colors.reset}
`)

  logSystem("Starting development environment...")
  logSystem(`Backend URL: ${BACKEND_URL}`)
  logSystem(`Frontend URL: http://localhost:5173`)
  console.log()

  // Track child processes for cleanup
  const processes = []

  // Handle graceful shutdown
  const cleanup = () => {
    console.log()
    logSystem("Shutting down...")
    processes.forEach((proc) => {
      if (!proc.killed) {
        proc.kill()
      }
    })
    process.exit(0)
  }

  process.on("SIGINT", cleanup)
  process.on("SIGTERM", cleanup)

  try {
    // 1. Start backend
    const backendProcess = startBackend()
    processes.push(backendProcess)

    // 2. Wait for backend to be ready
    const backendReady = await waitForBackend()
    if (!backendReady) {
      logError("Cannot continue without backend. Exiting...")
      cleanup()
      return
    }

    console.log()

    // 3. Generate API types
    try {
      await generateApiTypes()
    } catch (err) {
      logError(`API type generation failed: ${err.message}`)
      logSystem("Continuing without updated types...")
    }

    console.log()

    // 4. Start frontend
    const frontendProcess = startFrontend()
    processes.push(frontendProcess)

    console.log()
    logSuccess("Development environment is ready!")
    console.log()
    console.log(`  ${colors.bright}Backend:${colors.reset}  ${BACKEND_URL}`)
    console.log(`  ${colors.bright}Frontend:${colors.reset} http://localhost:5173`)
    console.log(`  ${colors.bright}API Docs:${colors.reset} ${BACKEND_URL}/api/docs`)
    console.log()
    console.log(`${colors.dim}Press Ctrl+C to stop all servers${colors.reset}`)
    console.log()
  } catch (err) {
    logError(`Failed to start development environment: ${err.message}`)
    cleanup()
  }
}

main()
