﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CryptoHashVerify;
using Dicom.Application.Commands.CreateUser;
using Dicom.Application.Commands.Login;
using Dicom.Application.Common.Interfaces;
using Dicom.Application.Options;
using Dicom.Application.Responses;
using Dicom.Entity.Identity;
using Dicom.Infrastructure.Persistence;
using Dicom.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Dicom.Application.Services
{
    public class AuthenticationService : IAuthentication
    {
        private readonly DicomRepositories _dal;
        private readonly JwtSettings _jwtSettings;
        private readonly IUnitOfWork _unitOfWork;
        public AuthenticationService(JwtSettings jwtSettings, DicomRepositories dal, IUnitOfWork unitOfWork)
        {
            _jwtSettings = jwtSettings;
            _dal = dal;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthenticationResponse> LoginAsync(LoginCommand user)
        {
            var existingUser = await _dal.UserRepositoryAsync.GetQuerable(x => x.Email == user.Email).Include(x => x.Role).FirstOrDefaultAsync();

            if (existingUser == null)
            {
                return new AuthenticationResponse
                {
                    Errors = new[] { "Username / password incorrect" }
                };
            }

            if (!CheckPasswordAsync(existingUser.Password, existingUser.Salt, user.Password))
            {
                return new AuthenticationResponse
                {
                    Errors = new[] { "Username / password incorrect" }
                };
            }

            return await GenerateAuthenticationResponseForUserAsync(existingUser);
        }

        public async Task<AuthenticationResponse> RegisterAsync(CreateUserCommand user)
        {
            var existingUser = await _dal.UserRepositoryAsync.FirstOrDefaultAsync(x => x.Email == user.Email);

            if (existingUser != null)
            {
                return new AuthenticationResponse
                {
                    Errors = new[] { "User with this user id already exists" }
                };
            }

            var (password, salt) = GenerateHashPasswordAndSalt(password: user.Password);

            var role = await _dal.RoleRepositoryAsync.FirstAsync(x => x.Name == RoleNames.User);

            var userId = Guid.NewGuid();
            var result = await _dal.UserRepositoryAsync.InsertAsync(
                new User
                {
                    Id = userId, 
                    Password = password, 
                    Salt = salt, 
                    Role = role,
                    RoleId = role.Id,
                    CreatedAt = DateTime.Now,
                    Email = user.Email,
                    FirstName = "",
                    LastName = "",
                    PhoneNumber = ""
                });

            if (!result.HasValue)
            {
                return new AuthenticationResponse
                {
                    Errors = new[] { "Unable to create user" }
                };
            }

            var newUser = await _dal.UserRepositoryAsync.GetByIDAsync(userId);
            var status = await _dal.UserRepositoryAsync.SaveChangesAsync();

            if (status == 0)
            {
                return new AuthenticationResponse
                {
                    Errors = new[] { "Unable to create user" }
                };
            }

            return await GenerateAuthenticationResponseForUserAsync(newUser);
        }

        public static bool CheckPasswordAsync(string hashPassword, string salt, string password) => HashVerify.VerifyHashString(hashPassword, salt, password);
        

        private Task<AuthenticationResponse> GenerateAuthenticationResponseForUserAsync(User user)
        {
            var jwtHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret); 

            var claims = new List<Claim>
            {
               new Claim(ClaimTypes.Role, user.Role.Name),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
               new Claim("id", user.Id.ToString()),
               new Claim(ClaimTypes.UserData, user.Id.ToString()),
               new Claim(ClaimTypes.Email, user.Email)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Audience = "http://dev.chandu.com",
                Expires = DateTime.UtcNow.Add(_jwtSettings.TokenLifetime),
                SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };


            var token = jwtHandler.CreateToken(tokenDescriptor);

            return Task.FromResult(new AuthenticationResponse
            {
                IsSuccess = true,
                Token = jwtHandler.WriteToken(token)
            });
        }

        public static (string, string) GenerateHashPasswordAndSalt(string password)
        {
            return HashVerify.GenerateHashString(password);
        }
    }
}
