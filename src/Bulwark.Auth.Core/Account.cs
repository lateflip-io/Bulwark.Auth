﻿using System.Threading.Tasks;
using Bulwark.Auth.Core.Domain;
using Bulwark.Auth.Core.Exception;
using Bulwark.Auth.Repositories;
using Bulwark.Auth.Repositories.Exception;

namespace Bulwark.Auth.Core;
/// <summary>
/// This class manages common account operations such as create, delete, change password, etc.
/// </summary>
public class Account
{
    private readonly IAccountRepository _accountRepository;
    private readonly JwtTokenizer _tokenizer;

    public Account(IAccountRepository accountRepository,
        JwtTokenizer tokenizer)
    {
        _accountRepository = accountRepository;
        _tokenizer = tokenizer;
    }

    /// <summary>
    /// Creates an account if the oldEmail is not already in use and returns a verification token.
    /// VerificationToken must verified before the account can be used.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <returns cref="VerificationToken"></returns>
    /// <exception cref="BulwarkAccountException"></exception>
    public async Task<VerificationToken> Create(string email,
        string password)
    {
        try
        {
            var verificationModel = await _accountRepository.Create(email,
                password);

            return new VerificationToken(verificationModel.Token,
                verificationModel.Created);
        }
        catch (BulwarkDbDuplicateException exception)
        {
            throw new BulwarkAccountException($"Email: {email} in use", exception);
        }
        catch(BulwarkDbException exception)
        {
            throw new BulwarkAccountException("Cannot create account", exception);
        }
    }

    /// <summary>
    /// Will verify the account with the given oldEmail and verification token. This will enable an account for use
    /// </summary>
    /// <param name="email"></param>
    /// <param name="verificationToken"></param>
    /// <exception cref="BulwarkAccountException"></exception>
    public async Task Verify(string email, string verificationToken)
    {
        try
        {
            await _accountRepository.Verify(email, verificationToken);
        }
        catch (BulwarkDbException exception)
        {
            throw new BulwarkAccountException($"Cannot verify account: {email}", exception);
        }
    }

    /// <summary>
    /// When a account has a valid access token they can delete there account.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="accessToken"></param>
    /// <exception cref="BulwarkAccountException"></exception>
    public async Task Delete(string email, string accessToken)
    {
        try
        {
            var token = await ValidAccessToken(email, accessToken);
            if (token != null)
            {
                await _accountRepository.Delete(email);
            }
        }
        catch (BulwarkDbException exception)
        {
            throw new BulwarkAccountException($"Cannot delete account: {email}", exception);
        }
    }
    
    
    /// <summary>
    ///  When a account has a valid access token they can change there email
    /// </summary>
    /// <param name="oldEmail"></param>
    /// <param name="newEmail"></param>
    /// <param name="accessToken"></param>
    /// <exception cref="BulwarkAccountException"></exception>
    public async Task<VerificationToken> ChangeEmail(string oldEmail, string newEmail,
        string accessToken)
    {
        try
        { 
             var token = await ValidAccessToken(oldEmail, accessToken);
             if (token == null) throw new BulwarkTokenException("Invalid access token");
             var verificationModel = await _accountRepository.ChangeEmail(oldEmail, newEmail);
                 
             return new VerificationToken(verificationModel.Token,
                 verificationModel.Created);
        }
        catch (BulwarkDbDuplicateException exception)
        {
            throw new BulwarkAccountException($"Email: {oldEmail} in use", exception);
        }
        catch (BulwarkDbException exception)
        {
            throw new BulwarkAccountException($"Cannot change oldEmail for account: ${oldEmail}", exception);
        }
    }

    /// <summary>
    /// When a account has a valid access token they can change there password
    /// </summary>
    /// <param name="email"></param>
    /// <param name="newPassword"></param>
    /// <param name="accessToken"></param>
    /// <exception cref="BulwarkAccountException"></exception>
    public async Task ChangePassword(string email, string newPassword,
        string accessToken)
    {
        try
        {
            var token = await ValidAccessToken(email, accessToken);
            if (token != null)
            {
                await _accountRepository.ChangePassword(email, newPassword);
            }
        }
        catch (BulwarkDbException exception)
        {
            throw new BulwarkAccountException($"Cannot change password for account: {email}", exception);
        }
    }

    /// <summary>
    /// This will generate a token that can be used to reset a password. This is sent through an email to a account. 
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    /// <exception cref="BulwarkAccountException"></exception>
    public async Task<string> ForgotPassword(string email)
    {
        try
        {
            var model = await _accountRepository.ForgotPassword(email);
            return model.Token;
        }
        catch (BulwarkDbException exception)
        {
            throw new BulwarkAccountException($"Cannot generate forgot token: {email}", exception);
        }
    }
    
    /// <summary>
    /// This will reset a password for a account using a token generated from ForgotPassword
    /// </summary>
    /// <param name="email"></param>
    /// <param name="token"></param>
    /// <param name="newPassword"></param>
    /// <exception cref="BulwarkAccountException"></exception>
    public async Task ResetPasswordWithToken(string email,
        string token, string newPassword)
    {
        try
        {
            await _accountRepository.ResetPasswordWithToken(email,
                token, newPassword);
        }
        catch (BulwarkDbException exception)
        {
            throw new BulwarkAccountException($"Cannot reset password: {email}", exception);
        }
    }

    /// <summary>
    /// This will validate a access token and return a readable token model
    /// </summary>
    /// <param name="email"></param>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    private async Task<AccessToken> ValidAccessToken(string email, string accessToken)
    {
        var account = await _accountRepository.GetAccount(email);
        var token = _tokenizer.ValidateAccessToken(account.Id,
            accessToken);
        return token;
    }
}