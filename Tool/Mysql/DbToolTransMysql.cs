using System;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Cherry.Db.Tool.Mysql
{
    public class DbToolTransMysql : DbToolTrans
    {
        private MySqlConnection _connection;
        private MySqlCommand _command;
        private MySqlTransaction _transaction;

        internal static DbToolTrans Create(string conStr)
        {
            MySqlConnection connection = null;
            MySqlCommand command = null;
            MySqlTransaction transaction = null;
            try
            {
                connection = new MySqlConnection(conStr);
                connection.Open();
                transaction = connection.BeginTransaction();
                command = connection.CreateCommand(); 
                command.Transaction = transaction;
                return new DbToolTransMysql()
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
            MySqlConnection connection = null;
            MySqlCommand command = null;
            MySqlTransaction transaction = null;
            try
            {
                connection = new MySqlConnection(conStr);
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();
                command = connection.CreateCommand();
                command.Transaction = transaction;
                return new DbToolTransMysql()
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