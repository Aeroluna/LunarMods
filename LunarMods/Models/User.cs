﻿namespace LunarMods.Models;

public class User
{
    public ulong Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Roles { get; set; } = string.Empty;
}
