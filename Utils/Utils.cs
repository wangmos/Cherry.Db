using System;
using System.Collections;
using System.Reflection;
using System.Text;

namespace Cherry.Db.Utils
{
    internal static class Utils
    {

        /// <summary>
        /// 是否集合类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsCollection(this Type type)
            => (type.GetInterface("ICollection") ?? type.GetInterface("ICollection`1")) != null;

        /// <summary>
        /// 是否字典
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsDictionary(this Type type)
            => (type.GetInterface("IDictionary") ?? type.GetInterface("IDictionary`2")) != null;

        /// <summary>
        /// 取得泛型成员类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="index"></param>
        /// <returns></returns> 
        public static Type GetGenericType(this Type type, int index = 0)
        {
            var ls = type.GetGenericArguments();
            return ls.Length > index ? ls[index] : null;
        }

        /// <summary>
        /// 委托函数
        /// </summary>
        /// <param name="t"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public static void InvokeMethod(this object t, string methodName, params object[] args)
        {
            t.GetType()
                .InvokeMember(methodName, //反射的属性
                    //搜索的方式
                    BindingFlags.NonPublic | BindingFlags.Public
                                           | BindingFlags.InvokeMethod
                                           | BindingFlags.Instance,
                    null,
                    //目标
                    t,
                    //参数
                    args);
        }

        /// <summary>
        /// 文本填充对象容器  不知道数据类型的
        /// </summary>
        /// <param name="o"></param>
        /// <param name="val"></param>
        /// <param name="sep"></param>
        /// <param name="InsertFunc"></param>
        public static void FillCollectionByStr(this ICollection o, string val, string sep, Func<object, object[], object> InsertFunc = null)
        {
            ((dynamic)o).Clear();

            if (string.IsNullOrEmpty(val)) return;

            var type = o.GetType().GetGenericType();

            foreach (var str in val.Split(new[] { sep }, StringSplitOptions.None))
            {
                var valVal = type.FromSimpleString(str, sep, null);

                if (InsertFunc == null)
                    o.InvokeMethod("Add", valVal);
                else InsertFunc(o, new[] {valVal});
            }

        }

        /// <summary>
        /// 文本填充字典 不知道数据类型的
        /// </summary>
        /// <param name="o"></param>
        /// <param name="val"></param>
        /// <param name="sep"></param>
        /// <param name="InsertFunc"></param>
        public static void FillDictionaryByStr(this IDictionary o, string val, string sep, Func<object,object[],object> InsertFunc = null)
        {
            ((dynamic)o).Clear(); 

            if (string.IsNullOrEmpty(val)) return;

            var ls = val.Split(new[] { sep }, StringSplitOptions.None);

            var keyType = o.GetType().GetGenericType();
            var valType = o.GetType().GetGenericType(1);

            for (var i = 0; i < ls.Length; i += 2)
            {
                var key = ls[i];
                var valStr = ls[i + 1];

                var keyVal = keyType.FromSimpleString(key, sep, null);
                var valVal = valType.FromSimpleString(valStr, sep, null);

                if (InsertFunc == null)
                    o.InvokeMethod("Insert", keyVal, valVal, false);
                else InsertFunc(o, new[] {keyVal, valVal, false});
            }
        }
         

        /// <summary>
        /// 对象容器拼接文本  
        /// </summary>
        /// <param name="src"></param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public static string JoinCollectionToStr(this ICollection src, string sep)
        {
            var sb = new StringBuilder();
            foreach (var str in src)
            {
                sb.Append($"{str.ToSimpleString(sep)}{sep}");
            }
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - sep.Length, sep.Length);
            }
            return sb.ToString();
        }


        /// <summary>
        /// 字典拼接文本
        /// </summary>
        /// <param name="src"></param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public static string JoinDictionaryToStr(this IDictionary src, string sep)
        {
            var sb = new StringBuilder();
            foreach (DictionaryEntry pair in src)
            {
                sb.Append($"{(pair.Key).ToSimpleString(sep)}{sep}{(pair.Value).ToSimpleString(sep)}{sep}");
            }
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - sep.Length, sep.Length);
            }

            return sb.ToString();
        }

        /// <summary>
        ///  简单字符串到各种内建类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="val"></param>
        /// <param name="sep"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static object FromSimpleString(this Type type, string val, string sep, object o)
        {
            if (type.IsEnum)
            {
                return Enum.Parse(type, val);//数字到枚举 
            }
            if (type == typeof(DateTime))
            {
                return (double.TryParse(val, out var i)
                    ? DateTime.Parse("1970-01-01").AddSeconds(i).ToLocalTime() : DateTime.MinValue);
            }
            if (type == typeof(TimeSpan))
            {
                return (int.TryParse(val, out var i)
                    ? TimeSpan.FromSeconds(i) : TimeSpan.Zero);
            }
            if (type.IsDictionary())
            {
                ((IDictionary)o).FillDictionaryByStr(val, sep);
                return o;
            }

            if (type.IsCollection())
            {
                ((ICollection)o).FillCollectionByStr(val, sep);
                return o;
            }

            return Convert.ChangeType(val, type);

        }

        /// <summary>
        /// 各种类型 返回简单字符串表示
        /// </summary>
        /// <param name="src"></param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public static string ToSimpleString<T>(this T src, string sep)
        {
            switch (src)
            {
                case decimal _:
                case double _:
                case float _:
                    return $"{src:F}"; //0.00
                case DateTime dt:
                    return $"{(dt.ToUniversalTime() - DateTime.Parse("1970-01-01")).TotalSeconds}"; //秒
                case TimeSpan ts:
                    return $"{ts.TotalSeconds}"; //秒
                case Enum _:
                    return $"{Convert.ToInt32(src)}"; //数字
                case IDictionary dicSrc:
                    return dicSrc.JoinDictionaryToStr(sep);
                default:
                    return src is ICollection colSrc ? colSrc.JoinCollectionToStr(sep) : src.ToString();
            }
        }

        /// <summary>
        /// 删除最后的字符
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static StringBuilder RemoveLast(this StringBuilder sb, int len = 1)
        {
            return sb.Length < len ? sb.Clear() : sb.Remove(sb.Length - len, len);
        }
    }
}