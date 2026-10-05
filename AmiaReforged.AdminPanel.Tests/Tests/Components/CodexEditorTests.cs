using AmiaReforged.Shared.Quests;
using System.Net;
using System.Net.Http.Json;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class CodexEditorTests : Bunit.TestContext
{
    private readonly TestHttpMessageHandler _handler = new();

    public CodexEditorTests()
    {
        var httpClient = new HttpClient(_handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("WorldEngine")).Returns(httpClient);

        var endpointService = new Mock<IWorldEngineEndpointService>();
        endpointService
            .Setup(s => s.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorldEngineEndpoint
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                BaseUrl = "http://localhost:8080",
                ApiKey = "test-key"
            });

        Services.AddSingleton<LoreApiService>(new LoreApiService(factory.Object, endpointService.Object));
        Services.AddSingleton<QuestApiService>(new QuestApiService(factory.Object, endpointService.Object));
        Services.AddSingleton<IndustryApiService>(new IndustryApiService(factory.Object, endpointService.Object));
        Services.AddSingleton<ILogger<CodexEditor>>(new Mock<ILogger<CodexEditor>>().Object);
    }

    // ==================== Static Helpers: Lore Category Names ====================

    [Test]
    public void GetLoreCategoryName_ReturnsCorrectNames()
    {
        CodexEditor.CeGetLoreCategoryName(0).Should().Be("Arcana");
        CodexEditor.CeGetLoreCategoryName(1).Should().Be("Architecture & Engineering");
        CodexEditor.CeGetLoreCategoryName(2).Should().Be("Dungeoneering");
        CodexEditor.CeGetLoreCategoryName(3).Should().Be("Geography");
        CodexEditor.CeGetLoreCategoryName(4).Should().Be("History");
        CodexEditor.CeGetLoreCategoryName(5).Should().Be("Local");
        CodexEditor.CeGetLoreCategoryName(6).Should().Be("Nature");
        CodexEditor.CeGetLoreCategoryName(7).Should().Be("Nobility & Royalty");
        CodexEditor.CeGetLoreCategoryName(8).Should().Be("Religion");
        CodexEditor.CeGetLoreCategoryName(9).Should().Be("The Planes");
        CodexEditor.CeGetLoreCategoryName(10).Should().Be("OOC");
    }

    [Test]
    public void GetLoreCategoryName_ReturnsFallbackForUnknown()
    {
        CodexEditor.CeGetLoreCategoryName(99).Should().Be("Unknown (99)");
        CodexEditor.CeGetLoreCategoryName(-1).Should().Be("Unknown (-1)");
    }

    // ==================== Static Helpers: Lore Tier Names ====================

    [Test]
    public void GetLoreTierName_ReturnsCorrectNames()
    {
        CodexEditor.CeGetLoreTierName(0).Should().Be("Common");
        CodexEditor.CeGetLoreTierName(1).Should().Be("Uncommon");
        CodexEditor.CeGetLoreTierName(2).Should().Be("Rare");
        CodexEditor.CeGetLoreTierName(3).Should().Be("Legendary");
    }

    [Test]
    public void GetLoreTierName_ReturnsFallbackForUnknown()
    {
        CodexEditor.CeGetLoreTierName(99).Should().Be("Unknown (99)");
    }

    // ==================== Static Helpers: Config ====================

    [Test]
    public void GetConfigString_ReturnsValue_WhenKeyExists()
    {
        var obj = new ObjectiveDefinitionDto
        {
            Config = new Dictionary<string, object> { ["mode"] = "clue_graph" }
        };

        CodexEditor.GetConfigString(obj, "mode", "fallback").Should().Be("clue_graph");
    }

    [Test]
    public void GetConfigString_ReturnsFallback_WhenKeyMissing()
    {
        var obj = new ObjectiveDefinitionDto { Config = new Dictionary<string, object>() };

        CodexEditor.GetConfigString(obj, "missing", "fallback").Should().Be("fallback");
    }

    [Test]
    public void GetConfigString_ReturnsFallback_WhenConfigNull()
    {
        var obj = new ObjectiveDefinitionDto { Config = null };

        CodexEditor.GetConfigString(obj, "key", "fallback").Should().Be("fallback");
    }

    [Test]
    public void SetConfig_SetsValue_WhenConfigNull()
    {
        var obj = new ObjectiveDefinitionDto { Config = null };

        CodexEditor.SetConfig(obj, "key", "value");

        obj.Config.Should().NotBeNull();
        obj.Config!["key"]!.ToString().Should().Be("value");
    }

    [Test]
    public void SetConfig_OverwritesExistingValue()
    {
        var obj = new ObjectiveDefinitionDto
        {
            Config = new Dictionary<string, object> { ["key"] = "old" }
        };

        CodexEditor.SetConfig(obj, "key", "new");

        obj.Config!["key"]!.ToString().Should().Be("new");
    }

    [Test]
    public void SetConfig_CreatesMultipleKeys()
    {
        var obj = new ObjectiveDefinitionDto { Config = null };

        CodexEditor.SetConfig(obj, "k1", "v1");
        CodexEditor.SetConfig(obj, "k2", "v2");

        obj.Config.Should().HaveCount(2);
        obj.Config!["k1"]!.ToString().Should().Be("v1");
        obj.Config["k2"]!.ToString().Should().Be("v2");
    }

    // ==================== Rendering Basics ====================

    [Test]
    public void RendersToolbar_WithCancelAndSaveButtons()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.Find(".we-ce-toolbar").Should().NotBeNull();
        cut.FindAll("button").Should().Contain(b => b.TextContent.Contains("Cancel"));
        cut.FindAll("button").Should().Contain(b => b.TextContent.Contains("Save"));
    }

    [Test]
    public void RendersToolbar_WithoutViewMenuButton()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.FindAll("button").Should().NotContain(b => b.TextContent.Contains("View"));
    }

    [Test]
    public void RendersToolbar_ShowsLoreBadge()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.Find(".we-ce-toolbar").TextContent.Should().Contain("Lore");
    }

    [Test]
    public void RendersSplitLayout_InsteadOfGLContainer()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.FindAll("#we-codex-gl-container").Should().BeEmpty();
        cut.Find(".we-ce-layout-area .we-split").Should().NotBeNull();
        cut.FindAll(".we-ce-layout-area .we-split__pane").Should().HaveCountGreaterThan(1);
    }

    [Test]
    public void RendersLayoutArea()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.Find(".we-ce-layout-area").Should().NotBeNull();
    }

    // ==================== Callbacks ====================

    [Test]
    public void OnClose_Fires_WhenCancelClicked()
    {
        bool closeFired = false;

        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeFired = true))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.FindAll("button").Single(b => b.TextContent.Contains("Cancel")).Click();

        closeFired.Should().BeTrue();
    }

    // ==================== Deletion ====================

    [TestCase(CodexEditor.CodexSubType.Lore)]
    [TestCase(CodexEditor.CodexSubType.Quest)]
    public void Delete_RequiresConfirmation_AndCancelKeepsEntry(CodexEditor.CodexSubType subType)
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(subType);

        cut.Find(".we-ce-toolbar .btn-outline-danger").Click();

        cut.Find("[role='dialog']").TextContent.Should().Contain("Test entry").And.Contain("test_entry");
        _handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Delete);

        cut.Find("[role='dialog'] .btn-secondary").Click();

        cut.FindAll("[role='dialog']").Should().BeEmpty();
        cut.Find(".we-ce-toolbar").TextContent.Should().Contain("Test entry");
        _handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Delete);
    }

    [TestCase(CodexEditor.CodexSubType.Lore)]
    [TestCase(CodexEditor.CodexSubType.Quest)]
    public void Delete_RemovesEntry_ResetsForm_AndRefreshesLists(CodexEditor.CodexSubType subType)
    {
        int refreshCount = 0;
        IRenderedComponent<CodexEditor> cut = RenderEntry(subType, onRefresh: () => refreshCount++);
        int listRequestsBefore = _handler.Requests.Count(r => r.Path == EntryBase(subType));
        cut.FindAll(".we-ce-layout-area span").Should().Contain(e => e.TextContent == "Test entry");

        cut.Find(".we-ce-toolbar .btn-outline-danger").Click();
        cut.Find("[role='dialog'] .btn-danger").Click();

        cut.WaitForAssertion(() =>
        {
            _handler.Requests.Where(r => r.Method == HttpMethod.Delete).Should().ContainSingle()
                .Which.Path.Should().Be($"{EntryBase(subType)}/test_entry");
            refreshCount.Should().Be(1);
            _handler.Requests.Count(r => r.Path == EntryBase(subType)).Should().Be(listRequestsBefore + 1);
            cut.Find(".we-ce-toolbar").TextContent.Should().Contain($"New {subType}").And.Contain("entry deleted.");
            cut.FindAll(".we-ce-toolbar .btn-outline-danger").Should().BeEmpty();
            cut.FindAll("[role='dialog']").Should().BeEmpty();
            cut.Find($"input[placeholder='e.g. {(subType == CodexEditor.CodexSubType.Lore ? "lore_ancient_ruins" : "quest_lost_artifact")}']")
                .GetAttribute("value").Should().BeNullOrEmpty();
            cut.FindAll(".we-ce-layout-area span").Should().NotContain(e => e.TextContent == "Test entry");
        });
    }

    [TestCase(CodexEditor.CodexSubType.Lore)]
    [TestCase(CodexEditor.CodexSubType.Quest)]
    public async Task Delete_DisablesActionsUntilRequestCompletes(CodexEditor.CodexSubType subType)
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(subType);
        var completion = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _handler.DeleteResponse = completion.Task;
        cut.Find(".we-ce-toolbar .btn-outline-danger").Click();

        Task deleting = cut.Find("[role='dialog'] .btn-danger").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        try
        {
            cut.WaitForAssertion(() =>
            {
                cut.Find(".we-ce-toolbar .btn-outline-danger").TextContent.Should().Contain("Deleting");
                cut.FindAll(".we-ce-toolbar button").Should().OnlyContain(b => b.HasAttribute("disabled"));
                cut.Find("select").HasAttribute("disabled").Should().BeTrue();
                _handler.Requests.Where(r => r.Method == HttpMethod.Delete).Should().ContainSingle();
            });
        }
        finally
        {
            completion.SetResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            await deleting;
        }
    }

    [TestCase(CodexEditor.CodexSubType.Lore)]
    [TestCase(CodexEditor.CodexSubType.Quest)]
    public void Delete_ApiFailure_KeepsEntryAndShowsError(CodexEditor.CodexSubType subType)
    {
        int refreshCount = 0;
        IRenderedComponent<CodexEditor> cut = RenderEntry(subType, deleteFails: true, onRefresh: () => refreshCount++);

        cut.Find(".we-ce-toolbar .btn-outline-danger").Click();
        cut.Find("[role='dialog'] .btn-danger").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".we-ce-toolbar").TextContent.Should().Contain("Test entry").And.Contain("Delete failed");
            cut.Find(".we-ce-toolbar .btn-outline-danger").HasAttribute("disabled").Should().BeFalse();
            refreshCount.Should().Be(0);
        });
    }

    [TestCase(CodexEditor.CodexSubType.Lore)]
    [TestCase(CodexEditor.CodexSubType.Quest)]
    public void NewEntry_HasNoDeleteButton(CodexEditor.CodexSubType subType)
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(subType, isNew: true);

        cut.FindAll(".we-ce-toolbar .btn-outline-danger").Should().BeEmpty();
    }

    [Test]
    public void SwitchingEntryType_ClearsDeleteTarget()
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Lore);

        cut.Find("select").Change("Quest");

        cut.WaitForAssertion(() =>
        {
            cut.Find(".we-ce-toolbar").TextContent.Should().Contain("New Quest");
            cut.FindAll(".we-ce-toolbar .btn-outline-danger").Should().BeEmpty();
            _handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Delete);
        });
    }

    [Test]
    public void Quest_CreateAndUpdate_PreservesObjectivesTransitionsAndRewards()
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, isNew: true);
        cut.Find("[data-quest-title]").Input("Cellar Rats");
        cut.Find("[data-quest-description]").Input("Help the innkeeper clear the cellar.");
        cut.Find("[data-add-stage]").Click();
        cut.Find("[data-stage-name]").Input("Gather rat tails");
        cut.Find("[data-stage-journal]").Input("Collect five rat tails.");
        cut.Find("[data-add-objective]").Click();
        cut.Find("[data-objective-text]").Input("Collect rat tails");
        cut.Find("[data-objective-target]").Input("rat_tail");
        cut.Find("[data-objective-count]").Input("5");
        cut.FindComponent<QuestRewardEditor>().FindAll("input")[1].Input("100");
        cut.Find("[data-add-stage]").Click();
        cut.Find("[data-stage-state]").Change("Completed");
        SaveButton(cut).Click();

        cut.WaitForAssertion(() =>
        {
            _handler.Requests.Should().Contain(r => r.Method == HttpMethod.Post);
            cut.Instance.HasUnsavedChanges.Should().BeFalse();
        });
        QuestDefinitionDto saved = SavedQuest();
        saved.QuestId.Should().Be("quest_cellar_rats");
        saved.Stages.Should().HaveCount(2);
        saved.Stages[0].Name.Should().Be("Gather rat tails");
        saved.Stages[0].Rewards!.Gold.Should().Be(100);
        saved.Stages[0].ObjectiveGroups.Single().Objectives.Single().RequiredCount.Should().Be(5);
        saved.Stages[1].QuestState.Should().Be("Completed");

        SelectStage(cut, 10);
        cut.Find("[data-stage-name]").Input("Gather seven rat tails");
        cut.Find("[data-objective-count]").Input("7");
        cut.Find(".we-qe-journal").TextContent.Should().Contain("0 / 7");
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => _handler.Requests.Should().Contain(r => r.Method == HttpMethod.Put && r.Path.EndsWith("/quest_cellar_rats")));
        SavedQuest().Stages[0].ObjectiveGroups.Single().Objectives.Single().RequiredCount.Should().Be(7);
        SavedQuest().Stages[0].Name.Should().Be("Gather seven rat tails");
        cut.Instance.HasUnsavedChanges.Should().BeFalse();
    }

    [Test]
    public async Task Quest_StageName_UpdatesNavigationAndLinksAndSurvivesReload()
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: ValidQuest());
        SelectStage(cut, 30);
        cut.Find("[data-stage-name]").Input("Return to the innkeeper");
        cut.Instance.HasUnsavedChanges.Should().BeTrue();
        cut.Find(".we-qe-heading h3").TextContent.Should().Contain("Return to the innkeeper");
        cut.FindAll(".we-qe-nav-button").Single(b => b.QuerySelector(".we-qe-stage-id")?.TextContent == "30")
            .TextContent.Should().Contain("Return to the innkeeper");
        SelectStage(cut, 10);
        cut.Find("[data-next-stage] option[value='30']").TextContent.Should().Contain("Return to the innkeeper");
        cut.Find("[data-stage-transition]").TextContent.Should().Contain("Return to the innkeeper");
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => cut.Instance.HasUnsavedChanges.Should().BeFalse());
        SavedQuest().Stages[1].Name.Should().Be("Return to the innkeeper");
        SavedQuest().Stages[1].JournalText.Should().Be("The cellar is clear.");
        SavedQuest().Stages[1].StageId.Should().Be(30);
        SavedQuest().Stages[0].NextStageId.Should().Be(30);

        await cut.InvokeAsync(() => cut.Instance.OpenExistingAsync("test_entry", CodexEditor.CodexSubType.Quest));
        SelectStage(cut, 30);
        cut.Find("[data-stage-name]").GetAttribute("value").Should().Be("Return to the innkeeper");
        cut.Find("[data-stage-name]").Input("");
        cut.Find(".we-qe-heading h3").TextContent.Should().Contain("Completed");
    }

    [Test]
    public void Quest_BrokenBranch_ShowsTheTargetErrorWithoutSendingAWrite()
    {
        QuestDefinitionDto quest = ValidQuest();
        quest.Stages[0].NextStageId = 999;
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: quest);
        SaveButton(cut).Click();

        cut.Find(".we-qe-validation").TextContent.Should().Contain("linked stage 999 does not exist");
        cut.Find("[data-next-stage]").TextContent.Should().Contain("Unavailable target: stage 999");
        _handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Post || r.Method == HttpMethod.Put);
    }

    [Test]
    public void Quest_StageRemoval_RequiresConfirmationAndBrokenLinksMustBeRepaired()
    {
        QuestDefinitionDto quest = ValidQuest();
        quest.Stages.Insert(1, new QuestStageDto { StageId = 20, JournalText = "Return to the inn." });
        quest.Stages[0].NextStageId = 20;
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: quest);
        SelectStage(cut, 20);
        cut.Find("[data-remove-stage]").Click();
        cut.Find("[role='dialog']").TextContent.Should().Contain("Other stages link here");
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Contains("Keep stage")).Click();
        cut.FindAll("[data-stage-id]").Should().ContainSingle();
        cut.Find("[data-remove-stage]").Click();
        cut.Find("[data-confirm-remove-stage]").Click();
        cut.Find(".we-qe-validation").TextContent.Should().Contain("linked stage 20 does not exist");
        SaveButton(cut).Click();
        _handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Put);
        cut.Find("[data-next-stage]").Change("30");
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => _handler.Requests.Should().Contain(r => r.Method == HttpMethod.Put));
        SavedQuest().Stages.Select(s => s.StageId).Should().Equal(10, 30);
    }

    [Test]
    public void Quest_Close_KeepEditingPreservesDraftAndDiscardCloses()
    {
        int closes = 0;
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: ValidQuest(), onClose: () => closes++);
        cut.Find("[data-stage-journal]").Input("An unsaved journal entry");
        cut.Find(".we-ce-toolbar .btn-secondary").Click();
        cut.Find("[role='dialog'] .btn-secondary").Click();
        closes.Should().Be(0);
        cut.Find("[data-stage-journal]").GetAttribute("value").Should().Be("An unsaved journal entry");
        cut.Instance.HasUnsavedChanges.Should().BeTrue();
        cut.Find(".we-ce-toolbar .btn-secondary").Click();
        cut.Find("[role='dialog'] .btn-outline-danger").Click();
        closes.Should().Be(1);
        cut.Instance.HasUnsavedChanges.Should().BeFalse();
    }

    [Test]
    public void Quest_FailedSaveAndContinue_DoesNotCloseOrLoseTheDraft()
    {
        int closes = 0;
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest,
            quest: ValidQuest(), saveFails: true, onClose: () => closes++);
        cut.Find("[data-stage-journal]").Input("Keep this journal entry");
        cut.Find(".we-ce-toolbar .btn-secondary").Click();
        cut.Find("[role='dialog'] .btn-primary").Click();
        cut.WaitForAssertion(() => cut.Find("[role='dialog']").TextContent.Should().Contain("Save failed"));
        closes.Should().Be(0);
        cut.Instance.HasUnsavedChanges.Should().BeTrue();
        cut.Find("[data-stage-journal]").GetAttribute("value").Should().Be("Keep this journal entry");
    }

    [Test]
    public void Quest_ChangingEntryType_PromptsBeforeDiscardingTheDraft()
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: ValidQuest());
        cut.Find("[data-stage-journal]").Input("Changed");
        cut.Find("select[aria-label='Entry type']").Change("Lore");
        cut.Find("[role='dialog']").TextContent.Should().Contain("unsaved changes");
        cut.FindAll(".we-qe-workspace").Should().ContainSingle();
        cut.Find("[role='dialog'] .btn-outline-danger").Click();
        cut.WaitForAssertion(() => cut.Find(".we-ce-toolbar").TextContent.Should().Contain("New Lore"));
    }

    [Test]
    public void Quest_InvalidConfiguration_CannotBeSavedAndStillCountsAsUnsaved()
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: ValidQuest());
        cut.Find("[data-config-json]").Input("{unfinished");
        cut.Instance.HasUnsavedChanges.Should().BeTrue();
        SaveButton(cut).Click();
        cut.Find(".we-qe-validation").TextContent.Should().Contain("valid JSON object");
        _handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Put);
        cut.Find("[data-config-json]").Input("{\"track_loss\":\"true\"}");
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => _handler.Requests.Should().Contain(r => r.Method == HttpMethod.Put));
    }

    [Test]
    public void Quest_ChangingStageId_UpdatesExistingLinksAndDisallowsDuplicateIds()
    {
        QuestDefinitionDto quest = ValidQuest();
        quest.DefaultStageId = 30;
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: quest);
        SelectStage(cut, 30);
        cut.Find("[data-stage-id]").Change("40");
        cut.FindAll(".we-qe-nav-button").Single(b => b.TextContent == "Quest details").Click();
        cut.Find("[data-default-stage]").GetAttribute("value").Should().Be("40");
        SelectStage(cut, 10);
        cut.Find("[data-next-stage]").GetAttribute("value").Should().Be("40");
        cut.Find("[data-stage-id]").Change("40");
        SaveButton(cut).Click();
        _handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Put);
        cut.Find(".we-qe-validation").TextContent.Should().Contain("positive, unused stage ID");
        cut.Find("[data-stage-id]").GetAttribute("value").Should().Be("40");
        cut.Find("[data-stage-id]").Closest("details")!.HasAttribute("open").Should().BeTrue();
    }

    [Test]
    public async Task Quest_DefaultStage_CanBeSelectedReloadedAndCleared()
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: ValidQuest());
        cut.FindAll(".we-qe-nav-button").Single(b => b.TextContent == "Quest details").Click();
        cut.Find("[data-default-stage]").GetAttribute("value").Should().BeEmpty();
        cut.Find("[data-default-stage]").Change("10");
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => cut.Instance.HasUnsavedChanges.Should().BeFalse());
        SavedQuest().DefaultStageId.Should().Be(10);

        await cut.InvokeAsync(() => cut.Instance.OpenExistingAsync("test_entry", CodexEditor.CodexSubType.Quest));
        cut.FindAll(".we-qe-nav-button").Single(b => b.TextContent == "Quest details").Click();
        cut.Find("[data-default-stage]").GetAttribute("value").Should().Be("10");
        cut.Find("[data-default-stage]").Change("");
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => SavedQuest().DefaultStageId.Should().BeNull());
    }

    [Test]
    public void Quest_MissingDefaultStage_BlocksSavingUntilItIsCleared()
    {
        QuestDefinitionDto quest = ValidQuest();
        quest.DefaultStageId = 999;
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: quest);
        SaveButton(cut).Click();
        cut.Find(".we-qe-validation").TextContent.Should().Contain("Default stage must refer");
        _handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Put);
        cut.Find("[data-default-stage]").Change("");
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => _handler.Requests.Should().Contain(r => r.Method == HttpMethod.Put));
    }

    [Test]
    public void Quest_KeepingTheDraftAfterChangingEntryType_RestoresTheSelector()
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: ValidQuest());
        cut.Find("[data-stage-journal]").Input("Keep this draft");
        cut.Find("select[aria-label='Entry type']").Change("Lore");
        cut.Find("[role='dialog'] .btn-secondary").Click();
        cut.Find("select[aria-label='Entry type']").GetAttribute("value").Should().Be("Quest");
        cut.Find("[data-stage-journal]").GetAttribute("value").Should().Be("Keep this draft");
    }

    [Test]
    public void Quest_ConfigJsonAndTheTypedControlsStayInSync()
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: ValidQuest());
        cut.Find("[data-config-json]").Input("{\"track_loss\":true,\"custom\":42}");
        var checkbox = cut.Find(".we-qe-objective input[type='checkbox']");
        checkbox.HasAttribute("checked").Should().BeTrue();
        checkbox.Change(false);
        cut.Find("[data-config-json]").GetAttribute("value").Should().Contain("\"false\"").And.Contain("42");
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => _handler.Requests.Should().Contain(r => r.Method == HttpMethod.Put));
        SavedQuest().Stages[0].ObjectiveGroups[0].Objectives[0].Config["custom"].ToString().Should().Be("42");
    }

    [Test]
    public void Quest_TabDeactivation_PreservesAndClosesTheInvestigationCanvas()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        const string graph = "{\"clues\":[],\"deductions\":[]}";
        JSInterop.Setup<string>("clueGraphEditor.getGraphJson").SetResult(graph);
        QuestDefinitionDto quest = ValidQuest();
        quest.Stages[0].ObjectiveGroups[0].Objectives[0].TypeTag = "investigate";
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Quest, quest: quest);
        cut.FindAll("button").Single(b => b.TextContent == "Open investigation editor").Click();
        cut.WaitForAssertion(() => JSInterop.Invocations.Should().Contain(i => i.Identifier == "clueGraphEditor.init"));
        cut.SetParametersAndRender(p => p.Add(e => e.IsActive, false));
        cut.FindAll("#ce-graph-canvas").Should().BeEmpty();
        cut.Instance.HasUnsavedChanges.Should().BeTrue();
        cut.Find("[data-config-json]").GetAttribute("value").Should().Contain("graph_json");
        JSInterop.Invocations.Should().Contain(i => i.Identifier == "clueGraphEditor.destroy");
    }

    private static QuestDefinitionDto ValidQuest() => new()
    {
        QuestId = "test_entry", Title = "Test entry", Description = "Clear the cellar.",
        Stages =
        [
            new() { StageId = 10, JournalText = "Collect the tails.", NextStageId = 30,
                ObjectiveGroups = [new() { DisplayName = "Collect tails", Objectives = [new()
                { ObjectiveId = "rat_tails", TypeTag = "collect", DisplayText = "Collect rat tails", TargetTag = "rat_tail", RequiredCount = 5 }] }] },
            new() { StageId = 30, JournalText = "The cellar is clear.", QuestState = "Completed" }
        ]
    };

    private QuestDefinitionDto SavedQuest() => System.Text.Json.JsonSerializer.Deserialize<QuestDefinitionDto>(
        _handler.Bodies.Last(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    private static AngleSharp.Dom.IElement SaveButton(IRenderedComponent<CodexEditor> cut) => cut.Find(".we-ce-toolbar .btn-primary");
    private static void SelectStage(IRenderedComponent<CodexEditor> cut, int id) => cut.FindAll(".we-qe-nav-button")
        .Single(b => b.QuerySelector(".we-qe-stage-id")?.TextContent == id.ToString()).Click();

    private IRenderedComponent<CodexEditor> RenderEntry(CodexEditor.CodexSubType subType,
        bool isNew = false, bool deleteFails = false, Action? onRefresh = null,
        QuestDefinitionDto? quest = null, bool saveFails = false, Action? onClose = null)
    {
        _handler.Requests.Clear();
        _handler.Bodies.Clear();
        _handler.DeleteResponse = null;
        bool deleted = false;
        object entry = subType == CodexEditor.CodexSubType.Lore
            ? new LoreDefinitionDto { LoreId = "test_entry", Title = "Test entry" }
            : quest ?? new QuestDefinitionDto { QuestId = "test_entry", Title = "Test entry" };
        _handler.ResponseFactory = request =>
        {
            if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put)
            {
                if (saveFails) return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = JsonContent.Create(new { error = "Save failed", detail = "Test failure" })
                };
                entry = System.Text.Json.JsonSerializer.Deserialize<QuestDefinitionDto>(_handler.Bodies.Last(),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(entry) };
            }
            if (request.Method == HttpMethod.Delete)
            {
                deleted = !deleteFails;
                return deleteFails
                    ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = JsonContent.Create(new { error = "Delete failed", detail = "Test failure" })
                    }
                    : new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            object body = request.RequestUri!.AbsolutePath.EndsWith("/test_entry")
                ? entry
                : new
                {
                    items = !deleted && request.RequestUri.AbsolutePath == EntryBase(subType)
                        ? new[] { entry } : Array.Empty<object>()
                };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
        };
        Guid endpointId = Guid.NewGuid();
        Services.GetRequiredService<LoreApiService>().SelectEndpoint(endpointId);
        Services.GetRequiredService<QuestApiService>().SelectEndpoint(endpointId);
        Services.GetRequiredService<IndustryApiService>().SelectEndpoint(endpointId);

        return RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OpenOnParameters, true)
            .Add(p => p.EntityKey, isNew ? null : "test_entry")
            .Add(p => p.InitialSubType, subType)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => onClose?.Invoke()))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => onRefresh?.Invoke())));
    }

    private static string EntryBase(CodexEditor.CodexSubType subType) =>
        $"/api/worldengine/codex/{(subType == CodexEditor.CodexSubType.Lore ? "lore" : "quests")}";

    // ==================== CodexSubType Enum ====================

    [Test]
    public void CodexSubType_HasBothValues()
    {
        CodexEditor.CodexSubType.Lore.Should().Be(CodexEditor.CodexSubType.Lore);
        CodexEditor.CodexSubType.Quest.Should().Be(CodexEditor.CodexSubType.Quest);
        CodexEditor.CodexSubType.Lore.Should().NotBe(CodexEditor.CodexSubType.Quest);
    }

    // ==================== Helper types ====================

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path)> Requests { get; } = [];
        public List<string> Bodies { get; } = [];
        public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFactory { get; set; }
        public Task<HttpResponseMessage>? DeleteResponse { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri!.AbsolutePath));
            if (request.Content != null) Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            if (request.Method == HttpMethod.Delete && DeleteResponse != null) return await DeleteResponse;
            if (ResponseFactory != null) return ResponseFactory(request);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
            return response;
        }
    }
}
