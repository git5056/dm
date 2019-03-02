using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class DMDictDesc
    {
        public static DMDictDesc Instance
        {
            get
            {
                if (_instance == null && flagInited)
                {
                    lock (locker)
                    {
                        if (_instance == null)
                        {
                            _instance = new DMDictDesc();
                            flagInited = true;
                        }
                    }
                }
                return _instance;
            }
        }

        private static bool flagInited = false;
        private static object locker = new object();
        private static DMDictDesc _instance = null;

        private DMDictDesc()
        {

        }
    }
}
