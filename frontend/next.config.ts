import type { NextConfig } from "next"

const nextConfig: NextConfig = {
  output: "standalone",
  experimental: {
    cpus: 1,
  },
}

export default nextConfig
