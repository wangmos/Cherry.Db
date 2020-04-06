using System;
using System.Text;
using System.Threading.Tasks;

namespace Cherry.Db.Tool
{
    public abstract class DbToolTrans : IDisposable
    {    
        /// <summary>
        /// 对记录有影响的返回行数  其他返回-1
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public virtual int Run(StringBuilder sql)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public virtual void Commit()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 事务回滚
        /// </summary>
        public virtual void Rollback()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// 对记录有影响的返回行数  其他返回-1
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public virtual Task<int> RunAsync(StringBuilder sql)
        {
            throw new NotImplementedException();
        }    
 
        /// <summary>
        /// 释放所有资源
        /// </summary>
        public virtual void Dispose() { }
    }
}