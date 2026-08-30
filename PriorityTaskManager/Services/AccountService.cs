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
        private readonly bool _defaultNewAccountsToSubscription;

        /// <param name="accountRepository">Backing store for accounts.</param>
        /// <param name="defaultNewAccountsToSubscription">
        /// Issue #50's MVP/beta grace period flag: when <c>true</c>, <see cref="Register(string, string)"/>
        /// (the overload used by real self-service registration) grants <see cref="SubscriptionTier.Subscription"/>
        /// instead of <see cref="SubscriptionTier.Free"/>, since no real payment integration exists yet. Flip to
        /// <c>false</c> at V1 once real payment/entitlement enforcement lands.
        /// </param>
        public AccountService(IAccountRepository accountRepository, bool defaultNewAccountsToSubscription = false)
        {
            _accountRepository = accountRepository;
            _defaultNewAccountsToSubscription = defaultNewAccountsToSubscription;
        }

        /// <summary>
        /// Registers a new account with the given email and password, granting the beta-grace-period
        /// default tier (see <see cref="_defaultNewAccountsToSubscription"/>). Throws
        /// <see cref="InvalidOperationException"/> if an account already exists for that email.
        /// </summary>
        public Account Register(string email, string password) =>
            Register(email, password, _defaultNewAccountsToSubscription ? SubscriptionTier.Subscription : SubscriptionTier.Free);

        /// <summary>
        /// Registers a new account with the given email, password, and an explicit tier (bypassing the
        /// beta-grace-period default; used by dev seeding and tests). Throws
        /// <see cref="InvalidOperationException"/> if an account already exists for that email.
        /// </summary>
        public Account Register(string email, string password, SubscriptionTier tier)
        {
            var normalizedEmail = NormalizeEmail(email);
            if (_accountRepository.FindByEmail(normalizedEmail) != null)
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            var account = new Account { Email = normalizedEmail, SubscriptionTier = tier };
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
