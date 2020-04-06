using System;

namespace Cherry.Db.Common
{
    /// <inheritdoc />
    /// <summary>
    /// 列特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ColAttribute : Attribute
    {
        public ColType Type { get; set; } = ColType.Col;

        /// <summary>
        /// 列名字
        /// </summary>
        public string Name { get; set; }
    }
}