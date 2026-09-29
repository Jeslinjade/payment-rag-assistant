using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

const int width = 54;
var apiUrl = "http://localhost:5000/api/query";

var question = args.Length > 0
    ? string.Join(' ', args)
    : Prompt("Ask the payment assistant something: ");

PrintHeader();
Console.WriteLine();
Console.WriteLine("Query:");
Console.WriteLine(question);
Console.WriteLine();
PrintDivider();
Console.WriteLine("RESPONSE");
PrintDivider();
Console.WriteLine();

using var http = new HttpClient();
var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
{
    Content = JsonContent.Create(new { question })
};

using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
response.EnsureSuccessStatusCode();

await using var stream = await response.Content.ReadAsStreamAsync();
using var reader = new StreamReader(stream);

var answer = new StringBuilder();
string? currentEvent = null;
JsonElement? metadata = null;

while (!reader.EndOfStream)
{
    var line = await reader.ReadLineAsync();
    if (string.IsNullOrWhiteSpace(line)) continue;

    if (line.StartsWith("event: "))
    {
        currentEvent = line["event: ".Length..].Trim();
        continue;
    }

    if (line.StartsWith("data: "))
    {
        var payload = line["data: ".Length..];

        if (currentEvent == "metadata")
        {
            metadata = JsonSerializer.Deserialize<JsonElement>(payload);
            currentEvent = null;
        }
        else
        {
            answer.Append(payload);
        }
    }
}

foreach (var wrapped in WordWrap(answer.ToString(), width - 2))
    Console.WriteLine(wrapped);

Console.WriteLine();
PrintDivider();
Console.WriteLine("RAG METADATA");
PrintDivider();
Console.WriteLine();

if (metadata is { } meta)
{
    var confidence = meta.GetProperty("confidence").GetDouble();
    var gap = meta.GetProperty("knowledgeGap").GetBoolean();
    var retries = meta.GetProperty("retries").GetInt32();
    var sources = meta.GetProperty("sources").EnumerateArray()
        .Select(s => s.GetProperty("SourceDocument").GetString())
        .Distinct();

    Console.WriteLine($"Confidence    : {confidence:0.00}");
    Console.WriteLine($"Knowledge Gap : {gap.ToString().ToLower()}");
    Console.WriteLine($"Retries       : {retries}");
    Console.WriteLine();
    Console.WriteLine("Sources:");
    foreach (var s in sources)
        Console.WriteLine($"  \u2022 {s}");
}
else
{
    Console.WriteLine("(no metadata received)");
}

Console.WriteLine();
PrintDivider();

static string Prompt(string label)
{
    Console.Write(label);
    return Console.ReadLine() ?? string.Empty;
}

void PrintHeader()
{
    Console.WriteLine("\u2554" + new string('\u2550', width) + "\u2557");
    var title = "PAYMENT RAG ASSISTANT";
    var pad = (width - title.Length) / 2;
    Console.WriteLine("\u2551" + new string(' ', pad) + title + new string(' ', width - pad - title.Length) + "\u2551");
    Console.WriteLine("\u255A" + new string('\u2550', width) + "\u255D");
}

void PrintDivider() => Console.WriteLine(new string('\u2500', width));

static IEnumerable<string> WordWrap(string text, int maxWidth)
{
    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var line = new StringBuilder();

    foreach (var word in words)
    {
        if (line.Length + word.Length + 1 > maxWidth)
        {
            yield return line.ToString();
            line.Clear();
        }
        if (line.Length > 0) line.Append(' ');
        line.Append(word);
    }
    if (line.Length > 0) yield return line.ToString();
}
