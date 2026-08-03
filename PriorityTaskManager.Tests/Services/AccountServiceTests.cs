using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.Tests.Services
{
    public class AccountServiceTests
    {
        private class InMemoryAccountRepository : IAccountRepository
        {
            public List<Account> Accounts { get; } = new();

            public Account? FindByEmail(string normalizedEmail) =>
                Accounts.FirstOrDefault(a => a.Email == normalizedEmail);

            public Account? FindById(Guid id) =>
                Accounts.FirstOrDefault(a => a.Id == id);

            public void Add(Account account) => Accounts.Add(account);
        }

        [Fact]
        public void Register_NormalizesEmailAndHashesPassword()
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);

            var account = service.Register(" User@Example.com ", "correct horse battery staple");

            Assert.Equal("user@example.com", account.Email);
            Assert.NotEqual("correct horse battery staple", account.PasswordHash);
            Assert.NotEmpty(account.PasswordHash);
            Assert.Single(repository.Accounts);
        }

        [Fact]
        public void Register_Throws_WhenEmailAlreadyRegistered()
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);
            service.Register("user@example.com", "correct horse battery staple");

            Assert.Throws<InvalidOperationException>(() =>
                service.Register("USER@example.com", "another password entirely"));
        }

        [Fact]
        public void ValidateCredentials_ReturnsAccount_WhenPasswordMatches()
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);
            service.Register("user@example.com", "correct horse battery staple");

            var result = service.ValidateCredentials("User@Example.com", "correct horse battery staple");

            Assert.NotNull(result);
            Assert.Equal("user@example.com", result!.Email);
        }

        [Fact]
        public void ValidateCredentials_ReturnsNull_WhenPasswordIncorrect()
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);
            service.Register("user@example.com", "correct horse battery staple");

            var result = service.ValidateCredentials("user@example.com", "wrong password");

            Assert.Null(result);
        }

        [Fact]
        public void ValidateCredentials_ReturnsNull_WhenEmailUnknown()
        {
            var repository = new InMemoryAccountRepository();
            var service = new AccountService(repository);

            var result = service.ValidateCredentials("nobody@example.com", "correct horse battery staple");

            Assert.Null(result);
        }
    }
}
