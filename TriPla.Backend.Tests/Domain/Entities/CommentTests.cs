using FluentAssertions;
using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Tests.Domain.Entities;

[TestFixture]
public class CommentTests
{
    [Test]
    public void Constructor_RequiresContent()
    {
        var act = () => new Comment(Guid.NewGuid(), Guid.NewGuid(), "");
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Edit_UpdatesContentAndEditedAt()
    {
        var comment = new Comment(Guid.NewGuid(), Guid.NewGuid(), "original");
        comment.EditedAt.Should().BeNull();

        comment.Edit("updated");
        comment.Content.Should().Be("updated");
        comment.EditedAt.Should().NotBeNull();
    }
}
