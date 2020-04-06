using System.Text;
using System.Threading.Tasks;

namespace Cherry.Db.Opt.Selector
{
    public class DbSelectorMysql<T> :DbSelector<T> where T : DbContext<T>, new()
    {
        public override DbSelector<T> Limit(int len, int sIndex = 0)
        {
            LimitStr = $" limit {sIndex},{len}";
            return this;
        }

        public override bool CallProcBool(string procName, params object[] args)
        {
            var sb = new StringBuilder($"call {procName}(");
            DbAnalysis<T>.FormatProcArgs(sb, args);
            var res = DbContext<T>.DbTool.Run(sb.Append($")"));
            return res > -1;
        }

        public override TR CallProc<TR>(string procName, params object[] args)
        {
            var sb = new StringBuilder($"call {procName}(");
            DbAnalysis<T>.FormatProcArgs(sb, args);
            return DbContext<T>.DbTool.Run<TR>(sb.Append($")"));
        }

        public override T[] CallProc(string procName, params object[] args)
        {
            InitCols();

            var sb = new StringBuilder($"call {procName}(");
            DbAnalysis<T>.FormatProcArgs(sb, args); 
            var tbl = DbContext<T>.DbTool.Select(sb);
            var ls = FillTbl(tbl);
            return ls;
        }
         

        /// <summary>
        /// 仅mysql支持
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public override async Task<bool> CallProcBoolAsync(string procName, params object[] args)
        {
            var sb = new StringBuilder($"call {procName}(");
            DbAnalysis<T>.FormatProcArgs(sb, args);
            var res = await DbContext<T>.DbTool.RunAsync(sb.Append($")"));
            return res > -1;
        }
        /// <summary>
        /// 仅mysql支持
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public override Task<TR> CallProcAsync<TR>(string procName, params object[] args)
        {
            var sb = new StringBuilder($"call {procName}(");
            DbAnalysis<T>.FormatProcArgs(sb, args);
            return DbContext<T>.DbTool.RunAsync<TR>(sb.Append($")"));
        }

        /// <summary>
        /// 仅mysql支持
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public override async Task<T[]> CallProcAsync(string procName, params object[] args)
        {
            InitCols();

            var sb = new StringBuilder($"call {procName}(");
            DbAnalysis<T>.FormatProcArgs(sb, args); 
            var tbl = await DbContext<T>.DbTool.SelectAsync(sb);
            var ls = FillTbl(tbl);
            return ls;
        }
         
    }
}