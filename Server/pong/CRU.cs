using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;


namespace ConsoleApp5
{
    class TOP
        {
            [JsonProperty("name")]
            public string name { get; set; }

            [JsonProperty("score")]
            public int score { get; set; }

            public override string ToString() => $"Name = {name} Score = {score}";
        }
        class CRU
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
                    list.Add(top);
                    list = list.OrderByDescending(i => i.score).ToList();                  
                    list = list.GetRange(0, 10);
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
                    File.WriteAllText(DB, string.Empty);
                    File.WriteAllText(DB, JsonConvert.SerializeObject(list));
                }
                return list;
            }
        }

        //how to use
        //static void Main(string[] args)
        //{
        //    CRU allTop = new CRU();
        //    var all = allTop.Read();

        //    for (int i = 0; i < all.Count; i++)
        //    {
        //        Console.WriteLine(all[i] + " ");
        //    }

        //    TOP newper = new TOP();
        //    newper.name = "Nik";
        //    newper.score = 8;

        //    var found = all.Where(x => x.name.Equals(newper.name)).FirstOrDefault();

        //    if (found != null) 
        //    {
        //        allTop.Update(newper.name, newper.score);
        //    }
        //    else
        //    {
        //        allTop.Create(newper);
        //    }

        //    Console.WriteLine("stalo:_______________________");
        //    all = allTop.Read();
        //    for (int i = 0; i < all.Count; i++)
        //    {
        //        Console.WriteLine(all[i] + " ");
        //    }
        //    Console.ReadKey();
        //}
}
