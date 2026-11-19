// Intent-classification eval: runs the real hybrid classifier (keyword rules + local model) over a labeled
// set of WhatsApp messages and writes a Markdown report. Needs a running Ollama server; never runs in CI.
//
//   dotnet run --project tools/AcademyWhatsAppBot.Evals -- --label baseline [--model qwen2.5:3b] [--endpoint http://localhost:11434]

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

using AcademyWhatsAppBot.Api.Intents;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using OllamaSharp;

var settings = ParseArgs(args);
var root = FindRepositoryRoot();
var model = settings.GetValueOrDefault("model", "qwen2.5:3b");
var endpoint = settings.GetValueOrDefault("endpoint", "http://localhost:11434");
var label = settings.GetValueOrDefault("label", "run");
var casesPath = Path.Combine(root, settings.GetValueOrDefault("cases", "evals/intent-cases.json"));

var cases = JsonSerializer.Deserialize<List<EvalCase>>(File.ReadAllText(casesPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("No cases found.");

using var chatClient = new OllamaApiClient(new Uri(endpoint), model);
var keywords = new KeywordIntentClassifier();
var classifier = new HybridIntentClassifier(
    keywords,
    new LlmIntentClassifier(chatClient),
    Options.Create(new AiOptions { Provider = AiProviders.Ollama, Endpoint = endpoint, Model = model, TimeoutSeconds = 120 }),
    NullLogger<HybridIntentClassifier>.Instance);

// Load the model first so the first case doesn't include the load time.
await classifier.ClassifyAsync("hola, una pregunta");

var results = new List<EvalResult>();
foreach (var evalCase in cases)
{
    var expected = ParseIntent(evalCase.Expected);
    var byRules = KeywordIntentClassifier.Classify(evalCase.Text) != Intent.Unknown;
    var stopwatch = Stopwatch.StartNew();
    var actual = await classifier.ClassifyAsync(evalCase.Text);
    stopwatch.Stop();

    var result = new EvalResult(evalCase.Text, expected, actual, byRules ? "rules" : "model", stopwatch.Elapsed.TotalSeconds);
    results.Add(result);
    Console.WriteLine($"[{(result.Passed ? "OK" : "XX")}] {result.Source,-5} {result.Seconds,5:0.0}s  {evalCase.Text}  expected={expected} actual={actual}");
}

var report = BuildReport(label, model, results);
var outputPath = Path.Combine(root, "evals", "results", $"{label}.md");
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, report);

var passed = results.Count(result => result.Passed);
Console.WriteLine();
Console.WriteLine($"Accuracy: {passed}/{results.Count} ({100.0 * passed / results.Count:0.0}%)");
Console.WriteLine($"Report: {Path.GetRelativePath(root, outputPath)}");

static string BuildReport(string label, string model, List<EvalResult> results)
{
    var culture = CultureInfo.InvariantCulture;
    var passed = results.Count(result => result.Passed);
    var modelResults = results.Where(result => result.Source == "model").ToList();
    var builder = new StringBuilder();

    builder.AppendLine(culture, $"# Intent eval: {label}");
    builder.AppendLine();
    builder.AppendLine(culture, $"- Model: `{model}` (Ollama, CPU only), keyword rules first");
    builder.AppendLine(culture, $"- Cases: {results.Count} (`evals/intent-cases.json`)");
    builder.AppendLine(culture, $"- **Accuracy: {passed}/{results.Count} ({100.0 * passed / results.Count:0.0}%)**");
    builder.AppendLine(culture, $"- Answered by rules: {results.Count - modelResults.Count}, by the model: {modelResults.Count}");
    if (modelResults.Count > 0)
    {
        builder.AppendLine(culture, $"- Model latency: avg {modelResults.Average(result => result.Seconds):0.0} s, max {modelResults.Max(result => result.Seconds):0.0} s");
    }

    builder.AppendLine();
    builder.AppendLine("| Intent | Correct |");
    builder.AppendLine("|---|---|");
    foreach (var group in results.GroupBy(result => result.Expected).OrderBy(group => group.Key))
    {
        builder.AppendLine(culture, $"| {group.Key} | {group.Count(result => result.Passed)}/{group.Count()} |");
    }

    var failures = results.Where(result => !result.Passed).ToList();
    builder.AppendLine();
    builder.AppendLine(failures.Count == 0 ? "No failures." : "## Failures");
    if (failures.Count > 0)
    {
        builder.AppendLine();
        builder.AppendLine("| Message | Expected | Actual | Source |");
        builder.AppendLine("|---|---|---|---|");
        foreach (var failure in failures)
        {
            builder.AppendLine(culture, $"| {failure.Text} | {failure.Expected} | {failure.Actual} | {failure.Source} |");
        }
    }

    return builder.ToString();
}

static Intent ParseIntent(string value) =>
    Enum.Parse<Intent>(value.Replace("_", string.Empty, StringComparison.Ordinal), ignoreCase: true);

static Dictionary<string, string> ParseArgs(string[] args)
{
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i + 1 < args.Length; i += 2)
    {
        values[args[i].TrimStart('-')] = args[i + 1];
    }

    return values;
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null && !directory.EnumerateFiles("*.slnx").Any())
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("Run from inside the repository.");
}

internal sealed record EvalCase(string Text, string Expected);

internal sealed record EvalResult(string Text, Intent Expected, Intent Actual, string Source, double Seconds)
{
    public bool Passed => Expected == Actual;
}
