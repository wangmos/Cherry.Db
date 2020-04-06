using Cherry.Db.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks; 

namespace Cherry.Db.Opt.Selector
{
    public class DbSelector<T> where T : DbContext<T>, new()
    {
        /// <summary>
        /// 分页数据
        /// </summary>
        public PageInfo PageInfo = new PageInfo();

        protected readonly Dictionary<string, DbAnalysis<T>.DbAnalysisInfo> Cols =
            new Dictionary<string, DbAnalysis<T>.DbAnalysisInfo>();

        private readonly HashSet<string> _min = new HashSet<string>();
        private readonly HashSet<string> _count = new HashSet<string>();
        private readonly HashSet<string> _max = new HashSet<string>();
        private readonly HashSet<string> _sum = new HashSet<string>();
        private readonly HashSet<string> _avg = new HashSet<string>();
        private readonly HashSet<string> _orderBy = new HashSet<string>();
        private readonly HashSet<string> _groupBy = new HashSet<string>();
        private readonly StringBuilder _where = new StringBuilder();
        protected string LimitStr;
        private bool _asc;

        public DbSelector<T> Clear()
        {
            Cols.Clear();
            _min.Clear();
            _max.Clear();
            _sum.Clear();
            _avg.Clear();
            _orderBy.Clear();
            _groupBy.Clear();
            _where.Clear();
            LimitStr = null;
            return this;
        }

        public DbSelector<T> Col(Expression<Func<T, object>> cols)
        { 
            DbAnalysis<T>.Col(cols, Cols, true);
            return this;
        }

        /// <summary>
        /// 列名自动添加_min后缀
        /// </summary>
        /// <param name="cols"></param>
        /// <returns></returns>
        public virtual DbSelector<T> Min(Expression<Func<T, object>> cols)
        {
            DbAnalysis<T>.Col(cols, _min, true);
            return this;
        }
        /// <summary>
        /// 列名自动添加_count后缀
        /// </summary>
        /// <param name="cols"></param>
        /// <returns></returns>
        public virtual DbSelector<T> Count(Expression<Func<T, object>> cols)
        {
            DbAnalysis<T>.Col(cols, _count, true);
            return this;
        }

        /// <summary>
        /// 列名自动添加_max后缀
        /// </summary>
        /// <param name="cols"></param>
        /// <returns></returns>
        public virtual DbSelector<T> Max(Expression<Func<T, object>> cols)
        {
            DbAnalysis<T>.Col(cols, _max, true);
            return this;
        }

        /// <summary>
        /// 列名自动添加_sum后缀
        /// </summary>
        /// <param name="cols"></param>
        /// <returns></returns>
        public virtual DbSelector<T> Sum(Expression<Func<T, object>> cols)
        {
            DbAnalysis<T>.Col(cols, _sum, true);
            return this;
        }

        /// <summary>
        /// 列名自动添加_avg后缀
        /// </summary>
        /// <param name="cols"></param>
        /// <returns></returns>
        public virtual DbSelector<T> Avg(Expression<Func<T, object>> cols)
        {
            DbAnalysis<T>.Col(cols, _avg, true);
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="asc"></param>
        /// <param name="cols"></param>
        /// <returns></returns>
        public virtual DbSelector<T> OrderBy(bool asc, Expression<Func<T, object>> cols)
        {
            _asc = asc;
            DbAnalysis<T>.Col(cols, _orderBy, true);
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="cols"></param>
        /// <returns></returns>
        public virtual DbSelector<T> GroupBy(Expression<Func<T, object>> cols)
        {
            PageInfo.SetTotalNum(0);
            DbAnalysis<T>.Col(cols, _groupBy, true);
            return this;
        }

        /// <summary>
        /// 尽量不要在表达式内创建对象或者复杂的表达式
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public virtual DbSelector<T> Where(Expression<Func<T, bool>> where)
        {
            PageInfo.SetTotalNum(0);
            DbAnalysis<T>.Where(where, _where);
            return this;
        }

        public virtual DbSelector<T> Where(string whereSql)
        {
            PageInfo.SetTotalNum(0);
            _where.Clear();
            _where.Append($" {whereSql}");
            return this;
        }

        /// <summary>
        /// access 不支持
        /// </summary>
        /// <param name="len"></param>
        /// <param name="sIndex"></param>
        /// <returns></returns>
        public virtual DbSelector<T> Limit(int len, int sIndex = 0)
        {
            return this;
        }

        protected virtual void InitCols()
        {
            if (Cols.Count == 0 && _min.Count == 0 && _max.Count == 0
                && _avg.Count == 0 && _sum.Count == 0 && _count.Count == 0)
            {
                foreach (var col in DbAnalysis<T>.ColCols)
                {
                    Cols[col.Value.ColName] = col.Value;
                }

                foreach (var col in DbAnalysis<T>.ColView)
                {
                    Cols[col.Value.ColName] = col.Value;
                }
            }

            Cols[DbAnalysis<T>.KeyMemName] = DbAnalysis<T>.KeyInfo;
        }

        private StringBuilder Sql
        {
            get
            {
                var sb = new StringBuilder("select ");

                foreach (var col in Cols.Keys)
                {
                    sb.Append($"`{col}`,");
                }

                foreach (var col in _min)
                {
                    sb.Append($"min(`{col}`) `{col}_min`,");
                }

                foreach (var col in _count)
                {
                    sb.Append($"count(`{col}`) `{col}_count`,");
                }

                foreach (var col in _max)
                {
                    sb.Append($"max(`{col}`) `{col}_max`,");
                }

                foreach (var col in _avg)
                {
                    sb.Append($"avg(`{col}`) `{col}_avg`,");
                }

                foreach (var col in _sum)
                {
                    sb.Append($"sum(`{col}`) `{col}_sum`,");
                }

                sb.RemoveLast().Append($" from `{DbAnalysis<T>.ViewTblName}`");

                if (_where.Length > 0)
                {
                    sb.Append($" where {_where}");
                }

                if (_groupBy.Count > 0)
                {
                    sb.Append(" group by ");
                    foreach (var col in _groupBy)
                    {
                        sb.Append($"`{col}`,");
                    }

                    sb.RemoveLast();
                }

                if (_orderBy.Count > 0)
                {
                    sb.Append(" order by ");
                    foreach (var col in _orderBy)
                    {
                        sb.Append($"`{col}`,");
                    }

                    sb.RemoveLast().Append(_asc ? "" : " desc");
                }


                //检查分页
                if (PageInfo.PerPageNum > 0)
                {
                    Limit(PageInfo.PerPageNum, PageInfo.StartIndex);
                }
                
                if (LimitStr?.Length > 0)
                {
                    sb.Append(LimitStr);
                }

                return sb;
            }
        }

        public T One()
        {
            Limit(1);
            var ls = ToList();
            if (ls.Length > 0) return ls[0];
            return default(T);
        }

        /// <summary>
        /// 单线程 更省内存 
        /// </summary>
        /// <param name="act"></param>
        public virtual void Foreach(Action<T> act)
        {
            InitCols();

            if (PageInfo.PerPageNum > 0)
            {
                PageInfo.SetTotalNum(Count());
            }

            DbContext<T>.DbTool.Reader(Sql, (reader) => { FillReader(act, reader); });
        }

        protected static void FillReader(Action<T> act, IDataReader reader)
        {
            var ls = new List<DbAnalysis<T>.DbAnalysisInfo>(reader.FieldCount);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                DbAnalysis<T>.GetColInfo(reader.GetName(i), out var mem, true);
                ls.Add(mem);
            }

            using (reader)
            {
                while (reader.Read())
                {
                    var t = new T();

                    for (var index = 0; index < ls.Count; index++)
                    {
                        var memberInfo = ls[index];
                        DbAnalysis<T>.FillT(memberInfo, t, reader.GetValue(index));
                    }

                    act(t);
                }
            }
        }


        /// <summary>
        /// 占用内存大 多线程装载 
        /// </summary>
        /// <returns></returns>
        public virtual T[] ToList()
        {
            InitCols();

            if (PageInfo.PerPageNum > 0)
            {
                PageInfo.SetTotalNum(Count());
            }

            var to = TimeOut.Start();

            var tbl = DbContext<T>.DbTool.Select(Sql);

            Console.WriteLine("GetTbl:" + to.ElapsedMilliseconds);
            to.ReStart();

            var ls = FillTbl(tbl);

            Console.WriteLine("FillTbl:" + to.ElapsedMilliseconds);

            return ls;
        }

        public virtual TV[] ToList<TV>()
        {
            if (Cols.Count != 1) return null;

            if (PageInfo.PerPageNum > 0)
            {
                PageInfo.SetTotalNum(Count());
            }

            var tbl = DbContext<T>.DbTool.Select(Sql);

            var ls = new TV[tbl?.Rows.Count ?? 0];

            if (tbl?.Rows.Count > 0)
            {
                var valType = Cols.Values.ElementAt(0).ValType;
                Parallel.For(0, tbl.Rows.Count, index =>
                {
                    var val = tbl.Rows[index];
                    ls[index] = (TV) DbAnalysis<T>.GetDbVal(valType, val[0]);
                });
            }

            return ls;
        }

        protected static T[] FillTbl(DataTable tbl)
        {
            var ls = new T[tbl?.Rows.Count ?? 0];
            if (tbl?.Rows.Count > 0)
            {
                Parallel.For(0, tbl.Rows.Count, index =>
                {
                    var tblRow = tbl.Rows[index];
                    var t = new T();
                    DbAnalysis<T>.FillT(t, tblRow);
                    ls[index] = t;
                });

                tbl.Clear();
            }

            return ls;
        }

        /// <summary>
        /// 仅mysql支持
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual bool CallProcBool(string procName, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 仅mysql支持
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual TR CallProc<TR>(string procName, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 仅mysql支持
        /// </summary> 
        /// <param name="procName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual T[] CallProc(string procName, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 更省内存 单线程 
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public virtual async Task ForeachAsync(Action<T> act)
        {
            InitCols();

            if (PageInfo.PerPageNum > 0)
            {
                PageInfo.SetTotalNum(await CountAsync());
            }

            await DbContext<T>.DbTool.ReaderAsync(Sql, (reader) => { FillReader(act, reader); });
        }


        /// <summary>
        /// 占用内存大 多线程装载 
        /// </summary>
        /// <returns></returns>
        public virtual async Task<T[]> ToListAsync()
        {
            InitCols();

            if (PageInfo.PerPageNum > 0)
            {
                PageInfo.SetTotalNum(await CountAsync());
            }

            var tbl = await DbContext<T>.DbTool.SelectAsync(Sql);

            var ls = FillTbl(tbl);

            return ls;
        }


        /// <summary>
        /// 仅mysql支持
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Task<bool> CallProcBoolAsync(string procName, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 仅mysql支持
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Task<TR> CallProcAsync<TR>(string procName, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 仅mysql支持
        /// </summary> 
        /// <param name="procName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Task<T[]> CallProcAsync(string procName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public async Task<T> OneAsync()
        {
            Limit(1);
            var ls = await ToListAsync();
            if (ls.Length > 0) return ls[0];
            return default(T);
        }

        public virtual async Task<TV[]> ToListAsync<TV>()
        {
            if (Cols.Count != 1) return null;

            if (PageInfo.PerPageNum > 0)
            {
                PageInfo.SetTotalNum(await CountAsync());
            }

            var tbl = await DbContext<T>.DbTool.SelectAsync(Sql);

            var ls = new TV[tbl?.Rows.Count ?? 0];

            if (tbl?.Rows.Count > 0)
            {
                var valType = Cols.Values.ElementAt(0).ValType;
                Parallel.For(0, tbl.Rows.Count, index =>
                {
                    var val = tbl.Rows[index];
                    ls[index] = (TV) DbAnalysis<T>.GetDbVal(valType, val[0]);
                });
            }

            return ls;
        }
         
        private StringBuilder CountSql
        {
            get
            {
                var sb = new StringBuilder($"select count(1) from {DbAnalysis<T>.ViewTblName}");
                if (_where.Length > 0)
                {
                    sb.Append($" where {_where}");
                }

                if (_groupBy.Count > 0)
                {
                    sb.Append(" group by ");
                    foreach (var col in _groupBy)
                    {
                        sb.Append($"`{col}`,");
                    }

                    sb.RemoveLast();
                }

                return sb;
            }
        }

        public int Count()
        {
            var sb = _groupBy.Count > 0
                ? new StringBuilder($"select count(1) from ({CountSql}) t")
                : CountSql;

            return (int) DbContext<T>.DbTool.Run<long>(sb);
        }

        public async Task<int> CountAsync()
        {
            var sb = _groupBy.Count > 0
                ? new StringBuilder($"select count(1) from ({CountSql}) t")
                : CountSql;

            return (int) (await DbContext<T>.DbTool.RunAsync<long>(sb));
        }
         
    }
}