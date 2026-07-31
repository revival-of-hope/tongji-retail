import { defineConfig } from "@hey-api/openapi-ts"

export default defineConfig({
  input: "../backend/openapi/retail-system.json",
  output: "lib/api/generated",
  plugins: ["@hey-api/client-fetch", "@hey-api/typescript", "@hey-api/sdk"],
})
