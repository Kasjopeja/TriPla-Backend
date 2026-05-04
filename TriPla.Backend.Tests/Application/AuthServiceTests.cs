using FluentAssertions;
using Moq;
using TriPla.Backend.Application.Auth;
using TriPla.Backend.Application.DTOs.Auth;
using TriPla.Backend.Application.Interfaces;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Tests.Fakes;

namespace TriPla.Backend.Tests.Application;

[TestFixture]
public class AuthServiceTests
{
    [Test]
    public async Task RegisterAsync_CreatesUserAndReturnsToken()
    {
        var uow = new InMemoryUnitOfWork();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("HASHED");

        var tokenProvider = new Mock<ITokenProvider>();
        tokenProvider.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("TOKEN");

        var service = new AuthService(uow, hasher.Object, tokenProvider.Object);

        var result = await service.RegisterAsync(new RegisterRequest("Alice", "Smith", "alice@example.com", "s3cret!!"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("TOKEN");
        uow.UsersStore.Store.Should().HaveCount(1);
        uow.UsersStore.Store.Values.Single().PasswordHash.Should().Be("HASHED");
    }

    [Test]
    public async Task RegisterAsync_FailsIfEmailAlreadyExists()
    {
        var uow = new InMemoryUnitOfWork();
        var existing = new User("Bob", "Jones", "bob@example.com", "h");
        await uow.Users.AddAsync(existing);

        var service = new AuthService(uow, Mock.Of<IPasswordHasher>(), Mock.Of<ITokenProvider>());

        var result = await service.RegisterAsync(new RegisterRequest("Bob", "Jones", "bob@example.com", "x"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Test]
    public async Task LoginAsync_FailsForUnknownEmail()
    {
        var uow = new InMemoryUnitOfWork();
        var service = new AuthService(uow, Mock.Of<IPasswordHasher>(), Mock.Of<ITokenProvider>());

        var result = await service.LoginAsync(new LoginRequest("missing@example.com", "x"));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task LoginAsync_FailsForIncorrectPassword()
    {
        var uow = new InMemoryUnitOfWork();
        var existing = new User("Alice", "Smith", "alice@example.com", "real-hash");
        await uow.Users.AddAsync(existing);

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Verify("wrong", "real-hash")).Returns(false);

        var service = new AuthService(uow, hasher.Object, Mock.Of<ITokenProvider>());

        var result = await service.LoginAsync(new LoginRequest("alice@example.com", "wrong"));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task LoginAsync_ReturnsTokenForValidCredentials()
    {
        var uow = new InMemoryUnitOfWork();
        var existing = new User("Alice", "Smith", "alice@example.com", "real-hash");
        await uow.Users.AddAsync(existing);

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Verify("correct", "real-hash")).Returns(true);
        var tokenProvider = new Mock<ITokenProvider>();
        tokenProvider.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("TOKEN");

        var service = new AuthService(uow, hasher.Object, tokenProvider.Object);

        var result = await service.LoginAsync(new LoginRequest("alice@example.com", "correct"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("TOKEN");
    }
}
