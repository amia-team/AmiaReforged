using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Entities;

[TestFixture]
public class CodexNoteEntryTests
{
    private DateTime _testDate;
    private Guid _testId;

    [SetUp]
    public void SetUp()
    {
        _testDate = new DateTime(2025, 10, 22, 12, 0, 0);
        _testId = Guid.NewGuid();
    }

    #region Construction Tests

    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "This is my note content",
            NoteCategory.General,
            _testDate,
            false,
            false,
            "My Note Title"
        );

        // Assert
        Assert.That(note.Id, Is.EqualTo(_testId));
        Assert.That(note.Content, Is.EqualTo("This is my note content"));
        Assert.That(note.Category, Is.EqualTo(NoteCategory.General));
        Assert.That(note.DateCreated, Is.EqualTo(_testDate));
        Assert.That(note.LastModified, Is.EqualTo(_testDate));
        Assert.That(note.IsDmNote, Is.False);
        Assert.That(note.IsPrivate, Is.False);
        Assert.That(note.Title, Is.EqualTo("My Note Title"));
    }

    [Test]
    public void Constructor_WithoutTitle_CreatesInstanceWithNullTitle()
    {
        // Arrange & Act
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content without title",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Assert
        Assert.That(note.Title, Is.Null);
    }

    [Test]
    public void Constructor_AsDmNote_SetsDmNoteFlag()
    {
        // Arrange & Act
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "DM note content",
            NoteCategory.DmNote,
            _testDate,
            true,
            false
        );

        // Assert
        Assert.That(note.IsDmNote, Is.True);
    }

    [Test]
    public void Constructor_AsPrivateNote_SetsPrivateFlag()
    {
        // Arrange & Act
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Private note content",
            NoteCategory.General,
            _testDate,
            false,
            true
        );

        // Assert
        Assert.That(note.IsPrivate, Is.True);
    }

    [Test]
    public void Constructor_WithAllCategories_CreatesCorrectly()
    {
        // Arrange & Act & Assert
        NoteCategory[] categories = new[]
        {
            NoteCategory.General,
            NoteCategory.Quest,
            NoteCategory.Character,
            NoteCategory.Location,
            NoteCategory.DmNote,
            NoteCategory.DmPrivate
        };

        foreach (NoteCategory category in categories)
        {
            CodexNoteEntry note = new CodexNoteEntry(
                Guid.NewGuid(),
                "Test content",
                category,
                _testDate,
                false,
                false
            );

            Assert.That(note.Category, Is.EqualTo(category));
        }
    }

    #endregion

    #region Constructor Validation Tests

    [Test]
    public void Constructor_WithEmptyContent_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new CodexNoteEntry(
            _testId,
            "",
            NoteCategory.General,
            _testDate,
            false,
            false
        ));

        Assert.That(ex.Message, Does.Contain("Note content cannot be empty"));
        Assert.That(ex.ParamName, Is.EqualTo("content"));
    }

    [Test]
    public void Constructor_WithWhitespaceContent_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new CodexNoteEntry(
            _testId,
            "   ",
            NoteCategory.General,
            _testDate,
            false,
            false
        ));

        Assert.That(ex.Message, Does.Contain("Note content cannot be empty"));
    }

    [Test]
    public void Constructor_WithNullContent_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new CodexNoteEntry(
            _testId,
            null!,
            NoteCategory.General,
            _testDate,
            false,
            false
        ));

        Assert.That(ex.Message, Does.Contain("Note content cannot be empty"));
    }

    [Test]
    public void Constructor_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new CodexNoteEntry(
            Guid.Empty,
            "Valid content",
            NoteCategory.General,
            _testDate,
            false,
            false
        ));

        Assert.That(ex.Message, Does.Contain("Note ID cannot be empty"));
        Assert.That(ex.ParamName, Is.EqualTo("id"));
    }

    #endregion

    #region UpdateContent Tests

    [Test]
    public void UpdateContent_WithValidContent_UpdatesContentAndTimestamp()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Original content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );
        DateTime modifiedDate = _testDate.AddHours(1);

        // Act
        note.UpdateContent("Updated content", modifiedDate);

        // Assert
        Assert.That(note.Content, Is.EqualTo("Updated content"));
        Assert.That(note.LastModified, Is.EqualTo(modifiedDate));
    }

    [Test]
    public void UpdateContent_MultipleTimes_UpdatesCorrectly()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Original content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Act
        note.UpdateContent("First update", _testDate.AddHours(1));
        note.UpdateContent("Second update", _testDate.AddHours(2));
        note.UpdateContent("Third update", _testDate.AddHours(3));

        // Assert
        Assert.That(note.Content, Is.EqualTo("Third update"));
        Assert.That(note.LastModified, Is.EqualTo(_testDate.AddHours(3)));
    }

    [Test]
    public void UpdateContent_WithEmptyContent_ThrowsArgumentException()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Original content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() =>
            note.UpdateContent("", _testDate.AddHours(1)));

        Assert.That(ex.Message, Does.Contain("Note content cannot be empty"));
        Assert.That(ex.ParamName, Is.EqualTo("newContent"));
    }

    [Test]
    public void UpdateContent_WithWhitespaceContent_ThrowsArgumentException()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Original content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() =>
            note.UpdateContent("   ", _testDate.AddHours(1)));

        Assert.That(ex.Message, Does.Contain("Note content cannot be empty"));
    }

    [Test]
    public void UpdateContent_WithNullContent_ThrowsArgumentException()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Original content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() =>
            note.UpdateContent(null!, _testDate.AddHours(1)));

        Assert.That(ex.Message, Does.Contain("Note content cannot be empty"));
    }

    [Test]
    public void UpdateContent_DoesNotChangeOtherProperties()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Original content",
            NoteCategory.Quest,
            _testDate,
            false,
            true,
            "Original Title"
        );

        // Act
        note.UpdateContent("New content", _testDate.AddHours(1));

        // Assert
        Assert.That(note.Id, Is.EqualTo(_testId));
        Assert.That(note.Category, Is.EqualTo(NoteCategory.Quest));
        Assert.That(note.DateCreated, Is.EqualTo(_testDate));
        Assert.That(note.IsPrivate, Is.True);
        Assert.That(note.Title, Is.EqualTo("Original Title"));
    }

    #endregion

    #region UpdateTitle Tests

    [Test]
    public void UpdateTitle_WithValidTitle_UpdatesTitleAndTimestamp()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false,
            "Original Title"
        );
        DateTime modifiedDate = _testDate.AddHours(1);

        // Act
        note.UpdateTitle("New Title", modifiedDate);

        // Assert
        Assert.That(note.Title, Is.EqualTo("New Title"));
        Assert.That(note.LastModified, Is.EqualTo(modifiedDate));
    }

    [Test]
    public void UpdateTitle_WithNull_SetsTitleToNull()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false,
            "Original Title"
        );

        // Act
        note.UpdateTitle(null, _testDate.AddHours(1));

        // Assert
        Assert.That(note.Title, Is.Null);
    }

    [Test]
    public void UpdateTitle_FromNullToValue_SetsTitle()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Act
        note.UpdateTitle("New Title", _testDate.AddHours(1));

        // Assert
        Assert.That(note.Title, Is.EqualTo("New Title"));
    }

    [Test]
    public void UpdateTitle_UpdatesLastModified()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );
        DateTime modifiedDate = _testDate.AddHours(2);

        // Act
        note.UpdateTitle("Title", modifiedDate);

        // Assert
        Assert.That(note.LastModified, Is.EqualTo(modifiedDate));
    }

    #endregion

    #region UpdateCategory Tests

    [Test]
    public void UpdateCategory_WithValidCategory_UpdatesCategoryAndTimestamp()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );
        DateTime modifiedDate = _testDate.AddHours(1);

        // Act
        note.UpdateCategory(NoteCategory.Quest, modifiedDate);

        // Assert
        Assert.That(note.Category, Is.EqualTo(NoteCategory.Quest));
        Assert.That(note.LastModified, Is.EqualTo(modifiedDate));
    }

    [Test]
    public void UpdateCategory_WithAllCategories_WorksCorrectly()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        NoteCategory[] categories = new[]
        {
            NoteCategory.Quest,
            NoteCategory.Character,
            NoteCategory.Location,
            NoteCategory.DmNote,
            NoteCategory.DmPrivate
        };

        // Act & Assert
        DateTime modifiedDate = _testDate;
        foreach (NoteCategory category in categories)
        {
            modifiedDate = modifiedDate.AddHours(1);
            note.UpdateCategory(category, modifiedDate);
            Assert.That(note.Category, Is.EqualTo(category));
        }
    }

    [Test]
    public void UpdateCategory_UpdatesLastModified()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );
        DateTime modifiedDate = _testDate.AddHours(3);

        // Act
        note.UpdateCategory(NoteCategory.Location, modifiedDate);

        // Assert
        Assert.That(note.LastModified, Is.EqualTo(modifiedDate));
    }

    #endregion

    #region UpdatePrivacy Tests

    [Test]
    public void UpdatePrivacy_FromFalseToTrue_UpdatesPrivacyAndTimestamp()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );
        DateTime modifiedDate = _testDate.AddHours(1);

        // Act
        note.UpdatePrivacy(true, modifiedDate);

        // Assert
        Assert.That(note.IsPrivate, Is.True);
        Assert.That(note.LastModified, Is.EqualTo(modifiedDate));
    }

    [Test]
    public void UpdatePrivacy_FromTrueToFalse_UpdatesPrivacyAndTimestamp()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            true
        );
        DateTime modifiedDate = _testDate.AddHours(1);

        // Act
        note.UpdatePrivacy(false, modifiedDate);

        // Assert
        Assert.That(note.IsPrivate, Is.False);
        Assert.That(note.LastModified, Is.EqualTo(modifiedDate));
    }

    [Test]
    public void UpdatePrivacy_WithSameValue_StillUpdatesTimestamp()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            true
        );
        DateTime modifiedDate = _testDate.AddHours(1);

        // Act
        note.UpdatePrivacy(true, modifiedDate);

        // Assert
        Assert.That(note.IsPrivate, Is.True);
        Assert.That(note.LastModified, Is.EqualTo(modifiedDate));
    }

    #endregion

    #region MatchesSearch Tests

    [Test]
    public void MatchesSearch_WithContentMatch_ReturnsTrue()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "This is a note about dragons and treasure",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Act & Assert
        Assert.That(note.MatchesSearch("dragons"), Is.True);
        Assert.That(note.MatchesSearch("treasure"), Is.True);
        Assert.That(note.MatchesSearch("note"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithTitleMatch_ReturnsTrue()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content here",
            NoteCategory.General,
            _testDate,
            false,
            false,
            "Dragon Encounter Notes"
        );

        // Act & Assert
        Assert.That(note.MatchesSearch("dragon"), Is.True);
        Assert.That(note.MatchesSearch("encounter"), Is.True);
        Assert.That(note.MatchesSearch("notes"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithNoMatch_ReturnsFalse()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Simple content",
            NoteCategory.General,
            _testDate,
            false,
            false,
            "Simple Title"
        );

        // Act & Assert
        Assert.That(note.MatchesSearch("dragon"), Is.False);
        Assert.That(note.MatchesSearch("treasure"), Is.False);
    }

    [Test]
    public void MatchesSearch_WithNullTitle_OnlyMatchesContent()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content with dragons",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Act & Assert
        Assert.That(note.MatchesSearch("dragons"), Is.True);
        Assert.That(note.MatchesSearch("title"), Is.False);
    }

    [Test]
    public void MatchesSearch_WithEmptySearchTerm_ReturnsFalse()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Act & Assert
        Assert.That(note.MatchesSearch(""), Is.False);
        Assert.That(note.MatchesSearch("   "), Is.False);
    }

    [Test]
    public void MatchesSearch_WithNullSearchTerm_ReturnsFalse()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Act & Assert
        Assert.That(note.MatchesSearch(null!), Is.False);
    }

    [Test]
    public void MatchesSearch_IsCaseInsensitive()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content with DRAGONS",
            NoteCategory.General,
            _testDate,
            false,
            false,
            "Title with TREASURE"
        );

        // Act & Assert
        Assert.That(note.MatchesSearch("dragons"), Is.True);
        Assert.That(note.MatchesSearch("DRAGONS"), Is.True);
        Assert.That(note.MatchesSearch("treasure"), Is.True);
        Assert.That(note.MatchesSearch("TREASURE"), Is.True);
    }

    #endregion

    #region MatchesCategory Tests

    [Test]
    public void MatchesCategory_WithMatchingCategory_ReturnsTrue()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.Quest,
            _testDate,
            false,
            false
        );

        // Act & Assert
        Assert.That(note.MatchesCategory(NoteCategory.Quest), Is.True);
    }

    [Test]
    public void MatchesCategory_WithDifferentCategory_ReturnsFalse()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Act & Assert
        Assert.That(note.MatchesCategory(NoteCategory.Quest), Is.False);
        Assert.That(note.MatchesCategory(NoteCategory.Character), Is.False);
    }

    [Test]
    public void MatchesCategory_WithAllCategories_WorksCorrectly()
    {
        // Arrange & Act & Assert
        NoteCategory[] categories = new[]
        {
            NoteCategory.General,
            NoteCategory.Quest,
            NoteCategory.Character,
            NoteCategory.Location,
            NoteCategory.DmNote,
            NoteCategory.DmPrivate
        };

        foreach (NoteCategory category in categories)
        {
            CodexNoteEntry note = new CodexNoteEntry(
                Guid.NewGuid(),
                "Content",
                category,
                _testDate,
                false,
                false
            );

            Assert.That(note.MatchesCategory(category), Is.True);
        }
    }

    #endregion

    #region LastModified Timestamp Tests

    [Test]
    public void LastModified_InitiallyEqualsDateCreated()
    {
        // Arrange & Act
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false
        );

        // Assert
        Assert.That(note.LastModified, Is.EqualTo(note.DateCreated));
    }

    [Test]
    public void LastModified_UpdatesWithEachOperation()
    {
        // Arrange
        CodexNoteEntry note = new CodexNoteEntry(
            _testId,
            "Content",
            NoteCategory.General,
            _testDate,
            false,
            false,
            "Title"
        );

        // Act & Assert
        DateTime time1 = _testDate.AddHours(1);
        note.UpdateContent("New content", time1);
        Assert.That(note.LastModified, Is.EqualTo(time1));

        DateTime time2 = _testDate.AddHours(2);
        note.UpdateTitle("New title", time2);
        Assert.That(note.LastModified, Is.EqualTo(time2));

        DateTime time3 = _testDate.AddHours(3);
        note.UpdateCategory(NoteCategory.Quest, time3);
        Assert.That(note.LastModified, Is.EqualTo(time3));

        DateTime time4 = _testDate.AddHours(4);
        note.UpdatePrivacy(true, time4);
        Assert.That(note.LastModified, Is.EqualTo(time4));
    }

    #endregion
}
