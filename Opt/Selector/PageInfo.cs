using System;

namespace Cherry.Db.Opt.Selector
{
    public class PageInfo
    {
        /// <summary>
        /// 每页数量
        /// </summary>
        public int PerPageNum { get; private set; }
        /// <summary>
        /// 总数量 
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// 总页码
        /// </summary>
        public int TotalPageNum { get; private set; }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurPageIndex { get; private set; }

        /// <summary>
        /// 开始索引
        /// </summary>
        internal int StartIndex { get; private set; }

        /// <summary>
        /// 重置分页 0不分页 大于0使用分页
        /// </summary>
        public void Init(int pageNum)
        {
            PerPageNum = pageNum;
            CurPageIndex = StartIndex = TotalPageNum = TotalCount = 0; 
        }

        internal void SetTotalNum(int num)
        {
            if (num == 0)
            {
                Init(PerPageNum);
                return;
            }
            TotalCount = num;
            TotalPageNum = (int)Math.Ceiling(TotalCount * 1.0 / PerPageNum);
            if (CurPageIndex > TotalPageNum)
            {
                Goto(TotalPageNum);
            }

            if (num > 0 && CurPageIndex <= 0) Goto(1);
        }
         
        public bool Goto(int pageNum)
        {
            if (CurPageIndex != pageNum && pageNum > 0 && pageNum <= TotalPageNum)
            {
                CurPageIndex = pageNum;
                StartIndex = (CurPageIndex - 1) * PerPageNum;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 首页
        /// </summary>
        /// <returns></returns>
        public bool First()
        {
            if (CurPageIndex > 1)
            {
                CurPageIndex = 1;
                StartIndex = 0;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 最后一页
        /// </summary>
        /// <returns></returns>
        public bool End()
        {
            if (TotalPageNum > CurPageIndex)
            {
                CurPageIndex = TotalPageNum;
                StartIndex = (CurPageIndex - 1) * PerPageNum;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 下一页
        /// </summary>
        /// <returns></returns>
        public bool Next()
        {
            if (CurPageIndex < TotalPageNum)
            {
                CurPageIndex++;
                StartIndex = (CurPageIndex - 1) * PerPageNum;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 上一页
        /// </summary>
        /// <returns></returns>
        public bool Previous()
        {
            if (CurPageIndex > 1)
            {
                CurPageIndex--;
                StartIndex = (CurPageIndex - 1) * PerPageNum;
                return true;
            }
            return false;
        }
    }
}