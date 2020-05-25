using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BingPic.Win32Helper;

namespace BingPic
{
    public static class DesktopTextHelper
    {
        static IntPtr progman = FindWindow("Progman", null);

        public static void ClearText()
        {
            SendMessage(progman, 0x0034, 4, IntPtr.Zero);
        }

        static private (Rectangle, float) GetResolutionAndScalingFactor()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                IntPtr desktop = g.GetHdc();
                int horzRes = GetDeviceCaps(desktop, DESKTOPHORZRES);
                int vertRes = GetDeviceCaps(desktop, DESKTOPVERTRES);
                int vertResLogical = GetDeviceCaps(desktop, VERTRES);
                return (new Rectangle(0, 0, horzRes, vertRes), (float)vertRes / vertResLogical);
            }
        }

        public static void DrawText(string FontName, float FontSize, FontStyle FontStyle, Color FontColor, string Text)
        {
            //向progman发送0x052C，使它创建一个workerw
            SendMessageTimeout
            (
                progman,
                0x052C,
                new IntPtr(0),
                IntPtr.Zero,
                SMTO_NORMAL,
                1000,
                out _
            );
            IntPtr workerw = IntPtr.Zero;
            //获取这个workerw
            EnumWindows
            (
                new EnumWindowsProc((tophandle, topparamhandle) =>
                {
                    IntPtr p = FindWindowEx
                    (
                        tophandle,
                        IntPtr.Zero,
                        "SHELLDLL_DefView",
                        IntPtr.Zero
                    );
                    if (p != IntPtr.Zero)
                    {
                        workerw = FindWindowEx
                        (
                            IntPtr.Zero,
                            tophandle,
                            "WorkerW",
                            IntPtr.Zero
                        );
                    }
                    return true;
                }),
                IntPtr.Zero
            );
            (var res, var factor) = GetResolutionAndScalingFactor();
            //改变workerw的大小，适应不同的DPI
            SetWindowPos(workerw, new IntPtr(0), 0, 0, res.Width, res.Height, ShowWindow);
            IntPtr dc = GetDCEx(workerw, IntPtr.Zero, UserDefined);
            if (dc != IntPtr.Zero)
            {
                //画东西
                using (Graphics g = Graphics.FromHdc(dc))
                {
                    using (Font font = new Font(FontName, FontSize * factor, FontStyle, GraphicsUnit.Pixel))
                    {
                        using (StringFormat stringFormat = new StringFormat())
                        {
                            stringFormat.LineAlignment = StringAlignment.Near;
                            stringFormat.Alignment = StringAlignment.Far;
                            g.DrawString(Text, font, new SolidBrush(FontColor), res, stringFormat);
                        }
                    }
                }
                ReleaseDC(workerw, dc);
            }
        }
    }
}
