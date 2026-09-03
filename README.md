# V2rayN

### A GUI client for Windows, Linux and macOS — sing-box first, with advanced routing, ad-blocking and preset packages.

[![Release](https://img.shields.io/github/v/release/rainsmen/V2rayN?logo=github&label=Release)](https://github.com/rainsmen/V2rayN/releases)
[![Downloads](https://img.shields.io/github/downloads/rainsmen/V2rayN/latest/total?logo=github&label=Downloads)](https://github.com/rainsmen/V2rayN/releases)
[![Build](https://img.shields.io/github/actions/workflow/status/rainsmen/V2rayN/build-windows.yml?logo=github&label=Build)](https://github.com/rainsmen/V2rayN/actions)

[![Windows](https://img.shields.io/badge/Windows-supported-0078D6?logo=windows)](https://github.com/rainsmen/V2rayN)
[![Linux](https://img.shields.io/badge/Linux-supported-FCC624?logo=linux&logoColor=000)](https://github.com/rainsmen/V2rayN)
[![macOS](https://img.shields.io/badge/macOS-supported-000000?logo=apple)](https://github.com/rainsmen/V2rayN)

> Forked from [2dust/v2rayN](https://github.com/2dust/v2rayN). This fork restructures the core architecture and adds advanced routing features.

---

## What's Different / 与原版的区别

This fork applies three major improvements over the upstream v2rayN:

### 1. sing-box as Default Core / sing-box 为默认内核

- **Default core changed to sing-box** for all protocols (VMess, VLESS, Trojan, Shadowsocks, Hysteria2, TUIC, Anytls, WireGuard, etc.)
- Removed redundant cores: v2fly, v2fly_v5, mihomo (Clash), hysteria, hysteria2, tuic, naiveproxy, juicity, shadowquic — sing-box already supports these protocols natively
- **Optional cores retained**: Xray (for kcp/xhttp transports), brook, overtls, mieru (protocols sing-box doesn't support)
- Existing configs auto-migrate: old CoreType values convert to sing-box on first launch, with `guiNDB.db` backed up to `guiNDB.db.bak`
- Smaller distribution: default `bin/` ships only sing-box (~40-100MB saved)

### 2. Ad-blocking & Advanced Routing / 去广告与高级路由

- **Ad-block toggle** (off by default) — injects `reject` rules at route layer and `NXDOMAIN` at DNS layer
- Three third-party ad-block ruleset sources: `category-ads-all`, `anti-AD`, `Loyalsoldier-reject`
- **Advanced rule fields** in the routing rule editor:
  - `source_ip` / `source_ip_cidr` matching
  - `actionType` (route / reject / sniff / resolve / hijack-dns) — overrides outboundTag
  - `logicType` (and / or) for sing-box logical rules
  - `invert` flag for rule negation
- **Xray compatibility**: `reject` maps to `block`, `source_ip` maps to `source`, and sing-box specific actions (`sniff`/`resolve`/`hijack-dns`) are safely ignored without unintended proxying

### 3. Removed Promotion / 移除推广

- Removed the base64-obfuscated promotion URL and all related UI/menu items
- No sponsor/donation/affiliate content

### 4. Preset Packages / 预置套餐

One-click import of preset routing rule bundles from the Routing Setting window:

| Preset | Description |
|--------|-------------|
| **Ad Block** | Blocks `geosite:category-ads-all` + `geoip:ad` |
| **Streaming Media** | Proxies Netflix / YouTube / Disney+ / HBO / Spotify / TikTok, bypasses CN |
| **Bypass CN** | Whitelist mode — bypass mainland China, proxy everything else |

### 5. Ruleset Manager / 规则集管理器

Visual editor for sing-box custom rule sets (`CustomRulesetPath4Singbox`):
- Add / remove / edit `Ruleset4Sbox` entries (tag, type, format, url, path, download_detour)
- Changes saved to JSON file and persisted to the routing item

---

## Download / 下载

Download the latest release here:

[https://github.com/rainsmen/V2rayN/releases](https://github.com/rainsmen/V2rayN/releases)

---

## Supported Platforms / 支持平台

| Platform / 平台 | x64 | arm64 | riscv64 | loong64 |
| --- | --- | --- | --- | --- |
| Windows | ✅ | ✅ | - | - |
| Linux | ✅ | ✅ | ✅ | ✅ |
| macOS | ✅ | ✅ | - | - |

---

## Migration Guide / 迁移指南

If upgrading from upstream v2rayN:

1. **Backup**: Your `guiNDB.db` is automatically backed up to `guiNDB.db.bak` on first launch
2. **Core migration**: Nodes using removed cores (mihomo/v2fly/hysteria etc.) auto-switch to sing-box
3. **Clash YAML users**: Clash YAML custom config is no longer supported — migrate to sing-box custom config template
4. **kcp/xhttp transport users**: Manually switch those nodes to the Xray optional core in node settings
5. **Xray users**: Xray remains available as an optional core — download it via Check Updates if needed

---

## Build / 编译

```bash
# Requires .NET 10 SDK
git clone --recursive https://github.com/rainsmen/V2rayN.git
cd V2rayN/v2rayN
dotnet build -c Release
```

---

## Credits / 致谢

- Original project: [2dust/v2rayN](https://github.com/2dust/v2rayN)
- Core: [SagerNet/sing-box](https://github.com/SagerNet/sing-box)
- Optional core: [XTLS/Xray-core](https://github.com/XTLS/Xray-core)
- Ad-block rulesets: [anti-AD](https://github.com/privacy-protection-tools/anti-AD), [Loyalsoldier/sing-box-rules](https://github.com/Loyalsoldier/sing-box-rules)
- GeoIP/GeoSite: [Loyalsoldier/v2ray-rules-dat](https://github.com/Loyalsoldier/v2ray-rules-dat)
