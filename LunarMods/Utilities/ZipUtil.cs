namespace LunarMods.Utilities;

public static class ZipUtil
{
    public static string Format(IEnumerable<string> entries)
    {
        Directory top = new(string.Empty);
        foreach (string entry in entries)
        {
            bool directory = entry.EndsWith('/');
            string splitEntry = directory ? entry.Remove(entry.Length - 1, 1) : entry;
            string[] parts = splitEntry.Split('/');
            Directory parent = top;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                Directory? current = (Directory?)parent.Entries.FirstOrDefault(n => n.FileName == parts[i]);
                if (current == null)
                {
                    current = new Directory(parts[i]);
                    parent.Entries.Add(current);
                }

                parent = current;
            }

            string last = parts[^1];
            Entry file = directory ? new Directory(last) : new Entry(last);
            parent.Entries.Add(file);
        }

        List<string> result = new();
        FormatRecursively(result, top, string.Empty, null, true);
        return string.Join("\n", result);
    }

    private static void FormatRecursively(List<string> list, Entry entry, string prefix, string? prepre, bool isEnd)
    {
        if (prepre != null)
        {
            list.Add($"{prepre}{prefix}{entry.FileName}");
        }

        if (entry is not Directory directory)
        {
            return;
        }

        string preprepre = prepre != null ? prepre + (isEnd ? "    " : "│   ") : string.Empty;
        Entry[] entries = directory.Entries.OrderByDescending(n => n is Directory).ThenBy(n => n.FileName).ToArray();
        for (int i = 0; i < entries.Length - 1; i++)
        {
            FormatRecursively(list, entries[i], "├── ", preprepre, false);
        }

        Entry last = entries[^1];

        // ReSharper disable once TailRecursiveCall
        FormatRecursively(list, last, "└── ", preprepre, true);
    }

    public class Entry
    {
        public Entry(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; }
    }

    public class Directory : Entry
    {
        public List<Entry> Entries { get; } = new();

        public Directory(string fileName) : base(fileName)
        {
        }
    }
}
