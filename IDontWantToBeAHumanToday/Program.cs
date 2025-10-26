using System.Text;

namespace IDontWantToBeAHumanToday;

internal static class Program
{
    private static readonly (string Name, string Description, string[] Activities)[] Transformations =
    {
        (
            "Cloud",
            "drifting lazily across the sky",
            new[]
            {
                "float over the city",
                "listen to the murmur of distant thunderstorms",
                "cast a gentle shadow on a field of clover"
            }
        ),
        (
            "Cat",
            "stretching in a sunbeam with zero responsibilities",
            new[]
            {
                "nap for the third time this morning",
                "charm a stranger into providing snacks",
                "knock pens from a desk with unapologetic grace"
            }
        ),
        (
            "Rainstorm",
            "tapping rhythms on rooftops and windowpanes",
            new[]
            {
                "wash the city clean",
                "sing to the people who forgot their umbrellas",
                "feed flowers and trees with a gentle soak"
            }
        ),
        (
            "Library Ghost",
            "haunting the stacks with whispers of stories",
            new[]
            {
                "rearrange books so the right reader finds them",
                "turn pages for someone dozing at a desk",
                "collect forgotten memories between the shelves"
            }
        )
    };

    private static readonly string[] GroundingRitual =
    {
        "Stretch your arms overhead.",
        "Notice three things you can see.",
        "Take a deep breath for a slow count of four.",
        "Remind yourself that tomorrow can be different."
    };

    private static readonly string Divider = new('-', 50);

    private static readonly Dictionary<string, string> CompanionNotes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cloud"] = "Nimbus the wandering cloud promises to carry your worries away.",
        ["cat"] = "Whiskers the cat offers lessons in unapologetic rest.",
        ["rainstorm"] = "Pluvius the rainstorm hums lullabies in a thousand droplets.",
        ["library ghost"] = "Page the library ghost archives your stress for safekeeping."
    };

    private static readonly string[] Farewells =
    {
        "Remember: opting out is temporary, but rest is essential.",
        "Carry this softness back with you when you're ready.",
        "Being human tomorrow can wait; savor the calm you created today.",
        "The world can turn without you for a little while—let it."
    };

    private static readonly Random Rng = new();

    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        PrintIntro();

        var choice = PromptForTransformation();
        DescribeTransformation(choice);
        OfferCompanion(choice);
        WalkThroughActivities(choice);
        GuideGrounding();
        SayFarewell();
    }

    private static void PrintIntro()
    {
        Console.WriteLine(Divider);
        Console.WriteLine("I Don't Want to Be a Human Today\n");
        Console.WriteLine("Welcome. You've found a pocket dimension for temporary escape.");
        Console.WriteLine("Pick a form that feels gentle, and let the day reshape itself around you.\n");
    }

    private static string PromptForTransformation()
    {
        Console.WriteLine("Who will you be today? Choose one:");
        for (var index = 0; index < Transformations.Length; index++)
        {
            var option = Transformations[index];
            Console.WriteLine($"  {index + 1}. {option.Name} — {option.Description}");
        }

        Console.WriteLine();
        Console.Write("Type the number or name of your choice: ");
        while (true)
        {
            var response = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(response))
            {
                Console.Write("Try again with a number or name: ");
                continue;
            }

            var trimmed = response.Trim();
            if (int.TryParse(trimmed, out var numericChoice))
            {
                var index = numericChoice - 1;
                if (index >= 0 && index < Transformations.Length)
                {
                    return Transformations[index].Name;
                }
            }
            else
            {
                var match = Transformations.FirstOrDefault(t =>
                    string.Equals(t.Name, trimmed, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match.Name))
                {
                    return match.Name;
                }
            }

            Console.Write("That option isn't in our sanctuary. Try again: ");
        }
    }

    private static void DescribeTransformation(string choice)
    {
        var info = Transformations.First(t => string.Equals(t.Name, choice, StringComparison.OrdinalIgnoreCase));
        Console.WriteLine($"\n{Divider}");
        Console.WriteLine($"You dissolve into being a {info.Name}.\n");
        Console.WriteLine($"Experience: {info.Description}.");
    }

    private static void OfferCompanion(string choice)
    {
        if (CompanionNotes.TryGetValue(choice, out var note))
        {
            Console.WriteLine();
            Console.WriteLine(note);
        }
    }

    private static void WalkThroughActivities(string choice)
    {
        var info = Transformations.First(t => string.Equals(t.Name, choice, StringComparison.OrdinalIgnoreCase));

        Console.WriteLine();
        Console.WriteLine("Pick an activity to savor (or press Enter to let fate decide):");
        for (var index = 0; index < info.Activities.Length; index++)
        {
            Console.WriteLine($"  {index + 1}. {info.Activities[index]}");
        }

        Console.Write("Your activity: ");
        var response = Console.ReadLine();
        string selectedActivity;
        if (string.IsNullOrWhiteSpace(response))
        {
            selectedActivity = info.Activities[Rng.Next(info.Activities.Length)];
        }
        else if (int.TryParse(response.Trim(), out var numericChoice))
        {
            var index = Math.Clamp(numericChoice - 1, 0, info.Activities.Length - 1);
            selectedActivity = info.Activities[index];
        }
        else
        {
            selectedActivity = info.Activities
                .FirstOrDefault(a => a.Contains(response!, StringComparison.OrdinalIgnoreCase))
                ?? info.Activities[Rng.Next(info.Activities.Length)];
        }

        Console.WriteLine($"\nAs a {info.Name.ToLowerInvariant()}, you {selectedActivity}.");
        Console.WriteLine("Let that image linger for as long as you need.\n");
    }

    private static void GuideGrounding()
    {
        Console.WriteLine(Divider);
        Console.WriteLine("When you are ready to drift back toward humanity, try this grounding ritual:");

        foreach (var step in GroundingRitual)
        {
            Console.WriteLine($" • {step}");
        }

        Console.WriteLine();
    }

    private static void SayFarewell()
    {
        var farewell = Farewells[Rng.Next(Farewells.Length)];
        Console.WriteLine(farewell);
        Console.WriteLine("Return whenever you need to disrobe your mortal coil for a moment.\n");
    }
}
