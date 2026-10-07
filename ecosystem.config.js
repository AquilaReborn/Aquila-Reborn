module.exports = {
  apps: [{
    name: "aquila",
    script: "./Content.Goobstation.Server",
    interpreter: "none",
    cwd: "/root/Kakila-Station/bin/Content.Server",
    env: {
      DOTNET_TieredPGO: "1",
      DOTNET_SYSTEM_NET_DISABLEIPV6: "1",
      DOTNET_gcServer: "1",
      ROBUST_NUMERICS_AVX: "true"
    }
  }]
}
