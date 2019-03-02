using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Config
    {
        public static Config Instance
        {
            get
            {
                if (_instance == null && flagInited)
                {
                    lock (locker)
                    {
                        if (_instance == null)
                        {
                            _instance = new Config();
                            flagInited = true;
                        }
                    }
                }
                return _instance;
            }
        }

        private static bool flagInited = false;
        private static object locker = new object();
        private static Config _instance = null;

        private Config()
        {

        }

        public String ConfigPath { get {
                return "aconfg.txt";
            } }

        public Boolean Debug { get; set; }

        public String DMWorkPath { get; set; }

        public String DictPath { get; set; }

        public String DMDictDescFile { get; set; }
    }

    static class Exc
    {
        /// <summary>
        /// 利用反射来判断对象是否包含某个属性
        /// </summary>
        /// <param name="instance">object</param>
        /// <param name="propertyName">需要判断的属性</param>
        /// <returns>是否包含</returns>
        public static bool ContainProperty(this object instance, string propertyName)
        {
            if (instance != null && !string.IsNullOrEmpty(propertyName))
            {
                PropertyInfo _findedPropertyInfo = instance.GetType().GetProperty(propertyName);
                return (_findedPropertyInfo != null);
            }
            return false;
        }

        /// <summary>
        /// 利用反射赋值
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public static void SetValue(this object instance, string propertyName, object value)
        {
            Type type = instance.GetType();
            var ps = type.GetProperties();
            foreach (var p in ps)
            {
                if(p.Name == propertyName)
                {
                    var propertyType = p.PropertyType.FullName;
                    switch (propertyType)
                    {
                        case "System.String":
                            p.SetValue(instance, value.ToString(), null);
                            break;
                        case "System.Boolean":
                            p.SetValue(instance, Boolean.Parse(value.ToString()), null);
                            break;
                        default:break;
                    }
                    break;
                }
            }
        }
    }
}
