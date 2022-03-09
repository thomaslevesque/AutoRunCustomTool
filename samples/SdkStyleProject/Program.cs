PrintFileContent("trigger.txt");
PrintFileContent("target.txt");
PrintFileContent("target2.txt");

static void PrintFileContent(string fileName)
{
    var content = File.ReadAllText(fileName);
    Console.WriteLine($"Content of '{fileName}':");
    Console.WriteLine(content);
    Console.WriteLine();
}
