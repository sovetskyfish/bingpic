﻿# 必应美图

一个超小型的工具，让你的桌面每天换个样！（其实就是必应每日一图）

# 配置文件格式

在可执行文件的同目录下建立一个`settings.ini`文件，它应该包含一个名为`Core`的Section，所有配置项均在此Section下声明。下面是默认配置等价的INI文件示例：

```
[Core]
Interval = 10
WallpaperStyle = StretchToFill
ShowCopyright = true
```

其中，`Interval`为程序时间检查间隔（以分钟计）；`WallpaperStyle`为壁纸模式，有四个取值：`Center`，`Stretch`，`StretchToFill`以及`Tile`；`ShowCopyright`决定是否在桌面右下角展示版权信息。各项可以单独设置。

必应美图的高清无码API由[晨旭](https://github.com/chenxuuu)提供~
