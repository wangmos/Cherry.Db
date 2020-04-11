using System;
using System.Threading.Tasks;

namespace Cherry.Db.Utils
{
    internal static class ParallelEx
    { 
        /// <summary>
        /// 并行平均按任务量执行任务
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="perTaskNum"></param> 
        /// <param name="act"></param>
        internal static void ForRange(int startIndex, int endIndex, int perTaskNum, Action<int, int> act)
        {
            ForRange(startIndex, endIndex, perTaskNum, 0, (si, len, tag) => act(si, len));
        }

        /// <summary>
        /// 并行平均按任务量执行任务 
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="perTaskNum"></param>
        /// <param name="tag"></param>
        /// <param name="act"></param>
        internal static void ForRange<T>(int startIndex, int endIndex, int perTaskNum, T tag, Action<int, int, T> act)
        {
            var len = endIndex - startIndex;
            if (len > perTaskNum)
            {
                var times = len / perTaskNum;
                if (times == 1) times = 2;
                perTaskNum = len / times;
                var left = len % perTaskNum;

                Parallel.For(0, times, i =>
                {
                    var si = startIndex + i * perTaskNum;
                    var taskNum = perTaskNum;

                    if (i <= left)
                    {
                        si += i;
                        if (i < left)
                        {
                            taskNum += 1;
                        }
                    }

                    act(si, taskNum, tag);
                });
            }
            else
            {
                act(startIndex, len, tag);
            }
        }


    }
}