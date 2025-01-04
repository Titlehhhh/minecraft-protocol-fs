using SandBoxLib;
using System;

namespace ConsoleApp1;

public class Program
{
    public static void Main(string[] args)
    {
        for (int i = 0; i <= GetInt(); i++)
        {
            Console.WriteLine(i);
        }
    }
    static int GetInt()
    {
        Console.WriteLine("Get");
        return 5;
    }

}
