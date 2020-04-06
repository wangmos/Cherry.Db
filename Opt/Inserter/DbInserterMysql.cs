using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Cherry.Db.Utils;

// ReSharper disable PossibleNullReferenceException

namespace Cherry.Db.Opt.Inserter
{
    public class DbInserterMysql<T> : DbInserter<T> where T : DbContext<T>, new()
    {  
        protected override ConcurrentBag<StringBuilder> GetSql(List<T> ts)
        {
            var sqlLs = new ConcurrentBag<StringBuilder>();

            DbContext<T>.BatchWork(ts.Count, (index, len) =>
            {
                sqlLs.Add(SqlMysql(ts, index, len));
            }); 

            return sqlLs;
        }

        private StringBuilder SqlMysql(List<T> ts, int index, int len)
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
                t.GenKey();

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
    }
}