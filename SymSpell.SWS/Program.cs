var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.IncludeFields = true;
});

// Configure and register SymSpell as a singleton
const int initialCapacity = 82765;
const int maxEditDistance = 2;
const int prefixLength = 7;
var symSpell = new SymSpell(initialCapacity, maxEditDistance, prefixLength);

// Load dictionary
string path = Path.Combine(AppContext.BaseDirectory, "frequency_dictionary_en_82_765.txt");
if (!symSpell.LoadDictionary(path, 0, 1))
{
    Console.Error.WriteLine("Dictionary file not found: " + Path.GetFullPath(path));
    // App will start, but lookups will be incorrect.
}
builder.Services.AddSingleton(symSpell);


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
