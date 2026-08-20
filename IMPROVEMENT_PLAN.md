# v2rayN 改进计划

> 基于 2026-08-20 代码评估结果制定
> 评估基线：v2rayN 主分支（commit at clone time）
> 三大目标：① 默认 sing-box 内核 + 可选外挂内核 ② 移除推广内容 ③ 引入 sing-box 高级路由特性（去广告默认关闭可选开启、引入第三方规则集源）

---

## 0. 总体原则

- **分阶段交付**：三个工作流相互独立，按风险从低到高推进
- **双前端同步**：每项 UI 改动须同时覆盖 WPF（`v2rayN/`）和 Avalonia（`v2rayN.Desktop/`）
- **本地化完整**：涉及用户可见字符串时，同步更新 8 种语言 `.resx`（en/zh-Hans/zh-Hant/fa/fr/ru/hu/id）
- **不破坏存量配置**：数据库 schema 变更须提供迁移；旧配置加载时降级兼容
- **测试先行**：每个工作流完成后运行 `ServiceLib.Tests`，新增功能补充测试用例

## 1. 阶段划分与里程碑

| 阶段 | 工作流 | 预估工作量 | 风险等级 | 前置依赖 |
|------|--------|-----------|---------|---------|
| P0 | 工作流 A：移除推广内容 | 0.5 人日 | 极低 | 无 |
| P1 | 工作流 C：增强高级路由特性 | 4-6 人日 | 低 | 无（增量式） |
| P2 | 工作流 B：精简为 sing-box 单内核 | 5-8 人日 | 中 | P0 完成；建议 P1 完成后进行（减少回归面） |

> 推荐顺序：P0 → P1 → P2。P0 零风险可立即合入；P1 增量增强不破坏现有功能；P2 影响面最大，放在最后。

---

## 2. 工作流 A：移除推广内容（P0）

### 2.1 目标
移除项目内唯一一处 Base64 混淆的推广 URL 及其在双前端的所有触点，清理相关僵尸资源。

### 2.2 任务清单

| # | 任务 | 文件 | 行号 | 说明 |
|---|------|------|------|------|
| A1 | 删除推广 URL 常量 | `ServiceLib/Global.cs` | 11 | 移除 `PromotionUrl` 常量 |
| A2 | 移除 WPF 推广菜单项 | `v2rayN/Views/MainWindow.xaml` | 280-295 | 删除 `menuPromotion` MenuItem 及其图标 |
| A3 | 移除 WPF 推广事件 | `v2rayN/Views/MainWindow.xaml.cs` | 27, 246-249 | 删除事件绑定与 `MenuPromotion_Click` 方法 |
| A4 | 移除 Avalonia 推广菜单项 | `v2rayN.Desktop/Views/MainWindow.axaml` | 101 | 删除 `menuPromotion` MenuItem |
| A5 | 移除 Avalonia 推广事件 | `v2rayN.Desktop/Views/MainWindow.axaml.cs` | 27, 240-243 | 删除事件绑定与 `MenuPromotion_Click` 方法 |
| A6 | 移除本地化字符串 | `ServiceLib/Resx/ResUI*.resx`（8 个文件） | 420-421 | 删除 `menuPromotion` data 条目 |
| A7 | 移除 Designer 访问器 | `ServiceLib/Resx/ResUI.Designer.cs` | 1396-1402 | 删除 `menuPromotion` 属性 |
| A8 | 清理僵尸位图资源 | `v2rayN/Properties/Resources.Designer.cs` | 173-181 | 删除 `promotion` Bitmap 访问器（已无对应数据） |

### 2.3 验证
- 编译 WPF 与 Avalonia 两个项目均无报错
- 全局搜索 `Promotion`、`menuPromotion`、`9.234456`、`aHR0cHM6Ly85` 均无残留
- 运行程序确认工具栏无推广入口

---

## 3. 工作流 B：精简为 sing-box 单内核（P2）

### 3.1 目标
将 sing-box 设为全协议默认内核；移除冗余的 Xray/v2fly/v2fly_v5 配置生成代码与 mihomo/Clash 处理逻辑；保留 Xray 下载能力（不作为默认）以兼顾存量用户；对 sing-box 已内含支持的协议（Hysteria2/TUIC 等）移除独立内核；对 sing-box 不支持的协议（brook/overtls/mieru 等）保留为可选外挂内核，用户可自定义选择。

### 3.2 范围界定

**保留（默认内核）**：
- `ECoreType.sing_box` — 唯一默认内核，全协议优先走 sing-box
- `Services/CoreConfig/Singbox/` 全部 9 个文件
- sing-box 的下载、更新、TUN、规则集、DNS 全套逻辑

**保留（可选外挂内核，用户自定义选择）**：
- `ECoreType.Xray` — 保留下载能力与配置生成，仅作为可选项（非默认），用于 kcp/xhttp 等 sing-box 不支持的传输场景
- `ECoreType.brook`、`ECoreType.overtls`、`ECoreType.mieru` — sing-box 不支持的协议，保留为可选外挂子进程
- 对应的 `CoreInfoManager` 条目、`CoreUrls` 映射、`ECoreType` 枚举值

**移除（sing-box 已内含支持的协议独立内核）**：
- `ECoreType.hysteria`、`ECoreType.hysteria2` — sing-box 已内含 Hysteria2 协议
- `ECoreType.tuic` — sing-box 已内含 TUIC 协议
- `ECoreType.naiveproxy` — sing-box 已内含 Naive 协议
- `ECoreType.juicity`、`ECoreType.shadowquic` — 同上，sing-box 可覆盖
- 以上枚举值、CoreInfoManager 条目、CoreUrls 映射

**移除（冗余配置生成代码）**：
- v2fly / v2fly_v5 的配置生成（共享 V2ray 路径，但 v2fly/v2fly_v5 枚举移除后不再可选）
- mihomo/Clash 配置处理（`CoreConfigClashService.cs`）— Clash YAML 用户需迁移至 sing-box 自定义配置模板
- `ECoreType.v2fly`、`ECoreType.v2fly_v5`、`ECoreType.mihomo` 枚举值

**注意权衡**：
- mihomo/Clash YAML 自定义配置用户需迁移（Release Notes 明确告知，提供迁移指引）
- 默认走 sing-box 后，原使用 kcp/xhttp 传输的节点需手动切换至 Xray 可选内核（加载时检测并提示）
- Xray `balancers` 负载均衡在 sing-box 下用 selector/urltest 出站替代（功能等价，配置不兼容）

### 3.3 任务清单

#### 3.3.1 枚举与数据模型精简

| # | 任务 | 文件 | 说明 |
|---|------|------|------|
| B1 | 精简 ECoreType 枚举 | `ServiceLib/Enums/ECoreType.cs` | 保留 `sing_box=24`、`v2rayN=99`（自更新）、`Xray=2`（可选）、`brook=27`、`overtls=28`、`mieru=30`（sing-box 不支持的协议外挂）；移除 v2fly/v2fly_v5/mihomo/hysteria/hysteria2/tuic/naiveproxy/juicity/shadowquic 共 9 个值 |
| B2 | 精简 CoreUrls 字典 | `ServiceLib/Global.cs:632-649` | 仅保留 sing_box、v2rayN、Xray、brook、overtls、mieru 六个映射 |
| B3 | 精简协议支持集合 | `ServiceLib/Global.cs:355-382` | 移除 `SingboxOnlyConfigType`（不再需要对比）；`XraySupportConfigType` 保留（Xray 作为可选项仍需校验）；`SingboxSupportConfigType` 保留为默认 |
| B4 | 精简 CoreInfoManager | `ServiceLib/Manager/CoreInfoManager.cs:97-292` | `InitCoreInfo()` 保留 sing-box、v2rayN、Xray、brook、overtls、mieru 初始化；移除 hysteria/hysteria2/tuic/naiveproxy/juicity/shadowquic/v2fly/v2fly_v5/mihomo 共 9 项；`GetCheckUpdateCoreTypes()` 保留 sing-box、Xray、v2rayN |
| B5 | 修改默认内核 | `ServiceLib/Manager/AppManager.cs:666-675` | `GetCoreType()` 默认返回 `ECoreType.sing_box`；保留 per-node override 机制（用户可手动切至 Xray/brook 等可选内核） |
| B6 | 修改 UI 默认值 | `ServiceLib/ViewModels/OptionSettingViewModel.cs:238-242` | 所有协议默认内核改为 `sing_box`；内核选择 ComboBox 保留但默认选项改为 sing-box |
| B7 | 简化 CoreConfigContextBuilder | `ServiceLib/Handler/Builder/CoreConfigContextBuilder.cs:36-37` | 默认走 sing-box 路径；保留对 Xray 的分支（可选内核场景仍需生成 Xray 配置） |
| B8 | 简化 CoreConfigHandler 调度 | `ServiceLib/Handler/CoreConfigHandler.cs:18-31` | 保留 Singbox 与 V2ray 两条路径（V2ray 路径服务可选的 Xray 内核）；移除 Clash 分支 |
| B9 | 简化 NodeValidator | `ServiceLib/Handler/Builder/NodeValidator.cs:16-79` | 默认按 sing-box 校验；保留 Xray 校验分支（可选内核场景）；kcp/xhttp 节点在 sing-box 下提示用户切换至 Xray 内核 |

#### 3.3.2 配置生成服务清理

| # | 任务 | 文件 | 说明 |
|---|------|------|------|
| B10 | 保留 V2ray 配置生成（服务可选 Xray） | `ServiceLib/Services/CoreConfig/V2ray/` | **不删除**，保留 9 个文件以支持用户手动选择 Xray 内核的场景 |
| B11 | 删除 Clash 配置生成 | `ServiceLib/Services/CoreConfig/CoreConfigClashService.cs` | 280 行，mihomo 枚举已移除，Clash YAML 路径不再需要 |
| B12 | 检查 V2ray 样本依赖 | `ServiceLib/Sample/` | 确认 `custom_routing_*` 等模板是否仍被 sing-box 路径引用；若仅 V2ray 用则保留（Xray 可选场景仍需） |

#### 3.3.3 进程与更新逻辑精简

| # | 任务 | 文件 | 说明 |
|---|------|------|------|
| B13 | 简化 CoreManager | `ServiceLib/Manager/CoreManager.cs:298-305` | `ShouldRunAsSudo()` 保留 sing-box 与 Xray；`IsRunningCore` 等价判断仅保留 sing-box 等价组 |
| B14 | 简化 UpdateService 版本解析 | `ServiceLib/Services/UpdateService.cs:227-287` | `GetCoreVersion()` / `ParseDownloadUrl()` 保留 sing-box 与 Xray 分支；移除 mihomo/hysteria 等已删枚举的分支 |
| B15 | 保留可选内核下载逻辑 | `ServiceLib/Services/UpdateService.cs:400-444` | 保留 sing-box 规则集下载；Xray 内核下载逻辑保留（用户可选下载）；移除已删枚举对应资源更新 |

#### 3.3.4 UI 清理（双前端）

| # | 任务 | 文件 | 说明 |
|---|------|------|------|
| B16 | 保留内核选择 UI，默认改为 sing-box | `v2rayN.Desktop/Views/OptionSettingWindow.axaml:1122-1229` 及 WPF 对应 | 保留各协议的内核选择 ComboBox，默认选项改为 sing-box；可选值仅保留 sing-box/Xray/brook/overtls/mieru |
| B17 | 精简 Help 菜单内核官网 | `v2rayN/Views/MainWindow.xaml.cs:431-445` 及 Avalonia 对应 | `AddHelpMenuItem()` 仅保留 sing-box、Xray、brook、overtls、mieru 五项 |
| B18 | 移除 Clash 专用视图 | 搜索 `ClashProxiesView` 等引用 | 移除 mihomo 专用 UI（若存在） |
| B19 | 精简内核切换相关本地化字符串 | `ServiceLib/Resx/ResUI*.resx` | 清理涉及 v2fly/v2fly_v5/mihomo/hysteria/tuic/naiveproxy 等已移除内核的提示文本 |

#### 3.3.5 数据库与配置迁移

| # | 任务 | 说明 |
|---|------|------|
| B20 | 首启动强制备份 | 首次加载旧配置时备份 `guiNDB.db` 至 `guiNDB.db.bak`（检测配置版本字段，仅执行一次） |
| B21 | 旧节点 CoreType 字段迁移 | `ProfileItem.CoreType` 中已移除枚举值（v2fly/mihomo/hysteria 等）的存量节点自动转为 `sing_box`；Xray/brook 等保留枚举的节点维持原值 |
| B22 | 选项设置迁移 | `Config.CoreTypeItem` 中已移除内核的条目自动转为 `sing_box`；保留内核条目维持原值 |
| B23 | 版本号升级 | 在 `Config` 增加配置版本字段，迁移完成后标记，避免重复执行 |

### 3.4 验证
- `ServiceLib.Tests` 全部通过
- 编译 WPF 与 Avalonia 无错误/警告
- 导入 VMess/VLESS/Trojan/Shadowsocks/Hysteria2/TUIC/Anytls/WireGuard 节点默认走 sing-box 均可连接
- 手动将节点切换至 Xray 可选内核后可正常连接（kcp/xhttp 传输验证）
- 手动将 brook/overtls/mieru 节点切换至对应可选外挂内核可连接
- TUN 模式可用
- 规则集下载与路由分流正常
- 加载旧版 v2rayN 配置库自动迁移且不报错，`guiNDB.db.bak` 备份生成
- 分发包体积对比（`bin/` 默认仅含 sing-box，减少约 40-100MB；Xray 等可选内核按需下载）

### 3.5 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 现有用户配置库不兼容 | 节点无法连接 | B20-B23 自动迁移；首启动强制备份 `guiNDB.db.bak` |
| Clash YAML 用户流失 | 功能缺失 | Release Notes 明确说明；提供迁移指引（改用 sing-box 自定义配置模板） |
| kcp/xhttp 传输节点默认失败 | sing-box 不支持 | 加载时检测并提示用户手动切换至 Xray 可选内核 |
| 上游 sing-box 协议变化 | 单点依赖 | 保留 CoreInfoManager 抽象层与 Xray 可选路径作为 fallback |

---

## 4. 工作流 C：引入 sing-box 高级路由特性（P1）

### 4.1 目标
充分暴露 sing-box 路由/DNS 高级能力，重点增强去广告与高级规则分流，提升开箱即用体验。

### 4.2 现状基线
v2rayN 已实现 sing-box 大部分高级特性（规则集、DNS 劫持、FakeIP、TLS 分片、ICMP 策略、逻辑规则、进程路由等），但存在两类不足：
1. **去广告未默认启用**：`category-ads-all` 规则集已自动下载（`UpdateService.cs:426`），但默认路由模板未配置阻断规则
2. **高级规则字段 UI 暴露不足**：逻辑规则、invert、action 类型、source_ip 等在 `RulesItem` 模型中缺失或 UI 不可编辑

### 4.3 任务清单

#### 4.3.1 去广告功能（默认关闭，可选开启）

| # | 任务 | 文件 | 说明 |
|---|------|------|------|
| C1 | 新增去广告路由模板 | `ServiceLib/Sample/custom_routing_white` 等 | 在白名单/黑名单模板中增加 `{"outboundTag":"block","domain":["geosite:category-ads-all"]}` 规则；DNS 层增加 `category-ads-all` → NXDOMAIN |
| C2 | 新增"去广告"开关字段（默认 false） | `ServiceLib/Models/Config` 新增 `EnableAdBlock` 字段，默认值 `false` | 在路由设置页增加开关，开启时自动注入去广告规则；关闭时不影响现有路由行为 |
| C3 | 去广告开关 UI | `RoutingSettingWindow.axaml` 及 WPF 对应 | 在路由设置顶部增加 ToggleSwitch，默认关闭状态 |
| C4 | 注入逻辑实现 | `SingboxRoutingService.cs` | `GenerateRouting` 方法中根据 `EnableAdBlock` 动态追加 block 规则（优先级置于用户规则之前） |
| C5 | DNS 层去广告 | `SingboxDnsService.cs:377-381` | 已有 `block` → `rcode:"NXDOMAIN"` 实现，仅需在 `EnableAdBlock` 时追加 DNS 规则 |
| C5a | 引入第三方去广告规则集源 | `ServiceLib/Global.cs:181-186` `SingboxRulesetSources` | 增加 AdGuard（`https://github.com/AdguardTeam/AdGuardDNS` 转 srs）与 anti-AD（`https://github.com/privacy-protection-tools/anti-AD`）作为补充源；用户可在规则集管理器中选择启用 |

#### 4.3.2 规则集管理增强

| # | 任务 | 文件 | 说明 |
|---|------|------|------|
| C6 | 规则集源可视化编辑 | 新建 `RulesetManagerWindow.axaml` + ViewModel | 列表展示 `Global.SingboxRulesetSources`，支持增删改、启用/禁用、查看本地缓存状态 |
| C7 | 规则集更新触发 | `UpdateService.cs:400-444` | 增加手动刷新按钮；展示每个 `.srs` 的版本与更新时间 |
| C8 | 规则集入口 | `RoutingSettingWindow.axaml` | 增加"管理规则集"按钮打开 C6 窗口 |

#### 4.3.3 高级规则分流 UI 扩展

| # | 任务 | 文件 | 说明 |
|---|------|------|------|
| C9 | 扩展 RulesItem 模型 | `ServiceLib/Models/Entities/RulesItem.cs` | 新增字段：`SourceIp`、`User`、`ActionType`（route/reject/sniff/resolve/hijack-dns）、`LogicType`（and/or）、`Invert`（bool） |
| C10 | 数据库迁移 | `ConfigHandler` | 新字段 nullable，旧数据自动兼容 |
| C11 | 扩展规则编辑 UI | `RoutingRuleDetailsWindow.axaml:1-263` 及 WPF 对应 | 增加：源 IP、Action 类型下拉、逻辑类型下拉、Invert 复选框 |
| C12 | sing-box 逻辑规则生成 | `SingboxRoutingService.cs:400-438` | 扩展逻辑规则生成，支持 `LogicType` 显式指定；`SourceIp` 字段生成 `source_ip` 规则 |
| C13 | action 类型生成 | `SingboxRoutingService.cs` | 根据 `ActionType` 生成 `action:"sniff"`/`"resolve"`/`"hijack-dns"` 等而非传统 `outboundTag` |
| C14 | Xray 侧兼容处理 | 若 P2 未完成则需处理；若完成则跳过 | Xray 不支持部分 action，降级为 `outboundTag` 映射（sniff→无、reject→block、resolve→proxy） |

#### 4.3.4 预置规则集套餐

| # | 任务 | 说明 |
|---|------|------|
| C15 | 内置"广告拦截"套餐 | 在路由设置增加一键导入：`category-ads-all` + `category-ads-ir` 等多地区广告规则集 → block |
| C16 | 内置"流媒体分流"套餐 | 一键导入：Netflix/Disney+/YouTube/Spotify 等规则集 → proxy/direct 按地区 |
| C17 | 内置"国内直连"套餐 | 一键导入：geosite:cn + geoip:cn + geoip:private → direct（替代现有白名单模板，使其更易发现） |
| C18 | 套餐管理 UI | 路由设置页增加"预置套餐"下拉，导入后作为新 RoutingItem |

### 4.4 验证
- 默认状态下（`EnableAdBlock=false`）路由行为与改造前完全一致（无回归）
- 手动开启去广告后，访问已知广告域名被阻断（路由层 + DNS 层双重验证）
- AdGuard/anti-AD 第三方规则集源可在规则集管理器中选择并下载
- 规则编辑器可创建逻辑规则（如 `domain IS ad.com AND ip IS 1.2.3.4 → block`）
- `Invert` 复选框生成的 `invert:true` 规则生效
- `ActionType=sniff` 规则在生成的 sing-box JSON 中正确出现
- 预置套餐一键导入后路由功能正常
- `ServiceLib.Tests` 全部通过

---

## 5. 测试策略

### 5.1 单元测试
- 工作流 A：无需新增（纯删除）
- 工作流 B：新增 `CoreInfoManagerTests` 验证仅 sing-box；新增 `ConfigMigrationTests` 验证旧 CoreType 迁移
- 工作流 C：新增 `SingboxRoutingAdBlockTests`、`RulesItemAdvancedTests`、`RulesetManagerTests`

### 5.2 集成验证
- 准备旧版 v2rayN 配置库（含 Xray 节点），验证 P2 迁移流程
- 准备多协议测试节点（VMess/VLESS/Trojan/SS/Hysteria2/TUIC），验证 P2 后全部可连
- 准备广告测试 URL 列表，验证 P1 去广告效果

### 5.3 回归测试
- 每个 PR 提交前运行 `dotnet test v2rayN/ServiceLib.Tests`
- 每个工作流完成后在 Windows + Linux 双平台冒烟测试

---

## 6. 发布策略

### 6.1 版本规划
- **v7.x（P0）**：仅移除推广，作为小版本发布
- **v7.x+1（P1）**：增强路由特性（去广告默认关闭可选开启、规则编辑器扩展、引入第三方规则集源），向后兼容
- **v8.0（P2）**：默认 sing-box 内核 + 可选外挂内核，明确标注 Breaking Changes：
  - 默认内核统一改为 sing-box，存量配置自动迁移
  - 移除 v2fly/mihomo/hysteria/tuic 等冗余内核（sing-box 已内含支持）
  - 保留 Xray/brook/overtls/mieru 作为可选外挂内核（用户可手动选择）
  - Clash YAML 配置不再支持，提供迁移指引
  - 首启动强制备份 `guiNDB.db.bak`
  - 提供回退指南（使用 v7.x 或原版 v2rayN）

### 6.2 通信
- Release Notes 详细说明每阶段变更
- P2 发布前在 Wiki 增加迁移指南
- Telegram 频道预告 Breaking Change

---

## 7. 关键文件索引

便于实施时快速定位：

```
枚举与全局
├── ServiceLib/Enums/ECoreType.cs                    # 内核类型枚举
├── ServiceLib/Enums/ERuleType.cs                    # 规则类型（ALL/Routing/DNS）
├── ServiceLib/Enums/ERuleMode.cs                    # 规则模式
├── ServiceLib/Global.cs                            # 推广URL(L11)、CoreUrls(L632)、协议集合(L355)、规则集源(L181)
└── ServiceLib/Models/Entities/RulesItem.cs         # 规则模型（待扩展）

核心管理
├── ServiceLib/Manager/CoreInfoManager.cs           # 内核信息（L97-292 硬编码）
├── ServiceLib/Manager/AppManager.cs                # GetCoreType 默认内核（L666-675）
├── ServiceLib/Manager/CoreManager.cs                # 进程管理、sudo 判断
└── ServiceLib/Handler/ConfigHandler.cs              # 配置加载、路由模板初始化（L2636）

配置生成
├── ServiceLib/Handler/CoreConfigHandler.cs         # 调度入口（L18-31）
├── ServiceLib/Handler/Builder/CoreConfigContextBuilder.cs  # 上下文构建（L36-37）
├── ServiceLib/Handler/Builder/NodeValidator.cs      # 节点校验（L16-79）
├── ServiceLib/Services/CoreConfig/Singbox/          # sing-box 配置（保留）
│   ├── CoreConfigSingboxService.cs
│   ├── SingboxRoutingService.cs                     # 路由生成（去广告注入点）
│   ├── SingboxDnsService.cs                         # DNS 生成
│   ├── SingboxRulesetService.cs                     # 规则集转换
│   └── ...
├── ServiceLib/Services/CoreConfig/V2ray/           # Xray 配置（待删除）
└── ServiceLib/Services/CoreConfig/CoreConfigClashService.cs  # Clash 配置（待删除）

更新服务
└── ServiceLib/Services/UpdateService.cs            # 内核更新、规则集下载（L400-444）

UI（双前端）
├── v2rayN/Views/MainWindow.xaml(.cs)               # WPF 主窗（推广L280-295）
├── v2rayN.Desktop/Views/MainWindow.axaml(.cs)       # Avalonia 主窗（推广L101）
├── v2rayN.Desktop/Views/OptionSettingWindow.axaml   # 内核选择（L1122-1229）
├── v2rayN.Desktop/Views/RoutingSettingWindow.axaml  # 路由设置
├── v2rayN.Desktop/Views/RoutingRuleDetailsWindow.axaml  # 规则编辑（待扩展）

本地化
└── ServiceLib/Resx/ResUI*.resx                      # 8 种语言（推广 L420-421）

样本
└── ServiceLib/Sample/custom_routing_white          # 默认白名单路由模板（去广告注入点）
```

---

## 8. 已确认决策

以下决策已与维护者确认，作为实施依据：

1. **辅助内核去留**：sing-box 已内含支持的协议（Hysteria2/TUIC/Naive 等）其独立内核移除；sing-box 不支持的协议（brook/overtls/mieru 等）保留为可选外挂内核，用户可自定义选择。
2. **Xray 保留为可选**：保留 Xray 下载能力与配置生成代码，不作为默认，用于 kcp/xhttp 等 sing-box 不支持的传输场景。默认全改为 sing-box。
3. **去广告默认关闭**：`EnableAdBlock` 默认值 `false`，UI 提供开关供用户手动开启，避免误伤。
4. **引入第三方规则集源**：在 `SingboxRulesetSources` 增加 AdGuard 与 anti-AD 作为去广告规则集补充源。
5. **配置迁移强制备份**：首次加载旧配置时强制备份 `guiNDB.db` 至 `guiNDB.db.bak`，通过配置版本字段确保仅执行一次。

---

*文档结束。实施时建议按阶段创建独立分支/PR，逐阶段合入。*
