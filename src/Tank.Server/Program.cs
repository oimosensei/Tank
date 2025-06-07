var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMagicOnion();
builder.Services.AddSingleton<GameContextRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
try
{
    app.MapMagicOnionService();
}
catch (System.Reflection.TargetInvocationException tie)
{
    Console.WriteLine("TargetInvocationException occurred. Details below:");
    Exception? innerEx = tie.InnerException;
    while (innerEx != null)
    {
        Console.WriteLine("--- Inner Exception ---");
        Console.WriteLine($"Type: {innerEx.GetType().FullName}");
        Console.WriteLine($"Message: {innerEx.Message}");
        Console.WriteLine($"StackTrace: {innerEx.StackTrace}");
        Console.WriteLine("--- End Inner Exception ---");
        innerEx = innerEx.InnerException; // さらに内側の例外があれば表示
    }
    throw; // アプリケーションを停止させるために再スロー
}
catch (Exception ex) // その他の予期せぬ例外
{
    Console.WriteLine($"An unexpected exception occurred: {ex.GetType().FullName}");
    Console.WriteLine($"Message: {ex.Message}");
    Console.WriteLine($"StackTrace: {ex.StackTrace}");
    throw;
}

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();