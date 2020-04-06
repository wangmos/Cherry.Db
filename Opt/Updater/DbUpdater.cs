using Cherry.Db.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable PossibleNullReferenceException

namespace Cherry.Db.Opt.Updater
{
    public class DbUpdater<T> where T : DbContext<T>, new()
    { 
        protected readonly Dictionary<string, DbAnalysis<T>.DbAnalysisInfo> Cols = new Dictionary<string, DbAnalysis<T>.DbAnalysisInfo>();
        protected readonly StringBuilder WhereSb = new StringBuilder();
        protected readonly StringBuilder SetSb = new StringBuilder();
         
        public DbUpdater<T> Clear()
        {
            Cols.Clear();
            WhereSb.Clear();
            SetSb.Clear(); 
            return this;
        } 

        public DbUpdater<T> Col(Expression<Func<T, object>> cols)
        {
            DbAnalysis<T>.Col(cols, Cols, false);
            return this;
        }

        /// <summary>
        /// 仅mysql支持 
        /// update set col case matchCol when key then val else defVal end
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="col"></param>
        /// <param name="matchCol"></param>
        /// <param name="pattern"></param>
        /// <param name="defVal"></param>
        public virtual DbUpdater<T> Set(Expression<Func<T, object>> col, Expression<Func<T, object>> matchCol,
            Expression<Func<T, object>> defVal,
            params (Expression<Func<T, object>>, Expression<Func<T, object>>)[] pattern)
        {
            throw new NotImplementedException();
        }

        public virtual DbUpdater<T> Set(Expression<Func<T, object>> col, Expression<Func<T, object>> val)
        {
            var colName = DbAnalysis<T>.Col(col, false);
            SetSb.Append($"`{colName}` = ");
            DbAnalysis<T>.Where(val.Body, SetSb);
            SetSb.Append(",");
            return this;
        }

        /// <summary>
        /// 尽量不要在表达式内创建对象
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public virtual DbUpdater<T> Where(Expression<Func<T, bool>> where)
        {
            DbAnalysis<T>.Where(where, WhereSb);
            return this;
        }

        public virtual DbUpdater<T> Where(string whereSql)
        {
            WhereSb.Clear();
            WhereSb.Append($" {whereSql}");
            return this;
        }

        protected virtual void InitCols()
        {
            if (Cols.Count == 0)
            {
                foreach (var col in DbAnalysis<T>.ColCols)
                {
                    Cols[col.Value.ColName] = col.Value;
                }
            }
            else Cols.Remove(DbAnalysis<T>.KeyMemName);
        }
         
        public virtual bool Update()
        {
            if (SetSb.Length == 0) return false;

            var sql = Sql();

            return DbContext<T>.DbTool.Run(sql) > 0;
        }

        protected StringBuilder Sql()
        {
            var sb = new StringBuilder($"update `{DbAnalysis<T>.TblName}` set {SetSb}").RemoveLast()
                .Append(WhereSb.Length == 0 ? "" : $" where {WhereSb}");
            return sb;
        }


        /// <summary>
        /// 设置了where 则使用对象数据更新整张表
        /// 否则只更新对象一条数据
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual bool Update(T t)
        {
            InitCols();

            var sql = Sql(t);

            return DbContext<T>.DbTool.Run(sql) > 0;
        } 

        private StringBuilder Sql(T t)
        {

            var sb = new StringBuilder($"update `{DbAnalysis<T>.TblName}` set ");

            foreach (var col in Cols)
            {
                sb.Append($"`{col.Key}`=");

                lock (t)
                {
                    var val = col.Value.Getter(t);
                    DbAnalysis<T>.FormatVal(sb, val);
                }

                sb.Append(",");
            }

            object keyVal = null;

            if(WhereSb.Length == 0)
                lock (t)
                {
                    keyVal = DbAnalysis<T>.KeyInfo.Getter(t);
                }

            sb.RemoveLast()
                .Append(WhereSb.Length == 0
                    ? $" where `{DbAnalysis<T>.KeyMemName}`={DbAnalysis<T>.FormatVal(keyVal)}"
                    : $" where {WhereSb}");

            return sb;
        }
         
        /// <summary>
        /// 批量更新
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public virtual bool Update(List<T> ts)
        { 
            if (ts.Count == 0) return false;

            InitCols();

            var sqlLs = GetSql(ts);

            return DbContext<T>.DbTool.UseTrans(sqlLs);
        }
         
        protected virtual IEnumerable<StringBuilder> GetSql(List<T> ts)
        { 
            var sqlLs = new ConcurrentBag<StringBuilder>();

            DbContext<T>.BatchWork(ts.Count, (index, len) =>
            {
                for (int i = 0; i < len; i++)
                {
                    sqlLs.Add(Sql(ts[index + i]));
                }
            });

            return sqlLs;
        }
         

        /// <summary>
        /// 仅mysql支持
        /// 使用set,where语句更新数据表
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> UpdateAsync()
        {
            if (SetSb.Length == 0) return false;

            var sql = Sql();

            return await DbContext<T>.DbTool.RunAsync(sql) > 0;
        }
        /// <summary>
        /// 设置了where 则使用对象数据更新整张表
        /// 否则只更新对象一条数据
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual async Task<bool> UpdateAsync(T t)
        {
            InitCols();

            var sql = Sql(t);

            return await DbContext<T>.DbTool.RunAsync(sql) > 0;
        }
        /// <summary>
        /// 批量更新
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public virtual Task<bool> UpdateAsync(List<T> ts)
        {
            if (ts.Count == 0) return Task.FromResult(false);

            InitCols();

            var sqlLs = GetSql(ts);

            return DbContext<T>.DbTool.UseTransAsync(sqlLs);
        }
 
    }
}