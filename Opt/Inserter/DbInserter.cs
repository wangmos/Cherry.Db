using Cherry.Db.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable PossibleNullReferenceException

namespace Cherry.Db.Opt.Inserter
{
    public class DbInserter<T> where T : DbContext<T>, new()
    {
        protected readonly Dictionary<string, DbAnalysis<T>.DbAnalysisInfo> Cols = new Dictionary<string, DbAnalysis<T>.DbAnalysisInfo>(); 

        public DbInserter<T> Clear()
        {
            Cols.Clear(); 
            return this;
        }

        public DbInserter<T> Col(Expression<Func<T, object>> cols)
        {
            DbAnalysis<T>.Col(cols, Cols, false);
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

            Cols[DbAnalysis<T>.KeyInfo.ColName] = DbAnalysis<T>.KeyInfo;
        }

        public virtual bool Insert(T t)
        {
            InitCols();
             
            var sql = Sql(t); 

            return DbContext<T>.DbTool.Run(sql) > 0;
        } 
          
        private StringBuilder Sql(T t)
        {
            t.GenKey();
             
            var sb = new StringBuilder($"insert into `{DbAnalysis<T>.TblName}`(");
             
            foreach (var col in Cols.Keys)
            {
                sb.Append($"`{col}`,");
            }

            sb.RemoveLast().Append(")values(");

            foreach (var mem in Cols.Values)
            {
                lock (t)
                {
                    var val = mem.Getter(t);
                    DbAnalysis<T>.FormatVal(sb, val);
                }
                sb.Append(",");
            }

            sb.RemoveLast().Append(")");

            return sb;
        }
         
        public virtual bool Insert(List<T> ts)
        {
            if (ts.Count == 0) return false;

            InitCols(); 

            var sqlLs = GetSql(ts);

            return DbContext<T>.DbTool.UseTrans(sqlLs);
        }
         
        protected virtual ConcurrentBag<StringBuilder> GetSql(List<T> ts)
        {
            var sqlLs = new ConcurrentBag<StringBuilder>();

            DbContext<T>.BatchWork(ts.Count, (index, len) =>
            {
                for (var i = 0; i < len; i++)
                {
                    sqlLs.Add(Sql(ts[index + i]));
                }
            });

            return sqlLs;
        } 
         
        public virtual async Task<bool> InsertAsync(T t)
        {
            InitCols(); 

            var sql = Sql(t);

            return await DbContext<T>.DbTool.RunAsync(sql) > 0;
        }

        public virtual async Task<bool> InsertAsync(List<T> ts)
        {
            if (ts.Count == 0) return false;

            InitCols(); 

            var sqlLs = GetSql(ts);

            return await DbContext<T>.DbTool.UseTransAsync(sqlLs);
        } 
    }
}