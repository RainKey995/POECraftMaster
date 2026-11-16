# POECraftMaster

## 应用配置

- 应用入口在创建覆盖层和托盘前初始化 `ConfigurationManager.Instance`。配置目录统一为 `%LOCALAPPDATA%\POECraftMaster`。
- 使用管理器持有的配置实例。设置页和制作页共用 `Profiles`，使导入的词缀方案立即可用。新配置应在管理器中登记路径、启动加载和持久化方式。
- `account.json` 首次运行时创建，含 `AccountName` 和 `Cookie`。账号设置通过管理器读取和保存用户选中的文件。Cookie 是敏感凭据，避免在日志、测试样本或提交中记录真实值。
- `hotkey.json` 缺失时使用默认快捷键。托盘线程先注册新的全局快捷键，再调用管理器持久化。保存失败时保留原注册。
- `coordinates.json` 启动时加载并校验，缺失或无效时显示加载错误并阻止制作。修改后重启应用。示例位于 `examples/coordinates.json`，实际坐标需要按游戏界面校准。
- `craft-profiles.json` 缺失时使用空方案集合，保存或导入方案时写入文件。各配置类型维护自身的 JSON 格式和校验，默认路径及初始化由管理器统一负责。
