# SurfWeb 构建发布入口（仓库根目录：.\Build\surf.ps1）
# 须 UTF-8 BOM，供 Windows PowerShell 5.1 正确解析中文。

param(
    [Parameter(Position = 0)]
    [ValidateSet('docker', 'host')]
    [string]$Action,
    [switch]$FullImage,
    [switch]$SkipBuild,
    [switch]$NonInteractive,
    [switch]$NoStartApi
)

$ErrorActionPreference = 'Stop'
$Root = Resolve-Path (Join-Path $PSScriptRoot '..')
$ComposeFile = Join-Path $PSScriptRoot 'docker/compose.yml'
$EnvExample = Join-Path $PSScriptRoot 'env.example'
$EnvFile = Join-Path $PSScriptRoot '.env'
$Interactive = -not $NonInteractive -and -not $PSBoundParameters.ContainsKey('Action')

Set-Location $Root

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw ('未找到 {0}，请先安装或加入 PATH。' -f $Name)
    }
}

function Invoke-Checked {
    param(
        [scriptblock]$Command,
        [string]$FailureMessage
    )
    & $Command
    if ($LASTEXITCODE -ne 0) {
        if ($FailureMessage) { throw $FailureMessage }
        throw ('命令失败，退出码 {0}' -f $LASTEXITCODE)
    }
}

function Read-Choice {
    param(
        [string]$Prompt,
        [string]$Default = '1'
    )
    $answer = Read-Host $Prompt
    if ([string]::IsNullOrWhiteSpace($answer)) { return $Default }
    return $answer.Trim()
}

function Select-DeployTarget {
    Write-Host ''
    Write-Host '=== SurfWeb 构建发布 ===' -ForegroundColor White
    Write-Host '  1  Docker  编译并启动容器（推荐，http://localhost:8080）'
    Write-Host '  2  宿主机  编译产物并启动本机 API（前端需 Nginx/IIS）'
    Write-Host ''
    $choice = Read-Choice '请选择 [1]'
    if ($choice -eq '2') { return 'host' }
    return 'docker'
}

function Select-DockerPlan {
    if ($FullImage) { return @{ Mode = 'full'; SkipBuild = $false } }
    if ($SkipBuild) { return @{ Mode = 'prebuilt'; SkipBuild = $true } }

    Write-Host ''
    Write-Host '  Docker 编译方式：'
    Write-Host '  1  标准  本机编译 + 运行时镜像（推荐）'
    Write-Host '  2  完整  容器内编译（需 sdk/node，首建慢）'
    Write-Host '  3  仅镜像  跳过编译（需已有 publish/api 与 Web/dist）'
    Write-Host ''
    switch (Read-Choice '请选择 [1]') {
        '2' { return @{ Mode = 'full'; SkipBuild = $false } }
        '3' { return @{ Mode = 'prebuilt'; SkipBuild = $true } }
        default { return @{ Mode = 'prebuilt'; SkipBuild = $false } }
    }
}

function Read-BuildEnv {
    $vars = @{}
    if (-not (Test-Path $EnvFile)) { return $vars }
    $utf8 = New-Object System.Text.UTF8Encoding $false
    foreach ($line in [System.IO.File]::ReadAllLines($EnvFile, $utf8)) {
        if ($line -match '^\s*#' -or [string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -match '^\s*([^=]+)=(.*)$') {
            $vars[$Matches[1].Trim()] = $Matches[2].Trim()
        }
    }
    return $vars
}

function Test-BuildEnvNeedsEdit {
    $v = Read-BuildEnv
    if ([string]::IsNullOrWhiteSpace($v['SHAVIT_CONNECTION_STRING'])) { return $true }
    $provider = if ($v['DATABASE_PROVIDER']) { $v['DATABASE_PROVIDER'].Trim() } else { 'MySql' }
    if ($provider -match '^(?i)sqlite$') { return $false }
    $content = Get-Content $EnvFile -Raw -ErrorAction SilentlyContinue
    return $content -match 'CHANGE_ME|YOUR_DB_HOST'
}

function Require-BuildEnvFile {
    if (-not (Test-Path $EnvFile)) {
        Write-Host ''
        Write-Host '未找到 Build/.env，正在从 env.example 自动创建…' -ForegroundColor Yellow
        Copy-Item $EnvExample $EnvFile
        Write-Host ('已创建 {0}' -f $EnvFile) -ForegroundColor Green
    }

    if (Test-BuildEnvNeedsEdit) {
        Write-Host '请填写 Build/.env（至少需要 SHAVIT_CONNECTION_STRING；MySql 需替换模板占位符）。' -ForegroundColor Yellow
        if ($Interactive) {
            Start-Process notepad $EnvFile
            Read-Host '编辑并保存后按 Enter 继续'
        }
        if (Test-BuildEnvNeedsEdit) {
            if ($Interactive) {
                throw 'Build/.env 仍为模板或未填写连接串，请先编辑 SHAVIT_CONNECTION_STRING。'
            }
            throw 'Build/.env 仍为模板或未填写连接串；非交互模式请先编辑。'
        }
    }
}

function Escape-SingleQuoted([string]$Value) {
    return $Value.Replace("'", "''")
}

function Get-ApiEnvCommandLines {
    $v = Read-BuildEnv
    $lines = @(
        '$env:ASPNETCORE_ENVIRONMENT = ''Production'''
        '$env:ASPNETCORE_URLS = ''http://127.0.0.1:5240'''
    )
    $map = @{
        'DATABASE_PROVIDER'                  = 'SurfWeb__Database__Provider'
        'SHAVIT_CONNECTION_STRING'           = 'ConnectionStrings__Shavit'
        'LATEST_RECORDS_TOKEN'               = 'SurfWeb__ExternalApi__LatestRecordsToken'
        'MAP_IMAGES_BASE_URL'                = 'SurfWeb__MapImages__BaseUrl'
        'MAP_IMAGES_EXTENSION'               = 'SurfWeb__MapImages__Extension'
        'SURF_SERVER_NAME'                   = 'SurfWeb__Servers__0__Name'
        'SURF_SERVER_ADDRESS'                = 'SurfWeb__Servers__0__Address'
        'SURF_SERVER_HOST'                   = 'SurfWeb__Servers__0__Host'
        'SURF_SERVER_PORT'                   = 'SurfWeb__Servers__0__Port'
        'SURF_SERVER_MAX_PLAYERS'            = 'SurfWeb__Servers__0__MaxPlayers'
    }
    foreach ($key in $map.Keys) {
        if ($v.ContainsKey($key) -and $v[$key]) {
            $escaped = Escape-SingleQuoted $v[$key]
            $lines += ('$env:{0} = ''{1}''' -f $map[$key], $escaped)
        }
    }
    return $lines
}

function Sync-WebProductionEnv {
    $v = Read-BuildEnv
    $title = if ($v['VITE_SITE_TITLE']) { $v['VITE_SITE_TITLE'] } else { '地满滑翔' }
    $path = Join-Path $Root 'Web/.env.production'
    $utf8 = New-Object System.Text.UTF8Encoding $false
    $content = @(
        'VITE_API_BASE_URL=/api/v1'
        ('VITE_SITE_TITLE={0}' -f $title)
    ) -join "`n"
    [System.IO.File]::WriteAllText($path, $content + "`n", $utf8)
    Write-Host '已根据 Build/.env 同步 Web/.env.production' -ForegroundColor DarkGray
}

function Publish-Api {
    Require-Command dotnet
    Write-Host '==> dotnet publish -> publish/api' -ForegroundColor Cyan
    Invoke-Checked { dotnet publish Server/SurfWeb.Api -c Release -o publish/api } 'dotnet publish 失败'
}

function Install-WebDependencies {
    if (Test-Path 'node_modules/.bin/vite.cmd') {
        Write-Host '==> 使用已有 node_modules' -ForegroundColor DarkGray
        return
    }
    if (Test-Path 'node_modules') {
        Write-Host '==> vite 未找到，尝试 npm install（若 EPERM 请先关闭 npm run dev）' -ForegroundColor Yellow
    }
    else {
        Write-Host '==> npm install（首次）' -ForegroundColor Cyan
    }
    npm install
    if ($LASTEXITCODE -ne 0) {
        throw 'npm install 失败；请关闭占用 Web/node_modules 的进程（如 npm run dev）后重试'
    }
}

function Build-Web {
    Require-Command npm
    Sync-WebProductionEnv
    Write-Host '==> npm run build -> Web/dist' -ForegroundColor Cyan
    Push-Location (Join-Path $Root 'Web')
    try {
        Install-WebDependencies
        Invoke-Checked { npm run build } 'npm run build 失败'
    }
    finally {
        Pop-Location
    }
    if (-not (Test-Path (Join-Path $Root 'Web/dist/index.html'))) {
        throw 'Web/dist 未生成，请检查 npm run build 输出'
    }
}

function Assert-BuildArtifacts {
    if (-not (Test-Path (Join-Path $Root 'publish/api/SurfWeb.Api.dll'))) {
        throw '缺少 publish/api，请选择标准模式或执行 .\Build\surf.ps1 host'
    }
    if (-not (Test-Path (Join-Path $Root 'Web/dist/index.html'))) {
        throw '缺少 Web/dist，请选择标准模式或执行 .\Build\surf.ps1 host'
    }
}

function Invoke-DockerCompose {
    param([string]$BuildMode)
    Require-Command docker
    $env:SURFWEB_BUILD_MODE = $BuildMode
    Write-Host ('==> docker compose up -d --build (mode={0})' -f $BuildMode) -ForegroundColor Cyan
    docker compose --project-directory $Root -f $ComposeFile --env-file $EnvFile up -d --build
    if ($LASTEXITCODE -ne 0) {
        Write-Host ''
        Write-Host 'Docker 构建失败。若拉取 nginx/node 或 mcr 镜像超时：' -ForegroundColor Yellow
        Write-Host '  1) 检查 Build/.env 中 NGINX_IMAGE / NODE_IMAGE（默认 DaoCloud 加速）'
        Write-Host '  2) Docker Desktop -> Settings -> Proxies -> http://127.0.0.1:7897 -> Apply & Restart'
        Write-Host '  3) 本机已编译可跳过: .\\Build\\surf.ps1 docker -SkipBuild（仍需能拉 nginx 基础镜像）'
        throw 'docker compose 失败'
    }
}

function Get-WebPort {
    $v = Read-BuildEnv
    if ($v['WEB_PORT']) { return $v['WEB_PORT'] }
    return '8080'
}

function Start-HostApi {
    $apiDir = Join-Path $Root 'publish/api'
    Write-Host '==> 启动 API（新窗口，环境来自 Build/.env）' -ForegroundColor Cyan
    $shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { 'pwsh' } else { 'powershell' }
    $envLines = (Get-ApiEnvCommandLines) -join '; '
    $cmd = @(
        "Set-Location '$apiDir'"
        $envLines
        "Write-Host 'API http://127.0.0.1:5240/health' -ForegroundColor Green"
        'dotnet SurfWeb.Api.dll'
    ) -join '; '
    Start-Process $shell -ArgumentList '-NoExit', '-Command', $cmd
}

function Invoke-DockerDeploy {
    param(
        [string]$Mode,
        [bool]$DoSkipBuild
    )
    if ($Mode -eq 'prebuilt' -and -not $DoSkipBuild) {
        Publish-Api
        Build-Web
    }
    elseif ($Mode -eq 'prebuilt') {
        Assert-BuildArtifacts
    }

    Invoke-DockerCompose -BuildMode $Mode

    $port = Get-WebPort
    Write-Host ''
    Write-Host '=== Docker 部署完成 ===' -ForegroundColor Green
    Write-Host ('站点: http://localhost:{0}' -f $port)
    Write-Host ('健康: http://localhost:{0}/health' -f $port)
    Write-Host '日志: docker compose --project-directory . -f Build/docker/compose.yml --env-file Build/.env logs -f api'
}

function Invoke-HostDeploy {
    Publish-Api
    Build-Web

    if (-not $NoStartApi) {
        Start-HostApi
    }

    Write-Host ''
    Write-Host '=== 宿主机构建完成 ===' -ForegroundColor Green
    Write-Host '产物: publish/api、Web/dist（配置来自 Build/.env）'
    if (-not $NoStartApi) {
        Write-Host 'API:  http://127.0.0.1:5240/health（已在新窗口启动）'
    }
    else {
        Write-Host '手动启动 API: 在 publish/api 目录执行 dotnet SurfWeb.Api.dll（需自行注入 Build/.env 变量）'
    }
    Write-Host '前端: 将 Web/dist 配到 Nginx，见 Build/host/nginx.example.conf'
    Write-Host '详情: doc/deploy.md'
}

if (-not $Action) {
    if ($NonInteractive) {
        $Action = 'docker'
    }
    else {
        $Action = Select-DeployTarget
    }
}

Require-BuildEnvFile

if ($Action -eq 'docker') {
    $plan = if ($Interactive) { Select-DockerPlan } else {
        @{
            Mode      = if ($FullImage) { 'full' } else { 'prebuilt' }
            SkipBuild = [bool]$SkipBuild
        }
    }
    Invoke-DockerDeploy -Mode $plan.Mode -DoSkipBuild $plan.SkipBuild
}
else {
    Invoke-HostDeploy
}
