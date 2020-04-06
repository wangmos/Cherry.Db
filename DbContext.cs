using Cherry.Db.Opt.Deleter;
using Cherry.Db.Opt.Inserter;
using Cherry.Db.Opt.Selector;
using Cherry.Db.Opt.Updater;
using Cherry.Db.Tool;
using Cherry.Db.Tool.Access;
using Cherry.Db.Tool.Mysql;
using System;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Cherry.Db.Utils;


namespace Cherry.Db
{
    /// <summary>
    /// 主键推荐int类型 非数字型请重写GenKey函数对主键赋值 每个引用类型的成员必须初始化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DbContext<T> where T : DbContext<T>, new()
    {   
        public static DbTool DbTool;
        public static DbConfig Config;
        public static int MaxDbId;
        private static Func<DbSelector<T>> genSelector;
        private static Func<DbInserter<T>> genInserter;
        private static Func<DbUpdater<T>> genUpdater;
        private static Func<DbDeleter<T>> genDeleter; 

        public static void Init(DbConfig config)
        {
            Config = config;

            switch (Config.Type)
            {
                case DbType.Mysql:
                    DbTool = new DbToolMysql(config);
                    genSelector = ()=> new DbSelectorMysql<T>();
                    genInserter = ()=> new DbInserterMysql<T>(); 
                    genUpdater = ()=> new DbUpdaterMysql<T>();
                    genDeleter = ()=> new DbDeleter<T>();
                    break;
                case DbType.Access:
                    DbTool = new DbToolAccess(config);
                    genSelector = () => new DbSelector<T>();
                    genInserter = () => new DbInserter<T>();
                    genUpdater = () => new DbUpdater<T>();
                    genDeleter = () => new DbDeleter<T>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"不支持的数据库类型:{Config.Type}");
            } 

            if(config.DigitalKey)
                MaxDbId = DbTool.Run<int>(new StringBuilder($"select max(`{DbAnalysis<T>.KeyInfo.ColName}`) from {DbAnalysis<T>.TblName}"));
        }

        /// <summary>
        /// 主键赋值
        /// </summary>
        public virtual void GenKey()
        {
            if (!Config.DigitalKey || (int)DbAnalysis<T>.KeyInfo.Getter((T)this) != 0) return;

            lock (DbTool)
            {
                MaxDbId += 1;
                DbAnalysis<T>.KeyInfo.Setter((T)this, MaxDbId);
            }
        }

        public static void BatchWork(int totalNum, Action<int, int> act)
        {
            ParallelEx.ForRange(0, totalNum, Config.AsyncNum, act);
        }

        public static DbSelector<T> Selector => genSelector(); 
        public static DbInserter<T> Inserter => genInserter();
        public static DbUpdater<T> Updater => genUpdater();
        public static DbDeleter<T> Deleter => genDeleter();
         
        public bool Insert(Expression<Func<T, object>> cols = null)
        {
            return cols == null
                ? Inserter.Insert((T)this)
                : Inserter.Col(cols).Insert((T)this);
        }

        public bool Update(Expression<Func<T, object>> cols = null)
        {
            return cols == null
                ? Updater.Update((T) this)
                : Updater.Col(cols).Update((T) this);
        }

        public bool Delete()
        {
            return Deleter.Delete((T)this);
        }

        public static int Count(Expression<Func<T, bool>> where)
        {
            var sb = new StringBuilder($"select count(1) from {DbAnalysis<T>.ViewTblName} where ");
            DbAnalysis<T>.Where(where.Body, sb);
            return (int)DbTool.Run<long>(sb); 
        }

        public static bool Exists(Expression<Func<T, bool>> where)
        { 
            return Count(where) > 0;
        }

        public static T One(Expression<Func<T, bool>> where)
        {
            return Selector.Where(where).One();
        } 

        public Task<bool> InsertAsync(Expression<Func<T, object>> cols = null)
        {
            return cols == null 
                ? Inserter.InsertAsync((T)this) 
                : Inserter.Col(cols).InsertAsync((T)this);
        } 

        public Task<bool> UpdateAsync(Expression<Func<T, object>> cols = null)
        {
            return cols == null
                ? Updater.UpdateAsync((T)this)
                : Updater.Col(cols).UpdateAsync((T)this);
        }

        public Task<bool> DeleteAsync()
        {
            return Deleter.DeleteAsync((T)this);
        }

        public static async Task<int> CountAsync(Expression<Func<T, bool>> where)
        {
            var sb = new StringBuilder($"select count(1) from {DbAnalysis<T>.ViewTblName} where ");
            DbAnalysis<T>.Where(where.Body, sb);
            return (int)(await DbTool.RunAsync<long>(sb));
        }

        public static async Task<bool> ExistsAsync(Expression<Func<T, bool>> where)
        {
            return await CountAsync(where) > 0;
        }

        public static Task<T> OneAsync(Expression<Func<T, bool>> where)
        {
            return Selector.Where(where).OneAsync(); 
        }
         
    }
}