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
      
     
        {
            var method = typeof(Program).GetMethod("Method1");
            var parameters = method!.GetParameters();
            var parameter = parameters[0];
            Console.WriteLine(parameter.RawDefaultValue is null);
        }
       
        
     
        {
            var method = typeof(Program).GetMethod("Method2");
            var parameters = method!.GetParameters();
            var parameter = parameters[0];
            Console.WriteLine(parameter.RawDefaultValue is null);
        }

        
        {
            var method = typeof(Program).GetMethod("Method3");
            var parameters = method!.GetParameters();
            var parameter = parameters[0];
            Console.WriteLine(parameter.RawDefaultValue is null);
        }



        var container = new Container();
        container.Push<Startup>();
        container.Get<Startup>();
        Console.ReadLine();
    }
}