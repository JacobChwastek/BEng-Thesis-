﻿using System;
using Dicom.Application.Responses;
using MediatR;

namespace Dicom.Application.Commands
{
    public class CreateUserCommand : IRequest<AuthenticationResponse>
    {
        public string Password { get; set; }
        
        public string Email { get; set; }
    }
}
