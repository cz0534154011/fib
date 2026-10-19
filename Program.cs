
using System.CommandLine;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;

string[] supportedLanguages =
{
    "c#",
    "python",
    "java",
    "javascript",
    "html",
    "react"
};

var extensions = new Dictionary<string, string>
{
    ["c#"] = "*.cs",
    ["python"] = "*.py",
    ["java"] = "*.java",
    ["javascript"] = "*.js",
    ["html"] = "*.html",
    ["react"] = "*.jsx"
};

var rootCommand = new RootCommand("Root commands for file bundler CLI");

var bundleCommand = new Command("bundle", "Packaging code files into a single file");



Option<string[]> languageOption = new Option<string[]>("--language")
{
    Required = true
};

bundleCommand.Add(languageOption);

Option<string> outputOption = new Option<string>("--output");
bundleCommand.Add(outputOption);

Option<bool> noteOption = new Option<bool>("--note")
{
    DefaultValueFactory = _ => false
};
bundleCommand.Add(noteOption);

Option<string> sortOption = new Option<string>("--sort")
{
    DefaultValueFactory = _ => "name"
};
bundleCommand.Add(sortOption);

Option<bool> removeEmptyLineOption = new Option<bool>("--remove-empty-lines")
{
    DefaultValueFactory = _ => false
};
bundleCommand.Add(removeEmptyLineOption);

Option<string> authorOption = new Option<string>("--author");
bundleCommand.Add(authorOption);


bundleCommand.SetAction((parseResult) =>
{
    string[] languages = parseResult.GetValue(languageOption)!
    .SelectMany(language => language.Split(','))
    .ToArray();


    if (languages.Length == 1 && languages[0] == "all")
    {
        languages = supportedLanguages;
    }
    foreach (string language in languages)
    {
        if (!supportedLanguages.Contains(language))
        {
            Console.WriteLine($"The language: {language} is not valid");
            return; ;
        }
    }

    string folderPath = Directory.GetCurrentDirectory();
    string? outputPath = parseResult.GetValue(outputOption);

    if (string.IsNullOrWhiteSpace(outputPath))
    {
        Console.WriteLine("Enter output - file name");
        return;
    }


    string fullOutputPath = Path.GetFullPath(outputPath);
    string? directory = Path.GetDirectoryName(fullOutputPath);

    if (directory == null || !Directory.Exists(directory))
    {
        Console.WriteLine("he output directory does not exist");
        return;
    }


    //    bool isFileNameOnly =
    //Path.GetFileName(fullOutputPath) == outputPath;

    //    if (isFileNameOnly)
    //    {
    //        fullOutputPath = Path.Combine(folderPath, fullOutputPath);
    //    }

    var files = new List<string>();

    foreach (string language in languages)
    {
        if (extensions.TryGetValue(language, out string? pattern))
        {
            files.AddRange(
                Directory.GetFiles(folderPath, pattern)
            );
        }
    }

    files = files
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();

    bool addNote = parseResult.GetValue(noteOption);

    string sortBy = parseResult.GetValue(sortOption)!;

    if (sortBy == "name")
    {
        files = files
            .OrderBy(file => Path.GetFileName(file),
                     StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    else if (sortBy == "type")
    {
        files = files
            .OrderBy(file => Path.GetExtension(file),
                     StringComparer.OrdinalIgnoreCase)
            .ThenBy(file => Path.GetFileName(file),
                    StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    else
    {
        Console.WriteLine("Invalid sort value. Use 'name' or 'type'.");
        return;
    }

    bool removeEmptyLines =
    parseResult.GetValue(removeEmptyLineOption);

    var combinedContent = new StringBuilder();

    string? author = parseResult.GetValue(authorOption);

    if (!string.IsNullOrWhiteSpace(author))
    {
        combinedContent.AppendLine(author);
    }

    foreach (string file in files)
    {
        if (addNote)
        {
            string fileName = Path.GetFileName(file);
            string relativePath = Path.GetRelativePath(folderPath, file);

            combinedContent.AppendLine(
                $"===== Source: {fileName} | Path: {relativePath} =====");
        }
        string content = File.ReadAllText(file);

        if (removeEmptyLines)
        {
            content = string.Join(
                Environment.NewLine,
                content.Split(
                    new[] { "\r\n", "\n", "\r" },
                    StringSplitOptions.None)
                .Where(line => !string.IsNullOrWhiteSpace(line))
            );
        }

        combinedContent.AppendLine(content);
        combinedContent.AppendLine();
    }

    File.WriteAllText(fullOutputPath, combinedContent.ToString());

});


var createRspCommand = new Command("create-rsp", "Creating a command according to your requirements");

createRspCommand.SetAction((parseResult) =>
{
    // 1. קבלת שפות
    string language;

    while (true)
    {
        Console.WriteLine(
            "Enter languages separated by commas, or 'all':");

        language = (Console.ReadLine() ?? "")
            .Trim()
            .ToLowerInvariant();

        if (language == "all")
            break;

        string[] selectedLanguages = language.Split(
            ',',
            StringSplitOptions.TrimEntries |
            StringSplitOptions.RemoveEmptyEntries);

        if (selectedLanguages.Length == 0)
        {
            Console.WriteLine("You must enter at least one language.");
            continue;
        }

        if (selectedLanguages.Any(
            lang => !supportedLanguages.Contains(lang)))
        {
            Console.WriteLine("One or more languages are not supported.");
            continue;
        }

        break;
    }

    // 2. קבלת שם קובץ הפלט
    string output;

    while (true)
    {
        Console.WriteLine("Enter output file path:");

        output = (Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine("Output path is required.");
            continue;
        }

        try
        {
            string fullPath = Path.GetFullPath(output);
            string? directory = Path.GetDirectoryName(fullPath);

            if (directory == null || !Directory.Exists(directory))
            {
                Console.WriteLine("The output directory does not exist.");
                continue;
            }

            output = fullPath;
            break;
        }
        catch (ArgumentException)
        {
            Console.WriteLine("Invalid output path.");
        }
        catch (NotSupportedException)
        {
            Console.WriteLine("Unsupported output path.");
        }
        catch (PathTooLongException)
        {
            Console.WriteLine("Output path is too long.");
        }
    }

    // 3. קבלת אפשרות note
    bool note;

    while (true)
    {
        Console.WriteLine("Include notes? (true/false)");

        if (bool.TryParse(Console.ReadLine(), out note))
            break;

        Console.WriteLine("Enter true or false.");
    }

    // 4. קבלת אפשרות sort
    string sort;

    while (true)
    {
        Console.WriteLine("Sort by name or type:");

        sort = (Console.ReadLine() ?? "")
            .Trim()
            .ToLowerInvariant();

        if (sort == "name" || sort == "type")
            break;

        Console.WriteLine("Enter 'name' or 'type'.");
    }

    // 5. קבלת אפשרות remove-empty-lines
    bool removeEmptyLines;

    while (true)
    {
        Console.WriteLine("Remove empty lines? (true/false)");

        if (bool.TryParse(Console.ReadLine(), out removeEmptyLines))
            break;

        Console.WriteLine("Enter true or false.");
    }

    // 6. קבלת שם המחבר — אופציונלי
    Console.WriteLine("Enter author (optional):");

    string? author = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(author))
        author = null;



    // בניית תוכן קובץ ה-Response File
    StringBuilder responseContent = new StringBuilder();

    responseContent.AppendLine("bundle");
    responseContent.AppendLine("--language");
    responseContent.AppendLine($"\"{language}\"");
    responseContent.AppendLine("--output");
    responseContent.AppendLine($"\"{output}\"");
    responseContent.AppendLine("--sort");
    responseContent.AppendLine(sort);

    if (note)
        responseContent.AppendLine("--note");

    if (removeEmptyLines)
        responseContent.AppendLine("--remove-empty-lines");

    if (author != null)
    {
        responseContent.AppendLine("--author");
        responseContent.AppendLine($"\"{author}\"");
    }

    // קבלת שם קובץ התגובה
    Console.WriteLine("Enter response file name (default: bundle.rsp):");

    string responsePath = Console.ReadLine()?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(responsePath))
        responsePath = "bundle.rsp";

    responsePath = Path.GetFullPath(responsePath);

    string? responseDirectory = Path.GetDirectoryName(responsePath);

    if (responseDirectory == null ||
        !Directory.Exists(responseDirectory))
    {
        Console.WriteLine("The response file directory does not exist.");
        return;
    }

    // שמירת קובץ התגובה
    try
    {
        File.WriteAllText(responsePath, responseContent.ToString());

        Console.WriteLine($"Response file created: {responsePath}");
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Error writing response file: {ex.Message}");
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.WriteLine($"Access denied: {ex.Message}");
    }

});



rootCommand.Add(createRspCommand);

rootCommand.Add(bundleCommand);

await rootCommand.Parse(args).InvokeAsync();

