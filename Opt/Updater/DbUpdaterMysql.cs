
// ReSharper disable PossibleNullReferenceException

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Cherry.Db.Utils;

namespace Cherry.Db.Opt.Updater
{
    public class DbUpdaterMysql<T>:DbUpdater<T> where T : DbContext<T>, new()
    {
        /// <summary>
        /// 仅mysql支持 
        /// update set col case matchCol when key then val else defVal end
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="col"></param>
        /// <param name="matchCol"></param>
        /// <param name="pattern"></param>
        /// <param name="defVal"></param>
        public override DbUpdater<T> Set(Expression<Func<T, object>> col, Expression<Func<T, object>> matchCol,
            Expression<Func<T, object>> defVal, 
            params (Expression<Func<T, object>>, Expression<Func<T, object>>)[] pattern)
        {
            var colName = DbAnalysis<T>.Col(col, false);
            var colName2 = matchCol != null 
                ? DbAnalysis<T>.Col(matchCol, true) : null;

            SetSb.Append($"`{colName}` = case ");
            if (colName2 != null) SetSb.Append($"`{colName2}`");

            foreach (var kv in pattern)
            {
                SetSb.Append(" when ");
                DbAnalysis<T>.Where(kv.Item1.Body, SetSb);
                SetSb.Append(" then ");
                DbAnalysis<T>.Where(kv.Item2.Body, SetSb);
            }

            SetSb.Append(" else ");
            if (defVal != null) DbAnalysis<T>.Where(defVal.Body, SetSb);
            else SetSb.Append($"`{colName}`");

            SetSb.Append(" end,");
            return this;
        }
          
        public override bool Update(List<T> ts)
        {
            if (ts.Count == 0) return false;
             
            var sqlLs = GetSql(ts);

            return DbContext<T>.DbTool.UseTrans(sqlLs);
        }

        protected override IEnumerable<StringBuilder> GetSql(List<T> ts)
        {
            var noCol = Cols.Count == 0;

            InitCols();
            Cols.Add(DbAnalysis<T>.KeyMemName, DbAnalysis<T>.KeyInfo);

            var sqlLs = new ConcurrentBag<StringBuilder>();

            Func<List<T>, int, int, StringBuilder> func;
            if (noCol)
                func = SqlMysql;
            else func = SqlMysql2;

            DbContext<T>.BatchWork(ts.Count, (index, len) =>
            {
                sqlLs.Add(func(ts, index, len));
            });

            return sqlLs;
        }

        /// <summary>
        /// 使用replace into 
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="index"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        private StringBuilder SqlMysql(List<T> ts, int index, int len)
        {  
            var sb = new StringBuilder($"replace into `{DbAnalysis<T>.TblName}`(");
        
            foreach (var col in Cols.Keys)
            {
                sb.Append($"`{col}`,"); 
            }

            sb.RemoveLast().Append(")values");

            for (var i = 0; i < len; i++)
            {
                sb.Append("(");
                var t = ts[index + i];
                foreach (var mem in Cols.Values)
                {
                    lock (t)
                    {
                        var val = mem.Getter(t);
                        DbAnalysis<T>.FormatVal(sb, val);
                    }
                    sb.Append(",");
                }

                sb.RemoveLast().Append("),");
            }

            sb.RemoveLast();

            return sb;
        }

        /// <summary>
        /// 使用insert into ......on duplicate key update .....
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="index"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        private StringBuilder SqlMysql2(List<T> ts, int index, int len)
        {  
            var sb = new StringBuilder($"insert into `{DbAnalysis<T>.TblName}`(");
         
            foreach (var col in Cols.Keys)
            {
                sb.Append($"`{col}`,"); 
            }

            sb.RemoveLast().Append(")values");

            for (var i = 0; i < len; i++)
            {
                sb.Append("(");
                var t = ts[index + i];
                foreach (var mem in Cols.Values)
                {
                    lock (t)
                    {
                        var val = mem.Getter(t);
                        DbAnalysis<T>.FormatVal(sb, val);
                    }
                    sb.Append(",");
                }

                sb.RemoveLast().Append("),");
            }

            sb.RemoveLast().Append(" on duplicate key update ");
             
            foreach (var col in Cols.Keys)
            {
                if(col == DbAnalysis<T>.KeyMemName) continue;

                sb.Append($"`{col}`=values(`{col}`),");
            }

            return sb.RemoveLast();
        }
           
        public override Task<bool> UpdateAsync(List<T> ts)
        {
            if (ts.Count == 0) return Task.FromResult(false);

            var sqlLs = GetSql(ts);

            return DbContext<T>.DbTool.UseTransAsync(sqlLs);
        }
 
    }
}