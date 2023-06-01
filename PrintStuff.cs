using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
namespace Program
{
    public class p
    {
        public static void cw(dynamic? thing) 
            => Console.WriteLine(thing);

        public static void w(dynamic? thing) 
            => Console.Write(thing);

        public static void cwa<T>(T[] array)
            => Array.ForEach(array, item => Console.WriteLine(item));

        public static void cwa<T>(IEnumerable<T> Collection)
            => Array.ForEach(Collection.ToArray(), item => Console.WriteLine(item));

        public static void ckv<T1, T2>(IDictionary<T1, T2> dic)
        {
            foreach (KeyValuePair<T1, T2> kvp in dic)
                Console.WriteLine($"{kvp.Key}:\t{kvp.Value}");
        }
    }
}
