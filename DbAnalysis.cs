using Cherry.Db.Common;
using Cherry.Db.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Cherry.Db
{
    public class DbAnalysis<T> where T : DbContext<T>, new()
    {
        public class DbAnalysisInfo
        {
            public Type ValType;
            public ColType ColType;
            public string ColName;
            public Action<T, object> Setter;
            public Func<object, object[],object> Setter2;
            public Func<T, object> Getter; 
        }

        public static string TblName;
        public static string ViewTblName;
        public static string KeyMemName;
        public static DbAnalysisInfo KeyInfo; 

        public static Dictionary<string, DbAnalysisInfo> ColCols = new Dictionary<string, DbAnalysisInfo>(20, StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, DbAnalysisInfo> ColView = new Dictionary<string, DbAnalysisInfo>(20, StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, DbAnalysisInfo> ColAggregate = new Dictionary<string, DbAnalysisInfo>(20, StringComparer.OrdinalIgnoreCase);

        static DbAnalysis()
        {
            var type = typeof(T);
            var tblInfo = type.GetCustomAttribute<TblAttribute>();
             
            TblName = tblInfo.Name ?? type.Name;
            ViewTblName = tblInfo.ViewName ?? TblName;
               
            AnalysisCol(type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance), false);
            AnalysisCol(type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance), true);

            if(KeyInfo == null)
                throw new TypeInitializationException(typeof(T).FullName,
                    new Exception("没有设置主键"));
        }

        private static void AnalysisCol(MemberInfo[] vales, bool isField)
        {
            if (vales == null) return;
            foreach (var val in vales)
            {
                var att = val.GetCustomAttribute<ColAttribute>();
                if (att == null) continue;
               
                var colName = att.Name ?? val.Name;

                var info = new DbAnalysisInfo()
                {
                    ColType = att.Type,
                    ColName = colName,
                };
                if (isField)
                {
                    info.ValType = ((FieldInfo) val).FieldType;
                    info.Setter = EmitHelper.CreateSetter<T, object>((FieldInfo) val);
                    info.Getter = EmitHelper.CreateGetter<T, object>((FieldInfo) val);
                }
                else
                {
                    info.ValType = ((PropertyInfo)val).PropertyType;
                    info.Setter = EmitHelper.CreateSetter<T, object>((PropertyInfo)val);
                    info.Getter = EmitHelper.CreateGetter<T, object>((PropertyInfo)val);
                }

                if (info.ValType.IsDictionary())
                {
                    info.Setter2 = EmitHelper.CreateMethod(info.ValType.GetMethod("Insert", BindingFlags.Instance | BindingFlags.NonPublic));
                }
                else if (info.ValType.IsCollection())
                {
                    info.Setter2 = EmitHelper.CreateMethod(info.ValType.GetMethod("Add"));
                }

                switch (att.Type)
                {
                    case ColType.Key:
                        KeyInfo = info;
                        KeyMemName = val.Name;
                        break;
                    case ColType.Col:
                        ColCols[val.Name] = info;
                        break;
                    case ColType.View:
                        ColView[val.Name] = info;
                        break;
                    case ColType.Aggregate:
                        ColAggregate[val.Name] = info;
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="memName"></param>
        /// <param name="info"></param>
        /// <param name="all">是否查询视图字段 insert update 不需要操作视图字段</param>
        /// <returns></returns>
        public static bool GetColInfo(string memName, out DbAnalysisInfo info, bool all)
        {
            if (memName == KeyMemName)
            {
                info = KeyInfo;
                return true;
            }

            return ColCols.TryGetValue(memName, out info) || all && ColView.TryGetValue(memName, out info);
        } 

        public static void FormatVal(StringBuilder sb, object val)
        {
            sb.Append(FormatVal(val));
        }
         
        public static string FormatVal(object val)
        {
            var valType = val.GetType();

            if (valType == typeof(string))
            {
                return $"'{((string)val).Replace("'", "\\'")}'";
            }
            if (valType.IsEnum)
            {
                return Convert.ToInt32(val).ToString();
            }
            if (val is DateTime valDt)
            {
                return DbContext<T>.Config.Type == DbType.Access ? $"#{valDt:G}#" : $"'{valDt:G}'";
            }
            if (valType.IsDictionary())
            {
                return $"'{((IDictionary)val).JoinDictionaryToStr("`").Replace("'", "\\'")}'";
            }
            if (valType.IsCollection())
            {
                return $"'{((ICollection)val).JoinCollectionToStr("`").Replace("'", "\\'")}'";
            }

            return val.ToString();
        }

        public static void FormatProcArgs(StringBuilder sb, params object[] vales)
        { 
            if(vales.Length == 0)return;

            foreach (var val in vales)
            {
                FormatVal(sb, val);
                sb.Append(",");
            }

            sb.Remove(sb.Length - 1, 1);
        }

        public static void FillT(T t, DataRow row)
        {
            FillT(KeyInfo, t, row);
            foreach (var kv in ColCols)
            {
                FillT(kv.Value, t, row);
            }
            foreach (var kv in ColView)
            {
                FillT(kv.Value, t, row);
            }
            foreach (var kv in ColAggregate)
            {
                FillT(kv.Value, t, row);
            }
        }

        public static void FillT(DbAnalysisInfo info, T t, DataRow row)
        {
            if (!row.Table.Columns.Contains(info.ColName)) return;
            FillT(info, t, row[info.ColName]);
        }

        public static void FillT(DbAnalysisInfo info, T t, object dbVal)
        { 
            if (dbVal == DBNull.Value || dbVal == null) return;

            var valType = info.ValType;

            if (valType.IsEnum)
            {
                 info.Setter(t, Enum.Parse(valType, dbVal.ToString()));
                 return;
            }  
            if (valType.IsDictionary())
            {
                var val = info.Getter(t);
                ((IDictionary)val).FillDictionaryByStr((string)dbVal, "`", info.Setter2);
                return;
            }
            if (valType.IsCollection())
            {
                var val = info.Getter(t);
                ((ICollection)val).FillCollectionByStr((string)dbVal, "`", info.Setter2);
                return;
            }

            info.Setter(t, dbVal.GetType() != valType ? Convert.ChangeType(dbVal, valType) : dbVal);
        }
         
        public static object GetDbVal(Type valType, object dbVal)
        {
            if (dbVal == DBNull.Value || dbVal == null) return null;
              
            if (valType.IsEnum)
            { 
                return Enum.Parse(valType, dbVal.ToString());
            } 
            if (valType.IsDictionary())
            {
                var val = valType.GetConstructor(new Type[] { })?.Invoke(null); 
                ((IDictionary)val).FillDictionaryByStr((string)dbVal, "`");
                return val;
            }
            if (valType.IsCollection())
            {
                var val = valType.GetConstructor(new Type[] { })?.Invoke(null);
                ((ICollection)val).FillCollectionByStr((string)dbVal, "`");
                return val;
            }

            return dbVal.GetType() != valType ? Convert.ChangeType(dbVal, valType) : dbVal;
        }

        #region expression

        /// <summary>
        /// t=>t.Age or t=>new{t.Age,t.username}
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="dic"></param>
        /// <param name="all">是否查询视图字段</param>
        public static void Col(Expression<Func<T, object>> expression, Dictionary<string, DbAnalysisInfo> dic, bool all)
        {
            dic.Clear();
            switch (expression.Body)
            {
                case MemberExpression member:
                    if (GetColInfo(Col(member, all), out var info, all))
                    {
                        dic[info.ColName] = info; 
                        return;
                    }

                    break;
                case NewExpression newExpression:
                    foreach (var memName in Col(newExpression, all))
                    {
                        if (GetColInfo(memName, out info, all))
                        {
                            dic[info.ColName] = info;
                        }
                        else break;
                    }

                    return;
                case UnaryExpression unary:
                    if (GetColInfo(Col(unary, all), out info, all))
                    {
                        dic[info.ColName] = info;
                        return;
                    }

                    break; 
            }
            throw new ArgumentException($"非法的字段名:{expression}");
        }

        /// <summary>
        /// t=>t.Age
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="all"></param>
        /// <returns></returns>
        public static string Col(Expression<Func<T, object>> expression, bool all)
        {
            switch (expression.Body)
            {
                case MemberExpression member:
                    if (GetColInfo(Col(member, all), out var info, all))
                    {
                        return info.ColName;
                    }

                    break;
                case UnaryExpression unary:
                    if (GetColInfo(Col(unary, all), out info, all))
                    {
                        return info.ColName;
                    }

                    break;
            }

            throw new ArgumentException($"非法的字段名:{expression}");
        }

        /// <summary>
        /// t=>t.Age or t=>new{t.Age,t.username}
        /// </summary>
        /// <param name="expression"></param> 
        /// <param name="ls"></param>
        /// <param name="all">是否查询视图字段</param>
        public static void Col(Expression<Func<T, object>> expression, ICollection<string> ls, bool all)
        {
            ls.Clear();
            switch (expression.Body)
            {
                case MemberExpression member:
                    if (GetColInfo(Col(member, all), out var info, all))
                    {
                        ls.Add(info.ColName);
                        return;
                    }

                    break;
                case NewExpression newExpression:
                    foreach (var memName in Col(newExpression, all))
                    {
                        if (GetColInfo(memName, out info, all))
                        {
                            ls.Add(info.ColName);
                        }
                        else break; 
                    }

                    return;
                case UnaryExpression unary:
                    if (GetColInfo(Col(unary, all), out info, all))
                    {
                        ls.Add(info.ColName);
                        return;
                    }

                    break; 
            }
            throw new ArgumentException($"非法的字段名:{expression}");
        }


        public static string Col(MemberExpression expression, bool all)
        {
            if (expression.Expression is ParameterExpression)
            {
                return expression.Member.Name;
            }
            throw new ArgumentException($"非法的字段名:{expression}");
        }

        public static List<string> Col(NewExpression expression, bool all)
        {
            var ls = new List<string>();
            //数组
            foreach (var valExpression in expression.Arguments)
            {
                if (valExpression is MemberExpression memberExpression && memberExpression.Expression is ParameterExpression)
                {
                    ls.Add(memberExpression.Member.Name);
                }
                else throw new ArgumentException($"非法的字段名:{expression}");
            }

            return ls;
        }

        public static string Col(UnaryExpression expression,  bool all)
        {
            if (expression.Operand is MemberExpression memberExpression && memberExpression.Expression is ParameterExpression)
            {
                return memberExpression.Member.Name;
            }
            throw new ArgumentException($"非法的字段名:{expression}");
        }


        /// <summary>
        /// t=>t.Age == 4 && t.UserName != "wang" 复杂运算请尽量将结果代入表达式
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="sb"></param>
        /// <returns></returns>
        public static void Where(Expression<Func<T, bool>> expression, StringBuilder sb)
        {
            sb.Clear();

            Where(expression.Body, sb); 
        }

        public static void Where(Expression expression, StringBuilder sb, bool not = false)
        {
            switch (expression)
            {
                case BinaryExpression binary:
                    Where(binary, sb);
                    break;
                case MemberExpression member:
                    Where(member, sb, not);
                    break;
                case ConstantExpression constant:
                    FormatVal(sb, constant.Value);
                    break;
                case MethodCallExpression methodCall:
                    Where(methodCall, sb, not);
                    break;
                case UnaryExpression unary:
                    Where(unary, sb);
                    break;
                default: throw new ArgumentException($"非法的where子句:{expression} 不支持的表达式");
            }
        }
        public static void Where(UnaryExpression expression, StringBuilder sb)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Negate:
                    sb.Append("-");
                    break;
                case ExpressionType.Not:
                    sb.Append("!");
                    break;
                case ExpressionType.Convert:
                    break;
                default: throw new ArgumentException($"非法的where子句:{expression} 不支持的表达式");
            }
            Where(expression.Operand, sb, expression.NodeType == ExpressionType.Not);
        }

        public static void Where(BinaryExpression expression, StringBuilder sb)
        {
            sb.Append("(");
            Where(expression.Left, sb);

            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                    sb.Append(" and ");
                    break;
                case ExpressionType.OrElse:
                    sb.Append(" or ");
                    break;
                case ExpressionType.Equal:
                    sb.Append("=");
                    break;
                case ExpressionType.NotEqual:
                    sb.Append("!=");
                    break;
                case ExpressionType.GreaterThan:
                    sb.Append(">");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    sb.Append(">=");
                    break;
                case ExpressionType.LessThan:
                    sb.Append("<");
                    break;
                case ExpressionType.LessThanOrEqual:
                    sb.Append("<=");
                    break;
                case ExpressionType.UnaryPlus:
                case ExpressionType.Add:
                    sb.Append("+");
                    break;
                case ExpressionType.Negate:
                case ExpressionType.Subtract:
                    sb.Append("-");
                    break;
                case ExpressionType.Multiply:
                    sb.Append("*");
                    break;
                case ExpressionType.Divide:
                    sb.Append("/");
                    break;
                case ExpressionType.Modulo:
                    sb.Append("%");
                    break;
            }

            Where(expression.Right, sb);
            sb.Append(")");
        }

        public static void Where(MemberExpression expression, StringBuilder sb, bool not = false)
        {
            if (expression.Expression is ParameterExpression)
            {
                if (GetColInfo(expression.Member.Name,out var info, true))
                {
                    sb.Append(info.ColName);
                    return;
                }
                throw new ArgumentException($"非法的where子句:{expression.Member.Name} 不存在该字段");
            }
            if (expression.Expression is MemberExpression memberExpression)
            {
                if (not) sb.Append(" not ");
                if (expression.Member.Name == "Length")
                {
                    sb.Append("length(");
                    Where(memberExpression, sb);
                    sb.Append(")");
                    return;
                }
            }
            throw new ArgumentException($"非法的where子句:{expression} 不支持的表达式");
        }

        public static void Where(MethodCallExpression expression, StringBuilder sb, bool not = false)
        {
            if (expression.Object is MemberExpression memberExpression && memberExpression.Expression is ParameterExpression)
            {
                Where(memberExpression, sb);

                if (not) sb.Append(" not");

                void FormatDate()
                {
                    sb.Append("date_add(");
                    Where(expression.Object, sb);
                    sb.Append(",interval ");
                    var val = GetExpVal(expression.Arguments[0]);
                    FormatVal(sb, val);
                }
                if (expression.Method.Name == "AddSeconds")
                {
                    FormatDate();
                    sb.Append(" second)");
                }
                else if (expression.Method.Name == "AddDays")
                {
                    FormatDate();
                    sb.Append(" day)");
                }
                else if (expression.Method.Name == "AddHours")
                {
                    FormatDate();
                    sb.Append(" hour)");
                }
                else if (expression.Method.Name == "AddMinutes")
                {
                    FormatDate();
                    sb.Append(" minute)");
                }
                else if (expression.Method.Name == "AddMonths")
                {
                    FormatDate();
                    sb.Append(" month)");
                }
                else if (expression.Method.Name == "AddYears")
                {
                    FormatDate();
                    sb.Append(" year)");
                }
                else if (expression.Method.Name == "StartsWith")
                {
                    sb.Append(" like '");
                    sb.Append(GetExpVal(expression.Arguments[0]));
                    sb.Append("%'");
                }
                else if (expression.Method.Name == "EndsWith")
                {
                    sb.Append(" like '%");
                    sb.Append(GetExpVal(expression.Arguments[0]));
                    sb.Append("'");
                }
                else if (expression.Method.Name == "Contains")
                {
                    sb.Append(" like '%");
                    sb.Append(GetExpVal(expression.Arguments[0]));
                    sb.Append("%'");
                }
                else if (expression.Method.Name != "ToString")
                {
                    throw new ArgumentException($"非法的where子句:{expression} 不支持的表达式");
                }
            }
            else if (expression.Method.Name == "Contains")
            {
                var ls = GetExpVal(expression.Object);
                var colIndex = 0;
                if (expression.Arguments.Count == 2)
                {
                    colIndex++;
                    ls = GetExpVal(expression.Arguments[0]);
                }

                Where(expression.Arguments[colIndex], sb);
                if (not) sb.Append(" not");
                sb.Append(" in(");

                foreach (var val in (ICollection)ls)
                {
                    FormatVal(sb, val);
                    sb.Append(",");
                }

                sb.RemoveLast().Append(")");

            }
            else
            {
                var val = GetExpVal(expression);
                FormatVal(sb, val);
            }

        }

        /// <summary>
        /// 不支持new等复杂运算
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static object GetExpVal(Expression expression)
        {
            switch (expression)
            {
                case ConstantExpression constant:
                    return constant.Value;
                case MemberExpression member:
                    return GetExpVal(member);
                case MethodCallExpression method:
                    return GetExpVal(method);
                case UnaryExpression unary:
                    return GetExpVal(unary);
                case null: return null;
                default: throw new ArgumentException($"非法的where子句:{expression} 不支持的表达式");
            }
        }

        public static object GetExpVal(UnaryExpression expression)
        {
            dynamic val = GetExpVal(expression.Operand);
            switch (expression.NodeType)
            {
                case ExpressionType.Negate:
                    return -val;
                case ExpressionType.Not:
                    return !val;
                default: throw new ArgumentException($"非法的where子句:{expression} 不支持的表达式");
            }
        }

        public static object GetExpVal(MethodCallExpression expression)
        {
            var obj = GetExpVal(expression.Object);
            var args = new object[expression.Arguments.Count];
            for (var i = 0; i < expression.Arguments.Count; i++)
            {
                args[i] = GetExpVal(expression.Arguments[i]);
            }

            return expression.Method.Invoke(obj, args);
        }

        public static object GetExpVal(MemberExpression expression)
        { 
            var obj = GetExpVal(expression.Expression);

            return (expression.Member as FieldInfo)?.GetValue(obj) ??
                   (expression.Member as PropertyInfo)?.GetValue(obj);
        }

        #endregion
    }
}