﻿namespace Bulwark.Auth.Common.Payloads;
public class RefreshToken
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string DeviceId { get; set; }
}


