using EmailMonitor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<EmailMonitorService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var cancellationTokenSource = new CancellationTokenSource();
var instance = builder.Services.BuildServiceProvider().GetService<EmailMonitorService>();

Task.Run(() => instance.StartMonitoringAsync(cancellationTokenSource.Token));

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
