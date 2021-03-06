﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Cherry.Db.Test
{
    public static class Test
    {
        static Test()
        {
            Student.Init(new DbConfig()
            {
                Pass = "Qq123456987,", //数据库密码
                DbName = "Test", //数据库名
                DigitalKey = true,//数字型主键 无需主动操作 非数字型 则重写GenKey方法,自定义生成主键值
            });
        }

        public static void TestInsert()
        {
            //单条数据插入
            var student = new Student
            {
                Name = "小王",
                Age = 18,
                Gender = Gender.Boy,
                InTime = DateTime.Now,
                Score = {["语文"] = 89, ["数学"] = 67, ["英语"] = 45},
            };
             
            student.Hobby.Add("唱歌");
            student.Hobby.Add("跳舞");

            student.Insert();//插入数据库 
            //await student.InsertAsync(); //异步版本 数据库操作都有相应的异步版本

            //student.Insert(t => t.Name); //指定单列插入
            //student.Insert(t => new { t.Name, t.Age }); //指定多列插入

            //Student.Inserter.Insert(student);//使用插入器操作
            //Student.Inserter.Col(t => t.Name).Insert(student);//指定单列插入
            //Student.Inserter.Col(t => new { t.Name, t.Age }).Insert(student);//指定多列插入



            //多条数据插入

            var student1 = new Student
            {
                Name = "小李",
                Age = 12,
                Gender = Gender.Girl,
                InTime = DateTime.Now,
                Score = { ["语文"] = 89, ["数学"] = 67, ["英语"] = 45 },
            };

            student1.Hobby.Add("唱歌");
            student1.Hobby.Add("跳舞");


            var student2= new Student
            {
                Name = "小张",
                Age = 12,
                Gender = Gender.Boy,
                InTime = DateTime.Now,
                Score = { ["语文"] = 89, ["数学"] = 67, ["英语"] = 45 },
            };

            student2.Hobby.Add("唱歌");
            student2.Hobby.Add("跳舞");

            var ls = new List<Student>()
            {
                student1,student2
            };

            Student.Inserter.Insert(ls);
        }




        public static void TestSelect()
        {
            //单条查询
            var student1 = Student.One(t => t.Id == 1);

            //指定查询列
            var student2 = Student.Selector.Col(t => t.Name).Where(t => t.Id > 1 && t.Name == "小王").One();

            //指定查询多列
            var student3 = Student.Selector.Col(t => new {t.Name,t.Age}).Where(t => t.Id >= 1 && t.Name == "小王").One();





            //多条查询
            var ls1 = Student.Selector.Where(t => t.Id > 0).ToList();

            //查询所有喜欢唱歌的学生
            ls1 = Student.Selector.Where(t => t.Hobby.Contains("唱歌")).ToList();

            //查询所有名字以"王"结尾的学生
            ls1 = Student.Selector.Where(t => t.Name.EndsWith("王")).ToList();

            //查询所有一个月以前入学的学生
            ls1 = Student.Selector.Where(t => t.InTime > DateTime.Now.AddMonths(-1)).ToList();

            //逐条拉取
            Student.Selector.Foreach(t=> Console.WriteLine(t.Name));



            
            //分页查询
            var sel = Student.Selector;//取得一个查询器

            sel.PageInfo.Init(2);//设置每页记录数 0则不使用分页

            var ls2 = sel.Where(t => t.Id > 0).ToList();
            //重新设置 where 或者 groupBy 子句后 分页记录将重置
            if (sel.PageInfo.Next())
            {
                ls2 = sel.ToList();
                ls2 = sel.Where(t => t.Name != "xiaowang").ToList();//此句并不会得到第二页的数据 因为重置了where条件
            }

            //复杂查询 sql:select age,count(age) age_count from student group by age
            var ls3 = Student.Selector.Col(t => t.Age).Count(t => t.Age).GroupBy(t=>t.Age).ToList();
        }

        public static void TestUpdate()
        {
            //单条更新
            var student1 = Student.One(t => t.Id == 1);
            student1.Age += 1;
            //只更新age字段
            student1.Update(t=>t.Age);
            student1.Update();//更新所有字段


            //多条更新
            var ls = Student.Selector.Where(t => t.Id > 0).ToList();
            foreach (var student in ls)
            {
                student.Age += 1;
            }

            Student.Updater.Col(t => t.Age).Update(ls.ToList());

            //使用更新器更新数据  设置了where条件的话 将是对整张表的更新  请注意！！！！

            //直接更新 按where条件更新整张表的Age数据为实例的值
            Student.Updater.Col(t=>t.Age).Where(t => t.Age == 12).Update(new Student()
            {
                Age = 14
            });

            //sql:update student set age=14 where age=12
            Student.Updater.Set(t => t.Age, t => 12).Where(t => t.Age == 14).Update();

            //mysql专属set方法 sql: update student set name = case age when 12 then 'xiaowang' when 13 then 'xiaozhang' else 'xiaoli' end 
            //当age=12时 更新name为xiaowang 当age=13时 更新name为xiaozhang 其他值更新为xiaoli
            Student.Updater.Set(t => t.Name, t => t.Age, t => "xiaoli", 
                (t => 12, t => "xiaowang"),
                (t => 13, t => "xiaozhang")).Update();
        }


        public static void TestDelete()
        {
            //单条删除
            var student1 = Student.One(t => t.Id == 1);
            student1.Delete();

            //多条删除
            var ls = Student.Selector.Where(t => t.Age == 13).ToList();
            Student.Deleter.Delete(ls.ToList());


            //直接操作数据库删除
            Student.Deleter.Where(t => t.Id > 0).Delete();
        }
    }
}