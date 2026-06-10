# Docker 部署

构建脚本、Compose、Dockerfile、环境变量模板与排错已集中在 **[`Build/README.md`](../Build/README.md)**。

**推荐入口（仓库根目录）：**

```powershell
.\Build\surf.ps1   # 首次会自动从 env.example 创建 Build/.env 并引导填写
.\Build\surf.ps1
```

宿主机 Nginx 部署（不用 Docker）见 [`doc/deploy.md`](deploy.md)。
