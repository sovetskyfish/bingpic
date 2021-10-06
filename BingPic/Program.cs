using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BingPic.Win32Helper;

namespace BingPic
{
    class Program
    {
        //常量
        const string startupRegistry = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string appName = "BingPic";

        //只读数据
        readonly static string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        enum WallpaperStyle
        {
            Center,
            Tile,
            Stretch,
            StretchToFill
        }

        static HttpClient client = new HttpClient(new HttpClientHandler() { Proxy = new WebProxy() });
        static int interval = 10;
        static WallpaperStyle style = WallpaperStyle.StretchToFill;
        static bool showCopyright = true;
        static bool autoStart = false;

        [STAThread]
        static void Main(string[] args)
        {
            //读取可能不存在的配置文件
            try
            {
                INI ini = new INI(Path.Combine(localAppData, "BingPic\\settings.ini"));
                try
                {
                    interval = Convert.ToInt32(ini.Read("Interval"));
                }
                catch { }
                try
                {
                    if (!Enum.TryParse(ini.Read("WallpaperStyle"), out style))
                    {
                        style = WallpaperStyle.StretchToFill;
                    }
                }
                catch
                {
                    style = WallpaperStyle.StretchToFill;
                }
                try
                {
                    if (!bool.TryParse(ini.Read("ShowCopyright"), out showCopyright))
                    {
                        showCopyright = true;
                    }
                }
                catch
                {
                    showCopyright = true;
                }
                try
                {
                    if (!bool.TryParse(ini.Read("AutoStart"), out autoStart))
                    {
                        autoStart = false;
                    }
                }
                catch
                {
                    autoStart = false;
                }
            }
            //若读取过程中出现任何错误，则使用默认值代替未被成功读取的值
            catch { }
            try
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(startupRegistry, true);
                var value = registryKey.GetValue(appName);
                if (autoStart)
                {
                    //若配置了自动启动，则将自己添加为自动启动项
                    if (value == null)
                    {
                        //如果没有该键值，则说明应用未被配置自动启动
                        registryKey.SetValue(appName, Application.ExecutablePath);
                    }
                }
                else
                {
                    //否则将自己从启动项中删除
                    if (value != null)
                    {
                        //如果存在该键值，则删除该键值
                        registryKey.DeleteValue(appName, false);
                    }
                }
            }
            //无论配置启动项成功与否，都不应该导致程序崩溃
            catch { }
            NotifyIcon notifyIcon = new NotifyIcon();
            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add("编辑设置", (s, e) =>
            {
                try
                {
                    if (!File.Exists(Path.Combine(localAppData, "BingPic\\settings.ini")))
                    {
                        //若不存在该文件，则首先创建一个新的
                        Directory.CreateDirectory(Path.Combine(localAppData, "BingPic"));
                        using (var settingsFile = File.CreateText(Path.Combine(localAppData, "BingPic\\settings.ini")))
                        {
                            //向文件中写入基本框架
                            settingsFile.WriteLine("; 自动生成的设置项配置文件\n; 您可以在项目的GitHub页面查看设置项说明");
                            settingsFile.WriteLine("[Core]");
                            settingsFile.Flush();
                        }
                    }
                }
                catch
                {
                    MessageBox.Show($"设置项文件不存在却无法创建设置项配置文件。\n" +
                        $"您可以尝试手动创建文件：\n" +
                        $"{Path.Combine(localAppData, "BingPic\\settings.ini")}\n" +
                        $"并编辑它来配置此应用程序。", "出错了");
                    return;
                }
                //打开文件资源管理器并选中该文件
                Process.Start("explorer.exe",
                    $"/select, \"{Path.Combine(localAppData, "BingPic\\settings.ini")}\"");
            });
            menu.MenuItems.Add("将壁纸另存为...", (s, e) =>
            {
                //弹出另存为对话框
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                dialog.Title = "另存为";
                dialog.Filter = "JPEG 图片|*.jpg";
                var result = dialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    //如果用户选择了保存的位置
                    try
                    {
                        if (File.Exists(dialog.FileName))
                        {
                            //删除已经存在的文件
                            File.Delete(dialog.FileName);
                        }
                        //将临时文件拷贝过去
                        File.Copy(Path.Combine(Path.GetTempPath(), "temp.jpg"), dialog.FileName);
                    }
                    catch
                    {
                        //保存出现了错误
                        MessageBox.Show($"无法将图片保存到 {dialog.FileName}", "保存失败");
                    }
                }
            });
            menu.MenuItems.Add("-");
            menu.MenuItems.Add("退出", (s, e) =>
            {
                notifyIcon.Dispose();
                //清除版权信息
                DesktopTextHelper.ClearText();
                Environment.Exit(0);
            });
            notifyIcon.ContextMenu = menu;
            notifyIcon.Text = "必应每日一图";
            notifyIcon.Icon = Properties.Resources.TrayIcon;
            notifyIcon.Visible = true;
            _ = Loop();
            Application.Run();
        }

        static async Task Loop()
        {
            var lastDay = -1;
            string lasturl = "";
            while (true)
            {
                try
                {
                    //检查日期并更换桌面壁纸
                    var currentDay = DateTime.Now.Day;
                    if (lastDay != currentDay)
                    {
                        //新的一天来临了！昨晚被杀的是（划掉
                        //获取最新的必应美图，此高清Uri由晨旭提供~
                        var response = await client.GetAsync("https://cn.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&pid=hp&uhd=1&uhdwidth=3840&uhdheight=2160");
                        if (!response.IsSuccessStatusCode) continue;
                        var json = await response.Content.ReadAsStringAsync();
                        var responseObj = BingResponse.FromJson(json);
                        var url = "https://cn.bing.com" + responseObj.Images[0].Url;
                        if (url == lasturl)
                        {
                            //这和上次的一样嘛！等待interval后重新获取
                            lastDay = -1;
                        }
                        else
                        {
                            response = await client.GetAsync(url);
                            string tmp = Path.Combine(Path.GetTempPath(), "temp.jpg");
                            using (System.Drawing.Image image = System.Drawing.Image.FromStream(await response.Content.ReadAsStreamAsync()))
                            {
                                //删除可能存在的旧的临时文件
                                if (File.Exists(tmp))
                                {
                                    try
                                    {
                                        File.Delete(tmp);
                                    }
                                    catch { }
                                }
                                //保存图片
                                image.Save(tmp);
                            }
                            //设置壁纸
                            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
                            string WallpaperStyle = "", TileWallpaper = "";
                            switch (style)
                            {
                                case Program.WallpaperStyle.Center:
                                    WallpaperStyle = "1";
                                    TileWallpaper = "0";
                                    break;
                                case Program.WallpaperStyle.Stretch:
                                    WallpaperStyle = "2";
                                    TileWallpaper = "0";
                                    break;
                                case Program.WallpaperStyle.StretchToFill:
                                    WallpaperStyle = "10";
                                    TileWallpaper = "0";
                                    break;
                                case Program.WallpaperStyle.Tile:
                                    WallpaperStyle = "1";
                                    TileWallpaper = "1";
                                    break;
                            }
                            key.SetValue("WallpaperStyle", WallpaperStyle);
                            key.SetValue("TileWallpaper", TileWallpaper);
                            SystemParametersInfo
                                (
                                SPI_SETDESKWALLPAPER,
                                0,
                                tmp,
                                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE
                                );
                            lastDay = currentDay;
                            lasturl = url;
                            if (showCopyright)
                            {
                                //绘制版权信息
                                var copyright = responseObj.Images[0].Copyright.Replace("(", "").Replace(")", "").Replace(" ©", "\n©");
                                DesktopTextHelper.ClearText();
                                DesktopTextHelper.DrawText("Microsoft YaHei UI", 14, FontStyle.Regular, Color.FromArgb(255, 255, 255, 255), copyright);
                            }
                        }
                    }
                }
                catch { }
                await Task.Delay(TimeSpan.FromMinutes(interval));
            }
        }
    }
}
