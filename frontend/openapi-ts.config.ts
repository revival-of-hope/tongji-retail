const config = {
  input: "../backend/openapi/retail-system.json",
  output: "lib/api/generated",
  plugins: ["@hey-api/client-fetch", "@hey-api/typescript", "@hey-api/sdk"],
}


export default config
