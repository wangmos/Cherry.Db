namespace Cherry.Db.Common
{
    /// <summary>
    /// 列类型
    /// </summary>
    public enum ColType
    {
        /// <summary>
        /// 主键
        /// </summary>
        Key,
        /// <summary>
        /// 普通列
        /// </summary>
        Col,
        /// <summary>
        /// 视图列，不会保存
        /// </summary>
        View,
        /// <summary>
        /// 聚合函数列,
        /// </summary>
        Aggregate,
    }
}