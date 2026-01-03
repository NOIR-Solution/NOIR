#!/usr/bin/env node
/**
 * Cross-platform script to generate TypeScript types from OpenAPI spec.
 * Works on Windows, macOS, and Linux.
 *
 * Usage:
 *   node scripts/generate-api-types.mjs [--from-url | --from-file]
 *   npm run generate:api
 */

import { spawn } from "node:child_process"
import { existsSync } from "node:fs"
import { resolve, dirname } from "node:path"
import { fileURLToPath } from "node:url"

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)
const frontendDir = resolve(__dirname, "..")
const outputFile = resolve(frontendDir, "src/types/api.generated.ts")

const BACKEND_URL = "http://localhost:4000/api/openapi/v1.json"
const OPENAPI_FILE = resolve(frontendDir, "../wwwroot/openapi.json")

function runCommand(command, args) {
  return new Promise((resolve, reject) => {
    const isWindows = process.platform === "win32"
    const cmd = isWindows ? `${command}.cmd` : command

    const child = spawn(cmd, args, {
      stdio: "inherit",
      shell: isWindows,
      cwd: frontendDir,
    })

    child.on("close", (code) => {
      if (code === 0) {
        resolve()
      } else {
        reject(new Error(`Command failed with exit code ${code}`))
      }
    })

    child.on("error", (err) => {
      reject(err)
    })
  })
}

async function generateFromUrl() {
  console.log(`Generating types from running backend at ${BACKEND_URL}...`)
  console.log("Make sure the backend is running: dotnet run --project src/NOIR.Web\n")

  await runCommand("npx", ["openapi-typescript", BACKEND_URL, "-o", outputFile])
}

async function generateFromFile() {
  if (!existsSync(OPENAPI_FILE)) {
    console.error(`Error: OpenAPI spec not found at ${OPENAPI_FILE}`)
    console.error("Run the backend first to generate it, or use --from-url with running backend")
    process.exit(1)
  }

  console.log(`Generating types from file: ${OPENAPI_FILE}\n`)
  await runCommand("npx", ["openapi-typescript", OPENAPI_FILE, "-o", outputFile])
}

function printUsage() {
  console.log(`
Usage: node scripts/generate-api-types.mjs [option]

Options:
  --from-url   Generate from running backend (default)
  --from-file  Generate from exported openapi.json file
  --help       Show this help message

Examples:
  npm run generate:api           # From running backend
  npm run generate:api:file      # From file
`)
}

async function main() {
  const args = process.argv.slice(2)
  const mode = args[0] || "--from-url"

  try {
    switch (mode) {
      case "--from-url":
        await generateFromUrl()
        break
      case "--from-file":
        await generateFromFile()
        break
      case "--help":
      case "-h":
        printUsage()
        process.exit(0)
        break
      default:
        console.error(`Unknown option: ${mode}`)
        printUsage()
        process.exit(1)
    }

    console.log(`\n✓ Generated: ${outputFile}`)
  } catch (error) {
    console.error(`\n✗ Failed to generate types: ${error.message}`)

    // Provide context-specific troubleshooting help
    const msg = error.message.toLowerCase()
    if (msg.includes("econnrefused") || msg.includes("fetch failed") || msg.includes("exit code 1")) {
      console.error("\nTroubleshooting:")
      console.error("  1. Ensure backend is running: dotnet run --project src/NOIR.Web")
      console.error(`  2. Verify the URL is accessible: ${BACKEND_URL}`)
      console.error("  3. Check if the port is correct (default: 5000)")
    } else if (msg.includes("enoent") || msg.includes("not found")) {
      console.error("\nTroubleshooting:")
      console.error("  1. Run: npm install")
      console.error("  2. Ensure openapi-typescript is in devDependencies")
    }

    process.exit(1)
  }
}

main()
