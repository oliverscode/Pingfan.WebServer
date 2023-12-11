using Pingfan.Inject;

namespace ConsoleTest;

class Program
{
 

    static void Main(string[] args)
    {
      
        var container = new Container();
        container.New<Startup>();
        Console.ReadLine();
    }
}