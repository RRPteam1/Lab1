using Newtonsoft.Json;

namespace Server.Network.DB
{
    public class CRU
    {
        public List<TOP> list;
        public string DB;

        public CRU(List<TOP> list, string DB = "DB.json")
        {
            this.list = list;
            this.DB = DB;
        }

        public CRU(string DB = "DB.json")
        {
            list = new List<TOP>();
            this.DB = DB;
        }

        public List<TOP> Create(TOP top)
        {
            if (File.Exists(DB))
            {
                list = Read();
                if(list == null) list = new List<TOP>();
                list.Add(top);
                list = list.OrderByDescending(i => i.score).ToList();

                if(list.Count > 10) list = list.GetRange(0, 10);
                File.WriteAllText(DB, string.Empty);
                File.WriteAllText(DB, JsonConvert.SerializeObject(list));
            }

            return list;
        }

        public List<TOP> Read()
        {
            if (File.Exists(DB))
                return JsonConvert.DeserializeObject<List<TOP>>(File.ReadAllText(DB));

            else return list;
        }


        public List<TOP> Update(string keyName, int newScore)
        {
            if (File.Exists(DB))
            {
                list = Read();
                if (list.Count == 0) return list;
                var item = list.Where(x => x.name.Equals(keyName)).First();
                if (item.score > newScore) return list;
                item.score = newScore;
                list = list.OrderByDescending(i => i.score).ToList();
                File.WriteAllText(DB, string.Empty); //empty db
                File.WriteAllText(DB, JsonConvert.SerializeObject(list));
            }
            return list;
        }
    }
}
