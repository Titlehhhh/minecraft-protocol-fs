
public class Program
{
    public static void Main()
    {
        string rand = RandomString(5);
        Console.WriteLine(rand);
    }
    private static string RandomString(int length)
    {
        string result = "";
        for (int i = 0; i < length; i++)
        {
            result += (char)Random.Shared.Next(65, 90);
        }
        return result;
    }
}
