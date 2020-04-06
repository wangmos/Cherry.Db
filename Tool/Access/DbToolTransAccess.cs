using System;
using System.Data.OleDb;
using System.Text;
using System.Threading.Tasks;

namespace Cherry.Db.Tool.Access
{
    public class DbToolTransAccess : DbToolTrans
    {
        private OleDbConnection _connection;
        private OleDbCommand _command;
        private OleDbTransaction _transaction; 

        internal static DbToolTrans Create(string conStr)
        {
            OleDbConnection connection = null;
            OleDbCommand command = null;
            OleDbTransaction transaction = null;
            try
            {
                connection = new OleDbConnection(conStr);
                connection.Open();
                transaction = connection.BeginTransaction();
                command = connection.CreateCommand();
                command.Transaction = transaction;
                return new DbToolTransAccess()
                {
                    _connection = connection,
                    _command = command,
                    _transaction = transaction
                };
            }
            catch (Exception e)
            {
                transaction?.Dispose();
                command?.Dispose(); 
                connection?.Dispose();

                DbTool.OnException?.Invoke(e);
            }

            return null;
        }

        internal static async Task<DbToolTrans> CreateAsync(string conStr)
        {
            OleDbConnection connection = null;
            OleDbCommand command = null;
            OleDbTransaction transaction = null;
            try
            {
                connection = new OleDbConnection(conStr);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();
                command = connection.CreateCommand();
                command.Transaction = transaction;
                return new DbToolTransAccess()
                {
                    _connection = connection,
                    _command = command,
                    _transaction = transaction
                };
            }
            catch (Exception e)
            {
                transaction?.Dispose();
                command?.Dispose();
                connection?.Dispose();

                DbTool.OnException?.Invoke(e);
            }

            return null;
        }

        /// <summary>
        /// 对记录有影响的返回行数  其他返回-1
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override int Run(StringBuilder sql)
        {
            DbTool.LogSql(sql);

            try
            {
                return _command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                DbTool.OnException?.Invoke(e);
            }
            return -1;
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public override void Commit()
        { 
            _transaction.Commit();
        }

        public override void Rollback()
        {
            _transaction.Rollback();
        }

        /// <summary>
        /// 对记录有影响的返回行数  其他返回-1
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override async Task<int> RunAsync(StringBuilder sql)
        {
            DbTool.LogSql(sql);
            try
            {
                return await _command.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                DbTool.OnException?.Invoke(e);
            }
            return -1;
        } 

        public override void Dispose()
        {
            _transaction?.Dispose();
            _command?.Dispose();
            _connection?.Dispose();
        }
    }
}