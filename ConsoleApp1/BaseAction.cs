using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class BaseAction
    {
        public static List<BaseAction> Instances { get; set; }

        public String Name { get; set; }

        public Boolean IsPic { get; set; }

        public String PicPath { get; set; }

        public String Action { get; set; }

        public List<Object> ActionParam { get; set; }

        public Int32 ActionParamCount { get; set; }

        public String Note { get; set; }

        public BaseAction Next { get; set; }

        public Int32 Level { get; set; }

        public Int32 SecLevel { get; set; }

        public String SignalFire { get; set; }

        public Boolean PriorityFlag { get; set; }

        public Int32 Priority { get; set; }

        #region 辅助属性

        public Boolean IsFirst { get; set; }

        public Boolean IsLast { get; set; }

        public Int32 RowIndex { get; set; }

        public Int32 ColumnIndex { get; set; }

        #endregion

        public static void Init()
        {
            Instances = new List<BaseAction>();
            var dicAction = new Dictionary<string, List<string>>();
            StreamReader sr = new StreamReader(Config.Instance.ConfigPath);
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
                        //curExcept.Add(cmdKey[0]);
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
    }
}
