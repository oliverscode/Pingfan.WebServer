using Pingfan.Inject;

namespace ConsoleTest;

class Program
{
    public static void Method1(string str)
    {
    }
    public static void Method2(string? str)
    {
    }
    public static void Method3(int age)
    {
    }

    static void Main(string[] args)
    {
      



        var container = new Container();
        container.New<Startup>();
        Console.ReadLine();
    }
}