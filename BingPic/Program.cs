using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BingPic
{
    class Program
    {
        enum WallpaperStyle
        {
            Center,
            Tile,
            Stretch,
            StretchToFill
        }

        static HttpClient client = new HttpClient();
        static int interval = 5;
        static WallpaperStyle style = WallpaperStyle.StretchToFill;

        static void Main(string[] args)
        {
            //读取可能不存在的配置文件
            try
            {
                INI ini = new INI("settings.ini");
                interval = Convert.ToInt32(ini.Read("Interval"));
                if (!Enum.TryParse(ini.Read("WallpaperStyle"), out style))
                {
                    style = WallpaperStyle.StretchToFill;
                }
            }
            //若读取过程中出现任何错误，则使用默认值代替未被成功读取的值
            catch { }
            NotifyIcon notifyIcon = new NotifyIcon();
            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add("退出", (s, e) => { Environment.Exit(0); });
            notifyIcon.ContextMenu = menu;
            notifyIcon.Text = "必应每日一图";
            notifyIcon.Icon = Properties.Resources.TrayIcon;
            notifyIcon.Visible = true;
            _ = Loop();
            Application.Run();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        static async Task Loop()
        {
            var lastDay = DateTime.Now.Day;
            while (true)
            {
                try
                {
                    //检查日期并更换桌面壁纸
                    var currentDay = -1;
                    if (lastDay != currentDay)
                    {
                        //新的一天来临了！昨晚被杀的是（划掉
                        lastDay = currentDay;
                        //获取最新的必应美图
                        var response = await client.GetAsync("https://cn.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&pid=hp&uhd=1&uhdwidth=3840&uhdheight=2160");
                        if (!response.IsSuccessStatusCode) continue;
                        var json = await response.Content.ReadAsStringAsync();
                        var responseObj = BingResponse.FromJson(json);
                        var url = "https://cn.bing.com" + responseObj.Images[0].Url;
                        response = await client.GetAsync(url);
                        System.Drawing.Image image = System.Drawing.Image.FromStream(await response.Content.ReadAsStreamAsync());
                        string tmp = Path.Combine(Path.GetTempPath(), "temp.jpg");
                        //删除可能存在的旧的临时文件
                        if (File.Exists(tmp))
                        {
                            try
                            {
                                File.Delete(tmp);
                            }
                            catch { }
                        }
                        //保存图片，设置壁纸
                        image.Save(tmp);
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
                    }
                }
                catch { }
                await Task.Delay(TimeSpan.FromMinutes(interval));
            }
        }
    }
}
