using System.Net.Http.Headers;
using AspNetCore.Swagger.Themes;
using Polly;
using Polly.Extensions.Http;
using Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddHttpClient("frankfurter", client =>
    {
        client.BaseAddress = new Uri("https://api.frankfurter.app/");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    })
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
builder.Services.AddScoped<ICurrencyService, CurrencyService>();

builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(ModernStyle.DeepSea);
}

app.UseHttpsRedirection();


app.UseAuthentication();


app.UseRouting();

app.UseAuthorization();


app.MapControllers();


app.UseStaticFiles();

app.UseDirectoryBrowser();


app.Run();