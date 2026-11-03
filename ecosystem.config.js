module.exports = {
  apps: [{
    name: "aquila",
    script: "./Content.Goobstation.Server",
    interpreter: "none",
    cwd: "/root/AquilaReborn/bin/Content.Server",
    env: {
      // настройки .NET
      DOTNET_TieredPGO: "1",
      DOTNET_TC_QuickJitForLoops: "1",
      DOTNET_ReadyToRun: "0",
      DOTNET_SYSTEM_NET_DISABLEIPV6: "1",
      DOTNET_gcServer: "1",
      ROBUST_NUMERICS_AVX: "true",
      
      // НАСТРОЙКИ ДЛЯ ПРОКСИ (WARP)
      HTTP_PROXY: "socks5://127.0.0.1:40000",
      HTTPS_PROXY: "socks5://127.0.0.1:40000",
      // ALL_PROXY: "socks5://127.0.0.1:40000"
      NO_PROXY: "localhost,127.0.0.1,::1"
    }
  }]
}
