using Pingfan.WebServer;
using Pingfan.WebServer.Middlewares;

namespace ConsoleTest;

public class Home
{
    public string Index(string name, long age)
    {
        return $"name={name}, age={age}";
    }

    public int Age()
    {
        return 25;
    }

    public object List()
    {
        return new List<int>() { 1, 2, 3, 4, 5 };
    }

    public object Start(Person person)
    {
        return $"Start name={person.Name}, age={person.Age}";
        
    }
}

public class Person : IMidRequestModel
{
    public string Name { get; set; }
    public int Age { get; set; }

    public void Check()
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new HttpArgumentException("Name is null", typeof(Person), nameof(Name));
        }

        if (Age != 18)
        {
            throw new HttpArgumentException("Age is not 18", typeof(Person), nameof(Age));
        }
    }
}