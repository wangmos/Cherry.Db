using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Cherry.Db.Utils;

namespace Cherry.Db.Opt.Deleter
{
    public class DbDeleter<T> where T : DbContext<T>, new()
    { 
        private readonly StringBuilder _where = new StringBuilder();

        public DbDeleter<T> Clear()
        { 
            _where.Clear();
            return this;
        }

        /// <summary>
        /// 尽量不要在表达式内创建对象
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public virtual DbDeleter<T> Where(Expression<Func<T, bool>> where)
        {
            DbAnalysis<T>.Where(where, _where);
            return this;
        }

        public virtual DbDeleter<T> Where(string whereSql)
        {
            _where.Clear();
            _where.Append($" {whereSql}");
            return this;
        }

        /// <summary> 
        /// 设置了where 则按条件删除表数据 否则删除整张表
        /// </summary> 
        /// <returns></returns>
        public virtual bool Delete()
        { 
            var sql = Sql(null);

            return DbContext<T>.DbTool.Run(sql) > 0;
        }
         
        public virtual bool Delete(T t)
        {
            var sql = Sql(t); 
            return DbContext<T>.DbTool.Run(sql) > 0;
        }
         
        /// <summary>
        /// 优先删除对象数据 其次where条件数据 最次整张表
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private StringBuilder Sql(T t)
        {
            object keyVal = null;

            if (t != null)
            {
                lock (t)
                {
                    keyVal = DbAnalysis<T>.KeyInfo.Getter(t);
                }
            }

            var sb = new StringBuilder($"delete from `{DbAnalysis<T>.TblName}`")
                .Append(keyVal == null
                    ? (_where.Length == 0 ? "" : $" where {_where}")
                    : $" where `{DbAnalysis<T>.KeyInfo.ColName}`= {DbAnalysis<T>.FormatVal(keyVal)}");

            return sb;
        }
         
        public virtual bool Delete(List<T> ts)
        {
            if (ts.Count == 0) return false;

            var sqlLs = GetSqlLs(ts);
           
            return DbContext<T>.DbTool.UseTrans(sqlLs);
        }
         
        private ConcurrentBag<StringBuilder> GetSqlLs(IReadOnlyList<T> ts)
        {   
            var sqlLs = new ConcurrentBag<StringBuilder>();

            DbContext<T>.BatchWork(ts.Count, (index, len) =>
            { 
                sqlLs.Add(Sql(ts, index, len));
            }); 

            return sqlLs;
        }

        private static StringBuilder Sql(IReadOnlyList<T> ts, int index, int len)
        {
            var sb = new StringBuilder($"delete from `{DbAnalysis<T>.TblName}` where `{DbAnalysis<T>.KeyInfo.ColName}` in (");

            for (var i = 0; i < len; i++)
            {
                var t = ts[index + i];
                lock (t) sb.Append($"{DbAnalysis<T>.FormatVal(DbAnalysis<T>.KeyInfo.Getter(t))},"); 
            }

            sb.RemoveLast().Append(")");
            return sb;
        }

 
        /// <summary>
        /// 设置了where 则按条件删除表数据 否则删除整张表
        /// </summary> 
        /// <returns></returns>
        public virtual async Task<bool> DeleteAsync()
        { 
            var sql = Sql(null);

            return await DbContext<T>.DbTool.RunAsync(sql) > 0;
        }

        public virtual async Task<bool> DeleteAsync(T t)
        {
            var sql = Sql(t); 
            return await DbContext<T>.DbTool.RunAsync(sql) > 0;
        }

        public virtual Task<bool> DeleteAsync(List<T> ts)
        {
            if (ts.Count == 0) return Task.FromResult(false);

            var sqlLs = GetSqlLs(ts);
           
            return DbContext<T>.DbTool.UseTransAsync(sqlLs);
        } 
    }
}