

using Pingfan.Inject;

namespace ConsoleTest;

class Program
{
    static void Main(string[] args)
    {
        var container = new Container();
        container.Push<Startup>();
        container.Get<Startup>();
        Console.ReadLine();
    }
}