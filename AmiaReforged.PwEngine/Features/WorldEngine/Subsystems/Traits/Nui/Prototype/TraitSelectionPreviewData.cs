namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui.Prototype;

internal sealed record TraitPreviewRow(string Name, string Category, int Cost, bool Acquired);

/// <summary>Pure sample data; never loads a character or calls a selection/persistence service.</summary>
internal sealed record TraitSelectionPreviewData(IReadOnlyList<TraitPreviewRow> Traits, int Page, int ViewedIndex,
    string ActiveCategory, TraitBudget Budget, string DetailTitle, string DetailBody, bool ShowSelect, bool ShowRemove,
    bool Enabled)
{
    public int PageCount => Math.Max(1, (Traits.Count + TraitSelectionGraphicalView.EntriesPerPage - 1) / TraitSelectionGraphicalView.EntriesPerPage);

    public static TraitSelectionPreviewData Create(string scenario, int? count = null)
    {
        int total = count ?? (scenario switch
        {
            "empty" or "error" => 0, "single" => 1, "full" or "disabled" => 8,
            "overflow" or "last" => 17, "debt" or "zero" => 4, _ => 2
        });
        if (total is < 0 or > 32) throw new ArgumentOutOfRangeException(nameof(count));
        int page = scenario == "last" ? Math.Max(0, (total - 1) / TraitSelectionGraphicalView.EntriesPerPage) : 0;
        int viewed = total == 0 ? -1 : scenario is "remove" or "confirmed" ? 0
            : page * TraitSelectionGraphicalView.EntriesPerPage + Math.Min(1, total - 1 - page * TraitSelectionGraphicalView.EntriesPerPage);
        (string Name, string Category, int Cost)[] examples =
        [
            ("Brave", "Mental", 1), ("Cowardly", "Background", -1), ("Keen Eyes", "Physical", 1),
            ("Patient", "Personality", 0), ("Strong Back", "Physical", 1), ("Bookworm", "Mental", 1),
            ("Silver Tongue", "Social", 1), ("Lucky", "Blessing", 1)
        ];
        List<TraitPreviewRow> traits = [];
        for (int i = 0; i < total; i++)
        {
            var (name, category, cost) = examples[i % examples.Length];
            if (i >= examples.Length) name += $" {i / examples.Length + 1}";
            if (scenario == "category" && i == 1) (name, category, cost) = ("Bookworm", "Mental", 1);
            if (scenario == "debt" && i == 2) (name, cost) = ("Expensive Sample Trait", 3);
            if (scenario == "long")
            {
                name += " — A deliberately long sample trait name for native clipping and tooltip review";
                category += " — a deliberately long sample subtitle for native clipping review";
            }
            bool acquired = (total >= 2 && i == 0) || ((scenario is "debt" or "zero") && i == 2);
            traits.Add(new TraitPreviewRow(name, category, cost, acquired));
        }
        TraitBudget budget = new() { SpentPoints = traits.Where(t => t.Acquired).Sum(t => t.Cost) };
        TraitPreviewRow? detail = viewed >= 0 ? traits[viewed] : null;
        string title = detail?.Name ?? (scenario == "error" ? "Character unavailable" : "Select a Trait");
        string body = detail == null ? scenario == "error"
            ? "No character key found. This is a visual-only error-state sample."
            : "Choose a trait from the list to view its details."
            : $"{(viewed == 1 && scenario != "category" ? "You are a scaredy lil baby mensch" : "Sample description for this trait.")}\n\nCost: {detail.Cost} point(s)";
        if (scenario == "long")
            body = string.Join("\n\n", Enumerable.Range(1, 18).Select(i =>
                $"Passage {i}. Sample trait description with enough text to exercise native wrapping and the detail scrollbar. " +
                "The title, panel frame and detail action must remain fixed while reading.")) +
                "\n\nPrerequisites: sample_prerequisite\nConflicts with: sample_conflict\nRaces: Human, Elf\nClasses: Fighter, Wizard" +
                $"\n\nCost: {detail!.Cost} point(s)\n\nEND OF LONG TRAIT SAMPLE";
        return new(traits, page, viewed, scenario == "category" ? "mental" : "all", budget, title, body,
            detail is { Acquired: false }, detail is { Acquired: true } && scenario != "confirmed",
            scenario is not ("disabled" or "error"));
    }
}
