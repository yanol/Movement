using DataService;
using DataService.Cache;
using Microsoft.EntityFrameworkCore;

SQLitePCL.Batteries.Init();
var builder = WebApplication.CreateBuilder(args);

// ── SQLite ────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=data.db"));

builder.Services.AddScoped<IDataRepository, DataRepository>();

// ── Redis ─────────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";
    options.InstanceName = "DataApi:";
});

// ── SDCS ──────────────────────────────────────────────────────
int capacity = builder.Configuration.GetValue<int>("Sdcs:Capacity");
builder.Services.AddSingleton<ISdcsCache<DataItem>>(
    new SdcsCache<DataItem>(capacity));

// ── Chain handlers ────────────────────────────────────────────
builder.Services.AddScoped<RedisCacheHandler>();
builder.Services.AddScoped<SdcsCacheHandler>();
builder.Services.AddScoped<DatabaseHandler>();

builder.Services.AddScoped<IDataRetrievalHandler>(sp =>
{
    var redis = sp.GetRequiredService<RedisCacheHandler>();
    var sdcs = sp.GetRequiredService<SdcsCacheHandler>();
    var db = sp.GetRequiredService<DatabaseHandler>();
    redis.SetNext(sdcs).SetNext(db);
    return redis;
});

builder.Services.AddScoped<IDataService, DataService.DataService>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();