using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
        /// <summary>
        /// 界面刷新间隔
        /// </summary>
        static readonly Int32 flushStamp = 1 * 1000;
        static dmnet.dmsoft dm = null;
        static Thread td = null;
        static Thread logicTd = null;
        static Dictionary<String, List<String>> dicAction = null;
        static Int32 signal = 0;
        static Int32 exceptSignal = 0;

        #region 窗口属性

        static Int32 screenXWidth = 0;
        static Int32 screenYHeight = 0;
        static Int32 screenLTx = 0;
        static Int32 screenLTy = 0;
        static Int32 screenRBx = 0;
        static Int32 screenRBy = 0;

        #endregion

        #region 命令

        static String Click = "Click";
        static String ClickRela = "ClickRela";
        static String DBClick = "DBClick";
        static String Wait = "Wait";
        static String BindProcess = "BindProcess";

        #endregion

        #region 变量

        static Int32 debugClickCount = 1;
        static Int32 debugClickMaxCount = 9;

        #endregion

        #region DLL

        [DllImport("user32.dll")]
        public static extern int MessageBoxTimeoutA(IntPtr hWnd, string msg, string Caps, int type, int Id, int time);//引用DLL
        //MessageBoxTimeoutA((IntPtr )0,"3秒后自动关闭","消息框",0,0,3000);// 直接调用  3秒后自动关闭 父窗口句柄没有直接用0代替

        #endregion

        /// <summary>
        /// 缓存坐标,用于减少查找时间
        /// </summary>
        static Dictionary<String, List<Tuple<Int32, Int32, Int32, Int32>>> cacheLocation = new Dictionary<string, List<Tuple<int, int, int, int>>>();

        /// <summary>
        /// 命令
        /// </summary>
        static Dictionary<String, String> cmd = new Dictionary<string, string>();

        /// <summary>
        /// 当前期待
        /// </summary>
        static List<String> curExcept = new List<string>();

        /// <summary>
        /// 当前状态
        /// </summary>
        static Dictionary<String, Tuple<Int32, Int32, Int32, Int32>> curState = new Dictionary<string, Tuple<int, int, int, int>>();

        static void Main(string[] args)
        {
            td = new Thread(() =>
            {
                InitThread();
            });
            td.Start();

            // 关闭退出
            var iptStr = Console.ReadLine();
            while (iptStr?.ToString()?.ToLower() != "stop")
            {
                iptStr = Console.ReadLine();
            }
            StopThread();
        }

        #region 线程相关

        /// <summary>
        /// 初始线程
        /// </summary>
        static void InitThread()
        {
            Console.WriteLine("开始线程");
            LoadConfig();
            InitThreadLoadZK();
            InitTreadLoadLogicAction();
            Run();
            InitThreadMontiorGameInterface();
        }

        /// <summary>
        /// 初始化配置
        /// </summary>
        static void LoadConfig()
        {
            StreamReader sr = new StreamReader(Config.Instance.ConfigPath);
            var line = sr.ReadLine();
            while (line != null)
            {
                if (!line.ToLower().StartsWith("cfg"))
                {
                    line = sr.ReadLine();
                    continue;
                }
                var key = line.Split(new char[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries)[1];
                var val = line.Split(new char[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries)[2];
                if (Config.Instance.ContainProperty(key))
                {
                    Config.Instance.SetValue(key, val);
                };
                line = sr.ReadLine();
            }
            sr.Close();
        }

        /// <summary>
        /// 加载字库
        /// </summary>
        static void InitThreadLoadZK()
        {
            Console.WriteLine("开始加载字库");

            // 初始化大漠插件
            dm = new dmnet.dmsoft();
            dm.SetPath(Path.GetFullPath(Config.Instance.DMWorkPath));

            //dm.SetDisplayInput($"pic:image.jpeg");
            var m = Process.GetProcessById(15496).MainWindowHandle;
            Console.WriteLine(m);
            dm.BindWindow(m.ToInt32(), "dx2", "windows", "normal", 0);
            dm.delay(300);

            if (String.IsNullOrEmpty(Config.Instance.DictPath))
            {
                Console.WriteLine("配置DictPath是必须的");
            }
            var dm_ret = dm.SetDict(0, Config.Instance.DictPath);

            // 计算窗口属性
            object x, y, x2, y2;
            if (dm.GetWindowRect(m.ToInt32(), out x, out y, out x2, out y2) > -1)
            {
                screenXWidth = (Int32)x2 - (Int32)x;
                screenYHeight = (Int32)y2 - (Int32)y;
                screenLTx = (Int32)x;
                screenLTy = (Int32)y;
                screenRBx = (Int32)x2;
                screenRBy = (Int32)y2;
            }
        }

        /// <summary>
        /// 初始化行为
        /// </summary>
        static void InitTreadLoadLogicAction()
        {
            dicAction = new Dictionary<string, List<string>>();
            StreamReader sr = new StreamReader("aconfg.txt");
            var line = sr.ReadLine();
            while (line != null)
            {
                if (String.IsNullOrEmpty(line) || line.ToLower().StartsWith("cfg"))
                {
                    line = sr.ReadLine();
                    continue;
                }
                var sp = line.Split(new char[] { ' ', '>' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < sp.Length; i++)
                {
                    if (i == 0 && Int32.TryParse(sp[i], out int temp))
                    {
                        continue;
                    }

                    // 开始游戏[Click] > 确定[Click]
                    var cmdKey = sp[i].Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                    if (i == 0 || i == 1)
                    {
                        curExcept.Add(cmdKey[0]);
                    }
                    if (cmdKey.Length > 0)
                    {
                        if (!dicAction.ContainsKey(cmdKey[0]))
                        {
                            dicAction[cmdKey[0]] = new List<string>();
                        }
                        dicAction[cmdKey[0]].Add(cmdKey[1]);
                    }
                }
                line = sr.ReadLine();
            }
            sr.Close();
        }

        /// <summary>
        /// 监听游戏界面
        /// </summary>
        static void InitThreadMontiorGameInterface()
        {
            Console.WriteLine("开始监听游戏界面");
            while (true)
            {
                Thread.Sleep(flushStamp);
                foreach (var itemWord in curExcept.Distinct().ToList())
                {
                    try
                    {
                        if (curState.ContainsKey(itemWord)) continue;

                        // 取cacheLocation
                        Int32 zbx = 0, zby = 0, zbx2 = 1920, zby2 = 1080;
                        var flagFindInCached = false;
                        if (cacheLocation.ContainsKey(itemWord))
                        {
                            foreach (var ietmCache in cacheLocation[itemWord])
                            {
                                zbx = ietmCache.Item1;
                                zby = ietmCache.Item2;
                                zbx2 = ietmCache.Item3;
                                zby2 = ietmCache.Item4;
                                object xx0 = 0, yy0 = 0;
                                if (dm.FindStr(zbx, zby, zbx2, zby2, itemWord, "7b3303-7b3303", 0.9, out xx0, out yy0) > -1)
                                {
                                    curState.Add(itemWord, new Tuple<int, int, int, int>((Int32)xx0, (Int32)yy0, 0, 0));
                                    Console.WriteLine("aaabbb{0},{1}", xx0, yy0);
                                    flagFindInCached = true;
                                    break;
                                }
                                //dm.findfl
                            }
                            var zbxs = cacheLocation[itemWord];
                        }

                        // 走了缓存也没查到,那也需要在查一次全局
                        if (!flagFindInCached)
                        {
                            object x0, y0;
                            if (dm.FindStr(0, 0, screenXWidth, screenYHeight, itemWord, "7b3303-7b3303", 0.9, out x0, out y0) > -1)
                            {
                                Int32 x = (Int32)x0, y = (Int32)y0;
                                curState.Add(itemWord, new Tuple<int, int, int, int>(x, y, 0, 0));
                                if (cacheLocation.ContainsKey(itemWord))
                                {
                                    foreach (var itemLocation in cacheLocation[itemWord].ToList())
                                    {
                                        // 计算精度差异
                                        var d = itemLocation.Item1 - (x - 10);
                                        d += (itemLocation.Item2 - (y - 10));
                                        if (d <= 10)
                                        {
                                            // 替换掉(先移除)
                                            cacheLocation[itemWord].Remove(itemLocation);
                                            break;
                                        }
                                    }
                                    cacheLocation[itemWord].Add(new Tuple<int, int, int, int>(x - 10, y - 10, screenXWidth, screenYHeight));
                                }
                                else
                                {
                                    cacheLocation[itemWord] = new List<Tuple<int, int, int, int>>();
                                    cacheLocation[itemWord].Add(new Tuple<int, int, int, int>(x - 10, y - 10, screenXWidth, screenYHeight));
                                }
                                Console.WriteLine("bbbaaa{0},{1}", x, y);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("开始游戏界面 Error:" + ex.Message);
                    }
                }
            }
        }

        #endregion

        #region 逻辑相关

        static void Run()
        {
            if (logicTd != null)
            {
                // 释放逻辑线程
                Console.WriteLine("释放逻辑线程");
                var tempTd = logicTd;
                logicTd = null;
                tempTd.Abort();
            }
            logicTd = new Thread(() =>
            {
                try
                {
                    Dictionary<String, Tuple<Int32, Int32, Int32, Int32>> tempState = null;
                    while (true)
                    {
                        tempState = curState;
                        if (tempState.Count > 0)
                        {
                            // 执行下一步逻辑
                            foreach (var itemCmd in tempState)
                            {
                                if (dicAction.ContainsKey(itemCmd.Key))
                                {
                                    foreach (var item in dicAction[itemCmd.Key])
                                    {
                                        switch (item)
                                        {
                                            case "Click":
                                                Console.WriteLine("Click");
                                                DMClick(itemCmd.Value.Item1, itemCmd.Value.Item2);
                                                break;
                                            case "DBClick":
                                                Console.WriteLine("DBClick");
                                                DMClick(itemCmd.Value.Item1, itemCmd.Value.Item2, 2);
                                                break;
                                            case "Wait":
                                                Console.WriteLine("Wait");
                                                Thread.Sleep(1000);
                                                break;
                                            default: break;
                                        }
                                    }
                                }
                                curExcept.Add("");

                                // 移除当前状态
                                tempState.Remove(itemCmd.Key);

                                // 修改当前期待

                            }
                        }
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Run Error:" + ex.Message);
                }
            });
            logicTd.Start();
        }

        #endregion

        static void StopThread()
        {
            if (td != null)
            {
                td.Abort();
            }
            if (logicTd != null)
            {
                logicTd.Abort();
            }
        }

        #region 大漠操作

        /// <summary>
        /// 大漠点击
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="count"></param>
        static void DMClick(Int32 x, Int32 y, Int32 count = 1)
        {
            dm.MoveTo(x, y);
            if (count == 2)
            {
                if (Config.Instance.Debug)
                {
                    //dm.ShowScrMsg(x, y, x + 2, y + 2, debugClickCount.ToString(), "ccc");
                    if (++debugClickCount > debugClickMaxCount)
                    {
                        debugClickCount = 0;
                    }
                    dm.delay(200);
                    //dm.ShowScrMsg(x, y, x + 2, y + 2, debugClickCount.ToString(), "ccc");
                    if (++debugClickCount > debugClickMaxCount)
                    {
                        debugClickCount = 0;
                    }
                }
                dm.LeftDoubleClick();
                return;
            }
            for(var i = 0; i< count; i++)
            {
                if (Config.Instance.Debug)
                {
                    // MessageBoxTimeoutA((IntPtr)0, debugClickCount.ToString(), "", 0, 0, 1000);// 直接调用  3秒后自动关闭 父窗口句柄没有直接用0代替
                    // dm.ShowScrMsg(x, y, x + 2, y + 2, debugClickCount.ToString(), "ccc");
                    if (++debugClickCount > debugClickMaxCount)
                    {
                        debugClickCount = 0;
                    }
                }
                dm.LeftClick();
                dm.delay(200);
            }
        }

        #endregion
    }
}
