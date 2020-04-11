using System;
using System.Threading.Tasks;

namespace Cherry.Db.Utils
{
    internal struct TimeOut
    {
        private int _lastTime;
        private int _seq;
        private DateTime _lastDateTime;
         

        /// <summary>
        /// 启动一个计时器
        /// </summary>
        /// <returns></returns>
        public static TimeOut Start()
        {
            var timeOut = new TimeOut
            {
                _lastDateTime = DateTime.Now
            };
            timeOut.ReStart();
            return timeOut;
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        /// <returns></returns>
        public TimeOut ReStart()
        {
            _lastTime = Environment.TickCount;
            _seq = 0;
            return this;
        }

        /// <summary>
        /// 是否超时
        /// </summary>
        /// <param name="millSecs"></param>
        /// <returns></returns>
        public bool IsTimeOut(int millSecs)
        {
            var nowT = Environment.TickCount;
            var t = nowT - _lastTime + _seq;
            if (t < millSecs) return false;
            _lastTime = nowT;
            _seq = t - millSecs;
            return true;
        }

        /// <summary>
        /// 当前间隔
        /// </summary>
        public long ElapsedMilliseconds => Environment.TickCount - _lastTime;


        public override string ToString() => 
            TimeSpan.FromMilliseconds(ElapsedMilliseconds).ToString(@"dd\.hh\:mm\:ss");


        /// <summary>
        /// 测试运行时长
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="act"></param> 
        /// <returns></returns>
        public void CheckFuncExcTime(string msg, Action act)
        {
            ReStart();
            act();
            Console.WriteLine($@"{msg} 运行共耗时:{ElapsedMilliseconds}ms"); 
        }

#if !net4
        public async Task CheckFuncExcTime(string msg, Func<Task> act)
        {
            ReStart();
            await act();
            Console.WriteLine($@"{msg} 运行共耗时:{ElapsedMilliseconds}ms");
        }  
#endif

        /// <summary>
        /// 是否超过今天时间点
        /// </summary>
        /// <param name="pointSecs"></param>
        /// <returns></returns>
        public bool IsPassTodaySecs(int pointSecs)
        {
            var p1 = (int)(_lastDateTime - DateTime.Today).TotalSeconds;
            var p2 = (int)(DateTime.Now - DateTime.Today).TotalSeconds;
            if (p1 < pointSecs && p2 >= pointSecs)
            {
                _lastDateTime = DateTime.Now;
                return true;
            }
            return false;
        }
    }
}