import { fileURLToPath, URL } from "node:url"
import { defineConfig } from "vitest/config"

export default defineConfig({
  resolve: {
    alias: { "@": fileURLToPath(new URL("./", import.meta.url)) },
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./tests/setup.ts"],
    coverage: {
      reporter: ["text", "json", "html"],
      exclude: ["lib/api/generated/**"],
    },
  },
})
