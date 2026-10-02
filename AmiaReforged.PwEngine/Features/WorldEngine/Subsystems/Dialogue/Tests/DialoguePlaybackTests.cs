using AmiaReforged.Shared.Dialogue;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Tests;

[TestFixture]
public class DialoguePlaybackTests
{
    private static DialogueNodeDto Node(string type = "NpcText") => new() { Id = Guid.NewGuid().ToString(), Type = type, Text = type };
    private static DialogueChoiceDto Link(DialogueNodeDto target, params DialogueConditionDto[] conditions) => new()
    {
        Id = Guid.NewGuid().ToString(), TargetNodeId = target.Id, ResponseText = "Reply", Conditions = conditions.ToList()
    };
    private static DialogueConditionDto Condition(string value = "yes") => new() { Type = "LocalVariable", Parameters = new() { ["variableName"] = "test", ["expectedValue"] = value } };
    private static DialogueTreeDto Tree(params DialogueNodeDto[] nodes) => new() { DialogueTreeId = "test", Title = "Test", RootNodeId = nodes[0].Id, Nodes = nodes.ToList() };
    private static Task<bool> Evaluate(IReadOnlyList<DialogueConditionDto> conditions) => Task.FromResult(conditions.All(c => c.Parameters["expectedValue"] == "yes"));
    private static Task<bool> Enter(DialogueNodeDto _) => Task.FromResult(true);

    [Test]
    public async Task ConditionalGreetingWinsOverEarlierUnconditionalDefault()
    {
        DialogueNodeDto fallback = Node("Root"), conditional = Node("Root");
        conditional.SortOrder = 10;
        conditional.Conditions.Add(Condition());
        DialoguePlayback playback = new(Tree(fallback, conditional));
        Assert.That((await playback.StartAsync(Evaluate, Enter)).Success, Is.True);
        Assert.That(playback.CurrentNodeId, Is.EqualTo(conditional.Id));
    }

    [Test]
    public async Task FailedConditionsUseOnlyExplicitUnconditionalDefault()
    {
        DialogueNodeDto fallback = Node("Root"), conditional = Node("Root");
        conditional.Conditions.Add(Condition("no"));
        DialoguePlayback playback = new(Tree(fallback, conditional));
        await playback.StartAsync(Evaluate, Enter);
        Assert.That(playback.CurrentNodeId, Is.EqualTo(fallback.Id));
    }

    [Test]
    public async Task SingleConditionalGreetingDoesNotBypassItsConditions()
    {
        DialogueNodeDto root = Node("Root");
        root.Conditions.Add(Condition("no"));
        DialoguePlayback playback = new(Tree(root));
        Assert.That((await playback.StartAsync(Evaluate, Enter)).Success, Is.False);
        Assert.That(playback.CurrentNodeId, Is.Null);
    }

    [Test]
    public async Task ConditionalGreetingsUsePriorityThenStableId()
    {
        DialogueNodeDto first = Node("Root"), second = Node("Root");
        first.Conditions.Add(Condition()); second.Conditions.Add(Condition());
        first.SortOrder = 20; second.SortOrder = 1;
        DialoguePlayback playback = new(Tree(first, second));
        await playback.StartAsync(Evaluate, Enter);
        Assert.That(playback.CurrentNodeId, Is.EqualTo(second.Id));
    }

    [Test]
    public async Task ChangedConditionsCannotRedirectDisplayedReplyToAnotherChoice()
    {
        DialogueNodeDto root = Node("Root"), first = Node(), second = Node();
        DialogueConditionDto condition = Condition();
        DialogueChoiceDto firstReply = Link(first, condition), secondReply = Link(second);
        root.Choices.AddRange([firstReply, secondReply]);
        DialoguePlayback playback = new(Tree(root, first, second));
        await playback.StartAsync(Evaluate, Enter);
        List<DialogueChoiceDto> visible = await playback.GetVisibleChoicesAsync(Evaluate);
        condition.Parameters["expectedValue"] = "no";
        Assert.That((await playback.ChooseAsync(root.Id, visible[0].Id, Evaluate, Enter)).Success, Is.False);
        Assert.That(playback.CurrentNodeId, Is.EqualTo(root.Id));
        Assert.That((await playback.ChooseAsync(root.Id, secondReply.Id, Evaluate, Enter)).Success, Is.True);
        Assert.That(playback.CurrentNodeId, Is.EqualTo(second.Id));
    }

    [Test]
    public async Task EntryConditionsAlsoControlReplyVisibility()
    {
        DialogueNodeDto root = Node("Root"), target = Node();
        target.Conditions.Add(Condition("no")); root.Choices.Add(Link(target));
        DialoguePlayback playback = new(Tree(root, target));
        await playback.StartAsync(Evaluate, Enter);
        Assert.That(await playback.GetVisibleChoicesAsync(Evaluate), Is.Empty);
    }

    [Test]
    public async Task ActionChainEntersEveryNodeOnceAndKeepsFinalText()
    {
        DialogueNodeDto root = Node("Root"), action = Node("Action"), ending = Node("End");
        ending.Text = "Goodbye!";
        root.Choices.Add(Link(action)); action.Choices.Add(Link(ending));
        List<string> entered = [];
        Task<bool> Record(DialogueNodeDto node) { entered.Add(node.Id); return Task.FromResult(true); }
        DialoguePlayback playback = new(Tree(root, action, ending));
        await playback.StartAsync(Evaluate, Record);
        await playback.ChooseAsync(root.Id, root.Choices[0].Id, Evaluate, Record);
        Assert.That(entered, Is.EqualTo(new[] { root.Id, action.Id, ending.Id }));
        Assert.That(playback.IsTerminal, Is.True);
        Assert.That(playback.CurrentNode!.Text, Is.EqualTo("Goodbye!"));
    }

    [Test]
    public async Task FailedActionStopsBeforeSubsequentNodes()
    {
        DialogueNodeDto root = Node("Root"), action = Node("Action"), target = Node();
        root.Choices.Add(Link(action)); action.Choices.Add(Link(target));
        List<string> entered = [];
        Task<bool> Record(DialogueNodeDto node) { entered.Add(node.Id); return Task.FromResult(node.Id != action.Id); }
        DialoguePlayback playback = new(Tree(root, action, target));
        await playback.StartAsync(Evaluate, Record);
        Assert.That((await playback.ChooseAsync(root.Id, root.Choices[0].Id, Evaluate, Record)).Success, Is.False);
        Assert.That(entered, Is.EqualTo(new[] { root.Id, action.Id }));
    }

    [Test]
    public async Task ConditionalActionContinuationCannotBeBypassed()
    {
        DialogueNodeDto root = Node("Root"), action = Node("Action"), target = Node();
        root.Choices.Add(Link(action)); action.Choices.Add(Link(target, Condition("no")));
        DialoguePlayback playback = new(Tree(root, action, target));
        await playback.StartAsync(Evaluate, Enter);
        Assert.That((await playback.ChooseAsync(root.Id, root.Choices[0].Id, Evaluate, Enter)).Success, Is.False);
        Assert.That(playback.CurrentNodeId, Is.EqualTo(action.Id));
    }

    [Test]
    public async Task AutomaticCycleStopsBeforeReplayingActions()
    {
        DialogueNodeDto root = Node("Root"), action = Node("Action");
        root.Choices.Add(Link(action)); action.Choices.Add(Link(action));
        int actionsEntered = 0;
        Task<bool> Record(DialogueNodeDto node) { if (node == action) actionsEntered++; return Task.FromResult(true); }
        DialoguePlayback playback = new(Tree(root, action));
        await playback.StartAsync(Evaluate, Record);
        Assert.That((await playback.ChooseAsync(root.Id, root.Choices[0].Id, Evaluate, Record)).Success, Is.False);
        Assert.That(actionsEntered, Is.EqualTo(1));
    }

    [Test]
    public async Task OverlappingClicksDoNotExecuteEffectsTwice()
    {
        DialogueNodeDto root = Node("Root"), target = Node(); root.Choices.Add(Link(target));
        DialoguePlayback playback = new(Tree(root, target)); await playback.StartAsync(Evaluate, Enter);
        TaskCompletionSource<bool> gate = new(); int entries = 0;
        Task<bool> Block(DialogueNodeDto _) { entries++; return gate.Task; }
        Task<DialoguePlaybackResult> first = playback.ChooseAsync(root.Id, root.Choices[0].Id, Evaluate, Block);
        Assert.That((await playback.ChooseAsync(root.Id, root.Choices[0].Id, Evaluate, Block)).Success, Is.False);
        gate.SetResult(true); await first;
        Assert.That(entries, Is.EqualTo(1));
    }
}
