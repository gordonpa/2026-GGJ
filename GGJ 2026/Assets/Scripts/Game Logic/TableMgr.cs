using Luban;
using SimpleJSON;
using UnityEngine;
namespace Util
{
    public class TableMgr : Singleton<TableMgr>
    {

        private cfg.Tables _tables;


        public cfg.Tables Tables
        {
            get
            {
                if (_tables == null)
                {
                    Load();
                }
                return _tables;
            }
        }

        void Load()
        {
            var tablesCtor = typeof(cfg.Tables).GetConstructors()[0];
            var loaderReturnType = tablesCtor.GetParameters()[0].ParameterType.GetGenericArguments()[1];
            // 根据cfg.Tables的构造函数的Loader的返回值类型决定使用json还是ByteBuf Loader
            System.Delegate loader = loaderReturnType == typeof(ByteBuf) ?
                new System.Func<string, ByteBuf>(LoadByteBuf)
                : new System.Func<string, JSONNode>(LoadJson);
            _tables = (cfg.Tables)tablesCtor.Invoke(new object[] { loader });
            Debug.Log("加载配置表格");
        }

        public void ReLoad()
        {
            Load();
        }

        private static JSONNode LoadJson(string file)
        {
            return JSON.Parse(Resources.Load<TextAsset>($"Table/{file}").text);
        }

        private static ByteBuf LoadByteBuf(string file)
        {
            return new ByteBuf(Resources.Load<TextAsset>($"Table/{file}").bytes);
        }
    }
}
