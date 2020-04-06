using System;
using System.Collections.Generic;
using System.ComponentModel;
using Cherry.Db.Common;

namespace Cherry.Db.Test
{

    /*
     *
 创建表Sql:

     CREATE TABLE `student` (
    `id`  int(11) NOT NULL AUTO_INCREMENT ,
    `name`  varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL ,
    `age`  int(11) NULL DEFAULT NULL ,
    `gender`  int(11) NULL DEFAULT NULL ,
    `intime`  datetime NULL DEFAULT NULL ,
    `score`  varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL ,
    `hobby`  varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL ,
    PRIMARY KEY (`id`)
    )
    ENGINE=InnoDB
    DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
    AUTO_INCREMENT=1
    ROW_FORMAT=DYNAMIC
; 
     *
     *
     */

    /// <summary>
    /// 性别
    /// </summary>
    public enum Gender
    {
        [Description("男孩")]
        Boy,
        [Description("女孩")]
        Girl
    }

    /// <summary>
    /// 学生表
    /// </summary>
    [Tbl]
    public class Student : DbContext<Student>
    {
        //如果表明和类名不同,则指定Name属性:[Tbl(Name = "student")]
        //如果是从视图中读取数据 则指定ViewName:[Tbl(Name = "student",ViewName = "student")]

        /// <summary>
        /// 主键 
        /// </summary>
        [Col(Type = ColType.Key)]
        public int Id; //如果列名和字段名不同,则指定Name属性  

        /// <summary>
        /// 姓名
        /// </summary>
        [Col]
        public string Name { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [Col]
        public int Age;

        /// <summary>
        /// 性别
        /// </summary>
        [Col]
        public Gender Gender = Gender.Boy; //数据库对应类型:int 

        /// <summary>
        /// 入学时间
        /// </summary>
        [Col]
        public DateTime InTime = DateTime.MinValue;

        /// <summary>
        /// 成绩
        /// </summary>
        [Col]
        public Dictionary<string,int> Score = new Dictionary<string, int>();  //数据库对应类型:varchar 或者 text  引用类型必须初始化!!!

        /// <summary>
        /// 爱好
        /// </summary>
        [Col]
        public List<string> Hobby = new List<string>(); //数据库对应类型:varchar 或者 text  引用类型必须初始化!!!

        /// <summary>
        /// 用于聚合查询 count(Age) 
        /// </summary>
        [Col(Type = ColType.Aggregate)]
        public int Age_count;
          
    }
}