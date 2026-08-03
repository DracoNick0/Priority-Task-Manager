using Microsoft.AspNetCore.Identity;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    /// <summary>
    /// Coordinates account registration and credential verification for the MVP email + password
    /// account model (see docs/ARCHITECTURE_DATA.md). Uses <see cref="PasswordHasher{TUser}"/>, a
    /// vetted PBKDF2-based implementation, instead of hand-rolled password hashing.
    /// </summary>
    public class AccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly PasswordHasher<Account> _passwordHasher = new();

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        /// <summary>
        /// Registers a new account with the given email and password. Throws
        /// <see cref="InvalidOperationException"/> if an account already exists for that email.
        /// </summary>
        public Account Register(string email, string password)
        {
            var normalizedEmail = NormalizeEmail(email);
            if (_accountRepository.FindByEmail(normalizedEmail) != null)
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            var account = new Account { Email = normalizedEmail };
            account.PasswordHash = _passwordHasher.HashPassword(account, password);
            _accountRepository.Add(account);
            return account;
        }

        /// <summary>
        /// Validates the given email/password pair against the stored account. Returns the matching
        /// <see cref="Account"/> on success, or <c>null</c> if the email is unknown or the password is
        /// incorrect. Deliberately does not distinguish between the two failure cases to avoid
        /// leaking which emails are registered.
        /// </summary>
        public Account? ValidateCredentials(string email, string password)
        {
            var account = _accountRepository.FindByEmail(NormalizeEmail(email));
            if (account == null)
            {
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded
                ? account
                : null;
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    }
}
