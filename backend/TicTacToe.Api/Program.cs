using TicTacToe.Api.Data;
using TicTacToe.Api.Endpoints;
using TicTacToe.Api.Services;

const string AngularCors = "AngularDevServer";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataLayer(builder.Configuration.GetConnectionString("Default"));
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<ScoreboardService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o => o.AddPolicy(AngularCors, p => p
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();
app.UseCors(AngularCors);

app.UseSwagger();
app.UseSwaggerUI();

app.MapApi();

app.Run();
