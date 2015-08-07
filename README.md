# Topshelf.Unix

Topshelf extensions for compatibility with Mono/Linux.

## Using

```csharp
static void Main()
{
    HostFactory.Run(config =>
    {
        var parameters = config.SelectPlatform();

        config.Service<MySelfHost>(s =>
        {
            s.ConstructUsing(name => new MySelfHost());
            s.WhenStarted(host => host.Start(parameters));
            s.WhenStopped(host => host.Stop(parameters));
        });

        config.SetServiceName("MyServiceName");
        config.SetDisplayName("MyServiceName");
        config.SetDescription("MyServiceName Description");
    });
}

class MySelfHost
{
    public void Start()
    {
        // Do Start
    }

    public void Stop()
    {
        // Do Stop
    }
}
```

### Windows:

```powershell
MyServiceName.exe install
MyServiceName.exe start
MyServiceName.exe stop
MyServiceName.exe uninstall
```

### Linux

```powershell
sudo mono MyServiceName.exe install
sudo mono MyServiceName.exe start
sudo mono MyServiceName.exe stop
sudo mono MyServiceName.exe uninstall
```

## Custom Parameters

```csharp
static void Main()
{
    HostFactory.Run(config =>
    {
        var parameters = config.SelectPlatform(p => p
            .AddStringParameter("param1")
            .AddStringParameter("param2")
            .AddStringParameter("param3"));

        config.Service<MySelfHost>(s =>
        {
            s.ConstructUsing(name => new MySelfHost());
            s.WhenStarted(host => host.Start(parameters));
            s.WhenStopped(host => host.Stop(parameters));
        });

        config.SetServiceName("MyServiceName");
        config.SetDisplayName("MyServiceName");
        config.SetDescription("MyServiceName Description");
    });
}

class MySelfHost
{
    public void Start(IDictionary<string, object> parameters)
    {
        // Do Start
        // var param1 = parameters["param1"]; // Value1
        // var param2 = parameters["param2"]; // Value2
        // var param3 = parameters["param3"]; // Value3
    }

    public void Stop(IDictionary<string, object> parameters)
    {
        // Do Stop
        // var param1 = parameters["param1"]; // Value1
        // var param2 = parameters["param2"]; // Value2
        // var param3 = parameters["param3"]; // Value3
    }
}
```

### Windows:

```powershell
MyServiceName.exe install -param1 "Value1" -param2 "Value2" -param3 "Value3"
MyServiceName.exe start
MyServiceName.exe stop
MyServiceName.exe uninstall
```

### Linux

```powershell
sudo mono MyServiceName.exe install -param1 "Value1" -param2 "Value2" -param3 "Value3"
sudo mono MyServiceName.exe start
sudo mono MyServiceName.exe stop
sudo mono MyServiceName.exe uninstall
```

## NuGet

https://www.nuget.org/packages/Topshelf.Unix/

```powershell
Install-Package Topshelf.Unix
```

## Notes

Currently Mono.Helpers code is tested only on Ubuntu 14.04.2 LTS.
