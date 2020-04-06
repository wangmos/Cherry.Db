using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Cherry.Db.Tool.Mysql
{
    public class DbToolMysql:DbTool
    {
        public DbToolMysql(DbConfig config)
        {
            ConStr = config.ConString ?? $"Server={config.Host};Uid={config.User};Pwd={config.Pass};" +
                      $"Database={config.DbName};Port={config.Port};Charset=utf8;" +
                      $"Keepalive={int.MaxValue};ConnectionLifeTime={int.MaxValue}" +
                      ";Pooling=true";

            using (var con = new MySqlConnection(ConStr))
            {
                con.Open();
                con.Close();
            }
        }

        /// <summary>
        /// 对记录有影响的返回行数  其他返回-1
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override int Run(StringBuilder sql)
        {
            LogSql(sql);

            try
            {
                using (var con = new MySqlConnection(ConStr))
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(sql.ToString(), con))
                    {
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                OnException?.Invoke(e);
            }
            return -1;
        }

        /// <summary>
        /// count(*) = long
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override T Run<T>(StringBuilder sql)
        {
            LogSql(sql);
            try
            {
                using (var con = new MySqlConnection(ConStr))
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(sql.ToString(), con))
                    {
                        var o = cmd.ExecuteScalar();
                        if (o != DBNull.Value)
                        {
                            return (T)o;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OnException?.Invoke(e);
            }
            return default(T);
        }

        public override bool Reader(StringBuilder sql, Action<IDataReader> act)
        {
            LogSql(sql);

            try
            {
                using (var con = new MySqlConnection(ConStr))
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(sql.ToString(), con))
                    {
                        var reader = cmd.ExecuteReader();
                        act(reader);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                OnException?.Invoke(e);
            }

            return false;
        } 

        public override DataTable Select(StringBuilder sql)
        {
            LogSql(sql);
            try
            {
                using (var con = new MySqlConnection(ConStr))
                {
                    con.Open();
                    using (var da = new MySqlDataAdapter(sql.ToString(), con))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
            catch (Exception e)
            {
                OnException?.Invoke(e);
            }
            return null;
        } 

        public override bool UseTrans(IEnumerable<StringBuilder> sqlLs)
        { 
            try
            {
                using (var con = new MySqlConnection(ConStr))
                {
                    con.Open();
                    var trans = con.BeginTransaction();
                    try
                    {
                        using (var cmd = con.CreateCommand())
                        {
                            cmd.Transaction = trans; 
                            foreach (var sql in sqlLs)
                            {
                                LogSql(sql);
                                cmd.CommandText = sql.ToString();
                                cmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                        return true;
                    }
                    catch (Exception e)
                    {
                        OnException?.Invoke(e);
                    }
                    trans.Rollback();
                    return false;
                }
            }
            catch (Exception e)
            {
                OnException?.Invoke(e);
            }
            return false;
        }

        public override DbToolTrans BeginTrans()
        {
            return DbToolTransMysql.Create(ConStr);
        } 


        /// <summary>
        /// 对记录有影响的返回行数  其他返回-1
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override async Task<int> RunAsync(StringBuilder sql)
        {
            LogSql(sql);
            try
            {
                using (var con = new MySqlConnection(ConStr))
                {
                    await con.OpenAsync();
                    using (var cmd = new MySqlCommand(sql.ToString(), con))
                    {
                        return  await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception e)
            {
                OnException?.Invoke(e);
            }
            return -1;
        }

        /// <summary>
        /// 返回单个值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override async Task<T> RunAsync<T>(StringBuilder sql)
        {
            LogSql(sql);
            try
            {
                using (var con = new MySqlConnection(ConStr))
                {
                    await con.OpenAsync();
                    using (var cmd = new MySqlCommand(sql.ToString(), con))
                    {
                        var o = await cmd.ExecuteScalarAsync();
                        if (o != DBNull.Value)
                        {
                            return (T)o;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OnException?.Invoke(e);
            }
            return default(T);
        }

        public override async Task<bool> ReaderAsync(StringBuilder sql,
            Action<IDataReader> act)
        {
            LogSql(sql);
            try
            {
                using (var con = new MySqlConnection(ConStr))
                {
                    await con.OpenAsync();
                    using (var cmd = new MySqlCommand(sql.ToString(), con))
                    {
                        var reader = await cmd.ExecuteReaderAsync();
                        act(reader);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                OnException?.Invoke(e);
            }

            return false;
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="sql"></param> 
        /// <returns></returns>
        public override async Task<DataTable> SelectAsync(StringBuilder sql)
        {
            LogSql(sql);
            try
            {
                using (var con = new MySqlConnection(ConStr))
                {
                    await con.OpenAsync();
                    using (var da = new MySqlDataAdapter(sql.ToString(), con))
                    {
                        var dt = new DataTable();
                        await da.FillAsync(dt);
                        return dt;
                    }
                }
            }
            catch (Exception e)
            {
                OnException?.Invoke(e);
            }
            return null;
        }
          
        public override async Task<bool> UseTransAsync(IEnumerable<StringBuilder> sqlLs)
        {
            try
            {
                using (var con = new MySqlConnection(ConStr))
                {
                    await con.OpenAsync();
                    var trans = await con.BeginTransactionAsync();
                    try
                    {
                        using (var cmd = con.CreateCommand())
                        {
                            cmd.Transaction = trans; 
                            foreach (var sql in sqlLs)
                            {
                                LogSql(sql);
                                cmd.CommandText = sql.ToString();
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        trans.Commit();
                        return true;
                    }
                    catch (Exception e)
                    {
                        OnException?.Invoke(e);
                    }
                    trans.Rollback();
                    return false;
                }
            }
            catch (Exception e)
            {
                OnException?.Invoke(e);
            }
            return false;
        }

        public override Task<DbToolTrans> BeginTransAsync()
        {
            return DbToolTransMysql.CreateAsync(ConStr);
        } 

        public override void Dispose() => MySqlConnection.ClearAllPools();
    }
} 