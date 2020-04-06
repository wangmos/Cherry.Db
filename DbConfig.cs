namespace Cherry.Db
{
    public class DbConfig  
    {
        /// <summary>
        /// 数据库类型 默认Mysql
        /// </summary>
        public DbType Type { get; set; } = DbType.Mysql;

        /// <summary>
        /// 主机 默认localhost
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// 端口 默认3306 
        /// </summary>
        public int Port { get; set; } = 3306;

        /// <summary>
        /// 数据库名 
        /// </summary>
        public string DbName { get; set; } = "";

        /// <summary>
        /// 用户名 默认root
        /// </summary>
        public string User { get; set; } = "root";

        /// <summary>
        /// 密码
        /// </summary>
        public string Pass { get; set; } = "";

        /// <summary>
        /// 是否数字型主键  如果非数字型  请重写GenKey方法 默认true
        /// </summary>
        public bool DigitalKey { get; set; } = true;

        /// <summary>
        /// 多线程操作阈值 默认5000
        /// </summary>
        public int AsyncNum { get; set; } = 5000; 

        /// <summary>
        /// 自定义连接字符串 默认null
        /// </summary>
        public string ConString { get; set; }
    }
}