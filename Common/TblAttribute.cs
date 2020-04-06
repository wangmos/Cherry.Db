using System;

namespace Cherry.Db.Common
{
    /// <inheritdoc />
    /// <summary>
    /// 表特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TblAttribute : Attribute
    {
        /// <summary>
        /// 表名 更新和删除操作 没有视图名 也执行查询操作
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 视图名字 执行查询操作
        /// </summary>
        public string ViewName { get; set; }
    }
}