using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Cherry.Db.Tool
{
    /// <inheritdoc />
    /// <summary>
    /// 数据库操作类
    /// </summary>
    public abstract class DbTool:IDisposable
    {
        /// <summary>
        /// 发生异常时
        /// </summary>
        public static Action<Exception> OnException; 

        /// <summary>
        /// 执行sql时
        /// </summary>
        public static Action<string> OnExcuSql; 

        /// <summary>
        /// 连接字符串
        /// </summary>
        protected string ConStr;
         
        /// <summary>
        /// 记录sql
        /// </summary>
        /// <param name="sb"></param>
        internal static void LogSql(StringBuilder sb)
        {
            OnExcuSql?.Invoke(sb.Length > 100 ? sb.ToString(0, 100) : sb.ToString());
        }

        /// <summary>
        /// 返回影响的记录行数  其他返回-1
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public virtual int Run(StringBuilder sql)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 返回单个值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public virtual T Run<T>(StringBuilder sql)
        {
            throw new NotImplementedException();
        } 
         
        /// <summary>
        /// 返回读取器
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="act"></param> 
        public virtual bool Reader(StringBuilder sql, Action<IDataReader> act)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="sql"></param> 
        /// <returns></returns>
        public virtual DataTable Select(StringBuilder sql)
        {
            throw new NotImplementedException();
        } 

        /// <summary>
        /// 使用事务执行sql组
        /// </summary>
        /// <param name="sqlLs"></param>
        /// <returns></returns>
        public virtual bool UseTrans(IEnumerable<StringBuilder> sqlLs)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 开启一个事务
        /// </summary>
        /// <returns></returns>
        public virtual DbToolTrans BeginTrans()
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
        /// 返回单个值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public virtual Task<T> RunAsync<T>(StringBuilder sql)
        {
            throw new NotImplementedException();
        }
         

        /// <summary>
        /// 返回读取器
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="act"></param> 
        public virtual Task<bool> ReaderAsync(StringBuilder sql, Action<IDataReader> act)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="sql"></param> 
        /// <returns></returns>
        public virtual Task<DataTable> SelectAsync(StringBuilder sql)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 使用事务执行sql组
        /// </summary>
        /// <param name="sqlLs"></param>
        /// <returns></returns>
        public virtual Task<bool> UseTransAsync(IEnumerable<StringBuilder> sqlLs)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 开启一个事务
        /// </summary>
        /// <returns></returns>
        public virtual Task<DbToolTrans> BeginTransAsync()
        {
            throw new NotImplementedException();
        } 
 
        /// <summary>
        /// 释放所有资源
        /// </summary>
        public virtual void Dispose() { }
    }
}