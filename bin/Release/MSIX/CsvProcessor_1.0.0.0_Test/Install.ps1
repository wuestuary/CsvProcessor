# 自我提升：申请管理员权限
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process powershell.exe -ArgumentList "-ExecutionPolicy Bypass -File `"$($MyInvocation.MyCommand.Path)`"" -Verb RunAs
    exit
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CsvProcessor 安装程序" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 获取脚本所在目录
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "安装目录: $scriptDir" -ForegroundColor Gray
Write-Host ""

# 列出目录内容（调试用）
Write-Host "目录内容：" -ForegroundColor Gray
Get-ChildItem | Format-Table Name, Extension -AutoSize
Write-Host ""

# 查找证书（任何 .cer 文件）
$certFiles = Get-ChildItem -Filter "*.cer"
if ($certFiles.Count -eq 0) {
    Write-Error "错误：找不到 .cer 证书文件！"
    Write-Host ""
    Write-Host "当前目录应有以下文件：" -ForegroundColor Yellow
    Write-Host "  - xxx.cer (证书文件)" -ForegroundColor White
    Write-Host "  - xxx.msix (安装包)" -ForegroundColor White
    Write-Host "  - Install.ps1 (此脚本)" -ForegroundColor White
    pause
    exit
}

$certPath = $certFiles[0].FullName
Write-Host "[√] 找到证书: $($certFiles[0].Name)" -ForegroundColor Green

# 查找安装包（任何 .msix 文件）
$msixFiles = Get-ChildItem -Filter "*.msix"
if ($msixFiles.Count -eq 0) {
    Write-Error "错误：找不到 .msix 安装包！"
    pause
    exit
}

$msixPath = $msixFiles[0].FullName
Write-Host "[√] 找到安装包: $($msixFiles[0].Name)" -ForegroundColor Green
Write-Host ""

# 安装证书
Write-Host "[1/2] 正在安装证书..." -ForegroundColor Yellow
try {
    $result = certutil -addstore -f "TrustedPublisher" "$certPath" 2>&1
    if ($LASTEXITCODE -ne 0) { throw $result }
    
    $result = certutil -addstore -f "Root" "$certPath" 2>&1
    if ($LASTEXITCODE -ne 0) { throw $result }
    
    Write-Host "       证书安装成功" -ForegroundColor Green
} catch {
    Write-Error "证书安装失败: $_"
    pause
    exit
}

# 安装应用
Write-Host "[2/2] 正在安装 CsvProcessor..." -ForegroundColor Yellow
try {
    Add-AppxPackage -Path "$msixPath"
    Write-Host "       应用安装成功" -ForegroundColor Green
} catch {
    Write-Error "应用安装失败: $_"
    Write-Host ""
    Write-Host "可能原因：" -ForegroundColor Yellow
    Write-Host "  1. 系统未开启旁加载模式" -ForegroundColor White
    Write-Host "     设置 → 隐私和安全性 → 开发者选项 → 旁加载应用" -ForegroundColor White
    Write-Host "  2. 证书未正确安装" -ForegroundColor White
    Write-Host "  3. 安装包损坏" -ForegroundColor White
    pause
    exit
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  安装完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "开始菜单中搜索 'CsvProcessor' 即可使用" -ForegroundColor Cyan
Write-Host ""
pause