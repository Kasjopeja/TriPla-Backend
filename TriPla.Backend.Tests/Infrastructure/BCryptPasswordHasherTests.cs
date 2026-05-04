using FluentAssertions;
using TriPla.Backend.Infrastructure.Identity;

namespace TriPla.Backend.Tests.Infrastructure;

[TestFixture]
public class BCryptPasswordHasherTests
{
    [Test]
    public void Hash_ProducesVerifiableHash()
    {
        var hasher = new BCryptPasswordHasher();
        var hash = hasher.Hash("s3cret!!");

        hasher.Verify("s3cret!!", hash).Should().BeTrue();
        hasher.Verify("different", hash).Should().BeFalse();
    }

    [Test]
    public void Hash_RejectsBlankPassword()
    {
        var hasher = new BCryptPasswordHasher();
        var act = () => hasher.Hash("  ");
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Verify_ReturnsFalseForInvalidHash()
    {
        var hasher = new BCryptPasswordHasher();
        hasher.Verify("password", "not-a-real-hash").Should().BeFalse();
    }
}
